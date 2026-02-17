namespace Csv2Code
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ===== Renk Paleti =====
            var bgDark = Color.FromArgb(30, 30, 46);
            var bgPanel = Color.FromArgb(40, 40, 60);
            var bgInput = Color.FromArgb(50, 50, 72);
            var accentBlue = Color.FromArgb(100, 140, 255);
            var accentGreen = Color.FromArgb(80, 200, 120);
            var accentOrange = Color.FromArgb(255, 165, 80);
            var textLight = Color.FromArgb(220, 220, 240);
            var textMuted = Color.FromArgb(160, 160, 185);
            var borderColor = Color.FromArgb(65, 65, 90);

            // ===== Form Ayarları =====
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1440, 860);
            Text = "CSV → C# Code Generator";
            BackColor = bgDark;
            ForeColor = textLight;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 700);

            // ================================================================
            //  SOL PANEL — Dosya İşlemleri
            // ================================================================
            pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = bgPanel,
                Padding = new Padding(12)
            };

            lblImportTitle = new Label
            {
                Text = "📂  IMPORT",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = accentBlue,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            btnImportFile = new Button
            {
                Text = "📄  CSV Dosya Seç",
                Dock = DockStyle.Top,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgInput,
                ForeColor = textLight,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 4, 0, 4)
            };
            btnImportFile.FlatAppearance.BorderColor = borderColor;
            btnImportFile.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 90);

            btnImportFolder = new Button
            {
                Text = "📁  Klasör Seç",
                Dock = DockStyle.Top,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgInput,
                ForeColor = textLight,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 4, 0, 4)
            };
            btnImportFolder.FlatAppearance.BorderColor = borderColor;
            btnImportFolder.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 90);

            lblFilesTitle = new Label
            {
                Text = "Yüklenen Dosyalar",
                Dock = DockStyle.Top,
                Height = 28,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(4, 8, 0, 0)
            };

            lstFiles = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = bgInput,
                ForeColor = textLight,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F)
            };

            var pnlLeftSeparator = new Panel
            {
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.Transparent
            };

            // Sol panel alt bölüm — Export ayarları
            pnlExportSettings = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 240,
                BackColor = bgPanel,
                Padding = new Padding(0, 8, 0, 0)
            };

            lblExportTitle = new Label
            {
                Text = "⚙️  SETTINGS",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = accentOrange,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            lblClassName = new Label
            {
                Text = "Sınıf Adı:",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8.5F),
                Padding = new Padding(4, 4, 0, 0)
            };

            txtClassName = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = bgInput,
                ForeColor = textLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Text = "GeneratedClass"
            };

            lblNamespace = new Label
            {
                Text = "Namespace:",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8.5F),
                Padding = new Padding(4, 4, 0, 0)
            };

            txtNamespace = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = bgInput,
                ForeColor = textLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F),
                Text = "Generated"
            };

            lblExportPath = new Label
            {
                Text = "Kayıt Yolu:",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = textMuted,
                Font = new Font("Segoe UI", 8.5F),
                Padding = new Padding(4, 4, 0, 0)
            };

            pnlExportPathRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.Transparent
            };

            txtExportPath = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = bgInput,
                ForeColor = textLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                Text = ""
            };

            btnBrowseExport = new Button
            {
                Dock = DockStyle.Right,
                Width = 36,
                Text = "...",
                FlatStyle = FlatStyle.Flat,
                BackColor = bgInput,
                ForeColor = textLight,
                Cursor = Cursors.Hand
            };
            btnBrowseExport.FlatAppearance.BorderColor = borderColor;

            pnlExportPathRow.Controls.Add(txtExportPath);
            pnlExportPathRow.Controls.Add(btnBrowseExport);

            // Export settings paneline ekle (ters sıra - dock top)
            pnlExportSettings.Controls.Add(pnlExportPathRow);
            pnlExportSettings.Controls.Add(lblExportPath);
            pnlExportSettings.Controls.Add(txtNamespace);
            pnlExportSettings.Controls.Add(lblNamespace);
            pnlExportSettings.Controls.Add(txtClassName);
            pnlExportSettings.Controls.Add(lblClassName);
            pnlExportSettings.Controls.Add(lblExportTitle);

            // Sol panele kontrol ekle (ters sıra)
            pnlLeft.Controls.Add(lstFiles);
            pnlLeft.Controls.Add(lblFilesTitle);
            pnlLeft.Controls.Add(pnlLeftSeparator);
            pnlLeft.Controls.Add(btnImportFolder);
            pnlLeft.Controls.Add(btnImportFile);
            pnlLeft.Controls.Add(lblImportTitle);
            pnlLeft.Controls.Add(pnlExportSettings);

            // Sol panel ayırıcı çizgi
            var leftBorder = new Panel
            {
                Dock = DockStyle.Left,
                Width = 1,
                BackColor = borderColor
            };

            // ================================================================
            //  ORTA + SAĞ ALAN — SplitContainer
            // ================================================================
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = bgDark,
                SplitterWidth = 4,
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };
            splitMain.Panel1.BackColor = bgDark;
            splitMain.Panel2.BackColor = bgDark;

            // ================================================================
            //  ORTA PANEL — Kolon Ayarları + Veri Önizleme (dikey split)
            // ================================================================
            splitCenter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = bgDark,
                SplitterWidth = 4,
                Panel1MinSize = 100,
                Panel2MinSize = 80
            };
            splitCenter.Panel1.BackColor = bgDark;
            splitCenter.Panel2.BackColor = bgDark;

            // --- Kolon Ayarları ---
            lblColumnsTitle = new Label
            {
                Text = "📋  KOLON AYARLARI",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = accentBlue,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            dgvColumns = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgPanel,
                GridColor = borderColor,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9.5F),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = bgPanel,
                    ForeColor = textLight,
                    SelectionBackColor = Color.FromArgb(60, 80, 140),
                    SelectionForeColor = textLight,
                    Padding = new Padding(4, 2, 4, 2)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(45, 45, 68),
                    ForeColor = accentBlue,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Padding = new Padding(4, 4, 4, 4),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36
            };

            // Kolon tanımları
            var colOriginalName = new DataGridViewTextBoxColumn
            {
                Name = "colOriginalName",
                HeaderText = "Orijinal Ad",
                ReadOnly = true,
                FillWeight = 30
            };

            var colPropertyName = new DataGridViewTextBoxColumn
            {
                Name = "colPropertyName",
                HeaderText = "Property Adı",
                ReadOnly = false,
                FillWeight = 30
            };

            var colCSharpType = new DataGridViewComboBoxColumn
            {
                Name = "colCSharpType",
                HeaderText = "C# Tipi",
                FillWeight = 25,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            colCSharpType.Items.AddRange(Services.CodeGeneratorService.SupportedTypes);

            var colSampleValue = new DataGridViewTextBoxColumn
            {
                Name = "colSampleValue",
                HeaderText = "Örnek Değer",
                ReadOnly = true,
                FillWeight = 15
            };

            dgvColumns.Columns.AddRange(colOriginalName, colPropertyName, colCSharpType, colSampleValue);

            splitCenter.Panel1.Controls.Add(dgvColumns);
            splitCenter.Panel1.Controls.Add(lblColumnsTitle);

            // --- Veri Önizleme ---
            lblDataPreviewTitle = new Label
            {
                Text = "📊  VERİ ÖNİZLEME",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = accentGreen,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            dgvDataPreview = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = bgPanel,
                GridColor = borderColor,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9F),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = bgPanel,
                    ForeColor = textLight,
                    SelectionBackColor = Color.FromArgb(60, 80, 140),
                    SelectionForeColor = textLight
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(45, 45, 68),
                    ForeColor = accentGreen,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 32
            };

            splitCenter.Panel2.Controls.Add(dgvDataPreview);
            splitCenter.Panel2.Controls.Add(lblDataPreviewTitle);

            splitMain.Panel1.Controls.Add(splitCenter);

            // ================================================================
            //  SAĞ PANEL — Kod Önizleme + Butonlar
            // ================================================================
            lblCodePreviewTitle = new Label
            {
                Text = "💻  KOD ÖNİZLEME",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = accentOrange,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };

            rtbCodePreview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 52),
                ForeColor = textLight,
                Font = new Font("Cascadia Code", 10F, FontStyle.Regular, GraphicsUnit.Point, 0),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false,
                DetectUrls = false
            };

            pnlActionButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = bgPanel,
                Padding = new Padding(8, 8, 8, 8)
            };

            btnPreview = new Button
            {
                Text = "🔄  Önizle",
                Dock = DockStyle.Left,
                Width = 140,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnPreview.FlatAppearance.BorderSize = 0;

            btnSave = new Button
            {
                Text = "💾  Kaydet",
                Dock = DockStyle.Right,
                Width = 140,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = accentGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;

            btnCopyCode = new Button
            {
                Text = "📋 Kopyala",
                Dock = DockStyle.Left,
                Width = 130,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(140, 80, 200),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnCopyCode.FlatAppearance.BorderSize = 0;

            // Araya spacer
            var spacer = new Panel { Dock = DockStyle.Left, Width = 8, BackColor = Color.Transparent };

            pnlActionButtons.Controls.Add(btnSave);
            pnlActionButtons.Controls.Add(btnCopyCode);
            pnlActionButtons.Controls.Add(spacer);
            pnlActionButtons.Controls.Add(btnPreview);

            splitMain.Panel2.Controls.Add(rtbCodePreview);
            splitMain.Panel2.Controls.Add(pnlActionButtons);
            splitMain.Panel2.Controls.Add(lblCodePreviewTitle);

            // ================================================================
            //  STATUS BAR
            // ================================================================
            statusStrip = new StatusStrip
            {
                BackColor = bgPanel,
                ForeColor = textMuted,
                SizingGrip = true
            };

            tsslStatus = new ToolStripStatusLabel
            {
                Text = "Hazır — CSV dosyası veya klasör seçin.",
                ForeColor = textMuted,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            tsslFileCount = new ToolStripStatusLabel
            {
                Text = "0 dosya",
                ForeColor = textMuted,
                BorderSides = ToolStripStatusLabelBorderSides.Left,
                BorderStyle = Border3DStyle.Etched
            };

            statusStrip.Items.AddRange(new ToolStripItem[] { tsslStatus, tsslFileCount });

            // ================================================================
            //  ANA FORMA EKLE
            // ================================================================
            Controls.Add(splitMain);
            Controls.Add(leftBorder);
            Controls.Add(pnlLeft);

            // SplitterDistance'ı form yüklendikten sonra ayarla
            Load += (s, e) =>
            {
                try
                {
                    splitMain.SplitterDistance = (int)(splitMain.Width * 0.55);
                    splitCenter.SplitterDistance = (int)(splitCenter.Height * 0.55);
                }
                catch { /* DPI/boyut uyumsuzluğu durumunda varsayılan kalır */ }
            };
            Controls.Add(statusStrip);
        }

        #endregion

        // Sol Panel — Import
        private Panel pnlLeft;
        private Label lblImportTitle;
        private Button btnImportFile;
        private Button btnImportFolder;
        private Label lblFilesTitle;
        private ListBox lstFiles;

        // Sol Panel — Export Settings
        private Panel pnlExportSettings;
        private Label lblExportTitle;
        private Label lblClassName;
        private TextBox txtClassName;
        private Label lblNamespace;
        private TextBox txtNamespace;
        private Label lblExportPath;
        private Panel pnlExportPathRow;
        private TextBox txtExportPath;
        private Button btnBrowseExport;

        // Orta — Split containers
        private SplitContainer splitMain;
        private SplitContainer splitCenter;

        // Orta — Kolon Ayarları
        private Label lblColumnsTitle;
        private DataGridView dgvColumns;

        // Orta — Veri Önizleme
        private Label lblDataPreviewTitle;
        private DataGridView dgvDataPreview;

        // Sağ — Kod Önizleme
        private Label lblCodePreviewTitle;
        private RichTextBox rtbCodePreview;
        private Panel pnlActionButtons;
        private Button btnPreview;
        private Button btnSave;
        private Button btnCopyCode;

        // Status Bar
        private StatusStrip statusStrip;
        private ToolStripStatusLabel tsslStatus;
        private ToolStripStatusLabel tsslFileCount;
    }
}
