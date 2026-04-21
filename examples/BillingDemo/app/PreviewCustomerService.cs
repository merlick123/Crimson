using Billing.Contracts;

public sealed class PreviewCustomerService : ICustomerService
{
    public long ServiceId => 9001;

    public string DisplayName { get; set; } = "Preview Billing Service";

    public string GetCustomer(string customerId) =>
        $$"""{"id":"{{customerId}}","name":"Preview Customer","state":"{{CustomerState.Active}}","mode":"preview"}""";

    public void UpdateName(string customerId, string newName)
    {
        DisplayName = $"Preview Billing Service ({newName.Trim()})";
    }
}
