using System.Net;
using System.Net.Http;

using Nefarius.DsHidMini.ControlApp.Models;

using Xunit;

namespace Nefarius.DsHidMini.ControlApp.Tests;

public class HttpRetryPolicyTests
{
    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void ShouldRetry_TemporaryHttpStatusCodes(HttpStatusCode statusCode)
    {
        Assert.True(HttpRetryPolicy.ShouldRetry(statusCode, null));
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public void ShouldRetry_IgnoresSuccessfulAndNonTransientClientErrors(HttpStatusCode statusCode)
    {
        Assert.False(HttpRetryPolicy.ShouldRetry(statusCode, null));
    }

    [Fact]
    public void ShouldRetry_HttpRequestException()
    {
        Assert.True(HttpRetryPolicy.ShouldRetry(null, new HttpRequestException("temporary failure")));
    }

    [Fact]
    public void ShouldRetry_NoErrorDoesNotRetry()
    {
        Assert.False(HttpRetryPolicy.ShouldRetry(null, null));
    }
}
