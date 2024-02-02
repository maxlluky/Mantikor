# Mantikor
``` 
Disclaimer: This project is only for demonstration purposes and should not be used for illegal acts! 
Please have a look at why I published this tool. Thanks
```
<img src="img/01.jpg">

## Whats Mantikor?
Mantikor is an Open-Source address-resolution spoofing-Tool. The application is written in C# with the use of [SharpPcap](https://github.com/chmorgan/sharppcap) and [PacketNet](https://github.com/chmorgan/packetnet).

## My intention
During my studies to become an IT specialist, I started to get very interested in programming network applications. Thereby, I noticed the huge number of possibilities and vulnerabilities in network protocols. After a short time, I got familiar with the ARP and NDP protocols. It didn’t take me much time to create the project called Mantikor. My intention was to learn more about ARP and NDP spoofing, combined with the idea of building some security mechanism to prevent it. For this reason I created this project to have a working vulnerability exploitation tool. For some years now, mechanisms such as "DAI" have been in place in Enterprise switches to prevent ARP spoofing. However, these features are not implemented in most networks.

## What's the easiest way to prevent spoofing in an Enterprise Network?
Company networks have an easy way to prevent ARP using the DAI (Dynamic Arp Inspection) feature. This can be configured on switches. Firewalls can also detect spoofing attacks.
[Cisco.com: Understanding and Configuring Dynamic ARP Inspectio](https://www.cisco.com/c/en/us/td/docs/switches/lan/catalyst4500/12-2/25ew/configuration/guide/conf/dynarp.html)

## What's the easiest way to prevent spoofing on a PC?
The easiest solution to prevent ARP and NDP spoofing is to manually (statically) enter the MAC address of the gateway into the arp and ndp-table.

1.  With Windows this can be done quite easily with the Powershell: (Example)
```PowerShell
Get-NetAdapter
```
Which returns:
```PowerShell
Name      InterfaceDescription                    ifIndex Status       MacAddress         LinkSpeed
----      --------------------                    ------- ------       ----------         ---------
Wi-Fi     Intel(R) Dual Band Wireless                  18 Disconnected 12-34-56-AB-CD-EF     6 Mbps
Ethernet  Intel(R) Ethernet Connection …                9 Up           78-90-12-GH-IJ-KL     1 Gbps
```
2.  To create a static ARP cache entry for that interface (that survive a reboot):
```PowerShell
New-NetNeighbor -InterfaceIndex 9 -IPAddress '192.168.178.1' -LinkLayerAddress '0000120000ff' -State Permanent
```
3.  You can remove the entry we just created by running this:
```PowerShell
Remove-NetNeighbor -InterfaceIndex 9 -IPAddress '192.168.178.1'
```

The same works for NDP. For this, the IPv6 address must be used instead of the IPv4 address of the gateway in step 2.

## How to use Mantikor
Mantikor is designed as a console program and can therefore also be executed directly via cmd or powershell. To select an item in the menu, the listed numbers are used. Mantikor allows to spoof either over IPv4/IPv6 or both protocols i.e. ARP and NDP at the same time.

## What you need
To compile Mantikor you need .NET 8.0, Visual Studio and Npcap installed on your computer.

## Changelog
- V1.0.8.0 - SharpPcap 6.2.5.0, .NET 8.0, Linux Support, changed to LibPcapDevice, new method to resolve via ARP, NDP-Classes merged, Code-Cleanup, fixed wrong User-Inputs.

## Copyright
The contents and works in this software created by the software operators are subject to German copyright law. The reproduction, editing, distribution and any kind of use outside the limits of copyright law require the written consent of the respective author or creator. Downloads and copies of this software are only permitted for private, non-commercial use.

Insofar as the content on this software was not created by the operator, the copyrights of third parties are observed. In particular, third-party content is identified as such. Should you nevertheless become aware of a copyright infringement, please inform us accordingly. If we become aware of any infringements, we will remove such contents immediately.

Source: [eRecht24.de](https://www.e-recht24.de/)
Cheers 👀
