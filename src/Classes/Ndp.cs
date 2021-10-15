using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

class Ndp
{
    //--Variables
    private ILiveDevice liveDevice;

    public Ndp(ILiveDevice pLiveDevice)
    {
        liveDevice = pLiveDevice;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pDestIPAddr"></param>
    /// <param name="pSourceIPAddr"></param>
    /// <param name="pDestHwAddr"></param>
    /// <returns></returns>
    public Packet buildNdpPacket(IPAddress pDestIPAddr, IPAddress pSourceIPAddr, PhysicalAddress pDestHwAddr)
    {
        NeighborAdvertisement nghbAdvPacket = new NeighborAdvertisement(pSourceIPAddr, liveDevice.MacAddress);
        IcmpV6Packet icmpv6Packet = new IcmpV6Packet(new ByteArraySegment(nghbAdvPacket.GetBytes()));
        icmpv6Packet.Type = (IcmpV6Type)136;
        icmpv6Packet.Code = 0;

        IPv6Packet ipv6Packet = new IPv6Packet(pSourceIPAddr, pDestIPAddr);
        EthernetPacket ethernetPacket = new EthernetPacket(liveDevice.MacAddress, pDestHwAddr, EthernetType.IPv6);

        ipv6Packet.PayloadPacket = icmpv6Packet;
        ethernetPacket.PayloadPacket = ipv6Packet;

        return ethernetPacket;
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

        foreach (byte b in buffer)
        {
            Debug.WriteLine(b.ToString());
        }

        return buffer;
    }
}