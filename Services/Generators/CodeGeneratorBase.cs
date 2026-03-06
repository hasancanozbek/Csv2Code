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
    /// Obje modu: Kod üretir.
    /// </summary>
    public abstract string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1);

    /// <summary>
    /// Liste modu: Her kolonu ayrı bir liste/array olarak üretir.
    /// </summary>
    public abstract string GenerateListCode(CsvFileData data, string variablePrefix, string namespaceName);

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

    /// <summary>
    /// Hücre değerinin bir array pattern olup olmadığını kontrol eder.
    /// {1;2;3}, {1.5,2.3}, [1;2;3], [a,b,c] gibi formatları algılar.
    /// </summary>
    public static bool IsArrayValue(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length < 3) return false;

        return (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            || (trimmed.StartsWith('[') && trimmed.EndsWith(']'));
    }

    /// <summary>
    /// Bir kolondaki değerlerden herhangi birinin array olup olmadığını kontrol eder.
    /// </summary>
    public static bool ColumnHasArrayValues(CsvFileData data, CsvColumn column)
    {
        foreach (var row in data.Rows)
        {
            var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex].Trim() : "";
            if (IsArrayValue(raw))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Array ayracını otomatik belirler:
    /// - Eğer iç metinde ';' varsa → ayraç ';' dir (Avrupa/Excel standardı).
    /// - Eğer ';' yoksa ve ',' varsa → ',' nin decimal mi array ayracı mı olduğunu belirle:
    ///   • Eğer iç metinde '.' varsa → '.' ondalık, ',' array ayracı.
    ///   • Eğer '.' yoksa → ',' nin context'ine bak: virgülden sonra 3+ basamak veya
    ///     birden fazla ',' varsa array ayracı kabul et.
    /// </summary>
    public static char DetectArraySeparator(string arrayValue)
    {
        var inner = ExtractArrayInner(arrayValue);

        // ';' varsa kesinlikle array ayracı
        if (inner.Contains(';'))
            return ';';

        // ';' yok, ',' var mı?
        if (!inner.Contains(','))
            return ';'; // Tek elemanlı array, varsayılan ';'

        // ',' var — ondalık mı array ayracı mı?
        // Eğer '.' da varsa → '.' ondalık ayracı, ',' array ayracı
        if (inner.Contains('.'))
            return ',';

        // '.' yok, sadece ',' var.
        // Virgülle split edip parçaları incele:
        var parts = inner.Split(',');

        // 3+ parça varsa kesinlikle array ayracı
        if (parts.Length >= 3)
            return ',';

        // 2 parça var — ondalık olabilir (1,5 gibi) veya array olabilir (1,2 gibi)
        // Her iki parça da sayısal mı kontrol et:
        // Eğer ikinci parçanın uzunluğu 1-2 ise ondalık olma ihtimali yüksek.
        // Ama kesin karar vermek zor, bu yüzden daha geniş context'e bak.
        if (parts.Length == 2)
        {
            var secondPart = parts[1].Trim();
            // İkinci parça 1-2 haneli sayı ve ilk parça da sayı ise → ondalık
            if (secondPart.Length <= 2
                && double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _)
                && double.TryParse(secondPart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                // Tek bir ondalık sayı olarak yorumla — array DEĞİL
                // Ama burada array ayracı sorsak bile, aslında dıştaki {} zaten
                // array olduğunu gösteriyor. {3,14} → 3.14 mü yoksa [3, 14] mi?
                // Eğer {} veya [] varsa array olarak yorumla.
                return ',';
            }
            return ',';
        }

        return ',';
    }

    /// <summary>
    /// Bir kolonun tüm array değerleri üzerinden array ayracını belirler.
    /// İlk array değerindeki ayracı temel alır.
    /// </summary>
    public static char DetectColumnArraySeparator(CsvFileData data, CsvColumn column)
    {
        foreach (var row in data.Rows)
        {
            var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex].Trim() : "";
            if (IsArrayValue(raw))
                return DetectArraySeparator(raw);
        }
        return ';'; // varsayılan
    }

    /// <summary>
    /// Array değerinin iç kısmını (parantez/bracket hariç) döndürür.
    /// </summary>
    public static string ExtractArrayInner(string arrayValue)
    {
        var trimmed = arrayValue.Trim();
        if (trimmed.Length < 2) return trimmed;

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            return trimmed[1..^1].Trim();
        }

        return trimmed;
    }

    /// <summary>
    /// Bir array değerini elemanlarına ayırır.
    /// </summary>
    public static List<string> ParseArrayElements(string arrayValue)
    {
        if (!IsArrayValue(arrayValue))
            return new List<string> { arrayValue.Trim() };

        var separator = DetectArraySeparator(arrayValue);
        var inner = ExtractArrayInner(arrayValue);

        if (string.IsNullOrWhiteSpace(inner))
            return new List<string>();

        return inner.Split(separator)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .ToList();
    }

    /// <summary>
    /// Formatlanmış değerler listesini numerik-farkındalıklı sıralar.
    /// Değerlerden sayısal kısmı çıkararak karşılaştırma yapar;
    /// sayısal çıkarılamazsa string olarak sıralar.
    /// </summary>
    public static void SortValues(List<string> values, Models.SortOrder sortOrder)
    {
        if (sortOrder == Models.SortOrder.None || values.Count <= 1)
            return;

        values.Sort(NumericAwareComparer);

        if (sortOrder == Models.SortOrder.Descending)
            values.Reverse();
    }

    /// <summary>
    /// Formatlanmış literal değerleri numerik-farkındalıklı karşılaştırır.
    /// "3456.3d" vs "78d" → sayısal olarak karşılaştırılır.
    /// "\"abc\"" vs "\"xyz\"" → string olarak karşılaştırılır.
    /// </summary>
    private static int NumericAwareComparer(string a, string b)
    {
        var numA = ExtractNumericValue(a);
        var numB = ExtractNumericValue(b);

        if (numA.HasValue && numB.HasValue)
            return numA.Value.CompareTo(numB.Value);

        return StringComparer.Ordinal.Compare(a, b);
    }

    /// <summary>
    /// Formatlanmış bir literal değerden sayısal kısmı çıkarır.
    /// Örn: "3456.3d" → 3456.3, "78f" → 78, "123" → 123,
    /// "True" → null, "\"abc\"" → null
    /// </summary>
    private static double? ExtractNumericValue(string formattedValue)
    {
        if (string.IsNullOrWhiteSpace(formattedValue))
            return null;

        var trimmed = formattedValue.Trim();

        // String literal ise ("..." veya '...') sayısal değil
        if ((trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            || (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
            return null;

        // null, None, true, false, True, False gibi keyword'ler
        if (trimmed is "null" or "None" or "true" or "false" or "True" or "False")
            return null;

        // Sondaki tip suffix'lerini temizle (d, f, m, L, 0.0f vb.)
        var cleaned = trimmed.TrimEnd('d', 'f', 'm', 'L', 'F', 'D', 'M');

        if (string.IsNullOrEmpty(cleaned))
            return null;

        if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double result))
            return result;

        return null;
    }

    #endregion
}
