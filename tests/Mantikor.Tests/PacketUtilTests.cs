using System.Net;
using System.Net.NetworkInformation;

namespace Mantikor.Tests;

/// <summary>
/// Tests for the pure packet- and address-building helpers. These contain the fiddly bit/byte layout
/// (checksums, multicast mapping, EUI-64) where mistakes are easy and silent, and they need no network.
/// </summary>
public class PacketUtilTests
{
    [Fact]
    public void SolicitedNodeMulticast_TakesLast24BitsOfTarget()
    {
        IPAddress target = IPAddress.Parse("fe80::211:22ff:fe33:4455");

        IPAddress result = PacketUtil_Class.SolicitedNodeMulticast(target);

        Assert.Equal(IPAddress.Parse("ff02::1:ff33:4455"), result);
    }

    [Theory]
    [InlineData("ff02::1:ff33:4455", new byte[] { 0x33, 0x33, 0xff, 0x33, 0x44, 0x55 })]
    [InlineData("ff02::1", new byte[] { 0x33, 0x33, 0x00, 0x00, 0x00, 0x01 })]
    public void MulticastMac_MapsLast32BitsAfter3333(string multicast, byte[] expected)
    {
        PhysicalAddress mac = PacketUtil_Class.MulticastMac(IPAddress.Parse(multicast));

        Assert.Equal(expected, mac.GetAddressBytes());
    }

    [Fact]
    public void DeriveLinkLocalFromMac_FollowsEui64AndFlipsUniversalLocalBit()
    {
        // RFC 4291 App. A: insert ff:fe in the middle and flip bit 1 of the first octet (00 -> 02).
        PhysicalAddress mac = PhysicalAddress.Parse("00-11-22-33-44-55");

        IPAddress linkLocal = PacketUtil_Class.DeriveLinkLocalFromMac(mac);

        Assert.Equal(IPAddress.Parse("fe80::211:22ff:fe33:4455"), linkLocal);
    }

    [Fact]
    public void ComputeIcmpV6Checksum_IsNonZeroAndValidatesToZero()
    {
        IPAddress source = IPAddress.Parse("fe80::1");
        IPAddress destination = IPAddress.Parse("ff02::1");
        byte[] icmp = new byte[8];
        icmp[0] = 128; // echo request

        ushort checksum = PacketUtil_Class.ComputeIcmpV6Checksum(source, destination, icmp);
        Assert.NotEqual(0, checksum);

        // RFC 1071: with the checksum written back in, the receiver's folded one's-complement sum is 0xFFFF.
        icmp[2] = (byte)(checksum >> 8);
        icmp[3] = (byte)(checksum & 0xFF);
        Assert.Equal(0, PacketUtil_Class.ComputeIcmpV6Checksum(source, destination, icmp));
    }

    [Fact]
    public void ComputeIcmpV6Checksum_DependsOnAddresses()
    {
        byte[] icmp = new byte[8];
        icmp[0] = 128;

        ushort a = PacketUtil_Class.ComputeIcmpV6Checksum(IPAddress.Parse("fe80::1"), IPAddress.Parse("ff02::1"), icmp);
        ushort b = PacketUtil_Class.ComputeIcmpV6Checksum(IPAddress.Parse("fe80::2"), IPAddress.Parse("ff02::1"), icmp);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BuildIcmpV6Frame_HasCorrectEthernetAndIpv6Headers()
    {
        PhysicalAddress srcMac = PhysicalAddress.Parse("00-11-22-33-44-55");
        PhysicalAddress dstMac = PhysicalAddress.Parse("66-77-88-99-AA-BB");
        IPAddress srcIp = IPAddress.Parse("fe80::1");
        IPAddress dstIp = IPAddress.Parse("fe80::2");
        byte[] message = { 0x80, 0x00, 0x12, 0x34, 0x00, 0x00, 0x00, 0x01 };

        byte[] frame = PacketUtil_Class.BuildIcmpV6Frame(srcMac, dstMac, srcIp, dstIp, message);

        Assert.Equal(14 + 40 + message.Length, frame.Length);
        Assert.Equal(dstMac.GetAddressBytes(), frame[0..6]);   // Ethernet destination
        Assert.Equal(srcMac.GetAddressBytes(), frame[6..12]);  // Ethernet source
        Assert.Equal(new byte[] { 0x86, 0xDD }, frame[12..14]); // EtherType IPv6
        Assert.Equal(0x06, frame[14] >> 4);                     // IP version 6
        Assert.Equal(58, frame[20]);                            // Next header = ICMPv6
        Assert.Equal(255, frame[21]);                           // Hop limit (RFC 4861)
        Assert.Equal(srcIp.GetAddressBytes(), frame[22..38]);   // IPv6 source
        Assert.Equal(dstIp.GetAddressBytes(), frame[38..54]);   // IPv6 destination
        Assert.Equal(message, frame[54..]);                     // ICMPv6 payload
    }
}
