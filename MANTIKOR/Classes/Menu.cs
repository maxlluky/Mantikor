using SharpPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

class Menu
{
    private static ICaptureDevice captureDevice;
    private static string deviceDescription = "(not configured)";
    
    private static Host gateway = new Host();
    private static List<Host> targetList = new List<Host>();
    private static List<Thread> threadList = new List<Thread>();

    private static Arp arp = new Arp();
    private static Ndp ndp = new Ndp();

    public void printFrontend()
    {
        Console.Clear();

        string logo = @"
░█▀▄▀█ ─█▀▀█ ░█▄─░█ ▀▀█▀▀ ▀█▀ ░█─▄▀ ░█▀▀▀█ ░█▀▀█ 
░█░█░█ ░█▄▄█ ░█░█░█ ─░█── ░█─ ░█▀▄─ ░█──░█ ░█▄▄▀ 
░█──░█ ░█─░█ ░█──▀█ ─░█── ▄█▄ ░█─░█ ░█▄▄▄█ ░█─░█
";

        Console.WriteLine(logo);

        Console.WriteLine("MANTIKOR {0} & SharpPcap {1}\n", Assembly.GetExecutingAssembly().GetName().Version, SharpPcap.Version.VersionString);

        Console.WriteLine("Use the numbers to navigate!");
        Console.WriteLine("[1] Configure Network Adapter => {0}", deviceDescription);
        Console.WriteLine("[2] Define new Target : Targets => {0}", targetList.Count);
        Console.WriteLine("[3] Print/Edit Target-List\n");
        Console.WriteLine("[4] Start Attack : Threads => {0}", threadList.Count);
        Console.WriteLine("[5] Force Stop\n");
    }

    public void configureNetworkAdapter()
    {
        var devices = CaptureDeviceList.Instance;

        if (devices.Count < 1)
        {
            Console.WriteLine("No devices were found on this machine");
            return;
        }

        int i = 0;

        foreach (var dev in devices)
        {
            dev.Open();
            Console.WriteLine("{0}) {1} {2}", i, dev.Description, dev.MacAddress);
            dev.Close();
            i++;
        }

        Console.WriteLine();
        Console.Write("Please choose an Adapter: ");

        captureDevice = devices[int.Parse(Console.ReadLine())];
        captureDevice.Open();
        deviceDescription = captureDevice.Description;
    }

    public void addNewTarget()
    {
        Host host = new Host();
        Console.Write("Target IP-Address [enter IPv4 or IPv6]\t: ");
        IPAddress tempAddr = IPAddress.Parse(Console.ReadLine());

        if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
        {
            host.ipAddress = tempAddr;
            host.physicalAddress = arp.getPhysicalAddress(host.ipAddress);

            if (gateway.ipAddress == null)
            {
                Console.Write("Gateway IPv4-Address:\t\t\t ");
                gateway.ipAddress = IPAddress.Parse(Console.ReadLine());
                gateway.physicalAddress = arp.getPhysicalAddress(gateway.ipAddress);
            }
        }
        else if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
        {
            host.ipAddress = tempAddr;

            Console.Write("Target Physical-Address\t\t\t: ");
            host.physicalAddress = parsePhysicalAddress(Console.ReadLine());

            if (gateway.ipAddress == null)
            {
                Console.Write("Gateway IPv6-Address\t\t\t: ");
                gateway.ipAddress = IPAddress.Parse(Console.ReadLine());

                Console.Write("Gateway Physical-Address\t\t: ");
                gateway.physicalAddress = parsePhysicalAddress(Console.ReadLine());
            }
        }

        targetList.Add(host);
    }

    public void printTargetList()
    {
        Console.WriteLine("Gateway IP-Address:{0} Physical-Address:{1}",
                gateway.ipAddress,
                gateway.physicalAddress);

        for (int i = 0; i < targetList.Count; i++)
        {
            Console.WriteLine("{0}. IP-Address:{1} Physical-Address:{2}",
                i,
                targetList[i].ipAddress,
                targetList[i].physicalAddress);
        }

        if (targetList.Count > 0)
        {


            Console.Write("Remove [Nr]: ");
            string removeNr = Console.ReadLine();

            try
            {
                targetList.RemoveAt(Convert.ToInt32(removeNr));
            }
            catch (Exception) { }
        }
    }

    public void startAttack()
    {
        if (captureDevice != null)
        {
            foreach (Host target in targetList)
            {
                if (target.ipAddress.AddressFamily.Equals(AddressFamily.InterNetwork))
                {
                    Thread thread = new Thread(() => arp.threadMethodeArpResponse(target, gateway, captureDevice));
                    thread.Start();

                    threadList.Add(thread);
                }
                else if (target.ipAddress.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                {
                    Thread thread = new Thread(() => ndp.threadMethodeNdpResponse(target, gateway, captureDevice));
                    thread.Start();

                    threadList.Add(thread);
                }
            }
        }
    }

    public void forceStop()
    {
        foreach (Thread item in threadList)
        {
            item.Abort();
        }
        threadList.Clear();
    }

    private PhysicalAddress parsePhysicalAddress(string pPhysicalAddress)
    {
        string phyAddrNew = "00-00-00-00-00-00";

        if (pPhysicalAddress.Contains(":"))
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
