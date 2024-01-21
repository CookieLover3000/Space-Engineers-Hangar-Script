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
        public enum DoorState
        {
            Opening,
            Open,
            Closing,
            Closed
        }
        public class Door
        {
            private List<IMyDoor> hangarDoors;
            public int Id { get; set; }
            public DoorState state;
            public Door(List<IMyDoor> hangarDoors, int Id)
            {
                this.hangarDoors = hangarDoors;
                this.Id = Id;
            }

            public void Open()
            {
                foreach (IMyDoor door in hangarDoors)
                    door.OpenDoor();
                state = DoorState.Open;
            }

            public void Close()
            {
                foreach (IMyDoor door in hangarDoors)
                    door.CloseDoor();
                state = DoorState.Closed;
            }
        }
    }
}
