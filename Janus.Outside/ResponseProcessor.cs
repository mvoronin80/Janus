using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using mikev.Janus.Common;

namespace mikev.Janus.Outside
{

    public static class ResponseProcessor
    {
        public static async Task Process(HttpContext context, IConfiguration configuration, ILogger logger)
        {
            StreamReader requestBodyReader = null;

            try
            {
                logger.LogInformation("Requests post: started");

                // check whether request has valid secret
                if (!Common.IsValidJanusSecret(context.Request, configuration)) {
                    logger.LogWarning($"Request get: secret wrong");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Request secret wrong");
                    return;
                }

                // read response - always one
                requestBodyReader = new StreamReader(context.Request.Body);;
                String body = await requestBodyReader.ReadToEndAsync();
                logger.LogInformation("Requests post: body read completed");

                // deserialize
                ResponseInfo response = JsonSerializer.Deserialize<ResponseInfo>(body);
                logger.LogInformation($"Requests post: body deserialized, request id = {response.RequestId}");

                // find corresponding request
                if (Common.theTasks.ContainsKey(response.RequestId)) {
                    TaskCompletionSource<ResponseInfo> completionSource = Common.theTasks[response.RequestId];
                    completionSource.SetResult(response);
                    logger.LogInformation($"Requests post: request id = {response.RequestId} notified completed");
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("Completed successfully");
                } else {
                    logger.LogWarning($"Requests post: request id = {response.RequestId} task not found");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Response processor error: request id {response.RequestId} not found");
                }

            }
            catch(Exception exception)
            {
                logger.LogError($"Response post: exception, message={exception.Message}");
                // return error
                Common.CallWithNoExceptions(logger, () => 
                    {
                        context.Response.StatusCode = 500; 
                    }
                );
                await context.Response.WriteAsync($"Exception in response processor, message={exception.Message}");
            }  
            finally
            {
                if (requestBodyReader != null)
                    requestBodyReader.Close();
            }
        }
    }

}