namespace Itmo.ObjectOrientedProgramming.Lab5.Domain.ValueObjects;

public readonly record struct Money
{
    public const decimal Maximum = 9999999999999999.99m;

    public Money(decimal amount)
    {
        if (amount < 0 || amount > Maximum || !HasKopeckPrecision(amount))
            throw new ArgumentOutOfRangeException(nameof(amount), "RUB amount must be nonnegative and have kopeck precision.");

        Amount = amount;
    }

    public decimal Amount { get; }
    public static Money Zero => new(0m);

    public static bool IsPositiveAmount(decimal amount) =>
        amount > 0 && amount <= Maximum && HasKopeckPrecision(amount);

    public static bool HasKopeckPrecision(decimal amount) => decimal.Truncate(amount * 100m) == amount * 100m;

    public Money Add(Money other) => new(checked(Amount + other.Amount));
    public Money Subtract(Money other) => new(Amount - other.Amount);
}
