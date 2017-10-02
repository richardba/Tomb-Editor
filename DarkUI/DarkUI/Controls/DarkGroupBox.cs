﻿using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DarkUI.Config;

namespace DarkUI.Controls
{
    public sealed class DarkGroupBox : GroupBox
    {
        private Color _borderColor = Colors.LightBorder;

        public DarkGroupBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint, true);
            Paint += DarkGroupBox_Paint;
            ForeColor = Color.Gainsboro;
            BackColor = Colors.GreyBackground;
            ResizeRedraw = true;
            DoubleBuffered = true;
        }

        private void DarkGroupBox_Paint(object sender, PaintEventArgs e)
        {
            if (Parent != null)
                e.Graphics.Clear(Parent.BackColor);
            Size tSize = TextRenderer.MeasureText(Text, Font);
            Rectangle borderRect = ClientRectangle;
            borderRect.Y = (borderRect.Y + (tSize.Height / 2));
            borderRect.Height = (borderRect.Height - (tSize.Height / 2));
            e.Graphics.FillRectangle(new SolidBrush(BackColor), borderRect);
            ControlPaint.DrawBorder(e.Graphics, borderRect, _borderColor, ButtonBorderStyle.Solid);
            Rectangle textRect = ClientRectangle;
            textRect.X = (textRect.X + 6);
            textRect.Y += borderRect.Top;
            textRect.Width = tSize.Width + 6;
            textRect.Height = tSize.Height - borderRect.Top;
            e.Graphics.FillRectangle(new SolidBrush(BackColor), textRect);
            textRect = ClientRectangle;
            textRect.X = (textRect.X + 6);
            textRect.Width = tSize.Width + 6;
            textRect.Height = tSize.Height;
            e.Graphics.DrawString(Text, Font, new SolidBrush(ForeColor), textRect);

        }

        [Category("Appearance")]
        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                Invalidate(); // causes control to be redrawn
            }
        }
    }
}
