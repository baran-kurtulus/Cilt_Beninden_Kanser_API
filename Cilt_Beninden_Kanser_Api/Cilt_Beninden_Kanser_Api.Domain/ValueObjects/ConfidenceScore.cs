using Cilt_Beninden_Kanser_Api.Domain.Exceptions;

namespace Cilt_Beninden_Kanser_Api.Domain.ValueObjects;

public readonly struct ConfidenceScore
{
    public double Value { get; }

    public ConfidenceScore(double value)
    {
        if (value is < 0 or > 1)
            throw new DomainException("Confidence skoru 0 ile 1 arasında olmalıdır.");

        Value = value;
    }

    public static implicit operator double(ConfidenceScore score) => score.Value;
}
