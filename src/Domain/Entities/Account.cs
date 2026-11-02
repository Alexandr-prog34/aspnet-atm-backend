using Itmo.ObjectOrientedProgramming.Lab5.Domain.ResultType;
using Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.Entities;

public sealed class Account
{
    public Account(Guid key, AccountNumber number, PinCode pinCode, Money initialBalance, DateTime createdAtUtc,
        string? ownerSubject = null, int failedPinAttempts = 0, DateTime? pinLockedUntilUtc = null)
    {
        Key = key;
        Number = number ?? throw new ArgumentNullException(nameof(number));
        PinCode = pinCode ?? throw new ArgumentNullException(nameof(pinCode));
        Balance = initialBalance;
        CreatedAtUtc = createdAtUtc;
        OwnerSubject = ownerSubject;
        FailedPinAttempts = failedPinAttempts;
        PinLockedUntilUtc = pinLockedUntilUtc;
    }

    public Guid Key { get; }

    public AccountNumber Number { get; }

    public Money Balance { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public PinCode PinCode { get; }

    public string? OwnerSubject { get; private set; }

    public int FailedPinAttempts { get; private set; }

    public DateTime? PinLockedUntilUtc { get; private set; }

    public bool IsOwnedBy(string subject) =>
        !string.IsNullOrWhiteSpace(subject) && string.Equals(OwnerSubject, subject, StringComparison.Ordinal);

    public void SetOwner(string subject) => OwnerSubject = subject;

    public PinVerificationResult VerifyPin(string? rawPin, DateTime nowUtc)
    {
        if (PinLockedUntilUtc is { } lockedUntil)
        {
            if (lockedUntil > nowUtc) return PinVerificationResult.Locked;
            PinLockedUntilUtc = null;
            FailedPinAttempts = 0;
        }
        if (PinCode.Matches(rawPin ?? string.Empty))
        {
            FailedPinAttempts = 0;
            PinLockedUntilUtc = null;
            return PinVerificationResult.Success;
        }

        FailedPinAttempts++;
        if (FailedPinAttempts >= 5)
        {
            PinLockedUntilUtc = nowUtc.AddMinutes(15);
            return PinVerificationResult.Locked;
        }

        return PinVerificationResult.Invalid;
    }

    public bool CheckPin(string rawPin)
    {
        return PinCode.Matches(rawPin);
    }

    public WithdrawResult Withdraw(decimal amount)
    {
        if (!Money.IsPositiveAmount(amount))
            return new WithdrawResult.InvalidAmount();

        if (amount > Balance.Amount)
            return new WithdrawResult.InsufficientFunds();

        Balance = Balance.Subtract(new Money(amount));
        return new WithdrawResult.Success(Balance.Amount);
    }

    public DepositResult Deposit(decimal amount)
    {
        if (!Money.IsPositiveAmount(amount) || amount > Money.Maximum - Balance.Amount)
            return new DepositResult.InvalidAmount();

        Balance = Balance.Add(new Money(amount));
        return new DepositResult.Success(Balance.Amount);
    }
}

public enum PinVerificationResult { Success, Invalid, Locked }
