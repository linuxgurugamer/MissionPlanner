#if false
using UnityEngine;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // Resource picker dialog

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
            if (_resourceTargetNode == null)
            {
                _showResourceDialog = false;
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Available only", ScaledGUILayoutWidth(110));
                GUILayout.Space(12);
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                _resourceFilter = GUILayout.TextField(_resourceFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            _resourceScroll = GUILayout.BeginScrollView(_resourceScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            for (int i = 0; i < ResourceStrings.Length; i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(ResourceStrings[i], ScaledGUILayoutWidth(320)))
                    {
                        var s = _resourceTargetNode.data;
                        s.resourceName = ResourceStrings[i];
                        _showResourceDialog = false;
                        _resourceTargetNode = null;
                        TrySaveToDisk_Internal(true);
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showResourceDialog = false;
                    _resourceTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
#endif