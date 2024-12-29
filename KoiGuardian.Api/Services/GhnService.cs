using KoiGuardian.Models.Request;
using System.Text;

namespace KoiGuardian.Api.Services
{
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class GhnService
    {
        private readonly HttpClient _httpClient;
        private readonly string _shopId = "195734";  // Your ShopId
        private readonly string _token = "1e102570-c518-11ef-a349-824cd7dd2091";  // Your Token
        private readonly string _baseUrl = "https://dev-online-gateway.ghn.vn/shiip/public-api/v2";  // GHN Base URL

        public GhnService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CreateShippingOrder(GHNRequest ghnRequest)
        {
            var requestUrl = $"{_baseUrl}/shipping-order/create";  // Update with your specific endpoint

            // Add headers for authentication
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
            _httpClient.DefaultRequestHeaders.Add("Token", _token);

            // Serialize the request body as JSON
            var content = new StringContent(JsonConvert.SerializeObject(ghnRequest), Encoding.UTF8, "application/json");

            // Send POST request to the GHN API
            var response = await _httpClient.PostAsync(requestUrl, content);

            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("Failed to create shipping order");
            }
        }
    }

}
