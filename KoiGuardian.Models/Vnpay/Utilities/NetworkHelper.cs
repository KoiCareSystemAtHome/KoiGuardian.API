using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Sockets;

namespace VNPAY.NET.Utilities
{
    public class NetworkHelper
    {
        /// <summary>
        /// Lấy địa chỉ IP từ HttpContext của API Controller.
        /// </summary>
        /// <param name="context">HttpContext từ controller</param>
        /// <returns>Địa chỉ IP của client (IPv4 hoặc IPv6)</returns>
        /// <exception cref="ArgumentNullException">Ném ra nếu HttpContext là null</exception>
        /// <exception cref="NullReferenceException">Ném ra nếu không tìm thấy địa chỉ IP</exception>
        public static string GetIpAddress(HttpContext context)
        {
            // Kiểm tra HttpContext có null không
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), "HttpContext không được null");
            }

            // Kiểm tra header X-Forwarded-For (dùng khi client ở sau proxy/load balancer)
            string ipAddress = null;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                // X-Forwarded-For có thể chứa nhiều IP (dạng "client_ip, proxy1, proxy2")
                // Lấy IP đầu tiên (IP của client)
                var forwardedIps = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                ipAddress = forwardedIps.Length > 0 ? forwardedIps[0].Trim() : null;
            }

            // Nếu không có X-Forwarded-For, lấy từ RemoteIpAddress
            if (string.IsNullOrEmpty(ipAddress))
            {
                var remoteIpAddress = context.Connection?.RemoteIpAddress;
                if (remoteIpAddress == null)
                {
                    throw new NullReferenceException("Không tìm thấy địa chỉ IP của client");
                }

                // Xử lý cả IPv4 và IPv6
                if (remoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // Nếu là IPv6, kiểm tra xem có ánh xạ IPv4 không
                    if (remoteIpAddress.IsIPv4MappedToIPv6)
                    {
                        ipAddress = remoteIpAddress.MapToIPv4().ToString();
                    }
                    else
                    {
                        // Trả về IPv6 nguyên bản
                        ipAddress = remoteIpAddress.ToString();
                    }
                }
                else
                {
                    // IPv4
                    ipAddress = remoteIpAddress.ToString();
                }
            }

            // Xử lý trường hợp localhost
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
            {
                // "::1" là localhost trong IPv6, thay bằng "127.0.0.1"
                ipAddress = "127.0.0.1";
            }

            return ipAddress;
        }
    }
}