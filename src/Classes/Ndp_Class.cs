using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

class Ndp_Class(ILiveDevice pLiveDevice)
{
    //--Variables
    private readonly ILiveDevice liveDevice = pLiveDevice;
    const int Flags = 0x60000000;
    readonly byte[] OptionMAC = [0x02, 0x01];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pDestIPAddr"></param>
    /// <param name="pSourceIPAddr"></param>
    /// <param name="pPhysicalAddress"></param>
    /// <returns></returns>
    public Packet BuildNdpPacket(IPAddress pDestIPAddr, IPAddress pSourceIPAddr, PhysicalAddress pPhysicalAddress)
    {
        // NeighborAdvertisement nghbAdvPacket = new(pSourceIPAddr, liveDevice.MacAddress);
        IcmpV6Packet icmpv6Packet = new(new ByteArraySegment(GetBytes(pSourceIPAddr, pPhysicalAddress)))
        {
            Type = (IcmpV6Type)136,
            Code = 0
        };

        IPv6Packet ipv6Packet = new(pSourceIPAddr, pDestIPAddr);
        EthernetPacket ethernetPacket = new(liveDevice.MacAddress, pPhysicalAddress, EthernetType.IPv6);

        ipv6Packet.PayloadPacket = icmpv6Packet;
        ethernetPacket.PayloadPacket = ipv6Packet;

        return ethernetPacket;
    }

    public byte[] GetBytes(IPAddress targetAddress, PhysicalAddress physicalAddress)
    {
        byte[] buffer = new byte[32];
        int pos = 4;
        Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.NetworkToHostOrder(Flags)), 0, buffer, pos, sizeof(int));
        pos += 4;
        Buffer.BlockCopy(targetAddress.GetAddressBytes(), 0, buffer, pos, targetAddress.GetAddressBytes().Length);
        pos += targetAddress.GetAddressBytes().Length;
        Buffer.BlockCopy(OptionMAC, 0, buffer, pos, OptionMAC.Length);
        pos += OptionMAC.Length;
        Buffer.BlockCopy(physicalAddress.GetAddressBytes(), 0, buffer, pos, physicalAddress.GetAddressBytes().Length);

        foreach (byte b in buffer)
        {
            Debug.WriteLine(b.ToString());
        }

        return buffer;
    }
}