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





