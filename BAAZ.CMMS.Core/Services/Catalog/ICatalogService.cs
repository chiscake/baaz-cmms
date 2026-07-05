using BAAZ.CMMS.Core.Services.Catalog;

namespace BAAZ.CMMS.Core.Services;

/// <summary>
/// Справочники: оборудование, подразделения, персонал ТОиР.
/// Покрывает UC-A1, UC-A3, UC-A4, UC-A6.
/// </summary>
public interface ICatalogService
    : IAssetCatalogService,
      ILocationCatalogService,
      ITechnicianCatalogService,
      IRepairDepartmentCatalogService
{
}
