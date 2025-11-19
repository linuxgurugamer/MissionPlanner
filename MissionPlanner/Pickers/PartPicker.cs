using System;
using UnityEngine;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        private void OpenPartPicker(StepNode target, bool availableOnly)
        {
            _partTargetNode = target;
            _partAvailableOnly = availableOnly;
            _partFilter = "";
            _showPartDialog = true;

            var mp = Input.mousePosition;
            _partRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _partRect.width - 40);
            _partRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _partRect.height - 40);
        }

        private void DrawPartPickerWindow(int id)
        {
            if (_partTargetNode == null) { _showPartDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                _partAvailableOnly = GUILayout.Toggle(_partAvailableOnly, GUIContent.none, ScaledGUILayoutWidth(22));
                GUILayout.Label("Available only", ScaledGUILayoutWidth(110));
                GUILayout.Space(12);
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                _partFilter = GUILayout.TextField(_partFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            _partScroll = GUILayout.BeginScrollView(_partScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));

            var list = PartLoader.LoadedPartsList;
            if (list != null)
            {
                foreach (var ap in list)
                {
                    if (ap == null) continue;
                    if (IsBannedPart(ap)) continue;
                    if (_partAvailableOnly && !IsPartAvailable(ap)) continue;

                    if (!String.IsNullOrEmpty(_partFilter))
                    {
                        var f = _partFilter.Trim();
                        if (!(ap.title.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 ||
                              ap.name.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(ap.title, ScaledGUILayoutWidth(320)))
                        {
                            var s = _partTargetNode.data;
                            s.partName = ap.name;
                            s.partTitle = ap.title;
                            s.partOnlyAvailable = _partAvailableOnly;
                            _showPartDialog = false;
                            _partTargetNode = null;
                            TrySaveToDisk_Internal(true);
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            else
            {
                GUILayout.Label("No parts loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showPartDialog = false;
                    _partTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public static bool IsBannedPart(AvailablePart ap)
        {
            if (ap == null) return true;
            string n = (ap.name ?? "").ToLowerInvariant();
            if (n.Contains("kerbaleva")) return true;
            if (n == "flag" || n.Contains("_flag")) return true;
            if (n.Contains("potato")) return true;
            if (n.Contains("asteroid")) return true;
            if (n.Contains("mumech.mj2.pod")) return true;
            if (n.Contains("mumech_mj2_pod")) return true;


            return false;
        }

        private bool IsPartAvailable(AvailablePart ap)
        {
            try
            {
                if (ResearchAndDevelopment.Instance != null)
                {
                    if (!ResearchAndDevelopment.PartTechAvailable(ap)) return false;
                    return ResearchAndDevelopment.PartModelPurchased(ap);
                }
                return true;
            }
            catch { return true; }
        }



    }
}