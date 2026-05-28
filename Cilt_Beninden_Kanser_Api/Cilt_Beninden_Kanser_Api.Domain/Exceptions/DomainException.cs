namespace Cilt_Beninden_Kanser_Api.Domain.Exceptions;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
