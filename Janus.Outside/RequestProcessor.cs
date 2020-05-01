using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mikev.Janus.Common;

namespace mikev.Janus.Outside
{

    public static class RequestProcessor
    {
        public static async Task Process(HttpContext context, IConfiguration configuration, ILogger logger)
        {   
            try 
            {
                logger.LogInformation("Requests get: started");
                if (!Common.IsValidJanusSecret(context.Request, configuration)) {
                    logger.LogWarning($"Request get: secret wrong");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Request secret wrong");
                    return;
                }

                // timeout
                int secondsToWait = int.Parse(configuration["WaitForRequestPickUpSeconds"]);

                // set of requests
                List<RequestInfo> result = new List<RequestInfo>();

                // create tasks to wait
                logger.LogInformation("Requests get: before wait");
                bool timeout = false;
                CancellationTokenSource ctsForDelayTask = new CancellationTokenSource();
                CancellationTokenSource ctsForChannelWaitTask = new CancellationTokenSource();

                logger.LogInformation($"Requests get: wait started");
                Task taskWait = Task.Run(async () => {
                    await Task.Delay( TimeSpan.FromSeconds(secondsToWait) );
                    logger.LogInformation($"Requests get: channel wait cancelled");
                    timeout = true;
                    ctsForChannelWaitTask.Cancel();
                }, ctsForDelayTask.Token);
                Task taskRead = Common.channelReader.WaitToReadAsync(ctsForChannelWaitTask.Token).AsTask();
                await Task.WhenAny(taskWait, taskRead);
                ctsForDelayTask.Cancel();

                logger.LogInformation($"Requests get: wait completed, timeout={timeout}");
                if (!timeout) {
                    while(Common.channelReader.TryRead(out RequestInfo request)) {
                        logger.LogInformation($"Requests get: {request.ToString()}");
                        result.Add(request);
                    }
                }
                logger.LogInformation($"Requests get: request count={result.Count}");
                
                // set requests back
                String body = JsonSerializer.Serialize(result.ToArray());
                logger.LogInformation("Requests get: serialized");
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync( body );
                logger.LogInformation("Requests get: completed successfully");  
            } 
            catch(Exception exception)
            {
                logger.LogInformation($"Requests get: exception, message={exception.Message}");
                // return error
                Common.CallWithNoExceptions(logger, () => 
                    {
                        context.Response.StatusCode = 500; 
                    }
                );
                await context.Response.WriteAsync($"Exception in request processor, message={exception.Message}");
            }
        }
    }

}