using System;

namespace Billing.Contracts;

public partial class CustomerService
{
    public CustomerService()
    {
    }

    /// <summary>Finds a customer record.</summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <returns>The serialized customer payload.</returns>
    public virtual string GetCustomer(string customerId)
    {
        throw new NotImplementedException();
    }

    /// <summary>Updates a customer display name.</summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="newName">The new display name.</param>
    public virtual void UpdateName(string customerId, string newName)
    {
        throw new NotImplementedException();
    }

}
