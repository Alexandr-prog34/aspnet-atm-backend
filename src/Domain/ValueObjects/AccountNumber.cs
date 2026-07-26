namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

public sealed class AccountNumber
{
    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Account number cannot be empty.", nameof(value));

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
