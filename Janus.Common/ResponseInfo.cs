using System;
using System.Collections.Generic;

namespace mikev.Janus.Common
{
    public class ResponseInfo
    {
        public String RequestId {get; set;}

        public int StatusCode {get; set;}

        public Dictionary<String, String[]> Headers {get; set; } = new Dictionary<string, string[]>();

        public String Body {get; set;}

        public override String ToString() 
        {
            return $"Response {this.RequestId}: StatusCode={this.StatusCode}";
        }
    }
}