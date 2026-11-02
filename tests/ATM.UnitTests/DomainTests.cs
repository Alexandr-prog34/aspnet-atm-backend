using Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;
using Xunit;

namespace ATM.UnitTests;

public sealed class DomainTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1.001)]
    public void DepositRejectsInvalidAmount(decimal amount)
    {
        Account account = NewAccount(100);
        Assert.IsType<DepositResult.InvalidAmount>(account.Deposit(amount));
        Assert.Equal(100, account.Balance.Amount);
    }

    [Fact]
    public void DepositChangesBalance()
    {
        Account account = NewAccount(100);
        Assert.IsType<DepositResult.Success>(account.Deposit(25.50m));
        Assert.Equal(125.50m, account.Balance.Amount);
    }

    [Fact]
    public void WithdrawDistinguishesInvalidAmountAndInsufficientFunds()
    {
        Account account = NewAccount(100);
        Assert.IsType<WithdrawResult.InvalidAmount>(account.Withdraw(-1));
        Assert.IsType<WithdrawResult.InvalidAmount>(account.Withdraw(1.001m));
        Assert.IsType<WithdrawResult.InsufficientFunds>(account.Withdraw(101));
        Assert.Equal(100, account.Balance.Amount);
        Assert.IsType<WithdrawResult.Success>(account.Withdraw(40));
        Assert.Equal(60, account.Balance.Amount);
    }

    [Fact]
    public void PinIsSaltedAndVerified()
    {
        Account first = NewAccount(0);
        Account second = NewAccount(0);
        Assert.NotEqual(first.PinCode.StoredHash, second.PinCode.StoredHash);
        Assert.DoesNotContain("1234", first.PinCode.StoredHash);
        Assert.True(first.CheckPin("1234"));
        Assert.False(first.CheckPin("4321"));
    }

    public static Account NewAccount(decimal initialBalance, string number = "40817810000000000001") =>
        new(Guid.NewGuid(), new AccountNumber(number), new PinCode("1234"),
            new Money(initialBalance), DateTime.UtcNow, "keycloak-user-sub");
}
