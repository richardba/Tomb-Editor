﻿using TombLib.IO;

namespace TombLib.Wad
{
    public static class Wad2Chunks
    {
        public static readonly byte[] MagicNumberObsolete = new byte[] { 0x57, 0x61, 0x64, 0x32 };
        public static readonly byte[] MagicNumber = new byte[] { 0x57, 0x41, 0x44, 0x32 };
        public static readonly ChunkId SuggestedGameVersion = ChunkId.FromString("W2SuggestedGameVersion");
        //public static readonly ChunkId TrNgWadObsolete = ChunkId.FromString("W2TrNgWad");
        //public static readonly ChunkId SoundManagementSystemObsolete = ChunkId.FromString("W2SoundMgmt");
        public static readonly ChunkId Textures = ChunkId.FromString("W2Textures");
        /**/public static readonly ChunkId Texture = ChunkId.FromString("W2Txt");
        /****/public static readonly ChunkId TextureIndex = ChunkId.FromString("W2Index");
        /****/public static readonly ChunkId TextureData = ChunkId.FromString("W2TxtData");
        public static readonly ChunkId Samples = ChunkId.FromString("W2WaveSamples");
        /**/public static readonly ChunkId Sample = ChunkId.FromString("W2Wav");
        /****/public static readonly ChunkId SampleIndex = ChunkId.FromString("W2Index");
        /****/public static readonly ChunkId SampleFilenameObsolete = ChunkId.FromString("W2WavName");
        /****/public static readonly ChunkId SampleData = ChunkId.FromString("W2WavData");
        public static readonly ChunkId SoundInfosObsolete = ChunkId.FromString("W2Sounds");
        public static readonly ChunkId SoundInfos = ChunkId.FromString("W2Sounds2");
        /**/public static readonly ChunkId SoundInfo = ChunkId.FromString("W2Sound");
        /****/public static readonly ChunkId SoundInfoNameObsolete = ChunkId.FromString("W2SampleName");
        /****/public static readonly ChunkId SoundInfoName = ChunkId.FromString("W2Name");
        /****/public static readonly ChunkId SoundInfoIndex = ChunkId.FromString("W2Index");
        /****/public static readonly ChunkId SoundInfoVolume = ChunkId.FromString("W2Vol");
        /****/public static readonly ChunkId SoundInfoRange = ChunkId.FromString("W2Ran");
        /****/public static readonly ChunkId SoundInfoPitch = ChunkId.FromString("W2Pit");
        /****/public static readonly ChunkId SoundInfoChance = ChunkId.FromString("W2Cha");
        /****/public static readonly ChunkId SoundInfoDisablePanning = ChunkId.FromString("W2NoPan");
        /****/public static readonly ChunkId SoundInfoRandomizePitch = ChunkId.FromString("W2RngP");
        /****/public static readonly ChunkId SoundInfoRandomizeVolume = ChunkId.FromString("W2RngV");
        /****/public static readonly ChunkId SoundInfoLoopBehaviour = ChunkId.FromString("W2Loop");
        /****/public static readonly ChunkId SoundInfoTargetSampleRate = ChunkId.FromString("W2SamplRate");
        /****/public static readonly ChunkId SoundInfoSampleIndex = ChunkId.FromString("W2SamplePtr");
        public static readonly ChunkId FixedSoundInfos = ChunkId.FromString("W2FixedSounds");
        /**/public static readonly ChunkId FixedSoundInfo = ChunkId.FromString("W2FixedSound");
        /****/public static readonly ChunkId FixedSoundInfoId = ChunkId.FromString("W2Id");
        /****/public static readonly ChunkId FixedSoundInfoSoundInfoId = ChunkId.FromString("W2Sound");
        public static readonly ChunkId Meshes = ChunkId.FromString("W2Meshes");
        /**/public static readonly ChunkId Mesh = ChunkId.FromString("W2Mesh");
        /****/public static readonly ChunkId MeshIndex = ChunkId.FromString("W2Index");
        /****/public static readonly ChunkId MeshName = ChunkId.FromString("W2MeshName");
        /****/public static readonly ChunkId MeshSphere = ChunkId.FromString("W2MeshSphere");
        /****/public static readonly ChunkId MeshSphereCenter = ChunkId.FromString("W2SphC");
        /****/public static readonly ChunkId MeshSphereRadius = ChunkId.FromString("W2SphR");
        /****/public static readonly ChunkId MeshBoundingBox = ChunkId.FromString("W2BBox");
        /****/public static readonly ChunkId MeshBoundingBoxMin = ChunkId.FromString("W2BBMin");
        /****/public static readonly ChunkId MeshBoundingBoxMax = ChunkId.FromString("W2BBMax");
        /****/public static readonly ChunkId MeshVertexPositions = ChunkId.FromString("W2VrtPos");
        /******/public static readonly ChunkId MeshVertexPosition = ChunkId.FromString("W2Pos");
        /****/public static readonly ChunkId MeshVertexNormals = ChunkId.FromString("W2VrtNorm");
        /******/public static readonly ChunkId MeshVertexNormal = ChunkId.FromString("W2N");
        /****/public static readonly ChunkId MeshVertexShades = ChunkId.FromString("W2VrtShd");
        /******/public static readonly ChunkId MeshVertexShade = ChunkId.FromString("W2Shd");
        /****/public static readonly ChunkId MeshPolygons = ChunkId.FromString("W2Polys");
        /******/public static readonly ChunkId MeshTriangle = ChunkId.FromString("W2Tr");
        /******/public static readonly ChunkId MeshQuad = ChunkId.FromString("W2Uq");
        /********/public static readonly ChunkId MeshPolygonExtraObsolete = ChunkId.FromString("W2Pe");
        /********/public static readonly ChunkId MeshPolygonIndices = ChunkId.FromString("W2PolyInd");
        /**********/public static readonly ChunkId MeshPolygonIndex = ChunkId.FromString("W2Ind");
        /********/public static readonly ChunkId MeshPolygonTexCoords = ChunkId.FromString("W2PolyUV");
        /**********/public static readonly ChunkId MeshPolygonTexCoord = ChunkId.FromString("W2UV");
        public static readonly ChunkId Sprites = ChunkId.FromString("W2Sprites");
        /**/public static readonly ChunkId Sprite = ChunkId.FromString("W2Spr");
        /****/public static readonly ChunkId SpriteIndex = ChunkId.FromString("W2Index");
        /****/public static readonly ChunkId SpriteSidesObsolete = ChunkId.FromString("W2SprSides");
        /****/public static readonly ChunkId SpriteData = ChunkId.FromString("W2TxtData");
        public static readonly ChunkId SpriteSequences = ChunkId.FromString("W2SpriteSequences");
        /**/public static readonly ChunkId SpriteSequence = ChunkId.FromString("W2SpriteSeq");
        ///****/public static readonly ChunkId SpriteSequenceName = ChunkId.FromString("W2SpriteSeqName");
        /****/public static readonly ChunkId SpriteSequenceData = ChunkId.FromString("W2SpriteSeqData");
        /****/public static readonly ChunkId SpriteSequenceSpriteIndices = ChunkId.FromString("W2SpriteSeqSprites");
        /******/public static readonly ChunkId SpriteSequenceSpriteIndex = ChunkId.FromString("W2SpritePtr");
        public static readonly ChunkId Moveables = ChunkId.FromString("W2Moveables");
        /****/public static readonly ChunkId Moveable = ChunkId.FromString("W2Moveable");
        ///******/public static readonly ChunkId MoveableName = ChunkId.FromString("W2MovName");
        /******/public static readonly ChunkId MoveableMeshes = ChunkId.FromString("W2MovMeshes");
        /********/public static readonly ChunkId MoveableMesh = ChunkId.FromString("W2MovMeshPtr");
        /******/public static readonly ChunkId MoveableLinks = ChunkId.FromString("W2Links");
        /********/public static readonly ChunkId MoveableLink = ChunkId.FromString("W2Lnk");
        /********/public static readonly ChunkId MoveableBone = ChunkId.FromString("W2Bone");
        /**********/public static readonly ChunkId MoveableBoneTransform = ChunkId.FromString("W2BoneTransf");
        /**********/public static readonly ChunkId MoveableBoneMeshPointer = ChunkId.FromString("W2BoneMesh");
        /**********/public static readonly ChunkId MoveableBoneName = ChunkId.FromString("W2BoneName");
        /**********/public static readonly ChunkId MoveableBoneTranslation = ChunkId.FromString("W2BoneTrans");
        /********/public static readonly ChunkId MoveableLinkOffset = ChunkId.FromString("W2LnkO");
        /******/public static readonly ChunkId Animations = ChunkId.FromString("W2Animations");
        /********/public static readonly ChunkId AnimationObsolete = ChunkId.FromString("W2Anm");
        /********/public static readonly ChunkId Animation = ChunkId.FromString("W2Ani");
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
        ///****/public static readonly ChunkId StaticName = ChunkId.FromString("W2StaticName");
        /****/public static readonly ChunkId StaticVisibilityBox = ChunkId.FromString("W2StaticVB");
        /****/public static readonly ChunkId StaticCollisionBox = ChunkId.FromString("W2StaticCB");
        /****/public static readonly ChunkId StaticLight = ChunkId.FromString("W2StaticLight");
        /******/public static readonly ChunkId StaticLightPosition = ChunkId.FromString("W2StaticLightPos");
        /******/public static readonly ChunkId StaticLightRadius = ChunkId.FromString("W2StaticLightR");
        /******/public static readonly ChunkId StaticLightIntensity = ChunkId.FromString("W2StaticLightI");
    }
}
