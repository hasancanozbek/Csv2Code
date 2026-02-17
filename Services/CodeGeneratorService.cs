using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services;

/// <summary>
/// CSV verisinden C# sınıf kodu üreten servis.
/// Kolon gruplama ve satır gruplama destekli.
/// </summary>
public static class CodeGeneratorService
{
    /// <summary>
    /// Desteklenen C# tipleri listesi.
    /// </summary>
    public static readonly string[] SupportedTypes =
    {
        "string",
        "int",
        "int?",
        "long",
        "long?",
        "float",
        "float?",
        "double",
        "double?",
        "decimal",
        "decimal?",
        "bool",
        "bool?",
        "char",
        "char?",
        "byte",
        "byte?",
        "short",
        "short?",
        "enum",
    };

    /// <summary>
    /// Desteklenen koleksiyon tipleri.
    /// </summary>
    public static readonly string[] CollectionTypes = { "None", "List", "Array" };

    /// <summary>
    /// CSV verisinden C# kodu üretir.
    /// </summary>
    /// <param name="data">Parse edilmiş CSV verisi</param>
    /// <param name="className">Sınıf adı</param>
    /// <param name="namespaceName">Namespace adı</param>
    /// <param name="groupByColumnIndex">Satır gruplama kolonu index'i (-1 = gruplama yok)</param>
    public static string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(className))
            className = "GeneratedClass";
        if (string.IsNullOrWhiteSpace(namespaceName))
            namespaceName = "Generated";

        className = SanitizeIdentifier(className);
        namespaceName = SanitizeIdentifier(namespaceName);

        var sb = new StringBuilder();

        // Namespace
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        // Enum tanımlarını class'tan önce üret
        GenerateEnumDefinitions(sb, data);

        // Class
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        // Properties oluştur
        GenerateProperties(sb, data);

        sb.AppendLine();

        // Static data
        if (groupByColumnIndex >= 0 && groupByColumnIndex < data.Columns.Count)
        {
            GenerateGroupedData(sb, data, className, groupByColumnIndex);
        }
        else
        {
            GenerateFlatData(sb, data, className);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    #region Enum Generation

    /// <summary>
    /// Enum olarak işaretlenen kolonlar için enum tanımları üretir.
    /// </summary>
    private static void GenerateEnumDefinitions(StringBuilder sb, CsvFileData data)
    {
        var enumColumns = data.Columns.Where(c => c.CSharpType == "enum" && c.IsStandalone).ToList();
        if (enumColumns.Count == 0) return;

        foreach (var column in enumColumns)
        {
            var enumName = GetEnumTypeName(column);
            var uniqueValues = GetUniqueColumnValues(data, column.ColumnIndex);

            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");

            for (int i = 0; i < uniqueValues.Count; i++)
            {
                var rawValue = uniqueValues[i];
                var enumMember = SanitizeEnumMember(rawValue);
                var separator = i < uniqueValues.Count - 1 ? "," : "";

                // Sayısal değerlerse açık değer ata
                if (int.TryParse(rawValue, out int numericValue))
                {
                    sb.AppendLine($"        {enumMember} = {numericValue}{separator}");
                }
                else
                {
                    sb.AppendLine($"        {enumMember}{separator}");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Bir kolonun tüm satırlardaki unique değerlerini sıralı döndürür.
    /// </summary>
    private static List<string> GetUniqueColumnValues(CsvFileData data, int columnIndex)
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

    /// <summary>
    /// Kolon adından enum tip adı üretir: PropertyName + "Type"
    /// </summary>
    private static string GetEnumTypeName(CsvColumn column)
    {
        return SanitizeIdentifier(column.PropertyName) + "Type";
    }

    /// <summary>
    /// Ham değeri geçerli bir enum üye adına dönüştürür.
    /// </summary>
    private static string SanitizeEnumMember(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "None";

        // Sayısal değerlere prefix ekle
        if (char.IsDigit(value[0]))
            return "Value_" + SanitizeIdentifier(value);

        return SanitizeIdentifier(value);
    }

    #endregion

    #region Property Generation

    /// <summary>
    /// Property'leri üretir. Gruplanan kolonlar tek bir List/Array property olur.
    /// </summary>
    private static void GenerateProperties(StringBuilder sb, CsvFileData data)
    {
        sb.AppendLine("        #region Properties");
        sb.AppendLine();

        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in data.Columns)
        {
            if (column.IsStandalone)
            {
                // Standalone property
                var propName = SanitizeIdentifier(column.PropertyName);
                var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : column.CSharpType;
                sb.AppendLine($"        public {typeName} {propName} {{ get; set; }}");
            }
            else
            {
                // Gruplanmış — sadece ilk karşılaşmada property oluştur
                if (processedGroups.Add(column.GroupName))
                {
                    var groupPropName = SanitizeIdentifier(column.GroupName);
                    var elementType = column.CSharpType;

                    switch (column.CollectionType)
                    {
                        case GroupCollectionType.List:
                            sb.AppendLine($"        public List<{elementType}> {groupPropName} {{ get; set; }}");
                            break;
                        case GroupCollectionType.Array:
                            sb.AppendLine($"        public {elementType}[] {groupPropName} {{ get; set; }}");
                            break;
                        default:
                            sb.AppendLine($"        public List<{elementType}> {groupPropName} {{ get; set; }}");
                            break;
                    }
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    #endregion

    #region Flat Data Generation

    /// <summary>
    /// Düz liste olarak veri üretir: List&lt;ClassName&gt;
    /// </summary>
    private static void GenerateFlatData(StringBuilder sb, CsvFileData data, string className)
    {
        sb.AppendLine($"        #region Data");
        sb.AppendLine();
        sb.AppendLine($"        public static readonly List<{className}> Items = new()");
        sb.AppendLine("        {");

        for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
        {
            var row = data.Rows[rowIndex];
            GenerateObjectInitializer(sb, data, row, className, "            ", rowIndex < data.Rows.Count - 1);
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    #endregion

    #region Grouped Data Generation (Row Grouping)

    /// <summary>
    /// Satır gruplama ile veri üretir: List&lt;List&lt;ClassName&gt;&gt;
    /// </summary>
    private static void GenerateGroupedData(StringBuilder sb, CsvFileData data, string className, int groupByColumnIndex)
    {
        sb.AppendLine($"        #region Data");
        sb.AppendLine();
        sb.AppendLine($"        public static readonly List<List<{className}>> GroupedItems = new()");
        sb.AppendLine("        {");

        // Satırları grup değerine göre grupla (sırayı koru)
        var groups = new List<(string Key, List<string[]> Rows)>();
        var groupMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in data.Rows)
        {
            var key = groupByColumnIndex < row.Length ? row[groupByColumnIndex].Trim() : "";
            if (!groupMap.TryGetValue(key, out int idx))
            {
                idx = groups.Count;
                groupMap[key] = idx;
                groups.Add((key, new List<string[]>()));
            }
            groups[idx].Rows.Add(row);
        }

        for (int gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            sb.AppendLine($"            // Group: {group.Key}");
            sb.AppendLine("            new List<" + className + ">()");
            sb.AppendLine("            {");

            for (int ri = 0; ri < group.Rows.Count; ri++)
            {
                GenerateObjectInitializer(sb, data, group.Rows[ri], className, "                ", ri < group.Rows.Count - 1);
            }

            var groupSep = gi < groups.Count - 1 ? "," : "";
            sb.AppendLine($"            }}{groupSep}");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    #endregion

    #region Object Initializer

    /// <summary>
    /// Tek bir obje initializer'ı üretir. Kolon gruplama mantığını da içerir.
    /// </summary>
    private static void GenerateObjectInitializer(StringBuilder sb, CsvFileData data, string[] row, string className, string indent, bool hasTrailingComma)
    {
        sb.AppendLine($"{indent}new {className}");
        sb.AppendLine($"{indent}{{");

        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var propertyLines = new List<string>();

        foreach (var column in data.Columns)
        {
            if (column.IsStandalone)
            {
                // Standalone property
                var propName = SanitizeIdentifier(column.PropertyName);
                var rawValue = column.ColumnIndex < row.Length ? row[column.ColumnIndex] : string.Empty;

                if (column.CSharpType == "enum")
                {
                    // Enum: EnumTypeName.MemberName formatı
                    var enumTypeName = GetEnumTypeName(column);
                    var enumMember = SanitizeEnumMember(rawValue.Trim());
                    propertyLines.Add($"{indent}    {propName} = {enumTypeName}.{enumMember}");
                }
                else
                {
                    var formattedValue = FormatValue(rawValue, column.CSharpType);
                    propertyLines.Add($"{indent}    {propName} = {formattedValue}");
                }
            }
            else
            {
                // Gruplanmış — tüm grup üyelerini topla
                if (processedGroups.Add(column.GroupName))
                {
                    var groupPropName = SanitizeIdentifier(column.GroupName);
                    var groupColumns = data.Columns
                        .Where(c => c.GroupName.Equals(column.GroupName, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.ColumnIndex)
                        .ToList();

                    var values = groupColumns.Select(gc =>
                    {
                        var rawValue = gc.ColumnIndex < row.Length ? row[gc.ColumnIndex] : string.Empty;
                        return FormatValue(rawValue, gc.CSharpType);
                    }).ToList();

                    var valuesStr = string.Join(", ", values);

                    switch (column.CollectionType)
                    {
                        case GroupCollectionType.Array:
                            propertyLines.Add($"{indent}    {groupPropName} = new {column.CSharpType}[] {{ {valuesStr} }}");
                            break;
                        case GroupCollectionType.List:
                        default:
                            propertyLines.Add($"{indent}    {groupPropName} = new List<{column.CSharpType}> {{ {valuesStr} }}");
                            break;
                    }
                }
            }
        }

        // Virgül ekle
        for (int i = 0; i < propertyLines.Count; i++)
        {
            var sep = i < propertyLines.Count - 1 ? "," : "";
            sb.AppendLine($"{propertyLines[i]}{sep}");
        }

        var trailingSep = hasTrailingComma ? "," : "";
        sb.AppendLine($"{indent}}}{trailingSep}");
    }

    #endregion

    #region Value Formatting

    /// <summary>
    /// Ham CSV değerini C# kod değerine dönüştürür.
    /// </summary>
    public static string FormatValue(string rawValue, string csharpType)
    {
        var trimmed = rawValue.Trim();
        bool isNullOrNa = string.IsNullOrWhiteSpace(trimmed)
                          || trimmed.Equals("NA", StringComparison.OrdinalIgnoreCase)
                          || trimmed.Equals("N/A", StringComparison.OrdinalIgnoreCase)
                          || trimmed.Equals("NULL", StringComparison.OrdinalIgnoreCase);

        // Nullable tipler
        if (csharpType.EndsWith("?") && isNullOrNa)
            return "null";

        switch (csharpType)
        {
            case "string":
                return $"\"{EscapeString(trimmed)}\"";

            case "int":
            case "int?":
                return ConvertToNumericLiteral(trimmed, "");

            case "long":
            case "long?":
                return ConvertToNumericLiteral(trimmed, "L");

            case "float":
            case "float?":
                return ConvertToDecimalLiteral(trimmed, "f");

            case "double":
            case "double?":
                return ConvertToDecimalLiteral(trimmed, "d");

            case "decimal":
            case "decimal?":
                return ConvertToDecimalLiteral(trimmed, "m");

            case "bool":
            case "bool?":
                return ConvertToBoolLiteral(trimmed);

            case "char":
            case "char?":
                if (isNullOrNa && csharpType.EndsWith("?"))
                    return "null";
                return trimmed.Length > 0 ? $"'{EscapeChar(trimmed[0])}'" : "'\\0'";

            case "byte":
            case "byte?":
                return ConvertToNumericLiteral(trimmed, "");

            case "short":
            case "short?":
                return ConvertToNumericLiteral(trimmed, "");

            default:
                return $"\"{EscapeString(trimmed)}\"";
        }
    }

    private static string ConvertToDecimalLiteral(string value, string suffix)
    {
        var normalized = value.Replace(',', '.');
        return $"{normalized}{suffix}";
    }

    private static string ConvertToNumericLiteral(string value, string suffix)
    {
        var normalized = value.Replace(',', '.');
        if (normalized.Contains('.'))
        {
            var dotIndex = normalized.IndexOf('.');
            normalized = normalized[..dotIndex];
        }
        return string.IsNullOrEmpty(suffix) ? normalized : $"{normalized}{suffix}";
    }

    private static string ConvertToBoolLiteral(string value)
    {
        var lower = value.ToLowerInvariant();
        return lower switch
        {
            "1" or "true" or "yes" or "evet" => "true",
            "0" or "false" or "no" or "hayır" => "false",
            _ => "false"
        };
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\0' => "\\0",
            _ => c.ToString()
        };
    }

    #endregion

    #region Append To Existing File

    /// <summary>
    /// Mevcut bir .cs dosyasının Items listesine yeni satırlar ekler.
    /// Items = new() { ... }; bloğunun kapanışını bulur ve yeni objeleri insert eder.
    /// </summary>
    public static string AppendToExistingFile(string existingContent, CsvFileData newData, string className)
    {
        className = SanitizeIdentifier(className);

        // Items listesinin son kapatma noktasını bul: "        };" pattern'i
        // Strateji: Son "};" satırından bir önceki "}," veya "}" satırının sonuna yeni veri ekle
        var lines = existingContent.Split('\n').ToList();

        // Items listesinin kapanışını bul (son "        };" satırı)
        int insertIndex = -1;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (trimmed == "        };")
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex < 0)
            throw new InvalidOperationException("Mevcut dosyada Items listesinin kapanışı ('        };') bulunamadı.");

        // Items listesinin son elemanının sonuna virgül ekle (zaten yoksa)
        for (int i = insertIndex - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // Son objenin kapanışı "            }" ise "            }," yap
                if (trimmed.EndsWith("}") && !trimmed.EndsWith("};") && !trimmed.EndsWith("},"))
                {
                    lines[i] = lines[i].TrimEnd('\r').TrimEnd() + ",\r";
                }
                break;
            }
        }

        // Yeni objeleri üret
        var sb = new StringBuilder();
        for (int rowIndex = 0; rowIndex < newData.Rows.Count; rowIndex++)
        {
            var row = newData.Rows[rowIndex];
            GenerateObjectInitializer(sb, newData, row, className, "            ", rowIndex < newData.Rows.Count - 1);
        }

        // Yeni satırları insert et
        var newLines = sb.ToString().Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrEmpty(l) || true) // tüm satırları koru
            .ToList();

        lines.InsertRange(insertIndex, newLines);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Sadece yeni objelerin kodunu üretir (append için önizleme).
    /// </summary>
    public static string GenerateAppendPreview(CsvFileData data, string className)
    {
        className = SanitizeIdentifier(className);
        var sb = new StringBuilder();
        sb.AppendLine($"// === {data.Rows.Count} yeni satır ===");

        for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
        {
            var row = data.Rows[rowIndex];
            GenerateObjectInitializer(sb, data, row, className, "            ", rowIndex < data.Rows.Count - 1);
        }

        return sb.ToString();
    }

    #endregion

    #region Helpers

    public static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Item";

        var sb = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '.')
                sb.Append(c);
        }

        var result = sb.ToString();
        if (result.Length > 0 && char.IsDigit(result[0]))
            result = "_" + result;

        return string.IsNullOrEmpty(result) ? "Item" : result;
    }

    #endregion
}
