# SimpleDnsServer

## Start dns server

string ipDns = NetworkUtils.GetLocalIpv4AsString(Proxy.ActivatedInterfaceIP()); //IP of test machine in to current subnet by the current interface

int apiPort = 60; //port rest api for manage dns server e.g register domain

int updPort = 53; //port upd listener, Plc will send request to this port for resolved qdn

string exePath = SimpletDnsClient.RestClient.GetServerExecPath(Attributes); //path to exe file of dns server

SimpletDnsClient.RestClient.StartServer(exePath, apiPort, updPort); // start dns server if not running

dnsClient = new SimpletDnsClient.RestClient(ipDns, apiPort); // create client for dns server
         

## Register domain

string domaim = Proxy.ActivatedInterfaceNOS();

string ipRecord = Proxy.ActivatedInterfaceIP();

dnsClient.Register(domaim, ipRecord);

log.Warn($"Dns client has sessionId: {dnsClient.SessionId}") // you can optain sessionId for investigation log from  DnsServer   


## Unregister domain 

All domain can be unregistered by this client;

You can also unregister single domain by dnsClient.Unregister(domain) dnsClient.UregisterSession();


## Dual stack IPv6 and IPv4
Definition: Dual-stack means the server can handle both IPv4 and IPv6 traffic, ideally on the same port.
Current Limitations: 

# Current Limitations
Current implementation of DNS server supports both IPv4 and IPv6, but not true dual-stack on the same port simultaneously. Each protocol is handled separately, and concurrent requests for each protocol are supported. Dual-stack limitations are due to library and OS constraints.

The main problem arises when trying to bind both IPv4 and IPv6 servers to the same port at the same time, due to OS and library limitations. IPv4-only requests do not conflict with each other; the conflict is specifically between IPv4 and IPv6 bindings on the same port.

The DNS server process binds to a single port for each protocol (IPv4 or IPv6).
Most DNS server libraries (including ARSoft.Tools.Net) do not support true dual-stack (IPv4/IPv6) binding on the same port in a single process.
OS-level restrictions prevent two processes from binding to the same port on different address families unless dual-stack is explicitly supported.









