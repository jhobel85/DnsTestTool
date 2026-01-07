using Microsoft.AspNetCore.Mvc;
using System;

#nullable enable
namespace SimpleDnsTestTool.Server.RestApi
{
    [ApiController]
    [Route("dns")]
    public class DnsApiController : ControllerBase
    {
        private DnsRecordManger recordManger;

        public DnsApiController(DnsRecordManger recordManger) => this.recordManger = recordManger;

        [HttpPost("register")]
        public IActionResult Register(string domain, string ip)
        {
            Console.WriteLine("I will try register domain: " + domain + " ip: " + ip);
            this.recordManger.Register(domain, ip);
            return (IActionResult)this.Ok();
        }

        [HttpPost("register/session")]
        public IActionResult RegisterSession(string domain, string ip, string sessionId)
        {
            Console.WriteLine("I will try register domain in session context: " + domain + " ip: " + ip);
            this.recordManger.Register(domain, ip, sessionId);
            return (IActionResult)this.Ok();
        }

        [HttpPost("unregister")]
        public IActionResult Unregister(string domain)
        {
            Console.WriteLine("I will try unregister domain:" + domain);
            this.recordManger.Unregister(domain);
            return (IActionResult)this.Ok();
        }

        [HttpPost("unregister/session")]
        public IActionResult UnregisterSession(string sessionId)
        {
            Console.WriteLine("I will try unregister session:" + sessionId);
            this.recordManger.UnregisterSession(sessionId);
            return (IActionResult)this.Ok();
        }

        [HttpDelete("unregister/all")]
        public IActionResult UnregisterAll()
        {
            this.recordManger.UnregisterAll();
            return (IActionResult)this.Ok();
        }

        [HttpGet("resolve")]
        public IActionResult Resolve(string domain)
        {
            Console.WriteLine("I will try resolve domain:" + domain);
            string str = this.recordManger.Resolve(domain);
            Console.WriteLine("Ip is: " + str);
            return (IActionResult)this.Ok((object)str);
        }

        [HttpGet("count")]
        public IActionResult RecordsCount()
        {
            Console.WriteLine("I will try get records count");
            int count = this.recordManger.GetCount();
            Console.WriteLine("All records count is: " + count.ToString());
            return (IActionResult)this.Ok((object)count);
        }

        [HttpGet("count/session")]
        public IActionResult RecordsSessionCount(string sessionId)
        {
            Console.WriteLine("I will try get records count of session:" + sessionId);
            int sessionCount = this.recordManger.GetSessionCount(sessionId);
            Console.WriteLine("Records count of session is: " + sessionCount.ToString());
            return (IActionResult)this.Ok((object)sessionCount);
        }
    }
}
