using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services.Generators;

/// <summary>
/// C++ kod üretici — struct, vector, enum class destekli.
/// Header-only (.h) dosyası üretir.
/// </summary>
public class CppGenerator : CodeGeneratorBase
{
    public override string[] SupportedTypes => new[]
    {
        "std::string", "int", "long", "float", "double",
        "bool", "char", "short", "unsigned int", "enum"
    };

    public override string FileExtension => ".h";
    public override string LanguageName => "C++";

    public override string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(className)) className = "GeneratedClass";
        if (string.IsNullOrWhiteSpace(namespaceName)) namespaceName = "Generated";
        className = SanitizeIdentifier(className);
        namespaceName = SanitizeIdentifier(namespaceName);

        var sb = new StringBuilder();
        sb.AppendLine("#pragma once");
        sb.AppendLine("#include <string>");
        sb.AppendLine("#include <vector>");
        sb.AppendLine("#include <array>");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        GenerateEnumDefinitions(sb, data);

        sb.AppendLine($"    struct {className}");
        sb.AppendLine("    {");
        GenerateMembers(sb, data);
        sb.AppendLine("    };");
        sb.AppendLine();

        if (groupByColumnIndex >= 0 && groupByColumnIndex < data.Columns.Count)
            GenerateGroupedData(sb, data, className, groupByColumnIndex);
        else
            GenerateFlatData(sb, data, className);

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
        sb.AppendLine("#pragma once");
        sb.AppendLine("#include <string>");
        sb.AppendLine("#include <vector>");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            var propName = SanitizeIdentifier(column.PropertyName);
            var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
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

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Düz liste üretir — hücrelerde array yoksa.
    /// </summary>
    private void GenerateFlatList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string propName, string typeName)
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
                values.Add($"{GetEnumTypeName(column)}::{SanitizeEnumMember(raw)}");
            else
                values.Add(FormatValue(raw, column.CSharpType));
        }

        SortValues(values, column.ListSortOrder);

        sb.AppendLine($"    inline const std::vector<{typeName}> {propName} = {{");
        foreach (var val in values)
            sb.AppendLine($"        {val},");
        sb.AppendLine("    };");
    }

    /// <summary>
    /// Matris (iç içe vector) üretir — hücrelerde array varsa.
    /// </summary>
    private void GenerateMatrixList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string propName, string typeName)
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
                    formatted.Add($"{GetEnumTypeName(column)}::{SanitizeEnumMember(elem)}");
                else
                    formatted.Add(FormatValue(elem, column.CSharpType));
            }

            SortValues(formatted, column.ListSortOrder);

            allArrays.Add(formatted);
        }

        sb.AppendLine($"    inline const std::vector<std::vector<{typeName}>> {propName} = {{");
        foreach (var arr in allArrays)
        {
            sb.AppendLine($"        {{{string.Join(", ", arr)}}},");
        }
        sb.AppendLine("    };");
    }

    public override string AppendToExistingFile(string existingContent, CsvFileData newData, string className)
    {
        className = SanitizeIdentifier(className);
        var lines = existingContent.Split('\n').ToList();

        int insertIndex = -1;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (trimmed == "    };")
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex < 0)
            throw new InvalidOperationException("Mevcut dosyada data listesinin kapanışı bulunamadı.");

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
        for (int i = 0; i < newData.Rows.Count; i++)
            GenerateStructInitializer(sb, newData, newData.Rows[i], className, "        ", i < newData.Rows.Count - 1);

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

            sb.AppendLine($"    enum class {enumName}");
            sb.AppendLine("    {");
            for (int i = 0; i < uniqueValues.Count; i++)
            {
                var raw = uniqueValues[i];
                var member = SanitizeEnumMember(raw);
                var sep = i < uniqueValues.Count - 1 ? "," : "";
                if (int.TryParse(raw, out int num))
                    sb.AppendLine($"        {member} = {num}{sep}");
                else
                    sb.AppendLine($"        {member}{sep}");
            }
            sb.AppendLine("    };");
            sb.AppendLine();
        }
    }

    #endregion

    #region Members

    private void GenerateMembers(StringBuilder sb, CsvFileData data)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var memberName = SanitizeIdentifier(column.PropertyName);
                var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
                sb.AppendLine($"        {typeName} {memberName};");
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupName = SanitizeIdentifier(column.GroupName);
                    var elementType = MapType(column.CSharpType);
                    var memberType = $"std::vector<{elementType}>";
                    sb.AppendLine($"        {memberType} {groupName};");
                }
            }
        }
    }

    #endregion

    #region Data

    private void GenerateFlatData(StringBuilder sb, CsvFileData data, string className)
    {
        sb.AppendLine($"    inline const std::vector<{className}> Items = {{");
        for (int i = 0; i < data.Rows.Count; i++)
            GenerateStructInitializer(sb, data, data.Rows[i], className, "        ", i < data.Rows.Count - 1);
        sb.AppendLine("    };");
    }

    private void GenerateGroupedData(StringBuilder sb, CsvFileData data, string className, int groupByColumnIndex)
    {
        sb.AppendLine($"    inline const std::vector<std::vector<{className}>> GroupedItems = {{");

        var groups = BuildGroups(data, groupByColumnIndex);
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            sb.AppendLine($"        // Group: {group.Key}");
            sb.AppendLine("        {");
            for (int ri = 0; ri < group.Rows.Count; ri++)
                GenerateStructInitializer(sb, data, group.Rows[ri], className, "            ", ri < group.Rows.Count - 1);
            sb.AppendLine($"        }}{(gi < groups.Count - 1 ? "," : "")}");
        }

        sb.AppendLine("    };");
    }

    private void GenerateStructInitializer(StringBuilder sb, CsvFileData data, string[] row, string className, string indent, bool trailing)
    {
        sb.AppendLine($"{indent}{className}{{");
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = new List<string>();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex] : "";
                if (column.CSharpType == "enum")
                {
                    var enumTypeName = GetEnumTypeName(column);
                    var enumMember = SanitizeEnumMember(raw.Trim());
                    lines.Add($"{indent}    /* .{SanitizeIdentifier(column.PropertyName)} = */ {enumTypeName}::{enumMember}");
                }
                else
                {
                    lines.Add($"{indent}    /* .{SanitizeIdentifier(column.PropertyName)} = */ {FormatValue(raw, column.CSharpType)}");
                }
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupColumns = data.Columns
                        .Where(c => c.IsIncluded && c.GroupName.Equals(column.GroupName, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.ColumnIndex).ToList();
                    var values = groupColumns.Select(gc =>
                    {
                        var raw = gc.ColumnIndex < row.Length ? row[gc.ColumnIndex] : "";
                        return FormatValue(raw, gc.CSharpType);
                    });
                    lines.Add($"{indent}    /* .{SanitizeIdentifier(column.GroupName)} = */ {{{string.Join(", ", values)}}}");
                }
            }
        }

        for (int i = 0; i < lines.Count; i++)
            sb.AppendLine($"{lines[i]}{(i < lines.Count - 1 ? "," : "")}");

        sb.AppendLine($"{indent}}}{(trailing ? "," : "")}");
    }

    #endregion

    #region Helpers

    private static string MapType(string csharpType) => csharpType switch
    {
        "string" or "std::string" => "std::string",
        "int" or "int?" => "int",
        "long" or "long?" => "long",
        "float" or "float?" => "float",
        "double" or "double?" => "double",
        "decimal" or "decimal?" => "double",
        "bool" or "bool?" => "bool",
        "char" or "char?" => "char",
        "byte" or "byte?" => "unsigned char",
        "short" or "short?" => "short",
        "unsigned int" => "unsigned int",
        _ => "std::string"
    };

    private static string FormatValue(string rawValue, string csharpType)
    {
        var trimmed = rawValue.Trim();
        bool isNull = IsNullOrNa(trimmed);
        var mappedType = MapType(csharpType);

        if (isNull)
        {
            return mappedType switch
            {
                "std::string" => "\"\"",
                "bool" => "false",
                "char" => "'\\0'",
                _ => "0"
            };
        }

        return mappedType switch
        {
            "std::string" => $"\"{EscapeString(trimmed)}\"",
            "int" or "long" or "short" or "unsigned char" or "unsigned int" => NormalizeDecimalSeparator(trimmed).Split('.')[0],
            "float" => $"{NormalizeDecimalSeparator(trimmed)}f",
            "double" => NormalizeDecimalSeparator(trimmed),
            "bool" => trimmed.ToLowerInvariant() is "1" or "true" or "yes" or "evet" ? "true" : "false",
            "char" => trimmed.Length > 0 ? $"'{trimmed[0]}'" : "'\\0'",
            _ => $"\"{EscapeString(trimmed)}\""
        };
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
