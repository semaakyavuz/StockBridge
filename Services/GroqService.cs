using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StockBridge.API.Services
{
    public interface IGroqService
    {
        Task<string> GenerateProductDescriptionAsync(string sku, string name);
    }

    public class GroqService : IGroqService
    {
        private const string ChatCompletionsUrl = "https://api.groq.com/openai/v1/chat/completions";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GroqService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<string> GenerateProductDescriptionAsync(string sku, string name)
        {
            var apiKey = _config["Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Groq API anahtarı yapılandırılmamış (appsettings.json -> Groq:ApiKey).");

            var model = _config["Groq:Model"];
            if (string.IsNullOrWhiteSpace(model))
                model = "llama-3.1-8b-instant";

            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = "Sen bir e-ticaret sitesi için ürün açıklaması yazan bir asistansın. Sadece Türkçe, 1-2 cümlelik, kısa ve satış odaklı ürün açıklaması üret. Açıklama dışında hiçbir ek yorum, başlık veya tırnak işareti kullanma." },
                    new { role = "user", content = $"SKU: {sku}\nÜrün Adı: {name}" }
                },
                temperature = 0.7,
                max_tokens = 150
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Groq API hatası ({(int)response.StatusCode}): {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<GroqChatResponse>();
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Groq API boş bir yanıt döndürdü.");

            return text;
        }
    }

    public class GroqChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice>? Choices { get; set; }
    }

    public class GroqChoice
    {
        [JsonPropertyName("message")]
        public GroqMessage? Message { get; set; }
    }

    public class GroqMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
