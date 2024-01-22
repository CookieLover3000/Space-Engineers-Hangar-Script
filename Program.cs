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
using System.Windows.Input;
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
    partial class Program : MyGridProgram
    {
        string specialTankID = "Special Oxygen Tank";
        string hangarID = "[Hangar ";

        int maxHangarAmount = 10;

        //==============================//
        //   DON'T TOUCH BEYOND THIS POINT   //
        //==============================//

        MyCommandLine _commandLine = new MyCommandLine();
        Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        bool running;

        List<Hangar> Hangars = new List<Hangar>();
        List<Hangar> HangarsInProgress = new List<Hangar>();

        public Program()
        {
            _commands["Open"] = Open;
            _commands["Close"] = Close;
            _commands["Toggle"] = Toggle;
            CreateHangars();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ParseData(argument);
            Running();
        }

        public void ParseData(string argument) // Parse the arguments that are given to the programmable blcok.
        {
            if (_commandLine.TryParse(argument))
            {
                Action commandAction;

                // Retrieve the first argument. Switches are ignored.
                string command = _commandLine.Argument(0);

                // Now we must validate that the first argument is actually specified,
                // then attempt to find the matching command delegate.
                if (command == null)
                {
                    Echo("No command specified");
                }
                else if (_commands.TryGetValue(_commandLine.Argument(0), out commandAction))
                {
                    // We have found a command. Invoke it.
                    commandAction();
                }
                else
                {
                    Echo($"Unknown command {command}");
                }
            }
        }

        public void Running()
        {
            // something somewhere is doing something.
            if (running)
            {
                // change runtime to run the script again.
                Runtime.UpdateFrequency = UpdateFrequency.Update10;

                // all the hangars that are done and can be removed will be added here
                List<Hangar> hangarsToRemove = new List<Hangar>();

                foreach (Hangar hangar in HangarsInProgress)
                {
                    if (hangar.State == HangarState.Running)
                        hangar.Running(); // keep running
                    else if (hangar.State == HangarState.Idle)
                        hangarsToRemove.Add(hangar); // hangar is done
                }

                // remove hangars that are done from in progress
                foreach (Hangar hangarToRemove in hangarsToRemove)
                    HangarsInProgress.Remove(hangarToRemove);

                // We're done! Script can stop running until it's called again.
                if (HangarsInProgress.Count == 0)
                {
                    running = false;
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    Echo("Done!");
                }
            }
        }

        public void Toggle()
        {
            // we're running the script
            running = true;

            // user arguments
            string hangarSpecifier = _commandLine.Argument(1);
            string doorSpecifier = _commandLine.Argument(2);

            // ints to store ID's
            int hangarID = -1;
            int doorID = -1;

            if (hangarSpecifier == null)
            {
                Echo("Please specify a hangar to open.");
                return;
            }
            else if (doorSpecifier == null)
            {
                Echo($"Please specify a door to open for Hangar {hangarSpecifier}.");
                return;
            }

            // Check if you want to open all Hangars.
            bool allHangars = string.Equals(hangarSpecifier, "all", StringComparison.OrdinalIgnoreCase);
            // Check if you want to open all Doors.
            bool allDoors = string.Equals(doorSpecifier, "all", StringComparison.OrdinalIgnoreCase);

            // here to stop program from trying to find hangarID when all hangars need to Toggle
            if (allHangars)
                Echo($"Toggling all hangars");
            // try to get the hangar number from the argument
            else if (int.TryParse(hangarSpecifier, out hangarID) == false)
            {
                Echo($"Cannot find Hangar {hangarSpecifier}. Please try again.");
                return;
            }
            // here to stop program from trying to find doorID when all doors need to Toggle
            if (allDoors)
                Echo($"Toggling all doors for hangar {hangarID}");
            // try to get the door number from the argument
            else if (int.TryParse(doorSpecifier, out doorID) == false)
            {
                if (hangarID != -1)
                    Echo($"Cannot find Door {doorSpecifier} from hangar {hangarID}. Please try again.");
                else
                    Echo($"Cannot find Door {doorSpecifier} from unknown hangar. Please try again.");
                return;
            }
            Echo($"door ID: {doorID}");

            // toggle all hangars to change
            if (allHangars)
            {
                foreach (var hangar in Hangars)
                {
                    // toggle all doors
                    if (allDoors)
                    {
                        hangar.Toggle();
                    }
                    else // toggle specific door
                        hangar.Toggle(doorID);
                    HangarsInProgress.Add(hangar);
                }
                return;
            }

            // find specifc hangar to toggle if allHangars is false
            Hangar specificHangar = Hangars.FirstOrDefault(hangar => hangar.Id == hangarID);
            if (specificHangar != null)
            {
                HangarsInProgress.Add(specificHangar);
                // all doors check
                if (allDoors)
                    specificHangar.Toggle();
                else // single door
                    specificHangar.Toggle(doorID);
            }
        }

        public void Open()
        {
            // we're running the script
            running = true;

            // user arguments
            string hangarSpecifier = _commandLine.Argument(1);
            string doorSpecifier = _commandLine.Argument(2);

            // ints to store ID's
            int hangarID = -1;
            int doorID = -1;

            if (hangarSpecifier == null)
            {
                Echo("Please specify a hangar to open.");
                return;
            }
            else if (doorSpecifier == null)
            {
                Echo($"Please specify a door to open for Hangar {hangarSpecifier}.");
                return;
            }

            // Check if you want to open all Hangars.
            bool allHangars = string.Equals(hangarSpecifier, "all", StringComparison.OrdinalIgnoreCase);
            // Check if you want to open all Doors.
            bool allDoors = string.Equals(doorSpecifier, "all", StringComparison.OrdinalIgnoreCase);

            // here to stop program from trying to find hangarID when all hangars need to Toggle
            if (allHangars)
                Echo($"Opening all hangars");
            // try to get the hangar number from the argument
            else if (int.TryParse(hangarSpecifier, out hangarID) == false)
            {
                Echo($"Cannot find Hangar {hangarSpecifier}. Please try again.");
                return;
            }
            // here to stop program from trying to find doorID when all doors need to Toggle
            if (allDoors)
                Echo($"Opening all doors for hangar {hangarID}");
            // try to get the door number from the argument
            else if (int.TryParse(doorSpecifier, out doorID) == false)
            {
                if (hangarID != -1)
                    Echo($"Cannot find Door {doorSpecifier} from hangar {hangarID}. Please try again.");
                else
                    Echo($"Cannot find Door {doorSpecifier} from unknown hangar. Please try again.");
                return;
            }

            // toggle all hangars to change
            if (allHangars)
            {
                foreach (var hangar in Hangars)
                {
                    // toggle all doors
                    if (allDoors)
                    {
                        hangar.Open();
                    }
                    else // toggle specific door
                        hangar.Open(doorID);
                    HangarsInProgress.Add(hangar);
                }
                return;
            }

            // find specifc hangar to toggle if allHangars is false
            Hangar specificHangar = Hangars.FirstOrDefault(hangar => hangar.Id == hangarID);
            if (specificHangar != null)
            {
                HangarsInProgress.Add(specificHangar);
                // all doors check
                if (allDoors)
                    specificHangar.Open();
                else // single door
                    specificHangar.Open(doorID);
            }
        }

        public void Close()
        {
            // we're running the script
            running = true;

            // user arguments
            string hangarSpecifier = _commandLine.Argument(1);
            string doorSpecifier = _commandLine.Argument(2);

            // ints to store ID's
            int hangarID = -1;
            int doorID = -1;

            if (hangarSpecifier == null)
            {
                Echo("Please specify a hangar to close.");
                return;
            }
            else if (doorSpecifier == null)
            {
                Echo($"Please specify a door to close for Hangar {hangarSpecifier}.");
                return;
            }

            // Check if you want to Close all Hangars.
            bool allHangars = string.Equals(hangarSpecifier, "all", StringComparison.OrdinalIgnoreCase);
            // Check if you want to Close all Doors.
            bool allDoors = string.Equals(doorSpecifier, "all", StringComparison.OrdinalIgnoreCase);

            // here to stop program from trying to find hangarID when all hangars need to Toggle
            if (allHangars)
                Echo($"Closing all hangars");
            // try to get the hangar number from the argument
            else if (int.TryParse(hangarSpecifier, out hangarID) == false)
            {
                Echo($"Cannot find Hangar {hangarSpecifier}. Please try again.");
                return;
            }
            // here to stop program from trying to find doorID when all doors need to Toggle
            if (allDoors)
                Echo($"Closing all doors for hangar {hangarID}");
            // try to get the door number from the argument
            else if (int.TryParse(doorSpecifier, out doorID) == false)
            {
                if (hangarID != -1)
                    Echo($"Cannot find Door {doorSpecifier} from hangar {hangarID}. Please try again.");
                else
                    Echo($"Cannot find Door {doorSpecifier} from unknown hangar. Please try again.");
                return;
            }
            Echo($"door ID: {doorID}");

            // toggle all hangars to change
            if (allHangars)
            {
                foreach (var hangar in Hangars)
                {
                    // toggle all doors
                    if (allDoors)
                    {
                        hangar.Close();
                    }
                    else // toggle specific door
                        hangar.Close(doorID);
                    HangarsInProgress.Add(hangar);
                }
                return;
            }

            // find specifc hangar to toggle if allHangars is false
            Hangar specificHangar = Hangars.FirstOrDefault(hangar => hangar.Id == hangarID);
            if (specificHangar != null)
            {
                HangarsInProgress.Add(specificHangar);
                // all doors check
                if (allDoors)
                    specificHangar.Close();
                else // single door
                    specificHangar.Close(doorID);
            }
        }

        public void CreateHangars()
        {
            int i = 1;
            int id = -1;

            IMyDoor door = null;
            IMyAirVent vent = null;
            IMyGasTank tank = null;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName(hangarID, blocks);

            for (i = 1; i <= maxHangarAmount; i++)
            {
                Echo($"i: {i}");
                List<IMyDoor> doors = new List<IMyDoor>();
                List<IMyAirVent> vents = new List<IMyAirVent>();
                List<IMyGasTank> specialTanks = new List<IMyGasTank>();

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (block.CustomName.Contains(specialTankID))
                    {
                        if (block is IMyGasTank)
                        {
                            tank = block as IMyGasTank;
                            specialTanks.Add(tank);
                        }
                    }
                    if (block.CustomName.Contains(i.ToString() + "]"))
                    {
                        bool hangarExists = Hangars.Any(hangar => hangar.Id == i);
                        if (hangarExists)
                        {
                            Echo($"Block is already part of Hangar {i}. As such this block will be ignored.");
                        }
                        else
                        {
                            id = i;
                            if (block is IMyDoor)
                            {
                                door = block as IMyDoor;
                                doors.Add(door);
                            }
                            else if (block is IMyAirVent)
                            {
                                vent = block as IMyAirVent;
                                vents.Add(vent);
                            }

                        }
                    }
                }
                Echo($"doors Count: {doors.Count}");
                Echo($"vents Count: {vents.Count}");
                Echo($"tank Count: {specialTanks.Count}");
                if (doors.Count > 0 && vents.Count > 0 && specialTanks.Count > 0)
                {
                    Hangars.Add(new Hangar(doors, vents, specialTanks, id, this));
                }
                else
                {
                    Echo($"Could not create Hangar with ID: {i}. Leaving Program.");
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }
            }
        }
    } // cringe wrapper 1
} // cringe wrapper 2
