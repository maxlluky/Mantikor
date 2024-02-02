using System.Net;
using System.Net.NetworkInformation;

class Target_Class
{
    //--Used to reach Target_Class
    public IPAddress t_ipAddr;
    public PhysicalAddress t_phAddr;

    //--Used to spoof Target_Class with wrong Information
    public IPAddress s_ipAddr;
    public PhysicalAddress s_phAddr;
}