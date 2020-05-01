using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace mikev.Janus.Inside
{
    class Program
    {
        static void Main(string[] args)
        {
            // create configuration
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = configBuilder.Build();

            // create logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Janus.Inside started, please <Enter> to stop");

            // create main loop task
            var cancellationTokenSource = new CancellationTokenSource();
            var taskMain = Task.Run(
                async () => await MainLoop.Process(configuration, logger),
                cancellationTokenSource.Token
            );

            // wait enter on keyboard to stop
            while(Console.ReadKey().Key != ConsoleKey.Enter) ;

            // wait for task
            cancellationTokenSource.Cancel();
            taskMain.Wait(TimeSpan.FromSeconds(int.Parse(configuration["MainTaskWaitToCompleteSeconds"])));
            logger.LogInformation("Janus.Inside stopped");
        }
    }
}
