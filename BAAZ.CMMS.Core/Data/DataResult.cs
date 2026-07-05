namespace BAAZ.CMMS.Core.Data;

/// <summary>Результат операции с данными — значение или ошибка.</summary>
public sealed class DataResult<T>
{
    public T? Value { get; }
    public DataError? Error { get; }
    public bool IsSuccess => Error is null;

    private DataResult(T? value, DataError? error)
    {
        Value = value;
        Error = error;
    }

    public static DataResult<T> Ok(T value) => new(value, null);
    public static DataResult<T> Fail(DataError error) => new(default, error);
}

/// <summary>Результат операции без возвращаемого значения.</summary>
public sealed class DataResult
{
    public DataError? Error { get; }
    public bool IsSuccess => Error is null;

    private DataResult(DataError? error) => Error = error;

    public static DataResult Ok() => new(null);
    public static DataResult Fail(DataError error) => new(error);
}
