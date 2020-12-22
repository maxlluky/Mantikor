using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

class Host
{
    public IPAddress ipAddress { get; set; } = IPAddress.Parse("0.0.0.0");
    public PhysicalAddress hwAddress { get; set; } = PhysicalAddress.Parse("00-00-00-00-00-00");
}
