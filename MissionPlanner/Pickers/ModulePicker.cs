using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // Module picker dialog
        private bool showModuleDialog = false;
        private StepNode moduleTargetNode = null;
        private Vector2 moduleScroll;
        private string moduleFilter = "";

        private void OpenModulePicker(StepNode target)
        {
            moduleTargetNode = target;
            moduleFilter = "";
            showModuleDialog = true;

            var mp = Input.mousePosition;
            moduleRect.x = Mathf.Clamp(mp.x, 40, Screen.width - moduleRect.width - 40);
            moduleRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - moduleRect.height - 40);
        }

        private void DrawModulePickerWindow(int id)
        {
            BringWindowForward(id, true);
            if (moduleTargetNode == null) { showModuleDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                moduleFilter = GUILayout.TextField(moduleFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            moduleScroll = GUILayout.BeginScrollView(moduleScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            if (ListAllModules.uniqueModules != null)
            {
                foreach (var pm in ListAllModules.uniqueModules)
                {
                    if (pm == null) continue;
                    if (IsBannedModule(pm)) continue;

                    if (!String.IsNullOrEmpty(moduleFilter))
                    {
                        var f = moduleFilter.Trim();
                        if (!(pm.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(pm, ScaledGUILayoutWidth(320)))
                        {
                            var s = moduleTargetNode.data;
                            s.moduleName = pm;
                            showModuleDialog = false;
                            moduleTargetNode = null;
                            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                                TrySaveToDisk_Internal(true);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else
            {
                GUILayout.Label("No modules loaded.", tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    showModuleDialog = false;
                    moduleTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private bool IsBannedModule(string pm)
        {
            if (pm == null) return true;
            string n = (pm ?? "").ToLowerInvariant();
            if (n.Contains("kerbaleva")) return true;
            if (n.Contains("flagdecal")) return true;
            if (n.Contains("flagsite")) return true;
            if (n.StartsWith("aya_")) return true;
            if (n.StartsWith("fxmodulelookatconstraint")) return true;
            if (n == "moduleasteroid") return true;
            if (n.Contains("moduleasteroidresource")) return true;
            if (n.Contains("fxmodulelookatconstraint")) return true;
            return false;
        }

    }
}