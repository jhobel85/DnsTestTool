# SimpleDnsTestTool Overview

SimpleDnsTestTool is a dual-stack DNS server and client toolkit designed for testing and development. It supports both IPv4 and IPv6 traffic, allowing reliable DNS record registration, resolution, and unregistration. The project emphasizes test stability and reliability through async/await patterns, ensuring operations complete in order and tests run consistently across network stacks.

# SimpleDnsServer

Definition: Dual-stack means the server can handle both IPv4 and IPv6 traffic, ideally on the same port.

Reliable Registration: By awaiting async registration methods, you ensure that a DNS record is fully registered before sending a DNS query. This prevents timing issues where a query might be sent before the server is ready, which is especially important when running tests for both IPv4 and IPv6.

Test Stability: Async/await ensures that each step (register, resolve, unregister) completes in order, making your dual-stack tests pass consistently.

# How to start the server
1] Default run: SimpleDnsServer.exe
- IPv4 will be localhost 172.0.0.1, UDP port 53 and API port 60
- IPv6 will be localhost [::1], UDP port 53 and API port 60
2] Custom run: SimpleDnsServer.exe --ip 192.168.50.1 --ip6 fd00:50::1 --apiPort 10053 --udpPort 10060
- custom IPv4 and IPv6
- custom ports
- If any of parameters not be specified default values be used.


# Rest API tests on localhost
## IPv4 Register and resovle:
curl -X POST "http://127.0.0.1:60/dns/register?domain=ip4.com&ip=192.168.10.20"

curl -X GET "http://127.0.0.1:60/dns/resolve?domain=ip4.com"

## IPv6 Register and resolve:
curl -g -X POST "http://[::1]:60/dns/register?domain=ip6.com&ip=fd00::101"

curl -g -X GET "http://[::1]:60/dns/resolve?domain=ip6.com"

## Show All entries
curl -g -X GET "http://[::1]:60/dns/entries"

## PowerShell syntax:
Invoke-WebRequest -Method POST "http://[::1]:60/dns/register?domain=ip6.com&ip=fd00::101"

Invoke-WebRequest -Method GET "http://[::1]:60/dns/resolve?domain=ip6.com"

## See DNS packets in Wireshark
The server resolves the name internally, without using DNS protocol. No UDP packets are created.

Therfore Resolve via nslookup (work only with port 53):

nslookup ip4.com 127.0.0.1

nslookup -q=AAAA ip6.com ::1

(nslookup ip6.com ::1 // nslookup tries both IPv4 and IPv6 when you specify ::1)

# Architecture & Module Responsibilities

## Key Modules

- **IDnsRecordManger / DnsRecordManger**: Core DNS record storage and resolution logic. Registered as a singleton for both API and UDP listener.
- **IDnsQueryHandler / DefaultDnsQueryHandler**: Handles DNS protocol queries, decoupled from UDP listener for testability and extension.
- **IProcessManager / DefaultProcessManager**: Abstracts process management (find/kill/check server processes), replacing static ProcessUtils.
- **IServerManager / DefaultServerManager**: Abstracts server process startup, replacing static ServerUtils.
- **RestClient**: Client for API, now using IHttpClient abstraction for testability.
- **Startup.cs**: Registers all abstractions for dependency injection.

## Design Principles

- All infrastructure utilities are now injectable via interfaces, supporting testability and extension.
- Static helpers are marked obsolete and replaced by DI-registered services.
- UDP listener delegates DNS query handling to an injected handler, not direct record manager access.

## Extending/Testing

- To mock process/server/network logic, implement the relevant interface and register your mock in DI.
- For custom DNS query handling, implement IDnsQueryHandler and register it in Startup.cs.

---







