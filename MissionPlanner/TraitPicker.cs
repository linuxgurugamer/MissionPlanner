using Experience;
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
        // Trait picker dialog
        private bool _showTraitDialog = false;
        private StepNode _traitTargetNode = null;
        private Vector2 _traitScroll;
        private string _traitFilter = "";

        private void OpenTraitPicker(StepNode target)
        {
            _traitTargetNode = target;
            _traitFilter = "";
            _showTraitDialog = true;

            var mp = Input.mousePosition;
            _traitRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _traitRect.width - 40);
            _traitRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _traitRect.height - 40);
        }

        private void DrawTraitPickerWindow(int id)
        {
            if (_traitTargetNode == null) { _showResourceDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Available only", GUILayout.Width(110));
            GUILayout.Space(12);
            GUILayout.Label("Search", GUILayout.Width(60));
            _traitFilter = GUILayout.TextField(_traitFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
#if false
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                _showResourceDialog = false;
                _traitTargetNode = null;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
#endif
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            _traitScroll = GUILayout.BeginScrollView(_traitScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            var traits = TraitUtil.traits;

            if (traits != null)
            {
                foreach (var trait in traits)
                {
                    if (trait == null) continue;
                    if (IsBannedTrait(trait)) continue;

                    if (!String.IsNullOrEmpty(_traitFilter))
                    {
                        var f = _traitFilter.Trim();
                        if (!(trait.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(trait, GUILayout.Width(320));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Choose", GUILayout.Width(80)))
                    {
                        var s = _traitTargetNode.data;
                        s.traitName = trait;
                        _showTraitDialog = false;
                        _traitTargetNode = null;
                        TrySaveToDisk_Internal(true);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No traits loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                _showTraitDialog = false;
                _traitTargetNode = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private bool IsBannedTrait(string pm)
        {
            if (pm == null) return true;
            string n = (pm ?? "").ToLowerInvariant();
            if (n.Contains("kerbaleva")) return true;
            return false;
        }

    }
}