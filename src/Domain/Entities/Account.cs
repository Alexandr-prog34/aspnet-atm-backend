using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

public sealed class Account
{
    public Account(Guid key, AccountNumber number, PinCode pinCode, Money initialBalance)
    {
        Key = key;
        Number = number ?? throw new ArgumentNullException(nameof(number));
        PinCode = pinCode ?? throw new ArgumentNullException(nameof(pinCode));
        Balance = initialBalance;
    }

    public Guid Key { get; }

    public AccountNumber Number { get; }

    public Money Balance { get; private set; }

    private PinCode PinCode { get; }

    public bool CheckPin(string rawPin)
    {
        return PinCode.Value == rawPin;
    }

    public WithdrawResult Withdraw(Money amount)
    {
        if (amount.Amount <= 0)
            return new WithdrawResult.InsufficientFunds();

        if (amount.Amount > Balance.Amount)
            return new WithdrawResult.InsufficientFunds();

        Balance = Balance.Subtract(amount);
        return new WithdrawResult.Success(Balance.Amount);
    }

    public DepositResult Deposit(Money amount)
    {
        if (amount.Amount <= 0)
            return new DepositResult.InvalidAmount();

        Balance = Balance.Add(amount);
        return new DepositResult.Success(Balance.Amount);
    }
}
