﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TombLib.GeometryIO
{
    public class IOGeometrySettings
    {
        public bool SwapXY { get; set; } = false;
        public bool SwapXZ { get; set; } = false;
        public bool SwapYZ { get; set; } = false;
        public bool FlipX { get; set; } = false;
        public bool FlipY { get; set; } = false;
        public bool FlipZ { get; set; } = false;
        public bool FlipV { get; set; } = false;
        public float Scale { get; set; } = 1.0f;
        public bool ClampUV { get; set; } = true;
        public bool PremultiplyUV { get; set; } = true;
    }
}
