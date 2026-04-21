using Billing.Contracts;

RunScenario("Generated Crimson service", new CustomerService());
RunScenario("Preview replacement service", new PreviewCustomerService());

static void RunScenario(string label, ICustomerService service)
{
    var request = new Request
    {
        CustomerId = "CUST-1001",
    };

    Console.WriteLine($"== {label} ==");
    Console.WriteLine($"Service name: {service.DisplayName}");
    Console.WriteLine($"Service id: {service.ServiceId}");
    Console.WriteLine($"Lookup payload: {service.GetCustomer(request.CustomerId)}");
    service.UpdateName(request.CustomerId, "Katherine Johnson");
    Console.WriteLine($"Updated payload: {service.GetCustomer(request.CustomerId)}");
    Console.WriteLine();
}
