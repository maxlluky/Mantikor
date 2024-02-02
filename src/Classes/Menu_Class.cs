using SharpPcap;
using SharpPcap.LibPcap;
using System.Reflection;

class Menu_Class
{
    public LibPcapLiveDevice captureDevice;
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
        Console.WriteLine("[2] Define new Targets");
        Console.WriteLine("[3] Print/Edit Target-List => {0}\n", pTargetList.GetLength());
        Console.WriteLine("[4] Start Attack : Threads => {0}", pAttack.GetThreadCount());
        Console.WriteLine("[5] Force Stop\n");
        
    }

    public void ConfigureNetworkAdapter()
    {
        var devices = LibPcapLiveDeviceList.Instance;

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

        try
        {
            captureDevice = devices[int.Parse(Console.ReadLine())];
            captureDevice.Open();
            deviceDescription = captureDevice.Description;
        }
        catch (FormatException) { }
    }
}
