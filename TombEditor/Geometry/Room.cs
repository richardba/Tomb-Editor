﻿using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using TombEditor.Compilers;
using Buffer = SharpDX.Toolkit.Graphics.Buffer;
using TombLib.Utils;

namespace TombEditor.Geometry
{
    public enum Reverberation : byte
    {
        Outside, SmallRoom, MediumRoom, LargeRoom, Pipe
    }

    public enum BlockFaceShape : byte
    {
        Rectangle, Triangle
    }

    public class Room
    {
        public const short DefaultHeight = 12;
        public const short MaxRoomDimensions = 20;

        public string Name { get; set; }
        public Vector3 Position { get; set; }
        public Block[,] Blocks { get; private set; }
        private List<PositionBasedObjectInstance> _objects = new List<PositionBasedObjectInstance>();

        public Room AlternateBaseRoom { get; set; } = null;
        public Room AlternateRoom { get; set; } = null;
        public short AlternateGroup { get; set; } = -1;

        public Vector4 AmbientLight { get; set; } = new Vector4(0.25f, 0.25f, 0.25f, 1.0f); // Normalized float. (1.0 meaning normal brightness, 2.0 is the maximal brightness supported by tomb4.exe)
        public short WaterLevel { get; set; }
        public short MistLevel { get; set; }
        public short ReflectionLevel { get; set; }
        public bool FlagWater { get; set; }
        public bool FlagReflection { get; set; }
        public bool FlagMist { get; set; }
        public bool FlagSnow { get; set; }
        public bool FlagRain { get; set; }
        public bool FlagCold { get; set; }
        public bool FlagDamage { get; set; }
        public bool FlagQuickSand { get; set; }
        public bool FlagOutside { get; set; }
        public bool FlagHorizon { get; set; }
        public bool ExcludeFromPathFinding { get; set; }
        public Reverberation Reverberation { get; set; }

        // Internal data structures
        private Buffer<EditorVertex> _vertexBuffer;
        private List<EditorVertex>[,] _sectorVertices;
        public struct VertexRange
        {
            public int Start;
            public int Count;
        };
        private VertexRange[,,] _sectorFaceVertexVertexRange;
        private int[,] _sectorAllVerticesOffset;
        private readonly List<EditorVertex> _allVertices = new List<EditorVertex>();

        // Helper data for Prj2 loading
        public int Prj2AlternateRoomIndex { get; set; }
        public int Prj2AlternateBaseRoomIndex { get; set; }
        public List<uint> Prj2Portals { get; set; }
        public List<uint> Prj2Triggers { get; set; }

        public Room(Level level, int numXSectors, int numZSectors, string name = "Unnamed", short ceiling = DefaultHeight)
        {
            Name = name;
            Resize(level, numXSectors, numZSectors, 0, ceiling);
            Prj2Portals = new List<uint>();
            Prj2Triggers = new List<uint>();
        }

        public void Resize(Level level, int numXSectors, int numZSectors, short floor = 0, short ceiling = DefaultHeight, DrawingPoint offset = new DrawingPoint())
        {
            // Remove sector based objects if there are any
            var sector_objects = Blocks != null ? SectorObjects.ToList() : new List<SectorBasedObjectInstance>();
            foreach (var instance in sector_objects)
                RemoveObject(level, instance);
            DrawingPoint oldSectorSize = Blocks != null ? SectorSize : new DrawingPoint();

            // Build new blocks
            Block[,] newBlocks = new Block[numXSectors, numZSectors];
            for (int x = 0; x < numXSectors; x++)
                for (int z = 0; z < numZSectors; z++)
                {
                    Block oldBlock = GetBlockTry(new DrawingPoint(x, z).Offset(offset));
                    newBlocks[x, z] = oldBlock ?? new Block(floor, ceiling);
                    if (newBlocks[x, z].Type == BlockType.BorderWall)
                        newBlocks[x, z].Type = BlockType.Wall;
                    if (x == 0 || z == 0 || x == numXSectors - 1 || z == numZSectors - 1)
                        newBlocks[x, z].Type = BlockType.BorderWall;
                }

            // Update data structures
            _sectorVertices = new List<EditorVertex>[numXSectors, numZSectors];
            for (int x = 0; x < numXSectors; x++)
                for (int z = 0; z < numZSectors; z++)
                    _sectorVertices[x, z] = new List<EditorVertex>();
            _sectorFaceVertexVertexRange = new VertexRange[numXSectors, numZSectors, (int)Block.FaceCount];
            _sectorAllVerticesOffset = new int[numXSectors, numZSectors];

            Blocks = newBlocks;

            // Move objects
            SectorPos = SectorPos.Offset(offset);
            foreach (var instance in _objects)
                instance.Position -= new Vector3(offset.X * 1024, 0, offset.Y * 1024);

            // Add sector based objects again
            Rectangle newArea = new Rectangle(offset.X, offset.Y, numXSectors - 1, numZSectors - 1);
            foreach (var instance in sector_objects)
            {
                Rectangle instanceNewAreaConstraint = newArea.Inflate(-1);
                if (instance is Portal)
                    switch (((Portal)instance).Direction) // Special constraints for portals on walls
                    {
                        case PortalDirection.North:
                            if (newArea.Bottom != (oldSectorSize.Y - 1))
                                continue;
                            instanceNewAreaConstraint = newArea.Inflate(-1, 0);
                            break;
                        case PortalDirection.South:
                            if (newArea.Top != 0)
                                continue;
                            instanceNewAreaConstraint = newArea.Inflate(-1, 0);
                            break;
                        case PortalDirection.East:
                            if (newArea.Right != (oldSectorSize.X - 1))
                                continue;
                            instanceNewAreaConstraint = newArea.Inflate(0, -1);
                            break;
                        case PortalDirection.West:
                            if (newArea.Left != 0)
                                continue;
                            instanceNewAreaConstraint = newArea.Inflate(0, -1);
                            break;
                    }
                if (!instance.Area.Intersects(instanceNewAreaConstraint))
                    continue;
                Rectangle instanceNewArea = instance.Area.Intersect(instanceNewAreaConstraint).OffsetNeg(offset);
                if (instance is Portal)
                    AddBidirectionalPortalsToLevel(level, (Portal)instance.Clone(instanceNewArea));
                else
                    AddObject(level, instance.Clone(instanceNewArea));
            }

            // Update state
            UpdateCompletely();
        }

        public bool Flipped => (AlternateRoom != null) || (AlternateBaseRoom != null);

        public DrawingPoint SectorSize => new DrawingPoint(NumXSectors, NumZSectors);

        public DrawingPoint SectorPos
        {
            get { return new DrawingPoint((int)Position.X, (int)Position.Z); }
            set { Position = new Vector3(value.X, Position.Y, value.Y); }
        }

        public IEnumerable<Portal> Portals
        {
            get
            { // No LINQ because it is really slow.
                var portals = new HashSet<Portal>();
                foreach (var block in Blocks)
                    foreach (var portal in block.Portals)
                        portals.Add(portal);
                return portals;
            }
        }

        public IEnumerable<TriggerInstance> Triggers
        {
            get
            { // No LINQ because it is really slow.
                var triggers = new HashSet<TriggerInstance>();
                foreach (var block in Blocks)
                    foreach (var trigger in block.Triggers)
                        triggers.Add(trigger);
                return triggers;
            }
        }

        public IEnumerable<SectorBasedObjectInstance> SectorObjects
        {
            get
            {
                foreach (var instance in Portals)
                    yield return instance;
                foreach (var instance in Triggers)
                    yield return instance;
            }
        }

        public IReadOnlyList<PositionBasedObjectInstance> Objects => _objects;

        public IEnumerable<ObjectInstance> AnyObjects
        {
            get
            {
                foreach (var instance in Portals)
                    yield return instance;
                foreach (var instance in Triggers)
                    yield return instance;
                foreach (var instance in _objects)
                    yield return instance;
            }
        }

        public Rectangle WorldArea => new Rectangle((int)Position.X, (int)Position.Z, NumXSectors + (int)Position.X, NumZSectors + (int)Position.Z);

        public Block GetBlock(DrawingPoint pos)
        {
            return Blocks[pos.X, pos.Y];
        }

        public Block GetBlockTry(int x, int z)
        {
            if (Blocks == null)
                return null;
            if ((x >= 0) && (z >= 0) && (x < NumXSectors) && (z < NumZSectors))
                return Blocks[x, z];
            return null;
        }

        public Block GetBlockTry(DrawingPoint pos)
        {
            return GetBlockTry(pos.X, pos.Y);
        }

        public bool IsFaceDefined(int x, int z, BlockFace face)
        {
            return _sectorFaceVertexVertexRange[x, z, (int)face].Count != 0;
        }

        public VertexRange GetFaceVertexRange(int x, int z, BlockFace face)
        {
            VertexRange range = _sectorFaceVertexVertexRange[x, z, (int)face];
            int offset = _sectorAllVerticesOffset[x, z];
            return new VertexRange { Start = range.Start + offset, Count = range.Count };
        }

        public void BuildGeometry()
        {
            BuildGeometry(new Rectangle(0, 0, NumXSectors - 1, NumZSectors - 1));
        }

        public void BuildGeometry(Rectangle area)
        {
            // Adjust ranges
            int xMin = Math.Max(0, area.X);
            int xMax = Math.Min(NumXSectors - 1, area.Right);
            int zMin = Math.Max(0, area.Y);
            int zMax = Math.Min(NumZSectors - 1, area.Bottom);

            // Build face polygons
            for (int x = xMin; x <= xMax; x++)
            {
                for (int z = zMin; z <= zMax; z++)
                {
                    // Reset sector
                    for (BlockFace f = 0; f < Block.FaceCount; f++)
                    {
                        _sectorFaceVertexVertexRange[x, z, (int)f] = new VertexRange();
                        _sectorVertices[x, z].Clear();
                    }

                    // Save the height of the faces
                    int qa0 = Blocks[x, z].QAFaces[0];
                    int qa1 = Blocks[x, z].QAFaces[1];
                    int qa2 = Blocks[x, z].QAFaces[2];
                    int qa3 = Blocks[x, z].QAFaces[3];
                    int ws0 = Blocks[x, z].WSFaces[0];
                    int ws1 = Blocks[x, z].WSFaces[1];
                    int ws2 = Blocks[x, z].WSFaces[2];
                    int ws3 = Blocks[x, z].WSFaces[3];

                    // If x, z is one of the four corner then nothing has to be done
                    if ((x == 0 && z == 0) || (x == 0 && z == NumZSectors - 1) ||
                        (x == NumXSectors - 1 && z == NumZSectors - 1) || (x == NumXSectors - 1 && z == 0))
                        continue;

                    // Vertical polygons  ---------------------------------------------------------------------------------

                    // North
                    if (x > 0 && x < NumXSectors - 1 && z > 0 && z < NumZSectors - 2 &&
                        !(Blocks[x, z + 1].Type == BlockType.Wall &&
                         (Blocks[x, z + 1].FloorDiagonalSplit == DiagonalSplit.None || Blocks[x, z + 1].FloorDiagonalSplit == DiagonalSplit.NW || Blocks[x, z + 1].FloorDiagonalSplit == DiagonalSplit.NE)))
                    {
                        if ((Blocks[x, z].Type == BlockType.Wall || (Blocks[x, z].WallPortal != null &&
                            Blocks[x, z].WallOpacity != PortalOpacity.None)) &&
                            !(Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NW || Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NE))
                            AddVerticalFaces(x, z, FaceDirection.North, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.North, true, true, false);
                    }


                    // South
                    if (x > 0 && x < NumXSectors - 1 && z > 1 && z < NumZSectors - 1 &&
                        !(Blocks[x, z - 1].Type == BlockType.Wall &&
                         (Blocks[x, z - 1].FloorDiagonalSplit == DiagonalSplit.None || Blocks[x, z - 1].FloorDiagonalSplit == DiagonalSplit.SW || Blocks[x, z - 1].FloorDiagonalSplit == DiagonalSplit.SE)))
                    {
                        if ((Blocks[x, z].Type == BlockType.Wall ||
                            (Blocks[x, z].WallPortal != null &&
                            Blocks[x, z].WallOpacity != PortalOpacity.None)) &&
                            !(Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SW || Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SE))
                            AddVerticalFaces(x, z, FaceDirection.South, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.South, true, true, false);
                    }

                    // East
                    if (z > 0 && z < NumZSectors - 1 && x > 0 && x < NumXSectors - 2 &&
                        !(Blocks[x + 1, z].Type == BlockType.Wall &&
                        (Blocks[x + 1, z].FloorDiagonalSplit == DiagonalSplit.None || Blocks[x + 1, z].FloorDiagonalSplit == DiagonalSplit.NE || Blocks[x + 1, z].FloorDiagonalSplit == DiagonalSplit.SE)))
                    {
                        if ((Blocks[x, z].Type == BlockType.Wall || (Blocks[x, z].WallPortal != null &&
                             Blocks[x, z].WallOpacity != PortalOpacity.None)) &&
                            !(Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NE || Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SE))
                            AddVerticalFaces(x, z, FaceDirection.East, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.East, true, true, false);
                    }

                    // West
                    if (z > 0 && z < NumZSectors - 1 && x > 1 && x < NumXSectors - 1 &&
                        !(Blocks[x - 1, z].Type == BlockType.Wall &&
                        (Blocks[x - 1, z].FloorDiagonalSplit == DiagonalSplit.None || Blocks[x - 1, z].FloorDiagonalSplit == DiagonalSplit.NW || Blocks[x - 1, z].FloorDiagonalSplit == DiagonalSplit.SW)))
                    {
                        if ((Blocks[x, z].Type == BlockType.Wall || (Blocks[x, z].WallPortal != null &&
                             Blocks[x, z].WallOpacity != PortalOpacity.None)) &&
                            !(Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NW || Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SW))
                            AddVerticalFaces(x, z, FaceDirection.West, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.West, true, true, false);
                    }

                    // Diagonal faces
                    if (Blocks[x, z].FloorDiagonalSplit != DiagonalSplit.None)
                    {
                        if (Blocks[x, z].Type == BlockType.Wall)
                        {
                            AddVerticalFaces(x, z, FaceDirection.DiagonalFloor, true, true, true);
                        }
                        else
                        {
                            AddVerticalFaces(x, z, FaceDirection.DiagonalFloor, true, false, false);
                        }
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit != DiagonalSplit.None)
                    {
                        if (Blocks[x, z].Type != BlockType.Wall)
                        {
                            AddVerticalFaces(x, z, FaceDirection.DiagonalCeiling, false, true, false);
                        }
                    }

                    // North border wall
                    if (z == 0 && x != 0 && x != NumXSectors - 1 &&
                        !(Blocks[x, 1].Type == BlockType.Wall &&
                         (Blocks[x, 1].FloorDiagonalSplit == DiagonalSplit.None || Blocks[x, 1].FloorDiagonalSplit == DiagonalSplit.NW || Blocks[x, 1].FloorDiagonalSplit == DiagonalSplit.NE)))
                    {
                        bool addMiddle = false;

                        if (Blocks[x, z].WallPortal != null)
                        {
                            var portal = FindPortal(x, z, PortalDirection.South);
                            var adjoiningRoom = portal.AdjoiningRoom;
                            if (Flipped && AlternateBaseRoom != null)
                            {
                                if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                    adjoiningRoom = adjoiningRoom.AlternateRoom;
                            }

                            int facingX = x + (int)(Position.X - adjoiningRoom.Position.X);

                            if (adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].Type == BlockType.Wall &&
                                (adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].FloorDiagonalSplit == DiagonalSplit.None ||
                                 adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].FloorDiagonalSplit == DiagonalSplit.SE ||
                                 adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].FloorDiagonalSplit == DiagonalSplit.SW))
                            {
                                addMiddle = true;
                            }
                        }


                        if (addMiddle || (Blocks[x, z].Type == BlockType.BorderWall && Blocks[x, z].WallPortal == null) || (Blocks[x, z].WallPortal != null && Blocks[x, z].WallOpacity != PortalOpacity.None))
                            AddVerticalFaces(x, z, FaceDirection.North, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.North, true, true, false);
                    }

                    // South border wall
                    if (z == NumZSectors - 1 && x != 0 && x != NumXSectors - 1 &&
                        !(Blocks[x, NumZSectors - 2].Type == BlockType.Wall &&
                         (Blocks[x, NumZSectors - 2].FloorDiagonalSplit == DiagonalSplit.None || Blocks[x, NumZSectors - 2].FloorDiagonalSplit == DiagonalSplit.SW || Blocks[x, NumZSectors - 2].FloorDiagonalSplit == DiagonalSplit.SE)))
                    {
                        bool addMiddle = false;

                        if (Blocks[x, z].WallPortal != null)
                        {
                            var portal = FindPortal(x, z, PortalDirection.North);
                            var adjoiningRoom = portal.AdjoiningRoom;
                            if (Flipped && AlternateBaseRoom != null)
                            {
                                if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                    adjoiningRoom = adjoiningRoom.AlternateRoom;
                            }

                            int facingX = x + (int)(Position.X - adjoiningRoom.Position.X);

                            if (adjoiningRoom.Blocks[facingX, 1].Type == BlockType.Wall &&
                                (adjoiningRoom.Blocks[facingX, 1].FloorDiagonalSplit == DiagonalSplit.None ||
                                 adjoiningRoom.Blocks[facingX, 1].FloorDiagonalSplit == DiagonalSplit.NE ||
                                 adjoiningRoom.Blocks[facingX, 1].FloorDiagonalSplit == DiagonalSplit.NW))
                            {
                                addMiddle = true;
                            }
                        }

                        if (addMiddle || (Blocks[x, z].Type == BlockType.BorderWall && Blocks[x, z].WallPortal == null) || (Blocks[x, z].WallPortal != null && Blocks[x, z].WallOpacity != PortalOpacity.None))
                            AddVerticalFaces(x, z, FaceDirection.South, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.South, true, true, false);
                    }

                    // West border wall
                    if (x == 0 && z != 0 && z != NumZSectors - 1 &&
                        !(Blocks[1, z].Type == BlockType.Wall &&
                         (Blocks[1, z].FloorDiagonalSplit == DiagonalSplit.None || Blocks[1, z].FloorDiagonalSplit == DiagonalSplit.NE || Blocks[1, z].FloorDiagonalSplit == DiagonalSplit.SE)))
                    {
                        bool addMiddle = false;

                        if (Blocks[x, z].WallPortal != null)
                        {
                            var portal = FindPortal(x, z, PortalDirection.West);
                            var adjoiningRoom = portal.AdjoiningRoom;
                            if (Flipped && AlternateBaseRoom != null)
                            {
                                if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                    adjoiningRoom = adjoiningRoom.AlternateRoom;
                            }

                            int facingZ = z + (int)(Position.Z - adjoiningRoom.Position.Z);

                            if (adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].Type == BlockType.Wall &&
                                (adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].FloorDiagonalSplit == DiagonalSplit.None ||
                                 adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].FloorDiagonalSplit == DiagonalSplit.NW ||
                                 adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].FloorDiagonalSplit == DiagonalSplit.SW))
                            {
                                addMiddle = true;
                            }
                        }

                        if (addMiddle || (Blocks[x, z].Type == BlockType.BorderWall && Blocks[x, z].WallPortal == null) || (Blocks[x, z].WallPortal != null && Blocks[x, z].WallOpacity != PortalOpacity.None))
                            AddVerticalFaces(x, z, FaceDirection.East, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.East, true, true, false);
                    }

                    // East border wall
                    if (x == NumXSectors - 1 && z != 0 && z != NumZSectors - 1 &&
                        !(Blocks[NumXSectors - 2, z].Type == BlockType.Wall &&
                         (Blocks[NumXSectors - 2, z].FloorDiagonalSplit == DiagonalSplit.None || Blocks[NumXSectors - 2, z].FloorDiagonalSplit == DiagonalSplit.NW || Blocks[NumXSectors - 2, z].FloorDiagonalSplit == DiagonalSplit.SW)))
                    {
                        bool addMiddle = false;

                        if (Blocks[x, z].WallPortal != null)
                        {
                            var portal = FindPortal(x, z, PortalDirection.East);
                            var adjoiningRoom = portal.AdjoiningRoom;
                            if (Flipped && AlternateBaseRoom != null)
                            {
                                if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                    adjoiningRoom = adjoiningRoom.AlternateRoom;
                            }

                            int facingZ = z + (int)(Position.Z - adjoiningRoom.Position.Z);

                            if (adjoiningRoom.Blocks[1, facingZ].Type == BlockType.Wall &&
                                (adjoiningRoom.Blocks[1, facingZ].FloorDiagonalSplit == DiagonalSplit.None ||
                                 adjoiningRoom.Blocks[1, facingZ].FloorDiagonalSplit == DiagonalSplit.NE ||
                                 adjoiningRoom.Blocks[1, facingZ].FloorDiagonalSplit == DiagonalSplit.SE))
                            {
                                addMiddle = true;
                            }
                        }

                        if (addMiddle || (Blocks[x, z].Type == BlockType.BorderWall && Blocks[x, z].WallPortal == null) || (Blocks[x, z].WallPortal != null && Blocks[x, z].WallOpacity != PortalOpacity.None))
                            AddVerticalFaces(x, z, FaceDirection.West, true, true, true);
                        else
                            AddVerticalFaces(x, z, FaceDirection.West, true, true, false);
                    }

                    // If is a non diagonal wall, then continue
                    if (Blocks[x, z].Type == BlockType.Wall && Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.None)
                        continue;

                    //
                    // 1----2    Split 0: 231 413  
                    // | \  |    Split 1: 124 342
                    // |  \ |
                    // 4----3
                    //

                    //
                    // 1----2    Split 0: 231 413  
                    // |  / |    Split 1: 124 342
                    // | /  |
                    // 4----3
                    //

                    // Floor polygons ---------------------------------------------------------------------------------
                    var face = Blocks[x, z].GetFaceTexture(BlockFace.Floor);
                    
                    if (Block.IsQuad(qa0, qa1, qa2, qa3) || (Blocks[x, z].FloorDiagonalSplit != DiagonalSplit.None))
                    {
                        if ((Blocks[x, z].FloorPortal == null && Blocks[x, z].Type == BlockType.Floor) ||
                            (Blocks[x, z].FloorPortal != null && (Blocks[x, z].FloorOpacity != PortalOpacity.None || IsFloorSolid(new DrawingPoint(x, z)))) ||
                             Blocks[x, z].Type == BlockType.Wall)
                        {
                            if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.None)
                            {
                                AddRectangle(x, z, BlockFace.Floor,
                                    new Vector3(x * 1024.0f, qa0 * 256.0f, (z + 1) * 1024.0f),
                                    new Vector3((x + 1) * 1024.0f, qa1 * 256.0f, (z + 1) * 1024.0f),
                                    new Vector3((x + 1) * 1024.0f, qa2 * 256.0f, z * 1024.0f),
                                    new Vector3(x * 1024.0f, qa3 * 256.0f, z * 1024.0f),
                                    face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f));
                            }
                            else
                            {
                                bool splitDirection = Blocks[x, z].FloorSplitDirectionIsXEqualsY;

                                bool addTriangle1 = true;
                                bool addTriangle2 = true;

                                int y1 = 0;
                                int y2 = 0;
                                int y3 = 0;
                                int y4 = 0;
                                int y5 = 0;
                                int y6 = 0;

                                if (Blocks[x, z].FloorDiagonalSplit != DiagonalSplit.None)
                                {
                                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NW)
                                    {
                                        splitDirection = true;
                                        addTriangle1 = true;
                                        addTriangle2 = Blocks[x, z].Type != BlockType.Wall;
                                        y1 = qa0;
                                        y2 = qa0;
                                        y3 = qa0;
                                        y4 = qa2;
                                        y5 = qa3;
                                        y6 = qa1;
                                    }

                                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NE)
                                    {
                                        splitDirection = false;
                                        addTriangle1 = true;
                                        addTriangle2 = Blocks[x, z].Type != BlockType.Wall;
                                        y1 = qa1;
                                        y2 = qa1;
                                        y3 = qa1;
                                        y4 = qa3;
                                        y5 = qa0;
                                        y6 = qa2;
                                    }

                                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SE)
                                    {
                                        splitDirection = true;
                                        addTriangle1 = Blocks[x, z].Type != BlockType.Wall;
                                        addTriangle2 = true;
                                        y1 = qa0;
                                        y2 = qa1;
                                        y3 = qa3;
                                        y4 = qa2;
                                        y5 = qa2;
                                        y6 = qa2;
                                    }

                                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SW)
                                    {
                                        splitDirection = false;
                                        addTriangle1 = Blocks[x, z].Type != BlockType.Wall;
                                        addTriangle2 = true;
                                        y1 = qa1;
                                        y2 = qa2;
                                        y3 = qa0;
                                        y4 = qa3;
                                        y5 = qa3;
                                        y6 = qa3;
                                    }
                                }
                                else
                                {
                                    if (!splitDirection)
                                    {
                                        addTriangle1 = true;
                                        addTriangle2 = true;
                                        y1 = qa1;
                                        y2 = qa2;
                                        y3 = qa0;
                                        y4 = qa3;
                                        y5 = qa0;
                                        y6 = qa2;
                                    }
                                    else
                                    {
                                        addTriangle1 = true;
                                        addTriangle2 = true;
                                        y1 = qa0;
                                        y2 = qa1;
                                        y3 = qa3;
                                        y4 = qa2;
                                        y5 = qa3;
                                        y6 = qa1;
                                    }
                                }

                                if (!splitDirection)
                                {
                                    if (addTriangle1)
                                    {
                                        AddTriangle(x, z, BlockFace.Floor,
                                            new Vector3(x * 1024.0f, y3 * 256.0f, (z + 1) * 1024.0f),
                                            new Vector3((x + 1) * 1024.0f, y1 * 256.0f, (z + 1) * 1024.0f),
                                            new Vector3((x + 1) * 1024.0f, y2 * 256.0f, z * 1024.0f),
											face, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f),  false);
                                    }

                                    if (addTriangle2)
                                    {
                                        face = Blocks[x, z].GetFaceTexture(BlockFace.FloorTriangle2);
                                        AddTriangle(x, z, BlockFace.FloorTriangle2,
                                            new Vector3((x + 1) * 1024.0f, y6 * 256.0f, z * 1024.0f),
                                            new Vector3(x * 1024.0f, y4 * 256.0f, z * 1024.0f),
                                            new Vector3(x * 1024.0f, y5 * 256.0f, (z + 1) * 1024.0f),
											face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), false);
                                    }
                                }
                                else
                                {
                                    if (addTriangle1)
                                    {
                                        AddTriangle(x, z, BlockFace.Floor,
                                            new Vector3(x * 1024.0f, y3 * 256.0f, z * 1024.0f),
                                            new Vector3(x * 1024.0f, y1 * 256.0f, (z + 1) * 1024.0f),
                                            new Vector3((x + 1) * 1024.0f, y2 * 256.0f, (z + 1) * 1024.0f),
											face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), true);
                                    }

                                    if (addTriangle2)
                                    {
                                        face = Blocks[x, z].GetFaceTexture(BlockFace.FloorTriangle2);
                                        AddTriangle(x, z, BlockFace.FloorTriangle2,
                                            new Vector3((x + 1) * 1024.0f, y6 * 256.0f, (z + 1) * 1024.0f),
                                            new Vector3((x + 1) * 1024.0f, y4 * 256.0f, z * 1024.0f),
                                            new Vector3(x * 1024.0f, y5 * 256.0f, z * 1024.0f), 
											face, new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f), true);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if ((Blocks[x, z].FloorPortal == null && Blocks[x, z].Type == BlockType.Floor) ||
                            (Blocks[x, z].FloorPortal != null && (Blocks[x, z].FloorOpacity != PortalOpacity.None || IsFloorSolid(new DrawingPoint(x, z)))))
                        {
                            if (!Blocks[x, z].FloorSplitDirectionIsXEqualsY)
                            {
                                AddTriangle(x, z, BlockFace.Floor,
                                    new Vector3(x * 1024.0f, qa0 * 256.0f, (z + 1) * 1024.0f),
                                    new Vector3((x + 1) * 1024.0f, qa1 * 256.0f, (z + 1) * 1024.0f),
                                    new Vector3((x + 1) * 1024.0f, qa2 * 256.0f, z * 1024.0f),
                                    face, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f),  false);

                                face = Blocks[x, z].GetFaceTexture(BlockFace.FloorTriangle2);
                                AddTriangle(x, z, BlockFace.FloorTriangle2,
                                    new Vector3((x + 1) * 1024.0f, qa2 * 256.0f, z * 1024.0f),
                                    new Vector3(x * 1024.0f, qa3 * 256.0f, z * 1024.0f),
                                    new Vector3(x * 1024.0f, qa0 * 256.0f, (z + 1) * 1024.0f),
                                    face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f),  false);
                            }
                            else
                            {
                                AddTriangle(x, z, BlockFace.Floor,
                                    new Vector3(x * 1024.0f, qa3 * 256.0f, z * 1024.0f),
                                    new Vector3(x * 1024.0f, qa0 * 256.0f, (z + 1) * 1024.0f),
                                    new Vector3((x + 1) * 1024.0f, qa1 * 256.0f, (z + 1) * 1024.0f),
                                    face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f),  true);

                                face = Blocks[x, z].GetFaceTexture(BlockFace.FloorTriangle2);
                                AddTriangle(x, z, BlockFace.FloorTriangle2,
                                    new Vector3((x + 1) * 1024.0f, qa1 * 256.0f, (z + 1) * 1024.0f),
                                    new Vector3((x + 1) * 1024.0f, qa2 * 256.0f, z * 1024.0f),
                                    new Vector3(x * 1024.0f, qa3 * 256.0f, z * 1024.0f),
                                    face, new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f),  true);
                            }
                        }
                    }

                    // Ceiling polygons ---------------------------------------------------------------------------------
                    //
                    //  2----1    Split 0: 142 324  
                    //  | \  |    Split 1: 213 431
                    //  |  \ |
                    //  3----4
                    //

                    face = Blocks[x, z].GetFaceTexture(BlockFace.Ceiling);
                    
                    if (Block.IsQuad(ws0, ws1, ws2, ws3) || (Blocks[x, z].CeilingDiagonalSplit != DiagonalSplit.None))
                    {
                        if (!((Blocks[x, z].CeilingPortal == null && Blocks[x, z].Type == BlockType.Floor) ||
                            (Blocks[x, z].CeilingPortal != null && (Blocks[x, z].CeilingOpacity != PortalOpacity.None || IsCeilingSolid(new DrawingPoint(x, z)))) ||
                             Blocks[x, z].Type == BlockType.Wall))
                            continue;

                        if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.None)
                        {
                            AddRectangle(x, z, BlockFace.Ceiling,
                                new Vector3((x + 1) * 1024.0f, ws1 * 256.0f, (z + 1) * 1024.0f),
                                new Vector3((x) * 1024.0f, ws0 * 256.0f, (z + 1) * 1024.0f),
                                new Vector3((x) * 1024.0f, ws3 * 256.0f, (z) * 1024.0f),
                                new Vector3((x + 1) * 1024.0f, ws2 * 256.0f, (z) * 1024.0f),
                                face, new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f));
                        }
                        else
                        {
                            bool splitDirection = Blocks[x, z].CeilingSplitDirectionIsXEqualsY;

                            bool addTriangle1 = true;
                            bool addTriangle2 = true;

                            int y1 = 0;
                            int y2 = 0;
                            int y3 = 0;
                            int y4 = 0;
                            int y5 = 0;
                            int y6 = 0;

                            if (Blocks[x, z].CeilingDiagonalSplit != DiagonalSplit.None)
                            {
                                if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.NW)
                                {
                                    splitDirection = false;
                                    addTriangle1 = true;
                                    addTriangle2 = (Blocks[x, z].Type != BlockType.Wall);
                                    y1 = ws0;
                                    y2 = ws0;
                                    y3 = ws0;
                                    y4 = ws2;
                                    y5 = ws1;
                                    y6 = ws3;
                                }

                                if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.NE)
                                {
                                    splitDirection = true;
                                    addTriangle1 = true;
                                    addTriangle2 = (Blocks[x, z].Type != BlockType.Wall);
                                    y1 = ws1;
                                    y2 = ws1;
                                    y3 = ws1;
                                    y4 = ws3;
                                    y5 = ws2;
                                    y6 = ws0;
                                }

                                if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.SE)
                                {
                                    splitDirection = false;
                                    addTriangle1 = (Blocks[x, z].Type != BlockType.Wall);
                                    addTriangle2 = true;
                                    y1 = ws0;
                                    y2 = ws3;
                                    y3 = ws1;
                                    y4 = ws2;
                                    y5 = ws2;
                                    y6 = ws2;
                                }

                                if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.SW)
                                {
                                    splitDirection = true;
                                    addTriangle1 = (Blocks[x, z].Type != BlockType.Wall);
                                    addTriangle2 = true;
                                    y1 = ws1;
                                    y2 = ws0;
                                    y3 = ws2;
                                    y4 = ws3;
                                    y5 = ws3;
                                    y6 = ws3;
                                }
                            }
                            else
                            {
                                if (!splitDirection)
                                {
                                    addTriangle1 = true;
                                    addTriangle2 = true;
                                    y1 = ws0;
                                    y2 = ws3;
                                    y3 = ws1;
                                    y4 = ws2;
                                    y5 = ws1;
                                    y6 = ws3;
                                }
                                else
                                {
                                    addTriangle1 = true;
                                    addTriangle2 = true;
                                    y1 = ws1;
                                    y2 = ws0;
                                    y3 = ws2;
                                    y4 = ws3;
                                    y5 = ws2;
                                    y6 = ws0;
                                }
                            }

                            if (!splitDirection)
                            {
                                if (addTriangle1)
                                {
                                    AddTriangle(x, z, BlockFace.Ceiling,
                                        new Vector3((x + 1) * 1024.0f, y3 * 256.0f, (z + 1) * 1024.0f),
                                        new Vector3(x * 1024.0f, y1 * 256.0f, (z + 1) * 1024.0f),
                                        new Vector3(x * 1024.0f, y2 * 256.0f, z * 1024.0f),
                                        face, new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f),  true);
                                }

                                if (addTriangle2)
                                {
                                    face = Blocks[x, z].GetFaceTexture(BlockFace.CeilingTriangle2);
                                    AddTriangle(x, z, BlockFace.CeilingTriangle2,
                                        new Vector3((x) * 1024.0f, y6 * 256.0f, z * 1024.0f),
                                        new Vector3((x + 1) * 1024.0f, y4 * 256.0f, (z) * 1024.0f),
                                        new Vector3((x + 1) * 1024.0f, y5 * 256.0f, (z + 1) * 1024.0f),
                                        face, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f),  true);
                                }
                            }
                            else
                            {
                                if (addTriangle1)
                                {
                                    AddTriangle(x, z, BlockFace.Ceiling,
                                        new Vector3((x + 1) * 1024.0f, y3 * 256.0f, (z) * 1024.0f),
                                        new Vector3((x + 1) * 1024.0f, y1 * 256.0f, (z + 1) * 1024.0f),
                                        new Vector3((x) * 1024.0f, y2 * 256.0f, (z + 1) * 1024.0f),
                                        face, new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f),  false);
                                }

                                if (addTriangle2)
                                {
                                    face = Blocks[x, z].GetFaceTexture(BlockFace.CeilingTriangle2);
                                    AddTriangle(x, z, BlockFace.CeilingTriangle2,
                                        new Vector3((x) * 1024.0f, y6 * 256.0f, (z + 1) * 1024.0f),
                                        new Vector3(x * 1024.0f, y4 * 256.0f, (z) * 1024.0f),
                                        new Vector3((x + 1) * 1024.0f, y5 * 256.0f, z * 1024.0f),
                                        face, new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f),  false);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!((Blocks[x, z].CeilingPortal == null && Blocks[x, z].Type == BlockType.Floor) ||
                            (Blocks[x, z].CeilingPortal != null && (Blocks[x, z].CeilingOpacity != PortalOpacity.None || IsCeilingSolid(new DrawingPoint(x, z))))))
                            continue;

                        if (!Blocks[x, z].CeilingSplitDirectionIsXEqualsY)
                        {
                            AddTriangle(x, z, BlockFace.Ceiling,
                                new Vector3((x + 1) * 1024.0f, ws1 * 256.0f, (z + 1) * 1024.0f),
                                new Vector3(x * 1024.0f, ws0 * 256.0f, (z + 1) * 1024.0f),
                                new Vector3(x * 1024.0f, ws3 * 256.0f, z * 1024.0f), 
                                face, new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f),  true);

                            face = Blocks[x, z].GetFaceTexture(BlockFace.CeilingTriangle2);
                            AddTriangle(x, z, BlockFace.CeilingTriangle2,
                                new Vector3((x) * 1024.0f, ws3 * 256.0f, z * 1024.0f),
                                new Vector3((x + 1) * 1024.0f, ws2 * 256.0f, (z) * 1024.0f),
                                new Vector3((x + 1) * 1024.0f, ws1 * 256.0f, (z + 1) * 1024.0f),
                                face, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f),  true);
                        }
                        else
                        {
                            AddTriangle(x, z, BlockFace.Ceiling,
                                new Vector3((x + 1) * 1024.0f, ws2 * 256.0f, (z) * 1024.0f),
                                new Vector3((x + 1) * 1024.0f, ws1 * 256.0f, (z + 1) * 1024.0f),
                                new Vector3((x) * 1024.0f, ws0 * 256.0f, (z + 1) * 1024.0f),
                                face, new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f),  false);

                            face = Blocks[x, z].GetFaceTexture(BlockFace.CeilingTriangle2);
                            AddTriangle(x, z, BlockFace.CeilingTriangle2,
                                new Vector3((x) * 1024.0f, ws0 * 256.0f, (z + 1) * 1024.0f),
                                new Vector3(x * 1024.0f, ws3 * 256.0f, (z) * 1024.0f),
                                new Vector3((x + 1) * 1024.0f, ws2 * 256.0f, z * 1024.0f),
                                face, new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f),  false);
                        }
                    }
                }
            }

            // Collect all vertices
            _allVertices.Clear();
            for (int x = 0; x < NumXSectors; x++)
                for (int z = 0; z < NumZSectors; z++)
                {
                    _sectorAllVerticesOffset[x, z] = _allVertices.Count;
                    _allVertices.AddRange(_sectorVertices[x, z]);
                }
        }

        public void UpdateCompletely()
        {
            BuildGeometry();
            CalculateLightingForThisRoom();
            UpdateBuffers();
        }

        private enum FaceDirection
        {
            North, South, East, West, DiagonalFloor, DiagonalCeiling, DiagonalWall
        }

        private void AddVerticalFaces(int x, int z, FaceDirection direction, bool floor, bool ceiling, bool middle)
        {
            int xA, xB, zA, zB, yA, yB;

            Block otherBlock;
            TextureArea face;

            BlockFace qaFace, edFace, wsFace, rfFace, middleFace;
            int qA, qB, eA, eB, rA, rB, wA, wB, fA, fB, cA, cB;

            switch (direction)
            {
                case FaceDirection.North:
                    xA = x + 1;
                    xB = x;
                    zA = z + 1;
                    zB = z + 1;
                    otherBlock = Blocks[x, z + 1];
                    qA = Blocks[x, z].QAFaces[1];
                    qB = Blocks[x, z].QAFaces[0];
                    eA = Blocks[x, z].EDFaces[1];
                    eB = Blocks[x, z].EDFaces[0];
                    rA = Blocks[x, z].RFFaces[1];
                    rB = Blocks[x, z].RFFaces[0];
                    wA = Blocks[x, z].WSFaces[1];
                    wB = Blocks[x, z].WSFaces[0];
                    fA = otherBlock.QAFaces[2];
                    fB = otherBlock.QAFaces[3];
                    cA = otherBlock.WSFaces[2];
                    cB = otherBlock.WSFaces[3];
                    qaFace = BlockFace.NorthQA;
                    edFace = BlockFace.NorthED;
                    middleFace = BlockFace.NorthMiddle;
                    rfFace = BlockFace.NorthRF;
                    wsFace = BlockFace.NorthWS;

                    if (Blocks[x, z].WallPortal != null)
                    {
                        var portal = FindPortal(x, z, PortalDirection.South);
                        var adjoiningRoom = portal.AdjoiningRoom;
                        if (Flipped && AlternateBaseRoom != null)
                        {
                            if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                adjoiningRoom = adjoiningRoom.AlternateRoom;
                        }

                        int facingX = x + (int)(Position.X - adjoiningRoom.Position.X);

                        int qAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].QAFaces[1];
                        int qBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].QAFaces[0];
                        qA = (int)Position.Y + Blocks[x, 1].QAFaces[2];
                        qB = (int)Position.Y + Blocks[x, 1].QAFaces[3];
                        qA = Math.Max(qA, qAportal) - (int)Position.Y;
                        qB = Math.Max(qB, qBportal) - (int)Position.Y;

                        int wAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].WSFaces[1];
                        int wBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, adjoiningRoom.NumZSectors - 2].WSFaces[0];
                        wA = (int)Position.Y + Blocks[x, 1].WSFaces[2];
                        wB = (int)Position.Y + Blocks[x, 1].WSFaces[3];
                        wA = Math.Min(wA, wAportal) - (int)Position.Y;
                        wB = Math.Min(wB, wBportal) - (int)Position.Y;

                        Blocks[x, z].QAFaces[1] = (short)qA;
                        Blocks[x, z].QAFaces[0] = (short)qB;
                        Blocks[x, z].WSFaces[1] = (short)wA;
                        Blocks[x, z].WSFaces[0] = (short)wB;
                    }

                    if (Blocks[x, z].Type == BlockType.BorderWall)
                    {
                        if (Blocks[x, 1].WSFaces[2] < Blocks[x, z].QAFaces[1])
                        {
                            qA = Blocks[x, 1].WSFaces[2];
                            Blocks[x, z].QAFaces[1] = (short)qA;
                        }

                        if (Blocks[x, 1].WSFaces[3] < Blocks[x, z].QAFaces[0])
                        {
                            qB = Blocks[x, 1].WSFaces[3];
                            Blocks[x, z].QAFaces[0] = (short)qB;
                        }
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NW)
                    {
                        qA = Blocks[x, z].QAFaces[0];
                        qB = Blocks[x, z].QAFaces[0];
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NE)
                    {
                        qA = Blocks[x, z].QAFaces[1];
                        qB = Blocks[x, z].QAFaces[1];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.SE)
                    {
                        fA = otherBlock.QAFaces[2];
                        fB = otherBlock.QAFaces[2];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.SW)
                    {
                        fA = otherBlock.QAFaces[3];
                        fB = otherBlock.QAFaces[3];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.NW)
                    {
                        wA = Blocks[x, z].WSFaces[0];
                        wB = Blocks[x, z].WSFaces[0];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.NE)
                    {
                        wA = Blocks[x, z].WSFaces[1];
                        wB = Blocks[x, z].WSFaces[1];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.SE)
                    {
                        cA = otherBlock.WSFaces[2];
                        cB = otherBlock.WSFaces[2];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.SW)
                    {
                        cA = otherBlock.WSFaces[3];
                        cB = otherBlock.WSFaces[3];
                    }

                    break;

                case FaceDirection.South:
                    xA = x;
                    xB = x + 1;
                    zA = z;
                    zB = z;
                    otherBlock = Blocks[x, z - 1];
                    qA = Blocks[x, z].QAFaces[3];
                    qB = Blocks[x, z].QAFaces[2];
                    eA = Blocks[x, z].EDFaces[3];
                    eB = Blocks[x, z].EDFaces[2];
                    rA = Blocks[x, z].RFFaces[3];
                    rB = Blocks[x, z].RFFaces[2];
                    wA = Blocks[x, z].WSFaces[3];
                    wB = Blocks[x, z].WSFaces[2];
                    fA = otherBlock.QAFaces[0];
                    fB = otherBlock.QAFaces[1];
                    cA = otherBlock.WSFaces[0];
                    cB = otherBlock.WSFaces[1];
                    qaFace = BlockFace.SouthQA;
                    edFace = BlockFace.SouthED;
                    middleFace = BlockFace.SouthMiddle;
                    rfFace = BlockFace.SouthRF;
                    wsFace = BlockFace.SouthWS;

                    if (Blocks[x, z].WallPortal != null)
                    {
                        var portal = FindPortal(x, z, PortalDirection.North);
                        var adjoiningRoom = portal.AdjoiningRoom;
                        if (Flipped && AlternateBaseRoom != null)
                        {
                            if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                adjoiningRoom = adjoiningRoom.AlternateRoom;
                        }

                        int facingX = x + (int)(Position.X - adjoiningRoom.Position.X);
                        int qAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, 1].QAFaces[3];
                        int qBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, 1].QAFaces[2];
                        qA = (int)Position.Y + Blocks[x, NumZSectors - 2].QAFaces[0];
                        qB = (int)Position.Y + Blocks[x, NumZSectors - 2].QAFaces[1];
                        qA = Math.Max(qA, qAportal) - (int)Position.Y;
                        qB = Math.Max(qB, qBportal) - (int)Position.Y;

                        int wAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, 1].WSFaces[3];
                        int wBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[facingX, 1].WSFaces[2];
                        wA = (int)Position.Y + Blocks[x, NumZSectors - 2].WSFaces[0];
                        wB = (int)Position.Y + Blocks[x, NumZSectors - 2].WSFaces[1];
                        wA = Math.Min(wA, wAportal) - (int)Position.Y;
                        wB = Math.Min(wB, wBportal) - (int)Position.Y;

                        Blocks[x, z].QAFaces[3] = (short)qA;
                        Blocks[x, z].QAFaces[2] = (short)qB;
                        Blocks[x, z].WSFaces[3] = (short)wA;
                        Blocks[x, z].WSFaces[2] = (short)wB;
                    }

                    if (Blocks[x, z].Type == BlockType.BorderWall)
                    {
                        if (Blocks[x, NumZSectors - 2].WSFaces[0] < Blocks[x, z].QAFaces[3])
                        {
                            qA = Blocks[x, NumZSectors - 2].WSFaces[0];
                            Blocks[x, z].QAFaces[3] = (short)qA;
                        }

                        if (Blocks[x, NumZSectors - 2].WSFaces[1] < Blocks[x, z].QAFaces[2])
                        {
                            qB = Blocks[x, NumZSectors - 2].WSFaces[1];
                            Blocks[x, z].QAFaces[2] = (short)qB;
                        }
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SW)
                    {
                        qA = Blocks[x, z].QAFaces[3];
                        qB = Blocks[x, z].QAFaces[3];
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SE)
                    {
                        qA = Blocks[x, z].QAFaces[2];
                        qB = Blocks[x, z].QAFaces[2];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.NW)
                    {
                        fA = otherBlock.QAFaces[0];
                        fB = otherBlock.QAFaces[0];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.NE)
                    {
                        fA = otherBlock.QAFaces[1];
                        fB = otherBlock.QAFaces[1];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.SW)
                    {
                        wA = Blocks[x, z].WSFaces[3];
                        wB = Blocks[x, z].WSFaces[3];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.SE)
                    {
                        wA = Blocks[x, z].WSFaces[2];
                        wB = Blocks[x, z].WSFaces[2];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.NW)
                    {
                        cA = otherBlock.WSFaces[0];
                        cB = otherBlock.WSFaces[0];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.NE)
                    {
                        cA = otherBlock.WSFaces[1];
                        cB = otherBlock.WSFaces[1];
                    }

                    break;

                case FaceDirection.East:
                    xA = x + 1;
                    xB = x + 1;
                    zA = z;
                    zB = z + 1;
                    otherBlock = Blocks[x + 1, z];
                    qA = Blocks[x, z].QAFaces[2];
                    qB = Blocks[x, z].QAFaces[1];
                    eA = Blocks[x, z].EDFaces[2];
                    eB = Blocks[x, z].EDFaces[1];
                    rA = Blocks[x, z].RFFaces[2];
                    rB = Blocks[x, z].RFFaces[1];
                    wA = Blocks[x, z].WSFaces[2];
                    wB = Blocks[x, z].WSFaces[1];
                    fA = otherBlock.QAFaces[3];
                    fB = otherBlock.QAFaces[0];
                    cA = otherBlock.WSFaces[3];
                    cB = otherBlock.WSFaces[0];
                    qaFace = BlockFace.EastQA;
                    edFace = BlockFace.EastED;
                    middleFace = BlockFace.EastMiddle;
                    rfFace = BlockFace.EastRF;
                    wsFace = BlockFace.EastWS;

                    if (Blocks[x, z].WallPortal != null)
                    {
                        var portal = FindPortal(x, z, PortalDirection.West);
                        var adjoiningRoom = portal.AdjoiningRoom;
                        if (Flipped && AlternateBaseRoom != null)
                        {
                            if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                adjoiningRoom = adjoiningRoom.AlternateRoom;
                        }

                        int facingZ = z + (int)(Position.Z - adjoiningRoom.Position.Z);
                        int qAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].QAFaces[2];
                        int qBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].QAFaces[1];
                        qA = (int)Position.Y + Blocks[1, z].QAFaces[3];
                        qB = (int)Position.Y + Blocks[1, z].QAFaces[0];
                        qA = Math.Max(qA, qAportal) - (int)Position.Y;
                        qB = Math.Max(qB, qBportal) - (int)Position.Y;

                        int wAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].WSFaces[2];
                        int wBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[adjoiningRoom.NumXSectors - 2, facingZ].WSFaces[1];
                        wA = (int)Position.Y + Blocks[1, z].WSFaces[1];
                        wB = (int)Position.Y + Blocks[1, z].WSFaces[2];
                        wA = Math.Min(wA, wAportal) - (int)Position.Y;
                        wB = Math.Min(wB, wBportal) - (int)Position.Y;

                        Blocks[x, z].QAFaces[2] = (short)qA;
                        Blocks[x, z].QAFaces[1] = (short)qB;
                        Blocks[x, z].WSFaces[2] = (short)wA;
                        Blocks[x, z].WSFaces[1] = (short)wB;
                    }

                    if (Blocks[x, z].Type == BlockType.BorderWall)
                    {
                        if (Blocks[1, z].WSFaces[3] < Blocks[x, z].QAFaces[2])
                        {
                            qA = Blocks[1, z].WSFaces[3];
                            Blocks[x, z].QAFaces[2] = (short)qA;
                        }

                        if (Blocks[1, z].WSFaces[0] < Blocks[x, z].QAFaces[1])
                        {
                            qB = Blocks[1, z].WSFaces[0];
                            Blocks[x, z].QAFaces[1] = (short)qB;
                        }
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NE)
                    {
                        qA = Blocks[x, z].QAFaces[1];
                        qB = Blocks[x, z].QAFaces[1];
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SE)
                    {
                        qA = Blocks[x, z].QAFaces[2];
                        qB = Blocks[x, z].QAFaces[2];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.NW)
                    {
                        fA = otherBlock.QAFaces[0];
                        fB = otherBlock.QAFaces[0];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.SW)
                    {
                        fA = otherBlock.QAFaces[3];
                        fB = otherBlock.QAFaces[3];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.NE)
                    {
                        wA = Blocks[x, z].WSFaces[1];
                        wB = Blocks[x, z].WSFaces[1];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.SE)
                    {
                        wA = Blocks[x, z].WSFaces[2];
                        wB = Blocks[x, z].WSFaces[2];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.NW)
                    {
                        cA = otherBlock.WSFaces[0];
                        cB = otherBlock.WSFaces[0];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.SW)
                    {
                        cA = otherBlock.WSFaces[3];
                        cB = otherBlock.WSFaces[3];
                    }

                    break;

                case FaceDirection.DiagonalFloor:
                    switch (Blocks[x, z].FloorDiagonalSplit)
                    {
                        case DiagonalSplit.NW:
                            xA = x + 1;
                            xB = x;
                            zA = z + 1;
                            zB = z;
                            qA = Blocks[x, z].QAFaces[1];
                            qB = Blocks[x, z].QAFaces[3];
                            eA = Blocks[x, z].EDFaces[1];
                            eB = Blocks[x, z].EDFaces[3];
                            rA = Blocks[x, z].RFFaces[1];
                            rB = Blocks[x, z].RFFaces[3];
                            wA = Blocks[x, z].WSFaces[1];
                            wB = Blocks[x, z].WSFaces[3];
                            fA = Blocks[x, z].QAFaces[0];
                            fB = Blocks[x, z].QAFaces[0];
                            cA = Blocks[x, z].WSFaces[0];
                            cB = Blocks[x, z].WSFaces[0];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                        case DiagonalSplit.NE:
                            xA = x + 1;
                            xB = x;
                            zA = z;
                            zB = z + 1;
                            qA = Blocks[x, z].QAFaces[2];
                            qB = Blocks[x, z].QAFaces[0];
                            eA = Blocks[x, z].EDFaces[2];
                            eB = Blocks[x, z].EDFaces[0];
                            rA = Blocks[x, z].RFFaces[2];
                            rB = Blocks[x, z].RFFaces[0];
                            wA = Blocks[x, z].WSFaces[2];
                            wB = Blocks[x, z].WSFaces[0];
                            fA = Blocks[x, z].QAFaces[1];
                            fB = Blocks[x, z].QAFaces[1];
                            cA = Blocks[x, z].WSFaces[1];
                            cB = Blocks[x, z].WSFaces[1];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                        case DiagonalSplit.SE:
                            xA = x;
                            xB = x + 1;
                            zA = z;
                            zB = z + 1;
                            qA = Blocks[x, z].QAFaces[3];
                            qB = Blocks[x, z].QAFaces[1];
                            eA = Blocks[x, z].EDFaces[3];
                            eB = Blocks[x, z].EDFaces[1];
                            rA = Blocks[x, z].RFFaces[3];
                            rB = Blocks[x, z].RFFaces[1];
                            wA = Blocks[x, z].WSFaces[3];
                            wB = Blocks[x, z].WSFaces[1];
                            fA = Blocks[x, z].QAFaces[2];
                            fB = Blocks[x, z].QAFaces[2];
                            cA = Blocks[x, z].WSFaces[2];
                            cB = Blocks[x, z].WSFaces[2];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                        default:
                            xA = x;
                            xB = x + 1;
                            zA = z + 1;
                            zB = z;
                            qA = Blocks[x, z].QAFaces[0];
                            qB = Blocks[x, z].QAFaces[2];
                            eA = Blocks[x, z].EDFaces[0];
                            eB = Blocks[x, z].EDFaces[2];
                            rA = Blocks[x, z].RFFaces[0];
                            rB = Blocks[x, z].RFFaces[2];
                            wA = Blocks[x, z].WSFaces[0];
                            wB = Blocks[x, z].WSFaces[2];
                            fA = Blocks[x, z].QAFaces[3];
                            fB = Blocks[x, z].QAFaces[3];
                            cA = Blocks[x, z].WSFaces[3];
                            cB = Blocks[x, z].WSFaces[3];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                    }

                    break;

                case FaceDirection.DiagonalCeiling:
                    switch (Blocks[x, z].CeilingDiagonalSplit)
                    {
                        case DiagonalSplit.NW:
                            xA = x + 1;
                            xB = x;
                            zA = z + 1;
                            zB = z;
                            qA = Blocks[x, z].QAFaces[1];
                            qB = Blocks[x, z].QAFaces[3];
                            eA = Blocks[x, z].EDFaces[1];
                            eB = Blocks[x, z].EDFaces[3];
                            rA = Blocks[x, z].RFFaces[1];
                            rB = Blocks[x, z].RFFaces[3];
                            wA = Blocks[x, z].WSFaces[1];
                            wB = Blocks[x, z].WSFaces[3];
                            fA = Blocks[x, z].QAFaces[0];
                            fB = Blocks[x, z].QAFaces[0];
                            cA = Blocks[x, z].WSFaces[0];
                            cB = Blocks[x, z].WSFaces[0];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                        case DiagonalSplit.NE:
                            xA = x + 1;
                            xB = x;
                            zA = z;
                            zB = z + 1;
                            qA = Blocks[x, z].QAFaces[2];
                            qB = Blocks[x, z].QAFaces[0];
                            eA = Blocks[x, z].EDFaces[2];
                            eB = Blocks[x, z].EDFaces[0];
                            rA = Blocks[x, z].RFFaces[2];
                            rB = Blocks[x, z].RFFaces[0];
                            wA = Blocks[x, z].WSFaces[2];
                            wB = Blocks[x, z].WSFaces[0];
                            fA = Blocks[x, z].QAFaces[1];
                            fB = Blocks[x, z].QAFaces[1];
                            cA = Blocks[x, z].WSFaces[1];
                            cB = Blocks[x, z].WSFaces[1];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                        case DiagonalSplit.SE:
                            xA = x;
                            xB = x + 1;
                            zA = z;
                            zB = z + 1;
                            qA = Blocks[x, z].QAFaces[3];
                            qB = Blocks[x, z].QAFaces[1];
                            eA = Blocks[x, z].EDFaces[3];
                            eB = Blocks[x, z].EDFaces[1];
                            rA = Blocks[x, z].RFFaces[3];
                            rB = Blocks[x, z].RFFaces[1];
                            wA = Blocks[x, z].WSFaces[3];
                            wB = Blocks[x, z].WSFaces[1];
                            fA = Blocks[x, z].QAFaces[2];
                            fB = Blocks[x, z].QAFaces[2];
                            cA = Blocks[x, z].WSFaces[2];
                            cB = Blocks[x, z].WSFaces[2];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                        default:
                            xA = x;
                            xB = x + 1;
                            zA = z + 1;
                            zB = z;
                            qA = Blocks[x, z].QAFaces[0];
                            qB = Blocks[x, z].QAFaces[2];
                            eA = Blocks[x, z].EDFaces[0];
                            eB = Blocks[x, z].EDFaces[2];
                            rA = Blocks[x, z].RFFaces[0];
                            rB = Blocks[x, z].RFFaces[2];
                            wA = Blocks[x, z].WSFaces[0];
                            wB = Blocks[x, z].WSFaces[2];
                            fA = Blocks[x, z].QAFaces[3];
                            fB = Blocks[x, z].QAFaces[3];
                            cA = Blocks[x, z].WSFaces[3];
                            cB = Blocks[x, z].WSFaces[3];
                            qaFace = BlockFace.DiagonalQA;
                            edFace = BlockFace.DiagonalED;
                            middleFace = BlockFace.DiagonalMiddle;
                            rfFace = BlockFace.DiagonalRF;
                            wsFace = BlockFace.DiagonalWS;
                            break;
                    }

                    break;

                default:
                    xA = x;
                    xB = x;
                    zA = z + 1;
                    zB = z;
                    otherBlock = Blocks[x - 1, z];
                    qA = Blocks[x, z].QAFaces[0];
                    qB = Blocks[x, z].QAFaces[3];
                    eA = Blocks[x, z].EDFaces[0];
                    eB = Blocks[x, z].EDFaces[3];
                    rA = Blocks[x, z].RFFaces[0];
                    rB = Blocks[x, z].RFFaces[3];
                    wA = Blocks[x, z].WSFaces[0];
                    wB = Blocks[x, z].WSFaces[3];
                    fA = otherBlock.QAFaces[1];
                    fB = otherBlock.QAFaces[2];
                    cA = otherBlock.WSFaces[1];
                    cB = otherBlock.WSFaces[2];
                    qaFace = BlockFace.WestQA;
                    edFace = BlockFace.WestED;
                    middleFace = BlockFace.WestMiddle;
                    rfFace = BlockFace.WestRF;
                    wsFace = BlockFace.WestWS;

                    if (Blocks[x, z].WallPortal != null)
                    {
                        var portal = FindPortal(x, z, PortalDirection.East);
                        var adjoiningRoom = portal.AdjoiningRoom;

                        if (Flipped && AlternateBaseRoom != null)
                        {
                            if (adjoiningRoom.Flipped && adjoiningRoom.AlternateRoom != null)
                                adjoiningRoom = adjoiningRoom.AlternateRoom;
                        }

                        int facingZ = z + (int)(Position.Z - adjoiningRoom.Position.Z);
                        int qAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[1, facingZ].QAFaces[0];
                        int qBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[1, facingZ].QAFaces[3];
                        qA = (int)Position.Y + Blocks[NumXSectors - 2, z].QAFaces[1];
                        qB = (int)Position.Y + Blocks[NumXSectors - 2, z].QAFaces[2];
                        qA = Math.Max(qA, qAportal) - (int)Position.Y;
                        qB = Math.Max(qB, qBportal) - (int)Position.Y;

                        int wAportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[1, facingZ].WSFaces[0];
                        int wBportal = (int)adjoiningRoom.Position.Y + adjoiningRoom.Blocks[1, facingZ].WSFaces[3];
                        wA = (int)Position.Y + Blocks[NumXSectors - 2, z].WSFaces[1];
                        wB = (int)Position.Y + Blocks[NumXSectors - 2, z].WSFaces[2];
                        wA = Math.Min(wA, wAportal) - (int)Position.Y;
                        wB = Math.Min(wB, wBportal) - (int)Position.Y;

                        Blocks[x, z].QAFaces[3] = (short)qA;
                        Blocks[x, z].QAFaces[0] = (short)qB;
                        Blocks[x, z].WSFaces[3] = (short)wA;
                        Blocks[x, z].WSFaces[0] = (short)wB;
                    }

                    if (Blocks[x, z].Type == BlockType.BorderWall)
                    {
                        if (Blocks[NumXSectors - 2, z].WSFaces[1] < Blocks[x, z].QAFaces[0])
                        {
                            qA = Blocks[NumXSectors - 2, z].WSFaces[1];
                            Blocks[x, z].QAFaces[0] = (short)qA;
                        }

                        if (Blocks[NumXSectors - 2, z].WSFaces[2] < Blocks[x, z].QAFaces[3])
                        {
                            qB = Blocks[NumXSectors - 2, z].WSFaces[2];
                            Blocks[x, z].QAFaces[3] = (short)qB;
                        }
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.NW)
                    {
                        qA = Blocks[x, z].QAFaces[0];
                        qB = Blocks[x, z].QAFaces[0];
                    }

                    if (Blocks[x, z].FloorDiagonalSplit == DiagonalSplit.SW)
                    {
                        qA = Blocks[x, z].QAFaces[3];
                        qB = Blocks[x, z].QAFaces[3];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.NE)
                    {
                        fA = otherBlock.QAFaces[1];
                        fB = otherBlock.QAFaces[1];
                    }

                    if (otherBlock.FloorDiagonalSplit == DiagonalSplit.SE)
                    {
                        fA = otherBlock.QAFaces[2];
                        fB = otherBlock.QAFaces[2];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.NW)
                    {
                        wA = Blocks[x, z].WSFaces[0];
                        wB = Blocks[x, z].WSFaces[0];
                    }

                    if (Blocks[x, z].CeilingDiagonalSplit == DiagonalSplit.SW)
                    {
                        wA = Blocks[x, z].WSFaces[3];
                        wB = Blocks[x, z].WSFaces[3];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.NE)
                    {
                        cA = otherBlock.WSFaces[1];
                        cB = otherBlock.WSFaces[1];
                    }

                    if (otherBlock.CeilingDiagonalSplit == DiagonalSplit.SE)
                    {
                        cA = otherBlock.WSFaces[2];
                        cB = otherBlock.WSFaces[2];
                    }

                    break;
            }

            bool subdivide = false;

            if (qA >= fA && qB >= fB && !(qA == fA && qB == fB) && floor)
            {
                // verifico eventuali suddivisione
                yA = fA;
                yB = fB;

                if (eA >= yA && eB >= yB && qA >= eA && qB >= eB && !(eA == yA && eB == yB))
                {
                    subdivide = true;
                    yA = eA;
                    yB = eB;
                }

                // Poligoni QA e ED
                face = Blocks[x, z].GetFaceTexture(qaFace);

                // QA
                if (qA > yA && qB > yB)
                    AddRectangle(x, z, qaFace,
                        new Vector3(xA * 1024.0f, qA * 256.0f, zA * 1024.0f),
                        new Vector3(xB * 1024.0f, qB * 256.0f, zB * 1024.0f),
                        new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                        new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                        face, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
                else if (qA == yA && qB > yB)
                    AddTriangle(x, z, qaFace,
                        new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                        new Vector3(xB * 1024.0f, qB * 256.0f, zB * 1024.0f),
                        new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                        face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), false);
                else if (qA > yA && qB == yB)
                    AddTriangle(x, z, qaFace,
                        new Vector3(xA * 1024.0f, qA * 256.0f, zA * 1024.0f),
                        new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                        new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                        face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), true);

                // ED
                if (subdivide)
                {
                    yA = fA;
                    yB = fB;

                    face = Blocks[x, z].GetFaceTexture(edFace);

                    if (eA > yA && eB > yB)
                        AddRectangle(x, z, edFace,
                            new Vector3(xA * 1024.0f, eA * 256.0f, zA * 1024.0f),
                            new Vector3(xB * 1024.0f, eB * 256.0f, zB * 1024.0f),
                            new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                            new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                            face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f));
                    else if (eA > yA && eB == yB)
                        AddTriangle(x, z, edFace,
                            new Vector3(xA * 1024.0f, eA * 256.0f, zA * 1024.0f),
                            new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                            new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                            face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), true);
                    else if (eA == yA && eB > yB)
                        AddTriangle(x, z, edFace,
                            new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                            new Vector3(xB * 1024.0f, eB * 256.0f, zB * 1024.0f),
                            new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                            face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f),  false);
                }
            }

            subdivide = false;

            if (cA >= wA && cB >= wB && !(wA == cA && wB == cB) && ceiling)
            {
                // verifico eventuali suddivisione
                yA = cA;
                yB = cB;

                if (rA <= yA && rB <= yB && wA <= rA && wB <= rB && !(rA == yA && rB == yB))
                {
                    subdivide = true;
                    yA = rA;
                    yB = rB;
                }

                // Poligoni WS e RF
                if (ceiling)
                {
                    face = Blocks[x, z].GetFaceTexture(wsFace);

                    // WS
                    if (wA < yA && wB < yB)
                        AddRectangle(x, z, wsFace,
                            new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                            new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                            new Vector3(xB * 1024.0f, wB * 256.0f, zB * 1024.0f),
                            new Vector3(xA * 1024.0f, wA * 256.0f, zA * 1024.0f),
                            face, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));
                    else if (wA < yA && wB == yB)
                        AddTriangle(x, z, wsFace,
                            new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                            new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                            new Vector3(xA * 1024.0f, wA * 256.0f, zA * 1024.0f),
                            face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), true);
                    else if (wA == yA && wB < yB)
                        AddTriangle(x, z, wsFace,
                            new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                            new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                            new Vector3(xB * 1024.0f, wB * 256.0f, zB * 1024.0f),
                            face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), false);

                    // RF
                    if (subdivide)
                    {
                        yA = cA;
                        yB = cB;

                        face = Blocks[x, z].GetFaceTexture(rfFace);

                        if (rA < yA && rB < yB)
                            AddRectangle(x, z, rfFace,                                
                                new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                                new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                                new Vector3(xB * 1024.0f, rB * 256.0f, zB * 1024.0f),
                                new Vector3(xA * 1024.0f, rA * 256.0f, zA * 1024.0f),
                                face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f));
                        else if (rA < yA && rB == yB)
                            AddTriangle(x, z, rfFace,
                                new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                                new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                                new Vector3(xA * 1024.0f, rA * 256.0f, zA * 1024.0f),
                                face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), true);
                        else if (rA == yA && rB < yB)
                            AddTriangle(x, z, rfFace,
                                new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                                new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                                new Vector3(xB * 1024.0f, rB * 256.0f, zB * 1024.0f),
                                face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), false);
                    }
                }
            }

            if (!middle)
                return;

            face = Blocks[x, z].GetFaceTexture(middleFace);

            yA = wA > cA ? cA : wA;
            yB = wB > cB ? cB : wB;
            int yD = qA < fA ? fA : qA;
            int yC = qB < fB ? fB : qB;
            // middle
            if (yA != yD && yB != yC)
                AddRectangle(x, z, middleFace,
                    new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                    new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                    new Vector3(xB * 1024.0f, yC * 256.0f, zB * 1024.0f),
                    new Vector3(xA * 1024.0f, yD * 256.0f, zA * 1024.0f),
                    face, new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f));

            else if (yA != yD && yB == yC)
                AddTriangle(x, z, middleFace,
                    new Vector3(xA * 1024.0f, yD * 256.0f, zA * 1024.0f),
                    new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                    new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f), 
                    face, new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), true);

            else if (yA == yD && yB != yC)
                AddTriangle(x, z, middleFace,
                    new Vector3(xB * 1024.0f, yC * 256.0f, zB * 1024.0f),
                    new Vector3(xA * 1024.0f, yA * 256.0f, zA * 1024.0f),
                    new Vector3(xB * 1024.0f, yB * 256.0f, zB * 1024.0f),
                    face, new Vector2(1.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), false);
        }

        private Portal FindPortal(int x, int z, PortalDirection type)
        {
            if (Blocks[x, z].WallPortal != null)
                return Blocks[x, z].WallPortal;
            if (Blocks[x, z].FloorPortal != null && type == PortalDirection.Floor)
                return Blocks[x, z].FloorPortal;
            if (Blocks[x, z].CeilingPortal != null && type == PortalDirection.Ceiling)
                return Blocks[x, z].CeilingPortal;

            return null;
        }

        private void AddRectangle(int x, int z, BlockFace face, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, TextureArea texture, Vector2 editorUV0, Vector2 editorUV1, Vector2 editorUV2, Vector2 editorUV3)
        {
            var sectorVertices = _sectorVertices[x, z];
            int sectorVerticesStart = sectorVertices.Count;

            sectorVertices.Add(new EditorVertex { Position = p1, UV = texture.TexCoord1, EditorUV = editorUV1 });
            sectorVertices.Add(new EditorVertex { Position = p2, UV = texture.TexCoord2, EditorUV = editorUV2 });
            sectorVertices.Add(new EditorVertex { Position = p0, UV = texture.TexCoord0, EditorUV = editorUV0 });
            sectorVertices.Add(new EditorVertex { Position = p3, UV = texture.TexCoord3, EditorUV = editorUV3 });
            sectorVertices.Add(new EditorVertex { Position = p0, UV = texture.TexCoord0, EditorUV = editorUV0 });
            sectorVertices.Add(new EditorVertex { Position = p2, UV = texture.TexCoord2, EditorUV = editorUV2 });

            _sectorFaceVertexVertexRange[x, z, (int)face] = new VertexRange { Start = sectorVerticesStart, Count = 6 };
        }

        private void AddTriangle(int x, int z, BlockFace face, Vector3 p0, Vector3 p1, Vector3 p2, TextureArea texture, Vector2 editorUV0, Vector2 editorUV1, Vector2 editorUV2, bool IsXEqualYDiagonal)
        {
            var sectorVertices = _sectorVertices[x, z];
            int sectorVerticesStart = sectorVertices.Count;

            Vector2 editorUvFactor = new Vector2(IsXEqualYDiagonal ? -1.0f : 1.0f, -1.0f);
            sectorVertices.Add(new EditorVertex { Position = p0, UV = texture.TexCoord0, EditorUV = editorUV0 * editorUvFactor });
            sectorVertices.Add(new EditorVertex { Position = p1, UV = texture.TexCoord1, EditorUV = editorUV1 * editorUvFactor });
            sectorVertices.Add(new EditorVertex { Position = p2, UV = texture.TexCoord2, EditorUV = editorUV2 * editorUvFactor });

            _sectorFaceVertexVertexRange[x, z, (int)face] = new VertexRange { Start = sectorVerticesStart, Count = 3 };
        }

        public struct IntersectionInfo
        {
            public DrawingPoint Pos;
            public BlockFace Face;
            public float Distance;
        };

        public IntersectionInfo? RayIntersectsGeometry(Ray ray)
        {
            IntersectionInfo result = new IntersectionInfo { Distance = float.NaN };
            for (int x = 0; x < NumXSectors; x++)
                for (int z = 0; z < NumZSectors; z++)
                    for (BlockFace face = 0; face < Block.FaceCount; face++)
                    {
                        // Check for intersection on the correct side
                        var sectorVertices = _sectorVertices[x, z];
                        VertexRange vertexRange = _sectorFaceVertexVertexRange[x, z, (int)face];
                        for (int i = 0; i < vertexRange.Count; i += 3)
                        {
                            var p0 = sectorVertices[vertexRange.Start + i].Position;
                            var p1 = sectorVertices[vertexRange.Start + i + 1].Position;
                            var p2 = sectorVertices[vertexRange.Start + i + 2].Position;

                            float distance;
                            if (ray.Intersects(ref p0, ref p1, ref p2, out distance))
                            {
                                var normal = Vector3.Cross(p1 - p0, p2 - p0);
                                if (Vector3.Dot(ray.Direction, normal) <= 0)
                                    if (!(distance > result.Distance))
                                        result = new IntersectionInfo() { Distance = distance, Face = face, Pos = new DrawingPoint(x, z) };
                            }
                        }
                    }

            if (float.IsNaN(result.Distance))
                return null;
            return result;
        }



        private bool RayTraceCheckFloorCeiling(int x, int y, int z, int xLight, int zLight)
        {
            int currentX = (x / 1024) - (x > xLight ? 1 : 0);
            int currentZ = (z / 1024) - (z > zLight ? 1 : 0);

            Block block = Blocks[currentX, currentZ];
            int floorMin = block.FloorMin;
            int ceilingMax = block.CeilingMax;

            return floorMin <= y / 256 && ceilingMax >= y / 256;
        }

        private bool RayTraceX(int x, int y, int z, int xLight, int yLight, int zLight)
        {
            int deltaX;
            int deltaY;
            int deltaZ;

            int minX;
            int maxX;

            yLight = -yLight;
            y = -y;

            int yPoint = y;
            int zPoint = z;

            if (x <= xLight)
            {
                deltaX = xLight - x;
                deltaY = yLight - y;
                deltaZ = zLight - z;

                minX = x;
                maxX = xLight;
            }
            else
            {
                deltaX = x - xLight;
                deltaY = y - yLight;
                deltaZ = z - zLight;

                minX = xLight;
                maxX = x;

                yPoint = yLight;
                zPoint = zLight;
            }

            // deltaY *= -1;

            if (deltaX == 0)
                return true;

            int fracX = (((minX >> 10) + 1) << 10) - minX;
            int currentX = ((minX >> 10) + 1) << 10;
            int currentZ = deltaZ * fracX / (deltaX + 1) + zPoint;
            int currentY = deltaY * fracX / (deltaX + 1) + yPoint;

            if (currentX > maxX)
                return true;

            do
            {
                int currentXblock = currentX / 1024;
                int currentZblock = currentZ / 1024;

                if (currentZblock < 0 || currentXblock >= NumXSectors || currentZblock >= NumZSectors)
                {
                    if (currentX == maxX)
                        return true;
                }
                else
                {
                    int currentYclick = currentY / -256;

                    if (currentXblock > 0)
                    {
                        Block currentBlock = Blocks[currentXblock - 1, currentZblock];

                        if (((currentBlock.QAFaces[0] + currentBlock.QAFaces[3]) / 2 > currentYclick) ||
                            ((currentBlock.WSFaces[0] + currentBlock.WSFaces[3]) / 2 < currentYclick) ||
                            currentBlock.Type == BlockType.Wall)
                        {
                            return false;
                        }
                    }

                    if (currentX == maxX)
                    {
                        return true;
                    }

                    if (currentXblock >= 0)
                    {
                        var currentBlock = Blocks[currentXblock - 1, currentZblock];
                        var nextBlock = Blocks[currentXblock, currentZblock];

                        if (((currentBlock.QAFaces[2] + currentBlock.QAFaces[1]) / 2 > currentYclick) ||
                            ((currentBlock.WSFaces[2] + currentBlock.WSFaces[1]) / 2 < currentYclick) ||
                            currentBlock.Type == BlockType.Wall ||
                            ((nextBlock.QAFaces[0] + nextBlock.QAFaces[3]) / 2 > currentYclick) ||
                            ((nextBlock.WSFaces[0] + nextBlock.WSFaces[3]) / 2 < currentYclick) ||
                            nextBlock.Type == BlockType.Wall)
                        {
                            return false;
                        }
                    }
                }

                currentX += 1024;
                currentZ += (deltaZ << 10) / (deltaX + 1);
                currentY += (deltaY << 10) / (deltaX + 1);
            }
            while (currentX <= maxX);

            return true;
        }

        private bool RayTraceZ(int x, int y, int z, int xLight, int yLight, int zLight)
        {
            int deltaX;
            int deltaY;
            int deltaZ;

            int minZ;
            int maxZ;

            yLight = -yLight;
            y = -y;

            int yPoint = y;
            int xPoint = x;

            if (z <= zLight)
            {
                deltaX = xLight - x;
                deltaY = yLight - y;
                deltaZ = zLight - z;

                minZ = z;
                maxZ = zLight;
            }
            else
            {
                deltaX = x - xLight;
                deltaY = y - yLight;
                deltaZ = z - zLight;

                minZ = zLight;
                maxZ = z;

                xPoint = xLight;
                yPoint = yLight;
            }

            //deltaY *= -1;

            if (deltaZ == 0)
                return true;

            int fracZ = (((minZ >> 10) + 1) << 10) - minZ;
            int currentZ = ((minZ >> 10) + 1) << 10;
            int currentX = deltaX * fracZ / (deltaZ + 1) + xPoint;
            int currentY = deltaY * fracZ / (deltaZ + 1) + yPoint;

            if (currentZ > maxZ)
                return true;

            do
            {
                int currentXblock = currentX / 1024;
                int currentZblock = currentZ / 1024;

                if (currentXblock < 0 || currentZblock >= NumZSectors || currentXblock >= NumXSectors)
                {
                    if (currentZ == maxZ)
                        return true;
                }
                else
                {
                    int currentYclick = currentY / -256;

                    if (currentZblock > 0)
                    {
                        var currentBlock = Blocks[currentXblock, currentZblock - 1];

                        if (((currentBlock.QAFaces[2] + currentBlock.QAFaces[3]) / 2 > currentYclick) ||
                            ((currentBlock.WSFaces[2] + currentBlock.WSFaces[3]) / 2 < currentYclick) ||
                            currentBlock.Type == BlockType.Wall)
                        {
                            return false;
                        }
                    }

                    if (currentZ == maxZ)
                    {
                        return true;
                    }

                    if (currentZblock >= 0)
                    {
                        var currentBlock = Blocks[currentXblock, currentZblock - 1];
                        var nextBlock = Blocks[currentXblock, currentZblock];

                        if (((currentBlock.QAFaces[0] + currentBlock.QAFaces[1]) / 2 > currentYclick) ||
                            ((currentBlock.WSFaces[0] + currentBlock.WSFaces[1]) / 2 < currentYclick) ||
                            currentBlock.Type == BlockType.Wall ||
                            ((nextBlock.QAFaces[2] + nextBlock.QAFaces[3]) / 2 > currentYclick) ||
                            ((nextBlock.WSFaces[2] + nextBlock.WSFaces[3]) / 2 < currentYclick) ||
                            nextBlock.Type == BlockType.Wall)
                        {
                            return false;
                        }
                    }
                }

                currentZ += 1024;
                currentX += (deltaX << 10) / (deltaZ + 1);
                currentY += (deltaY << 10) / (deltaZ + 1);
            }
            while (currentZ <= maxZ);

            return true;
        }

        public void CalculateLightingForThisRoom()
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            // Reset all lighting
            for (int i = 0; i < _allVertices.Count; ++i)
            {
                var vertex = _allVertices[i];
                vertex.FaceColor = AmbientLight;
                _allVertices[i] = vertex;
            }

            // Calculate lighting
            for (int x = 0; x < NumXSectors; x++)
                for (int z = 0; z < NumZSectors; z++)
                    for (BlockFace f = 0; f < Block.FaceCount; f++)
                        if (IsFaceDefined(x, z, f))
                            CalculateLighting(x, z, f);

            // Calculate average of shared vertices
            Dictionary<Vector3, List<int>> sharedVertices = new Dictionary<Vector3, List<int>>();
            for (int i = 0; i < _allVertices.Count; ++i)
            {
                Vector3 position = _allVertices[i].Position;
                List<int> list;
                if (!sharedVertices.TryGetValue(position, out list))
                    sharedVertices.Add(position, list = new List<int>());
                list.Add(i);
            }

            foreach (var pair in sharedVertices)
            {
                Vector4 faceColorSum = new Vector4(0);
                foreach (var vertexIndex in pair.Value)
                    faceColorSum += _allVertices[vertexIndex].FaceColor;
                faceColorSum /= pair.Value.Count;
                foreach (var vertexIndex in pair.Value)
                {
                    var vertex = _allVertices[vertexIndex];
                    vertex.FaceColor = faceColorSum;
                    _allVertices[vertexIndex] = vertex;
                }
            }

            watch.Stop();
        }

        private void CalculateLighting(int x, int z, BlockFace face)
        {
            // No Linq here because it's slow
            List<Light> lights = new List<Light>();
            foreach (var instance in _objects)
            {
                Light light = instance as Light;
                if (light != null)
                    lights.Add(light);
            }

            VertexRange range = GetFaceVertexRange(x, z, face);
            if (range.Count == 0)
                return;

            var normal = Vector3.Cross(
                _allVertices[range.Start + 1].Position - _allVertices[range.Start].Position,
                _allVertices[range.Start + 2].Position - _allVertices[range.Start].Position);
            normal.Normalize();

            for (int i = 0; i < range.Count; ++i)
            {
                var position = _allVertices[range.Start + i].Position;

                int r = (int)(AmbientLight.X * 128);
                int g = (int)(AmbientLight.Y * 128);
                int b = (int)(AmbientLight.Z * 128);

                foreach (var light in lights) // No Linq here because it's slow
                {
                    if ((!light.Enabled) || (!light.IsStaticallyUsed))
                        continue;

                    switch (light.Type)
                    {
                        case LightType.Light:
                        case LightType.Shadow:
                            if (Math.Abs(Vector3.Distance(position, light.Position)) + 64.0f <= light.Out * 1024.0f)
                            {
                                // Get the distance between light and vertex
                                float distance = Math.Abs((position - light.Position).Length());

                                // If distance is greater than light out radius, then skip this light
                                if (distance > light.Out * 1024.0f)
                                    continue;

                                // Calculate light diffuse value
                                int diffuse = (int)(light.Intensity * 8192);

                                // Calculate the length squared of the normal vector
                                float dotN = Vector3.Dot(normal, normal);

                                // Do raytracing
                                if (dotN <= 0 ||
                                    !RayTraceCheckFloorCeiling((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Z) ||
                                    !RayTraceX((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Y, (int)light.Position.Z) ||
                                    !RayTraceZ((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Y, (int)light.Position.Z))
                                {
                                    if (light.CastsShadows)
                                        continue;
                                }

                                // Calculate the attenuation
                                var attenuaton = (light.Out * 1024.0f - distance) / (light.Out * 1024.0f - light.In * 1024.0f);
                                if (attenuaton > 1.0f)
                                    attenuaton = 1.0f;
                                if (attenuaton <= 0.0f)
                                    continue;

                                // Calculate final light color
                                int finalIntensity = (int)(dotN * attenuaton * diffuse);

                                r += (int)(finalIntensity * light.Color.X / 64.0f);
                                g += (int)(finalIntensity * light.Color.Y / 64.0f);
                                b += (int)(finalIntensity * light.Color.Z / 64.0f);
                            }
                            break;
                        case LightType.Effect:
                            if (Math.Abs(Vector3.Distance(position, light.Position)) + 64.0f <= light.Out * 1024.0f)
                            {
                                int x1 = (int)(Math.Floor(light.Position.X / 1024.0f) * 1024);
                                int z1 = (int)(Math.Floor(light.Position.Z / 1024.0f) * 1024);
                                int x2 = (int)(Math.Ceiling(light.Position.X / 1024.0f) * 1024);
                                int z2 = (int)(Math.Ceiling(light.Position.Z / 1024.0f) * 1024);

                                // TODO: winroomedit was supporting effect lights placed on vertical faces and effects light was applied to owning face
                                // ReSharper disable CompareOfFloatsByEqualityOperator
                                if (((position.X == x1 && position.Z == z1) || (position.X == x1 && position.Z == z2) || (position.X == x2 && position.Z == z1) ||
                                     (position.X == x2 && position.Z == z2)) && position.Y <= light.Position.Y)
                                {
                                    int finalIntensity = (int)(light.Intensity * 8192 * 0.25f);

                                    r += (int)(finalIntensity * light.Color.X / 64.0f);
                                    g += (int)(finalIntensity * light.Color.Y / 64.0f);
                                    b += (int)(finalIntensity * light.Color.Z / 64.0f);
                                }
                                // ReSharper restore CompareOfFloatsByEqualityOperator
                            }
                            break;
                        case LightType.Sun:
                            {
                                // Do raytracing now for saving CPU later
                                if (!RayTraceCheckFloorCeiling((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Z) ||
                                    !RayTraceX((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Y, (int)light.Position.Z) ||
                                    !RayTraceZ((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Y, (int)light.Position.Z))
                                {
                                    if (light.CastsShadows)
                                        continue;
                                }

                                // Calculate the light direction
                                var lightDirection = Vector3.Zero;

                                lightDirection.X = (float)(Math.Cos(MathUtil.DegreesToRadians(light.RotationX)) * Math.Sin(MathUtil.DegreesToRadians(light.RotationY)));
                                lightDirection.Y = (float)(Math.Sin(MathUtil.DegreesToRadians(light.RotationX)));
                                lightDirection.Z = (float)(Math.Cos(MathUtil.DegreesToRadians(light.RotationX)) * Math.Cos(MathUtil.DegreesToRadians(light.RotationY)));

                                lightDirection.Normalize();

                                // calcolo la luce diffusa
                                float diffuse = -Vector3.Dot(lightDirection, normal);

                                if (diffuse <= 0)
                                    continue;

                                if (diffuse > 1)
                                    diffuse = 1.0f;


                                int finalIntensity = (int)(diffuse * light.Intensity * 8192);
                                if (finalIntensity < 0)
                                    continue;

                                r += (int)(finalIntensity * light.Color.X / 64.0f);
                                g += (int)(finalIntensity * light.Color.Y / 64.0f);
                                b += (int)(finalIntensity * light.Color.Z / 64.0f);
                            }
                            break;
                        case LightType.Spot:
                            if (Math.Abs(Vector3.Distance(position, light.Position)) + 64.0f <= light.Cutoff * 1024.0f)
                            {
                                // Calculate the ray from light to vertex
                                var lightVector = position - light.Position;
                                lightVector.Y = -lightVector.Y;
                                lightVector.Normalize();

                                // Get the distance between light and vertex
                                float distance = Math.Abs((position - light.Position).Length());

                                // If distance is greater than light length, then skip this light
                                if (distance > light.Cutoff * 1024.0f)
                                    continue;

                                // Calculate the light direction
                                var lightDirection = Vector3.Zero;

                                lightDirection.X = (float)(-Math.Cos(MathUtil.DegreesToRadians(light.RotationX)) * Math.Sin(MathUtil.DegreesToRadians(light.RotationY)));
                                lightDirection.Y = (float)(Math.Sin(MathUtil.DegreesToRadians(light.RotationX)));
                                lightDirection.Z = (float)(-Math.Cos(MathUtil.DegreesToRadians(light.RotationX)) * Math.Cos(MathUtil.DegreesToRadians(light.RotationY)));

                                lightDirection.Normalize();

                                // Calculate the cosines values for In, Out
                                double d = -Vector3.Dot(lightVector, lightDirection);
                                double cosI2 = Math.Cos(MathUtil.DegreesToRadians(light.In));
                                double cosO2 = Math.Cos(MathUtil.DegreesToRadians(light.Out));

                                if (d < cosO2)
                                    continue;

                                if (!RayTraceCheckFloorCeiling((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Z) ||
                                    !RayTraceX((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Y, (int)light.Position.Z) ||
                                    !RayTraceZ((int)position.X, (int)position.Y, (int)position.Z, (int)light.Position.X, (int)light.Position.Y, (int)light.Position.Z))
                                {
                                    if (light.CastsShadows)
                                        continue;
                                }

                                // Calculate light diffuse value
                                float factor = (float)(1.0f - (d - cosI2) / (cosO2 - cosI2));
                                if (factor > 1.0f)
                                    factor = 1.0f;
                                if (factor <= 0.0f)
                                    continue;

                                float attenuation = 1.0f;
                                if (distance >= light.Len * 1024.0f)
                                    attenuation = 1.0f - (distance - light.Len * 1024.0f) / (light.Cutoff * 1024.0f - light.Len * 1024.0f);

                                if (attenuation > 1.0f)
                                    attenuation = 1.0f;
                                if (attenuation < 0.0f)
                                    continue;

                                Vector3 normal2 = normal;
                                normal2.Y = -normal2.Y;

                                float dot1 = Vector3.Dot(lightDirection, normal2);
                                if (dot1 < 0.0f)
                                    continue;
                                if (dot1 > 1.0f)
                                    dot1 = 1.0f;

                                int finalIntensity = (int)(attenuation * dot1 * factor * light.Intensity * 8192);

                                r += (int)(finalIntensity * light.Color.X / 64.0f);
                                g += (int)(finalIntensity * light.Color.Y / 64.0f);
                                b += (int)(finalIntensity * light.Color.Z / 64.0f);
                            }
                            break;
                    }
                }

                if (r < 0)
                    r = 0;
                if (g < 0)
                    g = 0;
                if (b < 0)
                    b = 0;

                // Apply color
                EditorVertex vertex = _allVertices[range.Start + i];

                vertex.FaceColor.X = r * (1.0f / 128.0f);
                vertex.FaceColor.Y = g * (1.0f / 128.0f);
                vertex.FaceColor.Z = b * (1.0f / 128.0f);
                vertex.FaceColor.W = 255.0f;

                _allVertices[range.Start + i] = vertex;
            }
        }

        public List<EditorVertex> GetRoomVertices()
        {
            return _allVertices;
        }

        public void UpdateBuffers()
        {
            // HACK
            if (_allVertices.Count == 0)
                return;

            // HACK
            if ((_vertexBuffer == null) || (_vertexBuffer.ElementCount < _allVertices.Count))
            {
                _vertexBuffer?.Dispose();
                _vertexBuffer = Buffer.New(DeviceManager.DefaultDeviceManager.Device, _allVertices.ToArray(), BufferFlags.VertexBuffer);
            }

            _vertexBuffer.SetData<EditorVertex>(_allVertices.ToArray());
        }

        public Buffer<EditorVertex> VertexBuffer => _vertexBuffer;

        public Matrix Transform => Matrix.Translation(new Vector3(Position.X * 1024.0f, Position.Y * 256.0f, Position.Z * 1024.0f));

        public int GetHighestCorner()
        {
            int max = int.MinValue;

            for (int x = 1; x < NumXSectors - 1; x++)
                for (int z = 1; z < NumZSectors - 1; z++)
                    if (Blocks[x, z].IsFloor)
                        max = Math.Max(max, Blocks[x, z].CeilingMax);

            return max;
        }

        public int GetLowestCorner()
        {
            int min = int.MaxValue;

            for (int x = 1; x < NumXSectors - 1; x++)
                for (int z = 1; z < NumZSectors - 1; z++)
                    if (Blocks[x, z].IsFloor)
                        min = Math.Min(min, Blocks[x, z].FloorMin);

            return min;
        }
        
        public Vector3 WorldPos => new Vector3(Position.X * 1024.0f, Position.Y * 256.0f, Position.Z * 1024.0f);

        public Vector3 GetLocalCenter()
        {
            float ceilingHeight = GetHighestCorner();
            float floorHeight = GetLowestCorner();
            float posX = NumXSectors * (0.5f * 1024.0f);
            float posY = (floorHeight + ceilingHeight) * (0.5f * 256.0f);
            float posZ = NumZSectors * (0.5f * 1024.0f);
            return new Vector3(posX, posY, posZ);
        }

        private Block GetBlockIfFloor(int x, int z)
        {
            Block block = Blocks.TryGet(x, z);
            if ((block != null) && block.IsAnyWall)
                return null;
            return block;
        }

        ///<param name="x">The X-coordinate. The point at room.Position it at (0, 0)</param>
        ///<param name="z">The Z-coordinate. The point at room.Position it at (0, 0)</param>
        public VerticalSpace? GetHeightAtPoint(int x, int z, Func<float?, float?, float?, float?, float> combineFloor, Func<float?, float?, float?, float?, float> combineCeiling)
        {
            Block blockXnZn = GetBlockIfFloor(x - 1, z - 1);
            Block blockXnZp = GetBlockIfFloor(x - 1, z);
            Block blockXpZn = GetBlockIfFloor(x, z - 1);
            Block blockXpZp = GetBlockIfFloor(x, z);
            if ((blockXnZn == null) && (blockXnZp == null) && (blockXpZn == null) && (blockXpZp == null))
                return null;

            return new VerticalSpace
            {
                FloorY = combineFloor(
                    blockXnZn?.QAFaces[Block.FaceXpZp],
                    blockXnZp?.QAFaces[Block.FaceXpZn],
                    blockXpZn?.QAFaces[Block.FaceXnZp],
                    blockXpZp?.QAFaces[Block.FaceXnZn]),
                CeilingY = combineCeiling(
                    blockXnZn?.WSFaces[Block.FaceXpZp],
                    blockXnZp?.WSFaces[Block.FaceXpZn],
                    blockXpZn?.WSFaces[Block.FaceXnZp],
                    blockXpZp?.WSFaces[Block.FaceXnZn])
            };
        }

        public VerticalSpace? GetHeightInArea(Rectangle area, Func<float?, float?, float?, float?, float> combineFloor, Func<float?, float?, float?, float?, float> combineCeiling)
        {
            VerticalSpace? result = null;
            for (int x = area.X; x <= area.Right; x++)
                for (int z = area.Y; z <= area.Bottom; z++)
                {
                    VerticalSpace? verticalSpace = GetHeightAtPoint(x, z, combineFloor, combineCeiling);
                    if (verticalSpace == null)
                        continue;
                    result = new VerticalSpace
                    {
                        FloorY = combineFloor(verticalSpace?.FloorY, result?.FloorY, null, null),
                        CeilingY = combineCeiling(verticalSpace?.CeilingY, result?.CeilingY, null, null)
                    };
                }
            return result;
        }


        private static float Average(float? Height0, float? Height1, float? Height2, float? Height3)
        {
            int Count = (Height0.HasValue ? 1 : 0) + (Height1.HasValue ? 1 : 0) + (Height2.HasValue ? 1 : 0) + (Height3.HasValue ? 1 : 0);
            float Sum = (Height0 ?? 0) + (Height1 ?? 0) + (Height2 ?? 0) + (Height3 ?? 0);
            return Sum / Count;
        }

        private static float Max(float? Height0, float? Height1, float? Height2, float? Height3)
        {
            return Math.Max(Math.Max(Height0 ?? float.NegativeInfinity, Height1 ?? float.NegativeInfinity),
                Math.Max(Height2 ?? float.NegativeInfinity, Height3 ?? float.NegativeInfinity));
        }

        private static float Min(float? Height0, float? Height1, float? Height2, float? Height3)
        {
            return Math.Min(Math.Min(Height0 ?? float.PositiveInfinity, Height1 ?? float.PositiveInfinity),
                Math.Min(Height2 ?? float.PositiveInfinity, Height3 ?? float.PositiveInfinity));
        }

        public VerticalSpace? GetHeightAtPointAverage(int x, int z)
        {
            return GetHeightAtPoint(x, z, Average, Average);
        }

        public VerticalSpace? GetHeightAtPointMinSpace(int x, int z)
        {
            return GetHeightAtPoint(x, z, Max, Min);
        }

        public VerticalSpace? GetHeightAtPointMaxSpace(int x, int z)
        {
            return GetHeightAtPoint(x, z, Min, Max);
        }

        public VerticalSpace? GetHeightInAreaAverage(Rectangle area)
        {
            return GetHeightInArea(area, Average, Average);
        }

        public VerticalSpace? GetHeightInAreaMinSpace(Rectangle area)
        {
            return GetHeightInArea(area, Max, Min);
        }

        public VerticalSpace? GetHeightInAreaMaxSpace(Rectangle area)
        {
            return GetHeightInArea(area, Min, Max);
        }

        public byte NumXSectors
        {
            get { return (byte)(Blocks.GetLength(0)); }
        }

        public byte NumZSectors
        {
            get { return (byte)(Blocks.GetLength(1)); }
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>Transforms the coordinates of QAFaces in such a way that the lowest one falls on Y = 0</summary>
        public void NormalizeRoomY()
        {
            // Determine lowest QAFace
            short lowest = short.MaxValue;
            for (int z = 0; z < NumZSectors; z++)
                for (int x = 0; x < NumXSectors; x++)
                {
                    var b = Blocks[x, z];
                    if (b.IsFloor)
                        for (int i = 0; i < 4; i++)
                            lowest = Math.Min(lowest, b.QAFaces[i]);
                }

            // Move room to new position
            Position += new Vector3(0, lowest, 0);

            // Transform room content in such a way, their world position is identical to before even though the room position changed
            for (int z = 0; z < NumZSectors; z++)
                for (int x = 0; x < NumXSectors; x++)
                {
                    var b = Blocks[x, z];
                    for (int i = 0; i < 4; i++)
                    {
                        b.QAFaces[i] -= lowest;
                        b.EDFaces[i] -= lowest;
                        b.WSFaces[i] -= lowest;
                        b.RFFaces[i] -= lowest;
                    }
                }

            foreach (var instance in _objects)
                instance.Position -= new Vector3(0, lowest * 256, 0);
        }


        public void AddObject(Level level, ObjectInstance instance)
        {
            if (instance is PositionBasedObjectInstance)
                _objects.Add((PositionBasedObjectInstance)instance);
            try
            {
                instance.AddToRoom(level, this);
            }
            catch
            { // If we fail, remove the object from the list
                if (instance is PositionBasedObjectInstance)
                    _objects.Remove((PositionBasedObjectInstance)instance);
                throw;
            }
        }

        public void RemoveObject(Level level, ObjectInstance instance)
        {
            instance.RemoveFromRoom(level, this);
            if (instance is PositionBasedObjectInstance)
                _objects.Remove((PositionBasedObjectInstance)instance);
        }

        public Portal AddBidirectionalPortalsToLevel(Level level, Portal portal)
        {
            Rectangle oppositeArea = Portal.GetOppositePortalArea(portal.Direction, portal.Area).Offset(SectorPos).OffsetNeg(portal.AdjoiningRoom.SectorPos);
            Portal oppositePortal = new Portal(oppositeArea, Portal.GetOppositeDirection(portal.Direction), this);

            AddObject(level, portal);
            try
            {
                portal.AdjoiningRoom.AddObject(level, oppositePortal);
            }
            catch
            {
                RemoveObject(level, portal);
                throw;
            }

            UpdateCompletely();
            portal.AdjoiningRoom.UpdateCompletely();

            return oppositePortal;
        }

        public bool IsFloorSolid(DrawingPoint pos)
        {
            Block block = GetBlock(pos);
            if ((block.FloorPortal == null) || block.IsAnyWall || block.ForceFloorSolid)
                return true;

            Room adjoiningRoom = block.FloorPortal.AdjoiningRoom;
            Block adjoiningBlock = adjoiningRoom.GetBlock(pos.Offset(SectorPos).OffsetNeg(adjoiningRoom.SectorPos));
            if (adjoiningBlock.IsAnyWall)
                return true;

            int identicalEdgeCount = 0;
            for (int i = 0; i < 4; ++i)
                if ((Position.Y + block.QAFaces[i]) == (adjoiningRoom.Position.Y + adjoiningBlock.WSFaces[i]))
                    ++identicalEdgeCount;
            return identicalEdgeCount < 4;
        }

        public bool IsCeilingSolid(DrawingPoint pos)
        {
            Block block = GetBlock(pos);
            if ((block.CeilingPortal == null) || block.IsAnyWall)
                return true;

            Room adjoiningRoom = block.CeilingPortal.AdjoiningRoom;
            Block adjoiningBlock = adjoiningRoom.GetBlock(pos.Offset(SectorPos).OffsetNeg(adjoiningRoom.SectorPos));
            if (adjoiningBlock.IsAnyWall || adjoiningBlock.ForceFloorSolid)
                return true;

            int identicalEdgeCount = 0;
            for (int i = 0; i < 4; ++i)
                if ((Position.Y + block.WSFaces[i]) == (adjoiningRoom.Position.Y + adjoiningBlock.QAFaces[i]))
                    ++identicalEdgeCount;
            return identicalEdgeCount < 4;
        }

        public void SmartBuildGeometry(Rectangle area)
        {
            area = area.Inflate(1); // Add margin
            BuildGeometry(area);
            CalculateLightingForThisRoom();
            UpdateBuffers();

            // Update adjoining rooms
            HashSet<Room> roomsProcessed = new HashSet<Room>();
            List<Portal> listOfPortals = Portals.ToList();
            foreach (var portal in listOfPortals)
            {
                if (!portal.Area.Intersects(area))
                    continue; // This portal is irrelavant since no changes happend in its area

                Rectangle portalArea = portal.Area.Intersect(area);
                Rectangle otherRoomPortalArea = Portal.GetOppositePortalArea(portal.Direction, portalArea)
                    .Offset(SectorPos).OffsetNeg(portal.AdjoiningRoom.SectorPos);
                portal.AdjoiningRoom.BuildGeometry(otherRoomPortalArea);
                roomsProcessed.Add(portal.AdjoiningRoom);
            }

            // Update lighting in room that were updated geometrically
            foreach (var adjoiningRoom in roomsProcessed)
            {
                adjoiningRoom.CalculateLightingForThisRoom();
                adjoiningRoom.UpdateBuffers();
            }
        }
    }

    public struct VerticalSpace
    {
        public float FloorY;
        public float CeilingY;

        public static VerticalSpace operator +(VerticalSpace old, float offset)
        {
            return new VerticalSpace { FloorY = old.FloorY + offset, CeilingY = old.CeilingY + offset };
        }
    };
}