using System;
using System.Collections.Generic;

namespace SipayCore.Responses
{
    public class SipayTokenResponse : SipayResponseWrapper
    {
        public TokenData Data { get; set; }
        public class TokenData
        {
            public string token { get; set; }
            public string is_3d { get; set; }

        }
    }
}
