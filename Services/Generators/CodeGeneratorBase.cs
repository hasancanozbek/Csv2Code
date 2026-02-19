using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services.Generators;

/// <summary>
/// Tüm dil generator'ları için ortak abstract base class.
/// </summary>
public abstract class CodeGeneratorBase
{
    /// <summary>
    /// Desteklenen C# tiplerine karşılık gelen hedef dil tipleri.
    /// </summary>
    public abstract string[] SupportedTypes { get; }

    /// <summary>
    /// Oluşturulan dosyanın uzantısı (ör: ".cs", ".h", ".py").
    /// </summary>
    public abstract string FileExtension { get; }

    /// <summary>
    /// Dil adı (UI'da gösterilecek).
    /// </summary>
    public abstract string LanguageName { get; }

    /// <summary>
    /// Kod üretir.
    /// </summary>
    public abstract string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1, int lookupKeyColumnIndex = -1);

    /// <summary>
    /// Mevcut dosyaya ekleme yapar.
    /// </summary>
    public abstract string AppendToExistingFile(string existingContent, CsvFileData newData, string className);

    #region Shared Utilities

    public static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Item";

        var sb = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
        }

        var result = sb.ToString();
        if (result.Length > 0 && char.IsDigit(result[0]))
            result = "_" + result;

        return string.IsNullOrEmpty(result) ? "Item" : result;
    }

    public static string GetEnumTypeName(CsvColumn column)
    {
        if (!string.IsNullOrWhiteSpace(column.EnumName))
            return SanitizeIdentifier(column.EnumName);

        return SanitizeIdentifier(column.PropertyName) + "Type";
    }

    /// <summary>
    /// Enum adı default mu (boş veya PropertyName + "Type")?
    /// Default ise enum tanımı üretilir, farklıysa mevcut enum kabul edilir.
    /// </summary>
    public static bool IsDefaultEnumName(CsvColumn column)
    {
        if (string.IsNullOrWhiteSpace(column.EnumName))
            return true;

        var defaultName = SanitizeIdentifier(column.PropertyName) + "Type";
        return string.Equals(column.EnumName.Trim(), defaultName, StringComparison.OrdinalIgnoreCase);
    }

    public static string SanitizeEnumMember(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "None";

        if (char.IsDigit(value[0]))
            return "Value_" + SanitizeIdentifier(value);

        return SanitizeIdentifier(value);
    }

    public static List<string> GetUniqueColumnValues(CsvFileData data, int columnIndex)
    {
        var values = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in data.Rows)
        {
            var val = columnIndex < row.Length ? row[columnIndex].Trim() : "";
            if (!string.IsNullOrWhiteSpace(val) && seen.Add(val))
            {
                values.Add(val);
            }
        }

        return values;
    }

    public static bool IsNullOrNa(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed)
               || trimmed.Equals("NA", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("N/A", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("NaN", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("nan", StringComparison.Ordinal)
               || trimmed == "-"
               || trimmed == "";
    }

    public static string NormalizeDecimalSeparator(string value)
    {
        bool hasComma = value.Contains(',');
        bool hasDot = value.Contains('.');

        if (hasComma && hasDot)
        {
            int lastComma = value.LastIndexOf(',');
            int lastDot = value.LastIndexOf('.');

            if (lastComma > lastDot)
                return value.Replace(".", "").Replace(',', '.');
            else
                return value.Replace(",", "");
        }

        if (hasComma)
            return value.Replace(',', '.');

        return value;
    }

    public static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    #endregion
}
