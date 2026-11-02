using Itmo.ObjectOrientedProgramming.Lab5.Application;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Abstractions;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Accounts;
using Itmo.ObjectOrientedProgramming.Lab5.Application.Application.UseCases.Operations;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace ATM.UnitTests;

public sealed class ApplicationTests
{
    private const string Owner = "keycloak-user-sub";

    [Fact]
    public async Task BalanceUsesVerifiedSubject()
    {
        Account own = DomainTests.NewAccount(70);
        IAccountRepository accounts = Substitute.For<IAccountRepository>();
        accounts.GetByOwnerSubjectAsync(Owner, Arg.Any<CancellationToken>()).Returns(own);
        var result = await new GetBalanceService(accounts).GetAsync(Owner);
        Assert.Equal(AccountAccessStatus.Success, result.Status);
        Assert.Equal(70, result.Value!.Balance);
        await accounts.Received(1).GetByOwnerSubjectAsync(Owner, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnotherSubjectCannotReadBalance()
    {
        IAccountRepository accounts = Substitute.For<IAccountRepository>();
        var result = await new GetBalanceService(accounts).GetAsync("intruder");
        Assert.Equal(AccountAccessStatus.AccountNotFound, result.Status);
    }

    [Fact]
    public async Task AccountCreationRequiresOwnerAndHashesPin()
    {
        IAccountRepository accounts = Substitute.For<IAccountRepository>();
        Account? saved = null;
        accounts.CreateAsync(Arg.Any<Account>(), Arg.Any<Transaction?>(), Arg.Any<CancellationToken>())
            .Returns(call => { saved = call.ArgAt<Account>(0); return true; });
        var service = new CreateAccountService(accounts, TimeProvider.System);
        Assert.Equal(AccountCreationStatus.InvalidInput,
            (await service.CreateAsync("number", "1234", 0, "")).Status);
        Assert.Equal(AccountCreationStatus.Success,
            (await service.CreateAsync("number", "1234", 0, Owner)).Status);
        Assert.True(saved!.IsOwnedBy(Owner));
        Assert.True(saved.CheckPin("1234"));
        Assert.DoesNotContain("1234", saved.PinCode.StoredHash);
    }

    [Fact]
    public async Task DepositCreatesOneMatchingHistoryEntry()
    {
        Account account = DomainTests.NewAccount(100);
        var (banking, transaction, entries) = Banking(account);
        MoneyOperationResult result = await banking.DepositAsync(Owner, 25.50m, "deposit-1");
        Assert.Equal(MoneyOperationStatus.Success, result.Status);
        Assert.Equal(125.50m, account.Balance.Amount);
        Assert.Single(entries);
        Assert.Equal(result.OperationId, entries[0].OperationId);
        transaction.Received(1).SaveAccount(account);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.001)]
    public async Task InvalidDepositDoesNotReachTransaction(decimal amount)
    {
        Account account = DomainTests.NewAccount(100);
        var (banking, transaction, entries) = Banking(account);

        MoneyOperationResult result = await banking.DepositAsync(Owner, amount, "invalid-deposit");

        Assert.Equal(MoneyOperationStatus.InvalidAmount, result.Status);
        Assert.Equal(100, account.Balance.Amount);
        Assert.Empty(entries);
        await transaction.DidNotReceiveWithAnyArgs().ClaimAsync(default!, default, default!);
    }

    [Fact]
    public async Task ChangedOwnerIsRejectedAfterAccountIsLocked()
    {
        Account account = DomainTests.NewAccount(100);
        var (banking, transaction, entries) = Banking(account);
        account.SetOwner("another-user-sub");

        MoneyOperationResult result = await banking.DepositAsync(Owner, 10, "changed-owner");

        Assert.Equal(MoneyOperationStatus.AccessDenied, result.Status);
        Assert.Equal(100, account.Balance.Amount);
        Assert.Empty(entries);
        await transaction.DidNotReceiveWithAnyArgs().ClaimAsync(default!, default, default!);
    }

    [Fact]
    public async Task WithdrawalNeedsPinAndDoesNotChangeBalanceOnFailure()
    {
        Account account = DomainTests.NewAccount(100);
        var (banking, transaction, entries) = Banking(account);
        MoneyOperationResult wrong = await banking.WithdrawAsync(Owner, 40, "wrong-pin", "wrong");
        Assert.Equal(MoneyOperationStatus.InvalidPin, wrong.Status);
        Assert.Equal(100, account.Balance.Amount);
        Assert.Empty(entries);
        await transaction.Received(1).ReleaseClaimAsync("wrong-pin", Arg.Any<CancellationToken>());
        MoneyOperationResult insufficient = await banking.WithdrawAsync(Owner, 800, "insufficient", "1234");
        Assert.Equal(MoneyOperationStatus.InsufficientFunds, insufficient.Status);
        Assert.Equal(100, account.Balance.Amount);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task TransferRequiresOwnerAndPinAndSharesOperationId()
    {
        Account source = DomainTests.NewAccount(100, "source");
        Account target = DomainTests.NewAccount(10, "target");
        var (banking, transaction, entries) = Banking(source);
        transaction.FindByNumberAsync("target", Arg.Any<CancellationToken>()).Returns(target);
        transaction.LockAccountAsync(target.Key, Arg.Any<CancellationToken>()).Returns(target);
        Assert.Equal(MoneyOperationStatus.InvalidPin,
            (await banking.TransferAsync(Owner, "target", 30, "wrong", "bad")).Status);
        Assert.Empty(entries);
        MoneyOperationResult result = await banking.TransferAsync(Owner, "target", 30, "good", "1234");
        Assert.Equal(MoneyOperationStatus.Success, result.Status);
        Assert.Equal(70, source.Balance.Amount);
        Assert.Equal(40, target.Balance.Amount);
        Assert.Equal(2, entries.Count);
        Assert.All(entries, entry => Assert.Equal(result.OperationId, entry.OperationId));
    }

    [Fact]
    public async Task InsufficientTransferDoesNotChangeEitherAccountOrHistory()
    {
        Account source = DomainTests.NewAccount(100, "source");
        Account target = DomainTests.NewAccount(10, "target");
        var (banking, transaction, entries) = Banking(source);
        transaction.FindByNumberAsync("target", Arg.Any<CancellationToken>()).Returns(target);
        transaction.LockAccountAsync(target.Key, Arg.Any<CancellationToken>()).Returns(target);

        MoneyOperationResult result = await banking.TransferAsync(Owner, "target", 101, "no-funds", "1234");

        Assert.Equal(MoneyOperationStatus.InsufficientFunds, result.Status);
        Assert.Equal(100, source.Balance.Amount);
        Assert.Equal(10, target.Balance.Amount);
        Assert.Empty(entries);
    }

    [Fact]
    public void PinLocksAfterFiveFailuresAndUnlocksAfterFifteenMinutes()
    {
        Account account = DomainTests.NewAccount(0);
        DateTime now = DateTime.UtcNow;
        for (int i = 0; i < 4; i++)
            Assert.Equal(PinVerificationResult.Invalid, account.VerifyPin("bad", now));
        Assert.Equal(PinVerificationResult.Locked, account.VerifyPin("bad", now));
        Assert.Equal(PinVerificationResult.Locked, account.VerifyPin("1234", now.AddMinutes(1)));
        Assert.Equal(PinVerificationResult.Success, account.VerifyPin("1234", now.AddMinutes(16)));
        Assert.Equal(0, account.FailedPinAttempts);
        for (int i = 0; i < 4; i++)
            Assert.Equal(PinVerificationResult.Invalid, account.VerifyPin("bad", now.AddMinutes(16)));
        Assert.Equal(PinVerificationResult.Locked, account.VerifyPin("bad", now.AddMinutes(16)));
    }

    private static (BankingService Banking, IFinancialTransaction Transaction, List<Transaction> Entries)
        Banking(Account account)
    {
        IAccountRepository accounts = Substitute.For<IAccountRepository>();
        accounts.GetByOwnerSubjectAsync(Owner, Arg.Any<CancellationToken>()).Returns(account);
        IFinancialTransaction transaction = Substitute.For<IFinancialTransaction>();
        transaction.ClaimAsync(Arg.Any<string>(), account.Key, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new IdempotencyClaim(true, null, null));
        transaction.LockAccountAsync(account.Key, Arg.Any<CancellationToken>()).Returns(account);
        var entries = new List<Transaction>();
        transaction.When(x => x.AddTransaction(Arg.Any<Transaction>()))
            .Do(call => entries.Add(call.Arg<Transaction>()));
        IFinancialTransactionRunner runner = Substitute.For<IFinancialTransactionRunner>();
        runner.ExecuteAsync(Arg.Any<Func<IFinancialTransaction, CancellationToken, Task<MoneyOperationResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<IFinancialTransaction, CancellationToken,
                Task<MoneyOperationResult>>>(0)(transaction, CancellationToken.None));
        return (new BankingService(accounts, runner, TimeProvider.System), transaction, entries);
    }
}
