namespace NexusERP.Application.Features.Customers.Exceptions;

public sealed class DuplicateCustomerCodeException : Exception
{
    public DuplicateCustomerCodeException(string code)
        : base($"Müşteri kodu '{code}' zaten kullanılıyor.")
    {
        Code = code;
    }

    public string Code { get; }
}
