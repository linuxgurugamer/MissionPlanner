using MissionPlanner.Utils;
using System;
using UnityEngine;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // SAS picker dialog
        private bool _showCategoryDialog = false;
        private StepNode _CategoryTargetNode = null;
        private Vector2 _CategoryScroll;
        private string _CategoryFilter = "";

        private void OpenCategoryPicker(StepNode target)
        {
            _CategoryTargetNode = target;
            _CategoryFilter = "";
            _showCategoryDialog = true;

            var mp = Input.mousePosition;
            _CategoryRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _CategoryRect.width - 40);
            _CategoryRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _CategoryRect.height - 40);
        }

        private void DrawCategoryPickerWindow(int id)
        {
            if (_CategoryTargetNode == null)
            {
                _showCategoryDialog = false;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Available only", ScaledGUILayoutWidth(110));
                GUILayout.Space(12);
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                _CategoryFilter = GUILayout.TextField(_CategoryFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            var cats = VABOrganizerUtils.SubCategories();
            _CategoryScroll = GUILayout.BeginScrollView(_CategoryScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            for (int i = 0; i < cats.Count; i++)
            {
                if (!String.IsNullOrEmpty(_CategoryFilter))
                {
                    var f = _CategoryFilter.Trim();
                    if (!(cats[i].IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cats[i].IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(StringFormatter.BeautifyName(cats[i]), ScaledGUILayoutWidth(320)))
                    {
                        var s = _CategoryTargetNode.data;
                        s.vabCategory = cats[i];
                        _showCategoryDialog = false;
                        _CategoryTargetNode = null;
                        TrySaveToDisk_Internal(true);
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showCategoryDialog = false;
                    _CategoryTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
