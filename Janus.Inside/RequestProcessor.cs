using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using mikev.Janus.Common;
using System.Text;
using System.Text.Json;

namespace mikev.Janus.Inside
{

    public static class RequestProcessor
    {
        
        public static async Task Process(HttpClient http, RequestInfo requestInfo, IConfiguration configuration, ILogger logger)
        {
            HttpRequestMessage httpRequestToDocuSign = null, httpRequestToJanusOutside = null;
            HttpResponseMessage httpResponseFromDocuSign = null, httpResponseFromJanusOutside = null;

            try
            {
                // log incoming request
                logger.LogInformation($"Request {requestInfo.RequestId} started, info={requestInfo.ToString()}");

                // prepare request to DocuSign
                StringBuilder requestUrl = new StringBuilder();
                requestUrl.Append(configuration["DocuSignBaseUrl"]);
                requestUrl.Append(requestInfo.Url);
                if (!String.IsNullOrWhiteSpace(requestInfo.Query)) {
                    requestUrl.Append(requestInfo.Query);
                }
                httpRequestToDocuSign = new HttpRequestMessage();
                httpRequestToDocuSign.RequestUri = new Uri(requestUrl.ToString());
                logger.LogInformation($"Request {requestInfo.RequestId} uri={requestUrl}");
                httpRequestToDocuSign.Method = StringToHttpMethod(requestInfo.Method);
                foreach(var header in requestInfo.Headers) {
                    httpRequestToDocuSign.Headers.Add(header.Key, header.Value);
                }
                if (!String.IsNullOrWhiteSpace(requestInfo.Body)) {
                    httpRequestToDocuSign.Content = new StringContent(requestInfo.Body);
                }

                // send request to DocuSign
                httpResponseFromDocuSign = await http.SendAsync(httpRequestToDocuSign);
                logger.LogInformation($"Request {requestInfo.RequestId} to DocuSign completed, statusCode={httpResponseFromDocuSign.StatusCode}");

                // collecting response for Janus.Outside
                ResponseInfo responseInfo = new ResponseInfo();
                String body = await httpResponseFromDocuSign.Content.ReadAsStringAsync();
                logger.LogInformation($"Request {requestInfo.RequestId} to DocuSign body read");
                if (configuration["ConvertBodyToRemoteAddress"] == true.ToString()) {
                    responseInfo.Body = ConvertDocusingAddressToJanusAddess(requestInfo.RequestId, body, httpResponseFromDocuSign, configuration, logger);
                } else {
                    responseInfo.Body = body;
                }
                responseInfo.StatusCode = (Int32)httpResponseFromDocuSign.StatusCode;
                responseInfo.RequestId = requestInfo.RequestId;
                foreach(var header in httpResponseFromDocuSign.Headers) {
                    responseInfo.Headers.Add(header.Key, Enumerable.ToArray(header.Value) );
                }

                // send response back to Janus.Outside
                httpRequestToJanusOutside = new HttpRequestMessage();
                httpRequestToJanusOutside.RequestUri = new Uri(configuration["JanusExternalBaseUrl"] + "/response");
                httpRequestToJanusOutside.Method = HttpMethod.Post;
                httpRequestToJanusOutside.Content = new StringContent(JsonSerializer.Serialize<ResponseInfo>(responseInfo));
                httpRequestToJanusOutside.Headers.Add("X-Janus-Secret", configuration["X-Janus-Secret"]);
                httpResponseFromJanusOutside = await http.SendAsync(httpRequestToJanusOutside);
                logger.LogInformation($"Request {requestInfo.RequestId} to Janus.Outside completed, statusCode={httpResponseFromJanusOutside.StatusCode}"); 
            }
            catch(Exception exception)
            {
                logger.LogError($"Exception in request processor, message={exception.Message}" );
            } 
            finally
            {
                if (httpRequestToDocuSign != null)
                    httpRequestToDocuSign.Dispose();
                if (httpRequestToJanusOutside != null)
                    httpRequestToJanusOutside.Dispose();
                if (httpResponseFromDocuSign != null)
                    httpResponseFromDocuSign.Dispose();
                if (httpResponseFromJanusOutside != null)
                    httpResponseFromJanusOutside.Dispose();
            }
        }

        private static String ConvertDocusingAddressToJanusAddess(String requestId, String body, HttpResponseMessage httpResponse, IConfiguration configuration, ILogger logger)
        {
            if (!httpResponse.Headers.Contains("Content-Type"))
                return body;
            String contentType = httpResponse.Headers.GetValues("Content-Type").First();
            if ( contentType.Contains("json") ) {
                logger.LogInformation($"Request {requestId} JSON body replaced");
                return body.Replace(configuration["DocuSignBaseUrl"], configuration["JanusExternalBaseUrl"]);
            }
            return body;
        } 

        public static HttpMethod StringToHttpMethod(String method) {
            if (HttpMethod.Delete.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Delete;
            }
            if (HttpMethod.Get.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Get;
            }
            if (HttpMethod.Head.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Head;
            }
            if (HttpMethod.Options.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Options;
            }
            if (HttpMethod.Patch.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Patch;
            }
            if (HttpMethod.Post.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Post;
            }
            if (HttpMethod.Put.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Put;
            }
            if (HttpMethod.Trace.Method.Equals(method, StringComparison.InvariantCultureIgnoreCase)) {
                return HttpMethod.Trace;
            }
            throw new ArgumentException();
        }
    }
}