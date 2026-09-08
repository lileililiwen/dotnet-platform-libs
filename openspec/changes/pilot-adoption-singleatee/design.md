## Dependencies

This change depends on the completed core, entitlement-contracts, and testing-toolkit changes. The ASP.NET package is optional and should only be adopted if it matches the pilot host without a broad web rewrite.

## Adoption Steps

First add the platform contracts and testing package using local references. Implement adapters from the platform entitlement/usage contracts to the existing `singleatee` services. Keep current EF entities, migrations, cache behavior, and product-specific limit rules. Run the existing test suite plus adapter tests.

Then pack the platform packages and repeat the pilot using package references. Compare build output, test behavior, dependency graph, and changed lines. Document any contract changes as a new OpenSpec change rather than modifying the platform silently.

## Success Criteria

The pilot compiles and passes its existing tests, feature-limit behavior is unchanged, no product entities move into the platform repository, and the consuming application can pin a platform package version independently.

## Verification

Run the application's targeted unit/integration tests, package restore using the private/local feed, dependency inspection, and strict OpenSpec validation in both repositories.
