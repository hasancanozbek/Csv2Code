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
}
