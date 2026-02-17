namespace Csv2Code.Models;

/// <summary>
/// CSV dosyasındaki bir kolonun metadata bilgisini tutar.
/// </summary>
public class CsvColumn
{
    /// <summary>
    /// CSV dosyasındaki orijinal kolon adı.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// C# property adı (kullanıcı tarafından değiştirilebilir).
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Seçilen C# tipi (varsayılan: string).
    /// </summary>
    public string CSharpType { get; set; } = "string";

    /// <summary>
    /// Kolon sırası (0-based index).
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Gruplama adı. Aynı grup adına sahip kolonlar birleştirilerek
    /// tek bir List/Array property oluşturur. Boşsa standalone property olur.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Grup koleksiyon tipi (None, List, Array).
    /// </summary>
    public GroupCollectionType CollectionType { get; set; } = GroupCollectionType.None;

    /// <summary>
    /// Bu kolon için unique değer dizisi üretilsin mi?
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Bu kolon standalone mı (gruplama yok)?
    /// </summary>
    public bool IsStandalone => string.IsNullOrWhiteSpace(GroupName);
}

/// <summary>
/// Grup koleksiyon tipi.
/// </summary>
public enum GroupCollectionType
{
    /// <summary>Standalone property — gruplama yok.</summary>
    None,
    /// <summary>List&lt;T&gt; olarak grupla.</summary>
    List,
    /// <summary>T[] olarak grupla.</summary>
    Array
}
