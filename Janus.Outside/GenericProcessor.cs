using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using mikev.Janus.Common;

namespace mikev.Janus.Outside
{

    public static class GenericProcessor
    {
        public static async Task Process(HttpContext context, IConfiguration configuration, ILogger logger)
        {
            // create unique request id
            String requestId = Guid.NewGuid().ToString();

            // disposable variables
            StreamReader requestBodyReader = null;

            try
            {
                logger.LogInformation($"Request {requestId}: started");

                // check if request contains out secret
                if (!Common.IsValidJanusSecret(context.Request, configuration)) {
                    logger.LogWarning($"Request {requestId}: secret wrong");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Request {requestId} secret wrong");
                    return;
                }

                // read request body
                requestBodyReader = new StreamReader(context.Request.Body);
                String body = await requestBodyReader.ReadToEndAsync();
                logger.LogInformation($"Request {requestId}: body read ok"); 
                
                // populate request info from request data
                RequestInfo requestInfo = new RequestInfo();
                requestInfo.RequestId = requestId;
                requestInfo.Url = context.Request.Path;
                foreach(var header in context.Request.Headers) {
                    if (header.Key != "X-Janus-Secret")
                        requestInfo.Headers.Add(header.Key, header.Value.ToArray());
                }
                requestInfo.Method = context.Request.Method;
                requestInfo.Body = body;
                requestInfo.Query = context.Request.QueryString.HasValue? context.Request.QueryString.Value : String.Empty;
                logger.LogInformation(requestInfo.ToString());

                // prepare result task
                TaskCompletionSource<ResponseInfo> completionSource = new TaskCompletionSource<ResponseInfo>();
                Task<ResponseInfo> task = completionSource.Task; 
                Common.theTasks[requestId] = completionSource;
                logger.LogInformation($"Request {requestId}: task created");

                // send this request info to channel
                Common.channelWriter.TryWrite(requestInfo);

                // wait for response
                int secondsToWait = int.Parse(configuration["WaitForGenericResponseSeconds"]);
                if (task.Wait(TimeSpan.FromSeconds(secondsToWait)))
                {
                    logger.LogInformation($"Request {requestId}: task completed");
                    // populate http response back
                    ResponseInfo responseInfo = task.Result;
                    context.Response.StatusCode = responseInfo.StatusCode;
                    foreach(var header in responseInfo.Headers) {
                        context.Response.Headers.Add(header.Key, new Microsoft.Extensions.Primitives.StringValues(header.Value));
                    }
                    await context.Response.WriteAsync(responseInfo.Body);
                    logger.LogInformation($"Request {requestId}: completed successfully");

                } else {
                    logger.LogWarning($"Request {requestId}: task timeout");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Request {requestId} wasn't completed on time");
                }

            }
            catch(Exception exception) 
            {
                logger.LogError($"Request {requestId}: exception {exception.Message}");
                Common.CallWithNoExceptions(logger, () => 
                    {
                        context.Response.StatusCode = 500; 
                    }
                );
                await context.Response.WriteAsync($"Janus external exception {exception.Message}");
            }
            finally
            {
                if (requestBodyReader != null)
                    requestBodyReader.Close();
                Common.theTasks.Remove(requestId, out _);
            }
        }
    }

}