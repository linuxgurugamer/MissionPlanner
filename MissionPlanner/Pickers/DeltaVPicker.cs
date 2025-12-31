using MissionPlanner.Utils;
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
        bool showDeltaVDialog = false;
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
            public string sortOrder;
        }

        static List<DeltaV> DeltaVDict = new List<DeltaV>();

        static bool deltaVloaded = false;
        static string loadedDeltaVTable = "";

        static public bool deltaVTableAvailable = false;
        private static string GetDeltaVFileAbsolute(string planetPack) { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", DELTA_V_FOLDER, planetPack + ".csv"); }

        static string packName = "";
        static int packIndex = 0;

        public static string[] GetCsvFileNamesWithoutExtension(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Directory path is null or empty.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            string[] files = Directory.GetFiles(directoryPath, "*.csv", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }

            return files;
        }

        public static bool DeltaVTableAvailable()
        {
            PlanetPackInfo packInfo = PlanetPackHeuristics.GetPlanetPackInfo();

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

        bool SetSelectedDeltaV(float dv)
        {
            if (dv > 0)
            {
                if (GUILayout.Button(dv.ToString("F0"), ScaledGUILayoutWidth(90)))
                {
                    deltaVTargetNode.data.deltaV = dv * (1 + pad / 100);
                    return true;
                }
            }
            else
            {
                GUILayout.Space(90 + 4);
            }
            return false;
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
                            isMoon = bool.Parse(cols[11].Trim()),
                            sortOrder = cols[12].Trim()
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
            deltaVTargetNode = target;
            deltaVFilter = "";
            showDeltaVDialog = true;

            pad = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().defaultPadding;
            LoadDeltaV(packName);
            var mp = Input.mousePosition;
            deltaVRect.x = Mathf.Clamp(mp.x, 40, Screen.width - deltaVRect.width - 40);
            deltaVRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - deltaVRect.height - 40);
        }

        float pad = 0f;
        string[] availablePlanetPackCSVs = null;
        private void DrawDeltaVPickerWindow(int id)
        {
            if (deltaVTargetNode == null)
            {
                showDeltaVDialog = false;
                return;
            }
            if (availablePlanetPackCSVs == null)
            {
                availablePlanetPackCSVs = GetCsvFileNamesWithoutExtension("GameData/"+DELTA_V_FOLDER);
                for (packIndex = 0; packIndex < availablePlanetPackCSVs.Length; packIndex++)
                    if (packName == availablePlanetPackCSVs[packIndex])
                        break;
            }
            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Planet Pack: ");
                int oldPackIndex = packIndex;
                packIndex = ComboBox.Box(PLANETPACK_COMBO, packIndex, availablePlanetPackCSVs, this, 200, false);
                if (oldPackIndex != packIndex)
                {
                    packName = availablePlanetPackCSVs[packIndex];
                    LoadDeltaV(packName);
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Start/Dest Filter:", ScaledGUILayoutWidth(120));
                deltaVFilter = GUILayout.TextField(deltaVFilter ?? "", GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                GUILayout.FlexibleSpace();

                pad = FloatField("Increase selected Delta V by this percentage", pad, 0, false, " %", width: 40, flex: false);
                pad = GUILayout.HorizontalSlider(pad, 0, 100, ScaledGUILayoutWidth(240));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Start", ScaledGUILayoutWidth(60)))
                {
                    StableSortByOrigin(DeltaVDict);
                }
                GUILayout.Space(20);
                if (GUILayout.Button("Dest", ScaledGUILayoutWidth(90)))
                {
                    StableSortByDestination(DeltaVDict);
                }
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
            deltaVScroll = GUILayout.BeginScrollView(deltaVScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var dv in DeltaVDict)
            {
                if (dv.Destination.StartsWith(deltaVFilter, StringComparison.OrdinalIgnoreCase) ||
                    dv.Origin.StartsWith(deltaVFilter, StringComparison.OrdinalIgnoreCase))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label(dv.Origin, ScaledGUILayoutWidth(60));
                        GUILayout.Space(20);
                        GUILayout.Label(dv.Destination, ScaledGUILayoutWidth(90));
                        GUILayout.Space(20);

                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.dV_to_low_orbit))
                            showDeltaVDialog = false;

                        GUILayout.Space(4);
                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.injection_dV))
                            showDeltaVDialog = false;
                        GUILayout.Space(4);
                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.capture_dV))
                            showDeltaVDialog = false;
                        GUILayout.Space(4);

                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.transfer_to_low_orbit_dV))
                            showDeltaVDialog = false;
                        GUILayout.Space(4);

                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.total_capture_dV))
                            showDeltaVDialog = false;
                        GUILayout.Space(4);

                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.dV_low_orbit_to_surface))
                            showDeltaVDialog = false;
                        GUILayout.Space(4);

                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.ascent_dV))
                            showDeltaVDialog = false;
                        GUILayout.Space(4);

                        if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().closeDvPickerAfterClick &&
                            SetSelectedDeltaV(dv.plane_change_dV))
                            showDeltaVDialog = false;
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    showDeltaVDialog = false;
                    deltaVTargetNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public void StableSortByOrigin(List<DeltaV> list)
        {
            if (list == null || list.Count <= 1)
                return;

            // Associate each element with its original index
            var indexed = list
                .Select((item, index) => new { item, index })
                .ToList();

            indexed.Sort((a, b) =>
            {
                int cmp = string.Compare(a.item.Origin, b.item.Origin,
                                         StringComparison.OrdinalIgnoreCase);

                if (cmp != 0)
                    return cmp;

                // Stable fallback: preserve original order
                return a.index.CompareTo(b.index);
            });

            // Write back in sorted order
            for (int i = 0; i < indexed.Count; i++)
                list[i] = indexed[i].item;
        }

        public static void StableSortByDestination(List<DeltaV> list)
        {
            if (list == null || list.Count <= 1)
                return;

            var indexed = list
                .Select((item, index) => new { item, index })
                .ToList();

            indexed.Sort((a, b) =>
            {
                int cmp = string.Compare(a.item.Destination, b.item.Destination,
                                         StringComparison.OrdinalIgnoreCase);

                if (cmp != 0)
                    return cmp;

                // Stable fallback
                return a.index.CompareTo(b.index);
            });

            for (int i = 0; i < indexed.Count; i++)
                list[i] = indexed[i].item;
        }


    }
}
