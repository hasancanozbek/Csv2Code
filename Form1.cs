using Csv2Code.Models;
using Csv2Code.Services;

namespace Csv2Code
{
    public partial class Form1 : Form
    {
        private readonly List<CsvFileData> _loadedFiles = new();
        private CsvFileData? _selectedFile;
        private GenerationMode _currentMode = GenerationMode.Object;

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
            btnRemoveFile.Click += BtnRemoveFile_Click;
            btnBrowseExport.Click += BtnBrowseExport_Click;
            btnPreview.Click += BtnPreview_Click;
            btnSave.Click += BtnSave_Click;
            btnCopyCode.Click += BtnCopyCode_Click;
            btnAppendToFile.Click += BtnAppendToFile_Click;
            lstFiles.SelectedIndexChanged += LstFiles_SelectedIndexChanged;
            dgvColumns.CellValueChanged += DgvColumns_CellValueChanged;
            dgvColumns.CurrentCellDirtyStateChanged += DgvColumns_CurrentCellDirtyStateChanged;
            dgvColumns.SelectionChanged += DgvColumns_SelectionChanged;
            dgvListColumns.CellValueChanged += DgvListColumns_CellValueChanged;
            dgvListColumns.CurrentCellDirtyStateChanged += DgvListColumns_CurrentCellDirtyStateChanged;
            txtEnumName.TextChanged += TxtEnumName_TextChanged;
            cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
            tabMode.SelectedIndexChanged += TabMode_SelectedIndexChanged;
        }

        #endregion

        #region File Management

        private void BtnRemoveFile_Click(object? sender, EventArgs e)
        {
            if (lstFiles.SelectedIndex < 0) return;

            var index = lstFiles.SelectedIndex;
            var file = _loadedFiles[index];

            _loadedFiles.RemoveAt(index);
            lstFiles.Items.RemoveAt(index);

            if (_selectedFile == file)
            {
                _selectedFile = null;
                dgvColumns.Rows.Clear();
                dgvListColumns.Rows.Clear();
                dgvDataPreview.Columns.Clear();
                dgvDataPreview.Rows.Clear();
                rtbCodePreview.Clear();
                pnlEnumSettings.Visible = false;
            }

            // Varsa bir sonraki veya önceki dosyayı seç
            if (lstFiles.Items.Count > 0)
            {
                lstFiles.SelectedIndex = Math.Min(index, lstFiles.Items.Count - 1);
            }

            UpdateStatus($"🗑️ {file.FileName} kaldırıldı. {_loadedFiles.Count} dosya kaldı.");
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

            // Kolon ayarlarını yükle (her iki mod için)
            LoadColumnsGrid(data);
            LoadListColumnsGrid(data);

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
                if (!col.IsIncluded) continue;
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

                row.Cells["colInclude"].Value = column.IsIncluded;
                row.Cells["colOriginalName"].Value = column.OriginalName;
                row.Cells["colPropertyName"].Value = column.PropertyName;
                row.Cells["colCSharpType"].Value = column.CSharpType;
                row.Cells["colGroupName"].Value = column.GroupName;
                row.Cells["colCollectionType"].Value = column.CollectionType.ToString();
            }

            pnlEnumSettings.Visible = false;
        }

        private void LoadListColumnsGrid(CsvFileData data)
        {
            dgvListColumns.Rows.Clear();

            foreach (var column in data.Columns)
            {
                var rowIndex = dgvListColumns.Rows.Add();
                var row = dgvListColumns.Rows[rowIndex];

                row.Cells["colListInclude"].Value = column.IsIncluded;
                row.Cells["colListOriginalName"].Value = column.OriginalName;
                row.Cells["colListVarName"].Value = column.PropertyName;
                row.Cells["colListType"].Value = column.CSharpType;
                row.Cells["colListCollectionType"].Value = column.ListCollectionType == GroupCollectionType.Array ? "Array" : "List";
                row.Cells["colListUnique"].Value = column.IsUniqueList;
                row.Cells["colListSortOrder"].Value = column.ListSortOrder.ToString();
            }
        }

        /// <summary>
        /// Seçili satır değiştiğinde enum panelini göster/gizle.
        /// </summary>
        private void DgvColumns_SelectionChanged(object? sender, EventArgs e)
        {
            if (_selectedFile == null || dgvColumns.CurrentRow == null)
            {
                pnlEnumSettings.Visible = false;
                return;
            }

            var rowIndex = dgvColumns.CurrentRow.Index;
            if (rowIndex < 0 || rowIndex >= _selectedFile.Columns.Count)
            {
                pnlEnumSettings.Visible = false;
                return;
            }

            var column = _selectedFile.Columns[rowIndex];
            if (column.CSharpType == "enum")
            {
                pnlEnumSettings.Visible = true;
                // Default enum adı ata eğer boşsa
                if (string.IsNullOrWhiteSpace(column.EnumName))
                    column.EnumName = Services.Generators.CodeGeneratorBase.SanitizeIdentifier(column.PropertyName) + "Type";

                txtEnumName.Text = column.EnumName;
            }
            else
            {
                pnlEnumSettings.Visible = false;
            }
        }

        /// <summary>
        /// Enum adı değiştiğinde modeli güncelle.
        /// </summary>
        private void TxtEnumName_TextChanged(object? sender, EventArgs e)
        {
            if (_selectedFile == null || dgvColumns.CurrentRow == null) return;

            var rowIndex = dgvColumns.CurrentRow.Index;
            if (rowIndex < 0 || rowIndex >= _selectedFile.Columns.Count) return;

            var column = _selectedFile.Columns[rowIndex];
            if (column.CSharpType == "enum")
                column.EnumName = txtEnumName.Text;
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

        #region Tab Mode

        private void TabMode_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var bgDark = Color.FromArgb(30, 30, 46);
            var bgPanel = Color.FromArgb(40, 40, 60);
            var bgInactive = Color.FromArgb(45, 45, 65);
            var accentBlue = Color.FromArgb(100, 140, 255);
            var accentPurple = Color.FromArgb(160, 120, 255);
            var textLight = Color.FromArgb(230, 230, 245);
            var textMuted = Color.FromArgb(140, 140, 170);

            var tabPage = tabMode.TabPages[e.Index];
            var bounds = tabMode.GetTabRect(e.Index);
            bool isSelected = tabMode.SelectedIndex == e.Index;

            // İç margin — tab'lar arası boşluk efekti
            const int hMargin = 3;
            const int topMargin = 3;
            var innerBounds = new Rectangle(
                bounds.Left + hMargin,
                bounds.Top + topMargin,
                bounds.Width - hMargin * 2,
                bounds.Height - topMargin
            );

            // Tab arka planı (iç alan)
            var tabBgColor = isSelected ? bgDark : bgInactive;
            using var bgBrush = new SolidBrush(tabBgColor);
            e.Graphics.FillRectangle(bgBrush, innerBounds);

            // Seçili tab — alt tarafına gradient accent bar
            if (isSelected)
            {
                var accentRect = new Rectangle(innerBounds.Left, innerBounds.Bottom - 3, innerBounds.Width, 3);
                using var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    accentRect, accentBlue, accentPurple,
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(gradientBrush, accentRect);
            }
            else
            {
                // Seçili olmayan tab — üstüne hafif border
                using var borderPen = new Pen(Color.FromArgb(55, 55, 80), 1);
                e.Graphics.DrawRectangle(borderPen, innerBounds);
            }

            // Tab metni
            var textColor = isSelected ? textLight : textMuted;
            using var textBrush = new SolidBrush(textColor);
            var textFont = isSelected
                ? new Font("Segoe UI", 10F, FontStyle.Bold)
                : new Font("Segoe UI", 9.5F, FontStyle.Regular);
            var textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            e.Graphics.DrawString(tabPage.Text, textFont, textBrush, innerBounds, textFormat);
            textFont.Dispose();
        }

        private void TabMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _currentMode = tabMode.SelectedIndex == 0 ? GenerationMode.Object : GenerationMode.List;

            // Mod'a göre label güncelle
            lblClassName.Text = _currentMode == GenerationMode.Object ? "Sınıf Adı:" : "Değişken Prefix:";

            // GroupBy sadece Object modda görünsün
            cmbGroupBy.Visible = _currentMode == GenerationMode.Object;
            lblGroupBy.Visible = _currentMode == GenerationMode.Object;

            // Tüm tab başlıklarının yeniden çizilmesini sağla
            tabMode.Invalidate();

            // Kod önizlemesini temizle
            rtbCodePreview.Clear();
        }

        #endregion

        #region Column Settings — Obje Modu

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
            var row = dgvColumns.Rows[e.RowIndex];

            if (e.ColumnIndex == dgvColumns.Columns["colInclude"]!.Index)
            {
                column.IsIncluded = row.Cells["colInclude"].Value is true;
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colPropertyName"]!.Index)
            {
                var newName = row.Cells["colPropertyName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newName))
                    column.PropertyName = newName;
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colCSharpType"]!.Index)
            {
                var newType = row.Cells["colCSharpType"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newType))
                    column.CSharpType = newType;

                // Enum seçildi/kaldırıldığında enum panelini güncelle
                if (newType == "enum")
                {
                    if (string.IsNullOrWhiteSpace(column.EnumName))
                        column.EnumName = Services.Generators.CodeGeneratorBase.SanitizeIdentifier(column.PropertyName) + "Type";
                    pnlEnumSettings.Visible = true;
                    txtEnumName.Text = column.EnumName;
                }
                else
                {
                    column.EnumName = "";
                    pnlEnumSettings.Visible = false;
                }
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colGroupName"]!.Index)
            {
                column.GroupName = row.Cells["colGroupName"].Value?.ToString() ?? "";
                // Grup adı girildiğinde koleksiyon tipi None ise otomatik List yap
                if (!string.IsNullOrWhiteSpace(column.GroupName) && column.CollectionType == GroupCollectionType.None)
                {
                    column.CollectionType = GroupCollectionType.List;
                    row.Cells["colCollectionType"].Value = "List";
                }
                // Grup adı temizlenirse koleksiyon tipini de temizle
                if (string.IsNullOrWhiteSpace(column.GroupName))
                {
                    column.CollectionType = GroupCollectionType.None;
                    row.Cells["colCollectionType"].Value = "None";
                }
            }
            else if (e.ColumnIndex == dgvColumns.Columns["colCollectionType"]!.Index)
            {
                var colType = row.Cells["colCollectionType"].Value?.ToString() ?? "None";
                column.CollectionType = colType switch
                {
                    "List" => GroupCollectionType.List,
                    "Array" => GroupCollectionType.Array,
                    _ => GroupCollectionType.None
                };
            }
        }

        #endregion

        #region Column Settings — Liste Modu

        private void DgvListColumns_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvListColumns.IsCurrentCellDirty)
            {
                dgvListColumns.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvListColumns_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_selectedFile == null || e.RowIndex < 0 || e.RowIndex >= _selectedFile.Columns.Count)
                return;

            var column = _selectedFile.Columns[e.RowIndex];
            var row = dgvListColumns.Rows[e.RowIndex];

            if (e.ColumnIndex == dgvListColumns.Columns["colListInclude"]!.Index)
            {
                column.IsIncluded = row.Cells["colListInclude"].Value is true;
            }
            else if (e.ColumnIndex == dgvListColumns.Columns["colListVarName"]!.Index)
            {
                var newName = row.Cells["colListVarName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newName))
                    column.PropertyName = newName;
            }
            else if (e.ColumnIndex == dgvListColumns.Columns["colListType"]!.Index)
            {
                var newType = row.Cells["colListType"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(newType))
                    column.CSharpType = newType;
            }
            else if (e.ColumnIndex == dgvListColumns.Columns["colListCollectionType"]!.Index)
            {
                var colType = row.Cells["colListCollectionType"].Value?.ToString() ?? "List";
                column.ListCollectionType = colType switch
                {
                    "Array" => GroupCollectionType.Array,
                    _ => GroupCollectionType.List
                };
            }
            else if (e.ColumnIndex == dgvListColumns.Columns["colListUnique"]!.Index)
            {
                column.IsUniqueList = row.Cells["colListUnique"].Value is true;
            }
            else if (e.ColumnIndex == dgvListColumns.Columns["colListSortOrder"]!.Index)
            {
                var sortStr = row.Cells["colListSortOrder"].Value?.ToString() ?? "None";
                column.ListSortOrder = sortStr switch
                {
                    "Ascending" => Models.SortOrder.Ascending,
                    "Descending" => Models.SortOrder.Descending,
                    _ => Models.SortOrder.None
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
            var lang = GetSelectedLanguage();
            string code;

            if (_currentMode == GenerationMode.Object)
            {
                var groupByIndex = GetGroupByColumnIndex();
                code = CodeGeneratorService.GenerateCode(
                    _selectedFile,
                    txtClassName.Text,
                    txtNamespace.Text,
                    groupByIndex,
                    lang
                );
            }
            else
            {
                SyncListColumnsFromGrid();
                code = CodeGeneratorService.GenerateListCode(
                    _selectedFile,
                    txtClassName.Text,
                    txtNamespace.Text,
                    lang
                );
            }

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
            var lang = GetSelectedLanguage();
            var ext = CodeGeneratorService.GetFileExtension(lang);
            var generator = CodeGeneratorService.GetGenerator(lang);
            var langName = generator.LanguageName;

            if (string.IsNullOrEmpty(exportPath))
            {
                using var dialog = new SaveFileDialog
                {
                    Title = $"{langName} Dosyasını Kaydet",
                    Filter = $"{langName} Dosyaları (*{ext})|*{ext}",
                    FileName = $"{txtClassName.Text}{ext}"
                };

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                exportPath = dialog.FileName;
                txtExportPath.Text = Path.GetDirectoryName(exportPath) ?? "";
            }
            else
            {
                if (Directory.Exists(exportPath))
                {
                    exportPath = Path.Combine(exportPath, $"{txtClassName.Text}{ext}");
                }
                else if (!exportPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    exportPath = Path.Combine(exportPath, $"{txtClassName.Text}{ext}");
                    var dir = Path.GetDirectoryName(exportPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }
            }

            try
            {
                SyncColumnsFromGrid();
                string code;

                if (_currentMode == GenerationMode.Object)
                {
                    var groupByIndex = GetGroupByColumnIndex();
                    code = CodeGeneratorService.GenerateCode(
                        _selectedFile,
                        txtClassName.Text,
                        txtNamespace.Text,
                        groupByIndex,
                        lang
                    );
                }
                else
                {
                    SyncListColumnsFromGrid();
                    code = CodeGeneratorService.GenerateListCode(
                        _selectedFile,
                        txtClassName.Text,
                        txtNamespace.Text,
                        lang
                    );
                }

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

            var lang = GetSelectedLanguage();
            var ext = CodeGeneratorService.GetFileExtension(lang);
            var generator = CodeGeneratorService.GetGenerator(lang);

            using var dialog = new OpenFileDialog
            {
                Title = $"Mevcut {generator.LanguageName} Dosyasını Seçin",
                Filter = $"{generator.LanguageName} Dosyaları (*{ext})|*{ext}",
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
                    txtClassName.Text,
                    lang
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
        /// Grid'deki değişiklikleri model'e senkronize eder (Obje Modu).
        /// </summary>
        private void SyncColumnsFromGrid()
        {
            if (_selectedFile == null) return;

            for (int i = 0; i < dgvColumns.Rows.Count && i < _selectedFile.Columns.Count; i++)
            {
                var row = dgvColumns.Rows[i];
                var column = _selectedFile.Columns[i];

                column.IsIncluded = row.Cells["colInclude"].Value is true;

                var propName = row.Cells["colPropertyName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(propName))
                    column.PropertyName = propName;

                var csharpType = row.Cells["colCSharpType"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(csharpType))
                    column.CSharpType = csharpType;

                column.EnumName = column.CSharpType == "enum" ? column.EnumName : "";

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
        /// Grid'deki değişiklikleri model'e senkronize eder (Liste Modu).
        /// </summary>
        private void SyncListColumnsFromGrid()
        {
            if (_selectedFile == null) return;

            for (int i = 0; i < dgvListColumns.Rows.Count && i < _selectedFile.Columns.Count; i++)
            {
                var row = dgvListColumns.Rows[i];
                var column = _selectedFile.Columns[i];

                column.IsIncluded = row.Cells["colListInclude"].Value is true;

                var varName = row.Cells["colListVarName"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(varName))
                    column.PropertyName = varName;

                var listType = row.Cells["colListType"].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(listType))
                    column.CSharpType = listType;

                var colTypeStr = row.Cells["colListCollectionType"].Value?.ToString() ?? "List";
                column.ListCollectionType = colTypeStr switch
                {
                    "Array" => GroupCollectionType.Array,
                    _ => GroupCollectionType.List
                };

                column.IsUniqueList = row.Cells["colListUnique"].Value is true;

                var sortStr = row.Cells["colListSortOrder"].Value?.ToString() ?? "None";
                column.ListSortOrder = sortStr switch
                {
                    "Ascending" => Models.SortOrder.Ascending,
                    "Descending" => Models.SortOrder.Descending,
                    _ => Models.SortOrder.None
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

        /// <summary>
        /// Dropdown'dan seçili hedef dili döndürür.
        /// </summary>
        private TargetLanguage GetSelectedLanguage()
        {
            return cmbLanguage.SelectedIndex switch
            {
                0 => TargetLanguage.CSharp,
                1 => TargetLanguage.Cpp,
                2 => TargetLanguage.C,
                3 => TargetLanguage.Python,
                4 => TargetLanguage.Java,
                _ => TargetLanguage.CSharp,
            };
        }

        /// <summary>
        /// Dil değiştiğinde tip sütunu seçeneklerini günceller.
        /// </summary>
        private void CmbLanguage_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var lang = GetSelectedLanguage();
            var types = CodeGeneratorService.GetSupportedTypes(lang);

            // dgvColumns'daki colCSharpType ComboBox sütununu güncelle
            if (dgvColumns.Columns["colCSharpType"] is DataGridViewComboBoxColumn typeCol)
            {
                typeCol.Items.Clear();
                foreach (var t in types)
                    typeCol.Items.Add(t);
            }

            // dgvListColumns'daki colListType ComboBox sütununu güncelle
            if (dgvListColumns.Columns["colListType"] is DataGridViewComboBoxColumn listTypeCol)
            {
                listTypeCol.Items.Clear();
                foreach (var t in types)
                    listTypeCol.Items.Add(t);
            }

            // Mevcut satırlardaki seçili tipi kontrol et — eğer yeni dilde yoksa string/varsayılan yap
            for (int i = 0; i < dgvColumns.Rows.Count; i++)
            {
                var cell = dgvColumns.Rows[i].Cells["colCSharpType"];
                var currentType = cell.Value?.ToString() ?? "";
                if (!types.Contains(currentType))
                {
                    cell.Value = types[0]; // ilk tip (string/str/String/...)
                }
            }

            for (int i = 0; i < dgvListColumns.Rows.Count; i++)
            {
                var cell = dgvListColumns.Rows[i].Cells["colListType"];
                var currentType = cell.Value?.ToString() ?? "";
                if (!types.Contains(currentType))
                {
                    cell.Value = types[0];
                }
            }

            // Önizlemeyi temizle
            rtbCodePreview.Clear();
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

    /// <summary>
    /// Dark tema uyumlu TabControl — arka planı otomatik koyu renkle boyar.
    /// </summary>
    internal class DarkTabControl : TabControl
    {
        private static readonly Color BgPanel = Color.FromArgb(40, 40, 60);
        private static readonly Color SeparatorColor = Color.FromArgb(55, 55, 80);

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new SolidBrush(BgPanel);
            e.Graphics.FillRectangle(brush, ClientRectangle);

            // Tab header altına ince bir ayırıcı çizgi
            if (TabCount > 0)
            {
                var tabRect = GetTabRect(0);
                var separatorY = tabRect.Bottom;
                using var pen = new Pen(SeparatorColor, 1);
                e.Graphics.DrawLine(pen, 0, separatorY, Width, separatorY);
            }
        }
    }
}
