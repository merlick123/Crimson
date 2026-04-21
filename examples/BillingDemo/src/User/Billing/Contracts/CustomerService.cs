using System;
using System.Collections.Generic;

namespace Billing.Contracts;

public partial class CustomerService
{
    private readonly Dictionary<string, CustomerRecord> _customers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CUST-1001"] = new("Ada Lovelace", CustomerState.Active),
        ["CUST-1002"] = new("Grace Hopper", CustomerState.Suspended),
    };

    public CustomerService()
    {
        ServiceId = 101;
        ConnectionString = "billing://demo/local";
        DisplayName = "  Crimson Billing Demo  ";
    }

    /// <summary>Finds a customer record.</summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <returns>The serialized customer payload.</returns>
    public virtual string GetCustomer(string customerId)
    {
        if (!_customers.TryGetValue(customerId, out var customer))
        {
            return $$"""{"id":"{{customerId}}","found":false}""";
        }

        return $$"""{"id":"{{customerId}}","name":"{{customer.Name}}","state":"{{customer.State}}","service":"{{DisplayName}}"}""";
    }

    /// <summary>Updates a customer display name.</summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="newName">The new display name.</param>
    public virtual void UpdateName(string customerId, string newName)
    {
        if (!_customers.TryGetValue(customerId, out var customer))
        {
            throw new ArgumentOutOfRangeException(nameof(customerId), customerId, "Unknown customer id.");
        }

        var cleaned = newName.Trim();
        if (cleaned.Length == 0)
        {
            throw new ArgumentException("Customer names cannot be blank.", nameof(newName));
        }

        _customers[customerId] = customer with { Name = cleaned };
    }

    partial void OnDisplayNameSetting(ref string value)
    {
        value = value.Trim();
        if (value.Length == 0)
        {
            throw new ArgumentException("DisplayName cannot be blank.");
        }
    }

    private sealed record CustomerRecord(string Name, CustomerState State);
}
