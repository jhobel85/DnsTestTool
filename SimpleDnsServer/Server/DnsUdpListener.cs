using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace SimpleDnsTestTool.Server
{
    public class DnsUdpListener : BackgroundService
    {
        private DnsServer udpServer;
        private DnsRecordManger recordManager;

        public DnsUdpListener(DnsRecordManger recordManager, IConfiguration config)
        {            
            string ipString = Constants.ResolveDnsIp(config);
            int port = int.Parse(Constants.ResolveUdpPort(config));
            IPAddress ipAddr = IPAddress.Parse(ipString);
            IPEndPoint bindEndPoint = new IPEndPoint(ipAddr, port);
            this.recordManager = recordManager;
            var transport = new UdpServerTransport(bindEndPoint);
            this.udpServer = new DnsServer(transport);
            this.udpServer.QueryReceived += new AsyncEventHandler<QueryReceivedEventArgs>(this.OnQueryReceived);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
                this.udpServer.Start();
            else
                this.udpServer.Stop();            
        }

        private async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
        {
            if (!(e.Query is DnsMessage query))
                return;
            this.logDnsMessageQuestions(query);
            DnsMessage responseInstance = query.CreateResponseInstance();
            if (query.Questions.Count == 1 && query.Questions[0].RecordType == RecordType.A)
            {
                string str = query.Questions[0].Name.ToString();
                string ipString = this.recordManager.Resolve(str);
                if (ipString != null)
                {
                    responseInstance.ReturnCode = ReturnCode.NoError;
                    responseInstance.AnswerRecords.Add((DnsRecordBase)new ARecord(DomainName.Parse(str), 3600, IPAddress.Parse(ipString)));
                }
                else
                    responseInstance.ReturnCode = ReturnCode.NxDomain;
            }
            else
                responseInstance.ReturnCode = ReturnCode.ServerFailure;
            this.logDnsMessageAnswers(responseInstance);
            e.Response = (DnsMessageBase)responseInstance;
        }

        public virtual void Dispose()
        {
            base.Dispose();
            this.udpServer?.Stop();
        }

        private void LogMessageHeader(DnsMessage message)
        {
            Console.WriteLine(string.Format("ID: {0}", (object)message.TransactionID));
            Console.WriteLine(string.Format("Operation Code: {0}", (object)message.OperationCode));
            Console.WriteLine(string.Format("Is Query: {0}", (object)message.IsQuery));
            Console.WriteLine(string.Format("Is Recursion Desired: {0}", (object)message.IsRecursionDesired));
            Console.WriteLine(string.Format("Is Checking Disabled: {0}", (object)message.IsCheckingDisabled));
            Console.WriteLine(string.Format("Is Authentic Data: {0}", (object)message.IsAuthenticData));
        }

        public void logDnsMessageQuestions(DnsMessage message)
        {
            Console.WriteLine("Questions:");
            this.LogMessageHeader(message);
            foreach (DnsQuestion question in message.Questions)
            {
                Console.WriteLine(string.Format("Questions Name: {0}", (object)question.Name));
                Console.WriteLine(string.Format("Questions Record Type: {0}", (object)question.RecordType));
                Console.WriteLine(string.Format("Questions Record Class: {0}", (object)question.RecordClass));
            }
        }

        public void logDnsMessageAnswers(DnsMessage message)
        {
            Console.WriteLine("Answers:");
            this.LogMessageHeader(message);
            foreach (DnsRecordBase answerRecord in message.AnswerRecords)
            {
                Console.WriteLine(string.Format("Answer Name: {0}", (object)answerRecord.Name));
                Console.WriteLine(string.Format("Answer Record Type: {0}", (object)answerRecord.RecordType));
                if (answerRecord.RecordType == RecordType.A)
                    Console.WriteLine(string.Format("Answer Ip adrress: {0}", (object)((AddressRecordBase)answerRecord).Address));
                Console.WriteLine(string.Format("Answer Time to Live: {0}", (object)answerRecord.TimeToLive));
            }
        }
    }
}
