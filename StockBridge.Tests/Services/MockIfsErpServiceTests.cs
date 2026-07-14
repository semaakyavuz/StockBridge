using Microsoft.Extensions.Logging;
using Moq;
using StockBridge.API.Services;

namespace StockBridge.Tests.Services
{
    public class MockIfsErpServiceTests
    {
        [Fact]
        public async Task GetSyncStatusAsync_UnknownSku_ReturnsNotFound()
        {
            var sut = new MockIfsErpService(Mock.Of<ILogger<MockIfsErpService>>());
            var sku = $"UNKNOWN-{Guid.NewGuid()}";

            var status = await sut.GetSyncStatusAsync(sku);

            Assert.False(status.IsSuccess);
            Assert.Null(status.LastSyncedAt);
        }

        [Fact]
        public async Task SyncProductAsync_RepeatedCalls_EventuallySucceedsAndRecordsSyncedItem()
        {
            var sut = new MockIfsErpService(Mock.Of<ILogger<MockIfsErpService>>());
            var succeeded = false;

            for (var i = 0; i < 100 && !succeeded; i++)
            {
                var sku = $"SKU-{Guid.NewGuid()}";
                succeeded = await sut.SyncProductAsync(i, sku, "Test Product", 10);

                if (succeeded)
                {
                    var status = await sut.GetSyncStatusAsync(sku);
                    Assert.True(status.IsSuccess);
                    Assert.NotNull(status.LastSyncedAt);
                }
            }

            Assert.True(succeeded, "Expected at least one successful sync out of 100 attempts (~90% success rate).");
        }

        [Fact]
        public async Task SyncProductAsync_RepeatedCalls_EventuallySimulatesTransientFailure()
        {
            var sut = new MockIfsErpService(Mock.Of<ILogger<MockIfsErpService>>());
            var failed = false;

            for (var i = 0; i < 100 && !failed; i++)
            {
                var sku = $"SKU-{Guid.NewGuid()}";
                var result = await sut.SyncProductAsync(i, sku, "Test Product", 10);
                failed = !result;
            }

            Assert.True(failed, "Expected at least one simulated failure out of 100 attempts (~10% failure rate).");
        }
    }
}
