using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services.Generators;

/// <summary>
/// Python kod üretici — dataclass, Enum, list, type hint destekli.
/// </summary>
public class PythonGenerator : CodeGeneratorBase
{
    public override string[] SupportedTypes => new[]
    {
        "str", "int", "float", "bool", "enum"
    };

    public override string FileExtension => ".py";
    public override string LanguageName => "Python";

    public override string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(className)) className = "GeneratedClass";
        className = SanitizeIdentifier(className);

        var sb = new StringBuilder();
        sb.AppendLine("from dataclasses import dataclass, field");
        sb.AppendLine("from enum import Enum");
        sb.AppendLine("from typing import Optional, List, Dict");
        sb.AppendLine();

        GenerateEnumDefinitions(sb, data);

        sb.AppendLine();
        sb.AppendLine("@dataclass");
        sb.AppendLine($"class {className}:");
        GenerateFields(sb, data);
        sb.AppendLine();
        sb.AppendLine();

        if (groupByColumnIndex >= 0 && groupByColumnIndex < data.Columns.Count)
            GenerateGroupedData(sb, data, className, groupByColumnIndex);
        else
            GenerateFlatData(sb, data, className);

        return sb.ToString();
    }

    public override string GenerateListCode(CsvFileData data, string variablePrefix, string namespaceName)
    {
        if (string.IsNullOrWhiteSpace(variablePrefix)) variablePrefix = "data";
        var prefix = ToSnakeCase(variablePrefix);

        var sb = new StringBuilder();
        sb.AppendLine("from typing import List");
        sb.AppendLine();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            var propName = ToSnakeCase(column.PropertyName);
            var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
            var isArrayColumn = ColumnHasArrayValues(data, column);

            if (isArrayColumn)
            {
                GenerateMatrixList(sb, data, column, prefix, propName, typeName);
            }
            else
            {
                GenerateFlatList(sb, data, column, prefix, propName, typeName);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Düz liste üretir — hücrelerde array yoksa.
    /// </summary>
    private void GenerateFlatList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string prefix, string propName, string typeName)
    {
        var rawValues = new List<string>();
        foreach (var row in data.Rows)
        {
            var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex].Trim() : "";
            rawValues.Add(raw);
        }

        if (column.IsUniqueList) rawValues = rawValues.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var values = new List<string>();
        foreach (var raw in rawValues)
        {
            if (column.CSharpType == "enum")
                values.Add($"{GetEnumTypeName(column)}.{SanitizeEnumMember(raw)}");
            else
                values.Add(FormatValue(raw, column.CSharpType));
        }

        SortValues(values, column.ListSortOrder);

        sb.AppendLine($"{prefix}_{propName}: List[{typeName}] = [");
        foreach (var val in values)
            sb.AppendLine($"    {val},");
        sb.AppendLine("]");
        sb.AppendLine();
    }

    /// <summary>
    /// Matris (iç içe liste) üretir — hücrelerde array varsa.
    /// </summary>
    private void GenerateMatrixList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string prefix, string propName, string typeName)
    {
        var allArrays = new List<List<string>>();
        foreach (var row in data.Rows)
        {
            var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex].Trim() : "";
            var elements = ParseArrayElements(raw);

            if (column.IsUniqueList)
                elements = elements.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var formatted = new List<string>();
            foreach (var elem in elements)
            {
                if (column.CSharpType == "enum")
                    formatted.Add($"{GetEnumTypeName(column)}.{SanitizeEnumMember(elem)}");
                else
                    formatted.Add(FormatValue(elem, column.CSharpType));
            }

            SortValues(formatted, column.ListSortOrder);

            allArrays.Add(formatted);
        }

        sb.AppendLine($"{prefix}_{propName}: List[List[{typeName}]] = [");
        foreach (var arr in allArrays)
        {
            sb.AppendLine($"    [{string.Join(", ", arr)}],");
        }
        sb.AppendLine("]");
        sb.AppendLine();
    }

    public override string AppendToExistingFile(string existingContent, CsvFileData newData, string className)
    {
        className = SanitizeIdentifier(className);
        var lines = existingContent.Split('\n').ToList();

        int insertIndex = -1;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (trimmed == "]")
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex < 0)
            throw new InvalidOperationException("Mevcut dosyada Items listesinin kapanışı (']') bulunamadı.");

        for (int i = insertIndex - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (trimmed.EndsWith(")") && !trimmed.EndsWith("],") && !trimmed.EndsWith("),"))
                    lines[i] = lines[i].TrimEnd('\r').TrimEnd() + ",\r";
                break;
            }
        }

        var sb = new StringBuilder();
        for (int i = 0; i < newData.Rows.Count; i++)
        {
            GenerateObjectCreation(sb, newData, newData.Rows[i], className, "    ", i < newData.Rows.Count - 1);
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

            sb.AppendLine();
            sb.AppendLine($"class {enumName}(Enum):");
            for (int i = 0; i < uniqueValues.Count; i++)
            {
                var raw = uniqueValues[i];
                var member = SanitizeEnumMember(raw);
                if (int.TryParse(raw, out int num))
                    sb.AppendLine($"    {member} = {num}");
                else
                    sb.AppendLine($"    {member} = \"{raw}\"");
            }
        }
    }

    #endregion

    #region Fields

    private void GenerateFields(StringBuilder sb, CsvFileData data)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var fieldName = ToSnakeCase(column.PropertyName);
                var typeName = column.CSharpType == "enum"
                    ? GetEnumTypeName(column)
                    : MapType(column.CSharpType);
                sb.AppendLine($"    {fieldName}: {typeName}");
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupName = ToSnakeCase(column.GroupName);
                    var elementType = MapType(column.CSharpType);
                    sb.AppendLine($"    {groupName}: List[{elementType}] = field(default_factory=list)");
                }
            }
        }
    }

    #endregion

    #region Data

    private void GenerateFlatData(StringBuilder sb, CsvFileData data, string className)
    {
        sb.AppendLine($"Items: List[{className}] = [");
        for (int i = 0; i < data.Rows.Count; i++)
            GenerateObjectCreation(sb, data, data.Rows[i], className, "    ", i < data.Rows.Count - 1);
        sb.AppendLine("]");
    }

    private void GenerateGroupedData(StringBuilder sb, CsvFileData data, string className, int groupByColumnIndex)
    {
        sb.AppendLine($"GroupedItems: List[List[{className}]] = [");

        var groups = BuildGroups(data, groupByColumnIndex);
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            sb.AppendLine($"    # Group: {group.Key}");
            sb.AppendLine("    [");
            for (int ri = 0; ri < group.Rows.Count; ri++)
                GenerateObjectCreation(sb, data, group.Rows[ri], className, "        ", ri < group.Rows.Count - 1);
            sb.AppendLine($"    ]{(gi < groups.Count - 1 ? "," : "")}");
        }

        sb.AppendLine("]");
    }

    private void GenerateObjectCreation(StringBuilder sb, CsvFileData data, string[] row, string className, string indent, bool trailing)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var args = new List<string>();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var fieldName = ToSnakeCase(column.PropertyName);
                var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex] : "";
                if (column.CSharpType == "enum")
                {
                    var enumTypeName = GetEnumTypeName(column);
                    var enumMember = SanitizeEnumMember(raw.Trim());
                    args.Add($"{fieldName}={enumTypeName}.{enumMember}");
                }
                else
                {
                    args.Add($"{fieldName}={FormatValue(raw, column.CSharpType)}");
                }
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupName = ToSnakeCase(column.GroupName);
                    var groupColumns = data.Columns
                        .Where(c => c.IsIncluded && c.GroupName.Equals(column.GroupName, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.ColumnIndex).ToList();
                    var values = groupColumns.Select(gc =>
                    {
                        var raw = gc.ColumnIndex < row.Length ? row[gc.ColumnIndex] : "";
                        return FormatValue(raw, gc.CSharpType);
                    });
                    args.Add($"{groupName}=[{string.Join(", ", values)}]");
                }
            }
        }

        sb.AppendLine($"{indent}{className}({string.Join(", ", args)}){(trailing ? "," : "")}");
    }

    #endregion

    #region Helpers

    private static string MapType(string csharpType) => csharpType switch
    {
        "string" or "str" => "str",
        "int" or "int?" or "byte" or "byte?" or "short" or "short?" => "int",
        "long" or "long?" => "int",
        "float" or "float?" or "double" or "double?" or "decimal" or "decimal?" => "float",
        "bool" or "bool?" => "bool",
        "char" or "char?" => "str",
        _ => "str"
    };

    private static string FormatValue(string rawValue, string csharpType)
    {
        var trimmed = rawValue.Trim();
        bool isNull = IsNullOrNa(trimmed);

        if (isNull)
        {
            return csharpType.EndsWith("?") ? "None" : (MapType(csharpType) switch
            {
                "str" => "\"\"",
                "int" => "0",
                "float" => "0.0",
                "bool" => "False",
                _ => "None"
            });
        }

        return MapType(csharpType) switch
        {
            "str" => $"\"{EscapeString(trimmed)}\"",
            "int" => NormalizeDecimalSeparator(trimmed).Split('.')[0],
            "float" => NormalizeDecimalSeparator(trimmed),
            "bool" => trimmed.ToLowerInvariant() is "1" or "true" or "yes" or "evet" ? "True" : "False",
            _ => $"\"{EscapeString(trimmed)}\""
        };
    }

    private static string ToSnakeCase(string name)
    {
        var sanitized = SanitizeIdentifier(name);
        var sb = new StringBuilder();
        for (int i = 0; i < sanitized.Length; i++)
        {
            var c = sanitized[i];
            if (char.IsUpper(c) && i > 0 && !char.IsUpper(sanitized[i - 1]))
            {
                sb.Append('_');
            }
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
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
}
