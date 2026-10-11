namespace DomoNow.Parking.Domain.Common;

public class Result
{
    private readonly Error? _error;

    protected Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new(null);
    public static Result Failure(Error error) => new(error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, null);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, Error? error) : base(error) => _value = value;

    public TValue Value => IsSuccess
        ? _value! // Non-null by construction: successful results are only created with a value.
        : throw new InvalidOperationException("A failed result has no value.");

    public static implicit operator Result<TValue>(TValue value) => new(value, null);
    public static implicit operator Result<TValue>(Error error) => new(default, error);
}
