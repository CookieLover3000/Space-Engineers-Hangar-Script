using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            IMyDoor hangarDoor = GridTerminalSystem.GetBlockWithName("Airtight Hangar Door 10") as IMyDoor;

            if (hangarDoor != null)
            {
                List<IMyDoor> adjacentHangarBlocks = GetAdjacentHangarBlocks(hangarDoor);

                if (adjacentHangarBlocks.Count > 0)
                {
                    Echo("Hangar blocks found:");

                    foreach (var adjacentBlock in adjacentHangarBlocks)
                    {
                        Echo(adjacentBlock.CustomName);
                    }
                }
                else
                {
                    Echo("No hangar blocks adjacent.");
                }
            }
        }

        Vector3I returnUpValue(IMyDoor hangarDoor)
        {
            // need to return negative value
            return -Base6Directions.GetIntVector(hangarDoor.Orientation.Up);
        }

        Vector3I returnLeftValue(IMyDoor hangarDoor)
        {
            return Base6Directions.GetIntVector(hangarDoor.Orientation.Left);
        }

        Vector3I returnRightValue (IMyDoor hangarDoor)
        {
            return -Base6Directions.GetIntVector(hangarDoor.Orientation.Left);
        }

        List<IMyDoor> GetAdjacentHangarBlocks(IMyDoor hangarDoor)
        {
            List<IMyDoor> adjacentHangarBlocks = new List<IMyDoor>();

            AddHangarDoorsRecursive(hangarDoor, adjacentHangarBlocks);

            return adjacentHangarBlocks;
        }

        void AddHangarDoorsRecursive(IMyDoor hangarDoor, List<IMyDoor> doors)
        {
            Vector3I doorPosition = hangarDoor.Position;
            IMyCubeGrid grid = hangarDoor.CubeGrid;
            IMySlimBlock slimBlock;

            slimBlock = grid.GetCubeBlock(doorPosition + (returnUpValue(hangarDoor) * 3));

            if (slimBlock != null)
            {
                IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                if (!doors.Contains(adjacentHangarBlock))
                {
                    doors.Add(adjacentHangarBlock);
                    AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                }
            }

            slimBlock = grid.GetCubeBlock(doorPosition + (returnLeftValue(hangarDoor) + returnUpValue(hangarDoor)));
            if (slimBlock != null)
            {
                IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                if (!doors.Contains(adjacentHangarBlock))
                {
                    doors.Add(adjacentHangarBlock);
                    AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                }
            }

            slimBlock = grid.GetCubeBlock(doorPosition + (returnRightValue(hangarDoor) + returnUpValue(hangarDoor)));
            if (slimBlock != null)
            {
                IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                if (!doors.Contains(adjacentHangarBlock))
                {
                    doors.Add(adjacentHangarBlock);
                    AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                }
            }
        }

        void GetAdjacentHangarBlocksRecursive(IMyDoor currentHangarDoor, List<IMyDoor> result)
        {
            IMyCubeGrid grid = currentHangarDoor.CubeGrid;

            if (grid == null)
                return;

            Vector3I doorPosition = currentHangarDoor.Position;

            // Check to the left and right (X-axis)
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                Vector3I adjacentPosition = new Vector3I(doorPosition.X + xOffset, doorPosition.Y, doorPosition.Z);
                IMySlimBlock adjacentSlimBlock = grid.GetCubeBlock(adjacentPosition);

                if (adjacentSlimBlock != null)
                {
                    IMyDoor adjacentHangarBlock = adjacentSlimBlock.FatBlock as IMyDoor;

                    if (adjacentHangarBlock != null && !result.Contains(adjacentHangarBlock))
                    {
                        result.Add(adjacentHangarBlock);

                        // Recursively check for adjacent hangar doors of the newly found hangar door
                        GetAdjacentHangarBlocksRecursive(adjacentHangarBlock, result);
                    }
                }
            }
        }


    }
}
