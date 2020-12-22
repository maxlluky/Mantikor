using System.Net;
using System.Net.NetworkInformation;

class Host
{
    public IPAddress ipAddress { get; set; } = IPAddress.Parse("0.0.0.0");
    public PhysicalAddress hwAddress { get; set; } = PhysicalAddress.Parse("00-00-00-00-00-00");
}
