# Platform starter sample

This sample intentionally does not enable durable eventing by default. A real
application that owns an EF Core context can opt in separately:

```csharp
builder.Services.AddPlatformEventingEfCore<ApplicationDbContext>();
builder.Services.AddSingleton<IDurableEventPublisher, ApplicationEventPublisher>();
builder.Services.AddPlatformDurableOutboxDispatcher("sample-api-1");
```

The application must call `ConfigurePlatformEventing` from its own
`DbContext.OnModelCreating`, own the migrations for the outbox/inbox tables,
and implement the transport publisher. No product event types, transport
credentials, or migrations belong in this sample.
