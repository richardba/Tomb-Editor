﻿using DarkUI.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TombEditor.Geometry;

namespace TombEditor
{
    public partial class FormFlybyCamera : DarkForm
    {
        public bool IsNew { get; set; }

        private FlybyCameraInstance _flyByCamera;

        public FormFlybyCamera(FlybyCameraInstance flyByCamera)
        {
            _flyByCamera = flyByCamera;
            InitializeComponent();
        }

        private void butCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void FormFlybyCamera_Load(object sender, EventArgs e)
        {
            cbBit0.Checked = (_flyByCamera.Flags & (1 >> 0)) != 0;
            cbBit1.Checked = (_flyByCamera.Flags & (1 >> 1)) != 0;
            cbBit2.Checked = (_flyByCamera.Flags & (1 >> 2)) != 0;
            cbBit3.Checked = (_flyByCamera.Flags & (1 >> 3)) != 0;
            cbBit4.Checked = (_flyByCamera.Flags & (1 >> 4)) != 0;
            cbBit5.Checked = (_flyByCamera.Flags & (1 >> 5)) != 0;
            cbBit6.Checked = (_flyByCamera.Flags & (1 >> 6)) != 0;
            cbBit7.Checked = (_flyByCamera.Flags & (1 >> 7)) != 0;
            cbBit8.Checked = (_flyByCamera.Flags & (1 >> 8)) != 0;
            cbBit9.Checked = (_flyByCamera.Flags & (1 >> 9)) != 0;
            cbBit10.Checked = (_flyByCamera.Flags & (1 >> 10)) != 0;
            cbBit11.Checked = (_flyByCamera.Flags & (1 >> 11)) != 0;
            cbBit12.Checked = (_flyByCamera.Flags & (1 >> 12)) != 0;
            cbBit13.Checked = (_flyByCamera.Flags & (1 >> 13)) != 0;
            cbBit14.Checked = (_flyByCamera.Flags & (1 >> 14)) != 0;
            cbBit15.Checked = (_flyByCamera.Flags & (1 >> 15)) != 0;

            tbTimer.Text = _flyByCamera.Timer.ToString();
            tbSequence.Text = _flyByCamera.Sequence.ToString();
            tbSpeed.Text = _flyByCamera.Speed.ToString();
            tbNumber.Text = _flyByCamera.Number.ToString();
            tbFOV.Text = _flyByCamera.Fov.ToString();
            tbRoll.Text = _flyByCamera.Roll.ToString();
        }

        private void butOK_Click(object sender, EventArgs e)
        {
            ushort flags = 0;
            flags |= (ushort)(cbBit0.Checked ? (1 << 0) : 0);
            flags |= (ushort)(cbBit1.Checked ? (1 << 1) : 0);
            flags |= (ushort)(cbBit2.Checked ? (1 << 2) : 0);
            flags |= (ushort)(cbBit3.Checked ? (1 << 3) : 0);
            flags |= (ushort)(cbBit4.Checked ? (1 << 4) : 0);
            flags |= (ushort)(cbBit5.Checked ? (1 << 5) : 0);
            flags |= (ushort)(cbBit6.Checked ? (1 << 6) : 0);
            flags |= (ushort)(cbBit7.Checked ? (1 << 7) : 0);
            flags |= (ushort)(cbBit8.Checked ? (1 << 8) : 0);
            flags |= (ushort)(cbBit9.Checked ? (1 << 9) : 0);
            flags |= (ushort)(cbBit10.Checked ? (1 << 10) : 0);
            flags |= (ushort)(cbBit11.Checked ? (1 << 11) : 0);
            flags |= (ushort)(cbBit12.Checked ? (1 << 12) : 0);
            flags |= (ushort)(cbBit13.Checked ? (1 << 13) : 0);
            flags |= (ushort)(cbBit14.Checked ? (1 << 14) : 0);
            flags |= (ushort)(cbBit15.Checked ? (1 << 15) : 0);
            _flyByCamera.Flags = flags;

            _flyByCamera.Timer = short.Parse(tbTimer.Text);
            _flyByCamera.Speed = short.Parse(tbSpeed.Text);
            _flyByCamera.Roll = short.Parse(tbRoll.Text);
            _flyByCamera.Sequence = short.Parse(tbSequence.Text);
            _flyByCamera.Number = short.Parse(tbNumber.Text);
            _flyByCamera.Fov = short.Parse(tbFOV.Text);
            
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
