using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;

public sealed class BankingService(IAccountRepository accounts, IFinancialTransactionRunner runner, TimeProvider clock)
{
    public Task<MoneyOperationResult> DepositAsync(string subject, decimal amount, string? key,
        CancellationToken ct = default) => ExecuteAsync(subject, amount, key, "deposit", null, null, ct);

    public Task<MoneyOperationResult> WithdrawAsync(string subject, decimal amount, string? key, string? pin,
        CancellationToken ct = default) => ExecuteAsync(subject, amount, key, "withdrawal", null, pin, ct);

    public Task<MoneyOperationResult> TransferAsync(string subject, string targetAccountNumber,
        decimal amount, string? key, string? pin, CancellationToken ct = default) =>
        ExecuteAsync(subject, amount, key, "transfer", targetAccountNumber, pin, ct);

    private async Task<MoneyOperationResult> ExecuteAsync(string subject, decimal amount, string? key,
        string kind, string? targetNumber, string? pin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 200)
            return new(MoneyOperationStatus.InvalidIdempotencyKey);
        if (!Money.IsPositiveAmount(amount) || (kind == "transfer" && string.IsNullOrWhiteSpace(targetNumber)))
            return new(MoneyOperationStatus.InvalidAmount);

        Account? ownerAccount = await accounts.GetByOwnerSubjectAsync(subject, ct);
        if (ownerAccount is null) return new(MoneyOperationStatus.AccountNotFound);
        Guid sourceKey = ownerAccount.Key;
        string hash = HashRequest(kind, sourceKey, amount, targetNumber);

        return await runner.ExecuteAsync(async (transaction, token) =>
        {
            Account? target = null;
            if (kind == "transfer")
            {
                target = await transaction.FindByNumberAsync(targetNumber!, token);
                if (target is null) return new MoneyOperationResult(MoneyOperationStatus.AccountNotFound);
                if (target.Key == sourceKey) return new MoneyOperationResult(MoneyOperationStatus.SameAccount);
            }

            Guid[] keys = target is null ? [sourceKey] : [sourceKey, target.Key];
            Array.Sort(keys);
            var locked = new Dictionary<Guid, Account>();
            foreach (Guid accountKey in keys)
            {
                Account? account = await transaction.LockAccountAsync(accountKey, token);
                if (account is null) return new MoneyOperationResult(MoneyOperationStatus.AccountNotFound);
                locked[accountKey] = account;
            }

            Account source = locked[sourceKey];
            if (!source.IsOwnedBy(subject)) return new MoneyOperationResult(MoneyOperationStatus.AccessDenied);
            IdempotencyClaim claim = await transaction.ClaimAsync(key, sourceKey, hash, token);
            if (!claim.IsNew && claim.RequestHash != hash)
                return new MoneyOperationResult(MoneyOperationStatus.IdempotencyConflict);

            if (kind != "deposit")
            {
                PinVerificationResult check = source.VerifyPin(pin, clock.GetUtcNow().UtcDateTime);
                if (check != PinVerificationResult.Success)
                {
                    if (claim.IsNew) await transaction.ReleaseClaimAsync(key, token);
                    transaction.SaveAccount(source);
                    return new MoneyOperationResult(check == PinVerificationResult.Locked
                        ? MoneyOperationStatus.PinLocked : MoneyOperationStatus.InvalidPin);
                }

                transaction.SaveAccount(source);
            }

            if (!claim.IsNew)
                return claim.Result is not null
                    ? claim.Result
                    : new MoneyOperationResult(MoneyOperationStatus.IdempotencyConflict);

            MoneyOperationResult result = kind switch
            {
                "deposit" => ChangeBalance(transaction, source, amount, true),
                "withdrawal" => ChangeBalance(transaction, source, amount, false),
                _ => Transfer(transaction, source, locked[target!.Key], amount),
            };
            await transaction.CompleteAsync(key, result, token);
            return result;
        }, ct);
    }

    private MoneyOperationResult ChangeBalance(IFinancialTransaction transaction, Account account,
        decimal amount, bool deposit)
    {
        if (deposit)
        {
            if (account.Deposit(amount) is DepositResult.InvalidAmount)
                return new(MoneyOperationStatus.InvalidAmount);
        }
        else
        {
            WithdrawResult result = account.Withdraw(amount);
            if (result is WithdrawResult.InvalidAmount) return new(MoneyOperationStatus.InvalidAmount);
            if (result is WithdrawResult.InsufficientFunds) return new(MoneyOperationStatus.InsufficientFunds);
        }

        Guid operationId = Guid.NewGuid();
        transaction.SaveAccount(account);
        transaction.AddTransaction(NewTransaction(operationId, account,
            deposit ? new TransactionType.Deposit() : new TransactionType.Withdrawal(), amount));
        return MoneyOperationResult.Success(account.Balance.Amount, operationId);
    }

    private MoneyOperationResult Transfer(IFinancialTransaction transaction, Account source, Account target,
        decimal amount)
    {
        if (amount > Money.Maximum - target.Balance.Amount)
            return new(MoneyOperationStatus.InvalidAmount);
        WithdrawResult debit = source.Withdraw(amount);
        if (debit is WithdrawResult.InsufficientFunds) return new(MoneyOperationStatus.InsufficientFunds);
        if (debit is WithdrawResult.InvalidAmount) return new(MoneyOperationStatus.InvalidAmount);
        if (target.Deposit(amount) is DepositResult.InvalidAmount)
            throw new InvalidOperationException("Validated target balance was rejected.");

        Guid operationId = Guid.NewGuid();
        transaction.SaveAccount(source);
        transaction.SaveAccount(target);
        transaction.AddTransaction(NewTransaction(operationId, source, new TransactionType.TransferOut(), amount));
        transaction.AddTransaction(NewTransaction(operationId, target, new TransactionType.TransferIn(), amount));
        return MoneyOperationResult.Success(source.Balance.Amount, operationId);
    }

    private Transaction NewTransaction(Guid operationId, Account account, TransactionType type, decimal amount) =>
        new(Guid.NewGuid(), operationId, account.Key, account.Number, type,
            new Money(amount), clock.GetUtcNow().UtcDateTime, account.Balance);

    private static string HashRequest(string kind, Guid accountKey, decimal amount, string? targetNumber)
    {
        string payload = $"{kind}|{accountKey:N}|{amount.ToString("G29", CultureInfo.InvariantCulture)}|{targetNumber}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
