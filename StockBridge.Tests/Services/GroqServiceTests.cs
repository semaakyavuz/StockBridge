using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using StockBridge.API.Services;

namespace StockBridge.Tests.Services
{
    public class GroqServiceTests
    {
        private static IConfiguration BuildConfig(string? apiKey = "test-key", string? model = null)
        {
            var dict = new Dictionary<string, string?>();
            if (apiKey != null) dict["Groq:ApiKey"] = apiKey;
            if (model != null) dict["Groq:Model"] = model;
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        private static HttpClient CreateHttpClient(HttpResponseMessage response)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            return new HttpClient(handlerMock.Object);
        }

        [Fact]
        public async Task GenerateProductDescriptionAsync_MissingApiKey_Throws()
        {
            var config = BuildConfig(apiKey: null);
            var client = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));
            var sut = new GroqService(client, config);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.GenerateProductDescriptionAsync("SKU1", "Test Product"));
        }

        [Fact]
        public async Task GenerateProductDescriptionAsync_SuccessResponse_ReturnsTrimmedText()
        {
            var config = BuildConfig();
            var json = "{\"choices\":[{\"message\":{\"content\":\"  \\\"Harika bir urun.\\\"  \"}}]}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var client = CreateHttpClient(response);
            var sut = new GroqService(client, config);

            var result = await sut.GenerateProductDescriptionAsync("SKU1", "Test Product");

            Assert.Equal("Harika bir urun.", result);
        }

        [Fact]
        public async Task GenerateProductDescriptionAsync_ApiError_ThrowsWithStatusCodeInMessage()
        {
            var config = BuildConfig();
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("boom")
            };
            var client = CreateHttpClient(response);
            var sut = new GroqService(client, config);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.GenerateProductDescriptionAsync("SKU1", "Test Product"));
            Assert.Contains("500", ex.Message);
        }

        [Fact]
        public async Task GenerateProductDescriptionAsync_EmptyChoices_Throws()
        {
            var config = BuildConfig();
            var json = "{\"choices\":[]}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            var client = CreateHttpClient(response);
            var sut = new GroqService(client, config);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => sut.GenerateProductDescriptionAsync("SKU1", "Test Product"));
        }
    }
}
