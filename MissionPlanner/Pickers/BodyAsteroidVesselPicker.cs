using System;
using System.Linq;
using UnityEngine;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // BodyAsteroid/Vessel picker dialog
        private bool _showBodyAsteroidVesselDialog = false;
        private StepNode _bodyAsteroidTargetNode = null;
        private Vector2 _bodyAsteroidScroll;
        private string _bodyAsteroidFilter = "";

        internal enum BodyAsteroidVessel { none, body, asteroid, vessel, trackedVessel };

        BodyAsteroidVessel selectionType;
        private void OpenBodyAsteroidVesselPicker(StepNode target, BodyAsteroidVessel bodyAsteroidVessel)
        {
            _bodyAsteroidTargetNode = target;
            _bodyAsteroidFilter = "";
            _showBodyAsteroidVesselDialog = true;
            selectionType = bodyAsteroidVessel;

            var mp = Input.mousePosition;
            _bodyAsteroidVesselRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _bodyAsteroidVesselRect.width - 40);
            _bodyAsteroidVesselRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _bodyAsteroidVesselRect.height - 40);
        }

        private void DrawBodyAsteroidVesselPickerWindow(int id)
        {
            BringWindowForward(id, true);
            if (_bodyAsteroidTargetNode == null) { _showBodyAsteroidVesselDialog = false; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                GUILayout.Label("Search:", ScaledGUILayoutWidth(60));
                _bodyAsteroidFilter = GUILayout.TextField(_bodyAsteroidFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();
            }

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
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(b.bodyName, ScaledGUILayoutWidth(320)))
                                {
                                    var s = _bodyAsteroidTargetNode.data;
                                    s.destBody = b.bodyName;
                                    s.flagBody = b.bodyName;
                                    _showBodyAsteroidVesselDialog = false;
                                    _bodyAsteroidTargetNode = null;
                                    TrySaveToDisk_Internal(true);
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                    break;
                case BodyAsteroidVessel.asteroid:
                    {
                        foreach (var v in Utils.BodyAndAsteroidUtils.GetAsteroidSummary())
                        {
                            lines++;
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(v, ScaledGUILayoutWidth(320)))
                                {
                                    var s = _bodyAsteroidTargetNode.data;
                                    s.destAsteroid = v;
                                    _showBodyAsteroidVesselDialog = false;
                                    _bodyAsteroidTargetNode = null;
                                    TrySaveToDisk_Internal(true);
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                    break;
                case BodyAsteroidVessel.trackedVessel:
                    {
                        var s = _bodyAsteroidTargetNode.data;
                        string oldTrackedVessel = s.trackedVessel;
                        Guid oldVesselGuid = s.vesselGuid;

                        foreach (var v in FlightGlobals.Vessels
                            .Where(v => v != null && v.vesselType >= VesselType.Probe && v.vesselType <= VesselType.Base))
                        {
                            lines++;
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button(v.vesselName, ScaledGUILayoutWidth(320)))
                                {
                                    s.trackedVessel = v.vesselName;
                                    s.vesselGuid = v.id;
                                    _showBodyAsteroidVesselDialog = false;
                                    _bodyAsteroidTargetNode = null;
                                    TrySaveToDisk_Internal(true);
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }

                        if (oldTrackedVessel != s.trackedVessel)
                        {
                            // Create a file for teh vessel with the mission file name in it
                            MissionVisitTracker.SaveTrackedVesselMission(s.vesselGuid, _missionName);
                            // delete any file for the vessel using the oldTrackedVessel
                            if (!string.IsNullOrEmpty(oldTrackedVessel))
                                MissionVisitTracker.DeleteTrackedVesselMission(oldVesselGuid);
                        }
                    }
                    break;

                case BodyAsteroidVessel.vessel:
                    {
                        foreach (var v in FlightGlobals.Vessels
                            .Where(v => v != null && v.vesselType >= VesselType.Probe && v.vesselType <= VesselType.Base))
                        {
                            lines++;
                            using (new GUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button(v.vesselName, ScaledGUILayoutWidth(320)))
                                {
                                    var s = _bodyAsteroidTargetNode.data;
                                    s.destVessel = v.vesselName;
                                    s.vesselGuid = v.id;
                                    _showBodyAsteroidVesselDialog = false;
                                    _bodyAsteroidTargetNode = null;
                                    TrySaveToDisk_Internal(true);
                                }
                            }
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

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showBodyAsteroidVesselDialog = false;
                    _bodyAsteroidTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
