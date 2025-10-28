using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {

        /// <summary>
        /// Returns all resource definitions currently loaded by KSP1.
        /// </summary>
        public static List<PartResourceDefinition> GetAllResources()
        {
            var list = new List<PartResourceDefinition>();
            var lib = PartResourceLibrary.Instance;
            if (lib == null) return list;

            // PartResourceDefinitionList implements IEnumerable<PartResourceDefinition>
            foreach (var def in lib.resourceDefinitions)
            {
                if (def != null) list.Add(def);
            }
            return list;
        }

        private void OpenResourcePicker(StepNode target)
        {
            _resourceTargetNode = target;
            _resourceFilter = "";
            _showResourceDialog = true;

            var mp = Input.mousePosition;
            _resourceRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _resourceRect.width - 40);
            _resourceRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _resourceRect.height - 40);
        }

        private void DrawResourcePickerWindow(int id)
        {
            if (_resourceTargetNode == null) { _showResourceDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Available only", GUILayout.Width(110));
            GUILayout.Space(12);
            GUILayout.Label("Search", GUILayout.Width(60));
            _resourceFilter = GUILayout.TextField(_resourceFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
#if false
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                _showResourceDialog = false;
                _resourceTargetNode = null;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
#endif
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            _resourceScroll = GUILayout.BeginScrollView(_resourceScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            List<PartResourceDefinition> list = GetAllResources();
            if (list != null)
            {
                foreach (var ap in list)
                {
                    if (ap == null) continue;

                    if (!String.IsNullOrEmpty(_resourceFilter))
                    {
                        var f = _resourceFilter.Trim();
                        if (!(ap.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(ap.name, GUILayout.Width(320));
                    GUILayout.Label("[" + ap.GetShortName() + "]", _tinyLabel, GUILayout.Width(160));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Choose", GUILayout.Width(80)))
                    {
                        var s = _resourceTargetNode.data;
                        s.resourceName = ap.name;
                        _showResourceDialog = false;
                        _resourceTargetNode = null;
                        TrySaveToDisk_Internal(true);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No resources loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                _showResourceDialog = false;
                _resourceTargetNode = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}