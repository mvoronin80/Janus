using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace mikev.Janus.Outside
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private IConfiguration configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<Startup>();

            app.UseRouting();

            app.Map("/request",  Common.ProcessorAdapterActionAppBuilder(this.configuration, logger, RequestProcessor.Process) );
            app.Map("/response", Common.ProcessorAdapterActionAppBuilder(this.configuration, logger, ResponseProcessor.Process) );

            app.Run( Common.ProcessorAdapterToRequestDelegate(this.configuration, logger, GenericProcessor.Process) );
        }
    }
}
