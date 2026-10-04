using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class TargetList_Class
{
    private readonly List<Target_Class> targetList = new List<Target_Class>();
    private readonly Scanner_Class scanner = new();

    public int GetLength()
    {
        return targetList.Count;
    }

    public List<Target_Class> GetTargetList()
    {
        return targetList;
    }

    /// <summary>
    /// Scans the local link for IPv6 hosts, lets the user pick a victim and a
    /// gateway from the results (or type an address), and resolves any missing
    /// MAC automatically via NDP.
    /// </summary>
    public void ScanAndAddTarget(LibPcapLiveDevice? pLiveDevice)
    {
        if (pLiveDevice == null)
        {
            Console.WriteLine("#> No network adapter configured. Choose one with [1] first. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("#> Scanning the link for IPv6 hosts (approx. 4s)...");
        List<DiscoveredHost> hosts;
        try
        {
            hosts = scanner.DiscoverHosts(pLiveDevice, 4);
        }
        catch (Exception ex)
        {
            Console.WriteLine("#> Scan failed: {0} Press \"ENTER\".", ex.Message);
            Console.ReadLine();
            return;
        }

        if (hosts.Count == 0)
        {
            Console.WriteLine("#> No IPv6 hosts answered. They may have IPv6 disabled or be filtering");
            Console.WriteLine("#> ICMPv6. You can still add a target manually with [3]. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        PrintHosts(hosts);

        DiscoveredHost? victim = PickHost(hosts, "Select the TARGET by number: ");
        if (victim == null)
        {
            return;
        }

        IPAddress? gatewayIp;
        PhysicalAddress? gatewayMac;

        DiscoveredHost? gatewayHost = PickHost(hosts, "Select the GATEWAY by number (or press ENTER to type it): ");
        if (gatewayHost != null)
        {
            gatewayIp = gatewayHost.IpAddress;
            gatewayMac = gatewayHost.MacAddress;
        }
        else
        {
            gatewayIp = ReadIpAddress("Gateway IPv6-Address: ");
            if (gatewayIp == null)
            {
                return;
            }
            gatewayMac = ResolveOrAskMac(pLiveDevice, gatewayIp, "Gateway");
            if (gatewayMac == null)
            {
                return;
            }
        }

        Target_Class target = new()
        {
            t_ipAddr = victim.IpAddress,
            t_phAddr = victim.MacAddress,
            s_ipAddr = gatewayIp,
            s_phAddr = gatewayMac
        };

        targetList.Add(target);
        Console.WriteLine("#> Target added: {0} via gateway {1}. Press \"ENTER\".", victim.IpAddress, gatewayIp);
        Console.ReadLine();
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
                // IPv6: try to resolve the MAC addresses automatically via NDP,
                // falling back to manual entry when a host does not answer.
                target.t_ipAddr = tempAddr;
                target.t_phAddr = ResolveOrAskMac(pLiveDevice, tempAddr, "Target");
                if (target.t_phAddr == null)
                {
                    return;
                }

                target.s_ipAddr = ReadIpAddress("Gateway IPv6-Address: ");
                if (target.s_ipAddr == null)
                {
                    return;
                }
                target.s_phAddr = ResolveOrAskMac(pLiveDevice, target.s_ipAddr, "Gateway");
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

    /// <summary>
    /// Resolves the MAC for an IPv6 address via NDP, prompting for manual entry
    /// if the host does not answer in time.
    /// </summary>
    private PhysicalAddress? ResolveOrAskMac(LibPcapLiveDevice pLiveDevice, IPAddress pAddress, string pLabel)
    {
        Console.WriteLine("#> Resolving {0} MAC for {1} via NDP...", pLabel, pAddress);
        PhysicalAddress? mac = scanner.ResolveMac(pLiveDevice, pAddress);

        if (mac != null)
        {
            Console.WriteLine("#> {0} MAC resolved: {1}", pLabel, mac);
            return mac;
        }

        Console.WriteLine("#> Could not resolve automatically.");
        return ReadPhysicalAddress(pLabel + " Physical-Address (or ENTER to cancel): ");
    }

    private static void PrintHosts(List<DiscoveredHost> pHosts)
    {
        Console.WriteLine();
        Console.WriteLine("Found {0} host(s):", pHosts.Count);
        for (int i = 0; i < pHosts.Count; i++)
        {
            Console.WriteLine("  [{0}] {1,-40} {2} {3}",
                i,
                pHosts[i].IpAddress,
                pHosts[i].MacAddress,
                pHosts[i].Hostname);
        }
        Console.WriteLine();
    }

    private static DiscoveredHost? PickHost(List<DiscoveredHost> pHosts, string pPrompt)
    {
        Console.Write(pPrompt);
        string? input = Console.ReadLine();

        if (int.TryParse(input, out int index) && index >= 0 && index < pHosts.Count)
        {
            return pHosts[index];
        }
        return null;
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
