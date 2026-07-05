using BAAZ.CMMS.Core.Models;

namespace BAAZ.CMMS.Core.Services;

/// <summary>Session-scoped кэш дерева локаций с версионированием.</summary>
public interface ILocationTreeCache
{
    LocationTreeSnapshot Current { get; }

    /// <summary>Загружает снимок при первом обращении; иначе возвращает кэш.</summary>
    Task<LocationTreeSnapshot> EnsureLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>Принудительная перезагрузка из API и инкремент Version.</summary>
    Task<LocationTreeSnapshot> InvalidateAndReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>Обновляет снимок из уже загруженного списка (без повторного API).</summary>
    LocationTreeSnapshot LoadFromItems(IReadOnlyList<LocationListItem> items);
}
