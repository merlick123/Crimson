# BillingDemo

Example Crimson project.

Commands:

```bash
crimson build examples/BillingDemo/Billing.crimsonproj
```

Generated output appears under:

```txt
src/Generated
src/User
```

The example also includes a small runnable .NET app that consumes the generated output:

```bash
crimson build examples/BillingDemo/Billing.crimsonproj
dotnet run --project examples/BillingDemo/app/BillingDemo.App.csproj
```
