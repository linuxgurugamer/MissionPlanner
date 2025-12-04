
using System;
using System.Collections;
using System.Collections.Generic;

using System.Reflection;
using UnityEngine;
using KSP;
using KSP.IO;


namespace MissionPlanner
{
    // HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().
    public class MissionPlannerSettings : GameParameters.CustomParameterNode
    {
        //Thanks to https://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813

        public override string Title { get { return "Mission Planner & Checklist"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Mission Planner"; } }
        public override string DisplaySection { get { return "Mission Planner"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("Show tooltips")]
        public bool showTooltips = true;

        [GameParameters.CustomParameterUI("Autosave", toolTip ="Automatically saves the mission after every edit")]
        public bool autosave = true;


        [GameParameters.CustomParameterUI("Show Save Indicator")]
        public bool showSaveIndicator = true;

        [GameParameters.CustomFloatParameterUI("Minimum time for landing to be considered Landed (seconds)", minValue = 0.0f, maxValue = 120.0f, displayFormat = "0.0", toolTip = "Will not alter existing contracts")]
        public double minTimeForLanded = 60.0f;

        [GameParameters.CustomIntParameterUI("Max resources for combobox", minValue = 10, maxValue = 25,
            toolTip = "If there are more resources than this value, a selection window will be available")]
        public int maxResourcesInCombo = 15;


        [GameParameters.CustomIntParameterUI("Default padding for Delta V selection", minValue = 0, maxValue = 50,
            toolTip = "The selected Delta V will be increased by this percentage")]
        public int defaultPadding = 15;

        [GameParameters.CustomIntParameterUI("Font size", minValue = 8, maxValue = 20)]
        public int fontSize = 12;

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset) { }

    }

    // HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().
    public class MissionPlannerSettings2 : GameParameters.CustomParameterNode
    {
        //Thanks to https://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813

        public override string Title { get { return "Misc Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Mission Planner"; } }
        public override string DisplaySection { get { return "Mission Planner"; } }
        public override int SectionOrder { get { return 2; } }
        public override bool HasPresets { get { return false; } }


        [GameParameters.CustomParameterUI("Use KSP Skin")]
        public bool useKspSkin = true;

        [GameParameters.CustomParameterUI("Hide on Pause")]
        public bool hideOnPause = false;

        [GameParameters.CustomParameterUI("Enable DeltaV Editor")]
        public bool deltaVEditorActive = false;


        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            return true;
        }

        public override void SetDifficultyPreset(GameParameters.Preset preset) { }

    }

}