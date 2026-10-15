# ATM Backend: Stage 1 stabilization design

## Scope and outcome

Stabilize the existing four-project application before adding PostgreSQL. The stage is complete when the .NET 10 solution builds, the existing account and session flows work, a user session can access only its own account, invalid monetary amounts have distinct typed results, and unit tests cover the corrected behavior. The existing in-memory adapters remain temporary infrastructure for this stage.

PostgreSQL transactions, concurrency control, idempotency, transfers, session expiry, PIN hashing, configured administrator credentials, and REST endpoint redesign belong to later stages of the supplied roadmap. Stage 1 must not claim their guarantees.

## Approaches considered

1. **Targeted stabilization (chosen):** retain the entities, use cases, repository ports, controllers, and in-memory adapters where possible; fix their contracts and defects in place. This gives a small reviewable change and a testable baseline.
2. **Replace the application with a new template:** faster to obtain a modern project skeleton but loses the current architecture and behavior, contrary to the requirement to reuse components.
3. **Implement all roadmap stages together:** avoids transitional contracts but makes regressions and financial correctness much harder to verify. The supplied roadmap calls for incremental delivery.

## Build and project structure

- Add a solution containing the existing Domain, Application, Infrastructure, and WebApi projects and a new unit-test project.
- Target `net10.0` consistently; enable nullable reference types and implicit usings consistently. Keep Domain free of ASP.NET Core, EF Core, and infrastructure dependencies.
- Declare explicit, compatible NuGet versions or central package management so restore works from the repository alone. Remove the commented duplicate project definition from the Infrastructure project file.
- Keep one `AddApplication` registration method. `Program.cs` remains the composition root and registers each module once.
- Update only the README statements needed to describe the stage's actual build/run and authentication behavior; the full deployment guide follows later.

## Account ownership and HTTP flow

- `CheckSessionService` already returns the account ID for a valid user session. Use that verified ID as the sole account selector for balance, deposit, withdrawal, and history use cases.
- Add an account lookup by ID to the account repository port and in-memory adapter. Money use cases accept a session key and resolve the account through the session service before touching balances or history. This enforces ownership inside Application, including callers other than HTTP controllers.
- The operations controller passes the session key and request amount to the use case. It stops reading `X-Account-Number`; the existing routes stay in place during Stage 1. A forged or missing account header therefore cannot change the target account.
- Keep admin session validation for account creation. Existing user and admin session responses remain usable. HTTP response mapping distinguishes missing/invalid sessions, wrong session type, missing account, invalid amount, and insufficient funds.
- Pass `HttpContext.RequestAborted` through all touched asynchronous controller and use-case calls.

## Monetary behavior

- `Money` represents nonnegative RUB values with at most two meaningful decimal places. Values that contain fractional kopecks are rejected, without rounding. A trailing zero beyond two displayed decimal places is acceptable when the numeric value is exactly representable in kopecks.
- Deposit and withdrawal input must be strictly positive. Invalid input returns a typed business result, not an exception. Withdrawal with a valid amount above the current balance returns a separate insufficient-funds result.
- No failed operation changes the balance or writes transaction history. A successful operation writes exactly one matching history entry through the current adapters. The in-memory adapters provide no crash, multi-process, or transaction guarantee; those guarantees are Stage 2 and Stage 3 work.
- Preserve the existing account creation flow, rejecting an invalid initial balance. Recording a nonzero initial balance as a transaction is part of the persistent accounting model in Stage 2; do not present the existing history as complete before that change.

## Tests and verification

- Add xUnit and NSubstitute unit tests that run without PostgreSQL or HTTP: amount rules, account deposit and withdrawal, PIN check, successful and failed use cases, session validation, and cross-account access attempts.
- Test the Application boundary with substitute repositories: a forged account number must have no effect because the use case has no account-number input; the account selected must match the session's account ID. Verify that invalid and insufficient-funds outcomes save neither account nor transaction.
- Restore, build, and run tests for the solution. The current host has no `dotnet` command and no running Docker daemon. Verification needs an available .NET 10 SDK or a working SDK container; if neither is available, report the exact unverified checks rather than calling the stage complete.

## Error handling and later migration

Stage 1 retains the current controller structure and typed application results. The Stage 4 authentication and ProblemDetails work can replace controller-level session parsing without changing the rule that Application derives the target account from a verified session. Stage 2 can replace the in-memory repository adapters and introduce an atomic financial operation boundary while retaining the Domain rules.
