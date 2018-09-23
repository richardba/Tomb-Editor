﻿using DarkUI.Docking;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TombLib.LevelData;
using TombLib.Rendering;

namespace TombEditor.ToolWindows
{
    public partial class TriggerList : DarkToolWindow
    {
        private readonly Editor _editor;

        public TriggerList()
        {
            InitializeComponent();
            CommandHandler.AssignCommandsToControls(Editor.Instance, this, toolTip);

            _editor = Editor.Instance;
            _editor.EditorEventRaised += EditorEventRaised;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _editor.EditorEventRaised -= EditorEventRaised;
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void EditorEventRaised(IEditorEvent obj)
        {
            // Update the trigger control
            if (obj is Editor.SelectedSectorsChangedEvent ||
                obj is Editor.SelectedRoomChangedEvent ||
                obj is Editor.RoomSectorPropertiesChangedEvent)
            {
                lstTriggers.BeginUpdate();
                lstTriggers.Items.Clear();

                if (_editor.Level != null && _editor.SelectedSectors.Valid)
                {
                    // Search for unique triggers inside the selected area
                    var triggers = new List<TriggerInstance>();
                    var area = _editor.SelectedSectors.Area;
                    for (int x = area.X0; x <= area.X1; x++)
                        for (int z = area.Y0; z <= area.Y1; z++)
                            foreach (var trigger in _editor.SelectedRoom.GetBlockTry(x, z)?.Triggers ?? new List<TriggerInstance>())
                                if (!triggers.Contains(trigger))
                                    triggers.Add(trigger);

                    // Add triggers to listbox
                    foreach (TriggerInstance trigger in triggers)
                    {
                        lstTriggers.Items.Add(trigger);
                    }
                }

                lstTriggers.EndUpdate();
            }

            // Update the trigger control selection

            if (obj is Editor.SelectedSectorsChangedEvent ||
                obj is Editor.SelectedRoomChangedEvent ||
                obj is Editor.SelectedObjectChangedEvent)
            {
                var trigger = _editor.SelectedObject as TriggerInstance;
                lstTriggers.SelectedItem = trigger != null && lstTriggers.Items.Contains(trigger) ? trigger : null;
            }

            // Update tooltip texts
            if (obj is Editor.ConfigurationChangedEvent)
            {
                if (((Editor.ConfigurationChangedEvent)obj).UpdateKeyboardShortcuts)
                    CommandHandler.AssignCommandsToControls(_editor, this, toolTip, true);
            }
        }

        private void DeleteTrigger()
        {
            if (_editor.SelectedRoom == null || !(_editor.SelectedObject is TriggerInstance))
                return;
            EditorActions.DeleteObject(_editor.SelectedObject);
        }

        private void lstTriggers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                DeleteTrigger();
        }

        private void butDeleteTrigger_Click(object sender, EventArgs e)
        {
            DeleteTrigger();
        }

        private void butEditTrigger_Click(object sender, EventArgs e)
        {
            if (_editor.SelectedRoom == null || !(_editor.SelectedObject is TriggerInstance))
                return;
            EditorActions.EditObject(_editor.SelectedObject, this);
        }

        private void lstTriggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_editor.SelectedRoom == null || lstTriggers.SelectedItem == null)
                return;
            _editor.SelectedObject = (ObjectInstance)lstTriggers.SelectedItem;
        }

        private void lstTriggers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = lstTriggers.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                var instance = lstTriggers.Items[index] as ObjectInstance;
                if (instance != null)
                    EditorActions.EditObject(instance, this);
            }
        }

        private void butAddTrigger_MouseEnter(object sender, EventArgs e)
        {
            if (_editor.Configuration.Editor_AutoSwitchSectorColoringInfo)
                _editor.SectorColoringManager.SetPriority(SectorColoringType.Trigger);
        }
    }
}
