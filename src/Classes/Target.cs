using System.Net;
using System.Net.NetworkInformation;

class Target
{
    //--Used to reach Target
    public IPAddress t_ipAddr;
    public PhysicalAddress t_phAddr;

    //--Used to spoof Target with wrong Information
    public IPAddress s_ipAddr;
    public PhysicalAddress s_phAddr;
}