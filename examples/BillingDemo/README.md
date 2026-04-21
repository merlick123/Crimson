# BillingDemo

BillingDemo is the canonical Crimson sample for the first release. It shows:

- `.idl` contracts generating C# interface and class projections
- user-owned implementations living under `src/User`
- generated plumbing under `src/Generated`
- automatic `crimson build` integration from the consuming C# project
- swappability through `ICustomerService`

Run it from the repo root:

```bash
dotnet run --project examples/BillingDemo/app/BillingDemo.App.csproj
```

The example app will:

1. run `crimson build` automatically
2. compile the generated and user-owned C# projection
3. execute two interchangeable implementations of `ICustomerService`

Generated output appears under:

```txt
src/Generated
src/User
```

If you want to run the Crimson build step explicitly yourself:

```bash
crimson validate examples/BillingDemo/Billing.crimsonproj
crimson build examples/BillingDemo/Billing.crimsonproj
```
