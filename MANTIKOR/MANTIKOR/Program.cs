using SharpPcap;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace MANTIKOR
{
    class Program
    {
        //--CaptureDevice
        private static ICaptureDevice captureDevice;

        //--Configuration
        private static string deviceDescription = "(not configured)";

        //--Represents the Source (attacker machine)
        private static Host source = new Host();

        //--Represents the Gateway
        private static Host gateway = new Host();

        //--Contains all definen targets (IP-MAC binding)
        private static List<Host> targetList = new List<Host>();

        //--Contains all Threads (objects). Includes aborted Threads.
        private static List<Thread> threadList = new List<Thread>();

        //--Used to send Arp. Represents the class to send Arp Packets.
        private static Arp arp = new Arp();

        //--Used to send Ndp. Represents the class to send Ndp Packets.
        private static Ndp ndp = new Ndp();
        

        static void Main(string[] args)
        {
            initializeConsole();
        }

        private static void printFrontend()
        {
            Console.Clear();

            string logo = @"
░█▀▄▀█ ─█▀▀█ ░█▄─░█ ▀▀█▀▀ ▀█▀ ░█─▄▀ ░█▀▀▀█ ░█▀▀█ 
░█░█░█ ░█▄▄█ ░█░█░█ ─░█── ░█─ ░█▀▄─ ░█──░█ ░█▄▄▀ 
░█──░█ ░█─░█ ░█──▀█ ─░█── ▄█▄ ░█─░█ ░█▄▄▄█ ░█─░█
";

            Console.WriteLine(logo);

            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("MANTIKOR {0} => SharpPcap {1}\n", Assembly.GetExecutingAssembly().GetName().Version, ver);

            Console.WriteLine("Use the numbers to navigate!");
            Console.WriteLine("[1] Configure Network Adapter => {0}", deviceDescription);
            Console.WriteLine("[2] Set Gateway & Source");
            Console.WriteLine("[3] Define new Target : Targets => {0}", targetList.Count);
            Console.WriteLine("[4] Print/Edit Target-List\n");
            Console.WriteLine("[5] Start Attack : Threads => {0}", threadList.Count);
            Console.WriteLine("[6] Force Stop\n");
        }

        private static void initializeConsole()
        {
            Console.Title = "MANTIKOR v." + Assembly.GetExecutingAssembly().GetName().Version;

            while (true)
            {
                printFrontend();
                Console.Write("#>");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        configureNetworkAdapter();
                        break;
                    case "2":
                        confGatewayAndSource();
                        break;
                    case "3":
                        editTargetList();
                        break;
                    case "4":
                        printTargetList();
                        break;                        
                    case "5":
                        startAttack();
                        break;
                    case "6":
                        forceStop();
                        break;
                    default:
                        break;
                }
            }
        }

        private static void configureNetworkAdapter()
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
            Console.Write("-- Please choose a device to send a packet on: ");

            captureDevice = devices[int.Parse(Console.ReadLine())];

            captureDevice.Open();

            deviceDescription = captureDevice.Description;
        }

        private static void confGatewayAndSource()
        {
            Console.WriteLine("MANTIKOR supports the use of IPv4 only, IPV6 only and Dual-Stack");
            Console.Write("Which protocols do you want to configure [IPv4/IPv6/both]: ");
            string input = Console.ReadLine();            

            switch (input)
            {
                case "IPv4":
                    Console.Write("Gateway IPv4-Address\t: ");
                    gateway.ipv4Address = IPAddress.Parse(Console.ReadLine());

                    Console.Write("Source IPv4-Address\t: ");
                    source.ipv4Address = IPAddress.Parse(Console.ReadLine());

                    source.physicalAddress = arp.getPhysicalAddress(source.ipv4Address);
                    gateway.physicalAddress = arp.getPhysicalAddress(gateway.ipv4Address);
                    break;
                case "IPv6":
                    Console.Write("Gateway IPv6-Address\t\t: ");
                    gateway.ipv6Address = IPAddress.Parse(Console.ReadLine());

                    Console.Write("Gateway Physical-Address\t: ");
                    gateway.physicalAddress = PhysicalAddress.Parse(Console.ReadLine());

                    Console.Write("Source IPv6-Address\t\t: ");
                    source.ipv6Address = IPAddress.Parse(Console.ReadLine());

                    Console.Write("Source Physical-Address\t\t: ");
                    source.physicalAddress = PhysicalAddress.Parse(Console.ReadLine());
                    break;
                case "both":
                    Console.Write("Gateway IPv4-Address\t: ");
                    gateway.ipv4Address = IPAddress.Parse(Console.ReadLine());

                    Console.Write("Gateway IPv6-Address\t: ");
                    gateway.ipv6Address = IPAddress.Parse(Console.ReadLine());

                    Console.Write("Source IPv4-Address\t: ");
                    source.ipv4Address = IPAddress.Parse(Console.ReadLine());

                    Console.Write("Source IPv6-Address\t: ");
                    source.ipv6Address = IPAddress.Parse(Console.ReadLine());

                    source.physicalAddress = arp.getPhysicalAddress(source.ipv4Address);
                    gateway.physicalAddress = arp.getPhysicalAddress(gateway.ipv4Address);
                    break;
            }                         
        }

        private static void editTargetList()
        {
            Console.Write("Target IP-Address [IPv4/IPv6]\t: ");
            Host target = new Host();
            target.ipv4Address = IPAddress.Parse(Console.ReadLine());
            if (target.ipv4Address.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                target.physicalAddress = arp.getPhysicalAddress(target.ipv4Address);
            }
            else
            {
                Console.Write("Target Hw-Address\t\t: ");
                target.physicalAddress = PhysicalAddress.Parse(Console.ReadLine());
            }

            targetList.Add(target);
        }

        private static void printTargetList()
        {
            for (int i=0; i < targetList.Count; i++)
            {
                Console.WriteLine("{0}. IP-Address: {1} Physical-Address: {2}",i, targetList[i].ipv4Address, targetList[i].physicalAddress);
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

        private static void startAttack()
        {
            if (captureDevice != null)
            {
                foreach (Host target in targetList)
                {
                    if (target.ipv4Address.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        Thread thread = new Thread(() => arp.threadMethodeArpResponse(source, target, gateway, captureDevice));
                        thread.Start();

                        threadList.Add(thread);
                    }
                    else if (target.ipv4Address.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                    {
                        Thread thread = new Thread(() => ndp.threadMethodeNdpResponse(source, target, gateway, captureDevice));
                        thread.Start();

                        threadList.Add(thread);
                    }
                }
            }
        }

        private static void forceStop()
        {
            foreach (Thread item in threadList)
            {
                item.Abort();
            }
        }
    }
}
