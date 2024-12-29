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
        private readonly string _shopId;  // Your ShopId
        private readonly string _token;  // Your Token
        private readonly string _baseUrl;  // GHN Base URL

        public GhnService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _shopId = configuration["GHN:ShopId"];
            _token = configuration["GHN:Token"];
            _baseUrl = configuration["GHN:BaseUrl"];
        }

        public async Task<string> CreateShippingOrder(GHNRequest ghnRequest)
        {
            var requestUrl = $"{_baseUrl}/v2/shipping-order/create";  // Update with your specific endpoint

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

        public async Task<string> TrackingShippingOrder(TrackingGHNRequest order_code)
        {
            var requestUrl = $"{_baseUrl}/v2/shipping-order/detail";  // Update with your specific endpoint

            // Add headers for authentication
            _httpClient.DefaultRequestHeaders.Clear();
            //_httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
            _httpClient.DefaultRequestHeaders.Add("Token", _token);

            // Serialize the request body as JSON
            var content = new StringContent(JsonConvert.SerializeObject(order_code), Encoding.UTF8, "application/json");

            // Send POST request to the GHN API
            var response = await _httpClient.PostAsync(requestUrl, content);
            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("Failed to tracking order status");
            }
        }
        //lấy tỉnh thành phố
        public async Task<string> getProvince()
        {
            var requestUrl = $"{_baseUrl}/master-data/province";  // Update with your specific endpoint

            // Add headers for authentication
            _httpClient.DefaultRequestHeaders.Clear();
            //_httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
            _httpClient.DefaultRequestHeaders.Add("Token", _token);

            // Serialize the request body as JSON
            //var content = new StringContent(JsonConvert.SerializeObject(), Encoding.UTF8, "application/json");

            // Send POST request to the GHN API
            var response = await _httpClient.GetAsync(requestUrl);
            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("Failed to load province");
            }
        }
        //Lấy quận huyện
        public async Task<string> getDistrict(getDistrict province_id)
        {
            var requestUrl = $"{_baseUrl}/master-data/district";  // Update with your specific endpoint

            // Add headers for authentication
            _httpClient.DefaultRequestHeaders.Clear();
            //_httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
            _httpClient.DefaultRequestHeaders.Add("Token", _token);

            // Serialize the request body as JSON
            var content = new StringContent(JsonConvert.SerializeObject(province_id), Encoding.UTF8, "application/json");

            // Send POST request to the GHN API
            var response = await _httpClient.PostAsync(requestUrl, content);
            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("Failed to load district by province");
            }
        }

        //Lấy phường xã
        public async Task<string> getWard(getWard district_id)
        {
            var requestUrl = $"{_baseUrl}/master-data/ward?{district_id}";  // Update with your specific endpoint

            // Add headers for authentication
            _httpClient.DefaultRequestHeaders.Clear();
            //_httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
            _httpClient.DefaultRequestHeaders.Add("Token", _token);

            // Serialize the request body as JSON
            var content = new StringContent(JsonConvert.SerializeObject(district_id), Encoding.UTF8, "application/json");

            // Send POST request to the GHN API
            var response = await _httpClient.PostAsync(requestUrl, content);
            // Handle the response
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new Exception("Failed to load ward by district");
            }
        }

    }

}
