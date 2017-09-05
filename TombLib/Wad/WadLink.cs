﻿using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TombLib.Wad
{
    public struct WadLink
    {
        public WadLinkOpcode Opcode { get { return _opcode; } }
        public Vector3 Offset { get { return _offset; } set { _offset = value; } }

        private WadLinkOpcode _opcode;
        private Vector3 _offset;

        public WadLink(WadLinkOpcode opcode, Vector3 offset)
        {
            _opcode = opcode;
            _offset = offset;
        }
    }
}
