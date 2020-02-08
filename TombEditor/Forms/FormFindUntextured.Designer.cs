﻿namespace TombEditor.Forms
{
    partial class FormFindUntextured
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.butNewSearch = new DarkUI.Controls.DarkButton();
            this.butCancel = new DarkUI.Controls.DarkButton();
            this.dgvUntextured = new DarkUI.Controls.DarkDataGridView();
            this.cbSelectedRooms = new DarkUI.Controls.DarkCheckBox();
            this.colRoom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCoordinates = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUntextured)).BeginInit();
            this.SuspendLayout();
            // 
            // butNewSearch
            // 
            this.butNewSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.butNewSearch.Checked = false;
            this.butNewSearch.Location = new System.Drawing.Point(142, 332);
            this.butNewSearch.Name = "butNewSearch";
            this.butNewSearch.Size = new System.Drawing.Size(80, 23);
            this.butNewSearch.TabIndex = 9;
            this.butNewSearch.Text = "New search";
            this.butNewSearch.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.butNewSearch.Click += new System.EventHandler(this.butNewSearch_Click);
            // 
            // butCancel
            // 
            this.butCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.butCancel.Checked = false;
            this.butCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.butCancel.Location = new System.Drawing.Point(228, 332);
            this.butCancel.Name = "butCancel";
            this.butCancel.Size = new System.Drawing.Size(80, 23);
            this.butCancel.TabIndex = 10;
            this.butCancel.Text = "Close";
            this.butCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.butCancel.Click += new System.EventHandler(this.butCancel_Click);
            // 
            // dgvUntextured
            // 
            this.dgvUntextured.AllowUserToAddRows = false;
            this.dgvUntextured.AllowUserToDeleteRows = false;
            this.dgvUntextured.AllowUserToDragDropRows = false;
            this.dgvUntextured.AllowUserToPasteCells = false;
            this.dgvUntextured.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvUntextured.ColumnHeadersHeight = 17;
            this.dgvUntextured.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colRoom,
            this.colCoordinates});
            this.dgvUntextured.Location = new System.Drawing.Point(7, 6);
            this.dgvUntextured.MultiSelect = false;
            this.dgvUntextured.Name = "dgvUntextured";
            this.dgvUntextured.RowHeadersWidth = 41;
            this.dgvUntextured.Size = new System.Drawing.Size(301, 320);
            this.dgvUntextured.TabIndex = 11;
            this.dgvUntextured.SelectionChanged += new System.EventHandler(this.dgvUntextured_SelectionChanged);
            // 
            // cbSelectedRooms
            // 
            this.cbSelectedRooms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbSelectedRooms.AutoSize = true;
            this.cbSelectedRooms.Checked = true;
            this.cbSelectedRooms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbSelectedRooms.Location = new System.Drawing.Point(7, 336);
            this.cbSelectedRooms.Name = "cbSelectedRooms";
            this.cbSelectedRooms.Size = new System.Drawing.Size(129, 17);
            this.cbSelectedRooms.TabIndex = 12;
            this.cbSelectedRooms.Text = "Selected rooms only";
            // 
            // colRoom
            // 
            this.colRoom.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRoom.FillWeight = 75F;
            this.colRoom.HeaderText = "Room";
            this.colRoom.Name = "colRoom";
            this.colRoom.ReadOnly = true;
            this.colRoom.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colCoordinates
            // 
            this.colCoordinates.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colCoordinates.FillWeight = 25F;
            this.colCoordinates.HeaderText = "Block";
            this.colCoordinates.Name = "colCoordinates";
            this.colCoordinates.ReadOnly = true;
            this.colCoordinates.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // FormFindUntextured
            // 
            this.AcceptButton = this.butNewSearch;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.butCancel;
            this.ClientSize = new System.Drawing.Size(314, 362);
            this.Controls.Add(this.cbSelectedRooms);
            this.Controls.Add(this.dgvUntextured);
            this.Controls.Add(this.butNewSearch);
            this.Controls.Add(this.butCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(330, 400);
            this.Name = "FormFindUntextured";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Find untextured faces";
            ((System.ComponentModel.ISupportInitialize)(this.dgvUntextured)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DarkUI.Controls.DarkButton butNewSearch;
        private DarkUI.Controls.DarkButton butCancel;
        private DarkUI.Controls.DarkDataGridView dgvUntextured;
        private DarkUI.Controls.DarkCheckBox cbSelectedRooms;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRoom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCoordinates;
    }
}