using SharpPcap;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

class Attack
{
    private static Arp arp = new Arp();
    private static Ndp ndp = new Ndp();
    private static List<Thread> threadList = new List<Thread>();

    public int getThreadCount()
    {
        return threadList.Count;
    }

    public void startAttack(ILiveDevice pCaptureDevice, TargetList pTargetList)
    {
        if (pCaptureDevice != null)
        {
            foreach (Target target in pTargetList.getTargetList())
            {
                if (target.t_ipAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
                {
                    Thread thread = new Thread(() => arp.threadMethodeArpResponse(target, pCaptureDevice));
                    thread.Start();

                    threadList.Add(thread);
                }
                else if (target.t_ipAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
                {
                    Thread thread = new Thread(() => ndp.threadMethodeNdpResponse(target, pCaptureDevice));
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
}