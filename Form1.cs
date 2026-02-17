using Csv2Code.Models;
using Csv2Code.Services;

namespace Csv2Code
{
    public partial class Form1 : Form
    {
        private readonly List<CsvFileData> _loadedFiles = new();
        private CsvFileData? _selectedFile;

        public Form1()
        {
            InitializeComponent();
            WireEvents();
        }

        #region Event Wiring

        private void WireEvents()
        {
            btnImportFile.Click += BtnImportFile_Click;
            btnImportFolder.Click += BtnImportFolder_Click;
            btnBrowseExport.Click += BtnBrowseExport_Click;
            btnPreview.Click += BtnPreview_Click;
            btnSave.Click += BtnSave_Click;
            btnCopyCode.Click += BtnCopyCode_Click;
            btnAppendToFile.Click += BtnAppendToFile_Click;
            lstFiles.SelectedIndexChanged += LstFiles_SelectedIndexChanged;
            dgvColumns.CellValueChanged += DgvColumns_CellValueChanged;
            dgvColumns.CurrentCellDirtyStateChanged += DgvColumns_CurrentCellDirtyStateChanged;
        }

        #endregion

        #region Import

        private void BtnImportFile_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "CSV Dosya Seç",
                Filter = "CSV Dosyaları (*.csv)|*.csv|Tüm Dosyalar (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            foreach (var filePath in dialog.FileNames)
            {
                ImportSingleFile(filePath);
            }
        }

        private void BtnImportFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "CSV dosyalarının bulunduğu klasörü seçin",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                var files = CsvParserService.ParseFolder(dialog.SelectedPath);
                foreach (var file in files)
                {
                    AddFileToList(file);
                }

                UpdateStatus($"{files.Count} dosya yüklendi: {dialog.SelectedPath}");
            }
            catch (Exception ex)
            {
                ShowError("Klasör okuma hatası", ex.Message);
            }
        }

        private void ImportSingleFile(string filePath)
        {
            try
            {
                // Aynı dosya zaten yüklü mü kontrol et
                if (_loadedFiles.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    UpdateStatus($"Dosya zaten yüklü: {Path.GetFileName(filePath)}");
                    return;
                }

                var data = CsvParserService.ParseFile(filePath);
                AddFileToList(data);
                UpdateStatus($"Dosya yüklendi: {data.FileName} ({data.Rows.Count} satır, {data.Columns.Count} kolon)");
            }
            catch (Exception ex)
            {
                ShowError("Dosya okuma hatası", ex.Message);
            }
        }

        private void AddFileToList(CsvFileData data)
        {
            // Aynı dosya zaten yüklü mü kontrol et
            if (_loadedFiles.Any(f => f.FilePath.Equals(data.FilePath, StringComparison.OrdinalIgnoreCase)))
                return;

            _loadedFiles.Add(data);
            lstFiles.Items.Add(data.FileName);
            tsslFileCount.Text = $"{_loadedFiles.Count} dosya";

            // İlk dosya ise otomatik seç
            if (lstFiles.SelectedIndex < 0)
                lstFiles.SelectedIndex = 0;
        }

        #endregion

        #region File Selection

        private void LstFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstFiles.SelectedIndex < 0 || lstFiles.SelectedIndex >= _loadedFiles.Count)
                return;

            _selectedFile = _loadedFiles[lstFiles.SelectedIndex];
            LoadFileToUI(_selectedFile);
        }

        private void LoadFileToUI(CsvFileData data)
        {
            // Class adını dosya adından oluştur
            txtClassName.Text = SanitizeClassName(data.FileName);

            // Group By dropdown'unu doldur
            PopulateGroupByDropdown(data);

            // Kolon ayarlarını yükle
            LoadColumnsGrid(data);

            // Veri önizlemesini yükle
            LoadDataPreview(data);

            // Kod önizlemesini temizle
            rtbCodePreview.Clear();
        }

        private void PopulateGroupByDropdown(CsvFileData data)
        {
            cmbGroupBy.Items.Clear();
            cmbGroupBy.Items.Add("(Gruplama Yok)");
            foreach (var col in data.Columns)
            {
                cmbGroupBy.Items.Add(col.OriginalName);
            }
            cmbGroupBy.SelectedIndex = 0;
        }

        private void LoadColumnsGrid(CsvFileData data)
        {
            dgvColumns.Rows.Clear();

            foreach (var column in data.Columns)
            {
                var rowIndex = dgvColumns.Rows.Add();
                var row = dgvColumns.Rows[rowIndex];

                row.Cells["colOriginalName"].Value = column.OriginalName;
                row.Cells["colPropertyName"].Value = column.PropertyName;
                row.Cells["colCSharpType"].Value = column.CSharpType;
                row.Cells["colGroupName"].Value = column.GroupName;
                row.Cells["colCollectionType"].Value = column.CollectionType.ToString();

                // İlk satırdan örnek değer al
                var sampleValue = data.Rows.Count > 0 && column.ColumnIndex < data.Rows[0].Length
                    ? data.Rows[0][column.ColumnIndex]
                    : "";
                row.Cells["colSampleValue"].Value = sampleValue;
            }
        }

        private void LoadDataPreview(CsvFileData data)
        {
            dgvDataPreview.Columns.Clear();
            dgvDataPreview.Rows.Clear();

            // Kolon başlıkları
            foreach (var col in data.Columns)
            {
                dgvDataPreview.Columns.Add(col.PropertyName, col.OriginalName);
            }

            // Satırları ekle (max 100 satır önizleme)
            var maxRows = Math.Min(data.Rows.Count, 100);
            for (int i = 0; i < maxRows; i++)
            {
                dgvDataPreview.Rows.Add(data.Rows[i]);
            }
        }

        #endregion

        #region Column Settings

        private void DgvColumns_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            // ComboBox değişikliklerinin anında uygulanması için
            if (dgvColumns.IsCurrentCellDirty)
            {
                dgvColumns.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvColumns_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_selectedFile == null || e.RowIndex < 0 || e.RowIndex >= _selectedFile.Columns.Count)
                return;

            var column = _selectedFile.Columns[e.RowIndex];

            if (e.ColumnIndex == dgvColumns.Columns["colPropertyName"]!.Index)
            {
                var newName = dgvColumns.Rows[e.RowIndex].Cells["colPropertyName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newName))
                    column.PropertyName = newName;
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colCSharpType"]!.Index)
            {
                var newType = dgvColumns.Rows[e.RowIndex].Cells["colCSharpType"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newType))
                    column.CSharpType = newType;
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colGroupName"]!.Index)
            {
                column.GroupName = dgvColumns.Rows[e.RowIndex].Cells["colGroupName"].Value?.ToString() ?? "";
                // Grup adı girildiğinde koleksiyon tipi None ise otomatik List yap
                if (!string.IsNullOrWhiteSpace(column.GroupName) && column.CollectionType == GroupCollectionType.None)
                {
                    column.CollectionType = GroupCollectionType.List;
                    dgvColumns.Rows[e.RowIndex].Cells["colCollectionType"].Value = "List";
                }
                // Grup adı temizlenirse koleksiyon tipini de temizle
                if (string.IsNullOrWhiteSpace(column.GroupName))
                {
                    column.CollectionType = GroupCollectionType.None;
                    dgvColumns.Rows[e.RowIndex].Cells["colCollectionType"].Value = "None";
                }
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colCollectionType"]!.Index)
            {
                var colType = dgvColumns.Rows[e.RowIndex].Cells["colCollectionType"].Value?.ToString() ?? "None";
                column.CollectionType = colType switch
                {
                    "List" => GroupCollectionType.List,
                    "Array" => GroupCollectionType.Array,
                    _ => GroupCollectionType.None
                };
            }
        }

        #endregion

        #region Code Generation

        private void BtnPreview_Click(object? sender, EventArgs e)
        {
            if (_selectedFile == null)
            {
                ShowWarning("Önce bir CSV dosyası seçin.");
                return;
            }

            SyncColumnsFromGrid();
            var groupByIndex = GetGroupByColumnIndex();
            var code = CodeGeneratorService.GenerateCode(
                _selectedFile,
                txtClassName.Text,
                txtNamespace.Text,
                groupByIndex
            );

            ApplySyntaxHighlighting(code);
            UpdateStatus("Kod önizlemesi oluşturuldu.");
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_selectedFile == null)
            {
                ShowWarning("Önce bir CSV dosyası seçin.");
                return;
            }

            var exportPath = txtExportPath.Text.Trim();
            if (string.IsNullOrEmpty(exportPath))
            {
                // SaveFileDialog ile sor
                using var dialog = new SaveFileDialog
                {
                    Title = "C# Dosyasını Kaydet",
                    Filter = "C# Dosyaları (*.cs)|*.cs",
                    FileName = $"{txtClassName.Text}.cs"
                };

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                exportPath = dialog.FileName;
                txtExportPath.Text = Path.GetDirectoryName(exportPath) ?? "";
            }
            else
            {
                // Klasör yolu ise dosya adı ekle
                if (Directory.Exists(exportPath))
                {
                    exportPath = Path.Combine(exportPath, $"{txtClassName.Text}.cs");
                }
                else if (!exportPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    exportPath = Path.Combine(exportPath, $"{txtClassName.Text}.cs");
                    var dir = Path.GetDirectoryName(exportPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }
            }

            try
            {
                SyncColumnsFromGrid();
                var groupByIndex = GetGroupByColumnIndex();
                var code = CodeGeneratorService.GenerateCode(
                    _selectedFile,
                    txtClassName.Text,
                    txtNamespace.Text,
                    groupByIndex
                );

                File.WriteAllText(exportPath, code, System.Text.Encoding.UTF8);
                ApplySyntaxHighlighting(code);
                UpdateStatus($"✅ Dosya kaydedildi: {exportPath}");

                MessageBox.Show(
                    $"Dosya başarıyla kaydedildi:\n{exportPath}",
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                ShowError("Kaydetme hatası", ex.Message);
            }
        }

        private void BtnCopyCode_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(rtbCodePreview.Text))
            {
                ShowWarning("Önce 'Önizle' butonuyla kod üretin.");
                return;
            }

            Clipboard.SetText(rtbCodePreview.Text);
            UpdateStatus("📋 Kod panoya kopyalandı.");
        }

        private void BtnAppendToFile_Click(object? sender, EventArgs e)
        {
            if (_selectedFile == null)
            {
                ShowWarning("Önce bir CSV dosyası seçin.");
                return;
            }

            using var dialog = new OpenFileDialog
            {
                Title = "Mevcut C# Dosyasını Seçin",
                Filter = "C# Dosyaları (*.cs)|*.cs",
                Multiselect = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                SyncColumnsFromGrid();
                var existingContent = File.ReadAllText(dialog.FileName, System.Text.Encoding.UTF8);
                var updatedCode = CodeGeneratorService.AppendToExistingFile(
                    existingContent,
                    _selectedFile,
                    txtClassName.Text
                );

                File.WriteAllText(dialog.FileName, updatedCode, System.Text.Encoding.UTF8);
                ApplySyntaxHighlighting(updatedCode);
                UpdateStatus($"✅ {_selectedFile.Rows.Count} satır mevcut dosyaya eklendi: {dialog.FileName}");

                MessageBox.Show(
                    $"{_selectedFile.Rows.Count} satır başarıyla eklendi:\n{dialog.FileName}",
                    "Başarılı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                ShowError("Dosyaya ekleme hatası", ex.Message);
            }
        }

        /// <summary>
        /// Grid'deki değişiklikleri model'e senkronize eder.
        /// </summary>
        private void SyncColumnsFromGrid()
        {
            if (_selectedFile == null) return;

            for (int i = 0; i < dgvColumns.Rows.Count && i < _selectedFile.Columns.Count; i++)
            {
                var row = dgvColumns.Rows[i];
                var column = _selectedFile.Columns[i];

                var propName = row.Cells["colPropertyName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(propName))
                    column.PropertyName = propName;

                var csharpType = row.Cells["colCSharpType"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(csharpType))
                    column.CSharpType = csharpType;

                column.GroupName = row.Cells["colGroupName"].Value?.ToString() ?? "";

                var colTypeStr = row.Cells["colCollectionType"].Value?.ToString() ?? "None";
                column.CollectionType = colTypeStr switch
                {
                    "List" => GroupCollectionType.List,
                    "Array" => GroupCollectionType.Array,
                    _ => GroupCollectionType.None
                };
            }
        }

        /// <summary>
        /// Group By dropdown'undan seçilen kolonun index'ini döndürür. -1 = gruplama yok.
        /// </summary>
        private int GetGroupByColumnIndex()
        {
            if (_selectedFile == null || cmbGroupBy.SelectedIndex <= 0)
                return -1;

            // İlk item "(Gruplama Yok)" olduğu için index - 1
            return cmbGroupBy.SelectedIndex - 1;
        }

        #endregion

        #region Export Path

        private void BtnBrowseExport_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Kayıt klasörünü seçin",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtExportPath.Text = dialog.SelectedPath;
            }
        }

        #endregion

        #region Syntax Highlighting

        private void ApplySyntaxHighlighting(string code)
        {
            rtbCodePreview.SuspendLayout();
            rtbCodePreview.Clear();
            rtbCodePreview.Text = code;

            // Tüm metni varsayılan renge ayarla
            rtbCodePreview.SelectAll();
            rtbCodePreview.SelectionColor = Color.FromArgb(220, 220, 240);

            // Keyword'ler — mavi
            var keywords = new[]
            {
                "namespace", "public", "class", "static", "readonly",
                "new", "get", "set", "null", "true", "false", "using"
            };
            HighlightWords(keywords, Color.FromArgb(86, 156, 214));

            // Tipler — açık yeşil
            var typeKeywords = new[]
            {
                "string", "int", "long", "float", "double", "decimal",
                "bool", "char", "byte", "short", "List", "void",
                "int?", "long?", "float?", "double?", "decimal?",
                "bool?", "char?", "byte?", "short?"
            };
            HighlightWords(typeKeywords, Color.FromArgb(78, 201, 176));

            // String literal'ler — turuncu
            HighlightPattern(code, "\"", "\"", Color.FromArgb(214, 157, 133));

            // Sayılar
            HighlightNumbers(code, Color.FromArgb(181, 206, 168));

            // Region yorumları — gri
            HighlightLineStarting(code, "#region", Color.FromArgb(128, 128, 128));
            HighlightLineStarting(code, "#endregion", Color.FromArgb(128, 128, 128));

            rtbCodePreview.SelectionStart = 0;
            rtbCodePreview.SelectionLength = 0;
            rtbCodePreview.ResumeLayout();
        }

        private void HighlightWords(string[] words, Color color)
        {
            foreach (var word in words)
            {
                int startIndex = 0;
                var text = rtbCodePreview.Text;

                while (startIndex < text.Length)
                {
                    int index = text.IndexOf(word, startIndex, StringComparison.Ordinal);
                    if (index < 0) break;

                    // Tam kelime kontrolü
                    bool validBefore = index == 0 || !char.IsLetterOrDigit(text[index - 1]) && text[index - 1] != '_';
                    int afterIndex = index + word.Length;

                    // ? ile biten tiplerde özel kontrol
                    bool validAfter;
                    if (word.EndsWith("?"))
                    {
                        validAfter = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]) && text[afterIndex] != '_';
                    }
                    else
                    {
                        validAfter = afterIndex >= text.Length || !char.IsLetterOrDigit(text[afterIndex]) && text[afterIndex] != '_' && text[afterIndex] != '?';
                    }

                    if (validBefore && validAfter)
                    {
                        rtbCodePreview.Select(index, word.Length);
                        rtbCodePreview.SelectionColor = color;
                    }

                    startIndex = index + word.Length;
                }
            }
        }

        private void HighlightPattern(string text, string start, string end, Color color)
        {
            int index = 0;
            while (index < text.Length)
            {
                int startPos = text.IndexOf(start, index, StringComparison.Ordinal);
                if (startPos < 0) break;

                int endPos = text.IndexOf(end, startPos + start.Length, StringComparison.Ordinal);
                if (endPos < 0) break;

                int length = endPos - startPos + end.Length;
                rtbCodePreview.Select(startPos, length);
                rtbCodePreview.SelectionColor = color;

                index = endPos + end.Length;
            }
        }

        private void HighlightNumbers(string text, Color color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    bool validBefore = i == 0 || !char.IsLetter(text[i - 1]) && text[i - 1] != '_';
                    if (!validBefore) continue;

                    int start = i;
                    while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '.' || text[i] == 'f' || text[i] == 'd' || text[i] == 'm' || text[i] == 'L'))
                        i++;

                    // Sayıdan sonra harf gelmemeli
                    if (i < text.Length && char.IsLetter(text[i]) && text[i] != 'f' && text[i] != 'd' && text[i] != 'm' && text[i] != 'L')
                        continue;

                    rtbCodePreview.Select(start, i - start);
                    rtbCodePreview.SelectionColor = color;
                }
            }
        }

        private void HighlightLineStarting(string text, string prefix, Color color)
        {
            var lines = text.Split('\n');
            int pos = 0;
            foreach (var line in lines)
            {
                var trimmedLine = line.TrimStart();
                if (trimmedLine.StartsWith(prefix))
                {
                    int offset = line.Length - trimmedLine.Length;
                    rtbCodePreview.Select(pos + offset, trimmedLine.Length);
                    rtbCodePreview.SelectionColor = color;
                }
                pos += line.Length + 1; // +1 for \n
            }
        }

        #endregion

        #region Helpers

        private static string SanitizeClassName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "GeneratedClass";

            var sb = new System.Text.StringBuilder();
            bool nextUpper = true;

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(nextUpper ? char.ToUpper(c) : c);
                    nextUpper = false;
                }
                else
                {
                    nextUpper = true;
                }
            }

            var result = sb.ToString();
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "GeneratedClass" : result;
        }

        private void UpdateStatus(string message)
        {
            tsslStatus.Text = message;
        }

        private static void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        #endregion
    }
}
