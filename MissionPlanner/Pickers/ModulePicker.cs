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
        private bool _showModuleDialog = false;
        private StepNode _moduleTargetNode = null;
        private Vector2 _moduleScroll;
        private string _moduleFilter = "";

        private void OpenModulePicker(StepNode target)
        {
            _moduleTargetNode = target;
            _moduleFilter = "";
            _showModuleDialog = true;

            var mp = Input.mousePosition;
            _moduleRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _moduleRect.width - 40);
            _moduleRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _moduleRect.height - 40);
        }

        private void DrawModulePickerWindow(int id)
        {
            if (_moduleTargetNode == null) { _showModuleDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                _moduleFilter = GUILayout.TextField(_moduleFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            _moduleScroll = GUILayout.BeginScrollView(_moduleScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            if (ListAllModules.uniqueModules != null)
            {
                foreach (var pm in ListAllModules.uniqueModules)
                {
                    if (pm == null) continue;
                    if (IsBannedModule(pm)) continue;

                    if (!String.IsNullOrEmpty(_moduleFilter))
                    {
                        var f = _moduleFilter.Trim();
                        if (!(pm.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(pm, ScaledGUILayoutWidth(320)))
                        {
                            var s = _moduleTargetNode.data;
                            s.moduleName = pm;
                            _showModuleDialog = false;
                            _moduleTargetNode = null;
                            TrySaveToDisk_Internal(true);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else
            {
                GUILayout.Label("No modules loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showModuleDialog = false;
                    _moduleTargetNode = null;
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