using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StockBridge.API.Controllers;
using StockBridge.API.Data;
using StockBridge.API.Models;
using StockBridge.API.Services;

namespace StockBridge.Tests.Controllers
{
    public class SyncControllerTests
    {
        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task SyncProduct_ProductNotFound_ReturnsNotFound()
        {
            using var context = CreateContext();
            var erpMock = new Mock<IErpService>();
            var controller = new SyncController(context, erpMock.Object, Mock.Of<ILogger<SyncController>>());

            var result = await controller.SyncProduct(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SyncProduct_ErpSucceeds_UpdatesLastSyncedAtAndReturnsOk()
        {
            using var context = CreateContext();
            var product = new Product { Sku = "SKU1", Name = "Test", StockQuantity = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var erpMock = new Mock<IErpService>();
            erpMock.Setup(s => s.SyncProductAsync(product.Id, product.Sku, product.Name, product.StockQuantity))
                .ReturnsAsync(true);

            var controller = new SyncController(context, erpMock.Object, Mock.Of<ILogger<SyncController>>());

            var result = await controller.SyncProduct(product.Id);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(product.LastSyncedAt);
            erpMock.Verify(
                s => s.SyncProductAsync(product.Id, product.Sku, product.Name, product.StockQuantity),
                Times.Once);
        }

        [Fact]
        public async Task SyncProduct_ErpFails_Returns503AndLeavesLastSyncedAtNull()
        {
            using var context = CreateContext();
            var product = new Product { Sku = "SKU2", Name = "Test2", StockQuantity = 5 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var erpMock = new Mock<IErpService>();
            erpMock.Setup(s => s.SyncProductAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            var controller = new SyncController(context, erpMock.Object, Mock.Of<ILogger<SyncController>>());

            var result = await controller.SyncProduct(product.Id);

            var objResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(503, objResult.StatusCode);
            Assert.Null(product.LastSyncedAt);
        }

        [Fact]
        public async Task SyncAll_OnlySyncsActiveProducts_AndAggregatesCounts()
        {
            using var context = CreateContext();
            context.Products.AddRange(
                new Product { Sku = "A", Name = "A", StockQuantity = 1, IsActive = true },
                new Product { Sku = "B", Name = "B", StockQuantity = 2, IsActive = true },
                new Product { Sku = "C", Name = "C", StockQuantity = 3, IsActive = false });
            await context.SaveChangesAsync();

            var erpMock = new Mock<IErpService>();
            erpMock.SetupSequence(s => s.SyncProductAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            var controller = new SyncController(context, erpMock.Object, Mock.Of<ILogger<SyncController>>());

            var result = await controller.SyncAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = okResult.Value!;
            var successCount = (int)value.GetType().GetProperty("successCount")!.GetValue(value)!;
            var failedCount = (int)value.GetType().GetProperty("failedCount")!.GetValue(value)!;
            var totalProducts = (int)value.GetType().GetProperty("totalProducts")!.GetValue(value)!;

            Assert.Equal(1, successCount);
            Assert.Equal(1, failedCount);
            Assert.Equal(2, totalProducts);
            erpMock.Verify(
                s => s.SyncProductAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetStatus_DelegatesToErpService_AndReturnsOk()
        {
            using var context = CreateContext();
            var expected = new ErpSyncResult { IsSuccess = true, Message = "IFS'te kayitli" };
            var erpMock = new Mock<IErpService>();
            erpMock.Setup(s => s.GetSyncStatusAsync("SKU1")).ReturnsAsync(expected);

            var controller = new SyncController(context, erpMock.Object, Mock.Of<ILogger<SyncController>>());

            var result = await controller.GetStatus("SKU1");

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(expected, okResult.Value);
        }
    }
}
