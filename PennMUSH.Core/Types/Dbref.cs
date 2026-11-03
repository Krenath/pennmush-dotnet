namespace PennMUSH.Core.Types;

using System;

public readonly struct Dbref : IEquatable<Dbref>, IComparable<Dbref>
{
    public static readonly Dbref Nothing = new Dbref(-1);
    public static readonly Dbref Ambiguous = new Dbref(-2);
    public static readonly Dbref Home = new Dbref(-3);
    public static readonly Dbref AnyOwner = new Dbref(-2);

    public readonly int Value;

    public bool IsValid => Value >= 0;
    public bool IsSpecial => Value < 0;

    public Dbref(int value)
    {
        Value = value;
    }

    public static Dbref Parse(string s)
    {
        if (TryParse(s, out var result))
            return result;
        throw new FormatException("Invalid Dbref format.");
    }

    public static bool TryParse(string? s, out Dbref result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }

        if (s.StartsWith('#'))
            s = s.Substring(1);

        if (int.TryParse(s, out int value))
        {
            result = new Dbref(value);
            return true;
        }

        result = default;
        return false;
    }

    public bool Equals(Dbref other) => Value == other.Value;
    public int CompareTo(Dbref other) => Value.CompareTo(other.Value);
    public override bool Equals(object? obj) => obj is Dbref other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(Dbref left, Dbref right) => left.Equals(right);
    public static bool operator !=(Dbref left, Dbref right) => !left.Equals(right);
    public override string ToString() => "#" + Value;
}