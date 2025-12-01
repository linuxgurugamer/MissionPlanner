using MissionPlanner.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace DeltaVEditor
{
    public class DeltaVEditorWindow : MonoBehaviour
    {
        public static DeltaVEditorWindow Instance;

        private Rect _windowRect = new Rect(200, 200, 1250, 800);
        private int _windowId;

        private bool _visible = false;

        // Default relative CSV path
        private const string CSVPATH = "GameData/MissionPlanner/PluginData/DeltaVTables/";
        private string _csvPath = CSVPATH;

        private readonly List<DeltaVRowEditor> _rows = new List<DeltaVRowEditor>();
        private Vector2 _scrollPos;

        private string _filterOrigin = "";
        private string _filterDestination = "";

        private string _statusMessage = "";

        // ---- File picker state ----
        private bool _showFilePicker = false;
        private bool _showFileNameEntry = false;
        private bool _show_SaveAsEntry = false;

        string planetPackName = "";
        private Vector2 _fileScrollPos;
        private List<string> _fileOptions = new List<string>();
        private string _pickerDirRelative = "";   // e.g. "GameData/MyMod/Data"

        CelestialBody homeWorld;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            _windowId = GetHashCode();

            homeWorld = FlightGlobals.Bodies.FirstOrDefault(b => b.isHomeWorld);

        }

        private void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            if (!_visible)
                return;

            _windowRect = GUILayout.Window(_windowId, _windowRect, DrawWindow, "Delta-V Editor");
        }

        private void DrawWindow(int id)
        {
            using (new GUILayout.VerticalScope())
            {

                // Path + Load/Save + Browse
                GUILayout.Label("CSV Path (relative to KSP root):");
                using (new GUILayout.HorizontalScope())
                {
                    _csvPath = GUILayout.TextField(_csvPath, GUILayout.MinWidth(400));

                    if (GUILayout.Button("Load...", GUILayout.Width(80)))
                    {
                        _show_SaveAsEntry = _showFileNameEntry = false;
                        ToggleFilePicker();
                    }

                    if (GUILayout.Button("New", GUILayout.Width(80)))
                    {
                        _rows.Clear();
                        _rows.Add(new DeltaVRowEditor("", ""));
                        _showFileNameEntry = true;
                    }

                    if (_csvPath.EndsWith(".csv"))
                    {
                        if (GUILayout.Button("Save", GUILayout.Width(80)))
                        {
                            SaveCsv();
                        }
                    }
                    else
                        if (GUILayout.Button("Name", GUILayout.Width(80)))
                    {
                        _showFileNameEntry = true;
                        planetPackName = "";

                    }
                    if (GUILayout.Button("Save As", GUILayout.Width(80)))
                    {
                        _show_SaveAsEntry = true;
                        planetPackName = "";
                    }
                }

                // File picker UI
                if (_showFilePicker)
                {
                    DrawFilePicker();
                }

                if (_show_SaveAsEntry || _showFileNameEntry)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Enter Planet Pack Name: ");
                        planetPackName = GUILayout.TextField(planetPackName, GUILayout.Width(120));
                    }
                    using (new GUILayout.HorizontalScope())
                    {
                        if (_show_SaveAsEntry)
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Save", GUILayout.Width(90)))
                            {
                                _show_SaveAsEntry = _showFileNameEntry = false;
                                _csvPath = CSVPATH + planetPackName + ".csv";
                                SaveCsv();
                            }
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("OK", GUILayout.Width(90)))
                        {
                            _show_SaveAsEntry = _showFileNameEntry = false;
                            _csvPath = CSVPATH + planetPackName + ".csv";
                        }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                            _show_SaveAsEntry = _showFileNameEntry = false;
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    // Filters
                    using (new GUILayout.HorizontalScope())
                    {
                        DeltaVEditorController.usePack = GUILayout.Toggle(DeltaVEditorController.usePack, "Use Loaded Planet Pack: " + DeltaVEditorController.packName);
                        if (DeltaVEditorController.usePack)
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Populate (homeworld to all)", GUILayout.Width(250)))
                            {
                                PopulateList(FlightGlobals.Bodies, false, false);
                            }
                            if (GUILayout.Button("Populate (all to all)", GUILayout.Width(250)))
                            {
                                PopulateList(FlightGlobals.Bodies, true, true);
                            }
                            if (GUILayout.Button("Populate (all to homeworld)", GUILayout.Width(250)))
                            {
                                PopulateList(FlightGlobals.Bodies, false, true);
                            }
                        }
                    }
                    GUILayout.Space(4);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Filter Origin:");
                        _filterOrigin = GUILayout.TextField(_filterOrigin, GUILayout.Width(150));

                        GUILayout.Label("Filter Destination:");
                        _filterDestination = GUILayout.TextField(_filterDestination, GUILayout.Width(150));
                        GUILayout.FlexibleSpace();
                    }
                    GUILayout.Space(5);

                    // Header row
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        if (GUILayout.Button("Origin", GUILayout.Width(100)))
                        {
                            StableSortByOrigin(_rows);
                        }
                        if (GUILayout.Button("Destination", GUILayout.Width(100)))
                        {
                            StableSortByDestination(_rows);
                        }
                        GUILayout.Label("To Low Orbit", GUILayout.Width(90));
                        GUILayout.Label("Injection", GUILayout.Width(90));
                        GUILayout.Label("Capture", GUILayout.Width(90));
                        GUILayout.Label("Transfer to Low Orbit", GUILayout.Width(90));
                        GUILayout.Label("Tot Capture", GUILayout.Width(90));
                        GUILayout.Label("Low To Surface", GUILayout.Width(90));
                        GUILayout.Label("Ascent", GUILayout.Width(90));
                        GUILayout.Label("Plane Chg", GUILayout.Width(90));
                        if (GUILayout.Button("Parent", GUILayout.Width(100)))
                        {
                            StableSortByParent(_rows);
                        }
                        GUILayout.Label("Is Moon", GUILayout.Width(50));
                        GUILayout.Label("Del", GUILayout.Width(30));
                        GUILayout.Label("Dup", GUILayout.Width(30));
                    }

                    if (_rows.Count > 0)
                    {
                        // Scrollable table
                        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true);

                        for (int i = 0; i < _rows.Count; i++)
                        {
                            var row = _rows[i];

                            if (!RowPassesFilter(row))
                                continue;

                            using (new GUILayout.HorizontalScope())
                            {
                                if (i > 0 && _rows[i - 1].Origin_str == row.Origin_str)
                                    GUILayout.Space(104);
                                else
                                    row.Origin_str = GUILayout.TextField(row.Origin_str, GUILayout.Width(100));
                                if (i > 0 && _rows[i - 1].Origin_str == row.Origin_str && _rows[i - 1].Destination_str == row.Destination_str)
                                    GUILayout.Space(104);
                                else
                                    row.Destination_str = GUILayout.TextField(row.Destination_str, GUILayout.Width(100));

                                // Save and restore GUI background color so we don’t leak red into other controls
                                Color oldColor = GUI.backgroundColor;

                                // Validation helpers
                                bool toOrbitValid = IsValidFloat(row.dV_to_low_orbit_str);
                                bool injectionValid = IsValidFloat(row.injection_dV_str);
                                bool captureValid = IsValidFloat(row.capture_dV_str);
                                bool twrToLowValid = IsValidFloat(row.transfer_to_low_orbit_dV_str);
                                bool totCaptValid = IsValidFloat(row.total_capture_dV_str);
                                bool toSurfValid = IsValidFloat(row.dV_low_orbit_to_surface_str);
                                bool ascentValid = IsValidFloat(row.ascent_dV_str);
                                bool planeChangeValid = IsValidFloat(row.plane_change_dV_str);

                                if (captureValid && twrToLowValid )
                                {
                                    float captureDv = Math.Max(0f, float.Parse(row.capture_dV_str));
                                    float transferToLowOrbitDv = Math.Max(0f, float.Parse(row.transfer_to_low_orbit_dV_str));
                                    float tot = (captureDv + transferToLowOrbitDv);
                                    if (tot > 0)
                                        row.total_capture_dV_str = tot.ToString();
                                }
                                if (!row.isMoon || row.parent == homeWorld.bodyDisplayName.TrimAll())
                                {
                                    GUI.backgroundColor = toOrbitValid ? oldColor : Color.red;
                                    row.dV_to_low_orbit_str = GUILayout.TextField(row.dV_to_low_orbit_str, GUILayout.Width(90));

                                    GUI.backgroundColor = injectionValid ? oldColor : Color.red;
                                    row.injection_dV_str = GUILayout.TextField(row.injection_dV_str, GUILayout.Width(90));

                                }
                                else
                                {
                                    GUILayout.Label($"(parent: {row.parent})", GUILayout.Width(90 + 90 + 4));
                                }

                                GUI.backgroundColor = captureValid ? oldColor : Color.red;
                                row.capture_dV_str = GUILayout.TextField(row.capture_dV_str, GUILayout.Width(90));

                                GUI.backgroundColor = twrToLowValid ? oldColor : Color.red;
                                row.transfer_to_low_orbit_dV_str = GUILayout.TextField(row.transfer_to_low_orbit_dV_str, GUILayout.Width(90));

                                GUI.backgroundColor = totCaptValid ? oldColor : Color.red;
                                row.total_capture_dV_str = GUILayout.TextField(row.total_capture_dV_str, GUILayout.Width(90));


                                GUI.backgroundColor = totCaptValid ? oldColor : Color.red;
                                row.dV_low_orbit_to_surface_str = GUILayout.TextField(row.dV_low_orbit_to_surface_str, GUILayout.Width(90));



                                GUI.backgroundColor = ascentValid ? oldColor : Color.red;
                                row.ascent_dV_str = GUILayout.TextField(row.ascent_dV_str, GUILayout.Width(90));


                                GUI.backgroundColor = ascentValid ? oldColor : Color.red;
                                row.plane_change_dV_str = GUILayout.TextField(row.plane_change_dV_str, GUILayout.Width(90));


                                // Restore color for the delete button
                                GUI.backgroundColor = oldColor;

                                if (DeltaVEditorController.usePack)
                                {
                                    GUILayout.Label(row.parent, GUILayout.Width(100));
                                    row.isMoon = GUILayout.Toggle(CelestialBodyUtils.IsMoon(row.Destination_str, out row.parent), "");
                                    GUILayout.Space(20);
                                }
                                else
                                {
                                    if (row.isMoon)
                                        row.parent = GUILayout.TextField(row.parent, GUILayout.Width(100));
                                    else
                                        GUILayout.Space(104);
                                    row.isMoon = GUILayout.Toggle(row.isMoon, "");
                                    GUILayout.Space(20);
                                }

                                if (GUILayout.Button("X", GUILayout.Width(30)))
                                {
                                    _rows.RemoveAt(i);
                                    i--;
                                }
                                GUILayout.Space(4);
                                if (GUILayout.Button("Dup", GUILayout.Width(30)))
                                {
                                    DeltaVRowEditor dvre = new DeltaVRowEditor(_rows[i]);
                                    _rows.Add(dvre);
                                    i--;
                                }
                            }
                        }

                        GUILayout.EndScrollView();
                    }
                    GUILayout.FlexibleSpace();
                    // Add row button
                    if (GUILayout.Button("Add Row"))
                    {
                        _rows.Add(new DeltaVRowEditor
                        (
                            "",
                            ""
                        ));
                    }

                    GUILayout.Space(5);

                    if (!string.IsNullOrEmpty(_statusMessage))
                    {
                        GUILayout.Label(_statusMessage, HighLogic.Skin.label);
                        GUILayout.Space(5);
                    }
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close", GUILayout.Width(80)))
                    {
                        _visible = false;
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            GUI.DragWindow();
        }

        void PopulateList(List<CelestialBody> bodies, bool allToAll, bool allToHome)
        {
            List<DeltaVRowEditor> newRows = new List<DeltaVRowEditor>();
            foreach (var b in bodies)
            {
                string origin = b.bodyDisplayName.TrimAll();
                string parent = b.referenceBody.bodyDisplayName.TrimAll();
                Log.Info($"PopulateList, bodyName: {origin}  bodyDisplayName: {b.bodyDisplayName}");
                bool isMoon = CelestialBodyUtils.IsMoon(origin, out string parent2);

                parent = parent + ":" + parent2;

                bool found = false;
                foreach (var r in _rows)
                {
                    if (r.Origin_str.TrimAll() == origin)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {

                    newRows.Add(new DeltaVRowEditor(origin, "", isMoon: isMoon, parent: parent));
                }
            }
            foreach (var body in newRows)
            {
                bool found = false;
                foreach (var r in _rows)
                    if (r.Origin_str.TrimAll() == homeWorld.bodyDisplayName.TrimAll().TrimAll() &&
                        r.Destination_str == body.Origin_str.TrimAll())
                        found = true;
                if (!found)
                _rows.Add(new DeltaVRowEditor(homeWorld.bodyDisplayName.TrimAll(), body.Origin_str.TrimAll(), isMoon: body.isMoon, parent: body.parent));
            }
            if (allToAll || allToHome)
            {
                foreach (var body in newRows)
                {
                    bool found = false;
                    foreach (var r in _rows)
                        if (r.Origin_str.TrimAll() == body.Origin_str.TrimAll() && r.Destination_str == homeWorld.bodyDisplayName.TrimAll())
                            found = true;
                    if (!found)
                        _rows.Add(new DeltaVRowEditor(body.Origin_str, homeWorld.bodyDisplayName.TrimAll()));
                    if (!allToHome)
                    {
                        foreach (var dest in newRows)
                        {
                            // Need to search _rows to avoid dups of body.origin/dest.origin
                            found = false;
                            foreach (var r in _rows)
                                if (r.Origin_str == body.Origin_str && r.Destination_str == dest.Origin_str)
                                    found = true;
                            if (!found)
                                _rows.Add(new DeltaVRowEditor(body.Origin_str, dest.Origin_str, isMoon: dest.isMoon, parent: dest.parent));
                        }
                    }
                }
            }
        }


        // --------------------
        // File picker support
        // --------------------

        private void ToggleFilePicker()
        {
            _showFilePicker = !_showFilePicker;

            if (_showFilePicker)
            {
                RefreshFileList();
            }
        }

        private void RefreshFileList()
        {
            _fileOptions.Clear();
            _fileScrollPos = Vector2.zero;

            try
            {
                string relDir = "GameData/MissionPlanner/PluginData/DeltaVTables/";

                _pickerDirRelative = relDir;

                string fullDir = Path.Combine(KSPUtil.ApplicationRootPath, relDir);

                if (!Directory.Exists(fullDir))
                {
                    _statusMessage = $"Directory does not exist: {relDir}";
                    return;
                }

                var files = Directory.GetFiles(fullDir, "*.csv", SearchOption.TopDirectoryOnly);
                foreach (var f in files)
                {
                    string name = Path.GetFileName(f);
                    _fileOptions.Add(name);
                }

                if (_fileOptions.Count == 0)
                {
                    _statusMessage = $"No *.csv files found in {relDir}";
                }
                else
                {
                    _statusMessage = $"Found {_fileOptions.Count} CSV file(s) in {relDir}";
                }
            }
            catch (Exception ex)
            {
                _statusMessage = "Error scanning directory: " + ex.Message;
                Debug.LogError("[DeltaVEditorWindow] RefreshFileList error: " + ex);
            }
        }

        private void DrawFilePicker()
        {
            GUILayout.Space(5);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Select CSV file in directory:");
            GUILayout.Label(_pickerDirRelative);

            _fileScrollPos = GUILayout.BeginScrollView(_fileScrollPos);

            if (_fileOptions.Count == 0)
            {
                GUILayout.Label("<no CSV files>");
            }
            else
            {
                foreach (var fileName in _fileOptions)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(fileName, GUILayout.Width(300)))
                        {
                            // When clicked, update _csvPath and close the picker
                            string combined = Path.Combine(_pickerDirRelative, fileName);
                            // Normalize to forward slashes for consistency
                            _csvPath = combined.Replace('\\', '/');
                            _showFilePicker = false;
                            _statusMessage = $"Selected file: {_csvPath}";
                            LoadCsv();

                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close Picker"))
            {
                _showFilePicker = false;
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private bool RowPassesFilter(DeltaVRowEditor row)
        {
            if (!string.IsNullOrEmpty(_filterOrigin))
            {
                if (row.Origin_str == null ||
                    row.Origin_str.IndexOf(_filterOrigin, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            if (!string.IsNullOrEmpty(_filterDestination))
            {
                if (row.Destination_str == null ||
                    row.Destination_str.IndexOf(_filterDestination, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            return true;
        }

        private static bool IsValidFloat(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;

            return float.TryParse(
                s.Trim(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out _);
        }

        // =====================
        // Public control API
        // =====================

        public static void Toggle()
        {
            if (Instance == null)
            {
                var go = new GameObject("DeltaVEditorWindow");
                Instance = go.AddComponent<DeltaVEditorWindow>();
            }

            Instance._visible = !Instance._visible;
        }

        // =====================
        // Load/Save
        // =====================

        private void LoadCsv()
        {
            _statusMessage = "";

            try
            {
                string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, _csvPath);
                var dvList = DeltaVCsv.Load(fullPath);

                _rows.Clear();
                foreach (var dv in dvList)
                    _rows.Add(DeltaVRowEditor.FromDeltaV(dv));

                _statusMessage = $"Loaded {dvList.Count} rows from {_csvPath}";
            }
            catch (Exception ex)
            {
                _statusMessage = "Error loading: " + ex.Message;
                Debug.LogError("[DeltaVEditorWindow] LoadCsv error: " + ex);
            }
        }

        private void SaveCsv()
        {
            _statusMessage = "";

            try
            {
                var dvList = new List<DeltaV>();
                int rowIndex = 0;

                foreach (var row in _rows)
                {
                    // skip totally empty rows
                    if (string.IsNullOrWhiteSpace(row.Origin_str) &&
                        string.IsNullOrWhiteSpace(row.Destination_str) &&
                        string.IsNullOrWhiteSpace(row.dV_to_low_orbit_str) &&
                        string.IsNullOrWhiteSpace(row.injection_dV_str) &&
                        string.IsNullOrWhiteSpace(row.capture_dV_str) &&
                        string.IsNullOrWhiteSpace(row.dV_low_orbit_to_surface_str) &&
                        string.IsNullOrEmpty(row.plane_change_dV_str)
                        )
                    {
                        continue;
                    }

                    // Validate numerics before converting
                    if (!IsValidFloat(row.dV_to_low_orbit_str) ||
                        !IsValidFloat(row.injection_dV_str) ||
                        !IsValidFloat(row.capture_dV_str) ||
                        !IsValidFloat(row.dV_low_orbit_to_surface_str) ||
                        !IsValidFloat(row.plane_change_dV_str)
                        )
                    {
                        _statusMessage = $"Error: row {rowIndex} has invalid numeric value(s). Fix red cells before saving.";
                        return;
                    }

                    if (!row.TryToDeltaV(out var dv))
                    {
                        _statusMessage = $"Error: failed to parse row {rowIndex}.";
                        return;
                    }
                    dvList.Add(dv);
                    rowIndex++;
                }

                string fullPath = Path.Combine(KSPUtil.ApplicationRootPath, _csvPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? KSPUtil.ApplicationRootPath);

                DeltaVCsv.Save(fullPath, dvList);
                _statusMessage = $"Saved {dvList.Count} rows to {_csvPath}";
            }
            catch (Exception ex)
            {
                _statusMessage = "Error saving: " + ex.Message;
                Debug.LogError("[DeltaVEditorWindow] SaveCsv error: " + ex);
            }
        }

        // =====================
        // Sort
        // =====================

        public void StableSortByParent(List<DeltaVRowEditor> list)
        {
            if (list == null || list.Count <= 1)
                return;

            // Associate each element with its original index
            var indexed = list
                .Select((item, index) => new { item, index })
                .ToList();

            indexed.Sort((a, b) =>
            {
                int cmp = string.Compare(a.item.parent, b.item.parent,
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


        public void StableSortByOrigin(List<DeltaVRowEditor> list)
        {
            if (list == null || list.Count <= 1)
                return;

            // Associate each element with its original index
            var indexed = list
                .Select((item, index) => new { item, index })
                .ToList();

            indexed.Sort((a, b) =>
            {
                int cmp = string.Compare(a.item.Origin_str, b.item.Origin_str,
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

        public static void StableSortByDestination(List<DeltaVRowEditor> list)
        {
            if (list == null || list.Count <= 1)
                return;

            var indexed = list
                .Select((item, index) => new { item, index })
                .ToList();

            indexed.Sort((a, b) =>
            {
                int cmp = string.Compare(a.item.Destination_str, b.item.Destination_str,
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