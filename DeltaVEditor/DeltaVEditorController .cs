using MissionPlanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace DeltaVEditor
{
    public class DeltaVEditorController : MonoBehaviour
    {

        static internal string packName = "";
        static internal bool usePack = false;

        private void Update()
        {
            if (MissionPlanner.HierarchicalStepsWindow.openDeltaVEditor)
            {
                DeltaVEditorWindow.Toggle();
                MissionPlanner.HierarchicalStepsWindow.openDeltaVEditor = false;
            }

            // e.g. press Alt+D to toggle
            //if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.D))
            //{
            //    DeltaVEditorWindow.Toggle();
            //}
        }

        private void Start()
        {
            var packInfo = PlanetPackHeuristics.GetPlanetPackInfo();

            Log.Info("Planet pack detected: " + packInfo);
            if (packInfo.Kind == PlanetPackKind.CustomSinglePack)
                packName = packInfo.FolderName;
            else
                packName = packInfo.Kind.ToString();

            Log.Info("DeltaV Planet Pack: " + packName);

        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class DeltaVEditorAddon : DeltaVEditorController { }

}
