using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;

class Arp_Class
{
    public static Packet BuildArpPacket(IPAddress pDestIPAddr, IPAddress pSourceIPAddr, PhysicalAddress pDestHwAddr, LibPcapLiveDevice pLiveDevice)
    {
        EthernetPacket ethernetPacket = new(pLiveDevice.MacAddress, pDestHwAddr, EthernetType.Arp);
        ArpPacket arpframe = new(ArpOperation.Response, pDestHwAddr, pDestIPAddr, pLiveDevice.MacAddress, pSourceIPAddr);
        ethernetPacket.PayloadPacket = arpframe;
        return ethernetPacket;
    }

    public static PhysicalAddress GetPhysicalAddress(IPAddress pIPAddress, LibPcapLiveDevice pLiveDevice)
    {
        ARP arper = new(pLiveDevice);

        PhysicalAddress resolvedPhyAddr = arper.Resolve(pIPAddress);

        if (resolvedPhyAddr == null)
        {
            Console.WriteLine("#> MAC address could not be resolved! Make sure that the IP is reachable!. Press \"ENTER\" to continue.");
            Console.Read();
        }

        return arper.Resolve(pIPAddress);
    }
}
