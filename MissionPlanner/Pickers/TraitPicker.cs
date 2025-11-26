using System;
using UnityEngine;


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
            BringWindowForward(id, true);
            if (_traitTargetNode == null)
            {
                _showTraitDialog = false;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                _traitFilter = GUILayout.TextField(_traitFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }
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

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(trait, GUILayout.Width(_traitRect.width - 40)))
                        {
                            var s = _traitTargetNode.data;
                            s.traitName = trait;
                            _showTraitDialog = false;
                            _traitTargetNode = null;
                            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                                TrySaveToDisk_Internal(true);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else
            {
                GUILayout.Label("No traits loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showTraitDialog = false;
                    _traitTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

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