﻿using SharpPcap;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class TargetList
{
    private List<Target> targetList = new List<Target>();
    private Arp arp;
    private Ndp ndp;

    public int getLength()
    {
        return targetList.Count;
    }

    public List<Target> getTargetList()
    {
        return targetList;
    }

    public void addNewTarget(ILiveDevice pLiveDevice)
    {
        // ARP & NDP
        arp = new Arp(pLiveDevice);
        ndp = new Ndp(pLiveDevice);

        // Target
        Target target = new Target();
        Console.Write("Target IP-Address \t: ");
        IPAddress tempAddr = IPAddress.Parse(Console.ReadLine());

        if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetwork))
        {
            target.t_ipAddr = tempAddr;
            target.t_phAddr = arp.getPhysicalAddress(target.t_ipAddr);

            Console.Write("Gateway IPv4-Address\t: ");
            target.s_ipAddr = IPAddress.Parse(Console.ReadLine());
            target.s_phAddr = arp.getPhysicalAddress(target.s_ipAddr);
        }
        else if (tempAddr.AddressFamily.Equals(AddressFamily.InterNetworkV6))
        {
            target.t_ipAddr = tempAddr;
            target.t_phAddr = ndp.getPhysicalAddress(IPAddress.Parse("fe80::586f:4076:95b8:555a"), target.t_ipAddr);

            Console.Write("Gateway IPv6-Address\t: ");
            target.s_ipAddr = IPAddress.Parse(Console.ReadLine());
            target.s_phAddr = ndp.getPhysicalAddress(IPAddress.Parse("fe80::586f:4076:95b8:555a"), target.s_ipAddr);
        }

        targetList.Add(target);
    }

    public void printTargetList()
    {
        for (int i = 0; i < targetList.Count; i++)
        {
            Console.WriteLine("{0}. IP-Address:{1}\tPhysical-Address:{2}\n=> IP-Address:{3}\tPhysical-Address:{4}",
                i,
                targetList[i].t_ipAddr,
                targetList[i].t_phAddr,
                targetList[i].s_ipAddr,
                targetList[i].s_phAddr);
        }

        if (targetList.Count > 0)
        {
            Console.Write("Remove [Nr]: ");
            string removeNr = Console.ReadLine();

            try
            {
                targetList.RemoveAt(Convert.ToInt32(removeNr));
            }
            catch (Exception) { }
        }
    }

    private PhysicalAddress parsePhysicalAddress(string pPhysicalAddress)
    {
        string phyAddrNew;
        if (pPhysicalAddress.Contains(":"))
        {
            phyAddrNew = pPhysicalAddress.Replace(":", "-");
        }
        else
        {
            phyAddrNew = pPhysicalAddress;
        }
        return PhysicalAddress.Parse(phyAddrNew);
    }
}