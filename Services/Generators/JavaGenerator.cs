using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services.Generators;

/// <summary>
/// Java kod üretici — class, ArrayList, enum, record destekli.
/// </summary>
public class JavaGenerator : CodeGeneratorBase
{
    public override string[] SupportedTypes => new[]
    {
        "String", "int", "Integer", "long", "Long",
        "float", "Float", "double", "Double",
        "boolean", "Boolean", "char", "Character",
        "byte", "Byte", "short", "Short", "enum"
    };

    public override string FileExtension => ".java";
    public override string LanguageName => "Java";

    public override string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(className)) className = "GeneratedClass";
        if (string.IsNullOrWhiteSpace(namespaceName)) namespaceName = "generated";
        className = SanitizeIdentifier(className);
        namespaceName = SanitizeIdentifier(namespaceName).ToLowerInvariant();

        var sb = new StringBuilder();
        sb.AppendLine($"package {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("import java.util.ArrayList;");
        sb.AppendLine("import java.util.Arrays;");
        sb.AppendLine("import java.util.List;");
        sb.AppendLine();

        GenerateEnumDefinitions(sb, data, className);

        sb.AppendLine($"public class {className} {{");
        sb.AppendLine();
        GenerateFields(sb, data);
        sb.AppendLine();
        GenerateConstructor(sb, data, className);
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
        if (string.IsNullOrWhiteSpace(namespaceName)) namespaceName = "generated";
        variablePrefix = SanitizeIdentifier(variablePrefix);
        namespaceName = SanitizeIdentifier(namespaceName).ToLowerInvariant();

        var sb = new StringBuilder();
        sb.AppendLine($"package {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("import java.util.ArrayList;");
        sb.AppendLine("import java.util.Arrays;");
        sb.AppendLine("import java.util.List;");
        sb.AppendLine();
        sb.AppendLine($"public class {variablePrefix} {{");
        sb.AppendLine();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            var propName = ToCamelCase(column.PropertyName);
            var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
            var boxedType = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapBoxedType(column.CSharpType);
            var isArrayColumn = ColumnHasArrayValues(data, column);

            if (isArrayColumn)
            {
                GenerateMatrixList(sb, data, column, propName, typeName, boxedType);
            }
            else
            {
                GenerateFlatList(sb, data, column, propName, typeName, boxedType);
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
        string propName, string typeName, string boxedType)
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

        if (column.ListCollectionType == GroupCollectionType.Array)
        {
            sb.AppendLine($"    public static final {typeName}[] {propName} = new {typeName}[] {{");
            foreach (var val in values)
                sb.AppendLine($"        {val},");
            sb.AppendLine("    };");
        }
        else
        {
            sb.AppendLine($"    public static final List<{boxedType}> {propName} = new ArrayList<>(Arrays.asList(");
            for (int i = 0; i < values.Count; i++)
                sb.AppendLine($"        {values[i]}{(i < values.Count - 1 ? "," : "")}");
            sb.AppendLine("    ));");
        }
    }

    /// <summary>
    /// Matris (iç içe liste) üretir — hücrelerde array varsa.
    /// </summary>
    private void GenerateMatrixList(StringBuilder sb, CsvFileData data, CsvColumn column,
        string propName, string typeName, string boxedType)
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

        if (column.ListCollectionType == GroupCollectionType.Array)
        {
            sb.AppendLine($"    public static final {typeName}[][] {propName} = new {typeName}[][] {{");
            foreach (var arr in allArrays)
            {
                sb.AppendLine($"        new {typeName}[] {{{string.Join(", ", arr)}}},");
            }
            sb.AppendLine("    };");
        }
        else
        {
            sb.AppendLine($"    public static final List<List<{boxedType}>> {propName} = new ArrayList<>(Arrays.asList(");
            for (int i = 0; i < allArrays.Count; i++)
            {
                var arr = allArrays[i];
                sb.AppendLine($"        new ArrayList<>(Arrays.asList({string.Join(", ", arr)})){(i < allArrays.Count - 1 ? "," : "")}");
            }
            sb.AppendLine("    ));");
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
            if (trimmed == "    ));")
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex < 0)
            throw new InvalidOperationException("Mevcut dosyada Items listesinin kapanışı bulunamadı.");

        for (int i = insertIndex - 1; i >= 0; i--)
        {
            var trimmed = lines[i].TrimEnd('\r').TrimEnd();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (trimmed.EndsWith(")") && !trimmed.EndsWith("));") && !trimmed.EndsWith("),"))
                    lines[i] = lines[i].TrimEnd('\r').TrimEnd() + ",\r";
                break;
            }
        }

        var sb = new StringBuilder();
        for (int i = 0; i < newData.Rows.Count; i++)
            GenerateObjectCreation(sb, newData, newData.Rows[i], className, "        ", i < newData.Rows.Count - 1);

        var newLines = sb.ToString().Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        lines.InsertRange(insertIndex, newLines);
        return string.Join("\n", lines);
    }

    #region Enum

    private void GenerateEnumDefinitions(StringBuilder sb, CsvFileData data, string outerClassName)
    {
        var enumColumns = data.Columns.Where(c => c.CSharpType == "enum" && c.IsStandalone && c.IsIncluded && IsDefaultEnumName(c)).ToList();
        if (enumColumns.Count == 0) return;

        foreach (var column in enumColumns)
        {
            var enumName = GetEnumTypeName(column);
            var uniqueValues = GetUniqueColumnValues(data, column.ColumnIndex);

            sb.AppendLine($"enum {enumName} {{");
            for (int i = 0; i < uniqueValues.Count; i++)
            {
                var raw = uniqueValues[i];
                var member = SanitizeEnumMember(raw);
                var sep = i < uniqueValues.Count - 1 ? "," : ";";
                sb.AppendLine($"    {member}{sep}");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    #endregion

    #region Fields & Constructor

    private void GenerateFields(StringBuilder sb, CsvFileData data)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var fieldName = ToCamelCase(column.PropertyName);
                var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
                sb.AppendLine($"    public {typeName} {fieldName};");
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupName = ToCamelCase(column.GroupName);
                    var elementType = MapType(column.CSharpType);
                    var boxed = MapBoxedType(column.CSharpType);
                    if (column.CollectionType == GroupCollectionType.Array)
                        sb.AppendLine($"    public {elementType}[] {groupName};");
                    else
                        sb.AppendLine($"    public List<{boxed}> {groupName};");
                }
            }
        }
    }

    private void GenerateConstructor(StringBuilder sb, CsvFileData data, string className)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parameters = new List<string>();
        var assignments = new List<string>();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var fieldName = ToCamelCase(column.PropertyName);
                var typeName = column.CSharpType == "enum" ? GetEnumTypeName(column) : MapType(column.CSharpType);
                parameters.Add($"{typeName} {fieldName}");
                assignments.Add($"        this.{fieldName} = {fieldName};");
            }
            else
            {
                if (processedGroups.Add(column.GroupName))
                {
                    var groupName = ToCamelCase(column.GroupName);
                    var boxed = MapBoxedType(column.CSharpType);
                    if (column.CollectionType == GroupCollectionType.Array)
                    {
                        var elementType = MapType(column.CSharpType);
                        parameters.Add($"{elementType}[] {groupName}");
                    }
                    else
                    {
                        parameters.Add($"List<{boxed}> {groupName}");
                    }
                    assignments.Add($"        this.{groupName} = {groupName};");
                }
            }
        }

        sb.AppendLine($"    public {className}({string.Join(", ", parameters)}) {{");
        foreach (var assignment in assignments)
            sb.AppendLine(assignment);
        sb.AppendLine("    }");
    }

    #endregion

    #region Data

    private void GenerateFlatData(StringBuilder sb, CsvFileData data, string className)
    {
        sb.AppendLine($"    public static final List<{className}> Items = new ArrayList<>(Arrays.asList(");
        for (int i = 0; i < data.Rows.Count; i++)
            GenerateObjectCreation(sb, data, data.Rows[i], className, "        ", i < data.Rows.Count - 1);
        sb.AppendLine("    ));");
    }

    private void GenerateGroupedData(StringBuilder sb, CsvFileData data, string className, int groupByColumnIndex)
    {
        var groups = BuildGroups(data, groupByColumnIndex);

        sb.AppendLine($"    public static final List<List<{className}>> GroupedItems = new ArrayList<>(Arrays.asList(");
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var group = groups[gi];
            sb.AppendLine($"        // Group: {group.Key}");
            sb.AppendLine("        new ArrayList<>(Arrays.asList(");
            for (int ri = 0; ri < group.Rows.Count; ri++)
                GenerateObjectCreation(sb, data, group.Rows[ri], className, "            ", ri < group.Rows.Count - 1);
            sb.AppendLine($"        )){(gi < groups.Count - 1 ? "," : "")}");
        }
        sb.AppendLine("    ));");
    }

    private void GenerateObjectCreation(StringBuilder sb, CsvFileData data, string[] row, string className, string indent, bool trailing)
    {
        var processedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var args = new List<string>();

        foreach (var column in data.Columns.Where(c => c.IsIncluded))
        {
            if (column.IsStandalone)
            {
                var raw = column.ColumnIndex < row.Length ? row[column.ColumnIndex] : "";
                if (column.CSharpType == "enum")
                {
                    var enumTypeName = GetEnumTypeName(column);
                    var enumMember = SanitizeEnumMember(raw.Trim());
                    args.Add($"{enumTypeName}.{enumMember}");
                }
                else
                {
                    args.Add(FormatValue(raw, column.CSharpType));
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

                    if (column.CollectionType == GroupCollectionType.Array)
                        args.Add($"new {MapType(column.CSharpType)}[]{{{string.Join(", ", values)}}}");
                    else
                        args.Add($"new ArrayList<>(Arrays.asList({string.Join(", ", values)}))");
                }
            }
        }

        sb.AppendLine($"{indent}new {className}({string.Join(", ", args)}){(trailing ? "," : "")}");
    }

    #endregion

    #region Helpers

    private static string MapType(string csharpType) => csharpType switch
    {
        "string" or "String" => "String",
        "int" or "int?" => "int",
        "Integer" => "Integer",
        "long" or "long?" or "Long" => "long",
        "float" or "float?" or "Float" => "float",
        "double" or "double?" or "Double" or "decimal" or "decimal?" => "double",
        "boolean" or "bool" or "bool?" or "Boolean" => "boolean",
        "char" or "char?" or "Character" => "char",
        "byte" or "byte?" or "Byte" => "byte",
        "short" or "short?" or "Short" => "short",
        _ => "String"
    };

    private static string MapBoxedType(string csharpType) => csharpType switch
    {
        "string" or "String" => "String",
        "int" or "int?" or "Integer" => "Integer",
        "long" or "long?" or "Long" => "Long",
        "float" or "float?" or "Float" => "Float",
        "double" or "double?" or "Double" or "decimal" or "decimal?" => "Double",
        "boolean" or "bool" or "bool?" or "Boolean" => "Boolean",
        "char" or "char?" or "Character" => "Character",
        "byte" or "byte?" or "Byte" => "Byte",
        "short" or "short?" or "Short" => "Short",
        _ => "String"
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
                "String" => "\"\"",
                "boolean" => "false",
                "char" => "'\\0'",
                _ => "0"
            };
        }

        return mapped switch
        {
            "String" => $"\"{EscapeString(trimmed)}\"",
            "int" or "byte" or "short" => NormalizeDecimalSeparator(trimmed).Split('.')[0],
            "long" => $"{NormalizeDecimalSeparator(trimmed).Split('.')[0]}L",
            "float" => $"{NormalizeDecimalSeparator(trimmed)}f",
            "double" => NormalizeDecimalSeparator(trimmed),
            "boolean" => trimmed.ToLowerInvariant() is "1" or "true" or "yes" or "evet" ? "true" : "false",
            "char" => trimmed.Length > 0 ? $"'{trimmed[0]}'" : "'\\0'",
            _ => $"\"{EscapeString(trimmed)}\""
        };
    }

    private static string ToCamelCase(string name)
    {
        var sanitized = SanitizeIdentifier(name);
        if (string.IsNullOrEmpty(sanitized)) return "item";
        return char.ToLower(sanitized[0]) + sanitized[1..];
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
