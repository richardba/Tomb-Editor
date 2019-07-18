﻿using TombLib.IO;

namespace TombLib.LevelData.IO
{
    // TODO Add documentation for binary offsets of the chunks
    internal static class Prj2Chunks
    {
        public static readonly byte[] MagicNumber = new byte[] { 0x50, 0x52, 0x4A, 0x32 };

        public static readonly ChunkId Settings = ChunkId.FromString("TeSettings");
        /**/public static readonly ChunkId ObsoleteWadFilePath = ChunkId.FromString("TeWadFilePath"); // UTF-8 string
        /**/public static readonly ChunkId FontTextureFilePath = ChunkId.FromString("TeFontTextureFilePath"); // UTF-8 string
        /**/public static readonly ChunkId SkyTextureFilePath = ChunkId.FromString("TeSkyTextureFilePath"); // UTF-8 string
        /**/public static readonly ChunkId Tr5ExtraSpritesFilePath = ChunkId.FromString("TeTr5ExtraSpritesFilePath"); // UTF-8 string
        /**/public static readonly ChunkId Tr5LaraType = ChunkId.FromString("TeTr5LaraType");
        /**/public static readonly ChunkId Tr5Weather = ChunkId.FromString("TeTr5Weather");
        /**/public static readonly ChunkId OldWadSoundPaths = ChunkId.FromString("TeOldWadSoundPaths");
        /****/public static readonly ChunkId OldWadSoundUpdateTag1_0_8 = ChunkId.FromString("TeOldWadSoundUpdateTag1_0_8");
        /****/public static readonly ChunkId OldWadSoundPath = ChunkId.FromString("TeOldWadSoundPath");
        /********/public static readonly ChunkId OldWadSoundPathPath = ChunkId.FromString("TePath"); // UTF-8 string
        /**/public static readonly ChunkId ScriptDirectory = ChunkId.FromString("TeScriptDirectory"); // UTF-8 string
        /**/public static readonly ChunkId GameDirectory = ChunkId.FromString("TeGameDirectory"); // UTF-8 string
        /**/public static readonly ChunkId GameLevelFilePath = ChunkId.FromString("TeGameLevelFilePath"); // UTF-8 string
        /**/public static readonly ChunkId GameExecutableFilePath = ChunkId.FromString("TeGameExecutableFilePath"); // UTF-8 string
        /**/public static readonly ChunkId GameEnableQuickStartFeature = ChunkId.FromString("TeGameEnableQuickStartFeature"); // UTF-8 string
        /**/public static readonly ChunkId GameVersion = ChunkId.FromString("TeGameVersion");
        /**/public static readonly ChunkId DefaultAmbientLight = ChunkId.FromString("TeDefaultAmbientLight");
        /**/public static readonly ChunkId TexturePadding = ChunkId.FromString("TeTexturePadding");
        /**/public static readonly ChunkId AgressiveTexturePacking = ChunkId.FromString("TeAgressiveTexturePacking");
        /**/public static readonly ChunkId AgressiveFloordataPacking = ChunkId.FromString("TeAgressiveFloordataPacking");
        /**/public static readonly ChunkId Wads = ChunkId.FromString("TeWads");
        /****/public static readonly ChunkId Wad = ChunkId.FromString("TeWad");
        /******/public static readonly ChunkId WadPath = ChunkId.FromString("TePath");
        /**/public static readonly ChunkId Textures = ChunkId.FromString("TeTextures");
        /******/public static readonly ChunkId InvisibleTexture = ChunkId.FromString("TeInvisibleTexture");
        /******/public static readonly ChunkId LevelTexture = ChunkId.FromString("TeLvlTexture");
        /**********/public static readonly ChunkId LevelTextureIndex = ChunkId.FromString("TeI");
        /**********/public static readonly ChunkId LevelTexturePath = ChunkId.FromString("TePath");
        /**********/public static readonly ChunkId LevelTextureConvert512PixelsToDoubleRows = ChunkId.FromString("Te512C");
        /**********/public static readonly ChunkId LevelTextureReplaceMagentaWithTransparency = ChunkId.FromString("TeMagentaR");
        /**********/public static readonly ChunkId LevelTextureFootStepSounds = ChunkId.FromString("TeTextureSounds");
        /**********/public static readonly ChunkId LevelTextureBumpmaps = ChunkId.FromString("TeTextureBumpmaps");
        /***********/public static readonly ChunkId LevelTextureCustomBumpmapPath = ChunkId.FromString("TeTextureCustomBumpmapPath");
        /**/public static readonly ChunkId ImportedGeometries = ChunkId.FromString("TeImportedGeometries");
        /******/public static readonly ChunkId ImportedGeometry = ChunkId.FromString("TeImportedGeometry");
        /********/public static readonly ChunkId ImportedGeometryIndex = ChunkId.FromString("TeI");
        /********/public static readonly ChunkId ImportedGeometryName = ChunkId.FromString("TeName");
        /********/public static readonly ChunkId ImportedGeometryPath = ChunkId.FromString("TePath");
        /********/public static readonly ChunkId ImportedGeometryScale = ChunkId.FromString("TeScale");
        /********/public static readonly ChunkId ImportedGeometryPosAxisFlags = ChunkId.FromString("TePosAxisFlags");
        /********/public static readonly ChunkId ImportedGeometryTexAxisFlags = ChunkId.FromString("TeTexAxisFlags");
        /********/public static readonly ChunkId ImportedGeometryInvertFaces = ChunkId.FromString("TeInvertFaces");
        /**/public static readonly ChunkId AnimatedTextureSets = ChunkId.FromString("TeAnimatedTextureSets");
        /******/public static readonly ChunkId AnimatedTextureSet = ChunkId.FromString("TeAnimatedTextureSet");
        /**********/public static readonly ChunkId AnimatedTextureSetName = ChunkId.FromString("TeAnimatedTextureSetName");
        /**********/public static readonly ChunkId AnimatedTextureSetType = ChunkId.FromString("TeAnimatedTextureSetType");
        /**********/public static readonly ChunkId AnimatedTextureSetFps = ChunkId.FromString("TeAnimatedTextureSetFps");
        /**********/public static readonly ChunkId AnimatedTextureSetUvRotate = ChunkId.FromString("TeAnimatedTextureSetUvRotate");
        /**********/public static readonly ChunkId AnimatedTextureSetExtraInfo = ChunkId.FromString("TeAnimatedTextureSetExtra");
        /**************/public static readonly ChunkId AnimatedTextureFrames = ChunkId.FromString("TeFrames");
        /******************/public static readonly ChunkId AnimatedTextureFrame = ChunkId.FromString("TeFrame");
        public static readonly ChunkId Rooms = ChunkId.FromString("TeRooms");
        /**/public static readonly ChunkId Room = ChunkId.FromString("TeRoom"); // Contains X, Y sectors, Name, Position directly
        /******/public static readonly ChunkId RoomIndex = ChunkId.FromString("TeI");
        /******/public static readonly ChunkId RoomName = ChunkId.FromString("TeName");
        /******/public static readonly ChunkId RoomPosition = ChunkId.FromString("TePos");
        /******/public static readonly ChunkId RoomSectors = ChunkId.FromString("TeSecs");
        /**********/public static readonly ChunkId Sector = ChunkId.FromString("TeS");
        /**************/public static readonly ChunkId SectorProperties = new ChunkId(new byte[] { 0 }); // These chunks occur very often, this minimizes their size impact
        /**************/public static readonly ChunkId SectorFloor = new ChunkId(new byte[] { 1 });
        /**************/public static readonly ChunkId SectorCeiling = new ChunkId(new byte[] { 2 });
        /**************/public static readonly ChunkId TextureLevelTexture = new ChunkId(new byte[] { 16 });
        /**************/public static readonly ChunkId TextureLevelTexture2 = new ChunkId(new byte[] { 18 });
        /**************/public static readonly ChunkId TextureInvisible = new ChunkId(new byte[] { 17 });
        /******/public static readonly ChunkId RoomAmbientLight = ChunkId.FromString("TeAmbient");
        /******/public static readonly ChunkId RoomAlternate = ChunkId.FromString("TeAlternate");
        /**********/public static readonly ChunkId AlternateRoom = ChunkId.FromString("TeRoom");
        /**********/public static readonly ChunkId AlternateGroup = ChunkId.FromString("TeGroup");
        /******/public static readonly ChunkId RoomFlagCold = ChunkId.FromString("TeCold");
        /******/public static readonly ChunkId RoomFlagDamage = ChunkId.FromString("TeDmg");
        /******/public static readonly ChunkId RoomFlagHorizon = ChunkId.FromString("TeHorizon");
        /******/public static readonly ChunkId RoomFlagOutside = ChunkId.FromString("TeOutside");
        /******/public static readonly ChunkId RoomFlagNoLensflare = ChunkId.FromString("TeNoLens");
        /******/public static readonly ChunkId RoomFlagExcludeFromPathFinding = ChunkId.FromString("TeNoPath");
        /******/public static readonly ChunkId RoomWaterLevel = ChunkId.FromString("TeWater");  // DEPRECATED
        /******/public static readonly ChunkId RoomRainLevel = ChunkId.FromString("TeRain");  // DEPRECATED
        /******/public static readonly ChunkId RoomSnowLevel = ChunkId.FromString("TeSnow");  // DEPRECATED
        /******/public static readonly ChunkId RoomQuickSandLevel = ChunkId.FromString("TeQuickSand");  // DEPRECATED
        /******/public static readonly ChunkId RoomMistLevel = ChunkId.FromString("TeMist");  // DEPRECATED
        /******/public static readonly ChunkId RoomReflectionLevel = ChunkId.FromString("TeReflect");  // DEPRECATED
        /******/public static readonly ChunkId RoomType = ChunkId.FromString("TeRoomType");
        /******/public static readonly ChunkId RoomTypeStrength = ChunkId.FromString("TeRoomTypeStrength");
        /******/public static readonly ChunkId RoomLightEffect = ChunkId.FromString("TeRoomLightEffect");
        /******/public static readonly ChunkId RoomLightEffectStrength = ChunkId.FromString("TeRoomLightEffectStrength");
        /******/public static readonly ChunkId RoomReverberation = ChunkId.FromString("TeReverb");
        /******/public static readonly ChunkId RoomLocked = ChunkId.FromString("TeLocked");
        /******/public static readonly ChunkId Objects = ChunkId.FromString("TeObjects");
        /**********/public static readonly ChunkId ObjectMovable = ChunkId.FromString("TeMov");
        /**********/public static readonly ChunkId ObjectMovable2 = ChunkId.FromString("TeMov2");
        /**********/public static readonly ChunkId ObjectItemLuaId = ChunkId.FromString("TeItLuaId");
        /**********/public static readonly ChunkId ObjectStatic = ChunkId.FromString("TeSta");
        /**********/public static readonly ChunkId ObjectStatic2 = ChunkId.FromString("TeSta2");
        /**********/public static readonly ChunkId ObjectCamera = ChunkId.FromString("TeCam");
        /**********/public static readonly ChunkId ObjectFlyBy = ChunkId.FromString("TeFly");
        /**********/public static readonly ChunkId ObjectFlyBy2 = ChunkId.FromString("TeFly2");
        /**********/public static readonly ChunkId ObjectFlyBy2LuaScript = ChunkId.FromString("TeFly2Lua");
        /**********/public static readonly ChunkId ObjectSink = ChunkId.FromString("TeSin");
        /**********/public static readonly ChunkId ObjectSoundSource = ChunkId.FromString("TeSou");
        /**********/public static readonly ChunkId ObjectSoundSource2 = ChunkId.FromString("TeSou2");
        /**********/public static readonly ChunkId ObjectSoundSource3 = ChunkId.FromString("TeSou3");
        /**********/public static readonly ChunkId ObjectImportedGeometry = ChunkId.FromString("TeImp");
        /**********/public static readonly ChunkId ObjectImportedGeometry2 = ChunkId.FromString("TeImp2");
        /**********/public static readonly ChunkId ObjectImportedGeometry3 = ChunkId.FromString("TeImp3");
        /**********/public static readonly ChunkId ObjectImportedGeometryMeshFilter = ChunkId.FromString("TeImpMshF");
        /**********/public static readonly ChunkId ObjectImportedGeometryLightingModel = ChunkId.FromString("TeImpLM");
        /**********/public static readonly ChunkId ObjectLight = ChunkId.FromString("TeLig");
        /**********/public static readonly ChunkId ObjectLight2 = ChunkId.FromString("TeLig2");
        /**********/public static readonly ChunkId ObjectPortal = ChunkId.FromString("TePor");
        /**********/public static readonly ChunkId ObjectTrigger = ChunkId.FromString("TeTri");
        /**********/public static readonly ChunkId ObjectTrigger2 = ChunkId.FromString("TeTri2");
        /************/public static readonly ChunkId ObjectTrigger2Type = ChunkId.FromString("TeTy");
        /************/public static readonly ChunkId ObjectTrigger2TargetType = ChunkId.FromString("TeTaTy");
        /************/public static readonly ChunkId ObjectTrigger2Target = ChunkId.FromString("TeTa");
        /************/public static readonly ChunkId ObjectTrigger2Timer = ChunkId.FromString("TeTi");
        /************/public static readonly ChunkId ObjectTrigger2Extra = ChunkId.FromString("TeEx");
        /************/public static readonly ChunkId ObjectTrigger2CodeBits = ChunkId.FromString("TeCo");
        /************/public static readonly ChunkId ObjectTrigger2OneShot = ChunkId.FromString("TeOS");
        /************/public static readonly ChunkId ObjectTrigger2LuaScript = ChunkId.FromString("TeTrLua");
        public static readonly ChunkId EmbeddedSoundInfoWad = ChunkId.FromString("TeEmbeddedSoundInfoWad");
    }
}
