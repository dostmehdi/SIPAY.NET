using Newtonsoft.Json;
using SipayCore.Models;
using SipayCore.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SipayCore.Requests
{
    public class SipayGetOrderStatusRequest
    {
        public Settings _settings { get; set; }

        public string InvoiceId { private get; set; }

        public string invoice_id { get { return this.InvoiceId; } }

        public SipayGetOrderStatusRequest(Settings settings)
        {
            this._settings = settings;
        }

    }

}
