using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>
/// A single host found on the local link during an IPv6 scan.
/// </summary>
class DiscoveredHost
{
    public IPAddress IpAddress { get; }
    public PhysicalAddress MacAddress { get; }
    public string Hostname { get; set; } = "";

    public DiscoveredHost(IPAddress pIpAddress, PhysicalAddress pMacAddress)
    {
        IpAddress = pIpAddress;
        MacAddress = pMacAddress;
    }
}

/// <summary>
/// Active IPv6 reconnaissance on the local link:
///  - <see cref="DiscoverHosts"/> pings the all-nodes multicast group and
///    collects every responder's IPv6 address and MAC (plus a best-effort
///    reverse-DNS hostname).
///  - <see cref="ResolveMac"/> is the IPv6 counterpart to an ARP lookup: it
///    sends a Neighbor Solicitation and returns the MAC from the matching
///    Neighbor Advertisement, so target and gateway MAC addresses no longer
///    have to be typed in by hand.
/// </summary>
class Scanner_Class
{
    private const byte EchoRequestType = 128;
    private const byte NeighborSolicitationType = 135;
    private const byte NeighborAdvertisementType = 136;
    private const byte SourceLinkLayerAddressOption = 0x01;
    private const byte OptionLengthInEightByteUnits = 0x01;

    /// <summary>
    /// Discovers IPv6 hosts by sending an ICMPv6 Echo Request to the all-nodes
    /// link-local multicast address (ff02::1) and capturing the replies for
    /// <paramref name="pScanSeconds"/> seconds.
    /// </summary>
    public List<DiscoveredHost> DiscoverHosts(LibPcapLiveDevice pDevice, int pScanSeconds)
    {
        var found = new Dictionary<string, DiscoveredHost>();
        PhysicalAddress ourMac = pDevice.MacAddress;
        IPAddress source = PacketUtil_Class.GetLinkLocalSource(pDevice);
        IPAddress allNodes = IPAddress.Parse("ff02::1");

        void Handler(object sender, PacketCapture e)
        {
            try
            {
                Packet packet = e.GetPacket().GetPacket();
                var ethernet = packet.Extract<EthernetPacket>();
                var ipv6 = packet.Extract<IPv6Packet>();
                if (ethernet == null || ipv6 == null)
                {
                    return;
                }

                PhysicalAddress mac = ethernet.SourceHardwareAddress;
                IPAddress ip = ipv6.SourceAddress;

                // Ignore our own traffic and non-host source addresses.
                if (mac.Equals(ourMac) || ip.Equals(IPAddress.IPv6Any) || ip.IsIPv6Multicast)
                {
                    return;
                }

                string key = ip.ToString();
                lock (found)
                {
                    if (!found.ContainsKey(key))
                    {
                        found[key] = new DiscoveredHost(ip, mac);
                    }
                }
            }
            catch
            {
                // A malformed capture must not break the scan.
            }
        }

        RunCapture(pDevice, "icmp6", Handler, () =>
        {
            SendEchoRequest(pDevice, ourMac, source, allNodes);
            Thread.Sleep(TimeSpan.FromSeconds(Math.Max(1, pScanSeconds)));
        });

        var result = found.Values.OrderBy(h => h.IpAddress.ToString(), StringComparer.Ordinal).ToList();
        foreach (var host in result)
        {
            host.Hostname = TryReverseDns(host.IpAddress);
        }
        return result;
    }

    /// <summary>
    /// Resolves the MAC address for an IPv6 address via a Neighbor
    /// Solicitation / Advertisement exchange. Returns null on timeout.
    /// </summary>
    public PhysicalAddress? ResolveMac(LibPcapLiveDevice pDevice, IPAddress pTarget, int pTimeoutMs = 2000)
    {
        PhysicalAddress ourMac = pDevice.MacAddress;
        IPAddress source = PacketUtil_Class.GetLinkLocalSource(pDevice);
        PhysicalAddress? resolved = null;
        using var signal = new ManualResetEventSlim(false);

        void Handler(object sender, PacketCapture e)
        {
            try
            {
                Packet packet = e.GetPacket().GetPacket();
                var ethernet = packet.Extract<EthernetPacket>();
                var ipv6 = packet.Extract<IPv6Packet>();
                var advertisement = packet.Extract<NdpNeighborAdvertisementPacket>();
                if (ethernet == null || ipv6 == null || advertisement == null)
                {
                    return;
                }

                // Match either the advertised target or the packet source, since
                // some stacks advertise from the link-local and some from the
                // queried address.
                if (advertisement.TargetAddress.Equals(pTarget) || ipv6.SourceAddress.Equals(pTarget))
                {
                    resolved = ethernet.SourceHardwareAddress;
                    signal.Set();
                }
            }
            catch
            {
                // Ignore malformed packets.
            }
        }

        RunCapture(pDevice, "icmp6", Handler, () =>
        {
            SendNeighborSolicitation(pDevice, ourMac, source, pTarget);
            signal.Wait(pTimeoutMs);
        });

        return resolved;
    }

    /// <summary>
    /// Runs <paramref name="pDriver"/> while a capture (filtered by
    /// <paramref name="pFilter"/>) delivers packets to <paramref name="pHandler"/>,
    /// then tears the capture down cleanly.
    /// </summary>
    private static void RunCapture(LibPcapLiveDevice pDevice, string pFilter, PacketArrivalEventHandler pHandler, Action pDriver)
    {
        if (!pDevice.Opened)
        {
            pDevice.Open();
        }

        try
        {
            pDevice.Filter = pFilter;
        }
        catch (PcapException)
        {
            // Older libpcap builds may reject an empty/unknown filter - ignore.
        }

        pDevice.OnPacketArrival += pHandler;
        pDevice.StartCapture();
        try
        {
            pDriver();
        }
        finally
        {
            try { pDevice.StopCapture(); } catch (PcapException) { }
            pDevice.OnPacketArrival -= pHandler;
        }
    }

    private void SendEchoRequest(LibPcapLiveDevice pDevice, PhysicalAddress pSrcMac, IPAddress pSrcIp, IPAddress pDstIp)
    {
        byte[] icmp = new byte[8];
        icmp[0] = EchoRequestType;
        icmp[1] = 0;
        // [2..4] checksum, [4..6] identifier, [6..8] sequence
        icmp[4] = 0x4D; icmp[5] = 0x4B; // identifier "MK"
        icmp[6] = 0x00; icmp[7] = 0x01; // sequence
        ushort checksum = PacketUtil_Class.ComputeIcmpV6Checksum(pSrcIp, pDstIp, icmp);
        icmp[2] = (byte)(checksum >> 8);
        icmp[3] = (byte)(checksum & 0xFF);

        byte[] frame = PacketUtil_Class.BuildIcmpV6Frame(pSrcMac, PacketUtil_Class.MulticastMac(pDstIp), pSrcIp, pDstIp, icmp);
        pDevice.SendPacket(frame);
    }

    private void SendNeighborSolicitation(LibPcapLiveDevice pDevice, PhysicalAddress pSrcMac, IPAddress pSrcIp, IPAddress pTarget)
    {
        IPAddress solicitedNode = PacketUtil_Class.SolicitedNodeMulticast(pTarget);

        byte[] icmp = new byte[32];
        icmp[0] = NeighborSolicitationType;
        icmp[1] = 0;
        // [4..8) reserved
        Buffer.BlockCopy(pTarget.GetAddressBytes(), 0, icmp, 8, 16);
        icmp[24] = SourceLinkLayerAddressOption;
        icmp[25] = OptionLengthInEightByteUnits;
        Buffer.BlockCopy(pSrcMac.GetAddressBytes(), 0, icmp, 26, 6);
        ushort checksum = PacketUtil_Class.ComputeIcmpV6Checksum(pSrcIp, solicitedNode, icmp);
        icmp[2] = (byte)(checksum >> 8);
        icmp[3] = (byte)(checksum & 0xFF);

        byte[] frame = PacketUtil_Class.BuildIcmpV6Frame(pSrcMac, PacketUtil_Class.MulticastMac(solicitedNode), pSrcIp, solicitedNode, icmp);
        pDevice.SendPacket(frame);
    }

    private static string TryReverseDns(IPAddress pAddress)
    {
        try
        {
            // GetHostEntry has no timeout, so cap it to keep the scan snappy.
            var task = Task.Run(() => Dns.GetHostEntry(pAddress).HostName);
            return task.Wait(TimeSpan.FromMilliseconds(600)) ? task.Result : "";
        }
        catch
        {
            return "";
        }
    }
}
