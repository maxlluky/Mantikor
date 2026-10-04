using SharpPcap;
using SharpPcap.LibPcap;
using System.Net.NetworkInformation;
using System.Reflection;

class Menu_Class
{
    public LibPcapLiveDevice? captureDevice;
    private static string deviceDescription = "(not configured)";

    public static void PrintFrontend(TargetList_Class pTargetList, Attack_Class pAttack)
    {
        Console.Clear();

        string logo = @"
░█▀▄▀█ ─█▀▀█ ░█▄─░█ ▀▀█▀▀ ▀█▀ ░█─▄▀ ░█▀▀▀█ ░█▀▀█
░█░█░█ ░█▄▄█ ░█░█░█ ─░█── ░█─ ░█▀▄─ ░█──░█ ░█▄▄▀
░█──░█ ░█─░█ ░█──▀█ ─░█── ▄█▄ ░█─░█ ░█▄▄▄█ ░█─░█
";

        Console.WriteLine(logo);

        Console.WriteLine("MANTIKOR {0} & SharpPcap {1}\n", Assembly.GetExecutingAssembly().GetName().Version, Pcap.SharpPcapVersion);

        Console.WriteLine("Use the numbers to navigate!");
        Console.WriteLine("To revoke or skip an entry, press the \"ENTER\" button\n");
        Console.WriteLine("[1] Configure Network Adapter => {0}", deviceDescription);
        Console.WriteLine("[2] Scan for IPv6 Hosts (auto MAC)");
        Console.WriteLine("[3] Define new Targets (manual)");
        Console.WriteLine("[4] Print/Edit Target-List => {0}\n", pTargetList.GetLength());
        Console.WriteLine("[5] Start Attack : Threads => {0}", pAttack.GetThreadCount());
        Console.WriteLine("[6] Force Stop");
        Console.WriteLine("[0] Exit\n");
    }

    public void ConfigureNetworkAdapter()
    {
        LibPcapLiveDeviceList devices;
        try
        {
            devices = LibPcapLiveDeviceList.Instance;
        }
        catch (DllNotFoundException)
        {
            // The native capture library is missing. On Linux install libpcap
            // (e.g. "sudo apt install libpcap0.8"); on Windows install Npcap.
            Console.WriteLine("#> Could not load the native packet capture library (libpcap/Npcap).");
            Console.WriteLine("#> Linux: install it with e.g. \"sudo apt install libpcap0.8\".");
            Console.WriteLine("#> Windows: install Npcap from https://npcap.com. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        if (devices.Count < 1)
        {
            Console.WriteLine("No devices were found on this machine. On Linux make sure you run with the");
            Console.WriteLine("required privileges (sudo, or grant the binary cap_net_raw/cap_net_admin).");
            Console.WriteLine("Press \"ENTER\" to continue.");
            Console.ReadLine();
            return;
        }

        for (int i = 0; i < devices.Count; i++)
        {
            var dev = devices[i];
            string description = string.IsNullOrEmpty(dev.Description) ? dev.Name : dev.Description;

            // Reading the MAC requires the device to be open; a failure here (for
            // example on virtual or loopback interfaces) must not abort the whole
            // listing.
            PhysicalAddress? mac = null;
            try
            {
                dev.Open();
                mac = dev.MacAddress;
            }
            catch (PcapException) { }
            finally
            {
                if (dev.Opened)
                {
                    dev.Close();
                }
            }

            Console.WriteLine("{0}) {1} {2}", i, description, mac);
        }

        Console.WriteLine();
        Console.Write("Please choose an Adapter: ");
        string? choice = Console.ReadLine();

        if (!int.TryParse(choice, out int index) || index < 0 || index >= devices.Count)
        {
            Console.WriteLine("#> Invalid selection. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        // Release a previously selected adapter before switching.
        if (captureDevice != null && captureDevice.Opened)
        {
            captureDevice.Close();
        }

        try
        {
            captureDevice = devices[index];
            captureDevice.Open();
            deviceDescription = string.IsNullOrEmpty(captureDevice.Description) ? captureDevice.Name : captureDevice.Description;
        }
        catch (PcapException ex)
        {
            captureDevice = null;
            deviceDescription = "(not configured)";
            Console.WriteLine("#> Could not open adapter: {0}", ex.Message);
            Console.WriteLine("#> On Linux this usually means missing privileges. Press \"ENTER\".");
            Console.ReadLine();
        }
    }
}
