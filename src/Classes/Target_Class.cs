using System.Net;
using System.Net.NetworkInformation;

class Target_Class
{
    //--Used to reach the target
    public IPAddress? t_ipAddr;
    public PhysicalAddress? t_phAddr;

    //--Used to spoof the target with wrong information (the impersonated host, e.g. the gateway)
    public IPAddress? s_ipAddr;
    public PhysicalAddress? s_phAddr;

    /// <summary>
    /// True only when every address needed to launch an attack against this
    /// target has been resolved. Guards against null MAC addresses that would
    /// otherwise crash a worker thread.
    /// </summary>
    public bool IsComplete()
    {
        return t_ipAddr != null && t_phAddr != null && s_ipAddr != null && s_phAddr != null;
    }
}
