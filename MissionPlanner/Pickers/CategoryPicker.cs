using MissionPlanner.Utils;
using System;
using UnityEngine;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // SAS picker dialog
        private bool showCategoryDialog = false;
        private StepNode CategoryTargetNode = null;
        private Vector2 CategoryScroll;
        private string CategoryFilter = "";

        private void OpenCategoryPicker(StepNode target)
        {
            CategoryTargetNode = target;
            CategoryFilter = "";
            showCategoryDialog = true;

            var mp = Input.mousePosition;
            CategoryRect.x = Mathf.Clamp(mp.x, 40, Screen.width - CategoryRect.width - 40);
            CategoryRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - CategoryRect.height - 40);
        }

        private void DrawCategoryPickerWindow(int id)
        {
            BringWindowForward(id, true);
            if (CategoryTargetNode == null)
            {
                showCategoryDialog = false;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Available only", ScaledGUILayoutWidth(110));
                GUILayout.Space(12);
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                CategoryFilter = GUILayout.TextField(CategoryFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            var cats = VABOrganizerUtils.SubCategories();
            CategoryScroll = GUILayout.BeginScrollView(CategoryScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            for (int i = 0; i < cats.Count; i++)
            {
                if (!String.IsNullOrEmpty(CategoryFilter))
                {
                    var f = CategoryFilter.Trim();
                    if (!(cats[i].IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cats[i].IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(StringFormatter.BeautifyName(cats[i]), ScaledGUILayoutWidth(320)))
                    {
                        var s = CategoryTargetNode.data;
                        s.vabCategory = cats[i];
                        showCategoryDialog = false;
                        CategoryTargetNode = null;
                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                            TrySaveToDisk_Internal(true);
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    showCategoryDialog = false;
                    CategoryTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
