using PacketDotNet;
using SharpPcap;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

class Arp
{
    //--Variables
    private ILiveDevice liveDevice;

    public Arp(ILiveDevice pLiveDevice)
    {
        liveDevice = pLiveDevice;
    }

    public Packet buildArpPacket(IPAddress pDestIPAddr, IPAddress pSourceIPAddr, PhysicalAddress pDestHwAddr)
    {
        EthernetPacket ethernetPacket = new EthernetPacket(liveDevice.MacAddress, pDestHwAddr, EthernetType.Arp);
        ArpPacket arpframe = new ArpPacket(ArpOperation.Response, pDestHwAddr, pDestIPAddr, liveDevice.MacAddress, pSourceIPAddr);
        ethernetPacket.PayloadPacket = arpframe;
        return ethernetPacket;
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
