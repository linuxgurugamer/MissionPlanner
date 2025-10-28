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

            GUILayout.BeginHorizontal();
            GUILayout.Label("Available only", GUILayout.Width(110));
            _partAvailableOnly = GUILayout.Toggle(_partAvailableOnly, GUIContent.none, GUILayout.Width(22));
            GUILayout.Space(12);
            GUILayout.Label("Search", GUILayout.Width(60));
            _partFilter = GUILayout.TextField(_partFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
#if false
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                _showPartDialog = false;
                _partTargetNode = null;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
#endif
            GUILayout.EndHorizontal();

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

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(ap.title, GUILayout.Width(320));
                    GUILayout.Label("[" + ap.name + "]", _tinyLabel, GUILayout.Width(160));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Choose", GUILayout.Width(80)))
                    {
                        var s = _partTargetNode.data;
                        s.partName = ap.name;
                        s.partTitle = ap.title;
                        s.partOnlyAvailable = _partAvailableOnly;
                        _showPartDialog = false;
                        _partTargetNode = null;
                        TrySaveToDisk_Internal(true);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("No parts loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                _showPartDialog = false;
                _partTargetNode = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private bool IsBannedPart(AvailablePart ap)
        {
            if (ap == null) return true;
            string n = (ap.name ?? "").ToLowerInvariant();
            if (n.Contains("kerbaleva")) return true;
            if (n == "flag" || n.Contains("_flag")) return true;
            if (n.Contains("potato")) return true;
            if (n.Contains("asteroid")) return true;
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