namespace FiasReportGenerator.Models;

/// <summary>Адресный объект из файла AS_ADDR_OBJ.</summary>
public class AddressObject : IComparable<AddressObject>
{
    /// <summary>Идентификатор записи.</summary>
    public int Id { get; set; }

    /// <summary>GUID адресного объекта.</summary>
    public required Guid ObjectId { get; set; }

    /// <summary>Тип операции (10 - добавление, 20 - изменение, 30 - удаление).</summary>
    public int OperTypeId { get; set; }

    /// <summary>GUID родительского объекта.</summary>
    public Guid? ParentObjectId { get; set; }

    /// <summary>Уровень адресного объекта.</summary>
    public required int Level { get; set; }

    /// <summary>Полное наименование типа объекта.</summary>
    public required string ObjectTypeName { get; set; }

    /// <summary>Наименование объекта.</summary>
    public required string Name { get; set; }

    /// <summary>Краткое наименование типа объекта.</summary>
    public required string TypeName { get; set; }

    /// <summary>Признак действующего адресного объекта.</summary>
    public bool IsActive { get; set; }

    /// <summary>Дата начала действия записи.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Дата окончания действия записи.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Дата последнего обновления записи.</summary>
    public DateTime UpdateDate { get; set; }

    /// <summary>Полное отображаемое наименование объекта.</summary>
    public string FullName => $"{ObjectTypeName} {Name}";

    public override string ToString()
    {
        return $"{FullName} (Level {Level}, Active: {IsActive})";
    }

    public int CompareTo(AddressObject? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}