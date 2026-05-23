using Microsoft.AspNetCore.Mvc;

namespace StockBridge.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthCallbackController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public AuthCallbackController(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("token")]
        public async Task<IActionResult> ExchangeToken([FromBody] TokenRequest req)
        {
            var zitadelDomain = _config["Zitadel:Domain"];
            var clientId = _config["Zitadel:ClientId"];
            var redirectUri = _config["Zitadel:RedirectUri"];

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = clientId!,
                ["redirect_uri"] = redirectUri!,
                ["code"] = req.Code,
                ["code_verifier"] = req.CodeVerifier
            });

            var response = await _httpClient.PostAsync($"{zitadelDomain}/oauth/v2/token", body);
            var content = await response.Content.ReadAsStringAsync();

            // Debug için log
            Console.WriteLine($"Zitadel response: {content}");

            return Content(content, "application/json");
        }
    }

    public class TokenRequest
    {
        public string Code { get; set; } = string.Empty;
        public string CodeVerifier { get; set; } = string.Empty;
    }
}
