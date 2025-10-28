using System.Linq;
using UnityEngine;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // BodyAsteroid/Vessel picker dialog
        private bool _showBodyAsteroidVesselDialog = false;
        private StepNode _bodyAsteroidTargetNode = null;
        private Vector2 _bodyAsteroidScroll;
        private string _bodyAsteroidFilter = "";

        internal enum BodyAsteroidVessel { none, body, asteroid, vessel };

        BodyAsteroidVessel selectionType;
        private void OpenBodyAsteroidVesselPicker(StepNode target, BodyAsteroidVessel bodyAsteroid)
        {
            _bodyAsteroidTargetNode = target;
            _bodyAsteroidFilter = "";
            _showBodyAsteroidVesselDialog = true;
            selectionType = bodyAsteroid;

            var mp = Input.mousePosition;
            _bodyAsteroidVesselRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _bodyAsteroidVesselRect.width - 40);
            _bodyAsteroidVesselRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _bodyAsteroidVesselRect.height - 40);
        }

        private void DrawBodyAsteroidVesselPickerWindow(int id)
        {
            if (_bodyAsteroidTargetNode == null) { _showBodyAsteroidVesselDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label("Search", GUILayout.Width(60));
            _bodyAsteroidFilter = GUILayout.TextField(_bodyAsteroidFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            _bodyAsteroidScroll = GUILayout.BeginScrollView(_bodyAsteroidScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            int lines = 0;
            switch (selectionType)
            {
                case BodyAsteroidVessel.body:
                    {
                        lines = FlightGlobals.Bodies.Count();
                        foreach (var b in FlightGlobals.Bodies)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(b.bodyName, GUILayout.Width(320));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Choose", GUILayout.Width(80)))
                            {
                                var s = _bodyAsteroidTargetNode.data;
                                s.bodyAsteroidVessel = b.bodyName;
                                _showBodyAsteroidVesselDialog = false;
                                _bodyAsteroidTargetNode = null;
                                TrySaveToDisk_Internal(true);
                            }
                            GUILayout.EndHorizontal();

                        }
                    }
                    break;
                case BodyAsteroidVessel.asteroid:
                    {
                        foreach (var v in BodyAndAsteroidUtils.GetAsteroidSummary())
                        {
                            lines++;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(v, GUILayout.Width(320));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Choose", GUILayout.Width(80)))
                            {
                                var s = _bodyAsteroidTargetNode.data;
                                s.bodyAsteroidVessel = v;
                                _showBodyAsteroidVesselDialog = false;
                                _bodyAsteroidTargetNode = null;
                                TrySaveToDisk_Internal(true);
                            }
                            GUILayout.EndHorizontal();

                        }
                    }
                    break;
                case BodyAsteroidVessel.vessel:
                    {
                        foreach (var v in FlightGlobals.Vessels
                            .Where(v => v != null && v.vesselType >= VesselType.Probe && v.vesselType <= VesselType.Base))
                        {
                            lines++;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(v.vesselName, GUILayout.Width(320));
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Choose", GUILayout.Width(80)))
                            {
                                var s = _bodyAsteroidTargetNode.data;
                                s.bodyAsteroidVessel = v.vesselName;
                                _showBodyAsteroidVesselDialog = false;
                                _bodyAsteroidTargetNode = null;
                                TrySaveToDisk_Internal(true);
                            }
                            GUILayout.EndHorizontal();

                        }
                    }
                    break;
                default:
                    break;
            }
            if (lines == 0)
            {
                GUILayout.Label("No bodyAsteroids loaded.", _tinyLabel);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                _showBodyAsteroidVesselDialog = false;
                _bodyAsteroidTargetNode = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}