# Platform billing

`Platform.Billing.Contracts` is the provider-neutral boundary. It defines checkout, portal,
subscription lookup, cancellation, webhook normalization, plan catalogs, entitlement storage,
and usage contracts. Provider IDs and plan mappings remain application-owned through
`ProviderPlanReference`; no Stripe, Lemon Squeezy, invoice, tax, wallet, or merchant-of-record
dependency is included.

`Platform.Billing` contains the small orchestration layer:

```csharp
var result = await new BillingEventOrchestrator(processedEventStore, projector, clock)
    .ProcessAsync(new ProviderEventEnvelope(normalizedProviderEvent));
```

The orchestrator marks `(provider, event id)` before projection. Duplicates are acknowledged as
`Duplicate`; projectors reject events older than the latest event they have applied. Hosts own
the durable processed-event and entitlement schemas by implementing the public interfaces.

`FeatureAccessEvaluator` combines the existing normalized `FeatureCheck` decision with an
`IUsageMeter`, returning stable reasons, current usage, and limits. Expired periods return
`FeatureCheckReason.Expired`; canceled or otherwise inactive subscriptions return
`NotSubscribed`.

`Platform.Billing.Testing` provides deterministic in-memory entitlement, usage, and provider
fakes. Persistent stores and concrete provider adapters are intentionally deferred to separate
changes.
