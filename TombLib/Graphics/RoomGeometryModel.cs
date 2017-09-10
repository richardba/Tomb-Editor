﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Toolkit.Graphics;
using SharpDX.Toolkit;
using SharpDX;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;
using TombLib.Wad;

namespace TombLib.Graphics
{
    public class RoomGeometryModel : Model<RoomGeometryMesh, RoomGeometryVertex>
    {
        public float Scale { get; private set; }

        public RoomGeometryModel(GraphicsDevice device, float scale)
            : base(device, ModelType.RoomGeometry)
        {
            Scale = scale;
        }

        public override void BuildBuffers()
        {

        }
    }
}
