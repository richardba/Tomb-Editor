﻿using System.Windows.Forms;
using DarkUI.Controls;

namespace TombEditor.Forms
{
    partial class FormAnimatedTextures
    {
        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.darkLabel1 = new DarkUI.Controls.DarkLabel();
            this.comboAnimatedTextureSets = new DarkUI.Controls.DarkComboBox();
            this.butAnimatedTextureSetDelete = new DarkUI.Controls.DarkButton();
            this.butAnimatedTextureSetNew = new DarkUI.Controls.DarkButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.comboCurrentTexture = new DarkUI.Controls.DarkComboBox();
            this.textureMap = new TombEditor.Forms.FormAnimatedTextures.PanelTextureMapForAnimations();
            this.panel1 = new System.Windows.Forms.Panel();
            this.butUpdate = new DarkUI.Controls.DarkButton();
            this.label1 = new DarkUI.Controls.DarkLabel();
            this.tooManyFramesWarning = new DarkUI.Controls.DarkLabel();
            this.previewProgressBar = new DarkUI.Controls.DarkProgressBar();
            this.texturesDataGridView = new DarkUI.Controls.DarkDataGridView();
            this.texturesDataGridViewColumnImage = new System.Windows.Forms.DataGridViewImageColumn();
            this.texturesDataGridViewColumnRepeat = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.texturesDataGridViewColumnTexture = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.texturesDataGridViewColumnArea = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.texturesDataGridViewColumnTexCoord0 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.texturesDataGridViewColumnTexCoord1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.texturesDataGridViewColumnTexCoord2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.texturesDataGridViewColumnTexCoord3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.labelHeaderNgSettings = new DarkUI.Controls.DarkLabel();
            this.darkLabel4 = new DarkUI.Controls.DarkLabel();
            this.settingsPanelNG = new System.Windows.Forms.Panel();
            this.labelUvRotate = new DarkUI.Controls.DarkLabel();
            this.comboUvRotate = new DarkUI.Controls.DarkComboBox();
            this.labelFps = new DarkUI.Controls.DarkLabel();
            this.comboFps = new DarkUI.Controls.DarkComboBox();
            this.labelEffect = new DarkUI.Controls.DarkLabel();
            this.comboEffect = new DarkUI.Controls.DarkComboBox();
            this.previewImage = new System.Windows.Forms.PictureBox();
            this.texturesDataGridViewControls = new TombLib.Controls.DarkDataGridViewControls();
            this.butOk = new DarkUI.Controls.DarkButton();
            this.warningToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.texturesDataGridView)).BeginInit();
            this.settingsPanelNG.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.previewImage)).BeginInit();
            this.SuspendLayout();
            // 
            // darkLabel1
            // 
            this.darkLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel1.Location = new System.Drawing.Point(1, 5);
            this.darkLabel1.Name = "darkLabel1";
            this.darkLabel1.Size = new System.Drawing.Size(125, 17);
            this.darkLabel1.TabIndex = 0;
            this.darkLabel1.Text = " Animation set";
            this.darkLabel1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // comboAnimatedTextureSets
            // 
            this.comboAnimatedTextureSets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboAnimatedTextureSets.ItemHeight = 18;
            this.comboAnimatedTextureSets.Location = new System.Drawing.Point(4, 25);
            this.comboAnimatedTextureSets.Name = "comboAnimatedTextureSets";
            this.comboAnimatedTextureSets.Size = new System.Drawing.Size(333, 24);
            this.comboAnimatedTextureSets.TabIndex = 1;
            this.comboAnimatedTextureSets.SelectedIndexChanged += new System.EventHandler(this.comboAnimatedTextureSets_SelectedIndexChanged);
            // 
            // butAnimatedTextureSetDelete
            // 
            this.butAnimatedTextureSetDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.butAnimatedTextureSetDelete.Enabled = false;
            this.butAnimatedTextureSetDelete.Image = global::TombEditor.Properties.Resources.general_trash_16;
            this.butAnimatedTextureSetDelete.ImagePadding = 3;
            this.butAnimatedTextureSetDelete.Location = new System.Drawing.Point(371, 25);
            this.butAnimatedTextureSetDelete.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.butAnimatedTextureSetDelete.Name = "butAnimatedTextureSetDelete";
            this.butAnimatedTextureSetDelete.Size = new System.Drawing.Size(24, 24);
            this.butAnimatedTextureSetDelete.TabIndex = 3;
            this.butAnimatedTextureSetDelete.Click += new System.EventHandler(this.butAnimatedTextureSetDelete_Click);
            // 
            // butAnimatedTextureSetNew
            // 
            this.butAnimatedTextureSetNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.butAnimatedTextureSetNew.Image = global::TombEditor.Properties.Resources.general_plus_math_16;
            this.butAnimatedTextureSetNew.ImagePadding = 4;
            this.butAnimatedTextureSetNew.Location = new System.Drawing.Point(342, 25);
            this.butAnimatedTextureSetNew.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.butAnimatedTextureSetNew.Name = "butAnimatedTextureSetNew";
            this.butAnimatedTextureSetNew.Size = new System.Drawing.Size(24, 24);
            this.butAnimatedTextureSetNew.TabIndex = 2;
            this.butAnimatedTextureSetNew.Click += new System.EventHandler(this.butAnimatedTextureSetNew_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(4, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 618F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(802, 618);
            this.tableLayoutPanel1.TabIndex = 43;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.comboCurrentTexture);
            this.panel2.Controls.Add(this.textureMap);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(404, 3);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(395, 612);
            this.panel2.TabIndex = 21;
            // 
            // comboCurrentTexture
            // 
            this.comboCurrentTexture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboCurrentTexture.FormattingEnabled = true;
            this.comboCurrentTexture.Location = new System.Drawing.Point(4, 5);
            this.comboCurrentTexture.Name = "comboCurrentTexture";
            this.comboCurrentTexture.Size = new System.Drawing.Size(385, 23);
            this.comboCurrentTexture.TabIndex = 4;
            this.comboCurrentTexture.DropDown += new System.EventHandler(this.comboCurrentTexture_DropDown);
            this.comboCurrentTexture.SelectedValueChanged += new System.EventHandler(this.comboCurrentTexture_SelectedValueChanged);
            // 
            // textureMap
            // 
            this.textureMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textureMap.Location = new System.Drawing.Point(4, 32);
            this.textureMap.Margin = new System.Windows.Forms.Padding(3, 30, 3, 3);
            this.textureMap.Name = "textureMap";
            this.textureMap.Size = new System.Drawing.Size(385, 577);
            this.textureMap.TabIndex = 0;
            this.textureMap.DoubleClick += new System.EventHandler(this.textureMap_DoubleClick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.butUpdate);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.tooManyFramesWarning);
            this.panel1.Controls.Add(this.previewProgressBar);
            this.panel1.Controls.Add(this.texturesDataGridView);
            this.panel1.Controls.Add(this.labelHeaderNgSettings);
            this.panel1.Controls.Add(this.darkLabel4);
            this.panel1.Controls.Add(this.settingsPanelNG);
            this.panel1.Controls.Add(this.previewImage);
            this.panel1.Controls.Add(this.texturesDataGridViewControls);
            this.panel1.Controls.Add(this.butAnimatedTextureSetDelete);
            this.panel1.Controls.Add(this.butAnimatedTextureSetNew);
            this.panel1.Controls.Add(this.darkLabel1);
            this.panel1.Controls.Add(this.comboAnimatedTextureSets);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(395, 613);
            this.panel1.TabIndex = 0;
            // 
            // butUpdate
            // 
            this.butUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.butUpdate.Enabled = false;
            this.butUpdate.Image = global::TombEditor.Properties.Resources.general_undo_16;
            this.butUpdate.ImagePadding = 4;
            this.butUpdate.Location = new System.Drawing.Point(371, 132);
            this.butUpdate.Margin = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.butUpdate.Name = "butUpdate";
            this.butUpdate.Size = new System.Drawing.Size(24, 24);
            this.butUpdate.TabIndex = 2;
            this.butUpdate.Click += new System.EventHandler(this.butUpdate_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.label1.Location = new System.Drawing.Point(8, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(387, 15);
            this.label1.TabIndex = 17;
            this.label1.Text = " Frames";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // tooManyFramesWarning
            // 
            this.tooManyFramesWarning.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tooManyFramesWarning.BackColor = System.Drawing.Color.Firebrick;
            this.tooManyFramesWarning.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tooManyFramesWarning.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.tooManyFramesWarning.Image = global::TombEditor.Properties.Resources.general_Warning_16;
            this.tooManyFramesWarning.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.tooManyFramesWarning.Location = new System.Drawing.Point(371, 162);
            this.tooManyFramesWarning.Name = "tooManyFramesWarning";
            this.tooManyFramesWarning.Size = new System.Drawing.Size(24, 218);
            this.tooManyFramesWarning.TabIndex = 19;
            this.tooManyFramesWarning.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.tooManyFramesWarning.Visible = false;
            // 
            // previewProgressBar
            // 
            this.previewProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.previewProgressBar.Location = new System.Drawing.Point(265, 594);
            this.previewProgressBar.Maximum = 0;
            this.previewProgressBar.Name = "previewProgressBar";
            this.previewProgressBar.Size = new System.Drawing.Size(130, 18);
            this.previewProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.previewProgressBar.TabIndex = 18;
            this.previewProgressBar.TextMode = DarkUI.Controls.DarkProgressBarMode.XOfN;
            // 
            // texturesDataGridView
            // 
            this.texturesDataGridView.AllowUserToAddRows = false;
            this.texturesDataGridView.AllowUserToOrderColumns = true;
            this.texturesDataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.texturesDataGridView.AutoGenerateColumns = false;
            this.texturesDataGridView.ColumnHeadersHeight = 17;
            this.texturesDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.texturesDataGridViewColumnImage,
            this.texturesDataGridViewColumnRepeat,
            this.texturesDataGridViewColumnTexture,
            this.texturesDataGridViewColumnArea,
            this.texturesDataGridViewColumnTexCoord0,
            this.texturesDataGridViewColumnTexCoord1,
            this.texturesDataGridViewColumnTexCoord2,
            this.texturesDataGridViewColumnTexCoord3});
            this.texturesDataGridView.Enabled = false;
            this.texturesDataGridView.Location = new System.Drawing.Point(4, 72);
            this.texturesDataGridView.Name = "texturesDataGridView";
            this.texturesDataGridView.RowHeadersWidth = 41;
            this.texturesDataGridView.RowTemplate.Height = 48;
            this.texturesDataGridView.Size = new System.Drawing.Size(361, 368);
            this.texturesDataGridView.TabIndex = 15;
            this.texturesDataGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.texturesDataGridView_CellDoubleClick);
            this.texturesDataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.texturesDataGridView_CellFormatting);
            this.texturesDataGridView.CellParsing += new System.Windows.Forms.DataGridViewCellParsingEventHandler(this.texturesDataGridView_CellParsing);
            this.texturesDataGridView.SelectionChanged += new System.EventHandler(this.texturesDataGridView_SelectionChanged);
            // 
            // texturesDataGridViewColumnImage
            // 
            this.texturesDataGridViewColumnImage.HeaderText = "Image";
            this.texturesDataGridViewColumnImage.ImageLayout = System.Windows.Forms.DataGridViewImageCellLayout.Zoom;
            this.texturesDataGridViewColumnImage.Name = "texturesDataGridViewColumnImage";
            this.texturesDataGridViewColumnImage.ReadOnly = true;
            this.texturesDataGridViewColumnImage.Width = 48;
            // 
            // texturesDataGridViewColumnRepeat
            // 
            this.texturesDataGridViewColumnRepeat.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.texturesDataGridViewColumnRepeat.DataPropertyName = "Repeat";
            this.texturesDataGridViewColumnRepeat.HeaderText = "Repeat";
            this.texturesDataGridViewColumnRepeat.Name = "texturesDataGridViewColumnRepeat";
            this.texturesDataGridViewColumnRepeat.Width = 67;
            // 
            // texturesDataGridViewColumnTexture
            // 
            this.texturesDataGridViewColumnTexture.DataPropertyName = "Texture";
            this.texturesDataGridViewColumnTexture.DisplayStyle = System.Windows.Forms.DataGridViewComboBoxDisplayStyle.ComboBox;
            this.texturesDataGridViewColumnTexture.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.texturesDataGridViewColumnTexture.HeaderText = "Texture";
            this.texturesDataGridViewColumnTexture.Name = "texturesDataGridViewColumnTexture";
            this.texturesDataGridViewColumnTexture.Width = 80;
            // 
            // texturesDataGridViewColumnArea
            // 
            this.texturesDataGridViewColumnArea.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.texturesDataGridViewColumnArea.HeaderText = "Area";
            this.texturesDataGridViewColumnArea.Name = "texturesDataGridViewColumnArea";
            this.texturesDataGridViewColumnArea.ReadOnly = true;
            this.texturesDataGridViewColumnArea.Width = 54;
            // 
            // texturesDataGridViewColumnTexCoord0
            // 
            this.texturesDataGridViewColumnTexCoord0.DataPropertyName = "TexCoord0";
            this.texturesDataGridViewColumnTexCoord0.HeaderText = "Edge 0";
            this.texturesDataGridViewColumnTexCoord0.Name = "texturesDataGridViewColumnTexCoord0";
            this.texturesDataGridViewColumnTexCoord0.Width = 70;
            // 
            // texturesDataGridViewColumnTexCoord1
            // 
            this.texturesDataGridViewColumnTexCoord1.DataPropertyName = "TexCoord1";
            this.texturesDataGridViewColumnTexCoord1.HeaderText = "Edge 1";
            this.texturesDataGridViewColumnTexCoord1.Name = "texturesDataGridViewColumnTexCoord1";
            this.texturesDataGridViewColumnTexCoord1.Width = 70;
            // 
            // texturesDataGridViewColumnTexCoord2
            // 
            this.texturesDataGridViewColumnTexCoord2.DataPropertyName = "TexCoord2";
            this.texturesDataGridViewColumnTexCoord2.HeaderText = "Edge 2";
            this.texturesDataGridViewColumnTexCoord2.Name = "texturesDataGridViewColumnTexCoord2";
            this.texturesDataGridViewColumnTexCoord2.Width = 70;
            // 
            // texturesDataGridViewColumnTexCoord3
            // 
            this.texturesDataGridViewColumnTexCoord3.DataPropertyName = "TexCoord3";
            this.texturesDataGridViewColumnTexCoord3.HeaderText = "Edge 3";
            this.texturesDataGridViewColumnTexCoord3.Name = "texturesDataGridViewColumnTexCoord3";
            this.texturesDataGridViewColumnTexCoord3.Width = 70;
            // 
            // labelHeaderNgSettings
            // 
            this.labelHeaderNgSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelHeaderNgSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.labelHeaderNgSettings.Location = new System.Drawing.Point(4, 443);
            this.labelHeaderNgSettings.Name = "labelHeaderNgSettings";
            this.labelHeaderNgSettings.Size = new System.Drawing.Size(255, 18);
            this.labelHeaderNgSettings.TabIndex = 10;
            this.labelHeaderNgSettings.Text = " NG Settings";
            this.labelHeaderNgSettings.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // darkLabel4
            // 
            this.darkLabel4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.darkLabel4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.darkLabel4.Location = new System.Drawing.Point(262, 443);
            this.darkLabel4.Name = "darkLabel4";
            this.darkLabel4.Size = new System.Drawing.Size(133, 18);
            this.darkLabel4.TabIndex = 11;
            this.darkLabel4.Text = " Preview";
            this.darkLabel4.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // settingsPanelNG
            // 
            this.settingsPanelNG.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.settingsPanelNG.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.settingsPanelNG.Controls.Add(this.labelUvRotate);
            this.settingsPanelNG.Controls.Add(this.comboUvRotate);
            this.settingsPanelNG.Controls.Add(this.labelFps);
            this.settingsPanelNG.Controls.Add(this.comboFps);
            this.settingsPanelNG.Controls.Add(this.labelEffect);
            this.settingsPanelNG.Controls.Add(this.comboEffect);
            this.settingsPanelNG.Location = new System.Drawing.Point(4, 464);
            this.settingsPanelNG.Name = "settingsPanelNG";
            this.settingsPanelNG.Size = new System.Drawing.Size(255, 148);
            this.settingsPanelNG.TabIndex = 12;
            // 
            // labelUvRotate
            // 
            this.labelUvRotate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.labelUvRotate.Location = new System.Drawing.Point(-5, 70);
            this.labelUvRotate.Name = "labelUvRotate";
            this.labelUvRotate.Size = new System.Drawing.Size(67, 23);
            this.labelUvRotate.TabIndex = 10;
            this.labelUvRotate.Text = "UvRotate:";
            this.labelUvRotate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboUvRotate
            // 
            this.comboUvRotate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboUvRotate.Location = new System.Drawing.Point(70, 70);
            this.comboUvRotate.Name = "comboUvRotate";
            this.comboUvRotate.Size = new System.Drawing.Size(180, 23);
            this.comboUvRotate.TabIndex = 11;
            this.comboUvRotate.SelectionChangeCommitted += new System.EventHandler(this.comboUvRotate_SelectionChangeCommitted);
            // 
            // labelFps
            // 
            this.labelFps.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.labelFps.Location = new System.Drawing.Point(9, 41);
            this.labelFps.Name = "labelFps";
            this.labelFps.Size = new System.Drawing.Size(53, 23);
            this.labelFps.TabIndex = 8;
            this.labelFps.Text = "Fps:";
            this.labelFps.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboFps
            // 
            this.comboFps.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboFps.Location = new System.Drawing.Point(70, 41);
            this.comboFps.Name = "comboFps";
            this.comboFps.Size = new System.Drawing.Size(180, 23);
            this.comboFps.TabIndex = 9;
            this.comboFps.SelectionChangeCommitted += new System.EventHandler(this.comboFps_SelectionChangeCommitted);
            // 
            // labelEffect
            // 
            this.labelEffect.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
            this.labelEffect.Location = new System.Drawing.Point(9, 12);
            this.labelEffect.Name = "labelEffect";
            this.labelEffect.Size = new System.Drawing.Size(53, 23);
            this.labelEffect.TabIndex = 6;
            this.labelEffect.Text = "Effect:";
            this.labelEffect.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // comboEffect
            // 
            this.comboEffect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboEffect.Location = new System.Drawing.Point(70, 12);
            this.comboEffect.Name = "comboEffect";
            this.comboEffect.Size = new System.Drawing.Size(180, 23);
            this.comboEffect.TabIndex = 7;
            this.comboEffect.SelectedIndexChanged += new System.EventHandler(this.comboEffect_SelectedIndexChanged);
            this.comboEffect.SelectionChangeCommitted += new System.EventHandler(this.comboEffect_SelectionChangeCommitted);
            // 
            // previewImage
            // 
            this.previewImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.previewImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.previewImage.Location = new System.Drawing.Point(265, 464);
            this.previewImage.Name = "previewImage";
            this.previewImage.Size = new System.Drawing.Size(130, 130);
            this.previewImage.TabIndex = 13;
            this.previewImage.TabStop = false;
            // 
            // texturesDataGridViewControls
            // 
            this.texturesDataGridViewControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.texturesDataGridViewControls.Enabled = false;
            this.texturesDataGridViewControls.Location = new System.Drawing.Point(371, 72);
            this.texturesDataGridViewControls.MinimumSize = new System.Drawing.Size(24, 100);
            this.texturesDataGridViewControls.Name = "texturesDataGridViewControls";
            this.texturesDataGridViewControls.Size = new System.Drawing.Size(24, 368);
            this.texturesDataGridViewControls.TabIndex = 16;
            // 
            // butOk
            // 
            this.butOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.butOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.butOk.Location = new System.Drawing.Point(669, 626);
            this.butOk.Name = "butOk";
            this.butOk.Size = new System.Drawing.Size(134, 23);
            this.butOk.TabIndex = 8;
            this.butOk.Text = "Ok";
            this.butOk.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.butOk.Click += new System.EventHandler(this.butOk_Click);
            // 
            // warningToolTip
            // 
            this.warningToolTip.AutomaticDelay = 100;
            this.warningToolTip.AutoPopDelay = 30000;
            this.warningToolTip.InitialDelay = 100;
            this.warningToolTip.ReshowDelay = 20;
            // 
            // FormAnimatedTextures
            // 
            this.AcceptButton = this.butOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.butOk;
            this.ClientSize = new System.Drawing.Size(809, 656);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.butOk);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(691, 500);
            this.Name = "FormAnimatedTextures";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Animated textures";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.texturesDataGridView)).EndInit();
            this.settingsPanelNG.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.previewImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private DarkLabel darkLabel1;
        private DarkComboBox comboAnimatedTextureSets;
        private DarkButton butAnimatedTextureSetDelete;
        private DarkButton butAnimatedTextureSetNew;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private DarkButton butOk;
        private DarkLabel label1;
        private DarkLabel tooManyFramesWarning;
        private DarkProgressBar previewProgressBar;
        private DarkDataGridView texturesDataGridView;
        private DarkLabel labelHeaderNgSettings;
        private DarkLabel darkLabel4;
        private Panel settingsPanelNG;
        private DarkLabel labelEffect;
        private DarkComboBox comboEffect;
        private PictureBox previewImage;
        private TombLib.Controls.DarkDataGridViewControls texturesDataGridViewControls;
        private DataGridViewImageColumn texturesDataGridViewColumnImage;
        private DataGridViewTextBoxColumn texturesDataGridViewColumnRepeat;
        private DataGridViewComboBoxColumn texturesDataGridViewColumnTexture;
        private DataGridViewTextBoxColumn texturesDataGridViewColumnArea;
        private DataGridViewTextBoxColumn texturesDataGridViewColumnTexCoord0;
        private DataGridViewTextBoxColumn texturesDataGridViewColumnTexCoord1;
        private DataGridViewTextBoxColumn texturesDataGridViewColumnTexCoord2;
        private DataGridViewTextBoxColumn texturesDataGridViewColumnTexCoord3;
        private DarkButton butUpdate;
        private ToolTip warningToolTip;
        private System.ComponentModel.IContainer components;
        private DarkLabel labelUvRotate;
        private DarkComboBox comboUvRotate;
        private DarkLabel labelFps;
        private DarkComboBox comboFps;
        private Panel panel2;
        private DarkComboBox comboCurrentTexture;
        private FormAnimatedTextures.PanelTextureMapForAnimations textureMap;
    }
}