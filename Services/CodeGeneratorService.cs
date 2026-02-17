using System.Text;
using Csv2Code.Models;
using Csv2Code.Services.Generators;

namespace Csv2Code.Services;

/// <summary>
/// Facade / Factory — dil seçimine göre uygun generator'ı döndürür.
/// Geriye uyumluluk için eski static metotlar korunmuştur.
/// </summary>
public static class CodeGeneratorService
{
    private static readonly Dictionary<TargetLanguage, CodeGeneratorBase> Generators = new()
    {
        [TargetLanguage.CSharp] = new CSharpGenerator(),
        [TargetLanguage.Cpp] = new CppGenerator(),
        [TargetLanguage.C] = new CGenerator(),
        [TargetLanguage.Python] = new PythonGenerator(),
        [TargetLanguage.Java] = new JavaGenerator(),
    };

    /// <summary>
    /// Seçili dile göre generator'ı döndürür.
    /// </summary>
    public static CodeGeneratorBase GetGenerator(TargetLanguage language)
        => Generators[language];

    /// <summary>
    /// Seçili dil için desteklenen tipler.
    /// </summary>
    public static string[] GetSupportedTypes(TargetLanguage language)
        => GetGenerator(language).SupportedTypes;

    /// <summary>
    /// Seçili dil için dosya uzantısı.
    /// </summary>
    public static string GetFileExtension(TargetLanguage language)
        => GetGenerator(language).FileExtension;

    /// <summary>
    /// Seçili dil için koleksiyon tipi seçenekleri.
    /// </summary>
    public static readonly string[] CollectionTypes = { "None", "List", "Array" };

    /// <summary>
    /// Kod üretir (geriye uyumlu — varsayılan C#).
    /// </summary>
    public static string GenerateCode(CsvFileData data, string className, string namespaceName, int groupByColumnIndex = -1, TargetLanguage language = TargetLanguage.CSharp, int lookupKeyColumnIndex = -1)
        => GetGenerator(language).GenerateCode(data, className, namespaceName, groupByColumnIndex, lookupKeyColumnIndex);

    /// <summary>
    /// Mevcut dosyaya ekleme yapar.
    /// </summary>
    public static string AppendToExistingFile(string existingContent, CsvFileData newData, string className, TargetLanguage language = TargetLanguage.CSharp)
        => GetGenerator(language).AppendToExistingFile(existingContent, newData, className);

    /// <summary>
    /// Append için önizleme kodu üretir.
    /// </summary>
    public static string GenerateAppendPreview(CsvFileData data, string className, TargetLanguage language = TargetLanguage.CSharp)
    {
        className = CodeGeneratorBase.SanitizeIdentifier(className);
        // Delegate to the generator for full code, then just return the data portion
        var fullCode = GetGenerator(language).GenerateCode(data, className, "Preview");
        return $"// === {data.Rows.Count} yeni satır ===\n{fullCode}";
    }

    /// <summary>
    /// Identifier sanitize (geriye uyumluluk).
    /// </summary>
    public static string SanitizeIdentifier(string name)
        => CodeGeneratorBase.SanitizeIdentifier(name);
}
