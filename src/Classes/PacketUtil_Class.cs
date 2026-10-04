using PacketDotNet;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// Shared low-level helpers for building IPv6/ICMPv6 frames and deriving the
/// multicast addresses used by the Neighbor Discovery Protocol. Kept in one
/// place so the spoofer (<see cref="Ndp_Class"/>) and the scanner
/// (<see cref="Scanner_Class"/>) build packets the exact same way.
/// </summary>
static class PacketUtil_Class
{
    private const byte IcmpV6ProtocolNumber = 58;

    /// <summary>
    /// Assembles a complete Ethernet + IPv6 + ICMPv6 frame. The hop limit is
    /// fixed at 255 because every NDP message requires it (RFC 4861 §11.2).
    /// </summary>
    public static byte[] BuildIcmpV6Frame(PhysicalAddress pSrcMac, PhysicalAddress pDstMac, IPAddress pSrcIp, IPAddress pDstIp, byte[] pIcmpMessage)
    {
        IPv6Packet ipv6 = new(pSrcIp, pDstIp)
        {
            HopLimit = 255,
            NextHeader = PacketDotNet.ProtocolType.IcmpV6,
            PayloadLength = (ushort)pIcmpMessage.Length
        };
        EthernetPacket ethernet = new(pSrcMac, pDstMac, EthernetType.IPv6);

        byte[] frame = new byte[14 + 40 + pIcmpMessage.Length];
        Buffer.BlockCopy(ethernet.Bytes, 0, frame, 0, 14);
        Buffer.BlockCopy(ipv6.Bytes, 0, frame, 14, 40);
        Buffer.BlockCopy(pIcmpMessage, 0, frame, 54, pIcmpMessage.Length);
        return frame;
    }

    /// <summary>
    /// Computes the ICMPv6 checksum over the IPv6 pseudo-header followed by the
    /// ICMPv6 message (RFC 4443 §2.3). The checksum field in
    /// <paramref name="pIcmpMessage"/> must be zero when this is called.
    /// </summary>
    public static ushort ComputeIcmpV6Checksum(IPAddress pSourceIp, IPAddress pDestinationIp, byte[] pIcmpMessage)
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

    /// <summary>
    /// Returns the solicited-node multicast address (ff02::1:ffXX:XXXX) for the
    /// given unicast address - the destination a Neighbor Solicitation is sent
    /// to (RFC 4291 §2.7.1).
    /// </summary>
    public static IPAddress SolicitedNodeMulticast(IPAddress pTarget)
    {
        byte[] t = pTarget.GetAddressBytes();
        byte[] multicast =
        {
            0xff, 0x02, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0x01, 0xff, t[13], t[14], t[15]
        };
        return new IPAddress(multicast);
    }

    /// <summary>
    /// Maps an IPv6 multicast address to its Ethernet multicast MAC
    /// (33:33 followed by the last four bytes of the address, RFC 2464 §7).
    /// </summary>
    public static PhysicalAddress MulticastMac(IPAddress pMulticastIp)
    {
        byte[] ip = pMulticastIp.GetAddressBytes();
        return new PhysicalAddress(new byte[] { 0x33, 0x33, ip[12], ip[13], ip[14], ip[15] });
    }

    /// <summary>
    /// Picks a usable IPv6 source address for the given adapter: its real
    /// link-local address if one is assigned, otherwise a link-local address
    /// derived from the MAC via EUI-64 (RFC 4291 Appendix A).
    /// </summary>
    public static IPAddress GetLinkLocalSource(LibPcapLiveDevice pDevice)
    {
        foreach (var address in pDevice.Addresses)
        {
            IPAddress? ip = address.Addr?.ipAddress;
            if (ip != null && ip.AddressFamily == AddressFamily.InterNetworkV6 && ip.IsIPv6LinkLocal)
            {
                return ip;
            }
        }

        return DeriveLinkLocalFromMac(pDevice.MacAddress);
    }

    internal static IPAddress DeriveLinkLocalFromMac(PhysicalAddress pMac)
    {
        byte[] m = pMac.GetAddressBytes();
        byte[] ll =
        {
            0xfe, 0x80, 0, 0, 0, 0, 0, 0,
            (byte)(m[0] ^ 0x02), m[1], m[2], 0xff, 0xfe, m[3], m[4], m[5]
        };
        return new IPAddress(ll);
    }
}
