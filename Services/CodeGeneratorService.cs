using System.Text;
using Csv2Code.Models;

namespace Csv2Code.Services;

/// <summary>
/// CSV verisinden C# sınıf kodu üreten servis.
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
    };

    /// <summary>
    /// CSV verisinden C# kodu üretir.
    /// </summary>
    public static string GenerateCode(CsvFileData data, string className, string namespaceName)
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

        // Class
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        // Properties
        sb.AppendLine("        #region Properties");
        sb.AppendLine();
        foreach (var column in data.Columns)
        {
            var typeName = column.CSharpType;
            var propName = SanitizeIdentifier(column.PropertyName);
            sb.AppendLine($"        public {typeName} {propName} {{ get; set; }}");
        }
        sb.AppendLine();
        sb.AppendLine("        #endregion");
        sb.AppendLine();

        // Static data list
        sb.AppendLine($"        #region Data");
        sb.AppendLine();
        sb.AppendLine($"        public static readonly List<{className}> Items = new()");
        sb.AppendLine("        {");

        for (int rowIndex = 0; rowIndex < data.Rows.Count; rowIndex++)
        {
            var row = data.Rows[rowIndex];
            sb.AppendLine($"            new {className}");
            sb.AppendLine("            {");

            for (int colIndex = 0; colIndex < data.Columns.Count; colIndex++)
            {
                var column = data.Columns[colIndex];
                var propName = SanitizeIdentifier(column.PropertyName);
                var rawValue = colIndex < row.Length ? row[colIndex] : string.Empty;
                var formattedValue = FormatValue(rawValue, column.CSharpType);

                var separator = colIndex < data.Columns.Count - 1 ? "," : "";
                sb.AppendLine($"                {propName} = {formattedValue}{separator}");
            }

            var rowSeparator = rowIndex < data.Rows.Count - 1 ? "," : "";
            sb.AppendLine($"            }}{rowSeparator}");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        #endregion");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Ham CSV değerini C# kod değerine dönüştürür.
    /// </summary>
    private static string FormatValue(string rawValue, string csharpType)
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

    /// <summary>
    /// Ondalık sayıyı C# literal formatına dönüştürür.
    /// Virgülü noktaya çevirir.
    /// </summary>
    private static string ConvertToDecimalLiteral(string value, string suffix)
    {
        // Türk formatındaki virgülü noktaya çevir
        var normalized = value.Replace(',', '.');
        return $"{normalized}{suffix}";
    }

    /// <summary>
    /// Tam sayıyı C# literal formatına dönüştürür.
    /// </summary>
    private static string ConvertToNumericLiteral(string value, string suffix)
    {
        // Virgül varsa noktaya çevir (olası ondalık kısmı kaldır)
        var normalized = value.Replace(',', '.');
        if (normalized.Contains('.'))
        {
            // Ondalık kısmı kaldır (int'e dönüşüm)
            var dotIndex = normalized.IndexOf('.');
            normalized = normalized[..dotIndex];
        }
        return string.IsNullOrEmpty(suffix) ? normalized : $"{normalized}{suffix}";
    }

    /// <summary>
    /// Bool değerine dönüştürür (0/1, true/false, yes/no destekler).
    /// </summary>
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

    /// <summary>
    /// String'i C# string literal'i için escape eder.
    /// </summary>
    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Char'ı C# char literal'i için escape eder.
    /// </summary>
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

    /// <summary>
    /// Geçerli bir C# identifier'ına dönüştürür.
    /// </summary>
    private static string SanitizeIdentifier(string name)
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
}
