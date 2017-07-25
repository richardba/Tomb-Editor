﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TombLib.Wad
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WadVertex
    {
        public short X;
        public short Y;
        public short Z;
    }
}
