namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

public readonly struct Money : IEquatable<Money>
{
    public Money(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        Amount = amount;
    }

    public static Money Zero => new(0m);

    public decimal Amount { get; }

    public Money Add(Money other)
    {
        return new(Amount + other.Amount);
    }

    public Money Subtract(Money other)
    {
        if (other.Amount > Amount)
            throw new InvalidOperationException("Resulting amount cannot be negative.");

        return new Money(Amount - other.Amount);
    }

    public bool Equals(Money other)
    {
        return Amount == other.Amount;
    }

    public override bool Equals(object? obj)
    {
        return obj is Money other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Amount.GetHashCode();
    }

    public static bool operator ==(Money left, Money right) => left.Equals(right);

    public static bool operator !=(Money left, Money right) => !left.Equals(right);
}
