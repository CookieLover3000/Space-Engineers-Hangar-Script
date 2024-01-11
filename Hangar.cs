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
    partial class Program
    {
        public enum PressureState
        {
            Depressurizing,
            Depressurized,
            Pressurizing,
            Pressurized,
            Init
        }
        public class Hangar
        {
            private List<IMyAirVent> vents;
            private List<IMyGasTank> specialTanks;
            private List<Door> doors;
            private int id;
            private Program program;
            public PressureState pressurestate;
            private List<IMyGasTank> oxygenTanks;
            private List<IMyGasGenerator> oxygenGenerators;
            public Hangar(List<IMyDoor> hangarDoors, List<IMyAirVent> vents, List<IMyGasTank> specialTanks, int id, Program program)
            {
                this.vents = vents;
                this.specialTanks = specialTanks;
                this.id = id;
                this.program = program;

                // Add all the doors to the hangar.
                int i = 0;
                while (hangarDoors.Count > 0)
                {
                    // increase integer to get new number for door
                    i++;
                    // look for all the hangar doors that are part of a door
                    List<IMyDoor> hangarBlocks = GetAdjacentHangarBlocks(hangarDoors[0]);
                    // remove found hangar doors from hangarDoors
                    foreach (var door in hangarBlocks)
                        hangarDoors.Remove(door);
                    // Create new Door
                    doors.Add(new Door(hangarBlocks, i));
                }
            }

            public void Open()
            {
                if (pressurestate != PressureState.Depressurized)
                    Depressurize();
                else
                {

                }
            }

            public void Close()
            {

            }

            private void Pressurize()
            {
                program.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygenTanks, x => x.DetailedInfo.Split(' ')[1] == "Oxygen");
                program.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);

                if (pressurestate != PressureState.Pressurizing)
                {
                    pressurestate = PressureState.Pressurizing;

                    foreach (var tank in oxygenTanks)
                        tank.Enabled = false;
                    foreach (var generator in oxygenGenerators)
                        generator.Enabled = false;
                    foreach (var tank in specialTanks)
                        tank.Enabled = true;
                    foreach (var vent in vents)
                        vent.Depressurize = false;
                }
                //if (specialTank.FilledRatio < 0.000001f)
                if(specialTanks.All(specialTank => specialTank.FilledRatio < 0.000001f))
                {
                    //program.Echo($"{specialTank} is empty, enabling other tanks");
                    foreach (var tank in oxygenTanks)
                        tank.Enabled = true;
                }

                if (vents[0].Status == VentStatus.Pressurized)
                {
                    pressurestate = PressureState.Pressurized;

                    foreach (var tank in oxygenTanks)
                        tank.Enabled = true;
                    foreach (var generator in oxygenGenerators)
                        generator.Enabled = true;
                foreach (var tank in specialTanks)
                    tank.Enabled = false;
                }
            }

            private void Depressurize()
            {
                program.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygenTanks, x => x.DetailedInfo.Split(' ')[1] == "Oxygen");
                program.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);

                pressurestate = PressureState.Depressurizing;

                if (vents[0].GetOxygenLevel() < 0.000001f)
                    pressurestate = PressureState.Depressurized;

                // Disable oxygen tanks
                foreach (var tank in oxygenTanks)
                    tank.Enabled = false;
                // Disable o2/h2 generators
                foreach (var generator in oxygenGenerators)
                    generator.Enabled = false;
                // Enable the Special tank
                foreach (var tank in specialTanks)
                    tank.Enabled = true;
                // Make the vents in the airlock depressurize.
                foreach (var vent in vents)
                    vent.Depressurize = true;

                if (pressurestate == PressureState.Depressurized)
                {
                    pressurestate = PressureState.Depressurized;

                    // Enable all tanks
                    foreach (var tank in oxygenTanks)
                        tank.Enabled = true;
                    // Enable the Generators
                    foreach (var generator in oxygenGenerators)
                        generator.Enabled = true;
                    // Disable the Special Tank
                    foreach (var tank in specialTanks)
                        tank.Enabled = false;
                }
                //if (specialTank.FilledRatio > 0.999999f)
                if (specialTanks.All(specialTank => specialTank.FilledRatio > 0.999999f))
                {
                    //_program.Echo($"{specialTank} is full, Opening Airlock without depressurizing.");
                    pressurestate = PressureState.Depressurized;
                }
            }

            Vector3I ReturnUpValue(IMyDoor hangarDoor)
            {
                // need to return negative value
                return -Base6Directions.GetIntVector(hangarDoor.Orientation.Up);
            }

            Vector3I ReturnLeftValue(IMyDoor hangarDoor)
            {
                return Base6Directions.GetIntVector(hangarDoor.Orientation.Left);
            }

            Vector3I ReturnRightValue(IMyDoor hangarDoor)
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

                slimBlock = grid.GetCubeBlock(doorPosition + (ReturnUpValue(hangarDoor) * 3));

                if (slimBlock != null)
                {
                    IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                    if (!doors.Contains(adjacentHangarBlock))
                    {
                        doors.Add(adjacentHangarBlock);
                        AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                    }
                }

                slimBlock = grid.GetCubeBlock(doorPosition + (ReturnLeftValue(hangarDoor) + ReturnUpValue(hangarDoor)));
                if (slimBlock != null)
                {
                    IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                    if (!doors.Contains(adjacentHangarBlock))
                    {
                        doors.Add(adjacentHangarBlock);
                        AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                    }
                }

                slimBlock = grid.GetCubeBlock(doorPosition + (ReturnRightValue(hangarDoor) + ReturnUpValue(hangarDoor)));
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
}
