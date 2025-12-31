using System;
using UnityEngine;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // Trait picker dialog
        private bool showTraitDialog = false;
        private StepNode traitTargetNode = null;
        private Vector2 traitScroll;
        private string traitFilter = "";

        private void OpenTraitPicker(StepNode target)
        {
            traitTargetNode = target;
            traitFilter = "";
            showTraitDialog = true;

            var mp = Input.mousePosition;
            traitRect.x = Mathf.Clamp(mp.x, 40, Screen.width - traitRect.width - 40);
            traitRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - traitRect.height - 40);
        }

        private void DrawTraitPickerWindow(int id)
        {
            BringWindowForward(id, true);
            if (traitTargetNode == null)
            {
                showTraitDialog = false;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                traitFilter = GUILayout.TextField(traitFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(6);
            traitScroll = GUILayout.BeginScrollView(traitScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            var traits = TraitUtil.traits;

            if (traits != null)
            {
                foreach (var trait in traits)
                {
                    if (trait == null) continue;
                    if (IsBannedTrait(trait)) continue;

                    if (!String.IsNullOrEmpty(traitFilter))
                    {
                        var f = traitFilter.Trim();
                        if (!(trait.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(trait, GUILayout.Width(traitRect.width - 40)))
                        {
                            var s = traitTargetNode.data;
                            s.traitName = trait;
                            showTraitDialog = false;
                            traitTargetNode = null;
                            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                                TrySaveToDisk_Internal(true);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else
            {
                GUILayout.Label("No traits loaded.", tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    showTraitDialog = false;
                    traitTargetNode = null;
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