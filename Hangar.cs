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
using static IngameScript.Program;

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

        public enum HangarState
        {
            Running,
            Idle
        }

        public class Hangar
        {
            public HangarState State { get; set; }
            public int Id { get; set; }

            private List<IMyAirVent> vents;
            private List<IMyGasTank> specialTanks;
            private List<Door> doors;

            private Program program;
            private PressureState pressurestate;
            private List<IMyGasTank> oxygenTanks;
            private List<IMyGasGenerator> oxygenGenerators;
            private List<Door> doorsInProgress = new List<Door>();

            public Hangar(List<IMyDoor> hangarDoors, List<IMyAirVent> vents, List<IMyGasTank> specialTanks, int id, Program program)
            {
                this.vents = vents;
                this.specialTanks = specialTanks;
                this.Id = id;
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

            public void Running()
            {
                foreach (var door in doors)
                {
                    // Add door to doorsInProgress if it's in progress.
                    if ((door.state == DoorState.Opening || door.state == DoorState.Closing) && !doorsInProgress.Contains(door))
                        doorsInProgress.Add(door);
                }

                // Call Open or Close, depending on state. Close should be impossible.
                foreach (var door in doorsInProgress)
                {
                    if (door.state == DoorState.Opening)
                        Open(door.Id);
                    else if (door.state == DoorState.Closing)
                        Close(door.Id);
                }

                // doors are done, so hangar is idle
                if (doorsInProgress.Count == 0 && (pressurestate == PressureState.Depressurized || pressurestate == PressureState.Pressurized))
                    State = HangarState.Idle;
                else if (pressurestate == PressureState.Depressurizing)
                    Depressurize();
                else if (pressurestate == PressureState.Pressurizing)
                    Pressurize();
            }

            public void Toggle()
            {
                foreach (var specificDoor in doors)
                {
                    if (specificDoor.state == DoorState.Open || specificDoor.state == DoorState.Opening)
                        Close(specificDoor.Id);
                    else if (specificDoor.state == DoorState.Closed || specificDoor.state == DoorState.Closing)
                        Open(specificDoor.Id);
                }
            }

            public void Toggle(int doorID)
            {
                Door specificDoor = doors.FirstOrDefault(door => door.Id == doorID);
                if (specificDoor == null)
                    return;
                if (specificDoor.state == DoorState.Open || specificDoor.state == DoorState.Opening)
                    Close(specificDoor.Id);
                else if (specificDoor.state == DoorState.Closed || specificDoor.state == DoorState.Closing)
                    Open(specificDoor.Id);

            }

            // version of Open for all doors
            public void Open()
            {
                // hangar is doing stuff.
                State = HangarState.Running;
                if (pressurestate != PressureState.Depressurized)
                {
                    Depressurize();
                    foreach (var door in doors)
                        door.state = DoorState.Opening;
                }
                else // hangar is deppressurized
                {
                    foreach (var door in doors)
                    {
                        door.Open();
                        doorsInProgress.Remove(door);
                    }
                }
            }

            // version of Open for single door
            public void Open(int doorID)
            {
                Door specificDoor = doors.FirstOrDefault(door => door.Id == doorID);
                if (specificDoor == null)
                    return;
                // hangar is doing stuff.
                State = HangarState.Running;
                if (pressurestate != PressureState.Depressurized)
                {
                    Depressurize();
                    specificDoor.state = DoorState.Opening;
                }
                else // hangar is deppressurized
                {
                    specificDoor.Open();
                    doorsInProgress.Remove(specificDoor);
                }
            }

            // Version of Close for all doors
            public void Close()
            {
                // hangar is doing stuff.
                State = HangarState.Running;
                foreach (var door in doors)
                    door.Close();
                Pressurize();
            }

            // Version of Close for single door
            public void Close(int doorID)
            {
                Door specificDoor = doors.FirstOrDefault(door => door.Id == doorID);
                if (specificDoor == null)
                    return;
                // hangar is doing stuff.
                State = HangarState.Running;
                specificDoor.Close();
                Pressurize();
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
                if (specialTanks.All(specialTank => specialTank.FilledRatio < 0.000001f))
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

            // return the up value of a hangardoor based on it's rotation
            Vector3I ReturnUpValue(IMyDoor hangarDoor)
            {
                // need to return negative value
                return -Base6Directions.GetIntVector(hangarDoor.Orientation.Up);
            }

            // return the left value of a hangardoor based on it's rotation
            Vector3I ReturnLeftValue(IMyDoor hangarDoor)
            {
                return Base6Directions.GetIntVector(hangarDoor.Orientation.Left);
            }

            // return the right value of a hangardoor based on it's rotation
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
                // get current position of the door
                Vector3I doorPosition = hangarDoor.Position;
                // get current position of door in grid
                IMyCubeGrid grid = hangarDoor.CubeGrid;
                // tbh, still don't know what a slimblock is
                IMySlimBlock slimBlock;

                // check if a door is above the current door
                slimBlock = grid.GetCubeBlock(doorPosition + (ReturnUpValue(hangarDoor) * 3));
                // Found something!
                if (slimBlock != null)
                {
                    IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                    if (!doors.Contains(adjacentHangarBlock))
                    {
                        // Add the door to the list of adjacentdoors
                        doors.Add(adjacentHangarBlock);
                        // look for more doors from the position of the new door
                        AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                    }
                }

                // check if a door is to the left of the current door
                slimBlock = grid.GetCubeBlock(doorPosition + (ReturnLeftValue(hangarDoor) + ReturnUpValue(hangarDoor)));
                // Found something!
                if (slimBlock != null)
                {
                    IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                    if (!doors.Contains(adjacentHangarBlock))
                    {
                        // Add the door to the list of adjacentdoors
                        doors.Add(adjacentHangarBlock);
                        // look for more doors from the position of the new door
                        AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                    }
                }

                // check if a door is to the right of the current door
                slimBlock = grid.GetCubeBlock(doorPosition + (ReturnRightValue(hangarDoor) + ReturnUpValue(hangarDoor)));
                // Found something!
                if (slimBlock != null)
                {
                    IMyDoor adjacentHangarBlock = slimBlock.FatBlock as IMyDoor;
                    if (!doors.Contains(adjacentHangarBlock))
                    {
                        // Add the door to the list of adjacentdoors
                        doors.Add(adjacentHangarBlock);
                        // look for more doors from the position of the new door
                        AddHangarDoorsRecursive(adjacentHangarBlock, doors);
                    }
                }
            }
        } // class bracket
    } // cringe wrapper 1
} // cringe wrapper 2
