using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NexusERP.API.Infrastructure.ExceptionHandling;
using NexusERP.Application.Features.Customers.Exceptions;

namespace NexusERP.Tests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        return services.BuildServiceProvider();
    }

    private static HttpContext CreateHttpContext(ServiceProvider provider)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };

        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(httpContext.Response.Body);
    }

    [Fact]
    public async Task TryHandleAsync_WhenValidationException_ShouldReturnBadRequestWithPropertyErrors()
    {
        using var provider = CreateServiceProvider();
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateHttpContext(provider);
        var exception = new ValidationException(new[]
        {
            new ValidationFailure("Code", "Code cannot be empty."),
            new ValidationFailure("Code", "Code must be unique."),
            new ValidationFailure("Name", "Name is required.")
        });

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        using var json = await ReadJsonAsync(httpContext);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Code", out var codeErrors));

        var codeMessages = codeErrors.EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(2, codeMessages.Length);
        Assert.Contains("Code cannot be empty.", codeMessages);
        Assert.Contains("Code must be unique.", codeMessages);

        Assert.True(errors.TryGetProperty("Name", out var nameErrors));
        var nameMessages = nameErrors.EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("Name is required.", nameMessages);
    }

    [Fact]
    public async Task TryHandleAsync_WhenKeyNotFoundException_ShouldReturnNotFoundProblemDetails()
    {
        using var provider = CreateServiceProvider();
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateHttpContext(provider);
        var exception = new KeyNotFoundException("Customer not found: 42");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        using var json = await ReadJsonAsync(httpContext);
        var root = json.RootElement;

        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("Kayıt bulunamadı", root.GetProperty("title").GetString());
        Assert.Equal("İstenen kayıt bulunamadı.", root.GetProperty("detail").GetString());

        var rawText = root.GetRawText();
        Assert.DoesNotContain("Customer not found: 42", rawText);
    }

    [Fact]
    public async Task TryHandleAsync_WhenDuplicateCustomerCodeException_ShouldReturnConflictProblemDetails()
    {
        using var provider = CreateServiceProvider();
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateHttpContext(provider);
        var exception = new DuplicateCustomerCodeException("C-100");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        using var json = await ReadJsonAsync(httpContext);
        var root = json.RootElement;

        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Müşteri kodu zaten kullanılıyor", root.GetProperty("title").GetString());
        Assert.Equal("Bu müşteri kodu daha önce kullanılmış. Farklı bir kod giriniz.", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("code", out var codeProperty));
        Assert.Equal("C-100", codeProperty.GetString());

        var rawText = root.GetRawText();
        Assert.DoesNotContain("stackTrace", rawText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Müşteri kodu 'C-100' zaten kullanılıyor.", rawText);
    }

    [Fact]
    public async Task TryHandleAsync_WhenInvalidOperationException_ShouldReturnFalseAndNotWrite409Response()
    {
        using var provider = CreateServiceProvider();
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateHttpContext(provider);
        var exception = new InvalidOperationException("This is not a handled exception");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_WhenUnknownException_ShouldReturnFalseAndNotWriteResponseBody()
    {
        using var provider = CreateServiceProvider();
        var handler = new GlobalExceptionHandler();
        var httpContext = CreateHttpContext(provider);
        var exception = new NotSupportedException("Unexpected failure");

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }
}
