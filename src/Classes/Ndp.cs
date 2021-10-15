using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using System;
using System.Diagnostics;
using System.IO;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pLocalIpAddress"></param>
    /// <returns></returns>
    public PhysicalAddress getPhysicalAddress(IPAddress pLocalIpAddress, IPAddress pTargetIpAddress)
    {
        PhysicalAddress physicalAddress = null;

        //--Listen
        liveDevice.Open(DeviceModes.Promiscuous, 1000);

        // Capture packets using GetNextPacket()
        PacketCapture e;
        GetPacketStatus retval;

        //--Send Neighboar Solicitation
        liveDevice.SendPacket(buildIcmpPacket(pLocalIpAddress, pTargetIpAddress));

        int counter = 0;
        while ((retval = liveDevice.GetNextPacket(out e)) == GetPacketStatus.PacketRead)
        {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            EthernetPacket ethernetPacket = packet.Extract<EthernetPacket>();
            IPv6Packet ipv6Packet = packet.Extract<IPv6Packet>();
            IcmpV6Packet icmpv6Packet = packet.Extract<IcmpV6Packet>();

            if (ethernetPacket != null && ipv6Packet != null && icmpv6Packet != null)
            {
                if (icmpv6Packet.Type == IcmpV6Type.NeighborAdvertisement && icmpv6Packet.Bytes.Length == 24)
                {
                    using (MemoryStream memoryStream = new MemoryStream(icmpv6Packet.Bytes))
                    {
                        using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                        {
                            binaryReader.BaseStream.Position = 8;
                            byte[] targetAddress = binaryReader.ReadBytes(16);

                            if (targetAddress == pTargetIpAddress.GetAddressBytes())
                            {
                                physicalAddress = ethernetPacket.SourceHardwareAddress;
                                break;
                            }
                        }
                    }
                }
            }

            if (counter <= 100)
            {
                Debug.WriteLine("No Response found!");
                break;
            }
        }

        return physicalAddress;
    }

    public Packet buildIcmpPacket(IPAddress pLocalIpAddress, IPAddress pTargetIpAddress)
    {
        byte[] nghbSolPayload;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                binaryWriter.Write(pTargetIpAddress.GetAddressBytes()); // Target Address
                binaryWriter.Write(new byte[] { 0x01, 0x01 }); // Type & Length
                binaryWriter.Write(liveDevice.MacAddress.GetAddressBytes()); // Link-Layer address
            }
            nghbSolPayload = memoryStream.ToArray();
        }

        IcmpV6Packet icmpv6Packet = new IcmpV6Packet(new ByteArraySegment(new byte[8]))
        {
            Type = IcmpV6Type.NeighborSolicitation,
            PayloadData = nghbSolPayload,
        };

        IPv6Packet ipv6Packet = new IPv6Packet(pLocalIpAddress, pTargetIpAddress);
        EthernetPacket ethernetPacket = new EthernetPacket(liveDevice.MacAddress, PhysicalAddress.Parse("33-33-00-00-00-01"), EthernetType.IPv6);

        // Build Neighboar Solicitation
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