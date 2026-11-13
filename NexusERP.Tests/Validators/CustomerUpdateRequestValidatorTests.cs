using FluentValidation.TestHelper;
using NexusERP.Application.Features.Customers.DTOs;
using NexusERP.Application.Features.Customers.Validators;

namespace NexusERP.Tests.Validators;

public class CustomerUpdateRequestValidatorTests
{
    private readonly CustomerUpdateRequestValidator _validator = new();

    [Fact]
    public void Code_WhenEmpty_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = string.Empty, Name = "Test Customer" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Code_WhenWhitespace_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "   ", Name = "Test Customer" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Code_WhenLongerThan32Characters_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = new string('A', 33), Name = "Test Customer" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Name_WhenEmpty_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = string.Empty };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_WhenWhitespace_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = "   " };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_WhenLongerThan200Characters_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = new string('N', 201) };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Email_WhenInvalidFormat_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = "Test Customer", Email = "invalid-email" };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_WhenNull_ShouldPassValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = "Test Customer", Email = null };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_WhenLongerThan254Characters_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest
        {
            Code = "C001",
            Name = "Test Customer",
            Email = new string('a', 250) + "@example.com"
        };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Phone_WhenLongerThan32Characters_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = "Test Customer", Phone = new string('1', 33) };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void TaxNumber_WhenLongerThan32Characters_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = "Test Customer", TaxNumber = new string('1', 33) };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.TaxNumber);
    }

    [Fact]
    public void Address_WhenLongerThan500Characters_ShouldFailValidation()
    {
        var model = new CustomerUpdateRequest { Code = "C001", Name = "Test Customer", Address = new string('A', 501) };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void ValidCustomer_ShouldPassValidation()
    {
        var model = new CustomerUpdateRequest
        {
            Code = "C001",
            Name = "Test Customer",
            Email = "test@example.com",
            Phone = "5551234567",
            TaxNumber = "1234567890",
            Address = "Istanbul"
        };

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
