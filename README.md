# Mantikor
``` 
Disclaimer: This project is for demonstration purposes and should not be used for illegal acts! 
Please have a look at why I published this tool. Thanks
```
<img src="img/01.jpg">

## Whats Mantikor?
Mantikor is an Open-Source address-resolution spoofing-Tool. The application is written in C# with the use of SharpPcap and PacketDotNet.

## Why publishing another ARP and NDP spoofing tool?
During my studies to become an IT specialist, I started to get very interested in programming network applications. Thereby, I noticed the huge number of possibilities and vulnerabilities in network protocols. After a short time, I got familiar with the ARP and NDP protocols. It didn’t take me much time to great the project called Mantikor. My intention was to learn more about ARP and NDP spoofing, combined with the idea of building some security mechanism to prevent it. To gain more experience, I continued to improve my tool over the time. Unfortunately, nothing important has been done about the worst security holes in IP networks for so many years now.

## What is the easiest way to prevent spoofing?
The easiest solution for me to prevent ARP and NDP spoofing was to manually (statically) enter the MAC address of my gateway into the arp and ndp-table.

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
To compile Mantikor you need .NET 4.8, Visual Studio and Npcap installed on your computer. Mantikor was not developed with .NET Core!

## Changelog
- V1.0.7 - Upgrade to SharpPcap V.6.1, also resolves MAC over IPv6.

## Copyright
The contents and works in this software created by the software operators are subject to German copyright law. The reproduction, editing, distribution and any kind of use outside the limits of copyright law require the written consent of the respective author or creator. Downloads and copies of this software are only permitted for private, non-commercial use.

Insofar as the content on this software was not created by the operator, the copyrights of third parties are observed. In particular, third-party content is identified as such. Should you nevertheless become aware of a copyright infringement, please inform us accordingly. If we become aware of any infringements, we will remove such contents immediately.

Source: [eRecht24.de](https://www.e-recht24.de/)
Cheers 👀
