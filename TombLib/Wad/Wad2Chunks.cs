﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TombLib.IO;

namespace TombLib.Wad
{
    class Wad2Chunks
    {
        public static readonly byte[] MagicNumber = new byte[] { 0x57, 0x41, 0x44, 0x32 };
        public static readonly ChunkId GameVersion = ChunkId.FromString("W2GameVersion");
        public static readonly ChunkId Textures = ChunkId.FromString("W2Textures");
        /**/public static readonly ChunkId Texture = ChunkId.FromString("W2Txt");
        /****/public static readonly ChunkId TextureData = ChunkId.FromString("W2TxtData");
        public static readonly ChunkId Sprites = ChunkId.FromString("W2Sprites");
        /**/public static readonly ChunkId Sprite = ChunkId.FromString("W2Spr");
        /****/public static readonly ChunkId SpriteSides = ChunkId.FromString("W2SprSides");
        /****/public static readonly ChunkId SpriteData = ChunkId.FromString("W2SprData");
        public static readonly ChunkId Meshes = ChunkId.FromString("W2Meshes");
        /**/public static readonly ChunkId Mesh = ChunkId.FromString("W2Mesh");
        /****/public static readonly ChunkId Sphere = ChunkId.FromString("W2MeshSphere");
        /****/public static readonly ChunkId SphereCentre = ChunkId.FromString("W2SphC");
        /****/public static readonly ChunkId SphereRadius = ChunkId.FromString("W2SphR");
        /****/public static readonly ChunkId BoundingBox = ChunkId.FromString("W2BBox");
        /****/public static readonly ChunkId BoundingBoxMin = ChunkId.FromString("W2BBMin");
        /****/public static readonly ChunkId BoundingBoxMax = ChunkId.FromString("W2BBMax");
        /****/public static readonly ChunkId MeshVertexPositions = ChunkId.FromString("W2VrtPos");
        /******/public static readonly ChunkId MeshVertexPosition = ChunkId.FromString("W2Pos");
        /****/public static readonly ChunkId MeshVertexNormals = ChunkId.FromString("W2VrtNorm");
        /******/public static readonly ChunkId MeshVertexNormal = ChunkId.FromString("W2N");
        /****/public static readonly ChunkId MeshVertexShades = ChunkId.FromString("W2VrtShd");
        /******/public static readonly ChunkId MeshVertexShade = ChunkId.FromString("W2Shd");
        /****/public static readonly ChunkId MeshPolygons = ChunkId.FromString("W2Polys");
        /******/public static readonly ChunkId MeshTriangle = ChunkId.FromString("W2Tr");
        /******/public static readonly ChunkId MeshQuad = ChunkId.FromString("W2Uq");
        /********/public static readonly ChunkId MeshPolygonExtra = ChunkId.FromString("W2Pe");
        /********/public static readonly ChunkId MeshPolygonIndices = ChunkId.FromString("W2PolyInd");
        /**********/public static readonly ChunkId MeshPolygonIndex = ChunkId.FromString("W2Ind");
        /********/public static readonly ChunkId MeshPolygonTexCoords = ChunkId.FromString("W2PolyUV");
        /**********/public static readonly ChunkId MeshPolygonTexCoord = ChunkId.FromString("W2UV");
        public static readonly ChunkId Waves = ChunkId.FromString("W2WaveSamples");
        /**/public static readonly ChunkId Wave = ChunkId.FromString("W2Wav");
        /****/public static readonly ChunkId WaveName = ChunkId.FromString("W2WavName");
        /****/public static readonly ChunkId WaveData = ChunkId.FromString("W2WavData");
        public static readonly ChunkId Moveables = ChunkId.FromString("W2Moveables");
        /****/public static readonly ChunkId Moveable = ChunkId.FromString("W2Moveable");
        /******/public static readonly ChunkId MoveableName = ChunkId.FromString("W2MovName");
        /******/public static readonly ChunkId MoveableOffset = ChunkId.FromString("W2MovOffset");
        /******/public static readonly ChunkId MoveableMeshes = ChunkId.FromString("W2MovMeshes");
        /********/public static readonly ChunkId MoveableMesh = ChunkId.FromString("W2MovMeshPtr");
        /******/public static readonly ChunkId MoveableLinks = ChunkId.FromString("W2Links");
        /********/public static readonly ChunkId MoveableLink = ChunkId.FromString("W2Lnk");
        /********/public static readonly ChunkId MoveableLinkOffset = ChunkId.FromString("W2LnkO");
        /******/public static readonly ChunkId Animations = ChunkId.FromString("W2Animations");
        /********/public static readonly ChunkId Animation = ChunkId.FromString("W2Anm");
        /**********/public static readonly ChunkId AnimationName = ChunkId.FromString("W2AnmName");
        /**********/public static readonly ChunkId StateChanges = ChunkId.FromString("W2StChs");
        /************/public static readonly ChunkId StateChange = ChunkId.FromString("W2StCh");
        /**************/public static readonly ChunkId Dispatches = ChunkId.FromString("W2Disps");
        /****************/public static readonly ChunkId Dispatch = ChunkId.FromString("W2Disp");
        /**********/public static readonly ChunkId KeyFrames = ChunkId.FromString("W2Kfs");
        /************/public static readonly ChunkId KeyFrame = ChunkId.FromString("W2Kf");
        /**************/public static readonly ChunkId KeyFrameBoundingBox = ChunkId.FromString("W2KfBB");
        /**************/public static readonly ChunkId KeyFrameOffset = ChunkId.FromString("W2KfOffs");
        /**************/public static readonly ChunkId KeyFrameAngles = ChunkId.FromString("W2KfAs");
        /****************/public static readonly ChunkId KeyFrameAngle = ChunkId.FromString("W2KfA");
        /**********/public static readonly ChunkId AnimCommands = ChunkId.FromString("W2Cmds");
        /************/public static readonly ChunkId AnimCommand = ChunkId.FromString("W2Cmd");
        public static readonly ChunkId Statics = ChunkId.FromString("W2Statics");
        /**/public static readonly ChunkId Static = ChunkId.FromString("W2Static");
        /****/public static readonly ChunkId StaticName = ChunkId.FromString("W2StaticName");
        /****/public static readonly ChunkId StaticVisibilityBox = ChunkId.FromString("W2StaticVB");
        /****/public static readonly ChunkId StaticCollisionBox = ChunkId.FromString("W2StaticCB");
        public static readonly ChunkId SpriteSequences = ChunkId.FromString("W2SpriteSequences");
        /**/public static readonly ChunkId SpriteSequence = ChunkId.FromString("W2SpriteSeq");
        /****/public static readonly ChunkId SpriteSequenceName = ChunkId.FromString("W2SpriteSeqName");
        /****/public static readonly ChunkId SpriteSequenceData = ChunkId.FromString("W2SpriteSeqData");
        /****/public static readonly ChunkId SpriteSequenceSprites = ChunkId.FromString("W2SpriteSeqSprites");
        /******/public static readonly ChunkId SpriteSequenceSprite = ChunkId.FromString("W2SpritePtr");
        public static readonly ChunkId Sounds = ChunkId.FromString("W2Sounds");
        /**/public static readonly ChunkId Sound = ChunkId.FromString("W2Sound");
        /****/public static readonly ChunkId SoundName = ChunkId.FromString("W2SoundName");
        /****/public static readonly ChunkId SoundSample = ChunkId.FromString("W2SamplePtr");
        /****/public static readonly ChunkId SoundSampleName = ChunkId.FromString("W2SampleName");
    }
}
