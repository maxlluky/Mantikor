using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class TargetList_Class
{
    private readonly List<Target_Class> targetList = new List<Target_Class>();

    public int GetLength()
    {
        return targetList.Count;
    }

    public List<Target_Class> GetTargetList()
    {
        return targetList;
    }

    public void AddNewTarget(LibPcapLiveDevice? pLiveDevice)
    {
        if (pLiveDevice == null)
        {
            Console.WriteLine("#> No network adapter configured. Choose one with [1] first. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        try
        {
            Target_Class target = new();

            IPAddress? tempAddr = ReadIpAddress("Target IPv4 / IPv6-Address: ");
            if (tempAddr == null)
            {
                return;
            }

            if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                // IPv4: both MAC addresses can be resolved automatically via ARP.
                target.t_ipAddr = tempAddr;
                target.t_phAddr = Arp_Class.GetPhysicalAddress(target.t_ipAddr, pLiveDevice);

                target.s_ipAddr = ReadIpAddress("Gateway IPv4-Address: ");
                if (target.s_ipAddr == null)
                {
                    return;
                }
                target.s_phAddr = Arp_Class.GetPhysicalAddress(target.s_ipAddr, pLiveDevice);
            }
            else if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
            {
                // IPv6: there is no ARP, so the hardware addresses are entered
                // manually (they can be read from the target's neighbor cache).
                target.t_ipAddr = tempAddr;
                target.t_phAddr = ReadPhysicalAddress("Target Physical-Address: ");

                target.s_ipAddr = ReadIpAddress("Gateway IPv6-Address: ");
                if (target.s_ipAddr == null)
                {
                    return;
                }
                target.s_phAddr = ReadPhysicalAddress("Gateway Physical-Address: ");
            }
            else
            {
                return;
            }

            if (!target.IsComplete())
            {
                Console.WriteLine("#> Target not added: some addresses could not be resolved. Press \"ENTER\".");
                Console.ReadLine();
                return;
            }

            targetList.Add(target);
        }
        catch (FormatException)
        {
            Console.WriteLine("#> Invalid input - target not added. Press \"ENTER\".");
            Console.ReadLine();
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
            string? removeNr = Console.ReadLine();

            if (int.TryParse(removeNr, out int index) && index >= 0 && index < targetList.Count)
            {
                targetList.RemoveAt(index);
            }
        }
    }

    private static IPAddress? ReadIpAddress(string pPrompt)
    {
        Console.Write(pPrompt);
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        if (IPAddress.TryParse(input.Trim(), out IPAddress? address))
        {
            return address;
        }

        Console.WriteLine("#> \"{0}\" is not a valid IP address.", input);
        return null;
    }

    private static PhysicalAddress? ReadPhysicalAddress(string pPrompt)
    {
        Console.Write(pPrompt);
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        // Accept both colon- and hyphen-separated notations.
        string normalized = input.Trim().Replace(":", "-").ToUpperInvariant();

        if (PhysicalAddress.TryParse(normalized, out PhysicalAddress? address))
        {
            return address;
        }

        Console.WriteLine("#> \"{0}\" is not a valid MAC address.", input);
        return null;
    }
}
