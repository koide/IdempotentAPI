using System.Net;
using FluentAssertions;
using Xunit;

namespace IdempotentAPI.IntegrationTests
{
    // Semi-Automated Tests.
    // TODO: Fully automated tests.
    //
    // Current Semi-Automated Requirements:
    // - Local Redis in Port 6379.
    public class TestWebAPIsConcurrentTests : IClassFixture<WebApi1ApplicationFactory>, IClassFixture<WebApi2ApplicationFactory>
    {
        private readonly HttpClient _httpClientForInstance1;
        private readonly HttpClient _httpClientForInstance2;

        public TestWebAPIsConcurrentTests(WebApi1ApplicationFactory api1ApplicationFactory,
            WebApi2ApplicationFactory api2ApplicationFactory)
        {
            _httpClientForInstance1 = api1ApplicationFactory.CreateClient();
            _httpClientForInstance2 = api2ApplicationFactory.CreateClient();
        }

        [Fact]
        public async Task PostRequestsConcurrent_OnClusterEnvironment_WithErrorResponse_ShouldReturnTheErrorAndA409Response()
        {
            // Arrange
            var guid = Guid.NewGuid().ToString();

            _httpClientForInstance1.DefaultRequestHeaders.Add("IdempotencyKey", guid);
            _httpClientForInstance2.DefaultRequestHeaders.Add("IdempotencyKey", guid);

            // Act
            var httpGetTask1 = _httpClientForInstance1.PostAsync("v6/TestingIdempotentAPI/customNotAcceptable406?delaySeconds=5", null);
            var httpGetTask2 = _httpClientForInstance2.PostAsync("v6/TestingIdempotentAPI/customNotAcceptable406?delaySeconds=5", null);

            await Task.WhenAll(httpGetTask1, httpGetTask2);

            // Assert
            var resultStatusCodes = new List<HttpStatusCode>() { httpGetTask1.Result.StatusCode, httpGetTask2.Result.StatusCode };
            resultStatusCodes.Should().Contain(HttpStatusCode.NotAcceptable);
            resultStatusCodes.Should().Contain(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task PostRequestsConsecutively_WithErrorResponse_ShouldReturnErrorResponsesWithDifferentData()
        {
            // Arrange
            var guid = Guid.NewGuid().ToString();

            _httpClientForInstance1.DefaultRequestHeaders.Add("IdempotencyKey", guid);

            // Act
            var httpResponse1 = await _httpClientForInstance1.PostAsync("v6/TestingIdempotentAPI/customNotAcceptable406?delaySeconds=5", null);
            var httpResponse2 = await _httpClientForInstance1.PostAsync("v6/TestingIdempotentAPI/customNotAcceptable406?delaySeconds=5", null);


            // Assert
            httpResponse1.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
            httpResponse2.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);

            var responseContent1 = await httpResponse1.Content.ReadAsStringAsync();
            var responseContent2 = await httpResponse2.Content.ReadAsStringAsync();

            responseContent1.Should().NotBe(responseContent2);
        }
    }
}