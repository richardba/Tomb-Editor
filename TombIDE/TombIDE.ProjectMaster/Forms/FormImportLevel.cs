﻿using DarkUI.Controls;
using DarkUI.Forms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TombIDE.Shared;
using TombLib.Projects;

namespace TombIDE
{
	public partial class FormImportLevel : DarkForm
	{
		private IDE _ide;

		#region Initialization

		public FormImportLevel(IDE ide, string prj2FilePath)
		{
			_ide = ide;

			InitializeComponent();

			// Setup some information
			textBox_Prj2Path.BackColor = Color.FromArgb(48, 48, 48); // Mark as uneditable
			textBox_Prj2Path.Text = prj2FilePath;
			textBox_Prj2Path.Tag = prj2FilePath; // Keep the full path in the Tag (because we are visually changing the text later)

			textBox_LevelName.Text = Path.GetFileNameWithoutExtension(prj2FilePath);
		}

		#endregion Initialization

		#region Level importing methods

		private void button_Import_Click(object sender, EventArgs e)
		{
			button_Import.Enabled = false;

			try
			{
				string levelName = SharedMethods.RemoveIllegalPathSymbols(textBox_LevelName.Text.Trim());
				levelName = LevelHandling.RemoveIllegalNameSymbols(levelName);

				if (string.IsNullOrWhiteSpace(levelName))
					throw new ArgumentException("You must enter a valid name for the level.");

				if (radioButton_SelectedCopy.Checked && treeView.SelectedNodes.Count == 0)
					throw new ArgumentException("You must select which .prj2 files you want to import.");

				// Check for name duplicates
				foreach (ProjectLevel projectLevel in _ide.Project.Levels)
				{
					if (projectLevel.Name.ToLower() == levelName.ToLower())
						throw new ArgumentException("A level with the same name already exists on the list.");
				}

				string dataFileName = textBox_CustomFileName.Text.Trim();

				if (string.IsNullOrWhiteSpace(dataFileName))
					throw new ArgumentException("You must specify the custom DAT file name.");

				if (radioButton_SpecifiedCopy.Checked || radioButton_SelectedCopy.Checked)
					ImportAndCopyFiles(levelName, dataFileName);
				else if (radioButton_FolderKeep.Checked)
					ImportButKeepFiles(levelName, dataFileName);
			}
			catch (Exception ex)
			{
				DarkMessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				button_Import.Enabled = true;

				DialogResult = DialogResult.None;
			}
		}

		private void ImportAndCopyFiles(string levelName, string dataFileName)
		{
			string fullSpecifiedPrj2FilePath = textBox_Prj2Path.Tag.ToString();

			string levelFolderPath = Path.Combine(_ide.Project.LevelsPath, levelName); // A path inside the project's /Levels/ folder
			string specificFileName = Path.GetFileName(fullSpecifiedPrj2FilePath); // The user-specified file name

			// Create the level folder
			if (!Directory.Exists(levelFolderPath))
				Directory.CreateDirectory(levelFolderPath);

			if (Directory.EnumerateFileSystemEntries(levelFolderPath).ToArray().Length > 0) // 99% this will never accidentally happen
				throw new ArgumentException("A folder with the same name as the \"Level name\" already exists in\n" +
					"the project's /Levels/ folder and it's not empty.");

			if (radioButton_SpecifiedCopy.Checked)
			{
				// Only copy the specified file into the created level folder
				string destPath = Path.Combine(levelFolderPath, specificFileName);
				File.Copy(fullSpecifiedPrj2FilePath, destPath);
			}
			else if (radioButton_SelectedCopy.Checked)
			{
				// Check if the user-specified file was selected on the list, if not, then set the SpecificFile property to "$(LatestFile)"
				bool specificFileSelected = false;

				// Copy all selected files into the created levelFolderPath
				foreach (DarkTreeNode node in treeView.SelectedNodes)
				{
					if (node.Text == specificFileName)
						specificFileSelected = true;

					string nodePrj2Path = node.Tag.ToString(); // node.Tag is the full source path of the currently processed file
					string destPath = Path.Combine(levelFolderPath, Path.GetFileName(nodePrj2Path));
					File.Copy(nodePrj2Path, destPath);
				}

				if (!specificFileSelected) // If the user-specified file was not selected on the list
					specificFileName = "$(LatestFile)";
			}

			CreateAndAddLevelToProject(levelName, levelFolderPath, dataFileName, specificFileName);
		}

		private void ImportButKeepFiles(string levelName, string dataFileName)
		{
			string fullSpecifiedPrj2FilePath = textBox_Prj2Path.Tag.ToString();

			string levelFolderPath = Path.GetDirectoryName(fullSpecifiedPrj2FilePath);
			string specificFile = Path.GetFileName(fullSpecifiedPrj2FilePath);

			CreateAndAddLevelToProject(levelName, levelFolderPath, dataFileName, specificFile);
		}

		private void CreateAndAddLevelToProject(string levelName, string levelFolderPath, string dataFileName, string specificFileName)
		{
			// Create the ProjectLevel instance
			ProjectLevel importedLevel = new ProjectLevel
			{
				Name = levelName,
				FolderPath = levelFolderPath,
				DataFileName = dataFileName,
				SpecificFile = specificFileName
			};

			UpdateLevelSettings(importedLevel);

			if (checkBox_GenerateSection.Checked)
			{
				int ambientSoundID = (int)numeric_SoundID.Value;
				bool horizon = checkBox_EnableHorizon.Checked;

				List<string> scriptMessages = LevelHandling.GenerateSectionMessages(importedLevel, ambientSoundID, horizon);

				_ide.AddLevelToProject(importedLevel, scriptMessages);
			}
			else
				_ide.AddLevelToProject(importedLevel);
		}

		private void UpdateLevelSettings(ProjectLevel importedLevel)
		{
			if (radioButton_SpecifiedCopy.Checked)
			{
				string specifiedFileName = Path.GetFileName(textBox_Prj2Path.Tag.ToString());
				string internalFilePath = Path.Combine(importedLevel.FolderPath, specifiedFileName);

				LevelHandling.UpdatePrj2GameSettings(internalFilePath, importedLevel, _ide.Project);
			}
			else if (radioButton_SelectedCopy.Checked)
			{
				UpdateAllPrj2FilesInLevelDirectory(importedLevel);
			}
			else if (radioButton_FolderKeep.Checked)
			{
				DialogResult result = DarkMessageBox.Show(this, "Do you want to update the \"Game\" settings of all the .prj2 files in the\n" +
					"specified folder to match the project settings?", "Update settings?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

				if (result == DialogResult.Yes)
					UpdateAllPrj2FilesInLevelDirectory(importedLevel);
			}
		}

		private void UpdateAllPrj2FilesInLevelDirectory(ProjectLevel importedLevel)
		{
			string[] files = Directory.GetFiles(importedLevel.FolderPath, "*.prj2", SearchOption.TopDirectoryOnly);

			progressBar.Visible = true;
			progressBar.BringToFront();
			progressBar.Maximum = files.Length;

			foreach (string file in files)
			{
				if (!ProjectLevel.IsBackupFile(Path.GetFileName(file)))
					LevelHandling.UpdatePrj2GameSettings(file, importedLevel, _ide.Project);

				progressBar.Increment(1);
			}
		}

		#endregion Level importing methods

		#region Other level importing events / methods

		private void textBox_LevelName_TextChanged(object sender, EventArgs e)
		{
			if (!checkBox_CustomFileName.Checked)
				textBox_CustomFileName.Text = textBox_LevelName.Text.Trim().Replace(' ', '_');
		}

		private void checkBox_CustomFileName_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox_CustomFileName.Checked)
				textBox_CustomFileName.Enabled = true;
			else
			{
				textBox_CustomFileName.Enabled = false;
				textBox_CustomFileName.Text = textBox_LevelName.Text.Trim().Replace(' ', '_');
			}
		}

		private void textBox_CustomFileName_TextChanged(object sender, EventArgs e)
		{
			int cachedCaretPosition = textBox_CustomFileName.SelectionStart;

			textBox_CustomFileName.Text = textBox_CustomFileName.Text.Replace(' ', '_');
			textBox_CustomFileName.SelectionStart = cachedCaretPosition;
		}

		private void radioButton_SpecifiedCopy_CheckedChanged(object sender, EventArgs e)
		{
			if (!radioButton_SpecifiedCopy.Checked)
				return;

			textBox_Prj2Path.Text = textBox_Prj2Path.Tag.ToString(); // Switch back to the full path
			textBox_Prj2Path.BackColor = Color.FromArgb(48, 48, 48); // Reset the BackColor
			ClearAndDisableTreeView();
		}

		private void radioButton_SelectedCopy_CheckedChanged(object sender, EventArgs e)
		{
			if (!radioButton_SelectedCopy.Checked)
				return;

			textBox_Prj2Path.Text = Path.GetDirectoryName(textBox_Prj2Path.Tag.ToString()); // Switch to just the folder path
			textBox_Prj2Path.BackColor = Color.FromArgb(64, 80, 96); // Change the BackColor to indicate the change
			EnableAndFillTreeView();
		}

		private void radioButton_SpecificKeep_CheckedChanged(object sender, EventArgs e)
		{
			if (!radioButton_FolderKeep.Checked)
				return;

			textBox_Prj2Path.Text = Path.GetDirectoryName(textBox_Prj2Path.Tag.ToString()); // Switch to just the folder path
			textBox_Prj2Path.BackColor = Color.FromArgb(64, 80, 96); // Change the BackColor to indicate the change
			ClearAndDisableTreeView();
		}

		private void button_SelectAll_Click(object sender, EventArgs e)
		{
			treeView.SelectNodes(treeView.Nodes);
			treeView.Focus();
		}

		private void button_DeselectAll_Click(object sender, EventArgs e)
		{
			treeView.SelectedNodes.Clear();
			treeView.Invalidate();
		}

		private void ClearAndDisableTreeView()
		{
			treeView.SelectedNodes.Clear();
			treeView.Nodes.Clear();
			treeView.Invalidate();

			treeView.Enabled = false;
			button_SelectAll.Enabled = false;
			button_DeselectAll.Enabled = false;
		}

		private void EnableAndFillTreeView()
		{
			treeView.Enabled = true;
			button_SelectAll.Enabled = true;
			button_DeselectAll.Enabled = true;

			treeView.Nodes.Clear();

			foreach (string file in Directory.GetFiles(textBox_Prj2Path.Text, "*.prj2", SearchOption.TopDirectoryOnly))
			{
				if (ProjectLevel.IsBackupFile(Path.GetFileName(file)))
					continue;

				DarkTreeNode node = new DarkTreeNode
				{
					Text = Path.GetFileName(file),
					Tag = file,
				};

				treeView.Nodes.Add(node);
			}
		}

		#endregion Other level importing events / methods

		#region Script section generating

		private void checkBox_GenerateSection_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox_GenerateSection.Checked)
			{
				panel_ScriptSettings.Visible = true;
				panel_04.Height = 108;
				Height = 594;
			}
			else
			{
				panel_ScriptSettings.Visible = false;
				panel_04.Height = 35;
				Height = 521;
			}
		}

		private void button_OpenAudioFolder_Click(object sender, EventArgs e) =>
			SharedMethods.OpenFolderInExplorer(Path.Combine(_ide.Project.EnginePath, "audio"));

		#endregion Script section generating
	}
}
