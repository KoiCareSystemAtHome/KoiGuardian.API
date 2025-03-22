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
        public ActionResult <string> CreatePaymentData(double money, string description, string returnUrl)
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

                var paymentUrl = _vnpay.GetPaymentUrl(request);
                return paymentUrl;
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
                        // Lấy fe_return từ query
                        var feReturnUrl = Request.Query["fe_return"].ToString();
                        var email = Request.Query["email"].ToString();
                        var amount = float.Parse(Request.Query["vnp_Amount"]) / 100;
                        var VnPayTransactionId = Request.Query["vnp_BankTranNo"].ToString();

                        await _userService.UpdateAmount(email, amount, VnPayTransactionId);

                        // Trả về cả resultDescription và feReturnUrl
                        return Ok(new
                        {
                            Description = resultDescription,
                            RedirectUrl = feReturnUrl
                        });
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
            return BadRequest("Không có query string");
        }



    }
}
