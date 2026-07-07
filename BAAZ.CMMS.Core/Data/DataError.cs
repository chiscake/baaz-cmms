namespace BAAZ.CMMS.Core.Data;

public enum DataErrorCode
{
    Unauthorized,
    NotFound,
    Validation,
    Network,
    Unknown,
}

public sealed class DataError
{
    public DataErrorCode Code { get; }

    /// <summary>Ключ локализации для отображения пользователю.</summary>
    public string MessageKey { get; }

    /// <summary>Оригинальное сообщение ошибки от PostgREST/SDK (для отладки).</summary>
    public string? Detail { get; }

    public DataError(DataErrorCode code, string messageKey, string? detail = null)
    {
        Code = code;
        MessageKey = messageKey;
        Detail = detail;
    }

    public static DataError Network(string? detail = null) =>
        new(DataErrorCode.Network, "DataError_Network", detail);

    public static DataError Unauthorized(string? detail = null) =>
        new(DataErrorCode.Unauthorized, "DataError_Unauthorized", detail);

    public static DataError Validation(string messageKey, string? detail = null) =>
        new(DataErrorCode.Validation, messageKey, detail);

    public static DataError Unknown(string? detail = null) =>
        new(DataErrorCode.Unknown, "DataError_Unknown", detail);

    public static DataError NotFound(string messageKey, string? detail = null) =>
        new(DataErrorCode.NotFound, messageKey, detail);
}
