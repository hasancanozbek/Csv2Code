namespace Csv2Code.Models;

/// <summary>
/// Parse edilmiş bir CSV dosyasının tüm verisini tutar.
/// </summary>
public class CsvFileData
{
    /// <summary>
    /// CSV dosyasının adı (uzantısız).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// CSV dosyasının tam yolu.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Kolon tanımları.
    /// </summary>
    public List<CsvColumn> Columns { get; set; } = new();

    /// <summary>
    /// Satır verileri (ham string dizileri).
    /// </summary>
    public List<string[]> Rows { get; set; } = new();

    /// <summary>
    /// Algılanan veya belirlenen delimiter karakteri.
    /// </summary>
    public char Delimiter { get; set; } = ';';
}
