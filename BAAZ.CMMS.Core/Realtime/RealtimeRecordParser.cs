using System;

using BAAZ.CMMS.Core.Data.Models;
using BAAZ.CMMS.Core.Models.TmsIssuance;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Supabase.Postgrest.Models;
using Supabase.Realtime.PostgresChanges;

namespace BAAZ.CMMS.Core.Realtime;

/// <summary>
/// Разбор Realtime UPDATE, когда <see cref="PostgresChangesResponse.Model{TModel}"/> не гидратирует строку целиком.
/// </summary>
internal static class RealtimeRecordParser
{
    public static TmsToolRequisitionLinkModel? TryParseTmsToolLink(PostgresChangesResponse change)
    {
        var model = SafeModel<TmsToolRequisitionLinkModel>(change);
        if (model is { Id: var modelId } && modelId != Guid.Empty
            && !string.IsNullOrWhiteSpace(model.LastKnownStatus))
        {
            return model;
        }

        return TryParseFromJson(change, model);
    }

    private static TModel? SafeModel<TModel>(PostgresChangesResponse change)
        where TModel : BaseModel, new()
    {
        try
        {
            return change.Model<TModel>();
        }
        catch
        {
            return null;
        }
    }

    private static TmsToolRequisitionLinkModel? TryParseFromJson(
        PostgresChangesResponse change,
        TmsToolRequisitionLinkModel? partial)
    {
        var recordObject = TryReadRecordObject(change);
        if (recordObject is null)
            return partial is { Id: var partialOnlyId } && partialOnlyId != Guid.Empty ? partial : null;

        try
        {
            var id = ReadGuid(recordObject, "id");
            if (id == Guid.Empty && partial is { Id: var fallbackId })
                id = fallbackId;

            if (id == Guid.Empty)
                return null;

            var status = recordObject["last_known_status"]?.ToString();
            var tmsRequisitionId = ReadGuid(recordObject, "tms_requisition_id");

            return new TmsToolRequisitionLinkModel
            {
                Id = id,
                LastKnownStatus = string.IsNullOrWhiteSpace(status)
                    ? partial?.LastKnownStatus ?? TmsRequisitionStatuses.New
                    : status,
                CmmsRequestId = ReadGuidNullable(recordObject, "cmms_request_id") ?? partial?.CmmsRequestId,
                CmmsScheduleId = ReadGuidNullable(recordObject, "cmms_schedule_id") ?? partial?.CmmsScheduleId,
                TmsRequisitionId = tmsRequisitionId != Guid.Empty
                    ? tmsRequisitionId
                    : partial?.TmsRequisitionId ?? Guid.Empty,
            };
        }
        catch
        {
            return partial is { Id: var partialId } && partialId != Guid.Empty ? partial : null;
        }
    }

    private static Guid ReadGuid(JToken record, string propertyName)
    {
        var text = record[propertyName]?.ToString();
        return Guid.TryParse(text, out var id) ? id : Guid.Empty;
    }

    private static Guid? ReadGuidNullable(JToken record, string propertyName)
    {
        var text = record[propertyName]?.ToString();
        return Guid.TryParse(text, out var id) ? id : null;
    }

    private static JObject? TryReadRecordObject(PostgresChangesResponse change)
    {
        if (change.Payload is null)
            return null;

        try
        {
            var root = JObject.Parse(JsonConvert.SerializeObject(change.Payload));
            var record = root["Data"]?["Record"] ?? root["data"]?["record"];
            if (record is not JObject recordObject)
                return null;

            return recordObject;
        }
        catch
        {
            return null;
        }
    }
}
