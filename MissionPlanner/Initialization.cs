using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static MissionPlanner.RegisterToolbar;


namespace MissionPlanner
{
    internal class Initialization
    {
        static public ChecklistSystem checklistSystem;

        internal static void Initialize()
        {
            if (checklistSystem == null)
            {
                checklistSystem = new ChecklistSystem();

                string path = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", MissionPlanner.HierarchicalStepsWindow.SAVE_MOD_FOLDER, "checklist.cfg");

                if (File.Exists(path))
                    Checklist.cfg = ConfigNode.Load(path);
                else
                    Log.Info($"Path {path} not found");
                Checklist.cfgLoaded = true;
                checklistSystem.LoadChecklists();
            }
        }
    }
}
