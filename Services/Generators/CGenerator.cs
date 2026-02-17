using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services.Generators;

/// <summary>
/// C kod üretici — struct, statik array, enum destekli.
/// Header-only (.h) dosyası üretir.
/// </summary>
public class CGenerator : CodeGeneratorBase
{
    public override string[] SupportedTypes => new[]
    {
        "const char*", "int", "long", "float", "double",
        "char", "short", "unsigned int", "enum"
    };

    public override string FileExtension => ".h";
    public override string LanguageName => "C";

    public override string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1, int lookupKeyColumnIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(className)) className = "GeneratedData";
        className = SanitizeIdentifier(className);
        var upperName = className.ToUpperInvariant();

        var sb = new StringBuilder();
        sb.AppendLine($"#ifndef {upperName}_H");
        sb.AppendLine($"#define {upperName}_H");
        sb.AppendLine();
        sb.AppendLine("#include <stdlib.h>");
        sb.AppendLine();

        GenerateEnumDefinitions(sb, data);

        sb.AppendLine($"typedef struct {{");
        GenerateMembers(sb, data);
        sb.AppendLine($"}} {className};");
        sb.AppendLine();

        if (groupByColumnIndex >= 0 && groupByColumnIndex < data.Columns.Count)
            GenerateGroupedData(sb, data, className, groupByColumnIndex);
        else if (lookupKeyColumnIndex >= 0 && lookupKeyColumnIndex < data.Columns.Count)
            GenerateLookupData(sb, data, className, lookupKeyColumnIndex);
        else
            GenerateFlatData(sb, data, className);

        GenerateUniqueArrays(sb, data);

        sb.AppendLine($"#endif /* {upperName}_H */");
        return sb.ToString();
    }

    public override string AppendToExistingFile(string existingContent, CsvFileData newData, string className)
    {
        className = SanitizeIdentifier(className);
        var lines = existingContent.Split('\n').ToList();

        int insertIndex = -1;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (trimmed == "};")
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex < 0)
            throw new InvalidOperationException("Mevcut dosyada data array kapanışı bulunamadı.");

        // Eleman sayısını güncelle (basit yaklaşım)
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
            GenerateStructInit(sb, newData, newData.Rows[i], "    ", i < newData.Rows.Count - 1);

        var newLines = sb.ToString().Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        lines.InsertRange(insertIndex, newLines);
        return string.Join("\n", lines);
    }

    #region Enum

    private void GenerateEnumDefinitions(StringBuilder sb, CsvFileData data)
    {
        var enumColumns = data.Columns.Where(c => c.CSharpType == "enum" && c.IsStandalone).ToList();
        if (enumColumns.Count == 0) return;

        foreach (var column in enumColumns)
        {
            var enumName = GetEnumTypeName(column);
            var uniqueValues = GetUniqueColumnValues(data, column.ColumnIndex);

            sb.AppendLine($"typedef enum {{");
            for (int i = 0; i < uniqueValues.Count; i++)
            {
                var raw = uniqueValues[i];
                var member = $"{enumName}_{SanitizeEnumMember(raw)}";
                var sep = i < uniqueValues.Count - 1 ? "," : "";
                if (int.TryParse(raw, out int num))
                    sb.AppendLine($"    {member} = {num}{sep}");
                else
                    sb.AppendLine($"    {member}{sep}");
            }
            sb.AppendLine($"}} {enumName};");
            sb.AppendLine();
        }
    }

    #endregion

    #region Members

    private void GenerateMembers(StringBuilder sb, CsvFileData data)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in data.Columns)
        {
            if (column.IsStandalone)
            {
                var memberName = SanitizeIdentifier(column.PropertyName);
                var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
                sb.AppendLine($"    {typeName} {memberName};");
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupName = SanitizeIdentifier(column.GroupName);
                    var elementType = MapType(column.CSharpType);
                    var groupCols = data.Columns.Where(c => c.GroupName.Equals(column.GroupName, StringComparison.OrdinalIgnoreCase)).ToList();
                    sb.AppendLine($"    {elementType} {groupName}[{groupCols.Count}];");
                }
            }
        }
    }

    #endregion

    #region Data

    private void GenerateFlatData(StringBuilder sb, CsvFileData data, string className)
    {
        sb.AppendLine($"static const int {className}_count = {data.Rows.Count};");
        sb.AppendLine($"static const {className} {className}_Items[] = {{");
        for (int i = 0; i < data.Rows.Count; i++)
            GenerateStructInit(sb, data, data.Rows[i], "    ", i < data.Rows.Count - 1);
        sb.AppendLine("};");
        sb.AppendLine();
    }

    private void GenerateGroupedData(StringBuilder sb, CsvFileData data, string className, int groupByColumnIndex)
    {
        var groups = BuildGroups(data, groupByColumnIndex);

        // Her grubu ayrı array olarak üret
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            var groupArrayName = $"{className}_Group_{SanitizeIdentifier(group.Key)}";
            sb.AppendLine($"/* Group: {group.Key} */");
            sb.AppendLine($"static const int {groupArrayName}_count = {group.Rows.Count};");
            sb.AppendLine($"static const {className} {groupArrayName}[] = {{");
            for (int ri = 0; ri < group.Rows.Count; ri++)
                GenerateStructInit(sb, data, group.Rows[ri], "    ", ri < group.Rows.Count - 1);
            sb.AppendLine("};");
            sb.AppendLine();
        }
    }

    private void GenerateLookupData(StringBuilder sb, CsvFileData data, string className, int lookupKeyColumnIndex)
    {
        var keyColumn = data.Columns[lookupKeyColumnIndex];
        var groups = BuildGroups(data, lookupKeyColumnIndex);
        var propName = SanitizeIdentifier(keyColumn.PropertyName);

        foreach (var group in groups)
        {
            var safeName = SanitizeIdentifier(group.Key);
            sb.AppendLine($"/* {propName} = {group.Key} */");
            sb.AppendLine($"static const int {propName}_{safeName}_count = {group.Rows.Count};");
            sb.AppendLine($"static const {className} {propName}_{safeName}[] = {{");
            for (int ri = 0; ri < group.Rows.Count; ri++)
                GenerateStructInit(sb, data, group.Rows[ri], "    ", ri < group.Rows.Count - 1);
            sb.AppendLine("};");
            sb.AppendLine();
        }
    }

    private void GenerateUniqueArrays(StringBuilder sb, CsvFileData data)
    {
        var uniqueColumns = data.Columns.Where(c => c.IsUnique).ToList();
        if (uniqueColumns.Count == 0) return;

        sb.AppendLine();
        foreach (var column in uniqueColumns)
        {
            var propName = SanitizeIdentifier(column.PropertyName);
            var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
            var uniqueValues = GetUniqueColumnValues(data, column.ColumnIndex);

            var formatted = uniqueValues.Select(v =>
                column.CSharpType == "enum"
                    ? SanitizeEnumMember(v)
                    : FormatValue(v, column.CSharpType)
            );

            sb.AppendLine($"static const int unique_{propName}_count = {uniqueValues.Count};");
            sb.AppendLine($"static const {typeName} unique_{propName}[] = {{ {string.Join(", ", formatted)} }};");
        }
        sb.AppendLine();
    }

    private void GenerateStructInit(StringBuilder sb, CsvFileData data, string[] row, string indent, bool trailing)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var values = new List<string>();

        foreach (var column in data.Columns)
        {
            if (column.IsStandalone)
            {
                var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex] : "";
                if (column.CSharpType == "enum")
                {
                    var enumTypeName = GetEnumTypeName(column);
                    var enumMember = SanitizeEnumMember(raw.Trim());
                    values.Add($"{enumTypeName}_{enumMember}");
                }
                else
                {
                    values.Add(FormatValue(raw, column.CSharpType));
                }
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupColumns = data.Columns
                        .Where(c => c.GroupName.Equals(column.GroupName, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(c => c.ColumnIndex).ToList();
                    var groupValues = groupColumns.Select(gc =>
                    {
                        var raw = gc.ColumnIndex < row.Length ? row[gc.ColumnIndex] : "";
                        return FormatValue(raw, gc.CSharpType);
                    });
                    values.Add($"{{{string.Join(", ", groupValues)}}}");
                }
            }
        }

        sb.AppendLine($"{indent}{{{string.Join(", ", values)}}}{(trailing ? "," : "")}");
    }

    #endregion

    #region Helpers

    private static string MapType(string csharpType) => csharpType switch
    {
        "string" or "const char*" => "const char*",
        "int" or "int?" => "int",
        "long" or "long?" => "long",
        "float" or "float?" => "float",
        "double" or "double?" or "decimal" or "decimal?" => "double",
        "bool" or "bool?" => "int",
        "char" or "char?" => "char",
        "byte" or "byte?" => "unsigned char",
        "short" or "short?" => "short",
        "unsigned int" => "unsigned int",
        _ => "const char*"
    };

    private static string FormatValue(string rawValue, string csharpType)
    {
        var trimmed = rawValue.Trim();
        bool isNull = IsNullOrNa(trimmed);
        var mapped = MapType(csharpType);

        if (isNull)
        {
            return mapped switch
            {
                "const char*" => "\"\"",
                "char" => "'\\0'",
                _ => "0"
            };
        }

        return mapped switch
        {
            "const char*" => $"\"{EscapeString(trimmed)}\"",
            "int" or "long" or "short" or "unsigned char" or "unsigned int" => NormalizeDecimalSeparator(trimmed).Split('.')[0],
            "float" => $"{NormalizeDecimalSeparator(trimmed)}f",
            "double" => NormalizeDecimalSeparator(trimmed),
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
