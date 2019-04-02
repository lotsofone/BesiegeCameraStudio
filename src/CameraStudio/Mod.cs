using System;
using Modding;
using Modding.Blocks;
using UnityEngine;

namespace CameraStudio
{
    public class Mod : ModEntryPoint
    {
        public static GameObject Instance;
        public override void OnLoad()
        {
            Mod.Instance = new GameObject("CameraStudio Mod");
            UnityEngine.Object.DontDestroyOnLoad(Mod.Instance);
            Mod.Instance.AddComponent<ModMainUI>();

            Events.OnMachineSimulationToggle += MachineSimulationToggle;
        }
        public void MachineSimulationToggle(PlayerMachine machine, bool isStart)
        {
            if (isStart)
            {
            }
            else
            {
            }
        }
    }
}
