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
        public class Door
        {
            private List<IMyDoor> hangarDoors;
            private int id;
            public Door(List<IMyDoor> hangarDoors, int id)
            {
                this.hangarDoors = hangarDoors;
                this.id = id;
            }

            public void Open()
            {
                foreach (IMyDoor door in hangarDoors)
                    door.OpenDoor();
            }

            public void Close()
            {
                foreach (IMyDoor door in hangarDoors)
                    door.CloseDoor();
            }
        }
    }
}
