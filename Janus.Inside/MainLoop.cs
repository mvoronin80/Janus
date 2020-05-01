using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using mikev.Janus.Common;

namespace mikev.Janus.Inside
{
    public static class MainLoop
    {
        public static async Task Process(IConfiguration configuration, ILogger logger)
        {
            HttpClient httpClient = new HttpClient();

            // inifinite loop to pick up requests from Janus.Outside
            do 
            {
                // wait between requests to Janus.Outside to not overload it, check every "MainLoopDelaySeconds" seconds
                await Task.Delay( TimeSpan.FromSeconds( int.Parse(configuration["MainLoopDelaySeconds"]) ) );

                HttpResponseMessage httpResponse = null;
                try
                {
                    String urlJanusOutsideRequests = configuration["JanusExternalBaseUrl"] + "/request";
                    logger.LogInformation("Get requests started");
                    HttpRequestMessage httpRequest = new HttpRequestMessage();
                    httpRequest.Method = HttpMethod.Get;
                    httpRequest.RequestUri = new Uri(urlJanusOutsideRequests);
                    httpRequest.Headers.Add("X-Janus-Secret", configuration["X-Janus-Secret"]); 
                    httpResponse = await httpClient.SendAsync(httpRequest);
                    logger.LogInformation($"Get request completed, code={httpResponse.StatusCode.ToString()}" );

                    String body = await httpResponse.Content.ReadAsStringAsync();
                    logger.LogInformation("Get request body read");
                    if (httpResponse.StatusCode == HttpStatusCode.OK) {
                        List<RequestInfo> requests = JsonSerializer.Deserialize<List<RequestInfo>>(body);
                        logger.LogInformation($"Get request body read, request count={requests.Count}");
                        foreach(var request in requests) {
                            _ = Task.Run(
                                async () => await RequestProcessor.Process(httpClient, request, configuration, logger)
                            );
                        }    
                    } else {
                        logger.LogWarning($"Get request server error={body}");
                    }
                }
                catch(Exception exception) {
                    logger.LogError($"Exception in MainLoop, message={exception.Message}");
                }
                finally
                {
                    if (httpResponse != null)
                        httpResponse.Dispose();
                }
            } while(true);
        }
    }
}