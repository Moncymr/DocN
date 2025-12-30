using Xunit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using DocN.Server.Services.HealthChecks;

namespace DocN.Server.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task SemanticKernelHealthCheck_ReturnsHealthy_WhenKernelIsConfigured()
    {
        // Arrange
        var kernelBuilder = Kernel.CreateBuilder();
        var kernel = kernelBuilder.Build();
        var loggerMock = new Mock<ILogger<SemanticKernelHealthCheck>>();
        var healthCheck = new SemanticKernelHealthCheck(kernel, loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task SemanticKernelHealthCheck_ReturnsUnhealthy_WhenKernelIsNull()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SemanticKernelHealthCheck>>();
        var healthCheck = new SemanticKernelHealthCheck(null!, loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task OCRServiceHealthCheck_ReturnsDegraded_WhenOCRNotConfigured()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(DocN.Core.Interfaces.IOCRService)))
            .Returns(null);
        var loggerMock = new Mock<ILogger<OCRServiceHealthCheck>>();
        var healthCheck = new OCRServiceHealthCheck(serviceProviderMock.Object, loggerMock.Object);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("not configured", result.Description);
    }
}
