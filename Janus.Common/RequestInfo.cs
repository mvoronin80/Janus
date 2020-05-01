using System;
using System.Collections.Generic;

namespace mikev.Janus.Common
{
    public sealed class RequestInfo
    {
        public String RequestId {get; set;}

        public String Url {get; set;}

        public String Query {get; set;}

        public Dictionary<String, String[]> Headers {get; set; } = new Dictionary<string, string[]>();

        public String Method {get; set;}

        public String Body {get; set;}

        public override String ToString()
        {
            return $"Request {this.RequestId}:method={this.Method}, url={this.Url}, bodyEmpty={String.IsNullOrWhiteSpace(this.Body)}";
        }
    }
}