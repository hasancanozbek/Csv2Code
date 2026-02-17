using Csv2Code.Models;

namespace Csv2Code.Services;

/// <summary>
/// CSV dosyalarını parse eden servis.
/// </summary>
public static class CsvParserService
{
    private static readonly char[] PossibleDelimiters = { ';', ',', '\t', '|' };

    /// <summary>
    /// Tek bir CSV dosyasını parse eder.
    /// </summary>
    public static CsvFileData ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("CSV dosyası bulunamadı.", filePath);

        var lines = ReadAllLinesWithBom(filePath);
        if (lines.Count == 0)
            throw new InvalidDataException("CSV dosyası boş.");

        var delimiter = DetectDelimiter(lines[0]);
        var headerLine = lines[0];
        var headers = SplitCsvLine(headerLine, delimiter);

        var columns = new List<CsvColumn>();
        for (int i = 0; i < headers.Length; i++)
        {
            var name = headers[i].Trim();
            columns.Add(new CsvColumn
            {
                OriginalName = name,
                PropertyName = SanitizePropertyName(name),
                CSharpType = "string",
                ColumnIndex = i
            });
        }

        var rows = new List<string[]>();
        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = SplitCsvLine(line, delimiter);

            // Kolon sayısına göre normalize et
            var normalizedValues = new string[headers.Length];
            for (int j = 0; j < headers.Length; j++)
            {
                normalizedValues[j] = j < values.Length ? values[j].Trim() : string.Empty;
            }
            rows.Add(normalizedValues);
        }

        return new CsvFileData
        {
            FileName = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath,
            Columns = columns,
            Rows = rows,
            Delimiter = delimiter
        };
    }

    /// <summary>
    /// Bir klasördeki tüm CSV dosyalarını parse eder.
    /// </summary>
    public static List<CsvFileData> ParseFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException("Klasör bulunamadı: " + folderPath);

        var csvFiles = Directory.GetFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly);
        if (csvFiles.Length == 0)
            throw new InvalidDataException("Klasörde CSV dosyası bulunamadı.");

        return csvFiles.Select(ParseFile).ToList();
    }

    /// <summary>
    /// Dosyayı BOM desteğiyle okur.
    /// </summary>
    private static List<string> ReadAllLinesWithBom(string filePath)
    {
        using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
        var lines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line != null)
                lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// İlk satırdaki header'a göre delimiter algılar.
    /// En çok tekrar eden delimiter seçilir.
    /// </summary>
    private static char DetectDelimiter(string headerLine)
    {
        char bestDelimiter = ';';
        int maxCount = 0;

        foreach (var delimiter in PossibleDelimiters)
        {
            int count = CountDelimiterOccurrences(headerLine, delimiter);
            if (count > maxCount)
            {
                maxCount = count;
                bestDelimiter = delimiter;
            }
        }

        return bestDelimiter;
    }

    /// <summary>
    /// Tırnak içindeki delimiter'ları saymadan delimiter sayısını hesaplar.
    /// </summary>
    private static int CountDelimiterOccurrences(string line, char delimiter)
    {
        int count = 0;
        bool inQuotes = false;

        foreach (char c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == delimiter && !inQuotes)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Bir CSV satırını tırnak kurallarına uyarak böler.
    /// </summary>
    private static string[] SplitCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote ("" → ")
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    /// <summary>
    /// CSV kolon adını geçerli bir C# property adına dönüştürür.
    /// </summary>
    private static string SanitizePropertyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Column";

        // Boşluk ve geçersiz karakterleri kaldır
        var sanitized = new System.Text.StringBuilder();
        bool nextUpper = true;

        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sanitized.Append(nextUpper ? char.ToUpper(c) : c);
                nextUpper = false;
            }
            else
            {
                nextUpper = true;
            }
        }

        var result = sanitized.ToString();

        // Sayı ile başlıyorsa başına _ ekle
        if (result.Length > 0 && char.IsDigit(result[0]))
            result = "_" + result;

        return string.IsNullOrEmpty(result) ? "Column" : result;
    }
}
