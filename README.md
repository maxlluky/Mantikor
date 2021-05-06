# MANTIKOR
``` 
Disclaimer: This project is for demonstration purposes and should not be used for illegal acts! 
Please do not use in military or secret service organizations,
or for illegal purposes.
(This is the wish of the author and non-binding. Many people working
in these organizations do not care for laws and ethics anyways.
You are not one of the "good" ones if you ignore this.)
```

## Whats MANTIKOR?
Mantikor is an Open-Source address-resolution spoofing-Tool. The application is written in C# with the use of SharpPcap and PacketDotNet. I created the software to learn more about ARP and NDP Spoofing which results in a better understanding how to prevent and detect it in local networks. 

## How to use MANTIKOR
Mantikor is designed as a console program and can therefore also be executed directly via cmd or powershell. To select an item in the menu, the listed numbers are used. Important is that when defining an IPv4 target, the gateway should also be configured with an IPv4 address. The same is important for IPv6. Mantikor allows to spoof either IPv4/IPv6 or both protocols i.e. ARP and NDP at the same time.

## -> Settings 
When using it, the first thing to do is to configure the network adapter that will be used for the network attack. Option 1 is taken for this purpose.
After this, the gateway must be configured and the own IPv4/IPv6 address. 
Once the basic configuration is complete, any number of targets can be defined. You can specify either the IPv4 or IPv6 address of the target. If an IPv4 address is specified, the MAC address is resolved automatically. 
To start the attack, the network adapter, gateway, source and a target must be configured. For each endpoint a thread will be created. The attack can be launched using option 5.

Option 6 is used to end the attack, whereby the closing of the threads takes some time.

## Copyright
The contents and works in this software created by the software operators are subject to German copyright law. The reproduction, editing, distribution and any kind of use outside the limits of copyright law require the written consent of the respective author or creator. Downloads and copies of this software are only permitted for private, non-commercial use.

Insofar as the content on this software was not created by the operator, the copyrights of third parties are observed. In particular, third-party content is identified as such. Should you nevertheless become aware of a copyright infringement, please inform us accordingly. If we become aware of any infringements, we will remove such contents immediately.

Source: [eRecht24.de](https://www.e-recht24.de/)
