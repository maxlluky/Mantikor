using System.Net;
using System.Net.NetworkInformation;

class Host
{
    public IPAddress ipv4Address { get; set; } = IPAddress.Parse("0.0.0.0");
    public IPAddress ipv6Address { get; set; } = IPAddress.Parse("0:0:0:0:0:0:0:1");
    public PhysicalAddress physicalAddress { get; set; } = PhysicalAddress.Parse("00-00-00-00-00-00");
}
