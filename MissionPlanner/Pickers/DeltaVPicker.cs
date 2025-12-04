using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using static MissionPlanner.RegisterToolbar;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // Resource picker dialog
        bool _showDeltaVDialog = false;
        public class DeltaV
        {
            public string Origin;
            public string Destination;
            public float dV_to_low_orbit;
            public float injection_dV;
            public float capture_dV;
            public float transfer_to_low_orbit_dV;
            public float total_capture_dV;
            public float dV_low_orbit_to_surface;
            public float ascent_dV;
            public float plane_change_dV;

            public bool isMoon;
            public string parent;
        }

        static List<DeltaV> DeltaVDict = new List<DeltaV>();

        static bool deltaVloaded = false;
        static string loadedDeltaVTable = "";

        static public bool deltaVTableAvailable = false;
        private static string GetDeltaVFileAbsolute(string planetPack) { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", DELTA_V_FOLDER, planetPack + ".csv"); }

        static string packName = "";

        public static bool DeltaVTableAvailable()
        {
            var packInfo = PlanetPackHeuristics.GetPlanetPackInfo();

            Log.Info("Planet pack detected: " + packInfo);
            if (packInfo.Kind == PlanetPackKind.CustomSinglePack)
                packName = packInfo.FolderName;
            else
                packName = packInfo.Kind.ToString();

            Log.Info("DeltaV Planet Pack: " + packName);

            if (packName == "Stock" || packName == "PromisedWorlds")
            {
                packName = "Stock";
                return true;
            }
            return File.Exists(GetDeltaVFileAbsolute(packName));
        }

        void SetSelectedDeltaV(float dv)
        {
            if (dv > 0)
            {
                if (GUILayout.Button(dv.ToString("F0"), ScaledGUILayoutWidth(90)))
                {
                    _deltaVTargetNode.data.deltaV = dv * (1 + pad / 100);
                }
            }
            else
            {
                GUILayout.Space(90 + 4);
            }
        }

        public static void LoadDeltaV(string planetPack)
        {
            if (deltaVloaded && loadedDeltaVTable == planetPack)
                return;
            deltaVloaded = true;
            loadedDeltaVTable = planetPack;
            DeltaVDict.Clear();

            string csvPath = GetDeltaVFileAbsolute(planetPack);
            if (!File.Exists(csvPath))
            {
                UnityEngine.Debug.LogError("DeltaV CSV not found: " + planetPack);
            }

            using (var reader = new StreamReader(csvPath))
            {
                bool firstLine = true;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // Skip blank lines or comment lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Skip header line
                    if (firstLine)
                    {
                        firstLine = false;
                        continue;
                    }

                    string[] cols = line.Split(',');

                    if (cols.Length < 6)
                    {
                        UnityEngine.Debug.LogWarning("Invalid CSV row (expected 6 columns): " + line);
                        continue;
                    }

                    try
                    {

                        var dv = new DeltaV
                        {
                            Origin = cols[0].Trim(),
                            Destination = cols[1].Trim(),
                            dV_to_low_orbit = Math.Max(0f, ParseFloat(cols[2])),
                            injection_dV = Math.Max(0f, ParseFloat(cols[3])),
                            capture_dV = Math.Max(0f, ParseFloat(cols[4])),
                            transfer_to_low_orbit_dV = Math.Max(0f, ParseFloat(cols[5])),
                            total_capture_dV = Math.Max(0f, ParseFloat(cols[6])),
                            dV_low_orbit_to_surface = Math.Max(0f, ParseFloat(cols[7])),
                            ascent_dV = Math.Max(0f, ParseFloat(cols[8])),
                            plane_change_dV = Math.Max(0f, ParseFloat(cols[9])),
                            parent = cols[10].Trim(),
                            isMoon = bool.Parse(cols[11].Trim())
                        };

                        if (!string.IsNullOrEmpty(dv.Origin))
                            DeltaVDict.Add(dv);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("Error parsing row: " + line + "\n" + ex);
                    }
                }
            }
        }

        private static float ParseFloat(string s)
        {
            // Allows both "1234.56" and "1234,56" formats
            try
            {
                return float.Parse(
                    s.Trim(),
                    NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                return 0.0f;
            }
        }

        private void OpenDeltaVPicker(StepNode target)
        {
            _deltaVTargetNode = target;
            _deltaVFilter = "";
            _showDeltaVDialog = true;

            pad = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().defaultPadding;
            LoadDeltaV(packName);
            var mp = Input.mousePosition;
            _deltaVRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _deltaVRect.width - 40);
            _deltaVRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _deltaVRect.height - 40);
        }

        float pad = 0f;
        private void DrawDeltaVPickerWindow(int id)
        {
            if (_deltaVTargetNode == null)
            {
                _showDeltaVDialog = false;
                return;
            }
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Planetary Body:", ScaledGUILayoutWidth(110));
                GUILayout.Space(12);
                GUILayout.Label("Filter:", ScaledGUILayoutWidth(60));
                _deltaVFilter = GUILayout.TextField(_deltaVFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();

                pad = FloatField("Pad:", pad, 0, false, " %", width: 40);
            }

            GUILayout.Space(6);
            _deltaVScroll = GUILayout.BeginScrollView(_deltaVScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Start", ScaledGUILayoutWidth(60));
                GUILayout.Space(20);
                GUILayout.Label("Dest", ScaledGUILayoutWidth(90));
                GUILayout.Space(20);
                GUILayout.Label("To Low Orbit", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Injection", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Capture", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Transfer to Low Orbit", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Tot Capture", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Low To Surface", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Ascent", GUILayout.Width(90));
                GUILayout.Space(4);
                GUILayout.Label("Plane Chg", GUILayout.Width(90));
#if false
                GUILayout.Space(4);
                GUILayout.Label("Parent", GUILayout.Width(100));
                GUILayout.Space(4);
                GUILayout.Label("Is Moon", GUILayout.Width(50));
#endif
            }
            foreach (var dv in DeltaVDict.Where(kvp => kvp.Origin.StartsWith(_deltaVFilter)))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(dv.Origin, ScaledGUILayoutWidth(60));
                    GUILayout.Space(20);
                    GUILayout.Label(dv.Destination, ScaledGUILayoutWidth(90));
                    GUILayout.Space(20);

                    SetSelectedDeltaV(dv.dV_to_low_orbit);

                    GUILayout.Space(4);
                    SetSelectedDeltaV(dv.injection_dV);
                    GUILayout.Space(4);
                    SetSelectedDeltaV(dv.capture_dV);
                    GUILayout.Space(4);

                    SetSelectedDeltaV(dv.transfer_to_low_orbit_dV);
                    GUILayout.Space(4);

                    SetSelectedDeltaV(dv.total_capture_dV);
                    GUILayout.Space(4);

                    SetSelectedDeltaV(dv.dV_low_orbit_to_surface);
                    GUILayout.Space(4);

                    SetSelectedDeltaV(dv.ascent_dV);
                    GUILayout.Space(4);

                    SetSelectedDeltaV(dv.plane_change_dV);
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    _showDeltaVDialog = false;
                    _deltaVTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
