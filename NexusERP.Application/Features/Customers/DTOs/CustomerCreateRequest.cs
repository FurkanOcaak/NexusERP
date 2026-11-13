namespace NexusERP.Application.Features.Customers.DTOs;

public class CustomerCreateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? TaxNumber { get; set; }

    public string? Address { get; set; }
}
