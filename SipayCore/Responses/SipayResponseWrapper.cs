using System;
using System.Collections.Generic;

namespace SipayCore.Responses
{
    public class SipayResponseWrapper
    {
        public string status_code { get; set; }
        public string status_description { get; set; }
        public string message { get; set; }

    }
}

