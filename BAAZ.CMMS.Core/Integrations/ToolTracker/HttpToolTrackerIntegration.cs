using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BAAZ.CMMS.Core.Contracts.Integrations;

namespace BAAZ.CMMS.Core.Integrations.ToolTracker;

public sealed class HttpToolTrackerIntegration(
    string baseUrl,
    string integrationSecret,
    string supabaseAnonKey) : IToolTrackerIntegration
{
    public Task<bool> HasActiveTaskForTechnicianAsync(Guid technicianId, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task NotifyWorkReportCreatedAsync(
        Guid workReportId,
        Guid? requestId,
        Guid? scheduleId,
        decimal actualDurationHours,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task NotifyRequestStatusChangedAsync(
        Guid requestId,
        string newStatus,
        DateTimeOffset changedAt,
        ToolTrackerRequestStatusContext? context = null,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var inventoryId = context?.InventoryId ?? Guid.Empty;
        var payload = new
        {
            schema_version = 1,
            event_name = "request.status_changed",
            request_id = requestId,
            request_number = context?.RequestNumber ?? string.Empty,
            tool_id = inventoryId,
            inventory_id = inventoryId,
            inventory_kind = context?.InventoryKind ?? "tool",
            previous_status = context?.PreviousStatus ?? string.Empty,
            new_status = newStatus,
            changed_at = changedAt,
        };

        using var response = await client.PostAsJsonAsync(
            "/api/v1/integration/cmms/repair-request-status",
            payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower },
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient()
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", integrationSecret);
        client.DefaultRequestHeaders.Add("apikey", supabaseAnonKey);
        return client;
    }
}
