using Billing.Contracts;

var service = new CustomerService
{
    DisplayName = "Crimson Billing Demo",
};

Console.WriteLine("Billing demo is running.");
Console.WriteLine($"Service name: {service.DisplayName}");
Console.WriteLine($"Service id: {service.ServiceId}");
Console.WriteLine($"Sample state: {CustomerState.Active}");
