using System.Net;
using System.Net.NetworkInformation;

namespace Mantikor.Tests;

/// <summary>
/// Tests the Neighbor Advertisement byte layout, including the corrective (healing) variant used on stop.
/// </summary>
public class NdpTests
{
    static readonly PhysicalAddress OurMac = PhysicalAddress.Parse("00-11-22-33-44-55");
    static readonly PhysicalAddress VictimMac = PhysicalAddress.Parse("66-77-88-99-AA-BB");
    static readonly PhysicalAddress RealGatewayMac = PhysicalAddress.Parse("CC-DD-EE-00-11-22");
    static readonly IPAddress VictimIp = IPAddress.Parse("fe80::2");
    static readonly IPAddress GatewayIp = IPAddress.Parse("fe80::1");

    // Offset of the ICMPv6 message inside the Ethernet+IPv6 frame.
    const int IcmpOffset = 54;

    [Fact]
    public void BuildNeighborAdvertisement_HasCorrectTypeFlagsOptionAndAdvertisesOurMac()
    {
        var ndp = new Ndp_Class(OurMac);

        byte[] frame = ndp.BuildNeighborAdvertisement(VictimIp, GatewayIp, VictimMac);
        byte[] icmp = frame[IcmpOffset..];

        Assert.Equal(136, icmp[0]);                                   // Neighbor Advertisement
        Assert.Equal(0, icmp[1]);                                     // code
        Assert.Equal(0x60, icmp[4]);                                  // Solicited + Override flags
        Assert.Equal(GatewayIp.GetAddressBytes(), icmp[8..24]);       // advertised target address
        Assert.Equal(0x02, icmp[24]);                                 // target link-layer option type
        Assert.Equal(0x01, icmp[25]);                                 // option length (8-byte units)
        Assert.Equal(OurMac.GetAddressBytes(), icmp[26..32]);         // spoof: advertises our MAC

        // Destination is the victim; source IP is the address being advertised.
        Assert.Equal(VictimMac.GetAddressBytes(), frame[0..6]);
        Assert.Equal(GatewayIp.GetAddressBytes(), frame[22..38]);
        Assert.Equal(VictimIp.GetAddressBytes(), frame[38..54]);
    }

    [Fact]
    public void BuildNeighborAdvertisement_CorrectiveVariantAdvertisesRealOwnerMac()
    {
        var ndp = new Ndp_Class(OurMac);

        // Healing: tell the victim that the gateway is really at the gateway's own MAC.
        byte[] frame = ndp.BuildNeighborAdvertisement(VictimIp, GatewayIp, VictimMac, RealGatewayMac);
        byte[] icmp = frame[IcmpOffset..];

        Assert.Equal(RealGatewayMac.GetAddressBytes(), icmp[26..32]);
    }

    [Fact]
    public void BuildNeighborAdvertisement_HasValidChecksum()
    {
        var ndp = new Ndp_Class(OurMac);
        byte[] icmp = ndp.BuildNeighborAdvertisement(VictimIp, GatewayIp, VictimMac)[IcmpOffset..];

        // Recomputing over the message with its checksum in place must fold to zero.
        Assert.Equal(0, PacketUtil_Class.ComputeIcmpV6Checksum(GatewayIp, VictimIp, icmp));
    }
}
