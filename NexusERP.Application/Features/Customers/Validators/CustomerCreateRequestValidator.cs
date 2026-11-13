using FluentValidation;
using NexusERP.Application.Features.Customers.DTOs;

namespace NexusERP.Application.Features.Customers.Validators;

public class CustomerCreateRequestValidator : AbstractValidator<CustomerCreateRequest>
{
    public CustomerCreateRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Müşteri kodu boş olamaz.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Müşteri kodu boşluklardan oluşamaz.")
            .MaximumLength(32)
            .WithMessage("Müşteri kodu en fazla 32 karakter olabilir.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Müşteri adı boş olamaz.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Müşteri adı boşluklardan oluşamaz.")
            .MaximumLength(200)
            .WithMessage("Müşteri adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Email)
            .MaximumLength(254)
            .WithMessage("E-posta en fazla 254 karakter olabilir.")
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.Phone)
            .MaximumLength(32)
            .WithMessage("Telefon en fazla 32 karakter olabilir.");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(32)
            .WithMessage("Vergi numarası en fazla 32 karakter olabilir.");

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .WithMessage("Adres en fazla 500 karakter olabilir.");
    }
}
