using KoiGuardian.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;

namespace KoiGuardian.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VnpayController : ControllerBase
    {
        private readonly IVnpayService _vnpay;
        private readonly IConfiguration _configuration;
        private readonly IAccountServices _userService;

        public VnpayController(IVnpayService vnPayservice, IConfiguration configuration, IAccountServices accountServices)
        {
            _vnpay = vnPayservice;
            _configuration = configuration;
            _userService = accountServices;

            _vnpay.Initialize(_configuration["Vnpay:TmnCode"], _configuration["Vnpay:HashSecret"], _configuration["Vnpay:BaseUrl"]/*, _configuration["Vnpay:CallbackUrl"]*/);
        }

        /// <summary>
        /// Tạo url thanh toán
        /// </summary>
        /// <param name="money">Số tiền phải thanh toán</param>
        /// <param name="description">Mô tả giao dịch</param>
        /// <returns></returns>
        [HttpGet("CreatePaymentUrl")]
        public ActionResult<string> CreatePaymentUrl(double money, string description,string returnUrl)
        {
            try
            {
                var ipAddress = NetworkHelper.GetIpAddress(HttpContext); // Lấy địa chỉ IP của thiết bị thực hiện giao dịch

                var request = new PaymentRequest
                {
                    PaymentId = DateTime.Now.Ticks,
                    Money = money,
                    Description = description,
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY, // Tùy chọn. Mặc định là tất cả phương thức giao dịch
                    CreatedDate = DateTime.Now, // Tùy chọn. Mặc định là thời điểm hiện tại
                    Currency = Currency.VND, // Tùy chọn. Mặc định là VND (Việt Nam đồng)
                    Language = DisplayLanguage.Vietnamese, // Tùy chọn. Mặc định là tiếng Việt
                    CallBackUrl = returnUrl

                };

                var paymentUrl = _vnpay.GetPaymentUrl(request);

                return Created(paymentUrl, paymentUrl);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Thực hiện hành động sau khi thanh toán. URL này cần được khai báo với VNPAY trước (ví dụ: http://localhost:1234/api/Vnpay/IpnAction)
        /// </summary>
        /// <returns></returns>
        [HttpGet("IpnAction")]
        public IActionResult IpnAction()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                    if (paymentResult.IsSuccess)
                    {
                        // Thực hiện hành động nếu thanh toán thành công tại đây. Ví dụ: Cập nhật trạng thái đơn hàng trong cơ sở dữ liệu.
                        return Ok();
                    }

                    // Thực hiện hành động nếu thanh toán thất bại tại đây. Ví dụ: Hủy đơn hàng.
                    return BadRequest("Thanh toán thất bại");
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return NotFound("Không tìm thấy thông tin thanh toán.");
        }

        /// <summary>
        /// Tạo dữ liệu thô để React Native tự tạo link thanh toán
        /// </summary>
        [HttpGet("CreatePaymentData")]
        public ActionResult<Dictionary<string, string>> CreatePaymentData(double money, string description, string returnUrl)
        {
            try
            {
                var ipAddress = NetworkHelper.GetIpAddress(HttpContext);

                var request = new PaymentRequest
                {
                    PaymentId = DateTime.Now.Ticks,
                    Money = money,
                    Description = description,
                    IpAddress = ipAddress,
                    BankCode = BankCode.ANY,
                    CreatedDate = DateTime.Now,
                    Currency = Currency.VND,
                    Language = DisplayLanguage.Vietnamese,
                    CallBackUrl = returnUrl
                };

                // Tạo dữ liệu thô
                var data = new Dictionary<string, string>
                {
                    { "vnp_Amount", (request.Money * 100).ToString() },
                    { "vnp_Command", "pay" },
                    { "vnp_CreateDate", request.CreatedDate.ToString("yyyyMMddHHmmss") },
                    { "vnp_CurrCode", request.Currency.ToString() },
                    { "vnp_IpAddr", request.IpAddress },
                    { "vnp_Locale", request.Language == DisplayLanguage.Vietnamese ? "vn" : "en" },
                    { "vnp_OrderInfo", request.Description },
                    { "vnp_ReturnUrl", request.CallBackUrl },
                    { "vnp_TxnRef", request.PaymentId.ToString() },
                    { "vnp_Version", "2.1.0" },
                    { "vnp_TmnCode", _configuration["Vnpay:TmnCode"] }
                };

                // Tạo SecureHash
                var sortedData = data.OrderBy(k => k.Key);
                string rawData = string.Join("&", sortedData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                string secureHash = ComputeHmacSha512(rawData);
                data["vnp_SecureHash"] = secureHash;

                return Ok(data); // Trả về dữ liệu thô
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private string ComputeHmacSha512(string rawData)
        {
            // Sử dụng HMAC-SHA512 để tính SecureHash
            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_configuration["Vnpay:HashSecret"])))
            {
                byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Trả kết quả thanh toán về cho người dùng
        /// </summary>
        /// <returns></returns>
        [HttpGet("Callback")]
        public ActionResult<string> Callback()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                    var resultDescription = $"{paymentResult.PaymentResponse.Description}. {paymentResult.TransactionStatus.Description}.";

                    if (paymentResult.IsSuccess)
                    {
                        return Ok(resultDescription);
                    }

                    return BadRequest(resultDescription);
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            return NotFound("Không tìm thấy thông tin thanh toán.");
        }
        //nạp tiền
        [HttpGet("CallbackWithUserInfo")]
        public async Task<ActionResult<string>> CallbackWithUserInfo()
        {
            if (Request.QueryString.HasValue)
            {
                try
                {
                    var paymentResult = _vnpay.GetPaymentResult(Request.Query);
                    var resultDescription = $"{paymentResult.PaymentResponse.Description}. {paymentResult.TransactionStatus.Description}.";

                    if (paymentResult.IsSuccess)
                    {
                        var email = Request.Query["email"].ToString();
                        var amount = float.Parse(Request.Query["vnp_Amount"])/100;
                        var VnPayTransactionId =Request.Query["vnp_BankTranNo"].ToString();
                        await _userService.UpdateAmount(email, amount, VnPayTransactionId);
                        // Xử lý logic cập nhật đơn hàng dựa trên email và số tiền nạp vào
                        return Ok(resultDescription);
                    }

                    return BadRequest(resultDescription);
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        Message = "Lỗi xử lý thanh toán",
                        Error = ex.Message
                    });
                }
            }

            return NotFound("Không tìm thấy thông tin thanh toán.");
        }



    }
}
