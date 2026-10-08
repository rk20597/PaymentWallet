using System.Text;
using System.Text.Json;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;

namespace PaymentWallet.API.Services
{
    public class HyperswitchService : IHyperswitchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger<HyperswitchService> _logger;

        public HyperswitchService(
            IConfiguration config,
            ILogger<HyperswitchService> logger)
        {
            _apiKey = config["Hyperswitch:ApiKey"] ?? "";
            _baseUrl = config["Hyperswitch:BaseUrl"] ??
                "https://sandbox.hyperswitch.io";
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders
                .Add("api-key", _apiKey);
        }

        public async Task<HyperswitchPaymentResponse>
    CreatePayment(decimal amount,
    string currency, string description)
        {
            try
            {
                var payload = new
                {
                    amount = (int)(amount * 100),
                    currency = currency,
                    confirm = false,
                    capture_method = "manual",
                    description = description,
                    return_url =
                        "https://localhost:5235/payment/return"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    _baseUrl + "/payments", content);
                var responseBody = await response.Content
                    .ReadAsStringAsync();

                _logger.LogInformation(
                    "Hyperswitch create payment response: {Response}",
                    responseBody);

                // Extract payment_id manually
                using var doc = JsonDocument.Parse(
                    responseBody.Replace(
                        "return_url\":https://",
                        "return_url\":\"https://")
                    .Replace(
                        "/payment/return,",
                        "/payment/return\","));

                var root = doc.RootElement;

                if (root.TryGetProperty("error", out _))
                {
                    return new HyperswitchPaymentResponse
                    {
                        Status = "failed",
                        Error = "Hyperswitch API error"
                    };
                }

                var paymentId = root
                    .TryGetProperty("payment_id", out var pid)
                    ? pid.GetString() : null;

                var status = root
                    .TryGetProperty("status", out var st)
                    ? st.GetString() : "failed";

                var clientSecret = root
                    .TryGetProperty("client_secret", out var cs)
                    ? cs.GetString() : null;

                return new HyperswitchPaymentResponse
                {
                    PaymentId = paymentId,
                    Status = status,
                    ClientSecret = clientSecret
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Hyperswitch error: {Error}", ex.Message);
                return new HyperswitchPaymentResponse
                {
                    Status = "failed",
                    Error = ex.Message
                };
            }
        }


        public async Task<HyperswitchPaymentResponse>
            ConfirmPayment(string paymentId)
        {
            try
            {
                var payload = new
                {
                    payment_method = "card",
                    payment_method_data = new
                    {
                        card = new
                        {
                            card_number = "4111111111111111",
                            card_exp_month = "03",
                            card_exp_year = "2030",
                            card_holder_name = "Test User",
                            card_cvc = "737"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    _baseUrl + "/payments/" +
                    paymentId + "/confirm", content);
                _logger.LogInformation("Attempting to confirm payment: {PaymentId}",paymentId);

                var responseBody = await response.Content
                    .ReadAsStringAsync();

                _logger.LogInformation(
                    "Hyperswitch confirm response: {Response}",
                    responseBody);

                var result = JsonSerializer.Deserialize
                    <HyperswitchPaymentResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return result ?? new HyperswitchPaymentResponse
                {
                    Status = "failed",
                    Error = "Failed to parse response"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Hyperswitch confirm error: {Error}",
                    ex.Message);
                return new HyperswitchPaymentResponse
                {
                    Status = "failed",
                    Error = ex.Message
                };
            }
        }

        public async Task<HyperswitchPaymentResponse>
            CapturePayment(string paymentId, decimal amount)
        {
            try
            {
                var payload = new
                {
                    amount_to_capture = (int)(amount * 100)
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(
                    json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    _baseUrl + "/payments/" +
                    paymentId + "/capture", content);
                var responseBody = await response.Content
                    .ReadAsStringAsync();

                _logger.LogInformation(
                    "Hyperswitch capture response: {Response}",
                    responseBody);

                var result = JsonSerializer.Deserialize
                    <HyperswitchPaymentResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return result ?? new HyperswitchPaymentResponse
                {
                    Status = "failed",
                    Error = "Failed to parse response"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Hyperswitch capture error: {Error}",
                    ex.Message);
                return new HyperswitchPaymentResponse
                {
                    Status = "failed",
                    Error = ex.Message
                };
            }
        }
    }
}

