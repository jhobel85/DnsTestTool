using Microsoft.AspNetCore.Mvc;
using System.Linq;

#nullable enable
namespace DualstackDnsServer.RestApi;

[ApiController]
[Route("dns")]
public class DnsApiController(IDnsRecordManger recordManger) : ControllerBase
{
    private readonly IDnsRecordManger recordManger = recordManger;

    [HttpPost("register")]
    public IActionResult Register(string domain, string ip)
    {
        Console.WriteLine("Register domain: " + domain + " ip: " + ip);
        recordManger.Register(domain, ip);
        return (IActionResult)Ok();
    }

    [HttpPost("register/bulk")]
    public IActionResult RegisterBulk([FromBody] IEnumerable<DnsEntryDto> entries, string? sessionId = null)
    {
        if (entries == null)
            return BadRequest("Entries are required.");
        recordManger.RegisterMany(entries, sessionId);
        return Ok(new { Registered = entries.Count() });
    }

    [HttpPost("register/session")]
    public IActionResult RegisterSession(string domain, string ip, string sessionId)
    {
        Console.WriteLine("Register domain in session context: " + domain + " ip: " + ip);
        recordManger.Register(domain, ip, sessionId);
        return (IActionResult)Ok();
    }

    [HttpPost("unregister")]
    public IActionResult Unregister(string domain)
    {
        Console.WriteLine("Unregister domain:" + domain);
        recordManger.Unregister(domain);
        return (IActionResult)Ok();
    }

    [HttpPost("unregister/session")]
    public IActionResult UnregisterSession(string sessionId)
    {
        Console.WriteLine("Unregister session:" + sessionId);
        recordManger.UnregisterSession(sessionId);
        return (IActionResult)Ok();
    }

    [HttpDelete("unregister/all")]
    public IActionResult UnregisterAll()
    {
        recordManger.UnregisterAll();
        return (IActionResult)Ok();
    }

    [HttpGet("resolve")]
    public IActionResult Resolve(string domain)
    {
        Console.WriteLine("Resolve domain:" + domain);
        string? str = recordManger.Resolve(domain);
        Console.WriteLine("Ip is: " + str);
        return (IActionResult)Ok(str ?? "");
    }


    [HttpGet("entries")]
    public ActionResult<IEnumerable<DnsEntryDto>> GetAllEntries()
    {
        Console.WriteLine("Get all DNS entries");
        var entries = recordManger.GetAllEntries();
        return Ok(entries);
    }

    [HttpGet("count")]
    public IActionResult RecordsCount()
    {
        Console.WriteLine("Get records count");
        int count = recordManger.GetCount();
        Console.WriteLine("All records count is: " + count.ToString());
        return (IActionResult)Ok((object)count);
    }

    [HttpGet("count/session")]
    public IActionResult RecordsSessionCount(string sessionId)
    {
        Console.WriteLine("Get records count of session:" + sessionId);
        int sessionCount = recordManger.GetSessionCount(sessionId);
        Console.WriteLine("Records count of session is: " + sessionCount.ToString());
        return (IActionResult)Ok((object)sessionCount);
    }
}
