﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TombLib.IO;
using TombLib.Utils;
using TombLib.Wad;

namespace TombLib.LevelData.Compilers
{
    public partial class LevelCompilerClassicTR
    {
        private void WriteLevelTr3()
        {
            // Now begin to compile the geometry block in a MemoryStream
            // Now begin to compile the geometry block in a MemoryStream
            using (var writer = new BinaryWriterEx(new FileStream(_dest, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                ReportProgress(85, "Writing geometry data to memory buffer");

                // Write version
                writer.WriteBlockArray(new byte[] { 0x38, 0x00, 0x18, 0xFF });

                /*using (var readerPalette = new BinaryReader(new FileStream("Editor\\Misc\\Palette.Tr3.bin", FileMode.Open, FileAccess.Write, FileShare.None)))
                {
                    var palette = readerPalette.ReadBytes(1792);
                    // Write palette
                    writer.Write(palette);
                }*/

                // TODO: for now I write fake palette, they should be needed only for 8 bit textures
                for (var i = 0; i < 768; i++) writer.Write((byte)0x00);
                for (var i = 0; i < 1024; i++) writer.Write((byte)0x00);
                
                // Write textures
                int numTextureTiles = _texture32Data.GetLength(0) / (256 * 256 * 4);
                writer.Write(numTextureTiles);

                // Fake 8 bit textures (who uses 8 bit textures in 2018?)
                var fakeTextures = new byte[256 * 256 * numTextureTiles];
                writer.Write(fakeTextures);

                // 16 bit textures
                byte[] texture16Data = PackTextureMap32To16Bit(_texture32Data, 256, _texture32Data.GetLength(0) / (256 * 4));
                writer.Write(texture16Data);

                const int filler = 0;
                writer.Write(filler);

                var numRooms = (ushort)_level.Rooms.Count(r => r != null);
                writer.Write(numRooms);

                long offset;
                long offset2;
                foreach (var r in _level.Rooms.Where(r => r != null))
                {
                    _tempRooms[r].WriteTr3(writer);
                }

                // Write floordata
                var numFloorData = (uint)_floorData.Count;
                writer.Write(numFloorData);
                writer.WriteBlockArray(_floorData);

                // Write meshes
                offset = writer.BaseStream.Position;

                const int numMeshData = 0;
                writer.Write(numMeshData);
                var totalMeshSize = 0;

                for (var i = 0; i < _meshes.Count; i++)
                {
                    var meshSize = _meshes[i].WriteTr3(writer);
                    totalMeshSize += (int)meshSize;
                }

                offset2 = writer.BaseStream.Position;
                uint meshDataSize = (uint)((offset2 - offset - 4) / 2);

                // Save the size of the meshes
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                writer.Write(meshDataSize);
                writer.BaseStream.Seek(offset2, SeekOrigin.Begin);

                // Write mesh pointers
                writer.Write((uint)_meshPointers.Count);
                writer.WriteBlockArray(_meshPointers);

                // Write animations' data
                writer.Write((uint)_animations.Count);
                foreach (var anim in _animations)
                    anim.Write(writer, _level);

                writer.Write((uint)_stateChanges.Count);
                writer.WriteBlockArray(_stateChanges);

                writer.Write((uint)_animDispatches.Count);
                writer.WriteBlockArray(_animDispatches);

                writer.Write((uint)_animCommands.Count);
                writer.WriteBlockArray(_animCommands);

                writer.Write((uint)_meshTrees.Count);
                writer.WriteBlockArray(_meshTrees);

                writer.Write((uint)_frames.Count);
                writer.WriteBlockArray(_frames);

                writer.Write((uint)_moveables.Count);
                writer.WriteBlockArray(_moveables);

                writer.Write((uint)_staticMeshes.Count);
                writer.WriteBlockArray(_staticMeshes);

                // Sprites
                // TODO: Wad2 should contain all sprites
                /*
                using (var readerSprites = new BinaryReader(new FileStream("sprites3.bin", FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    var bufferSprites = readerSprites.ReadBytes((int)readerSprites.BaseStream.Length);
                    writer.Write(bufferSprites);
                }*/

                writer.Write((uint)_spriteTextures.Count);
                writer.WriteBlockArray(_spriteTextures);

                writer.Write((uint)_spriteSequences.Count);
                writer.WriteBlockArray(_spriteSequences);
                
                // Write camera, sound sources
                writer.Write((uint)_cameras.Count);
                writer.WriteBlockArray(_cameras);

                writer.Write((uint)_soundSources.Count);
                writer.WriteBlockArray(_soundSources);

                // Write pathfinding data
                writer.Write((uint)_boxes.Length);
                writer.WriteBlockArray(_boxes);

                writer.Write((uint)_overlaps.Length);
                writer.WriteBlockArray(_overlaps);

                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone1_Normal);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone2_Normal);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone3_Normal);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone4_Normal);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].FlyZone_Normal);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone1_Alternate);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone2_Alternate);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone3_Alternate);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].GroundZone4_Alternate);
                for (var i = 0; i < _boxes.Length; i++)
                    writer.Write(_zones[i].FlyZone_Alternate);

                // Write animated textures
                _objectTextureManager.WriteAnimatedTexturesForTr4(writer);

                // Write object textures
                _objectTextureManager.WriteObjectTextures(writer, _level);
                
                // Write items and AI objects
                writer.Write((uint)_items.Count);
                writer.WriteBlockArray(_items);

                var lightmap = new byte[8192];
                writer.Write(lightmap);

                const short numDemo = 0;
                writer.Write(numDemo);
                writer.Write(numDemo);

                // Write sounds

                // Write sound map
                var soundMapSize = GetSoundMapSize();
                var lastSound = 0;
                for (int i = 0; i < soundMapSize; i++)
                {
                    short soundMapValue = -1;
                    if (_level.Wad.SoundInfo.ContainsKey((ushort)i))
                    {
                        soundMapValue = (short)lastSound;
                        lastSound++;
                    }

                    writer.Write(soundMapValue);
                }

                // Write sound details
                writer.Write((uint)_level.Wad.SoundInfo.Count);

                short lastSample = 0;

                for (int i = 0; i < _level.Wad.SoundInfo.Count; i++)
                {
                    var wadInfo = _level.Wad.SoundInfo.ElementAt(i).Value;
                    var soundInfo = new tr_sound_details();

                    soundInfo.Sample = lastSample;
                    soundInfo.Volume = wadInfo.Volume;
                    soundInfo.Range = wadInfo.Range;
                    soundInfo.Pitch = wadInfo.Pitch;
                    soundInfo.Chance = wadInfo.Chance;

                    ushort characteristics = (ushort)(wadInfo.Samples.Count << 2);
                    if (wadInfo.FlagN)
                        characteristics |= 0x1000;
                    if (wadInfo.RandomizePitch)
                        characteristics |= 0x2000;
                    if (wadInfo.RandomizeGain)
                        characteristics |= 0x4000;
                    characteristics |= (byte)wadInfo.Loop;

                    soundInfo.Characteristics = characteristics;

                    writer.WriteBlock<tr_sound_details>(soundInfo);

                    lastSample += (short)wadInfo.Samples.Count;
                }

                // TODO: samples are in MAIN.SFX so I have to found a way to write samples indices here
                int numSampleIndices = lastSample;
                writer.Write(numSampleIndices);
                int filler3 = 0;
                for (int i = 0; i < numSampleIndices; i++)
                    writer.Write(filler3);

                writer.Flush();
            }

            return;
        }
    }
}
