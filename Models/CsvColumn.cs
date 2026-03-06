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
    /// Bu kolon kod üretimine dahil mi? (false ise atlanır)
    /// </summary>
    public bool IsIncluded { get; set; } = true;

    /// <summary>
    /// Liste modunda: sadece benzersiz değerler üretilsin mi?
    /// </summary>
    public bool IsUniqueList { get; set; }

    /// <summary>
    /// Liste modunda: sıralama yönü.
    /// </summary>
    public SortOrder ListSortOrder { get; set; } = SortOrder.None;

    /// <summary>
    /// Liste modunda: koleksiyon tipi (List veya Array).
    /// </summary>
    public GroupCollectionType ListCollectionType { get; set; } = GroupCollectionType.List;

    /// <summary>
    /// Kullanıcının belirlediği enum tipi adı.
    /// Boşsa otomatik olarak PropertyName + "Type" kullanılır.
    /// </summary>
    public string EnumName { get; set; } = string.Empty;

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
