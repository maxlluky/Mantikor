using PacketDotNet;
using SharpPcap;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

class ARP
{
    public void threadMethodeArpResponse(IPAddress pDestIP, IPAddress pSourceIP, PhysicalAddress pDestHwAddr, int pDelay, ICaptureDevice pCaptureDevice)
    {
        while (true)
        {
            try
            {
                sendArpResponse(pDestIP, pSourceIP, pDestHwAddr, pCaptureDevice);
                Thread.Sleep(pDelay);
            }
            catch (ThreadAbortException)
            {
                break;
            }
        }
    }

    private bool sendArpResponse(IPAddress pDestIPAddr, IPAddress pSourceIPAddr, PhysicalAddress pDestHwAddr, ICaptureDevice pDevice)
    {
        try
        {
            EthernetPacket ethernetPacket = new EthernetPacket(pDevice.MacAddress, pDestHwAddr, EthernetType.Arp);

            ArpPacket arpframe = new ArpPacket(ArpOperation.Response, pDestHwAddr, pDestIPAddr, pDevice.MacAddress, pSourceIPAddr);

            ethernetPacket.PayloadPacket = arpframe;

            pDevice.SendPacket(ethernetPacket);

            return true;
        }
        catch (Exception eX)
        {
            Console.WriteLine(eX.Message);
        }

        return false;
    }
}