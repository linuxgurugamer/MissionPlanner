using UnityEngine;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // SAS picker dialog
        private bool _showSASDialog = false;
        private StepNode _SASTargetNode = null;
        private Vector2 _SASScroll;
        private string _SASFilter = "";

        private void OpenSASPicker(StepNode target)
        {
            _SASTargetNode = target;
            _SASFilter = "";
            _showSASDialog = true;

            var mp = Input.mousePosition;
            _SASRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _SASRect.width - 40);
            _SASRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _SASRect.height - 40);
        }

        private void DrawSASPickerWindow(int id)
        {
            if (_SASTargetNode == null) { _showSASDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Available only", GUILayout.Width(110));
            GUILayout.Space(12);
            GUILayout.Label("Search", GUILayout.Width(60));
            _SASFilter = GUILayout.TextField(_SASFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            _SASScroll = GUILayout.BeginScrollView(_SASScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            for (int i = 0; i < SASUtils.SasLevelDescriptions.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(SASUtils.SasLevelDescriptions[i], GUILayout.Width(320));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Choose", GUILayout.Width(80)))
                {
                    var s = _SASTargetNode.data;
                    s.minSASLevel = i;
                    _showSASDialog = false;
                    _SASTargetNode = null;
                    TrySaveToDisk_Internal(true);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                _showSASDialog = false;
                _SASTargetNode = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}