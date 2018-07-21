﻿using DarkUI.Forms;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TombEditor.Forms;
using TombLib;
using TombLib.Forms;
using TombLib.GeometryIO;
using TombLib.GeometryIO.Exporters;
using TombLib.Graphics;
using TombLib.LevelData;
using TombLib.LevelData.Compilers;
using TombLib.LevelData.IO;
using TombLib.Rendering;
using TombLib.Utils;
using TombLib.Wad;

namespace TombEditor
{
    public static class EditorActions
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Editor _editor = Editor.Instance;

        public static bool ContinueOnFileDrop(IWin32Window owner, string description)
        {
            if (!_editor.HasUnsavedChanges)
                return true;

            switch (DarkMessageBox.Show(owner,
                "Your unsaved changes will be lost. Do you want to save?",
                description,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2))
            {
                case DialogResult.No:
                    return true;
                case DialogResult.Yes:
                    return SaveLevel(owner, false);
                default:
                    return false;
            }
        }

        public static void SmartBuildGeometry(Room room, RectangleInt2 area)
        {
            var watch = new Stopwatch();
            watch.Start();
            room.SmartBuildGeometry(area);
            watch.Stop();
            logger.Debug("Edit geometry time: " + watch.ElapsedMilliseconds + "  ms");
            _editor.RoomGeometryChange(room);
        }

        private enum SmoothGeometryEditingType
        {
            None,
            Floor,
            Wall,
            Any
        }

        public static void EditSectorGeometry(Room room, RectangleInt2 area, ArrowType arrow, BlockVertical vertical, short increment, bool smooth, bool oppositeDiagonalCorner = false, bool autoSwitchDiagonals = false, bool autoUpdateThroughPortal = true)
        {
            if (smooth)
            {
                // Scan selection and decide if the selected zone is wall-only, floor-only, or both.
                // It's needed to force smoothing function to edit either only wall sections or floor sections,
                // in case user wants to smoothly edit only wall splits or actual floor height.

                SmoothGeometryEditingType smoothEditingType = SmoothGeometryEditingType.None;

                for (int x = area.X0; x <= area.X1; x++)
                {
                    for (int z = area.Y0; z <= area.Y1; z++)
                    {
                        if (smoothEditingType != SmoothGeometryEditingType.Wall && room.Blocks[x, z].Type == BlockType.Floor)
                            smoothEditingType = SmoothGeometryEditingType.Floor;
                        else if (smoothEditingType != SmoothGeometryEditingType.Floor && room.Blocks[x, z].Type != BlockType.Floor)
                            smoothEditingType = SmoothGeometryEditingType.Wall;
                        else
                        {
                            smoothEditingType = SmoothGeometryEditingType.Any;
                            break;
                        }
                    }
                    if (smoothEditingType == SmoothGeometryEditingType.Any)
                        break;
                }

                // Adjust editing area to exclude the side on which the arrow starts
                // This is a superset of the behaviour of the old editor to smooth edit a single edge or side.
                switch (arrow)
                {
                    case ArrowType.EdgeE:
                        area = new RectangleInt2(area.X0 + 1, area.Y0, area.X1, area.Y1);
                        break;
                    case ArrowType.EdgeN:
                        area = new RectangleInt2(area.X0, area.Y0 + 1, area.X1, area.Y1);
                        break;
                    case ArrowType.EdgeW:
                        area = new RectangleInt2(area.X0, area.Y0, area.X1 - 1, area.Y1);
                        break;
                    case ArrowType.EdgeS:
                        area = new RectangleInt2(area.X0, area.Y0, area.X1, area.Y1 - 1);
                        break;
                    case ArrowType.CornerNE:
                        area = new RectangleInt2(area.X0 + 1, area.Y0 + 1, area.X1, area.Y1);
                        break;
                    case ArrowType.CornerNW:
                        area = new RectangleInt2(area.X0, area.Y0 + 1, area.X1 - 1, area.Y1);
                        break;
                    case ArrowType.CornerSW:
                        area = new RectangleInt2(area.X0, area.Y0, area.X1 - 1, area.Y1 - 1);
                        break;
                    case ArrowType.CornerSE:
                        area = new RectangleInt2(area.X0 + 1, area.Y0, area.X1, area.Y1 - 1);
                        break;
                }
                arrow = ArrowType.EntireFace;

                Action<Block, BlockEdge> smoothEdit = (Block block, BlockEdge edge) =>
                {
                    if (block == null)
                        return;

                    if (vertical.IsOnFloor() && block.Floor.DiagonalSplit == DiagonalSplit.None ||
                       vertical.IsOnCeiling() && block.Ceiling.DiagonalSplit == DiagonalSplit.None)
                    {
                        if (smoothEditingType == SmoothGeometryEditingType.Any ||
                           !block.IsAnyWall && smoothEditingType == SmoothGeometryEditingType.Floor ||
                           !block.IsAnyWall && smoothEditingType == SmoothGeometryEditingType.Wall)
                        {
                            block.ChangeHeight(vertical, edge, increment);
                            block.FixHeights(vertical);
                        }
                    }
                };


                // Smoothly change sectors on the corners
                smoothEdit(room.GetBlockTry(area.X0 - 1, area.Y1 + 1), BlockEdge.XpZn);
                smoothEdit(room.GetBlockTry(area.X1 + 1, area.Y1 + 1), BlockEdge.XnZn);
                smoothEdit(room.GetBlockTry(area.X1 + 1, area.Y0 - 1), BlockEdge.XnZp);
                smoothEdit(room.GetBlockTry(area.X0 - 1, area.Y0 - 1), BlockEdge.XpZp);

                // Smoothly change sectors on the sides
                for (int x = area.X0; x <= area.X1; x++)
                {
                    smoothEdit(room.GetBlockTry(x, area.Y0 - 1), BlockEdge.XnZp);
                    smoothEdit(room.GetBlockTry(x, area.Y0 - 1), BlockEdge.XpZp);

                    smoothEdit(room.GetBlockTry(x, area.Y1 + 1), BlockEdge.XnZn);
                    smoothEdit(room.GetBlockTry(x, area.Y1 + 1), BlockEdge.XpZn);
                }

                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    smoothEdit(room.GetBlockTry(area.X0 - 1, z), BlockEdge.XpZp);
                    smoothEdit(room.GetBlockTry(area.X0 - 1, z), BlockEdge.XpZn);

                    smoothEdit(room.GetBlockTry(area.X1 + 1, z), BlockEdge.XnZp);
                    smoothEdit(room.GetBlockTry(area.X1 + 1, z), BlockEdge.XnZn);
                }
            }

            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    Block block = room.Blocks[x, z];
                    Room.RoomBlockPair lookupBlock = room.GetBlockTryThroughPortal(x, z);

                    EditBlock:
                    {
                        if (arrow == ArrowType.EntireFace)
                        {
                            if (vertical == BlockVertical.Floor || vertical == BlockVertical.Ceiling)
                                block.RaiseStepWise(vertical, oppositeDiagonalCorner, increment, autoSwitchDiagonals);
                            else
                                block.Raise(vertical, false, increment);
                        }
                        else
                        {
                            var currentSplit = vertical.IsOnFloor() ? block.Floor.DiagonalSplit : block.Ceiling.DiagonalSplit;
                            var incrementInvalid = vertical.IsOnFloor() ? increment < 0 : increment > 0;
                            BlockEdge[] corners = new BlockEdge[2] { BlockEdge.XnZp, BlockEdge.XnZp };
                            DiagonalSplit[] splits = new DiagonalSplit[2] { DiagonalSplit.None, DiagonalSplit.None };

                            switch (arrow)
                            {
                                case ArrowType.EdgeN:
                                case ArrowType.CornerNW:
                                    corners[0] = BlockEdge.XnZp;
                                    corners[1] = BlockEdge.XpZp;
                                    splits[0] = DiagonalSplit.XpZn;
                                    splits[1] = arrow == ArrowType.CornerNW ? DiagonalSplit.XnZp : DiagonalSplit.XnZn;
                                    break;
                                case ArrowType.EdgeE:
                                case ArrowType.CornerNE:
                                    corners[0] = BlockEdge.XpZp;
                                    corners[1] = BlockEdge.XpZn;
                                    splits[0] = DiagonalSplit.XnZn;
                                    splits[1] = arrow == ArrowType.CornerNE ? DiagonalSplit.XpZp : DiagonalSplit.XnZp;
                                    break;
                                case ArrowType.EdgeS:
                                case ArrowType.CornerSE:
                                    corners[0] = BlockEdge.XpZn;
                                    corners[1] = BlockEdge.XnZn;
                                    splits[0] = DiagonalSplit.XnZp;
                                    splits[1] = arrow == ArrowType.CornerSE ? DiagonalSplit.XpZn : DiagonalSplit.XpZp;
                                    break;
                                case ArrowType.EdgeW:
                                case ArrowType.CornerSW:
                                    corners[0] = BlockEdge.XnZn;
                                    corners[1] = BlockEdge.XnZp;
                                    splits[0] = DiagonalSplit.XpZp;
                                    splits[1] = arrow == ArrowType.CornerSW ? DiagonalSplit.XnZn : DiagonalSplit.XpZn;
                                    break;
                            }

                            if (arrow <= ArrowType.EdgeW)
                            {
                                if (block.Type != BlockType.Wall && currentSplit != DiagonalSplit.None)
                                    continue;
                                for (int i = 0; i < 2; i++)
                                    if (currentSplit != splits[i])
                                        block.ChangeHeight(vertical, corners[i], increment);
                            }
                            else
                            {
                                if (block.Type != BlockType.Wall && currentSplit != DiagonalSplit.None)
                                {
                                    if (currentSplit == splits[1])
                                    {
                                        if (block.GetHeight(vertical, corners[0]) == block.GetHeight(vertical, corners[1]) && incrementInvalid)
                                            continue;
                                    }
                                    else if (autoSwitchDiagonals && currentSplit == splits[0] && block.GetHeight(vertical, corners[0]) == block.GetHeight(vertical, corners[1]) && !incrementInvalid)
                                        block.Transform(new RectTransformation { QuadrantRotation = 2 }, vertical.IsOnFloor());
                                    else
                                        continue;
                                }
                                block.ChangeHeight(vertical, corners[0], increment);
                            }
                        }
                        block.FixHeights(vertical);
                    }

                    if (autoUpdateThroughPortal && lookupBlock.Block != block)
                    {
                        block = lookupBlock.Block;
                        goto EditBlock;
                    }

                    // FIXME: VERY SLOW CODE! Since we need to update geometry in adjoining block through portal, and each block may contain portal to different room,
                    // we need to find a way to quickly update geometry in all possible adjoining rooms in area. Until then, this function is used on per-sector basis.

                    if (lookupBlock.Room != room)
                        SmartBuildGeometry(lookupBlock.Room, new RectangleInt2(lookupBlock.Pos, lookupBlock.Pos));
                }

            SmartBuildGeometry(room, area);
        }

        public static void ResetObjectRotation(RotationAxis axis = RotationAxis.None)
        {
            if (_editor.SelectedObject is IRotateableYX)
            {
                if (axis == RotationAxis.X || axis == RotationAxis.None)
                    (_editor.SelectedObject as IRotateableYX).RotationX = 0;
            }

            if (_editor.SelectedObject is IRotateableY)
            {
                if (axis == RotationAxis.Y || axis == RotationAxis.None)
                    (_editor.SelectedObject as IRotateableY).RotationY = 0;
            }

            if (_editor.SelectedObject is IRotateableYXRoll)
            {
                if (axis == RotationAxis.Roll || axis == RotationAxis.None)
                    (_editor.SelectedObject as IRotateableYXRoll).Roll = 0;
            }

            _editor.ObjectChange(_editor.SelectedObject, ObjectChangeType.Change);
        }

        public static void SmoothSector(Room room, int x, int z, BlockVertical vertical)
        {
            var currBlock = room.GetBlockTryThroughPortal(x, z);

            if (currBlock.Room != room ||
                vertical.IsOnFloor() && currBlock.Block.Floor.DiagonalSplit != DiagonalSplit.None ||
                vertical.IsOnCeiling() && currBlock.Block.Ceiling.DiagonalSplit != DiagonalSplit.None)
                return;

            Room.RoomBlockPair[] lookupBlocks = new Room.RoomBlockPair[8]
            {
                room.GetBlockTryThroughPortal(x - 1, z + 1),
                room.GetBlockTryThroughPortal(x, z + 1),
                room.GetBlockTryThroughPortal(x + 1, z + 1),
                room.GetBlockTryThroughPortal(x + 1, z),
                room.GetBlockTryThroughPortal(x + 1, z - 1),
                room.GetBlockTryThroughPortal(x, z - 1),
                room.GetBlockTryThroughPortal(x - 1, z - 1),
                room.GetBlockTryThroughPortal(x - 1, z)
            };

            int[] adj = new int[8];
            for (int i = 0; i < 8; i++)
                adj[i] = (currBlock.Room != null ? currBlock.Room.Position.Y : 0) - (lookupBlocks[i].Room != null ? lookupBlocks[i].Room.Position.Y : 0);

            int validBlockCntXnZp = (lookupBlocks[7].Room != null ? 1 : 0) + (lookupBlocks[0].Room != null ? 1 : 0) + (lookupBlocks[1].Room != null ? 1 : 0);
            int newXnZp = ((lookupBlocks[7].Block?.GetHeight(vertical, BlockEdge.XpZp) ?? 0) + adj[7] +
                                   (lookupBlocks[0].Block?.GetHeight(vertical, BlockEdge.XpZn) ?? 0) + adj[0] +
                                   (lookupBlocks[1].Block?.GetHeight(vertical, BlockEdge.XnZn) ?? 0) + adj[1]) / validBlockCntXnZp;

            int validBlockCntXpZp = (lookupBlocks[1].Room != null ? 1 : 0) + (lookupBlocks[2].Room != null ? 1 : 0) + (lookupBlocks[3].Room != null ? 1 : 0);
            int newXpZp = ((lookupBlocks[1].Block?.GetHeight(vertical, BlockEdge.XpZn) ?? 0) + adj[2] +
                                   (lookupBlocks[2].Block?.GetHeight(vertical, BlockEdge.XnZn) ?? 0) + adj[3] +
                                   (lookupBlocks[3].Block?.GetHeight(vertical, BlockEdge.XnZp) ?? 0) + adj[0]) / validBlockCntXpZp;

            int validBlockCntXpZn = (lookupBlocks[3].Room != null ? 1 : 0) + (lookupBlocks[4].Room != null ? 1 : 0) + (lookupBlocks[5].Room != null ? 1 : 0);
            int newXpZn = ((lookupBlocks[3].Block?.GetHeight(vertical, BlockEdge.XnZn) ?? 0) + adj[3] +
                                   (lookupBlocks[4].Block?.GetHeight(vertical, BlockEdge.XnZp) ?? 0) + adj[0] +
                                   (lookupBlocks[5].Block?.GetHeight(vertical, BlockEdge.XpZp) ?? 0) + adj[1]) / validBlockCntXpZn;

            int validBlockCntXnZn = (lookupBlocks[5].Room != null ? 1 : 0) + (lookupBlocks[6].Room != null ? 1 : 0) + (lookupBlocks[7].Room != null ? 1 : 0);
            int newXnZn = ((lookupBlocks[5].Block?.GetHeight(vertical, BlockEdge.XnZp) ?? 0) + adj[0] +
                                   (lookupBlocks[6].Block?.GetHeight(vertical, BlockEdge.XpZp) ?? 0) + adj[1] +
                                   (lookupBlocks[7].Block?.GetHeight(vertical, BlockEdge.XpZn) ?? 0) + adj[2]) / validBlockCntXnZn;

            currBlock.Block.ChangeHeight(vertical, BlockEdge.XnZp, Math.Sign(newXnZp - currBlock.Block.GetHeight(vertical, BlockEdge.XnZp)));
            currBlock.Block.ChangeHeight(vertical, BlockEdge.XpZp, Math.Sign(newXpZp - currBlock.Block.GetHeight(vertical, BlockEdge.XpZp)));
            currBlock.Block.ChangeHeight(vertical, BlockEdge.XpZn, Math.Sign(newXpZn - currBlock.Block.GetHeight(vertical, BlockEdge.XpZn)));
            currBlock.Block.ChangeHeight(vertical, BlockEdge.XnZn, Math.Sign(newXnZn - currBlock.Block.GetHeight(vertical, BlockEdge.XnZn)));

            SmartBuildGeometry(room, new RectangleInt2(x, z, x, z));
        }

        public static void ShapeGroup(Room room, RectangleInt2 area, ArrowType arrow, EditorToolType type, BlockVertical vertical, double heightScale, bool precise, bool stepped)
        {
            if (precise)
                heightScale /= 4;

            bool linearShape = type <= EditorToolType.HalfPipe;
            bool uniformShape = type >= EditorToolType.HalfPipe;
            bool step90 = arrow <= ArrowType.EdgeW;
            bool turn90 = arrow == ArrowType.EdgeW || arrow == ArrowType.EdgeE;
            bool reverseX = (arrow == ArrowType.EdgeW || arrow == ArrowType.CornerSW || arrow == ArrowType.CornerNW) ^ uniformShape;
            bool reverseZ = (arrow == ArrowType.EdgeS || arrow == ArrowType.CornerSW || arrow == ArrowType.CornerSE) ^ uniformShape;
            bool uniformAlign = arrow != ArrowType.EntireFace && type > EditorToolType.HalfPipe && step90;

            double sizeX = area.Width + (stepped ? 0 : 1);
            double sizeZ = area.Height + (stepped ? 0 : 1);
            double grainBias = uniformShape ? (!step90 ? 0 : 1) : 0;
            double grainX = (1 + grainBias) / sizeX / (uniformAlign && turn90 ? 2 : 1);
            double grainZ = (1 + grainBias) / sizeZ / (uniformAlign && !turn90 ? 2 : 1);

            for (int w = area.X0, x = 0; w < area.X0 + sizeX + 1; w++, x++)
                for (int h = area.Y0, z = 0; h != area.Y0 + sizeZ + 1; h++, z++)
                {
                    double currentHeight;
                    double currX = linearShape && !turn90 && step90 ? 0 : grainX * (reverseX ? sizeX - x : x) - (uniformAlign && turn90 ? 0 : grainBias);
                    double currZ = linearShape && turn90 && step90 ? 0 : grainZ * (reverseZ ? sizeZ - z : z) - (uniformAlign && !turn90 ? 0 : grainBias);

                    switch (type)
                    {
                        case EditorToolType.Ramp:
                            currentHeight = currX + currZ;
                            break;
                        case EditorToolType.Pyramid:
                            currentHeight = 1 - Math.Max(Math.Abs(currX), Math.Abs(currZ));
                            break;
                        default:
                            currentHeight = Math.Sqrt(1 - Math.Pow(currX, 2) - Math.Pow(currZ, 2));
                            currentHeight = double.IsNaN(currentHeight) ? 0 : currentHeight;
                            if (type == EditorToolType.QuarterPipe)
                                currentHeight = 1 - currentHeight;
                            break;
                    }
                    currentHeight = Math.Round(currentHeight * heightScale);

                    if (stepped)
                    {
                        room.Blocks[w, h].Raise(vertical, false, (int)currentHeight);
                        room.Blocks[w, h].FixHeights();
                    }
                    else
                        room.ModifyHeightThroughPortal(w, h, vertical, (int)currentHeight, area);
                }
            SmartBuildGeometry(room, area);
        }

        public static void ApplyHeightmap(Room room, RectangleInt2 area, ArrowType arrow, BlockVertical vertical, float[,] heightmap, float heightScale, bool precise, bool raw)
        {
            if (precise)
                heightScale /= 4;

            bool allFace = arrow == ArrowType.EntireFace;
            bool step90 = arrow <= ArrowType.EdgeW;
            bool turn90 = arrow == ArrowType.EdgeW || arrow == ArrowType.EdgeE;
            bool reverseX = arrow == ArrowType.EdgeW || arrow == ArrowType.CornerSW || arrow == ArrowType.CornerNW;
            bool reverseZ = arrow == ArrowType.EdgeS || arrow == ArrowType.CornerSW || arrow == ArrowType.CornerSE;

            float smoothGrainX = (float)(allFace || step90 && !turn90 ? Math.PI : Math.PI * 0.5f) / (area.Width + 1);
            float smoothGrainZ = (float)(allFace || step90 && turn90 ? Math.PI : Math.PI * 0.5f) / (area.Height + 1);

            for (int w = area.X0, x = 0; w < area.X1 + 2; w++, x++)
                for (int h = area.Y0, z = 0; h != area.Y1 + 2; h++, z++)
                {
                    var smoothFactor = raw ? 1 : Math.Sin(smoothGrainX * (reverseX ? area.Width + 1 - x : x)) * Math.Sin(smoothGrainZ * (reverseZ ? area.Height + 1 - z : z));

                    int currX = x * (heightmap.GetLength(0) / (area.Width + 2));
                    int currZ = z * (heightmap.GetLength(1) / (area.Height + 2));
                    room.ModifyHeightThroughPortal(w, h, vertical, (int)Math.Round(heightmap[currX, currZ] * smoothFactor * heightScale), area);
                }
            SmartBuildGeometry(room, area);
        }

        public static void FlipFloorSplit(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                    if (!room.Blocks[x, z].Floor.IsQuad && room.Blocks[x, z].Floor.DiagonalSplit == DiagonalSplit.None)
                        room.Blocks[x, z].Floor.SplitDirectionToggled = !room.Blocks[x, z].Floor.SplitDirectionToggled;

            SmartBuildGeometry(room, area);
        }

        public static void FlipCeilingSplit(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                    if (!room.Blocks[x, z].Ceiling.IsQuad && room.Blocks[x, z].Ceiling.DiagonalSplit == DiagonalSplit.None)
                        room.Blocks[x, z].Ceiling.SplitDirectionToggled = !room.Blocks[x, z].Ceiling.SplitDirectionToggled;

            SmartBuildGeometry(room, area);
        }

        public static void AddTrigger(Room room, RectangleInt2 area, IWin32Window owner)
        {
            // Allow creating a trigger using the bookmarked object if shift was pressed.
            ObjectInstance @object;
            if (Control.ModifierKeys.HasFlag(Keys.Shift))
                @object = _editor.BookmarkedObject;
            else
                @object = _editor.SelectedObject;


            // Initialize trigger with selected object if the selected object makes sense in the trigger context.
            var trigger = new TriggerInstance(area);
            if (@object is MoveableInstance)
            {
                trigger.TargetType = TriggerTargetType.Object;
                trigger.Target = @object;
            }
            else if (@object is FlybyCameraInstance)
            {
                trigger.TargetType = TriggerTargetType.FlyByCamera;
                trigger.Target = @object;
            }
            else if (@object is CameraInstance)
            {
                trigger.TargetType = TriggerTargetType.Camera;
                trigger.Target = @object;
            }
            else if (@object is SinkInstance)
            {
                trigger.TargetType = TriggerTargetType.Sink;
                trigger.Target = @object;
            }
            else if (@object is StaticInstance && _editor.Level.Settings.GameVersion == GameVersion.TRNG)
            {
                trigger.TargetType = TriggerTargetType.FlipEffect;
                trigger.Target = new TriggerParameterUshort(160);
                trigger.Timer = @object;
            }

            // Display form
            using (var formTrigger = new FormTrigger(_editor.Level, trigger, obj => _editor.ShowObject(obj),
                                                     r => _editor.SelectRoomAndResetCamera(r)))
            {
                if (formTrigger.ShowDialog(owner) != DialogResult.OK)
                    return;
            }
            room.AddObject(_editor.Level, trigger);
            _editor.ObjectChange(trigger, ObjectChangeType.Add);
            _editor.RoomSectorPropertiesChange(room);

            if (_editor.Configuration.Editor_AutoSwitchSectorColoringInfo)
                _editor.SectorColoringManager.SetPriority(SectorColoringType.Trigger);
        }

        public static Vector3 GetMovementPrecision(Keys modifierKeys)
        {
            if (modifierKeys.HasFlag(Keys.Control))
                return new Vector3(0.0f);
            if (modifierKeys.HasFlag(Keys.Shift))
                return new Vector3(64.0f);
            return new Vector3(512.0f, 128.0f, 512.0f);
        }

        public static void ScaleObject(IScaleable instance, float newScale, double quantization)
        {
            if (quantization != 0.0f)
            {
                double logScale = Math.Log(newScale);
                double logQuantization = Math.Log(quantization);
                logScale = Math.Round(logScale / logQuantization) * logQuantization;
                newScale = (float)Math.Exp(logScale);
            }
            // Set some limits to scale
            // TODO: object risks to be too small and to be not pickable. We should add some size check
            if (newScale < 1 / 32.0f)
                newScale = 1 / 32.0f;
            if (newScale > 128.0f)
                newScale = 128.0f;
            instance.Scale = newScale;

            _editor.ObjectChange(_editor.SelectedObject, ObjectChangeType.Change);
        }

        public static void MoveObject(PositionBasedObjectInstance instance, Vector3 pos, Keys modifierKeys)
        {
            MoveObject(instance, pos, GetMovementPrecision(modifierKeys), modifierKeys.HasFlag(Keys.Alt));
        }

        public static void MoveObject(PositionBasedObjectInstance instance, Vector3 pos, Vector3 precision = new Vector3(), bool canGoOutsideRoom = false)
        {
            if (instance == null)
                return;

            // Limit movement precision
            if (precision.X > 0.0f)
                pos.X = (float)Math.Round(pos.X / precision.X) * precision.X;
            if (precision.Y > 0.0f)
                pos.Y = (float)Math.Round(pos.Y / precision.Y) * precision.Y;
            if (precision.Z > 0.0f)
                pos.Z = (float)Math.Round(pos.Z / precision.Z) * precision.Z;

            // Limit movement area
            if (!canGoOutsideRoom)
            {
                float x = (float)Math.Floor(pos.X / 1024.0f);
                float z = (float)Math.Floor(pos.Z / 1024.0f);

                if (x < 0.0f || x > instance.Room.NumXSectors - 1 ||
                    z < 0.0f || z > instance.Room.NumZSectors - 1)
                    return;

                Block block = instance.Room.Blocks[(int)x, (int)z];
                if (block.IsAnyWall)
                    return;
            }

            // Update position
            instance.Position = pos;

            // Update state
            if (instance is LightInstance)
                instance.Room.RoomGeometry?.Relight(instance.Room);
            _editor.ObjectChange(instance, ObjectChangeType.Change);
        }

        public static void MoveObjectRelative(PositionBasedObjectInstance instance, Vector3 pos, Vector3 precision = new Vector3(), bool canGoOutsideRoom = false)
        {
            MoveObject(instance, instance.Position + pos, precision, canGoOutsideRoom);
        }

        public enum RotationAxis
        {
            Y,
            X,
            Roll,
            None
        }

        public static void RotateObject(ObjectInstance instance, RotationAxis axis, float angleInDegrees, float quantization = 0.0f, bool delta = true)
        {
            if (quantization != 0.0f)
                angleInDegrees = (float)(Math.Round(angleInDegrees / quantization) * quantization);

            switch (axis)
            {
                case RotationAxis.Y:
                    var rotateableY = instance as IRotateableY;
                    if (rotateableY == null)
                        return;
                    rotateableY.RotationY = angleInDegrees + (delta ? rotateableY.RotationY : 0);
                    break;
                case RotationAxis.X:
                    var rotateableX = instance as IRotateableYX;
                    if (rotateableX == null)
                        return;
                    rotateableX.RotationX = angleInDegrees + (delta ? rotateableX.RotationX : 0);
                    break;
                case RotationAxis.Roll:
                    var rotateableRoll = instance as IRotateableYXRoll;
                    if (rotateableRoll == null)
                        return;
                    rotateableRoll.Roll = angleInDegrees + (delta ? rotateableRoll.Roll : 0);
                    break;
            }
            if (instance is LightInstance)
                instance.Room.BuildGeometry();
            _editor.ObjectChange(instance, ObjectChangeType.Change);
        }

        public static void EditObject(ObjectInstance instance, IWin32Window owner)
        {
            if (instance is MoveableInstance)
            {
                using (var formMoveable = new FormMoveable((MoveableInstance)instance))
                    if (formMoveable.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is StaticInstance)
            {
                using (var formStaticMesh = new FormStaticMesh((StaticInstance)instance))
                    if (formStaticMesh.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is FlybyCameraInstance)
            {
                using (var formFlyby = new FormFlybyCamera((FlybyCameraInstance)instance))
                    if (formFlyby.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is CameraInstance)
            {
                using (var formCamera = new FormCamera((CameraInstance)instance))
                    if (formCamera.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is SinkInstance)
            {
                using (var formSink = new FormSink((SinkInstance)instance))
                    if (formSink.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is SoundSourceInstance)
            {
                using (var formSoundSource = new FormSoundSource((SoundSourceInstance)instance, _editor.Level.Settings.WadGetAllSoundInfos()))
                    if (formSoundSource.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is TriggerInstance)
            {
                using (var formTrigger = new FormTrigger(_editor.Level, (TriggerInstance)instance, obj => _editor.ShowObject(obj),
                                                         r => _editor.SelectRoomAndResetCamera(r)))
                    if (formTrigger.ShowDialog(owner) != DialogResult.OK)
                        return;
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
            else if (instance is ImportedGeometryInstance)
            {
                using (var formImportedGeometry = new FormImportedGeometry((ImportedGeometryInstance)instance, _editor.Level.Settings))
                {
                    if (formImportedGeometry.ShowDialog(owner) != DialogResult.OK)
                        return;
                    _editor.UpdateLevelSettings(formImportedGeometry.NewLevelSettings);
                }
                _editor.ObjectChange(instance, ObjectChangeType.Change);
            }
        }

        public static void PasteObject(VectorInt2 pos)
        {
            ObjectClipboardData data = Clipboard.GetDataObject().GetData(typeof(ObjectClipboardData)) as ObjectClipboardData;
            if (data == null)
                _editor.SendMessage("Clipboard contains no object data.", PopupType.Error);
            else
                PlaceObject(_editor.SelectedRoom, pos, data.MergeGetSingleObject(_editor));
        }

        public static void DeleteObjectWithWarning(ObjectInstance instance, IWin32Window owner)
        {
            if (DarkMessageBox.Show(owner, "Do you really want to delete " + instance + "?",
                    "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            DeleteObject(instance);
        }

        public static void DeleteObject(ObjectInstance instance)
        {
            var room = instance.Room;
            var adjoiningRoom = (instance as PortalInstance)?.AdjoiningRoom;
            var isTriggerableObject = instance is MoveableInstance || instance is StaticInstance || instance is CameraInstance ||
                                      instance is FlybyCameraInstance || instance is SinkInstance || instance is SoundSourceInstance;
            room.RemoveObject(_editor.Level, instance);

            // Delete trigger if is necessary
            if (isTriggerableObject)
            {
                var triggersToRemove = new List<TriggerInstance>();
                foreach (var r in _editor.Level.Rooms)
                    if (r != null)
                        foreach (var trigger in r.Triggers)
                        {
                            if (trigger.Target == instance)
                                triggersToRemove.Add(trigger);
                        }

                foreach (var t in triggersToRemove)
                    t.Room.RemoveObject(_editor.Level, t);
            }

            // Additional updates
            if (instance is SectorBasedObjectInstance)
                _editor.RoomSectorPropertiesChange(room);
            if (instance is LightInstance)
                room.BuildGeometry();
            if (instance is PortalInstance)
            {
                room.BuildGeometry();
                adjoiningRoom?.BuildGeometry();
                room.AlternateOpposite?.BuildGeometry();
                adjoiningRoom?.AlternateOpposite?.BuildGeometry();
            }

            // Avoid having the removed object still selected
            _editor.ObjectChange(instance, ObjectChangeType.Remove, room);
        }

        public static void RotateTexture(Room room, VectorInt2 pos, BlockFace face)
        {
            Block blocks = room.GetBlock(pos);
            TextureArea textureArea = blocks.GetFaceTexture(face);
            if (room.GetFaceShape(pos.X, pos.Y, face) == Block.FaceShape.Triangle)
            {
                Vector2 tempTexCoord = textureArea.TexCoord2;
                textureArea.TexCoord2 = textureArea.TexCoord1;
                textureArea.TexCoord1 = textureArea.TexCoord0;
                textureArea.TexCoord0 = tempTexCoord;
                textureArea.TexCoord3 = textureArea.TexCoord2;
            }
            else
            {
                Vector2 tempTexCoord = textureArea.TexCoord3;
                textureArea.TexCoord3 = textureArea.TexCoord2;
                textureArea.TexCoord2 = textureArea.TexCoord1;
                textureArea.TexCoord1 = textureArea.TexCoord0;
                textureArea.TexCoord0 = tempTexCoord;
            }
            blocks.SetFaceTexture(face, textureArea);

            // Update state
            room.BuildGeometry();
            _editor.RoomTextureChange(room);
        }

        public static void MirrorTexture(Room room, VectorInt2 pos, BlockFace face)
        {
            Block blocks = room.GetBlock(pos);
            TextureArea textureArea = blocks.GetFaceTexture(face);
            if (room.GetFaceShape(pos.X, pos.Y, face) == Block.FaceShape.Triangle)
            {
                Swap.Do(ref textureArea.TexCoord0, ref textureArea.TexCoord2);
                textureArea.TexCoord3 = textureArea.TexCoord2;
            }
            else
            {
                Swap.Do(ref textureArea.TexCoord0, ref textureArea.TexCoord1);
                Swap.Do(ref textureArea.TexCoord2, ref textureArea.TexCoord3);
            }
            blocks.SetFaceTexture(face, textureArea);

            // Update state
            room.BuildGeometry();
            _editor.RoomTextureChange(room);
        }

        public static void PickTexture(Room room, VectorInt2 pos, BlockFace face)
        {
            var area = room.GetBlock(pos).GetFaceTexture(face);
            if (area == null || area.TextureIsInvisble || area.Texture == null)
                return;
            _editor.SelectTextureAndCenterView(area);
        }

        public static void RotateSelectedTexture()
        {
            TextureArea textureArea = _editor.SelectedTexture;
            Vector2 texCoordTemp = textureArea.TexCoord3;
            textureArea.TexCoord3 = textureArea.TexCoord2;
            textureArea.TexCoord2 = textureArea.TexCoord1;
            textureArea.TexCoord1 = textureArea.TexCoord0;
            textureArea.TexCoord0 = texCoordTemp;
            _editor.SelectedTexture = textureArea;
        }

        public static void MirrorSelectedTexture()
        {
            TextureArea textureArea = _editor.SelectedTexture;
            Swap.Do(ref textureArea.TexCoord0, ref textureArea.TexCoord3);
            Swap.Do(ref textureArea.TexCoord1, ref textureArea.TexCoord2);
            _editor.SelectedTexture = textureArea;
        }

        private static bool ApplyTextureAutomaticallyNoUpdated(Room room, VectorInt2 pos, BlockFace face, TextureArea texture)
        {
            Block block = room.GetBlock(pos);

            TextureArea processedTexture = texture;

            if (_editor.Tool.TextureUVFixer && texture.TextureIsRectangle)
            {
                switch (face)
                {
                    case BlockFace.Floor:
                        if (!block.Floor.IsQuad)
                        {
                            if (block.Floor.SplitDirectionIsXEqualsZ)
                            {
                                Swap.Do(ref processedTexture.TexCoord0, ref processedTexture.TexCoord2);
                                processedTexture.TexCoord1 = processedTexture.TexCoord3;
                                processedTexture.TexCoord3 = processedTexture.TexCoord2;
                            }
                            else
                            {
                                processedTexture.TexCoord0 = processedTexture.TexCoord1;
                                processedTexture.TexCoord1 = processedTexture.TexCoord2;
                                processedTexture.TexCoord2 = processedTexture.TexCoord3;
                            }
                        }
                        break;

                    case BlockFace.FloorTriangle2:
                        if (block.Floor.IsQuad)
                            break;
                        else
                        {
                            if (block.Floor.SplitDirectionIsXEqualsZ)
                                processedTexture.TexCoord3 = processedTexture.TexCoord0;
                            else
                            {
                                processedTexture.TexCoord2 = processedTexture.TexCoord1;
                                processedTexture.TexCoord1 = processedTexture.TexCoord0;
                                processedTexture.TexCoord0 = processedTexture.TexCoord3;
                                processedTexture.TexCoord3 = processedTexture.TexCoord2;
                            }
                        }
                        break;


                    case BlockFace.Ceiling:
                        if (block.Ceiling.IsQuad)
                        {
                            Swap.Do(ref processedTexture.TexCoord0, ref processedTexture.TexCoord1);
                            Swap.Do(ref processedTexture.TexCoord2, ref processedTexture.TexCoord3);
                            break;
                        }
                        else
                        {
                            if (block.Ceiling.SplitDirectionIsXEqualsZ)
                            {
                                Swap.Do(ref processedTexture.TexCoord0, ref processedTexture.TexCoord2);
                                processedTexture.TexCoord1 = processedTexture.TexCoord3;
                                processedTexture.TexCoord3 = processedTexture.TexCoord2;
                            }
                            else
                            {
                                processedTexture.TexCoord0 = processedTexture.TexCoord1;
                                processedTexture.TexCoord1 = processedTexture.TexCoord2;
                                processedTexture.TexCoord2 = processedTexture.TexCoord3;
                            }
                        }
                        break;


                    case BlockFace.CeilingTriangle2:
                        if (!block.Ceiling.IsQuad)
                        {
                            if (block.Ceiling.SplitDirectionIsXEqualsZ)
                            {
                                processedTexture.TexCoord3 = processedTexture.TexCoord0;
                            }
                            else
                            {
                                processedTexture.TexCoord2 = processedTexture.TexCoord1;
                                processedTexture.TexCoord1 = processedTexture.TexCoord0;
                                processedTexture.TexCoord0 = processedTexture.TexCoord3;
                                processedTexture.TexCoord3 = processedTexture.TexCoord2;
                            }
                        }
                        break;

                    default:
                        // This kind of correspondence is really fragile, I am not sure what to do, -TRTombLevBauer
                        /*if (room.RoomGeometry != null)
                        {
                            VertexRange vertexRange = room.RoomGeometry.VertexRangeLookup[new VertexRangeKey(pos.X, pos.Y, face)];
                            if (indices.Count == 4)
                            {
                                float maxUp = Math.Max(vertices[indices[0]].Position.Y, vertices[indices[1]].Position.Y);
                                float minDown = Math.Min(vertices[indices[3]].Position.Y, vertices[indices[2]].Position.Y);

                                float difference = maxUp - minDown;

                                float delta0 = (minDown - vertices[indices[3]].Position.Y) / difference;
                                float delta1 = (maxUp - vertices[indices[0]].Position.Y) / difference;
                                float delta2 = (maxUp - vertices[indices[1]].Position.Y) / difference;
                                float delta3 = (minDown - vertices[indices[2]].Position.Y) / difference;

                                if (texture.TexCoord0.X == texture.TexCoord1.X && texture.TexCoord3.X == texture.TexCoord2.X)
                                {
                                    processedTexture.TexCoord0.Y += (texture.TexCoord0.Y - texture.TexCoord1.Y) * delta0;
                                    processedTexture.TexCoord1.Y += (texture.TexCoord0.Y - texture.TexCoord1.Y) * delta1;
                                    processedTexture.TexCoord2.Y += (texture.TexCoord3.Y - texture.TexCoord2.Y) * delta2;
                                    processedTexture.TexCoord3.Y += (texture.TexCoord3.Y - texture.TexCoord2.Y) * delta3;
                                }
                                else
                                {
                                    processedTexture.TexCoord0.X += (texture.TexCoord0.X - texture.TexCoord1.X) * delta0;
                                    processedTexture.TexCoord1.X += (texture.TexCoord0.X - texture.TexCoord1.X) * delta1;
                                    processedTexture.TexCoord2.X += (texture.TexCoord3.X - texture.TexCoord2.X) * delta2;
                                    processedTexture.TexCoord3.X += (texture.TexCoord3.X - texture.TexCoord2.X) * delta3;
                                }
                            }
                            else
                            {
                                float maxUp = Math.Max(Math.Max(vertices[indices[0]].Position.Y, vertices[indices[1]].Position.Y), vertices[indices[2]].Position.Y);
                                float minDown = Math.Min(Math.Min(vertices[indices[0]].Position.Y, vertices[indices[1]].Position.Y), vertices[indices[2]].Position.Y);
                                float difference = maxUp - minDown;

                                if (vertices[indices[0]].Position.X == vertices[indices[2]].Position.X && vertices[indices[0]].Position.Z == vertices[indices[2]].Position.Z)
                                {
                                    float delta0 = (minDown - vertices[indices[2]].Position.Y) / difference;
                                    float delta1 = (maxUp - vertices[indices[0]].Position.Y) / difference;
                                    float delta2 = (maxUp - vertices[indices[1]].Position.Y) / difference;
                                    float delta3 = (minDown - vertices[indices[1]].Position.Y) / difference;

                                    if (texture.TexCoord0.X == texture.TexCoord1.X && texture.TexCoord3.X == texture.TexCoord2.X)
                                    {
                                        processedTexture.TexCoord0.Y += (texture.TexCoord0.Y - texture.TexCoord1.Y) * delta0;
                                        processedTexture.TexCoord1.Y += (texture.TexCoord0.Y - texture.TexCoord1.Y) * delta1;
                                        processedTexture.TexCoord2.Y += (texture.TexCoord3.Y - texture.TexCoord2.Y) * delta2;
                                        processedTexture.TexCoord3.Y += (texture.TexCoord3.Y - texture.TexCoord2.Y) * delta3;
                                    }
                                    else
                                    {
                                        processedTexture.TexCoord0.X += (texture.TexCoord0.X - texture.TexCoord1.X) * delta0;
                                        processedTexture.TexCoord1.X += (texture.TexCoord0.X - texture.TexCoord1.X) * delta1;
                                        processedTexture.TexCoord2.X += (texture.TexCoord3.X - texture.TexCoord2.X) * delta2;
                                        processedTexture.TexCoord3.X += (texture.TexCoord3.X - texture.TexCoord2.X) * delta3;
                                    }

                                    processedTexture.TexCoord3 = processedTexture.TexCoord0;
                                    processedTexture.TexCoord0 = processedTexture.TexCoord1;
                                    processedTexture.TexCoord1 = processedTexture.TexCoord2;
                                    processedTexture.TexCoord2 = processedTexture.TexCoord3;

                                }
                                else
                                {
                                    float delta0 = (minDown - vertices[indices[0]].Position.Y) / difference;
                                    float delta1 = (maxUp - vertices[indices[0]].Position.Y) / difference;
                                    float delta2 = (maxUp - vertices[indices[1]].Position.Y) / difference;
                                    float delta3 = (minDown - vertices[indices[2]].Position.Y) / difference;

                                    if (texture.TexCoord0.X == texture.TexCoord1.X && texture.TexCoord3.X == texture.TexCoord2.X)
                                    {
                                        processedTexture.TexCoord0.Y += (texture.TexCoord0.Y - texture.TexCoord1.Y) * delta0;
                                        processedTexture.TexCoord1.Y += (texture.TexCoord0.Y - texture.TexCoord1.Y) * delta1;
                                        processedTexture.TexCoord2.Y += (texture.TexCoord3.Y - texture.TexCoord2.Y) * delta2;
                                        processedTexture.TexCoord3.Y += (texture.TexCoord3.Y - texture.TexCoord2.Y) * delta3;
                                    }
                                    else
                                    {
                                        processedTexture.TexCoord0.X += (texture.TexCoord0.X - texture.TexCoord1.X) * delta0;
                                        processedTexture.TexCoord1.X += (texture.TexCoord0.X - texture.TexCoord1.X) * delta1;
                                        processedTexture.TexCoord2.X += (texture.TexCoord3.X - texture.TexCoord2.X) * delta2;
                                        processedTexture.TexCoord3.X += (texture.TexCoord3.X - texture.TexCoord2.X) * delta3;
                                    }

                                    processedTexture.TexCoord0 = processedTexture.TexCoord3;

                                    Vector2 tempTexCoord = processedTexture.TexCoord2;
                                    processedTexture.TexCoord2 = processedTexture.TexCoord3;
                                    processedTexture.TexCoord3 = processedTexture.TexCoord0;
                                    processedTexture.TexCoord0 = processedTexture.TexCoord1;
                                    processedTexture.TexCoord1 = tempTexCoord;
                                }
                            }
                        }*/
                        break;
                }
            }
            return block.SetFaceTexture(face, processedTexture);
        }

        public static bool ApplyTextureAutomatically(Room room, VectorInt2 pos, BlockFace face, TextureArea texture)
        {
            var textureApplied = ApplyTextureAutomaticallyNoUpdated(room, pos, face, texture);
            if (textureApplied)
            {
                room.BuildGeometry();
                _editor.RoomTextureChange(room);
            }
            return textureApplied;
        }

        public static Dictionary<BlockFace, float[]> GetFaces(Room room, VectorInt2 pos, Direction direction, BlockFaceType section)
        {
            bool sectionIsWall = room.GetBlockTry(pos.X, pos.Y).IsAnyWall;

            Dictionary<BlockFace, float[]> segments = new Dictionary<BlockFace, float[]>();

            switch (direction)
            {
                case Direction.PositiveZ:
                    if (section == BlockFaceType.Ceiling || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveZ_RF))
                            segments.Add(BlockFace.PositiveZ_RF, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveZ_RF), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveZ_RF) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveZ_WS))
                            segments.Add(BlockFace.PositiveZ_WS, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveZ_WS), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveZ_WS) });
                    }
                    if (section == BlockFaceType.Floor || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveZ_QA))
                            segments.Add(BlockFace.PositiveZ_QA, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveZ_QA), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveZ_QA) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveZ_ED))
                            segments.Add(BlockFace.PositiveZ_ED, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveZ_ED), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveZ_ED) });
                    }
                    if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveZ_Middle))
                        segments.Add(BlockFace.PositiveZ_Middle, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveZ_Middle), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveZ_Middle) });
                    break;

                case Direction.NegativeZ:
                    if (section == BlockFaceType.Ceiling || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeZ_RF))
                            segments.Add(BlockFace.NegativeZ_RF, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeZ_RF), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeZ_RF) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeZ_WS))
                            segments.Add(BlockFace.NegativeZ_WS, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeZ_WS), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeZ_WS) });
                    }
                    if (section == BlockFaceType.Floor || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeZ_QA))
                            segments.Add(BlockFace.NegativeZ_QA, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeZ_QA), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeZ_QA) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeZ_ED))
                            segments.Add(BlockFace.NegativeZ_ED, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeZ_ED), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeZ_ED) });
                    }
                    if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeZ_Middle))
                        segments.Add(BlockFace.NegativeZ_Middle, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeZ_Middle), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeZ_Middle) });
                    break;

                case Direction.PositiveX:
                    if (section == BlockFaceType.Ceiling || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveX_RF))
                            segments.Add(BlockFace.PositiveX_RF, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveX_RF), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveX_RF) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveX_WS))
                            segments.Add(BlockFace.PositiveX_WS, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveX_WS), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveX_WS) });
                    }
                    if (section == BlockFaceType.Floor || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveX_QA))
                            segments.Add(BlockFace.PositiveX_QA, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveX_QA), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveX_QA) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveX_ED))
                            segments.Add(BlockFace.PositiveX_ED, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveX_ED), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveX_ED) });
                    }
                    if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.PositiveX_Middle))
                        segments.Add(BlockFace.PositiveX_Middle, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.PositiveX_Middle), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.PositiveX_Middle) });
                    break;

                case Direction.NegativeX:
                    if (section == BlockFaceType.Ceiling || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeX_RF))
                            segments.Add(BlockFace.NegativeX_RF, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeX_RF), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeX_RF) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeX_WS))
                            segments.Add(BlockFace.NegativeX_WS, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeX_WS), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeX_WS) });
                    }
                    if (section == BlockFaceType.Floor || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeX_QA))
                            segments.Add(BlockFace.NegativeX_QA, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeX_QA), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeX_QA) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeX_ED))
                            segments.Add(BlockFace.NegativeX_ED, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeX_ED), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeX_ED) });
                    }
                    if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.NegativeX_Middle))
                        segments.Add(BlockFace.NegativeX_Middle, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.NegativeX_Middle), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.NegativeX_Middle) });
                    break;

                case Direction.Diagonal:
                    if (section == BlockFaceType.Ceiling || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.DiagonalRF))
                            segments.Add(BlockFace.DiagonalRF, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.DiagonalRF), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.DiagonalRF) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.DiagonalWS))
                            segments.Add(BlockFace.DiagonalWS, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.DiagonalWS), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.DiagonalWS) });
                    }
                    if (section == BlockFaceType.Floor || sectionIsWall)
                    {
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.DiagonalQA))
                            segments.Add(BlockFace.DiagonalQA, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.DiagonalQA), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.DiagonalQA) });
                        if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.DiagonalED))
                            segments.Add(BlockFace.DiagonalED, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.DiagonalED), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.DiagonalED) });
                    }
                    if (room.IsFaceDefined(pos.X, pos.Y, BlockFace.DiagonalMiddle))
                        segments.Add(BlockFace.DiagonalMiddle, new float[2] { room.GetFaceHighestPoint(pos.X, pos.Y, BlockFace.DiagonalMiddle), room.GetFaceLowestPoint(pos.X, pos.Y, BlockFace.DiagonalMiddle) });
                    break;
            }

            return segments;
        }

        private static float[] GetAreaExtremums(Room room, RectangleInt2 area, Direction direction, BlockFaceType type)
        {
            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;

            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    var segments = GetFaces(room, new VectorInt2(x, z), direction, type);

                    foreach (var segment in segments)
                    {
                        minHeight = Math.Min(minHeight, segment.Value[1]);
                        maxHeight = Math.Max(maxHeight, segment.Value[0]);
                    }
                }

            return new float[2] { minHeight, maxHeight };
        }

        public static void TexturizeWallSection(Room room, VectorInt2 pos, Direction direction, BlockFaceType section, TextureArea texture, int subdivisions = 0, int iteration = 0, float[] overrideHeights = null)
        {
            if (subdivisions < 0 || iteration < 0)
                subdivisions = 0;

            if (overrideHeights?.Count() != 2)
                overrideHeights = null;

            var segments = GetFaces(room, pos, direction, section);

            var processedTexture = texture;
            float minSectionHeight = float.MaxValue;
            float maxSectionHeight = float.MinValue;

            if (overrideHeights == null)
            {
                foreach (var segment in segments)
                {
                    minSectionHeight = Math.Min(minSectionHeight, segment.Value[1]);
                    maxSectionHeight = Math.Max(maxSectionHeight, segment.Value[0]);
                }
            }
            else
            {
                minSectionHeight = overrideHeights[0];
                maxSectionHeight = overrideHeights[1];
            }

            float sectionHeight = maxSectionHeight - minSectionHeight;
            bool inverted = false;

            foreach (var segment in segments)
            {
                float currentHighestPoint = Math.Abs(segment.Value[0] - maxSectionHeight) / sectionHeight;
                float currentLowestPoint = Math.Abs(maxSectionHeight - segment.Value[1]) / sectionHeight;

                if (texture.TexCoord0.X == texture.TexCoord1.X && texture.TexCoord3.X == texture.TexCoord2.X)
                {
                    float textureHeight = texture.TexCoord0.Y - texture.TexCoord1.Y;

                    processedTexture.TexCoord0.Y = texture.TexCoord1.Y + textureHeight * currentLowestPoint;
                    processedTexture.TexCoord3.Y = texture.TexCoord2.Y + textureHeight * currentLowestPoint;
                    processedTexture.TexCoord1.Y = texture.TexCoord1.Y + textureHeight * currentHighestPoint;
                    processedTexture.TexCoord2.Y = texture.TexCoord2.Y + textureHeight * currentHighestPoint;

                    if (subdivisions > 0)
                    {
                        float stride = (texture.TexCoord2.X - texture.TexCoord1.X) / (subdivisions + 1);

                        if (inverted == false & (direction == Direction.NegativeX || direction == Direction.PositiveZ))
                        {
                            inverted = true;
                            iteration = subdivisions - iteration;
                        }

                        processedTexture.TexCoord0.X = texture.TexCoord0.X + stride * iteration;
                        processedTexture.TexCoord1.X = texture.TexCoord1.X + stride * iteration;
                        processedTexture.TexCoord3.X = texture.TexCoord3.X - stride * (subdivisions - iteration);
                        processedTexture.TexCoord2.X = texture.TexCoord2.X - stride * (subdivisions - iteration);
                    }

                }
                else
                {
                    float textureWidth = texture.TexCoord3.X - texture.TexCoord2.X;

                    processedTexture.TexCoord3.X = texture.TexCoord2.X + textureWidth * currentLowestPoint;
                    processedTexture.TexCoord0.X = texture.TexCoord1.X + textureWidth * currentLowestPoint;
                    processedTexture.TexCoord2.X = texture.TexCoord2.X + textureWidth * currentHighestPoint;
                    processedTexture.TexCoord1.X = texture.TexCoord1.X + textureWidth * currentHighestPoint;

                    if (subdivisions > 0)
                    {
                        float stride = (texture.TexCoord0.Y - texture.TexCoord3.Y) / (subdivisions + 1);

                        if (inverted == false & (direction == Direction.PositiveX || direction == Direction.NegativeZ))
                        {
                            inverted = true;
                            iteration = subdivisions - iteration;
                        }

                        processedTexture.TexCoord2.Y = texture.TexCoord2.Y + stride * iteration;
                        processedTexture.TexCoord3.Y = texture.TexCoord3.Y + stride * iteration;
                        processedTexture.TexCoord1.Y = texture.TexCoord1.Y - stride * (subdivisions - iteration);
                        processedTexture.TexCoord0.Y = texture.TexCoord0.Y - stride * (subdivisions - iteration);
                    }
                }

                ApplyTextureAutomaticallyNoUpdated(room, pos, segment.Key, processedTexture);
            }
        }

        public static void TexturizeGroup(Room room, SectorSelection selection, TextureArea texture, BlockFace pickedFace, bool subdivideWalls = false, bool unifyHeight = false)
        {
            RectangleInt2 area = selection.Valid ? selection.Area : _editor.SelectedRoom.LocalArea;

            if (pickedFace < BlockFace.Floor)
            {
                int xSubs = subdivideWalls == true ? area.X1 - area.X0 : 0;
                int zSubs = subdivideWalls == true ? area.Y1 - area.Y0 : 0;

                for (int x = area.X0, iterX = 0; x <= area.X1; x++, iterX++)
                    for (int z = area.Y0, iterZ = 0; z <= area.Y1; z++, iterZ++)
                        switch (pickedFace)
                        {
                            case BlockFace.NegativeX_QA:
                            case BlockFace.NegativeX_ED:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.NegativeX, BlockFaceType.Floor, texture, zSubs, iterZ, unifyHeight ? GetAreaExtremums(room, area, Direction.NegativeX, BlockFaceType.Floor) : null);
                                break;

                            case BlockFace.NegativeX_Middle:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.NegativeX, BlockFaceType.Wall, texture, zSubs, iterZ, unifyHeight ? GetAreaExtremums(room, area, Direction.NegativeX, BlockFaceType.Wall) : null);
                                break;

                            case BlockFace.NegativeX_RF:
                            case BlockFace.NegativeX_WS:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.NegativeX, BlockFaceType.Ceiling, texture, zSubs, iterZ, unifyHeight ? GetAreaExtremums(room, area, Direction.NegativeX, BlockFaceType.Ceiling) : null);
                                break;

                            case BlockFace.PositiveX_QA:
                            case BlockFace.PositiveX_ED:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.PositiveX, BlockFaceType.Floor, texture, zSubs, iterZ, unifyHeight ? GetAreaExtremums(room, area, Direction.PositiveX, BlockFaceType.Floor) : null);
                                break;

                            case BlockFace.PositiveX_Middle:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.PositiveX, BlockFaceType.Wall, texture, zSubs, iterZ, unifyHeight ? GetAreaExtremums(room, area, Direction.PositiveX, BlockFaceType.Wall) : null);
                                break;

                            case BlockFace.PositiveX_RF:
                            case BlockFace.PositiveX_WS:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.PositiveX, BlockFaceType.Ceiling, texture, zSubs, iterZ, unifyHeight ? GetAreaExtremums(room, area, Direction.PositiveX, BlockFaceType.Ceiling) : null);
                                break;

                            case BlockFace.NegativeZ_QA:
                            case BlockFace.NegativeZ_ED:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.NegativeZ, BlockFaceType.Floor, texture, xSubs, iterX, unifyHeight ? GetAreaExtremums(room, area, Direction.NegativeZ, BlockFaceType.Floor) : null);
                                break;

                            case BlockFace.NegativeZ_Middle:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.NegativeZ, BlockFaceType.Wall, texture, xSubs, iterX, unifyHeight ? GetAreaExtremums(room, area, Direction.NegativeZ, BlockFaceType.Wall) : null);
                                break;

                            case BlockFace.NegativeZ_RF:
                            case BlockFace.NegativeZ_WS:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.NegativeZ, BlockFaceType.Ceiling, texture, xSubs, iterX, unifyHeight ? GetAreaExtremums(room, area, Direction.NegativeZ, BlockFaceType.Ceiling) : null);
                                break;

                            case BlockFace.PositiveZ_QA:
                            case BlockFace.PositiveZ_ED:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.PositiveZ, BlockFaceType.Floor, texture, xSubs, iterX, unifyHeight ? GetAreaExtremums(room, area, Direction.PositiveZ, BlockFaceType.Floor) : null);
                                break;

                            case BlockFace.PositiveZ_Middle:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.PositiveZ, BlockFaceType.Wall, texture, xSubs, iterX, unifyHeight ? GetAreaExtremums(room, area, Direction.PositiveZ, BlockFaceType.Wall) : null);
                                break;

                            case BlockFace.PositiveZ_RF:
                            case BlockFace.PositiveZ_WS:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.PositiveZ, BlockFaceType.Ceiling, texture, xSubs, iterX, unifyHeight ? GetAreaExtremums(room, area, Direction.PositiveZ, BlockFaceType.Ceiling) : null);
                                break;

                            case BlockFace.DiagonalQA:
                            case BlockFace.DiagonalED:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.Diagonal, BlockFaceType.Floor, texture);
                                break;

                            case BlockFace.DiagonalMiddle:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.Diagonal, BlockFaceType.Wall, texture);
                                break;

                            case BlockFace.DiagonalRF:
                            case BlockFace.DiagonalWS:
                                TexturizeWallSection(room, new VectorInt2(x, z), Direction.Diagonal, BlockFaceType.Ceiling, texture);
                                break;
                        }
            }
            else
            {
                Vector2 verticalUVStride = (texture.TexCoord3 - texture.TexCoord2) / (area.Y1 - area.Y0 + 1);
                Vector2 horizontalUVStride = (texture.TexCoord2 - texture.TexCoord1) / (area.X1 - area.X0 + 1);

                for (int x = area.X0, x1 = 0; x <= area.X1; x++, x1++)
                {
                    Vector2 currentX = horizontalUVStride * x1;

                    for (int z = area.Y0, z1 = 0; z <= area.Y1; z++, z1++)
                    {
                        TextureArea currentTexture = texture;
                        Vector2 currentZ = verticalUVStride * z1;

                        currentTexture.TexCoord0 -= currentZ - currentX;
                        currentTexture.TexCoord1 = currentTexture.TexCoord0 - verticalUVStride;
                        currentTexture.TexCoord3 = currentTexture.TexCoord0 + horizontalUVStride;
                        currentTexture.TexCoord2 = currentTexture.TexCoord0 + horizontalUVStride - verticalUVStride;

                        switch (pickedFace)
                        {
                            case BlockFace.Floor:
                            case BlockFace.FloorTriangle2:
                                ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.Floor, currentTexture);
                                ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.FloorTriangle2, currentTexture);
                                break;

                            case BlockFace.Ceiling:
                            case BlockFace.CeilingTriangle2:
                                ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.Ceiling, currentTexture);
                                ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.CeilingTriangle2, currentTexture);
                                break;
                        }
                    }
                }
            }

            room.BuildGeometry();
            _editor.RoomTextureChange(room);
        }

        public static void TexturizeAll(Room room, SectorSelection selection, TextureArea texture, BlockFaceType type)
        {
            RectangleInt2 area = selection.Valid ? selection.Area : _editor.SelectedRoom.LocalArea;

            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    switch (type)
                    {
                        case BlockFaceType.Floor:
                            ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.Floor, texture);
                            ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.FloorTriangle2, texture);
                            break;

                        case BlockFaceType.Ceiling:
                            ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.Ceiling, texture);
                            ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), BlockFace.CeilingTriangle2, texture);
                            break;

                        case BlockFaceType.Wall:
                            for (BlockFace face = BlockFace.PositiveZ_QA; face <= BlockFace.DiagonalRF; face++)
                                if (room.IsFaceDefined(x, z, face))
                                    ApplyTextureAutomaticallyNoUpdated(room, new VectorInt2(x, z), face, texture);
                            break;
                    }

                }

            room.BuildGeometry();
            _editor.RoomTextureChange(room);
        }

        public static void PlaceObject(Room room, VectorInt2 pos, PositionBasedObjectInstance instance)
        {
            Block block = room.GetBlock(pos);
            int y = (block.Floor.XnZp + block.Floor.XpZp + block.Floor.XpZn + block.Floor.XnZn) / 4;

            instance.Position = new Vector3(pos.X * 1024 + 512, y * 256, pos.Y * 1024 + 512);
            room.AddObject(_editor.Level, instance);
            if (instance is LightInstance)
                room.BuildGeometry(); // Rebuild lighting!
            _editor.ObjectChange(instance, ObjectChangeType.Add);
            _editor.SelectedObject = instance;
        }

        public static void DeleteRooms(IEnumerable<Room> rooms_, IWin32Window owner)
        {
            rooms_ = rooms_.SelectMany(room => room.Versions).Distinct();
            HashSet<Room> rooms = new HashSet<Room>(rooms_);

            // Check if is the last room
            int remainingRoomCount = _editor.Level.Rooms.Count(r => r != null && !rooms.Contains(r) && !rooms.Contains(r.AlternateOpposite));
            if (remainingRoomCount <= 0)
            {
                _editor.SendMessage("You must have at least one room in your level.", PopupType.Error);
                return;
            }

            // Ask for confirmation
            if (DarkMessageBox.Show(owner,
                    "Do you really want to delete rooms? All objects (including portals) inside rooms will be deleted and " +
                    "triggers pointing to them will be removed.",
                    "Delete rooms", MessageBoxButtons.YesNo, MessageBoxIcon.Error) != DialogResult.Yes)
            {
                return;
            }

            // Do it finally
            List<Room> adjoiningRooms = rooms.SelectMany(room => room.Portals)
                .Select(portal => portal.AdjoiningRoom)
                .Distinct()
                .Except(rooms)
                .ToList();
            foreach (Room room in rooms)
                _editor.Level.DeleteAlternateRoom(room);

            // Update selection
            foreach (Room adjoiningRoom in adjoiningRooms)
            {
                adjoiningRoom?.BuildGeometry();
                adjoiningRoom?.AlternateOpposite?.BuildGeometry();
            }
            if (rooms.Contains(_editor.SelectedRoom))
                _editor.SelectRoomAndResetCamera(_editor.Level.Rooms.FirstOrDefault(r => r != null));
            _editor.RoomListChange();
        }

        public static void CropRoom(Room room, RectangleInt2 newArea, IWin32Window owner)
        {
            newArea = newArea.Inflate(1);
            if (newArea.Width + 1 > Room.MaxRoomDimensions || newArea.Height + 1 > Room.MaxRoomDimensions)
            {
                _editor.SendMessage("The selected area exceeds the maximum room size.", PopupType.Error);
                return;
            }
            if (DarkMessageBox.Show(owner, "Warning: if you crop this room, all portals and triggers outside the new area will be deleted." +
                " Do you want to continue?", "Crop room", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // Resize room
            if (room.AlternateOpposite != null)
            {
                room.AlternateOpposite.Resize(_editor.Level, newArea, (short)room.GetLowestCorner(), (short)room.GetHighestCorner());
                room.AlternateOpposite.BuildGeometry();
            }
            room.Resize(_editor.Level, newArea, (short)room.GetLowestCorner(), (short)room.GetHighestCorner());
            room.BuildGeometry();

            // Fix selection if necessary
            if (_editor.SelectedRoom == room && _editor.SelectedSectors.Valid)
            {
                var selection = _editor.SelectedSectors;
                selection.Area = selection.Area.Intersect(newArea) - newArea.Start;
                _editor.SelectedSectors = selection;
            }
            _editor.RoomPropertiesChange(room);
            _editor.RoomSectorPropertiesChange(room);
        }

        public static void SetDiagonalFloorSplit(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;

                    if (room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                    {
                        room.Blocks[x, z].Transform(new RectTransformation { QuadrantRotation = 1 }, true);
                    }
                    else
                    {
                        // Now try to guess the floor split
                        short maxHeight = -32767;
                        byte theCorner = 0;

                        if (room.Blocks[x, z].Floor.XnZp > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XnZp;
                            theCorner = 0;
                        }

                        if (room.Blocks[x, z].Floor.XpZp > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XpZp;
                            theCorner = 1;
                        }

                        if (room.Blocks[x, z].Floor.XpZn > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XpZn;
                            theCorner = 2;
                        }

                        if (room.Blocks[x, z].Floor.XnZn > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XnZn;
                            theCorner = 3;
                        }

                        if (theCorner == 0)
                        {
                            room.Blocks[x, z].Floor.XpZp = maxHeight;
                            room.Blocks[x, z].Floor.XnZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XnZp;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XnZp;
                        }

                        if (theCorner == 1)
                        {
                            room.Blocks[x, z].Floor.XnZp = maxHeight;
                            room.Blocks[x, z].Floor.XpZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XpZp;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XpZp;
                        }

                        if (theCorner == 2)
                        {
                            room.Blocks[x, z].Floor.XpZp = maxHeight;
                            room.Blocks[x, z].Floor.XnZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XpZn;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XpZn;
                        }

                        if (theCorner == 3)
                        {
                            room.Blocks[x, z].Floor.XnZp = maxHeight;
                            room.Blocks[x, z].Floor.XpZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XnZn;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XnZn;
                        }

                        room.Blocks[x, z].Floor.SplitDirectionToggled = false;
                        room.Blocks[x, z].FixHeights();
                    }
                }

            SmartBuildGeometry(room, area);
        }

        public static void SetDiagonalCeilingSplit(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;


                    if (room.Blocks[x, z].Ceiling.DiagonalSplit != DiagonalSplit.None)
                    {
                        room.Blocks[x, z].Transform(new RectTransformation { QuadrantRotation = 1 }, false);
                    }
                    else
                    {
                        // Now try to guess the floor split
                        short minHeight = 32767;
                        byte theCorner = 0;

                        if (room.Blocks[x, z].Ceiling.XnZp < minHeight)
                        {
                            minHeight = room.Blocks[x, z].Ceiling.XnZp;
                            theCorner = 0;
                        }

                        if (room.Blocks[x, z].Ceiling.XpZp < minHeight)
                        {
                            minHeight = room.Blocks[x, z].Ceiling.XpZp;
                            theCorner = 1;
                        }

                        if (room.Blocks[x, z].Ceiling.XpZn < minHeight)
                        {
                            minHeight = room.Blocks[x, z].Ceiling.XpZn;
                            theCorner = 2;
                        }

                        if (room.Blocks[x, z].Ceiling.XnZn < minHeight)
                        {
                            minHeight = room.Blocks[x, z].Ceiling.XnZn;
                            theCorner = 3;
                        }

                        if (theCorner == 0)
                        {
                            room.Blocks[x, z].Ceiling.XpZp = minHeight;
                            room.Blocks[x, z].Ceiling.XnZn = minHeight;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XnZp;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XnZp;
                        }

                        if (theCorner == 1)
                        {
                            room.Blocks[x, z].Ceiling.XnZp = minHeight;
                            room.Blocks[x, z].Ceiling.XpZn = minHeight;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XpZp;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XpZp;
                        }

                        if (theCorner == 2)
                        {
                            room.Blocks[x, z].Ceiling.XpZp = minHeight;
                            room.Blocks[x, z].Ceiling.XnZn = minHeight;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XpZn;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XpZn;
                        }

                        if (theCorner == 3)
                        {
                            room.Blocks[x, z].Ceiling.XnZp = minHeight;
                            room.Blocks[x, z].Ceiling.XpZn = minHeight;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XnZn;
                            if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                                room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XnZn;
                        }

                        room.Blocks[x, z].Ceiling.SplitDirectionToggled = false;
                        room.Blocks[x, z].FixHeights();
                    }
                }

            SmartBuildGeometry(room, area);
        }

        public static void SetDiagonalWall(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;

                    if (room.Blocks[x, z].Type == BlockType.Wall && room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None)
                        room.Blocks[x, z].Transform(new RectTransformation { QuadrantRotation = 1 });
                    else
                    {
                        // Now try to guess the floor split
                        short maxHeight = -32767;
                        byte theCorner = 0;

                        if (room.Blocks[x, z].Floor.XnZp > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XnZp;
                            theCorner = 0;
                        }

                        if (room.Blocks[x, z].Floor.XpZp > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XpZp;
                            theCorner = 1;
                        }

                        if (room.Blocks[x, z].Floor.XpZn > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XpZn;
                            theCorner = 2;
                        }

                        if (room.Blocks[x, z].Floor.XnZn > maxHeight)
                        {
                            maxHeight = room.Blocks[x, z].Floor.XnZn;
                            theCorner = 3;
                        }

                        if (theCorner == 0)
                        {
                            room.Blocks[x, z].Floor.XpZp = maxHeight;
                            room.Blocks[x, z].Floor.XnZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XnZp;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XnZp;
                        }
                        else if (theCorner == 1)
                        {
                            room.Blocks[x, z].Floor.XnZp = maxHeight;
                            room.Blocks[x, z].Floor.XpZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XpZp;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XpZp;
                        }
                        else if (theCorner == 2)
                        {
                            room.Blocks[x, z].Floor.XpZp = maxHeight;
                            room.Blocks[x, z].Floor.XnZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XpZn;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XpZn;
                        }
                        else
                        {
                            room.Blocks[x, z].Floor.XnZp = maxHeight;
                            room.Blocks[x, z].Floor.XpZn = maxHeight;
                            room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.XnZn;
                            room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.XnZn;
                        }

                        room.Blocks[x, z].Type = BlockType.Wall;
                    }
                }

            SmartBuildGeometry(room, area);
            _editor.RoomSectorPropertiesChange(room);
        }

        public static void RotateSectors(Room room, RectangleInt2 area, bool floor)
        {
            bool wallsRotated = false;

            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;
                    room.Blocks[x, z].Transform(new RectTransformation { QuadrantRotation = 1 }, floor);

                    if (room.Blocks[x, z].Floor.DiagonalSplit != DiagonalSplit.None && room.Blocks[x, z].IsAnyWall)
                        wallsRotated = true;
                }

            SmartBuildGeometry(room, area);
            _editor.RoomGeometryChange(room);

            if (wallsRotated)
                _editor.RoomSectorPropertiesChange(room);
        }

        public static void SetWall(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;
                    room.Blocks[x, z].Type = BlockType.Wall;
                    room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.None;
                }

            SmartBuildGeometry(room, area);
            _editor.RoomSectorPropertiesChange(room);
        }

        public static void SetFloor(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;

                    room.Blocks[x, z].Type = BlockType.Floor;
                    room.Blocks[x, z].Floor.DiagonalSplit = DiagonalSplit.None;
                }

            SmartBuildGeometry(room, area);
            _editor.RoomSectorPropertiesChange(room);
        }

        public static void SetCeiling(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    if (room.Blocks[x, z].Type == BlockType.BorderWall)
                        continue;

                    room.Blocks[x, z].Ceiling.DiagonalSplit = DiagonalSplit.None;
                }

            SmartBuildGeometry(room, area);
            _editor.RoomSectorPropertiesChange(room);
        }

        public static void ToggleBlockFlag(Room room, RectangleInt2 area, BlockFlags flag)
        {
            List<Room> roomsToUpdate = new List<Room>();
            roomsToUpdate.Add(room);

            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    Room.RoomBlockPair currentBlock = room.ProbeLowestBlock(x, z, _editor.Configuration.Editor_ProbeAttributesThroughPortals);
                    currentBlock.Block.Flags ^= flag;

                    if (!roomsToUpdate.Contains(currentBlock.Room))
                        roomsToUpdate.Add(currentBlock.Room);
                }

            foreach (var currentRoom in roomsToUpdate)
                _editor.RoomSectorPropertiesChange(currentRoom);
        }

        public static void ToggleForceFloorSolid(Room room, RectangleInt2 area)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                    room.Blocks[x, z].ForceFloorSolid = !room.Blocks[x, z].ForceFloorSolid;
            SmartBuildGeometry(room, area);
            _editor.RoomGeometryChange(room);
            _editor.RoomSectorPropertiesChange(room);
        }

        public static void AddPortal(Room room, RectangleInt2 area, IWin32Window owner)
        {
            // Check if one of the four corner is selected
            var cornerSelected = false;
            if (area.X0 == 0 && area.Y0 == 0 || area.X1 == 0 && area.Y1 == 0)
                cornerSelected = true;
            if (area.X0 == 0 && area.Y0 == room.NumZSectors - 1 || area.X1 == 0 && area.Y1 == room.NumZSectors - 1)
                cornerSelected = true;
            if (area.X0 == room.NumXSectors - 1 && area.Y0 == 0 || area.X1 == room.NumXSectors - 1 && area.Y1 == 0)
                cornerSelected = true;
            if (area.X0 == room.NumXSectors - 1 && area.Y0 == room.NumZSectors - 1 || area.X1 == room.NumXSectors - 1 && area.Y1 == room.NumZSectors - 1)
                cornerSelected = true;

            if (cornerSelected)
            {
                _editor.SendMessage("You have selected one of the four room's corners.", PopupType.Error);
                return;
            }

            // Check vertical space
            int floorLevel = int.MaxValue;
            int ceilingLevel = int.MinValue;
            for (int y = area.Y0; y <= area.Y1 + 1; ++y)
                for (int x = area.X0; x <= area.X1 + 1; ++x)
                {
                    floorLevel = room.GetHeightsAtPoint(x, y, BlockVertical.Floor).Select(v => v + room.Position.Y).Concat(new int[] { floorLevel }).Min();
                    ceilingLevel = room.GetHeightsAtPoint(x, y, BlockVertical.Ceiling).Select(v => v + room.Position.Y).Concat(new int[] { ceilingLevel }).Max();
                }

            // Check for possible candidates ...
            List<Tuple<PortalDirection, Room>> candidates = new List<Tuple<PortalDirection, Room>>();
            if (floorLevel != int.MaxValue && ceilingLevel != int.MinValue)
            {
                bool couldBeFloorCeilingPortal = false;
                if (new RectangleInt2(1, 1, room.NumXSectors - 2, room.NumZSectors - 2).Contains(area))
                    for (int z = area.Y0; z <= area.Y1; ++z)
                        for (int x = area.X0; x <= area.X1; ++x)
                            if (!room.Blocks[x, z].IsAnyWall)
                                couldBeFloorCeilingPortal = true;

                foreach (Room neighborRoom in _editor.Level.Rooms.Where(possibleNeighborRoom => possibleNeighborRoom != null))
                {
                    // Don't make a portal to the room itself
                    // Don't list alternate rooms as seperate candidates
                    if (neighborRoom == room || neighborRoom == room.AlternateOpposite || neighborRoom.AlternateBaseRoom != null)
                        continue;
                    RectangleInt2 neighborArea = area + (room.SectorPos - neighborRoom.SectorPos);
                    if (!new RectangleInt2(0, 0, neighborRoom.NumXSectors - 1, neighborRoom.NumZSectors - 1).Contains(neighborArea))
                        continue;

                    // Check if they vertically touch
                    int neighborFloorLevel = int.MaxValue;
                    int neighborCeilingLevel = int.MinValue;
                    for (int y = neighborArea.Y0; y <= neighborArea.Y1 + 1; ++y)
                        for (int x = neighborArea.X0; x <= neighborArea.X1 + 1; ++x)
                        {
                            neighborFloorLevel = room.GetHeightsAtPoint(x, y, BlockVertical.Floor).Select(v => v + room.Position.Y).Concat(new int[] { floorLevel }).Min();
                            neighborCeilingLevel = room.GetHeightsAtPoint(x, y, BlockVertical.Ceiling).Select(v => v + room.Position.Y).Concat(new int[] { ceilingLevel }).Max();
                            if (neighborRoom.AlternateOpposite != null)
                            {
                                neighborFloorLevel = neighborRoom.GetHeightsAtPoint(x, y, BlockVertical.Floor).Select(v => v + neighborRoom.Position.Y).Concat(new int[] { neighborFloorLevel }).Min();
                                neighborCeilingLevel = neighborRoom.GetHeightsAtPoint(x, y, BlockVertical.Ceiling).Select(v => v + neighborRoom.Position.Y).Concat(new int[] { neighborCeilingLevel }).Max();
                            }
                        }
                    if (neighborFloorLevel == int.MaxValue || neighborCeilingLevel == int.MinValue)
                        continue;
                    if (!(floorLevel <= neighborCeilingLevel && ceilingLevel >= neighborFloorLevel))
                        continue;

                    // Decide on a direction
                    if (couldBeFloorCeilingPortal &&
                        new RectangleInt2(1, 1, neighborRoom.NumXSectors - 2, neighborRoom.NumZSectors - 2).Contains(neighborArea))
                    {
                        if (Math.Abs(neighborCeilingLevel - floorLevel) <
                            Math.Abs(neighborFloorLevel - ceilingLevel))
                        { // Consider ceiling portal
                            candidates.Add(new Tuple<PortalDirection, Room>(PortalDirection.Ceiling, neighborRoom));
                        }
                        else
                        { // Consider floor portal
                            candidates.Add(new Tuple<PortalDirection, Room>(PortalDirection.Floor, neighborRoom));
                        }
                    }
                    if (area.Width == 0 && area.X0 == 0)
                        candidates.Add(new Tuple<PortalDirection, Room>(PortalDirection.WallNegativeX, neighborRoom));
                    if (area.Width == 0 && area.X0 == room.NumXSectors - 1)
                        candidates.Add(new Tuple<PortalDirection, Room>(PortalDirection.WallPositiveX, neighborRoom));
                    if (area.Height == 0 && area.Y0 == 0)
                        candidates.Add(new Tuple<PortalDirection, Room>(PortalDirection.WallNegativeZ, neighborRoom));
                    if (area.Height == 0 && area.Y0 == room.NumZSectors - 1)
                        candidates.Add(new Tuple<PortalDirection, Room>(PortalDirection.WallPositiveZ, neighborRoom));
                }
            }

            if (candidates.Count > 1)
            {
                using (var form = new FormChooseRoom("More than one possible room found that can be connected. " +
                    "Please choose one:", candidates.Select(candidate => candidate.Item2), selectedRoom => _editor.SelectedRoom = selectedRoom))
                {
                    if (form.ShowDialog(owner) != DialogResult.OK || form.SelectedRoom == null)
                        return;
                    candidates.RemoveAll(candidate => candidate.Item2 != form.SelectedRoom);
                }
            }
            if (candidates.Count != 1)
            {
                _editor.SendMessage("There are no possible room candidates for a portal.", PopupType.Error);
                return;
            }

            PortalDirection destinationDirection = candidates[0].Item1;
            Room destination = candidates[0].Item2;

            // Create portals
            var portals = room.AddObject(_editor.Level, new PortalInstance(area, destinationDirection, destination)).Cast<PortalInstance>();

            // Update
            foreach (Room portalRoom in portals.Select(portal => portal.Room).Distinct())
                portalRoom.BuildGeometry();
            foreach (PortalInstance portal in portals)
                _editor.ObjectChange(portal, ObjectChangeType.Add);

            // Reset selection
            _editor.Action = null;
            _editor.SelectedSectors = SectorSelection.None;
            _editor.SelectedObject = null;
            _editor.SelectedRooms = new[] { _editor.SelectedRoom };

            _editor.RoomSectorPropertiesChange(room);
            _editor.RoomSectorPropertiesChange(destination);
        }

        public static void AlternateRoomEnable(Room room, short AlternateGroup)
        {
            // Create new room
            var newRoom = room.Clone(_editor.Level, instance => instance.CopyToFlipRooms);
            newRoom.Name = "Flipped of " + room;
            newRoom.BuildGeometry();

            // Assign room
            _editor.Level.AssignRoomToFree(newRoom);
            _editor.RoomListChange();

            // Update room alternate groups
            room.AlternateGroup = AlternateGroup;
            room.AlternateRoom = newRoom;
            newRoom.AlternateGroup = AlternateGroup;
            newRoom.AlternateBaseRoom = room;

            _editor.RoomPropertiesChange(room);
            _editor.RoomPropertiesChange(newRoom);
        }

        public static void AlternateRoomDisableWithWarning(Room room, IWin32Window owner)
        {
            room = room.AlternateBaseRoom ?? room;

            // Ask for confirmation
            if (DarkMessageBox.Show(owner, "Do you really want to delete the flip room?",
                "Delete flipped room", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            // Change room selection if necessary
            if (_editor.SelectedRoom == room.AlternateRoom)
                _editor.SelectedRoom = room;

            // Delete alternate room
            _editor.Level.DeleteAlternateRoom(room.AlternateRoom);
            room.AlternateRoom = null;
            room.AlternateGroup = -1;

            _editor.RoomListChange();
            _editor.RoomPropertiesChange(room);
        }

        public static void SmoothRandom(Room room, RectangleInt2 area, float strengthDirection, BlockVertical vertical)
        {
            float[,] changes = new float[area.Width + 2, area.Height + 2];
            Random rng = new Random();
            for (int x = 1; x <= area.Width; x++)
                for (int z = 1; z <= area.Height; z++)
                    changes[x, z] = (float)rng.NextDouble() * strengthDirection;

            for (int x = 0; x <= area.Width; x++)
                for (int z = 0; z <= area.Height; z++)
                    for (BlockEdge edge = 0; edge < BlockEdge.Count; ++edge)
                        room.Blocks[area.X0 + x, area.Y0 + z].ChangeHeight(vertical, edge,
                            (int)Math.Round(changes[x + edge.DirectionX(), z + edge.DirectionZ()]));

            SmartBuildGeometry(room, area);
        }

        public static void SharpRandom(Room room, RectangleInt2 area, float strengthDirection, BlockVertical vertical)
        {
            Random rng = new Random();
            for (int x = 0; x <= area.Width; x++)
                for (int z = 0; z <= area.Height; z++)
                    for (BlockEdge edge = 0; edge < BlockEdge.Count; ++edge)
                        room.Blocks[area.X0 + x, area.Y0 + z].ChangeHeight(vertical, edge,
                            (int)Math.Round((float)rng.NextDouble() * strengthDirection));

            SmartBuildGeometry(room, area);
        }

        public static void Flatten(Room room, RectangleInt2 area, BlockVertical vertical)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    Block b = room.Blocks[x, z];
                    int sum = 0;
                    for (BlockEdge edge = 0; edge < BlockEdge.Count; ++edge)
                        sum += b.GetHeight(vertical, edge);
                    for (BlockEdge edge = 0; edge < BlockEdge.Count; ++edge)
                        b.SetHeight(vertical, edge, sum / 4);
                }
            SmartBuildGeometry(room, area);
        }

        public static void GridWalls(Room room, RectangleInt2 area, bool fiveDivisions = false)
        {
            for (int x = area.X0; x <= area.X1; x++)
                for (int z = area.Y0; z <= area.Y1; z++)
                {
                    Block block = room.Blocks[x, z];
                    if (block.IsAnyWall)
                    {
                        // Figure out corner heights
                        int?[] floorHeights = new int?[(int)BlockEdge.Count];
                        int?[] ceilingHeights = new int?[(int)BlockEdge.Count];
                        for (BlockEdge edge = 0; edge < BlockEdge.Count; ++edge)
                        {
                            int testX = x + edge.DirectionX(), testZ = z + edge.DirectionZ();
                            floorHeights[(int)edge] = room.GetHeightsAtPoint(testX, testZ, BlockVertical.Floor).Cast<int?>().Max();
                            ceilingHeights[(int)edge] = room.GetHeightsAtPoint(testX, testZ, BlockVertical.Ceiling).Cast<int?>().Min();
                        }

                        if (!floorHeights.Any(floorHeight => floorHeight.HasValue) || !ceilingHeights.Any(floorHeight => floorHeight.HasValue))
                            continue; // We can only do it if there is information available

                        for (BlockEdge edge = 0; edge < BlockEdge.Count; ++edge)
                        {
                            // Skip opposite diagonal step corner
                            switch (block.Floor.DiagonalSplit)
                            {
                                case DiagonalSplit.XnZn:
                                    if (edge == BlockEdge.XpZp)
                                        continue;
                                    break;
                                case DiagonalSplit.XnZp:
                                    if (edge == BlockEdge.XpZn)
                                        continue;
                                    break;
                                case DiagonalSplit.XpZn:
                                    if (edge == BlockEdge.XnZp)
                                        continue;
                                    break;
                                case DiagonalSplit.XpZp:
                                    if (edge == BlockEdge.XnZn)
                                        continue;
                                    break;
                            }

                            // Use the closest available vertical area information and divide it equally
                            int floor = floorHeights[(int)edge] ?? floorHeights[((int)edge + 1) % 4] ?? floorHeights[((int)edge + 3) % 4] ?? floorHeights[((int)edge + 2) % 4].Value;
                            int ceiling = ceilingHeights[(int)edge] ?? ceilingHeights[((int)edge + 1) % 4] ?? ceilingHeights[((int)edge + 3) % 4] ?? ceilingHeights[((int)edge + 2) % 4].Value;

                            block.SetHeight(BlockVertical.Ed, edge, (short)Math.Round(fiveDivisions ? (floor * 4.0f + ceiling * 1.0f) / 5.0f : floor));
                            block.Floor.SetHeight(edge, (short)Math.Round(fiveDivisions ? (floor * 3.0f + ceiling * 2.0f) / 5.0f : (floor * 2.0f + ceiling * 1.0f) / 3.0f));
                            block.Ceiling.SetHeight(edge, (short)Math.Round(fiveDivisions ? (floor * 2.0f + ceiling * 3.0f) / 5.0f : (floor * 1.0f + ceiling * 2.0f) / 3.0f));
                            block.SetHeight(BlockVertical.Rf, edge, (short)Math.Round(fiveDivisions ? (floor * 1.0f + ceiling * 4.0f) / 5.0f : ceiling));
                        }
                    }
                }

            SmartBuildGeometry(room, area);
        }

        public static void CreateRoomAboveOrBelow(Room room, Func<Room, int> GetYOffset, short newRoomHeight)
        {
            // Create room
            var newRoom = new Room(_editor.Level, room.NumXSectors, room.NumZSectors, _editor.Level.Settings.DefaultAmbientLight,
                                   "", newRoomHeight);
            newRoom.Position = room.Position + new VectorInt3(0, GetYOffset(newRoom), 0);
            newRoom.Name = "Room " + (newRoom.Position.Y > room.Position.Y ? "above " : "below ") + room.Name;
            _editor.Level.AssignRoomToFree(newRoom);
            _editor.RoomListChange();

            // Build the geometry of the new room
            newRoom.BuildGeometry();

            // Update the UI
            if (_editor.SelectedRoom == room)
                _editor.SelectedRoom = newRoom; //Don't center
        }

        public static void SplitRoom(IWin32Window owner)
        {
            if (!CheckForRoomAndBlockSelection(owner))
                return;

            RectangleInt2 area = _editor.SelectedSectors.Area.Inflate(1);
            var room = _editor.SelectedRoom;

            // Check for gray walls selection
            if (area.X0 == -1 || area.X1 == -1 ||
                area.X0 == room.NumXSectors || area.X1 == room.NumXSectors ||
                area.Y0 == -1 || area.Y1 == -1 ||
                area.Y0 == room.NumZSectors || area.Y1 == room.NumZSectors)
            {
                _editor.SendMessage("You can't select border walls when splitting a room.", PopupType.Error);
                return;
            }

            if (room.AlternateBaseRoom != null)
            {
                _editor.Level.AssignRoomToFree(room.AlternateBaseRoom.Split(_editor.Level, area));
                _editor.RoomGeometryChange(room);
                _editor.RoomSectorPropertiesChange(room);
            }

            if (room.AlternateRoom != null)
            {
                _editor.Level.AssignRoomToFree(room.AlternateRoom.Split(_editor.Level, area));
                _editor.RoomGeometryChange(room);
                _editor.RoomSectorPropertiesChange(room);
            }

            Vector3 oldRoomPos = room.Position;
            _editor.Level.AssignRoomToFree(room.Split(_editor.Level, area));
            _editor.RoomGeometryChange(room);
            _editor.RoomSectorPropertiesChange(room);
            _editor.RoomListChange();

            // Fix selection
            if (_editor.SelectedRoom == room && _editor.SelectedSectors.Valid)
            {
                var selection = _editor.SelectedSectors;
                selection.Area = selection.Area + new VectorInt2((int)(oldRoomPos.X - room.Position.X), (int)(oldRoomPos.Z - room.Position.Z));
                _editor.SelectedSectors = selection;
            }
        }

        public static void SelectConnectedRooms()
        {
            _editor.SelectRooms(_editor.Level.GetConnectedRooms(_editor.SelectedRooms).ToList());
        }

        public static void DuplicateRooms(IWin32Window owner)
        {
            var newRoom = _editor.SelectedRoom.Clone(_editor.Level);
            newRoom.Name = _editor.SelectedRoom.Name + " (copy)";
            newRoom.BuildGeometry();
            _editor.Level.AssignRoomToFree(newRoom);
            _editor.RoomListChange();
            _editor.SelectedRoom = newRoom;
        }

        public static bool CheckForRoomAndBlockSelection(IWin32Window owner)
        {
            if (_editor.SelectedRoom == null || !_editor.SelectedSectors.Valid)
            {
                _editor.SendMessage("Please select a valid group of sectors.", PopupType.Error);
                return false;
            }
            return true;
        }

        public static bool CheckForLockedRooms(IWin32Window owner, IEnumerable<Room> rooms)
        {
            if (rooms.All(room => !room.Locked))
                return false;

            // Inform user and offer an option to unlock the room position
            string message = "Can't move rooms because some rooms are locked. Unlock and continue?\n" +
                "Locked rooms: " + string.Join(" ,", rooms.Where(room => room.Locked).Select(s => s.Name));
            if (DarkMessageBox.Show(owner, message, "Locked rooms", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
                return true;

            // Unlock rooms
            foreach (Room room in rooms)
                room.Locked = false;
            return true;
        }

        public static void ApplyCurrentAmbientLightToAllRooms()
        {
            foreach (var room in _editor.Level.Rooms.Where(room => room != null))
                room.AmbientLight = _editor.SelectedRoom.AmbientLight;
            Parallel.ForEach(_editor.Level.Rooms.Where(room => room != null), room => room.RoomGeometry?.Relight(room));
            foreach (var room in _editor.Level.Rooms.Where(room => room != null))
                Editor.Instance.RaiseEvent(new Editor.RoomPropertiesChangedEvent { Room = room });
        }

        public static bool BuildLevel(bool autoCloseWhenDone, IWin32Window owner)
        {
            Level level = _editor.Level;
            if(level.Settings.Wads.All(wad => wad.Wad == null))
            {
                _editor.SendMessage("No wads loaded. Can't compile level without wads.", PopupType.Error);
                return false;
            }

            string fileName = level.Settings.MakeAbsolute(level.Settings.GameLevelFilePath);

            using (var form = new FormOperationDialog("Build *.tr4 level", autoCloseWhenDone,
                progressReporter =>
                {
                    var watch = new Stopwatch();
                    watch.Start();
                    var compiler = new LevelCompilerClassicTR(level, fileName, progressReporter);
                    var statistics = compiler.CompileLevel();
                    watch.Stop();
                    progressReporter.ReportProgress(100, "Elapsed time: " + watch.Elapsed.TotalMilliseconds + "ms");

                    // Raise an event for statistics update
                    Editor.Instance.RaiseEvent(new Editor.LevelCompilationCompletedEvent { InfoString = statistics.ToString() });

                    // Force garbage collector to compact memory
                    GC.Collect();
                }))
            {
                form.ShowDialog(owner);
                return form.DialogResult != DialogResult.Cancel;
            }
        }

        public static void BuildLevelAndPlay(IWin32Window owner)
        {
            if (_editor?.Level?.Settings?.WadTryGetMoveable(WadMoveableId.Lara) != null &&
                 _editor.Level.Rooms
                .Where(room => room != null)
                .SelectMany(room => room.Objects)
                .Any(obj => obj is ItemInstance && ((ItemInstance)obj).ItemType == new ItemType(WadMoveableId.Lara)))
            {
                if (BuildLevel(true, owner))
                    TombLauncher.Launch(_editor.Level.Settings, owner);
            }
            else
                _editor.SendMessage("No Lara found. Place Lara to play level.", PopupType.Error);

        }

        public static IEnumerable<LevelTexture> AddTexture(IWin32Window owner, IEnumerable<string> predefinedPaths = null)
        {
            List<string> paths = (predefinedPaths ?? LevelFileDialog.BrowseFiles(owner, _editor.Level.Settings,
                PathC.GetDirectoryNameTry(_editor.Level.Settings.LevelFilePath),
                "Load texture files", LevelTexture.FileExtensions, VariableType.LevelDirectory)).ToList();
            if (paths.Count == 0) // Fast track to avoid unnecessary updates
                return new LevelTexture[0];

            // Load textures concurrently
            LevelTexture[] results = new LevelTexture[paths.Count];
            Parallel.For(0, paths.Count, i => results[i] = new LevelTexture(_editor.Level.Settings, paths[i]));

            // Open GUI for texture that couldn't be loaded
            for (int i = 0; i < results.Length; ++i)
                while (results[i]?.LoadException != null)
                    switch (DarkMessageBox.Show(owner, "An error occurred while loading texture file '" + paths[i] + "'." +
                        "\nError message: " + results[i].LoadException.GetType(), "Unable to load texture file.",
                        paths.Count == 1 ? MessageBoxButtons.RetryCancel : MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error,
                        paths.Count == 1 ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1))
                    {
                        case DialogResult.Retry:
                            results[i].Reload(_editor.Level.Settings);
                            break;
                        case DialogResult.Ignore:
                            results[i] = null;
                            break;
                        default:
                            return new LevelTexture[0];
                    }

            // Update level
            _editor.Level.Settings.Textures.AddRange(results.Where(result => result != null));
            _editor.LoadedTexturesChange(results.FirstOrDefault(result => result != null));
            return results.Where(result => result != null);
        }

        public static void UpdateTextureFilepath(IWin32Window owner, LevelTexture toReplace)
        {
            var settings = _editor.Level.Settings;
            string path = LevelFileDialog.BrowseFile(owner, settings, toReplace.Path, "Load a texture", LevelTexture.FileExtensions, VariableType.LevelDirectory, false);
            if (path == toReplace?.Path)
                return;

            toReplace.SetPath(_editor.Level.Settings, path);
            _editor.LoadedTexturesChange(toReplace);
        }

        public static void UnloadTextures(IWin32Window owner)
        {
            if (DarkMessageBox.Show(owner, "Are you sure to unload ALL " + _editor.Level.Settings.Textures.Count +
                " texture files loaded?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            foreach (LevelTexture texture in _editor.Level.Settings.Textures)
                texture.SetPath(_editor.Level.Settings, null);
            _editor.LoadedTexturesChange();
        }

        public static void RemoveTextures(IWin32Window owner)
        {
            if (DarkMessageBox.Show(owner, "Are you sure to DELETE ALL " + _editor.Level.Settings.Textures.Count +
                " texture files loaded? Everything will be untextured.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _editor.Level.Settings.Textures.Clear();
            _editor.Level.RemoveTextures(texture => true);
            _editor.LoadedTexturesChange();
        }

        public static void RemoveTexture(IWin32Window owner, LevelTexture textureToDelete)
        {
            if (DarkMessageBox.Show(owner, "Are you sure to DELETE the texture " + textureToDelete +
                "? Everything using the texture will be untextured.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _editor.Level.Settings.Textures.Remove(textureToDelete);
            _editor.Level.RemoveTextures(texture => texture == textureToDelete);
            _editor.LoadedTexturesChange();
        }

        public static IEnumerable<ReferencedWad> AddWad(IWin32Window owner, IEnumerable<string> predefinedPaths = null)
        {
            List<string> paths = (predefinedPaths ?? LevelFileDialog.BrowseFiles(owner, _editor.Level.Settings,
                PathC.GetDirectoryNameTry(_editor.Level.Settings.LevelFilePath),
                "Load object files (*.wad)", Wad2.WadFormatExtensions, VariableType.LevelDirectory)).ToList();
            if (paths.Count == 0) // Fast track to avoid unnecessary updates
                return new ReferencedWad[0];

            // Load objects (*.wad files) concurrently
            ReferencedWad[] results = new ReferencedWad[paths.Count];
            GraphicalDialogHandler synchronizedDialogHandler = new GraphicalDialogHandler(owner); // Have only one to synchronize the messages.
            Parallel.For(0, paths.Count, i => results[i] = new ReferencedWad(_editor.Level.Settings, paths[i], synchronizedDialogHandler));

            // Open GUI for objects (*.wad files) that couldn't be loaded
            for (int i = 0; i < results.Length; ++i)
                while (results[i]?.LoadException != null)
                    switch (DarkMessageBox.Show(owner, "An error occurred while loading object file '" + paths[i] + "'." +
                        "\nError message: " + results[i].LoadException.GetType(), "Unable to load object file.",
                        paths.Count == 1 ? MessageBoxButtons.RetryCancel : MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error,
                        paths.Count == 1 ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1))
                    {
                        case DialogResult.Retry:
                            results[i].Reload(_editor.Level.Settings);
                            break;
                        case DialogResult.Ignore:
                            results[i] = null;
                            break;
                        default:
                            return new ReferencedWad[0];
                    }


            // Update level
            _editor.Level.Settings.Wads.AddRange(results.Where(result => result != null));
            _editor.LoadedWadsChange();
            return results.Where(result => result != null);
        }

        public static void UpdateWadFilepath(IWin32Window owner, ReferencedWad toReplace)
        {
            string path = LevelFileDialog.BrowseFile(owner, _editor.Level.Settings, toReplace.Path,
                "Load an object file (*.wad)", Wad2.WadFormatExtensions, VariableType.LevelDirectory, false);
            if (path == toReplace?.Path)
                return;
            toReplace.SetPath(_editor.Level.Settings, path);
            _editor.LoadedWadsChange();
        }

        public static void RemoveWads(IWin32Window owner)
        {
            if (DarkMessageBox.Show(owner, "Are you sure to delete ALL " + _editor.Level.Settings.Wads.Count +
                " wad files loaded?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _editor.Level.Settings.Wads.Clear();
            _editor.LoadedWadsChange();
        }

        public static void ReloadWads(IWin32Window owner)
        {
            var dialogHandler = new GraphicalDialogHandler(owner);
            foreach (var wad in _editor.Level.Settings.Wads)
                wad.Reload(_editor.Level.Settings, dialogHandler);
            _editor.LoadedWadsChange();
        }

        public static bool EnsureNoOutsidePortalsInSelecton(IWin32Window owner)
        {
            return Room.RemoveOutsidePortals(_editor.Level, _editor.SelectedRooms, list =>
            {
                StringBuilder portalsToRemoveList = list.Aggregate(new StringBuilder(), (str, room) => str.Append(room).Append("\n"), str => str.Remove(str.Length - 1, 1));
                return DarkMessageBox.Show(owner, "The rooms can't have portals to the outside. Do you want to continue by removing all portals to the outside? " +
                    " Portals to remove: " + portalsToRemoveList.ToString(),
                    "Outside portals", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            });
        }

        public static bool TransformRooms(RectTransformation transformation, IWin32Window owner)
        {
            if (!EnsureNoOutsidePortalsInSelecton(owner))
                return false;
            var newRooms = _editor.Level.TransformRooms(_editor.SelectedRooms, transformation);
            foreach (Room room in newRooms)
                room.BuildGeometry();
            _editor.SelectRoomsAndResetCamera(newRooms);

            _editor.RoomListChange();
            foreach (Room room in newRooms)
            {
                _editor.RoomGeometryChange(room);
                _editor.RoomSectorPropertiesChange(room);
            }
            return true;
        }

        public static void TryCopyObject(ObjectInstance instance, IWin32Window owner)
        {
            if (!(instance is PositionBasedObjectInstance))
            {
                _editor.SendMessage("No object selected. \nYou have to select position-based object before you can copy it.", PopupType.Info);
                return;
            }
            Clipboard.SetDataObject(new ObjectClipboardData(_editor));
        }

        public static void TryCopySectors(SectorSelection selection, IWin32Window owner)
        {
            Clipboard.SetDataObject(new SectorsClipboardData(_editor));
        }

        public static void TryStampObject(ObjectInstance instance, IWin32Window owner)
        {
            if (!(instance is PositionBasedObjectInstance))
            {
                _editor.SendMessage("No object selected. \nYou have to select position-based object before you can copy it.", PopupType.Info);
                return;
            }
            _editor.Action = new EditorActionPlace(false, (level, room) => (PositionBasedObjectInstance)instance.Clone());
        }

        public static void TryPasteSectors(SectorsClipboardData data, IWin32Window owner)
        {
            int x0 = _editor.SelectedSectors.Area.X0;
            int z0 = _editor.SelectedSectors.Area.Y0;
            int x1 = Math.Min(_editor.SelectedRoom.NumXSectors - 1, x0 + data.Width);
            int z1 = Math.Min(_editor.SelectedRoom.NumZSectors - 1, z0 + data.Height);

            var sectors = data.GetSectors();

            for (int x = x0; x < x1; x++)
                for (int z = z0; z < z1; z++)
                {
                    _editor.SelectedRoom.Blocks[x, z] = sectors[x - x0, z - z0].Clone();
                }

            _editor.SelectedRoom.BuildGeometry();
            _editor.RoomSectorPropertiesChange(_editor.SelectedRoom);
        }

        public static bool DragDropFileSupported(DragEventArgs e, bool allow3DImport = false)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return false;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
                if (Wad2.WadFormatExtensions.Matches(file) ||
                    LevelTexture.FileExtensions.Matches(file) ||
                    allow3DImport && ImportedGeometry.FileExtensions.Matches(file) ||
                    file.EndsWith(".prj", StringComparison.InvariantCultureIgnoreCase) ||
                    file.EndsWith(".prj2", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            return false;
        }

        public static void MoveLara(IWin32Window owner, VectorInt2 p)
        {
            // Search for first Lara and remove her
            MoveableInstance lara;
            foreach (Room room in _editor.Level.Rooms.Where(room => room != null))
                foreach (var instance in room.Objects)
                {
                    lara = instance as MoveableInstance;
                    if (lara != null && lara.WadObjectId == WadMoveableId.Lara)
                    {
                        room.RemoveObject(_editor.Level, instance);
                        _editor.ObjectChange(lara, ObjectChangeType.Remove, room);
                        goto FoundLara;
                    }
                }
            lara = new MoveableInstance { WadObjectId = WadMoveableId.Lara }; // Lara
            FoundLara:

            // Add lara to current sector
            PlaceObject(_editor.SelectedRoom, p, lara);
        }

        public static int DragDropCommonFiles(DragEventArgs e, IWin32Window owner)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return -1;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Is there a prj file to open?
            string prjFile = files.FirstOrDefault(file => file.EndsWith(".prj", StringComparison.InvariantCultureIgnoreCase));
            if (prjFile != null)
            {
                OpenLevelPrj(owner, prjFile);
                return files.Length - 1;
            }

            // Is there a prj2 file to open?
            string prj2File = files.FirstOrDefault(file => file.EndsWith(".prj2", StringComparison.InvariantCultureIgnoreCase));
            if (prjFile != null)
            {
                OpenLevel(owner, prj2File);
                return files.Length - 1;
            }

            // Are there any more specific files to open?
            // (Process the ones of the same type concurrently)
            IEnumerable<string> wadFiles = files.Where(file => Wad2.WadFormatExtensions.Matches(file));
            IEnumerable<string> textureFiles = files.Where(file => LevelTexture.FileExtensions.Matches(file));
            AddWad(owner, wadFiles.Select(file => _editor.Level.Settings.MakeRelative(file, VariableType.LevelDirectory)));
            AddTexture(owner, textureFiles.Select(file => _editor.Level.Settings.MakeRelative(file, VariableType.LevelDirectory)));
            return files.Length - (wadFiles.Count() + textureFiles.Count()); // Unsupported file count
        }

        public static void SetPortalOpacity(PortalOpacity opacity, IWin32Window owner)
        {
            var portal = _editor.SelectedObject as PortalInstance;
            if (_editor.SelectedRoom == null || portal == null)
            {
                _editor.SendMessage("No portal selected.", PopupType.Error);
                return;
            }

            portal.Opacity = opacity;
            _editor.SelectedRoom.BuildGeometry();
            _editor.ObjectChange(portal, ObjectChangeType.Change);
        }

        public static bool SaveLevel(IWin32Window owner, bool askForPath)
        {
            string filePath = _editor.Level.Settings.LevelFilePath;

            // Show save dialog if necessary
            if (askForPath || string.IsNullOrEmpty(filePath))
                filePath = LevelFileDialog.BrowseFile(owner, null, filePath, "Save level", LevelSettings.FileFormatsLevel, null, true);
            if (string.IsNullOrEmpty(filePath))
                return false;

            // Save level
            try
            {
                Prj2Writer.SaveToPrj2(filePath, _editor.Level);
            }
            catch (Exception exc)
            {
                logger.Error(exc, "Unable to save to \"" + filePath + "\".");
                _editor.SendMessage("There was an error while saving project file. Exception: " + exc.Message, PopupType.Error);
                return false;
            }

            // Update state
            _editor.HasUnsavedChanges = false;
            if (_editor.Level.Settings.LevelFilePath != filePath)
            {
                _editor.Level.Settings.LevelFilePath = filePath;
                _editor.LevelFileNameChange();
            }
            return true;
        }

        public static ItemType? GetCurrentItemWithMessage()
        {
            ItemType? result = _editor.ChosenItem;
            if (result == null)
                _editor.SendMessage("Select an item first.", PopupType.Error);
            return result;
        }

        public static void FindItem()
        {
            ItemType? currentItem = GetCurrentItemWithMessage();
            if (currentItem == null)
                return;

            // Search for matching objects after the previous one
            ObjectInstance previousFind = _editor.SelectedObject;
            ObjectInstance instance = _editor.Level.Rooms
                .Where(room => room != null)
                .SelectMany(room => room.Objects)
                .FindFirstAfterWithWrapAround(
                obj => previousFind == obj,
                obj => obj is ItemInstance && ((ItemInstance)obj).ItemType == currentItem.Value);

            // Show result
            if (instance == null)
                _editor.SendMessage("No object of the selected item type found.", PopupType.Info);
            else
                _editor.ShowObject(instance);
        }

        public static void ExportCurrentRoom(IWin32Window owner)
        {
            ExportRooms(new[] { _editor.SelectedRoom }, owner);
        }

        public static void ExportRooms(IEnumerable<Room> rooms, IWin32Window owner)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Export current room";
                saveFileDialog.Filter = BaseGeometryExporter.FileExtensions.GetFilter();
                saveFileDialog.AddExtension = true;
                saveFileDialog.DefaultExt = "obj";
                saveFileDialog.FileName = _editor.SelectedRoom.Name;

                if (saveFileDialog.ShowDialog(owner) == DialogResult.OK)
                {
                    using (var settingsDialog = new GeometryIOSettingsDialog(new IOGeometrySettings()))
                    {
                        settingsDialog.AddPreset(IOSettingsPresets.SettingsPresets);
                        string resultingExtension = Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();

                        if (resultingExtension.Equals(".mqo"))
                            settingsDialog.SelectPreset("Metasequoia MQO");

                        if (settingsDialog.ShowDialog(owner) == DialogResult.OK)
                        {
                            BaseGeometryExporter.GetTextureDelegate getTextureCallback = txt =>
                            {
                                if (txt is LevelTexture)
                                    return _editor.Level.Settings.MakeAbsolute(((LevelTexture)txt).Path);
                                else
                                    return "";
                            };

                            BaseGeometryExporter exporter = BaseGeometryExporter.CreateForFile(saveFileDialog.FileName, settingsDialog.Settings, getTextureCallback);

                            // Prepare data for export
                            var model = new IOModel();
                            var texture = _editor.Level.Settings.Textures[0];

                            // Create various materials
                            var materialOpaque = new IOMaterial(Material.Material_Opaque + "_0_0_0_0", texture, false, false, 0);
                            var materialOpaqueDoubleSided = new IOMaterial(Material.Material_OpaqueDoubleSided + "_0_0_1_0", texture, false, true, 0);
                            var materialAdditiveBlending = new IOMaterial(Material.Material_AdditiveBlending + "_0_1_0_0", texture, true, false, 0);
                            var materialAdditiveBlendingDoubleSided = new IOMaterial(Material.Material_AdditiveBlendingDoubleSided + "_0_1_1_0", texture, true, true, 0);

                            model.Materials.Add(materialOpaque);
                            model.Materials.Add(materialOpaqueDoubleSided);
                            model.Materials.Add(materialAdditiveBlending);
                            model.Materials.Add(materialAdditiveBlendingDoubleSided);

                            //var db = new RoomXmlFile();

                            foreach (var room in rooms)
                            {
                                var mesh = new IOMesh("TeRoom_" + _editor.Level.Rooms.ReferenceIndexOf(room));
                                mesh.Position = room.WorldPos;

                                // db.Rooms.Add(mesh.Name, new RoomXmlFile(mesh.Name, room.WorldPos, _editor.Level.Rooms.ReferenceIndexOf(room)));

                                // Add submeshes
                                mesh.Submeshes.Add(materialOpaque, new IOSubmesh(materialOpaque));
                                mesh.Submeshes.Add(materialOpaqueDoubleSided, new IOSubmesh(materialOpaqueDoubleSided));
                                mesh.Submeshes.Add(materialAdditiveBlending, new IOSubmesh(materialAdditiveBlending));
                                mesh.Submeshes.Add(materialAdditiveBlendingDoubleSided, new IOSubmesh(materialAdditiveBlendingDoubleSided));

                                if (room.RoomGeometry == null)
                                    continue;
                                for (var i = 0; i < room.RoomGeometry.VertexPositions.Count; i += 3)
                                {
                                    // TODO: Detect quads with RoomGeometry.IsQuad( and reconstruct quads.
                                    var textureArea = room.RoomGeometry.TriangleTextureAreas[i / 3];
                                    var poly = new IOPolygon(IOPolygonShape.Triangle); //indices.Count == 3 ? IOPolygonShape.Triangle : IOPolygonShape.Quad);
                                    poly.Indices.Add(i);
                                    poly.Indices.Add(i + 1);
                                    poly.Indices.Add(i + 2);

                                    // Get the right submesh
                                    var submesh = mesh.Submeshes[materialOpaque];
                                    if (textureArea.BlendMode == BlendMode.Additive)
                                    {
                                        if (textureArea.DoubleSided)
                                            submesh = mesh.Submeshes[materialAdditiveBlendingDoubleSided];
                                        else
                                            submesh = mesh.Submeshes[materialAdditiveBlending];
                                    }
                                    else
                                    {
                                        if (textureArea.DoubleSided)
                                            submesh = mesh.Submeshes[materialOpaqueDoubleSided];
                                    }
                                    submesh.Polygons.Add(poly);

                                    mesh.Positions.Add(room.RoomGeometry.VertexPositions[i] /*- deltaPos*/ + room.WorldPos /*- minPosition*/);
                                    mesh.Positions.Add(room.RoomGeometry.VertexPositions[i + 1] /*- deltaPos*/ + room.WorldPos /*- minPosition*/);
                                    mesh.Positions.Add(room.RoomGeometry.VertexPositions[i + 2] /*- deltaPos*/ + room.WorldPos /*- minPosition*/);


                                    mesh.UV.Add(textureArea.TexCoord0);
                                    mesh.UV.Add(textureArea.TexCoord1);
                                    mesh.UV.Add(textureArea.TexCoord2);

                                    mesh.Colors.Add(new Vector4(room.RoomGeometry.VertexColors[i], 1.0f));
                                    mesh.Colors.Add(new Vector4(room.RoomGeometry.VertexColors[i + 1], 1.0f));
                                    mesh.Colors.Add(new Vector4(room.RoomGeometry.VertexColors[i + 2], 1.0f));
                                }

                                model.Meshes.Add(mesh);
                            }

                            string dbFile = Path.GetDirectoryName(saveFileDialog.FileName) + "\\" + Path.GetFileNameWithoutExtension(saveFileDialog.FileName) + ".xml";

                            if (exporter.ExportToFile(model, saveFileDialog.FileName) /*&& RoomsImportExportXmlDatabase.WriteToFile(dbFile, db)*/)
                            {
                                _editor.SendMessage("Room exported correctly.", PopupType.Info);
                            }
                        }
                    }
                }
            }
        }

        public const string RoomIdentifier = "TeRoom_";

        public static void ImportRooms(IWin32Window owner)
        {
            string importedGeometryPath;
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Import rooms";
                openFileDialog.Filter = BaseGeometryImporter.FileExtensions.GetFilter();
                openFileDialog.FileName = _editor.SelectedRoom.Name;
                if (openFileDialog.ShowDialog(owner) != DialogResult.OK)
                    return;
                importedGeometryPath = openFileDialog.FileName;
            }

            // Add imported geometries
            try
            {
                string importedGeometryDirectory = Path.GetDirectoryName(importedGeometryPath);

                var info = ImportedGeometryInfo.Default;
                info.Path = importedGeometryPath;
                info.Name = Path.GetFileNameWithoutExtension(importedGeometryPath);

                ImportedGeometry newObject = new ImportedGeometry();
                _editor.Level.Settings.ImportedGeometryUpdate(newObject, info);
                _editor.Level.Settings.ImportedGeometries.Add(newObject);

                // Translate the vertices to room's origin
                foreach (var mesh in newObject.DirectXModel.Meshes)
                {
                    // Find the room
                    int roomIndex = int.Parse(mesh.Name.Split('_')[1]);
                    var room = _editor.Level.Rooms[roomIndex];

                    // Translate vertices
                    for (int j = 0; j < mesh.Vertices.Count; j++)
                    {
                        var vertex = mesh.Vertices[j];
                        vertex.Position.X -= room.WorldPos.X;
                        vertex.Position.Y -= room.WorldPos.Y;
                        vertex.Position.Z -= room.WorldPos.Z;
                        mesh.Vertices[j] = vertex;
                    }
                }

                // Rebuild DirectX buffer
                newObject.DirectXModel.UpdateBuffers();

                // Load the XML db
                /*string dbName = Path.GetDirectoryName(importedGeometryPath) + "\\" + Path.GetFileNameWithoutExtension(importedGeometryPath) + ".xml";
                var db = RoomsImportExportXmlDatabase.LoadFromFile(dbName);
                if (db == null)
                    throw new FileNotFoundException("There must be also an XML file with the same name of the 3D file");
        */

                // Create a dictionary of the rooms by name
                /* var roomDictionary = new Dictionary<string, IOMesh>();
                 foreach (var msh in newObject.DirectXModel)

                 // Translate rooms
                 for (int i=0;i<db.Rooms.Count;i++)
                 {
                     string roomMeshName = db.Rooms.ElementAt(i).Key;
                     foreach (var mesh in model.Meshes)
                         for (int i = 0; i < mesh.Positions.Count; i++)
                         {
                             var pos = mesh.Positions[i];
                             pos -= mesh.Origin;
                             mesh.Positions[i] = pos;
                         }
                 }
                 */

                // Figure out the relevant rooms
                Dictionary<int, int> roomIndices = new Dictionary<int, int>();
                int meshIndex = 0;
                foreach (ImportedGeometryMesh mesh in newObject.DirectXModel.Meshes)
                {
                    int currentIndex = 0;
                    do
                    {
                        int roomIndexStart = mesh.Name.IndexOf(RoomIdentifier, currentIndex, StringComparison.InvariantCultureIgnoreCase);
                        if (roomIndexStart < 0)
                            break;

                        int roomIndexEnd = roomIndexStart + RoomIdentifier.Length;
                        while (roomIndexEnd < mesh.Name.Length && !char.IsWhiteSpace(mesh.Name[roomIndexEnd]))
                            ++roomIndexEnd;

                        string roomIndexStr = mesh.Name.Substring(roomIndexStart + RoomIdentifier.Length, roomIndexEnd - (roomIndexStart + RoomIdentifier.Length));
                        ushort roomIndex;
                        if (ushort.TryParse(roomIndexStr, out roomIndex))
                        {
                            roomIndices.Add(meshIndex, roomIndex);
                            meshIndex++;
                        }

                        currentIndex = roomIndexEnd;
                    } while (currentIndex < mesh.Name.Length);
                }
                //roomIndices = roomIndices.Distinct().ToList();

                // Add rooms
                foreach (var pair in roomIndices)
                {
                    Room room = _editor.Level.Rooms[pair.Value];
                    var newImported = new ImportedGeometryInstance();
                    newImported.Position = Vector3.Zero;
                    newImported.Model = newObject;
                    newImported.MeshFilter = newObject.DirectXModel.Meshes[pair.Key].Name;
                    room.AddObject(_editor.Level, newImported);
                }
            }
            catch (Exception exc)
            {
                logger.Error(exc.Message);
                _editor.SendMessage("Unable to import rooms from geometry.", PopupType.Error);
            }
        }

        public static void OpenLevel(IWin32Window owner, string fileName = null)
        {
            if (!ContinueOnFileDrop(owner, "Open level"))
                return;

            if (string.IsNullOrEmpty(fileName))
                fileName = LevelFileDialog.BrowseFile(owner, null, fileName, "Open Tomb Editor level", LevelSettings.FileFormatsLevel, null, false);
            if (string.IsNullOrEmpty(fileName))
                return;

            Level newLevel = null;
            try
            {
                newLevel = Prj2Loader.LoadFromPrj2(fileName, new ProgressReporterSimple());
            }
            catch (Exception exc)
            {
                logger.Error(exc, "Unable to open \"" + fileName + "\"");
                _editor.SendMessage("There was an error while opening project file. File may be in use or may be corrupted. Exception: " + exc.Message, PopupType.Error);
            }
            _editor.Level = newLevel;
        }

        public static void OpenLevelPrj(IWin32Window owner, string fileName = null)
        {
            if (!ContinueOnFileDrop(owner, "Open level"))
                return;

            if (string.IsNullOrEmpty(fileName))
                fileName = LevelFileDialog.BrowseFile(owner, null, fileName, "Open Tomb Editor level", LevelSettings.FileFormatsLevelPrj, null, false);
            if (string.IsNullOrEmpty(fileName))
                return;


            Level newLevel = null;
            using (var form = new FormOperationDialog("Import PRJ", false, progressReporter =>
                newLevel = PrjLoader.LoadFromPrj(fileName, progressReporter)))
            {
                if (form.ShowDialog(owner) != DialogResult.OK || newLevel == null)
                    return;
                _editor.Level = newLevel;
                newLevel = null;
            }
        }

        public static void MoveRooms(VectorInt3 positionDelta, IEnumerable<Room> rooms)
        {
            HashSet<Room> roomsToMove = new HashSet<Room>(rooms);

            // Update position of all rooms
            foreach (Room room in roomsToMove)
                room.Position += positionDelta;

            // Make list of potential rooms to update
            HashSet<Room> roomsToUpdate = new HashSet<Room>();
            foreach (Room room in roomsToMove)
            {
                bool anyRoomUpdated = false;
                foreach (PortalInstance portal in room.Portals)
                    if (!roomsToMove.Contains(portal.AdjoiningRoom))
                    {
                        roomsToUpdate.Add(portal.AdjoiningRoom);
                        anyRoomUpdated = true;
                    }

                if (anyRoomUpdated)
                    roomsToUpdate.Add(room);
            }

            // Update
            foreach (Room room in roomsToUpdate)
            {
                room.BuildGeometry();
                _editor.RoomSectorPropertiesChange(room);
            }

            foreach (Room room in roomsToMove)
                _editor.RoomGeometryChange(room);
        }

        public static void BookmarkObject(ObjectInstance objectToBookmark)
        {
            _editor.BookmarkedObject = objectToBookmark;
            _editor.SendMessage("Object bookmarked: " + _editor.BookmarkedObject, PopupType.Info);
        }

        public static void SwitchMode(EditorMode mode)
        {
            if((mode == EditorMode.Map2D) != (_editor.Mode == EditorMode.Map2D))
                _editor.SendMessage(); // We change 2D to 3D, reset notifications

            _editor.Mode = mode;

            if (mode == EditorMode.Map2D && _editor.Configuration.Map2D_ShowFirstTimeHint)
            {
                _editor.SendMessage("Double click or press Alt + left click on the map to add a depth probe.\n" +
                                    "Double click or press Ctrl + left click on a depth probe to remove it.\n" +
                                    "\n" +
                                    "Press the middle mouse button to select multiple rooms or select connected rooms by double clicking.\n" +
                                    "The selection can be modified using Ctrl, Shift, Alt. To copy rooms, press Ctrl while moving.",
                                    PopupType.Info);

                _editor.Configuration.Map2D_ShowFirstTimeHint = false;
                _editor.ConfigurationChange();
            }

            if (_editor.Configuration.Editor_DiscardSelectionOnModeSwitch)
                _editor.SelectedSectors = SectorSelection.None;
        }

        public static void SwitchTool(EditorTool tool)
        {
            _editor.Tool = tool;
        }

        public static void SwitchToolOrdered(int toolIndex)
        {
            if (_editor.Mode == EditorMode.Map2D || toolIndex > (int)EditorToolType.Terrain ||
                _editor.Mode != EditorMode.Geometry && toolIndex > 5)
                return;

            EditorTool currentTool = _editor.Tool;

            switch (toolIndex)
            {
                case 0:
                    currentTool.Tool = EditorToolType.Selection;
                    break;
                case 1:
                    currentTool.Tool = EditorToolType.Brush;
                    break;
                case 2:
                    if (_editor.Mode == EditorMode.Geometry)
                        currentTool.Tool = EditorToolType.Shovel;
                    else
                        currentTool.Tool = EditorToolType.Pencil;
                    break;
                case 3:
                    if (_editor.Mode == EditorMode.Geometry)
                        currentTool.Tool = EditorToolType.Pencil;
                    else
                        currentTool.Tool = EditorToolType.Fill;
                    break;
                case 4:
                    if (_editor.Mode == EditorMode.Geometry)
                        currentTool.Tool = EditorToolType.Flatten;
                    else
                        currentTool.Tool = EditorToolType.Group;
                    break;
                case 5:
                    if (_editor.Mode == EditorMode.Geometry)
                        currentTool.Tool = EditorToolType.Smooth;
                    else
                        currentTool.TextureUVFixer = !currentTool.TextureUVFixer;
                    break;
                default:
                    currentTool.Tool = (EditorToolType)(toolIndex + 3);
                    break;
            }
            SwitchTool(currentTool);
        }
    }
}
