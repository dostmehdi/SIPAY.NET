using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using SipayCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SipayCore.Models;
using SipayCore.Requests;
using SipayCore.Responses;
using SIPAY.Extensions;
using SIPAY.Models;
using SipayCoreCore;

namespace SIPAY.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ILogger<CheckoutController> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CheckoutController(ILogger<CheckoutController> logger, IConfiguration config,
          IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormCollection form)
        {
            var paymentForm = GetPaymentInfo(form);

            var recurring = GetRecurringPaymentInfo(form);

            Settings settings = new Settings();

            settings.AppID = _config["SIPAY:AppID"];
            settings.AppSecret = _config["SIPAY:AppSecret"];
            settings.MerchantKey = _config["SIPAY:MerchantKey"];
            settings.BaseUrl = _config["SIPAY:BaseUrl"];

            Item product = new Item();
            product.Description = "";
            product.Name = "Test Product";
            product.Quantity = 1;
            product.Price = paymentForm.Amount;

            if (paymentForm.Is3D == PaymentType.WhiteLabel2D /*|| paymentForm.Is3D == PaymentType.WhiteLabel2DOr3D*/)
            {
                //// 2D 

                SipayPaymentRequest payRequest = new SipayPaymentRequest(settings, paymentForm.SelectedPosData);

                payRequest.CCNo = paymentForm.CreditCardNumber.Replace(" ", "");
                payRequest.CCHolderName = paymentForm.CreditCardName;
                payRequest.CCV = paymentForm.CreditCardCvv2;
                payRequest.ExpiryYear = paymentForm.CreditCardExpireYear.ToString();
                payRequest.ExpiryMonth = paymentForm.CreditCardExpireMonth.ToString();
                payRequest.InvoiceDescription = "";
                payRequest.InvoiceId = paymentForm.OrderId;
                payRequest.IS2D = 1;

                string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
                payRequest.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
                payRequest.CancelUrl = baseUrl + "/Checkout/CancelUrl";

                payRequest.Items.Add(product);

                if (recurring.Item1)
                {
                    payRequest.IsRecurringPayment = true;
                    payRequest.RecurringPaymentNumber = recurring.Item2;
                    payRequest.RecurringPaymentCycle = recurring.Item3;
                    payRequest.RecurringPaymentInterval = recurring.Item4;
                    payRequest.RecurringWebhookKey = "yakala.co";
                }

                SipayPaymentResponse payResponse = SipayPaymentService.Pay(payRequest, settings, GetAuthorizationToken(settings).Data.token);

                string fullQuery = "status_code : " + payResponse.status_code + "<br/>"
                     + "message :" + payResponse.message + "<br/>" + "status_description :" + payResponse.status_description;

                ViewBag.ResultMessage = fullQuery;

            }

            else if (paymentForm.Is3D == PaymentType.WhiteLabel3D || paymentForm.Is3D == PaymentType.WhiteLabel2DOr3D )
            {
                //// 3D

                Sipay3DPaymentRequest paymentRequest = new Sipay3DPaymentRequest(settings, paymentForm.SelectedPosData);

                paymentRequest.CCNo = paymentForm.CreditCardNumber.Replace(" ", "");
                paymentRequest.CCHolderName = paymentForm.CreditCardName;
                paymentRequest.CCV = paymentForm.CreditCardCvv2;
                paymentRequest.ExpiryYear = paymentForm.CreditCardExpireYear.ToString();
                paymentRequest.ExpiryMonth = paymentForm.CreditCardExpireMonth.ToString();
                paymentRequest.InvoiceDescription = "";
                paymentRequest.InvoiceId = paymentForm.OrderId;

                string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
                paymentRequest.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
                paymentRequest.CancelUrl = baseUrl + "/Checkout/CancelUrl";

                paymentRequest.Items.Add(product);

                if (recurring.Item1)
                {
                    paymentRequest.IsRecurringPayment = true;
                    paymentRequest.RecurringPaymentNumber = recurring.Item2;
                    paymentRequest.RecurringPaymentCycle = recurring.Item3;
                    paymentRequest.RecurringPaymentInterval = recurring.Item4;
                    paymentRequest.RecurringWebhookKey = "yakala.co";

                }

                string requestForm = paymentRequest.GenerateFormHtmlToRedirect(_config["SIPAY:BaseUrl"] + "/api/pay3d");

                // var bytes = Encoding.UTF8.GetBytes(requestForm);
                // await HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);

                return View("Request3DSipay", requestForm);
            }

            return View();
        }

        public IActionResult PaySmart()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PaySmart(IFormCollection form)
        {
            var paymentForm = GetSmartPaymentInfo(form);

            var recurring = GetRecurringPaymentInfo(form);

            Settings settings = new Settings();

            settings.AppID = _config["SIPAY:AppID"];
            settings.AppSecret = _config["SIPAY:AppSecret"];
            settings.MerchantKey = _config["SIPAY:MerchantKey"];
            settings.BaseUrl = _config["SIPAY:BaseUrl"];

            Item product = new Item();
            product.Description = "";
            product.Name = "Test Product";
            product.Quantity = 1;
            product.Price = paymentForm.Amount;

            if (paymentForm.Is3D == PaymentType.WhiteLabel2D)
            {
                //// 2D 

                SipaySmartPaymentRequest payRequest = new SipaySmartPaymentRequest(settings, paymentForm.SelectedPosData);

                payRequest.CCNo = paymentForm.CreditCardNumber.Replace(" ", "");
                payRequest.CCHolderName = paymentForm.CreditCardName ?? "Mehdi";
                payRequest.CCV = paymentForm.CreditCardCvv2;
                // payRequest.ExpiryYear = paymentForm.CreditCardExpireYear.ToString();
                // payRequest.ExpiryMonth = paymentForm.CreditCardExpireMonth.ToString();
                payRequest.ExpiryYear = "26";
                payRequest.ExpiryMonth = "12";
                payRequest.InvoiceDescription = "";
                payRequest.InvoiceId = paymentForm.OrderId;
                payRequest.Total = paymentForm.Amount;
                payRequest.InstallmentsNumber = paymentForm.InstallmentNumber;
                payRequest.CurrencyCode = "TRY";
                payRequest.CurrencyId = "1";
               
                string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
                payRequest.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
                payRequest.CancelUrl = baseUrl + "/Checkout/CancelUrl";

                payRequest.Items.Add(product);

                if (recurring.Item1)
                {
                    payRequest.IsRecurringPayment = true;
                    payRequest.RecurringPaymentNumber = recurring.Item2;
                    payRequest.RecurringPaymentCycle = recurring.Item3;
                    payRequest.RecurringPaymentInterval = recurring.Item4;
                    payRequest.RecurringWebhookKey = "yakala.co";
                }

                payRequest.HashKey = HashHelper.GenerateHashKey(paymentForm.Amount.ToString(), paymentForm.InstallmentNumber.ToString(), "TRY", settings.MerchantKey,
                     paymentForm.OrderId, settings.AppSecret);

                SipaySmartPaymentResponse payResponse = SipayPaymentService.SmartPay(payRequest, settings, GetAuthorizationToken(settings).Data.token);

                string fullQuery = "status_code : " + payResponse.status_code + "<br/>"
                     + "message :" + payResponse.message + "<br/>" + "status_description :" + payResponse.status_description;

                ViewBag.ResultMessage = fullQuery;

            }

            else if (paymentForm.Is3D == PaymentType.WhiteLabel3D)
            {
                //// 3D

                SipaySmart3DPaymentRequest paymentRequest = new SipaySmart3DPaymentRequest(settings);

                paymentRequest.CCNo = paymentForm.CreditCardNumber.Replace(" ", "");
                paymentRequest.CCHolderName = paymentForm.CreditCardName;
                paymentRequest.CCV = paymentForm.CreditCardCvv2;
                paymentRequest.ExpiryYear = paymentForm.CreditCardExpireYear.ToString();
                paymentRequest.ExpiryMonth = paymentForm.CreditCardExpireMonth.ToString();
                paymentRequest.InvoiceDescription = "";
                paymentRequest.InvoiceId = paymentForm.OrderId;

                string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
                paymentRequest.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
                paymentRequest.CancelUrl = baseUrl + "/Checkout/CancelUrl";

                paymentRequest.PayableAmount = paymentForm.Amount;
                paymentRequest.Total = paymentForm.Amount;
                paymentRequest.InstallmentsNumber = paymentForm.InstallmentNumber;
                paymentRequest.CurrencyCode = "TRY";
                paymentRequest.CurrencyId = "1";

                paymentRequest.HashKey = HashHelper.GenerateHashKey(paymentForm.Amount.ToString(), paymentForm.InstallmentNumber.ToString(), "TRY", settings.MerchantKey,
                     paymentForm.OrderId, settings.AppSecret);

                //var c = HashHelper.ValidateHashKey(paymentRequest.hash_key, settings.AppSecret);


                //paymentRequest.HashKey = "a8b65180f00e3c70:291a:7rHBQ5GxXIAutP__ykTOsU4Ru7sgXIOZVINbMkTuHe8eXyYwiJsr7ZlHvD7BiKjEiqU2aupr6HKBrLhfzXnBGc88E5JjqdxysL2mdeiz7yeU=";

                //var d = HashHelper.ValidateHashKey(paymentRequest.hash_key, settings.AppSecret);
                
                paymentRequest.Items.Add(product);

                if (recurring.Item1)
                {
                    paymentRequest.IsRecurringPayment = true;
                    paymentRequest.RecurringPaymentNumber = recurring.Item2;
                    paymentRequest.RecurringPaymentCycle = recurring.Item3;
                    paymentRequest.RecurringPaymentInterval = recurring.Item4;
                    paymentRequest.RecurringWebhookKey = "yakala.co";

                }

                string requestForm = paymentRequest.GenerateFormHtmlToRedirect(_config["SIPAY:BaseUrl"] + "/api/paySmart3D");

                var bytes = Encoding.UTF8.GetBytes(requestForm);
                await HttpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);

                return View("Request3DSipay", requestForm);
            }

            return View();
        }

        public IActionResult BrandedPayment()
        {
            return View();
        }

        [HttpPost]
        public ActionResult BrandedPayment(IFormCollection form)
        {
            decimal amount = 0;
            string orderId = "";

            if (!String.IsNullOrEmpty(form["Amount"]))
            {
                amount = decimal.Parse(form["Amount"]);
            }

            if (!String.IsNullOrEmpty(form["OrderId"]))
            {
                orderId = form["OrderId"];
            }

            var recurring = GetRecurringPaymentInfo(form);

            Settings settings = new Settings();

            settings.BaseUrl = _config["SIPAY:BaseUrl"];
            settings.MerchantKey = _config["SIPAY:MerchantKey"];

            SipayBrandedPaymentRequest brandedRequest = new SipayBrandedPaymentRequest(settings);
            brandedRequest.CurrencyCode = "TRY";
            brandedRequest.Name = "Test";
            brandedRequest.SurName = "test";

            var invoice = new Invoice();

            invoice.InvoiceId = orderId;
            invoice.Total = amount;

            string baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value;
            invoice.ReturnUrl = baseUrl + "/Checkout/SuccessUrl";
            invoice.CancelUrl = baseUrl + "/Checkout/CancelUrl";

            invoice.InvoiceDescription = "test description";

            invoice.BillingAddress1 = "address1";
            invoice.BillingAddress2 = "address2";
            invoice.BillingCity = "Istanbul";
            invoice.BillingCountry = "TURKEY";
            invoice.BillingEmail = "demo@sipay.com.tr";
            invoice.BillingPhone = "008801777711111";
            invoice.BillingPostcode = "1111";
            invoice.BillingState = "asdasd";

            Item product = new Item();
            product.Description = "";
            product.Name = "Test Product";
            product.Quantity = 1;
            product.Price = amount;
            invoice.Items.Add(product);

            if (recurring.Item1)
            {
                invoice.IsRecurringPayment = true;
                invoice.RecurringPaymentNumber = recurring.Item2;
                invoice.RecurringPaymentCycle = recurring.Item3;
                invoice.RecurringPaymentInterval = recurring.Item4;
                invoice.RecurringWebhookKey = "yakala.co";

            }

            brandedRequest.Invoice = invoice;

            SipayBrandedPaymentResponse payResponse = SipayPaymentService.BrandedPay(brandedRequest, settings);

            if (payResponse.status_code == "100")
            {
                return Redirect(payResponse.link);
            }

            ViewBag.Error("Hata Oluştu");

            return View();
        }

        public IActionResult Refund()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Refund(IFormCollection form)
        {
            decimal amount = 0;
            string orderId = "";

            if (!String.IsNullOrEmpty(form["Amount"]))
            {
                amount = decimal.Parse(form["Amount"]);
            }

            if (!String.IsNullOrEmpty(form["OrderId"]))
            {
                orderId = form["OrderId"];
            }

            Settings settings = new Settings();

            settings.AppID = _config["SIPAY:AppID"];
            settings.AppSecret = _config["SIPAY:AppSecret"];
            settings.MerchantKey = _config["SIPAY:MerchantKey"];
            settings.BaseUrl = _config["SIPAY:BaseUrl"];

            SipayRefundRequest refundRequest = new SipayRefundRequest(settings);

            refundRequest.InvoiceId = orderId;
            refundRequest.Amount = amount;

            SipayRefundResponse payResponse = SipayPaymentService.Refund(refundRequest, settings, GetAuthorizationToken(settings).Data.token);

            if (payResponse != null)
            {
                string fullQuery = "invoice_id : " + payResponse.invoice_id + "<br/>"
                                     + "status_code :" + payResponse.status_code + "<br/>" + "order_no :" + payResponse.order_no + "<br/>" + "status_description :" + payResponse.status_description;

                ViewBag.ResultMessage = fullQuery;
            }

            return View();
        }

        public ActionResult CheckBinCode(string binCode, decimal amount, bool isRecurring)
        {
            if (binCode.Length >= 6)
            {
                Settings settings = new Settings();

                settings.AppID = _config["SIPAY:AppID"];
                settings.AppSecret = _config["SIPAY:AppSecret"];
                settings.MerchantKey = _config["SIPAY:MerchantKey"];
                settings.BaseUrl = _config["SIPAY:BaseUrl"];

                SipayGetPosRequest posRequest = new SipayGetPosRequest();

                posRequest.CreditCardNo = binCode;
                posRequest.Amount = amount;
                posRequest.CurrencyCode = "TRY";
                //posRequest.CurrencyCode = "EUR";

                posRequest.IsRecurring = isRecurring;

                SipayGetPosResponse posResponse = SipayPaymentService.GetPos(posRequest, settings, GetAuthorizationToken(settings).Data.token);

                //GEÇİCİ

                for (int i = 0; i < posResponse.Data.Count; i++)
                {
                    posResponse.Data[i].amount_to_be_paid = posResponse.Data[i].amount_to_be_paid + (i * 0.1M);
                }

                return Ok(new { posResponse = posResponse, is_3d = GetAuthorizationToken(settings).Data.is_3d });
            }

            return Ok();
        }

        public IActionResult SuccessUrl()
        {
            string sipay_status = HttpContext.Request.Query["sipay_status"];
            string order_no = HttpContext.Request.Query["order_no"];
            string invoice_id = HttpContext.Request.Query["invoice_id"];
            string status_description = HttpContext.Request.Query["status_description"];
            string sipay_payment_method = HttpContext.Request.Query["sipay_payment_method"];

            string fullQuery = " invoice_id : " + invoice_id
                 + "sipay_status :" + sipay_status + "order_no :" + order_no + "status_description :" + status_description
                 + "sipay_payment_method :" + sipay_payment_method;

            ViewBag.SuccessMessage = fullQuery;

            return View();
        }
        public IActionResult CancelUrl()
        {
            string error_code = HttpContext.Request.Query["error-code"];
            string error = HttpContext.Request.Query["error"];
            string invoice_id = HttpContext.Request.Query["invoice_id"];

            string sipay_status = HttpContext.Request.Query["sipay_status"];
            string order_no = HttpContext.Request.Query["order_no"];
            string status_description = HttpContext.Request.Query["status_description"];
            string sipay_payment_method = HttpContext.Request.Query["sipay_payment_method"];

            string fullQuery = "error_code : " + error_code + " invoice_id : " + invoice_id + " error : " + error
                 + "sipay_status :" + sipay_status + "order_no :" + order_no + "status_description :" + status_description
                 + "sipay_payment_method :" + sipay_payment_method;

            ViewBag.Error = fullQuery;

            return View();
        }
        public IActionResult BrandedSuccessUrl()
        {
            string invoiceId = HttpContext.Request.Query["invoice_id"];

            Settings settings = new Settings();

            settings.BaseUrl = _config["SIPAY:BaseUrl"];
            settings.MerchantKey = _config["SIPAY:MerchantKey"];

            SipayGetOrderStatusRequest orderStatusRequest = new SipayGetOrderStatusRequest(settings);
            orderStatusRequest.InvoiceId = invoiceId;

            var payResponse = SipayPaymentService.GetOrderStatus(orderStatusRequest, settings);

            string fullQuery = "transaction status :" + payResponse.transaction_status + "message :" + payResponse.message
                               + "recurring_id :" + payResponse.recurring_id + "recurring_status :" + payResponse.recurring_id;

            ViewBag.SuccessMessage = fullQuery;

            return View();
        }

        public IActionResult BrandedCancelUrl()
        {
            string status = HttpContext.Request.Query["status"];
            string message = HttpContext.Request.Query["success_message"];
            string link = HttpContext.Request.Query["link"];
            string fullQuery = "status :" + status + "message :" + message + "link :" + link;

            ViewBag.Error = fullQuery;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [NonAction]
        public PaymentModel GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new PaymentModel();

            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];

            if (!String.IsNullOrEmpty(form["card-number"]))
            {
                paymentInfo.CreditCardNumber = form["card-number"];
            }
            if (!String.IsNullOrEmpty(form["ExpireMonth"]))
            {
                paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            }
            if (!String.IsNullOrEmpty(form["ExpireYear"]))
            {
                paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            }

            if (!String.IsNullOrEmpty(form["Amount"]))
            {
                paymentInfo.Amount = decimal.Parse(form["Amount"]);
            }

            if (!String.IsNullOrEmpty(form["OrderId"]))
            {
                paymentInfo.OrderId = form["OrderId"];
            }
            paymentInfo.CreditCardCvv2 = form["CardCode"];

            if (!String.IsNullOrEmpty(form["SelectedPosData"]))
            {
                var posData = form["SelectedPosData"];

                paymentInfo.SelectedPosData = JsonConvert.DeserializeObject<PosData>(form["SelectedPosData"]);
            }

            if (!String.IsNullOrEmpty(form["Is3D"]))
            {
                paymentInfo.Is3D = (PaymentType)(Int32.TryParse(form["Is3D"], out int is3D) ? is3D : 0);

            }

            return paymentInfo;
        }

        [NonAction]
        public PaymentModel GetSmartPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new PaymentModel();

            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];

            if (!String.IsNullOrEmpty(form["card-number"]))
            {
                paymentInfo.CreditCardNumber = form["card-number"];
            }
            if (!String.IsNullOrEmpty(form["ExpireMonth"]))
            {
                paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            }
            if (!String.IsNullOrEmpty(form["ExpireYear"]))
            {
                paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            }

            if (!String.IsNullOrEmpty(form["Amount"]))
            {
                paymentInfo.Amount = decimal.Parse(form["Amount"]);
            }

            if (!String.IsNullOrEmpty(form["OrderId"]))
            {
                paymentInfo.OrderId = form["OrderId"];
            }
            paymentInfo.CreditCardCvv2 = form["CardCode"];

            PaymentType paymentType = PaymentType.WhiteLabel2D;
            if (!String.IsNullOrEmpty(form["Is3D"]))
            {
                if (form["Is3D"] == "on") paymentType = PaymentType.WhiteLabel3D;
            }

            paymentInfo.Is3D = paymentType;

            paymentInfo.InstallmentNumber = 1;
            if (!String.IsNullOrEmpty(form["Installment"]))
            {
                paymentInfo.InstallmentNumber = int.Parse(form["Installment"]);
            }

            return paymentInfo;
        }

        [NonAction]
        public (bool, int, string, int) GetRecurringPaymentInfo(IFormCollection form)
        {
            bool is_recurring_payment = false;
            int recurring_payment_number = 0;
            string recurring_payment_cycle = "";
            int recurring_payment_interval = 0;

            if (!String.IsNullOrEmpty(form["is_recurring_payment"]))
            {
                is_recurring_payment = form["is_recurring_payment"] == "on";
                //Boolean.TryParse(form["is_recurring_payment"], out is_recurring_payment);
            }

            if (!String.IsNullOrEmpty(form["recurring_payment_number"]))
            {
                Int32.TryParse(form["recurring_payment_number"], out recurring_payment_number);
            }

            if (!String.IsNullOrEmpty(form["recurring_payment_cycle"]))
            {
                recurring_payment_cycle = form["recurring_payment_cycle"];
            }

            if (!String.IsNullOrEmpty(form["recurring_payment_interval"]))
            {
                Int32.TryParse(form["recurring_payment_interval"], out recurring_payment_interval);
            }

            return (is_recurring_payment, recurring_payment_number, recurring_payment_cycle, recurring_payment_interval);
        }

        [NonAction]
        public SipayTokenResponse GetAuthorizationToken(Settings settings)
        {
            if (HttpContext.Session.Get<SipayTokenResponse>("token") == default)
            {
                SipayTokenResponse tokenResponse = SipayPaymentService.CreateToken(settings);

                HttpContext.Session.Set<SipayTokenResponse>("token", tokenResponse);
            }

            return HttpContext.Session.Get<SipayTokenResponse>("token");
        }
    }
}
