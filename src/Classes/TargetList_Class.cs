using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class TargetList_Class
{
    private readonly List<Target_Class> targetList = [];
    private readonly Arp_Class arp = new();


    public int GetLength()
    {
        return targetList.Count;
    }

    public List<Target_Class> GetTargetList()
    {
        return targetList;
    }

    public void AddNewTarget(LibPcapLiveDevice pLiveDevice)
    {
        try
        {
            Target_Class target = new();
            Console.Write("Target IPv4 / IPv6-Address: ");
            IPAddress tempAddr = IPAddress.Parse(Console.ReadLine());

            if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                target.t_ipAddr = tempAddr;
                target.t_phAddr = Arp_Class.GetPhysicalAddress(target.t_ipAddr, pLiveDevice);

                Console.Write("Gateway IPv4-Address: ");
                target.s_ipAddr = IPAddress.Parse(Console.ReadLine());
                target.s_phAddr = Arp_Class.GetPhysicalAddress(target.s_ipAddr, pLiveDevice);
            }
            else if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
            {
                target.t_ipAddr = tempAddr;

                Console.Write("Target Physical-Address: ");
                target.t_phAddr = ParsePhysicalAddress(Console.ReadLine());

                Console.Write("Gateway IPv6-Address: ");
                target.s_ipAddr = IPAddress.Parse(Console.ReadLine());

                Console.Write("Gateway Physical-Address: ");
                target.s_phAddr = ParsePhysicalAddress(Console.ReadLine());
            }

            targetList.Add(target);
        }
        catch (FormatException)
        {

        }
    }

    public void PrintTargetList()
    {
        for (int i = 0; i < targetList.Count; i++)
        {
            Console.WriteLine("[{0}]. Target Nr.{0}\n=> IP-Address:{1}\n=> Physical-Address:{2}\n===> Matched Gateway\n=> IP-Address:{3}\n=> Physical-Address:{4}\n",
                i,
                targetList[i].t_ipAddr,
                targetList[i].t_phAddr,
                targetList[i].s_ipAddr,
                targetList[i].s_phAddr);
        }

        if (targetList.Count > 0)
        {
            Console.Write("Remove with [Entry-Nr] or press \"ENTER\": ");
            string removeNr = Console.ReadLine();

            try
            {
                targetList.RemoveAt(Convert.ToInt32(removeNr));
            }
            catch (Exception) { }
        }
    }

    private static PhysicalAddress ParsePhysicalAddress(string pPhysicalAddress)
    {
        string phyAddrNew;
        if (pPhysicalAddress.Contains(':'))
        {
            phyAddrNew = pPhysicalAddress.Replace(":", "-");
        }
        else
        {
            phyAddrNew = pPhysicalAddress;
        }
        return PhysicalAddress.Parse(phyAddrNew);
    }
}