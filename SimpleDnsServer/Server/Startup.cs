using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

#nullable enable
namespace SimpleDnsTestTool.Server
{
    public class Startup
    {
        private IConfiguration configuration;

        public Startup(IConfiguration configuration) => this.configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            ServiceCollectionServiceExtensions.AddSingleton<DnsRecordManger>(services);
            MvcServiceCollectionExtensions.AddControllers(services);
            ServiceCollectionHostedServiceExtensions.AddHostedService<DnsUdpListener>(services);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (HostEnvironmentEnvExtensions.IsDevelopment((IHostEnvironment)env))
                DeveloperExceptionPageExtensions.UseDeveloperExceptionPage(app);
            EndpointRoutingApplicationBuilderExtensions.UseRouting(app);
            EndpointRoutingApplicationBuilderExtensions.UseEndpoints(app, (Action<IEndpointRouteBuilder>)(endpoints => ControllerEndpointRouteBuilderExtensions.MapControllers(endpoints)));
        }
    }
}
