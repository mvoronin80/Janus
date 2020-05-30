using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace mikev.Janus.Outside
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // create default host builder
            Host.CreateDefaultBuilder(args)
                // use configuration from Startup class
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                // build configuration
                .Build()
                // run
                .Run();
        }           
    }
}
