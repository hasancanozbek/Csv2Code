using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services.Generators;

/// <summary>
/// C# kod üretici — mevcut tüm özellikler korunmuştur.
/// </summary>
public class CSharpGenerator : CodeGeneratorBase
{
    public override string[] SupportedTypes => new[]
    {
        "string", "int", "int?", "long", "long?",
        "float", "float?", "double", "double?",
        "decimal", "decimal?", "bool", "bool?",
        "char", "char?", "byte", "byte?", "short", "short?", "enum"
    };

    public override string FileExtension => ".cs";
    public override string LanguageName => "C#";

    public override string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(className)) className = "GeneratedClass";
        if (string.IsNullOrWhiteSpace(namespaceName)) namespaceName = "Generated";
        className = SanitizeIdentifier(className);
        namespaceName = SanitizeIdentifier(namespaceName);

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        GenerateEnumDefinitions(sb, data);

        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");
        GenerateProperties(sb, data);
        sb.AppendLine();

        if (groupByColumnIndex >= 0 && groupByColumnIndex < data.Columns.Count)
            GenerateGroupedData(sb, data, className, groupByColumnIndex);
        else
            GenerateFlatData(sb, data, className);

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public override string GenerateListCode(CsvFileData data, string variablePrefix, string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(variablePrefix)) variablePrefix = "Data";
        if (string.IsNullOrWhiteSpace(namespaceName)) namespaceName = "Generated";
        variablePrefix = SanitizeIdentifier(variablePrefix);
        namespaceName = SanitizeIdentifier(namespaceName);

        var sb = new StringBuilder();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine($"    public static class {variablePrefix}");
        sb.AppendLine("    {");

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            var propName = SanitizeIdentifier(column.PropertyName);
            var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : column.CSharpType;
            var isArrayColumn = ColumnHasArrayValues(data, column);

            if (isArrayColumn)
            {
                GenerateMatrixList(sb, data, column, propName, typeName);
            }
            else
            {
                GenerateFlatList(sb, data, column, propName, typeName);
            }

            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Düz (flat) liste üretir — hücrelerde array yoksa.
    /// </summary>
    private void GenerateFlatList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string propName, string typeName)
    {
        // Ham değerleri topla
        var rawValues = new List<string>();
        foreach (var row in data.Rows)
        {
            var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex].Trim() : "";
            rawValues.Add(raw);
        }

        // Unique filtresi: ham değerler üzerinde çalışır
        if (column.IsUniqueList)
            rawValues = rawValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Formatlama
        var values = new List<string>();
        foreach (var raw in rawValues)
        {
            if (column.CSharpType == "enum")
            {
                var enumTypeName = GetEnumTypeName(column);
                values.Add($"{enumTypeName}.{SanitizeEnumMember(raw)}");
            }
            else
            {
                values.Add(FormatValue(raw, column.CSharpType));
            }
        }

        SortValues(values, column.ListSortOrder);

        if (column.ListCollectionType == GroupCollectionType.Array)
        {
            sb.AppendLine($"        public static readonly {typeName}[] {propName} = new {typeName}[]");
            sb.AppendLine("        {");
            foreach (var val in values)
                sb.AppendLine($"            {val},");
            sb.AppendLine("        };");
        }
        else
        {
            sb.AppendLine($"        public static readonly List<{typeName}> {propName} = new()");
            sb.AppendLine("        {");
            foreach (var val in values)
                sb.AppendLine($"            {val},");
            sb.AppendLine("        };");
        }
    }

    /// <summary>
    /// Matris (iç içe liste) üretir — hücrelerde array varsa.
    /// Her hücredeki array bir iç liste/array olur, sonuç List&lt;List&lt;T&gt;&gt; veya T[][] olur.
    /// </summary>
    private void GenerateMatrixList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string propName, string typeName)
    {
        // Her satırdaki array elemanlarını parse et
        var allArrays = new List<List<string>>();
        foreach (var row in data.Rows)
        {
            var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex].Trim() : "";
            var elements = ParseArrayElements(raw);

            // Unique filtresi: iç array'deki elemanları benzersizleştir
            if (column.IsUniqueList)
                elements = elements.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var formatted = new List<string>();
            foreach (var elem in elements)
            {
                if (column.CSharpType == "enum")
                {
                    var enumTypeName = GetEnumTypeName(column);
                    formatted.Add($"{enumTypeName}.{SanitizeEnumMember(elem)}");
                }
                else
                {
                    formatted.Add(FormatValue(elem, column.CSharpType));
                }
            }

            // İç array sıralama
            SortValues(formatted, column.ListSortOrder);

            allArrays.Add(formatted);
        }

        if (column.ListCollectionType == GroupCollectionType.Array)
        {
            sb.AppendLine($"        public static readonly {typeName}[][] {propName} = new {typeName}[][]");
            sb.AppendLine("        {");
            foreach (var arr in allArrays)
            {
                sb.AppendLine($"            new {typeName}[] {{ {string.Join(", ", arr)} }},");
            }
            sb.AppendLine("        };");
        }
        else
        {
            sb.AppendLine($"        public static readonly List<List<{typeName}>> {propName} = new()");
            sb.AppendLine("        {");
            foreach (var arr in allArrays)
            {
                sb.AppendLine($"            new List<{typeName}> {{ {string.Join(", ", arr)} }},");
            }
            sb.AppendLine("        };");
        }
    }

    public override string AppendToExistingFile(string existingContent, CsvFileData newData, string className)
    {
        className = SanitizeIdentifier(className);
        var lines = existingContent.Split('\n').ToList();

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

        for (int i = insertIndex - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (trimmed.EndsWith("}") && !trimmed.EndsWith("};") && !trimmed.EndsWith("},"))
                    lines[i] = lines[i].TrimEnd('\r').TrimEnd() + ",\r";
                break;
            }
        }

        var sb = new StringBuilder();
        for (int rowIndex = 0; rowIndex < newData.Rows.Count; rowIndex++)
        {
            var row = newData.Rows[rowIndex];
            GenerateObjectInitializer(sb, newData, row, className, "            ", rowIndex < newData.Rows.Count - 1);
        }

        var newLines = sb.ToString().Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        lines.InsertRange(insertIndex, newLines);
        return string.Join("\n", lines);
    }

    #region Enum

    private void GenerateEnumDefinitions(StringBuilder sb, CsvFileData data)
    {
        var enumColumns = data.Columns.Where(c => c.CSharpType == "enum" && c.IsStandalone && c.IsIncluded && IsDefaultEnumName(c)).ToList();
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
                var sep = i < uniqueValues.Count - 1 ? "," : "";
                if (int.TryParse(rawValue, out int numericValue))
                    sb.AppendLine($"        {enumMember} = {numericValue}{sep}");
                else
                    sb.AppendLine($"        {enumMember}{sep}");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    #endregion

    #region Properties

    private void GenerateProperties(StringBuilder sb, CsvFileData data)
    {
        sb.AppendLine("        #region Properties");
        sb.AppendLine();
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var propName = SanitizeIdentifier(column.PropertyName);
                var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : column.CSharpType;
                sb.AppendLine($"        public {typeName} {propName} {{ get; set; }}");
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupPropName = SanitizeIdentifier(column.GroupName);
                    var elementType = column.CSharpType;
                    var propType = column.CollectionType == GroupCollectionType.Array
                        ? $"{elementType}[]"
                        : $"List<{elementType}>";
                    sb.AppendLine($"        public {propType} {groupPropName} {{ get; set; }}");
                }
            }
        }
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    #endregion

    #region Data Generation

    private void GenerateFlatData(StringBuilder sb, CsvFileData data, string className)
    {
        sb.AppendLine("        #region Data");
        sb.AppendLine();
        sb.AppendLine($"        public static readonly List<{className}> Items = new()");
        sb.AppendLine("        {");
        for (int i = 0; i < data.Rows.Count; i++)
            GenerateObjectInitializer(sb, data, data.Rows[i], className, "            ", i < data.Rows.Count - 1);
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    private void GenerateGroupedData(StringBuilder sb, CsvFileData data, string className, int groupByColumnIndex)
    {
        sb.AppendLine("        #region Data");
        sb.AppendLine();
        sb.AppendLine($"        public static readonly List<List<{className}>> GroupedItems = new()");
        sb.AppendLine("        {");

        var groups = BuildGroups(data, groupByColumnIndex);

        for (int gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            sb.AppendLine($"            // Group: {group.Key}");
            sb.AppendLine("            new List<" + className + ">()");
            sb.AppendLine("            {");
            for (int ri = 0; ri < group.Rows.Count; ri++)
                GenerateObjectInitializer(sb, data, group.Rows[ri], className, "                ", ri < group.Rows.Count - 1);
            sb.AppendLine($"            }}{(gi < groups.Count - 1 ? "," : "")}");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        #endregion");
    }

    private void GenerateObjectInitializer(StringBuilder sb, CsvFileData data, string[] row, string className, string indent, bool hasTrailingComma)
    {
        sb.AppendLine($"{indent}new {className}");
        sb.AppendLine($"{indent}{{");
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var propertyLines = new List<string>();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var propName = SanitizeIdentifier(column.PropertyName);
                var rawValue = column.ColumnIndex < row.Length ? row[column.ColumnIndex] : "";
                if (column.CSharpType == "enum")
                {
                    var enumTypeName = GetEnumTypeName(column);
                    var enumMember = SanitizeEnumMember(rawValue.Trim());
                    propertyLines.Add($"{indent}    {propName} = {enumTypeName}.{enumMember}");
                }
                else
                {
                    propertyLines.Add($"{indent}    {propName} = {FormatValue(rawValue, column.CSharpType)}");
                }
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupPropName = SanitizeIdentifier(column.GroupName);
                    var groupColumns = data.Columns
                        .Where(c => c.IsIncluded && c.GroupName.Equals(column.GroupName, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.ColumnIndex).ToList();
                    var values = groupColumns.Select(gc =>
                    {
                        var raw = gc.ColumnIndex < row.Length ? row[gc.ColumnIndex] : "";
                        return FormatValue(raw, gc.CSharpType);
                    }).ToList();
                    var valuesStr = string.Join(", ", values);
                    var init = column.CollectionType == GroupCollectionType.Array
                        ? $"new {column.CSharpType}[] {{ {valuesStr} }}"
                        : $"new List<{column.CSharpType}> {{ {valuesStr} }}";
                    propertyLines.Add($"{indent}    {groupPropName} = {init}");
                }
            }
        }

        for (int i = 0; i < propertyLines.Count; i++)
            sb.AppendLine($"{propertyLines[i]}{(i < propertyLines.Count - 1 ? "," : "")}");

        sb.AppendLine($"{indent}}}{(hasTrailingComma ? "," : "")}");
    }

    private static List<(string Key, List<string[]> Rows)> BuildGroups(CsvFileData data, int groupByColumnIndex)
    {
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
        return groups;
    }

    #endregion

    #region Value Formatting

    public static string FormatValue(string rawValue, string csharpType)
    {
        var trimmed = rawValue.Trim();
        bool isNull = IsNullOrNa(trimmed);

        if (csharpType.EndsWith("?") && isNull) return "null";

        return csharpType switch
        {
            "string" => $"\"{EscapeString(trimmed)}\"",
            "int" or "int?" => ConvertToNumericLiteral(trimmed, ""),
            "long" or "long?" => ConvertToNumericLiteral(trimmed, "L"),
            "float" or "float?" => ConvertToDecimalLiteral(trimmed, "f"),
            "double" or "double?" => ConvertToDecimalLiteral(trimmed, "d"),
            "decimal" or "decimal?" => ConvertToDecimalLiteral(trimmed, "m"),
            "bool" or "bool?" => ConvertToBoolLiteral(trimmed),
            "char" or "char?" => isNull && csharpType.EndsWith("?") ? "null" : (trimmed.Length > 0 ? $"'{EscapeChar(trimmed[0])}'" : "'\\0'"),
            "byte" or "byte?" => ConvertToNumericLiteral(trimmed, ""),
            "short" or "short?" => ConvertToNumericLiteral(trimmed, ""),
            _ => $"\"{EscapeString(trimmed)}\""
        };
    }

    private static string ConvertToDecimalLiteral(string value, string suffix)
        => $"{NormalizeDecimalSeparator(value)}{suffix}";

    private static string ConvertToNumericLiteral(string value, string suffix)
    {
        var normalized = NormalizeDecimalSeparator(value);
        if (normalized.Contains('.'))
            normalized = normalized[..normalized.IndexOf('.')];
        return string.IsNullOrEmpty(suffix) ? normalized : $"{normalized}{suffix}";
    }

    private static string ConvertToBoolLiteral(string value) => value.ToLowerInvariant() switch
    {
        "1" or "true" or "yes" or "evet" => "true",
        "0" or "false" or "no" or "hayır" => "false",
        _ => "false"
    };

    private static string EscapeChar(char c) => c switch
    {
        '\\' => "\\\\", '\'' => "\\'", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t", '\0' => "\\0",
        _ => c.ToString()
    };

    #endregion
}
