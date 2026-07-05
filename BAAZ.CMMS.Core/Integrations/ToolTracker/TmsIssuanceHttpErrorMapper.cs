using System.Net;
using System.Text.Json;
using BAAZ.CMMS.Core.Data;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

/// <summary>
/// Человекочитаемые ошибки вызовов TMS ISS-API из CMMS (Live).
/// </summary>
internal static class TmsIssuanceHttpErrorMapper
{
    public static DataError FromConnectionError(string apiId, Exception exception)
    {
        var hint = exception.Message.Contains("55321", StringComparison.Ordinal)
            ? "TmsIntegration_Error_WrongSupabaseUrl"
            : "TmsIntegration_Error_Unreachable";

        return new DataError(
            DataErrorCode.Network,
            hint,
            $"{apiId}: {exception.Message}");
    }

    public static DataError FromHttpResponse(string apiId, HttpStatusCode statusCode, string? responseBody)
    {
        var detailText = TryExtractDetail(responseBody);
        var status = (int)statusCode;

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new DataError(
                DataErrorCode.Unauthorized,
                "TmsIntegration_Error_Unauthorized",
                Format(apiId, status, detailText,
                    "Проверьте TMS Integration Secret в Settings CMMS (= TMS_INTEGRATION_SECRET в TMS .env).")),

            HttpStatusCode.Forbidden when ContainsIgnoreCase(detailText, "apikey") => new DataError(
                DataErrorCode.Unauthorized,
                "TmsIntegration_Error_ForbiddenApiKey",
                Format(apiId, status, detailText,
                    "TMS проверяет заголовок apikey против service_role TMS. Для local dev очистите TMS_INTEGRATION_SECRET в TMS .env или задайте в CMMS apikey = SUPABASE_KEY TMS.")),

            HttpStatusCode.Forbidden => new DataError(
                DataErrorCode.Unauthorized,
                "TmsIntegration_Error_ForbiddenSecret",
                Format(apiId, status, detailText,
                    "Секрет интеграции не совпадает: Settings → TMS Integration Secret и TMS_INTEGRATION_SECRET.")),

            HttpStatusCode.NotFound => new DataError(
                DataErrorCode.Network,
                "TmsIntegration_Error_WrongUrl",
                Format(apiId, status, detailText,
                    "TMS Base URL должен указывать на FastAPI (http://127.0.0.1:8000), не на Supabase (:54321 / :55321).")),

            HttpStatusCode.InternalServerError => new DataError(
                DataErrorCode.Unknown,
                "TmsIntegration_Error_Server",
                Format(apiId, status, detailText,
                    "Ошибка на стороне TMS: проверьте uvicorn, supabase start (TMS :55321), seed и логи FastAPI.")),

            _ => new DataError(
                DataErrorCode.Unknown,
                "TmsIntegration_Error_Http",
                Format(apiId, status, detailText, null)),
        };
    }

    private static string Format(string apiId, int status, string? serverDetail, string? hint)
    {
        var parts = new List<string> { $"{apiId}: HTTP {status}" };
        if (!string.IsNullOrWhiteSpace(serverDetail))
            parts.Add(serverDetail);
        if (!string.IsNullOrWhiteSpace(hint))
            parts.Add(hint);
        return string.Join(" — ", parts);
    }

    private static string? TryExtractDetail(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        var trimmed = responseBody.Trim();
        if (trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.TryGetProperty("detail", out var detail))
                {
                    return detail.ValueKind switch
                    {
                        JsonValueKind.String => detail.GetString(),
                        JsonValueKind.Array => string.Join("; ",
                            detail.EnumerateArray().Select(e => e.ToString())),
                        _ => detail.ToString(),
                    };
                }
            }
            catch (JsonException)
            {
                // fall through
            }
        }

        return trimmed.Length > 240 ? trimmed[..240] + "…" : trimmed;
    }

    private static bool ContainsIgnoreCase(string? text, string fragment)
        => !string.IsNullOrWhiteSpace(text)
           && text.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
