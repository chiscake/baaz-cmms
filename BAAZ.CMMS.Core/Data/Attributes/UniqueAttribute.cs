namespace BAAZ.CMMS.Core.Data.Attributes;

/// <summary>Маркер UNIQUE-ограничения колонки в PostgreSQL (для UI CrudWorkbench).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UniqueAttribute : Attribute;
