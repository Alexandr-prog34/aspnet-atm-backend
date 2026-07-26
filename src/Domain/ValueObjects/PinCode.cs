namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

public sealed record PinCode
{
    public PinCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PIN cannot be empty.", nameof(value));

        Value = value;
    }

    public string Value { get; }

    public bool Matches(string rawPin) => Value == rawPin;
}
