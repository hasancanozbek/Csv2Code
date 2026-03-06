namespace Csv2Code.Models;

/// <summary>
/// Kod üretim modu.
/// </summary>
public enum GenerationMode
{
    /// <summary>Her CSV satırı bir obje/sınıf instance'ına dönüşür.</summary>
    Object,
    /// <summary>Her CSV kolonu ayrı bir liste/array olarak üretilir.</summary>
    List
}

/// <summary>
/// Liste modunda sıralama yönü.
/// </summary>
public enum SortOrder
{
    /// <summary>Sıralama yok — CSV'deki sıra korunur.</summary>
    None,
    /// <summary>Küçükten büyüğe (A-Z, 0-9).</summary>
    Ascending,
    /// <summary>Büyükten küçüğe (Z-A, 9-0).</summary>
    Descending
}
