﻿using System.Globalization;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;
using VNPAY.NET.Utilities;
using Microsoft.AspNetCore.Http;

namespace KoiGuardian.Api.Services
{
    /// <summary>
    /// Giao diện định nghĩa các phương thức cần thiết để tích hợp với hệ thống thanh toán VNPAY.
    /// Các phương thức trong giao diện này cung cấp chức năng khởi tạo tham số thanh toán, tạo URL thanh toán và thực hiện giao dịch.
    /// </summary>
    public interface IVnpayService
    {
        /// <summary>
        /// Khởi tạo các tham số cần thiết cho giao dịch thanh toán với VNPAY.
        /// Phương thức này thiết lập các tham số như mã cửa hàng, mật khẩu bảo mật, và URL callback.
        /// </summary>
        /// <param name="tmnCode">Mã cửa hàng của bạn trên VNPAY.</param>
        /// <param name="hashSecret">Mật khẩu bảo mật dùng để mã hóa và xác thực giao dịch.</param>
        /// <param name="callbackUrl">URL mà VNPAY sẽ gọi lại sau khi giao dịch hoàn tất.</param>
        /// <param name="baseUrl">URL của trang web thanh toán, mặc định sử dụng URL của môi trường Sandbox.</param>
        /// <param name="version">Phiên bản của API mà bạn đang sử dụng.</param>
        /// <param name="orderType">Loại đơn hàng.</param>
        void Initialize(
            string tmnCode,
            string hashSecret,
            string baseUrl,
            //string callbackUrl,
            string version = "2.1.0",
            string orderType = "other");

        /// <summary>
        /// Tạo URL thanh toán cho giao dịch dựa trên các tham số trong yêu cầu thanh toán.
        /// </summary>
        /// <param name="request">Thông tin yêu cầu thanh toán, bao gồm các tham số như mã giao dịch, số tiền, mô tả,...</param>
        /// <param name="isTest">Chỉ định xem có phải là môi trường thử nghiệm hay không (mặc định là true).</param>
        /// <returns>URL thanh toán để chuyển hướng người dùng tới trang thanh toán của VNPAY.</returns>
        string GetPaymentUrl(PaymentRequest request);

        /// <summary>
        /// Thực hiện giao dịch thanh toán và trả về kết quả.
        /// Phương thức này được gọi khi nhận được thông tin callback từ VNPAY.
        /// </summary>
        /// <param name="collections">Thông tin các tham số trả về từ VNPAY qua callback.</param>
        /// <returns></returns>
        PaymentResult GetPaymentResult(IQueryCollection parameters);
    }
    public class VnpayService : IVnpayService
    {
        private string _tmnCode;
        private string _hashSecret;
        //private string _callbackUrl;
        private string _baseUrl;
        private string _version;
        private string _orderType;

        public void Initialize(string tmnCode,
            string hashSecret,
            string baseUrl,
            //string callbackUrl,
            string version = "2.1.0",
            string orderType = "other")
        {
            _tmnCode = tmnCode;
            _hashSecret = hashSecret;
           /* _callbackUrl = callbackUrl;*/
            _baseUrl = baseUrl;
            _version = version;
            _orderType = orderType;

            EnsureParametersBeforePayment();
        }

        /// <summary>
        /// Tạo URL thanh toán 
        /// </summary>
        /// <param name="request">Thông tin cần có để tạo yêu cầu</param>
        /// <returns></returns>
        public string GetPaymentUrl(PaymentRequest request)
        {
            EnsureParametersBeforePayment();

            if (request.Money < 5000 || request.Money > 1000000000)
            {
                throw new ArgumentException("Số tiền thanh toán phải nằm trong khoảng 5.000 (VND) đến 1.000.000.000 (VND).");
            }

            if (string.IsNullOrEmpty(request.Description))
            {
                throw new ArgumentException("Không được để trống mô tả giao dịch.");
            }

            if (string.IsNullOrEmpty(request.IpAddress))
            {
                throw new ArgumentException("Không được để trống địa chỉ IP.");
            }

            var helper = new PaymentHelper();
            helper.AddRequestData("vnp_Version", _version);
            helper.AddRequestData("vnp_Command", "pay");
            helper.AddRequestData("vnp_TmnCode", _tmnCode);
            helper.AddRequestData("vnp_Amount", (request.Money * 100).ToString());
            helper.AddRequestData("vnp_CreateDate", request.CreatedDate.ToString("yyyyMMddHHmmss"));
            helper.AddRequestData("vnp_CurrCode", request.Currency.ToString().ToUpper());
            helper.AddRequestData("vnp_IpAddr", request.IpAddress);
            helper.AddRequestData("vnp_Locale", EnumHelper.GetDescription(request.Language));
            helper.AddRequestData("vnp_BankCode", request.BankCode == BankCode.ANY ? string.Empty : request.BankCode.ToString());
            helper.AddRequestData("vnp_OrderInfo", request.Description.Trim());
            helper.AddRequestData("vnp_OrderType", _orderType);
            helper.AddRequestData("vnp_ReturnUrl", request.CallBackUrl);
            helper.AddRequestData("vnp_TxnRef", request.PaymentId.ToString());

            return helper.GetPaymentUrl(_baseUrl, _hashSecret);
        }

        /// <summary>
        /// Lấy kết quả thanh toán sau khi thực hiện giao dịch.
        /// </summary>
        /// <param name="parameters">Các tham số trong chuỗi truy vấn của <c>CallbackUrl</c></param>
        /// <returns></returns>
        public PaymentResult GetPaymentResult(IQueryCollection parameters)
        {
            var responseData = parameters
                .Where(kv => !string.IsNullOrEmpty(kv.Key) && kv.Key.StartsWith("vnp_"))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

            var vnp_BankCode = responseData.GetValueOrDefault("vnp_BankCode");
            var vnp_BankTranNo = responseData.GetValueOrDefault("vnp_BankTranNo");
            var vnp_CardType = responseData.GetValueOrDefault("vnp_CardType");
            var vnp_PayDate = responseData.GetValueOrDefault("vnp_PayDate");
            var vnp_OrderInfo = responseData.GetValueOrDefault("vnp_OrderInfo");
            var vnp_TransactionNo = responseData.GetValueOrDefault("vnp_TransactionNo");
            var vnp_ResponseCode = responseData.GetValueOrDefault("vnp_ResponseCode");
            var vnp_TransactionStatus = responseData.GetValueOrDefault("vnp_TransactionStatus");
            var vnp_TxnRef = responseData.GetValueOrDefault("vnp_TxnRef");
            var vnp_SecureHash = responseData.GetValueOrDefault("vnp_SecureHash");

            if (string.IsNullOrEmpty(vnp_BankCode)
                || string.IsNullOrEmpty(vnp_OrderInfo)
                || string.IsNullOrEmpty(vnp_TransactionNo)
                || string.IsNullOrEmpty(vnp_ResponseCode)
                || string.IsNullOrEmpty(vnp_TransactionStatus)
                || string.IsNullOrEmpty(vnp_TxnRef)
                || string.IsNullOrEmpty(vnp_SecureHash))
            {
                throw new ArgumentException("Không đủ dữ liệu để xác thực giao dịch");
            }

            var helper = new PaymentHelper();
            foreach (var (key, value) in responseData)
            {
                if (!key.Equals("vnp_SecureHash"))
                {
                    helper.AddResponseData(key, value);
                }
            }

            var responseCode = (ResponseCode)sbyte.Parse(vnp_ResponseCode);
            var transactionStatusCode = (TransactionStatusCode)sbyte.Parse(vnp_TransactionStatus);

            return new PaymentResult
            {
                PaymentId = long.Parse(vnp_TxnRef),
                VnpayTransactionId = long.Parse(vnp_TransactionNo),
                IsSuccess = transactionStatusCode == TransactionStatusCode.Code_00 && responseCode == ResponseCode.Code_00 && helper.IsSignatureCorrect(vnp_SecureHash, _hashSecret),
                Description = vnp_OrderInfo,
                PaymentMethod = string.IsNullOrEmpty(vnp_CardType) ? "Không xác định" : vnp_CardType,
                Timestamp = string.IsNullOrEmpty(vnp_PayDate)
                    ? DateTime.Now
                    : DateTime.ParseExact(vnp_PayDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                TransactionStatus = new TransactionStatus
                {
                    Code = transactionStatusCode,
                    Description = EnumHelper.GetDescription(transactionStatusCode)
                },
                PaymentResponse = new PaymentResponse
                {
                    Code = responseCode,
                    Description = EnumHelper.GetDescription(responseCode)
                },
                BankingInfor = new BankingInfor
                {
                    BankCode = vnp_BankCode,
                    BankTransactionId = string.IsNullOrEmpty(vnp_BankTranNo) ? "Không xác định" : vnp_BankTranNo,
                }
            };
        }

        private void EnsureParametersBeforePayment()
        {
            if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_tmnCode) || string.IsNullOrEmpty(_hashSecret) /*|| string.IsNullOrEmpty(_callbackUrl)*/)
            {
                throw new ArgumentException("Không tìm thấy BaseUrl, TmnCode, HashSecret");
            }
        }
    }
}
