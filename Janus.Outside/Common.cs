using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using mikev.Janus.Common;

namespace mikev.Janus.Outside
{

    public static class Common
    {
        private static Channel<RequestInfo> theChannel = Channel.CreateUnbounded<RequestInfo>();

        public readonly static ChannelReader<RequestInfo> channelReader = theChannel.Reader;

        public readonly static ChannelWriter<RequestInfo> channelWriter = theChannel.Writer; 

        public readonly static ConcurrentDictionary<String, TaskCompletionSource<ResponseInfo> > theTasks = new ConcurrentDictionary<string, TaskCompletionSource<ResponseInfo> >();

        public static void CallWithNoExceptions(ILogger logger, Action action) {
            try 
            {
                action();
            }
            catch(Exception exception)
            {
                logger.LogInformation($"CallWithNoExceptions error, message={exception.Message}");
            }
        }

        public static RequestDelegate ProcessorAdapterToRequestDelegate(IConfiguration configuration, ILogger logger, Func<HttpContext, IConfiguration, ILogger, Task> processor) {
            return async context => await processor(context, configuration, logger); 
        }

        public static Action<IApplicationBuilder> ProcessorAdapterActionAppBuilder(IConfiguration configuration, ILogger logger, Func<HttpContext, IConfiguration, ILogger, Task> processor) {
            return delegate(IApplicationBuilder app) {
                app.Run(async context => await processor(context, configuration, logger));
            };
        }

        public static bool IsValidJanusSecret(HttpRequest httpRequest, IConfiguration configuration) {
            if (!httpRequest.Headers.ContainsKey("X-Janus-Secret"))
                return false;
            return configuration["X-Janus-Secret"].Equals(httpRequest.Headers["X-Janus-Secret"]);
        }
    }


}