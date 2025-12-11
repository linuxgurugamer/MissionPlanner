using SpaceTuxUtility;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;
using static SpaceTuxUtility.ConfigNodeUtils;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        private string GetCurrentSaveName()
        {
            if (HighLogic.SaveFolder == null)
                Log.Error("SaveFolder is  null");
            return HighLogic.SaveFolder ?? "UnknownSave";
        }

        private static string SanitizeForFile(string s)
        {
            if (String.IsNullOrEmpty(s)) return "Unnamed";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c.ToString(), "_");
            return s.Trim();
        }

        private string GetSaveDirectoryAbsolute() { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", SAVE_MOD_FOLDER); }

        private string GetMissionDirectoryAbsolute() { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", MISSION_FOLDER); }
        private string GetDefaultMissionDirectoryAbsolute() { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", DEFAULT_MISSION_FOLDER); }

        private string GetCombinedFileName(string save, string mission) { return SanitizeForFile(save) + "__" + SanitizeForFile(mission) + SAVE_FILE_EXT; }
        private string GetSaveFileAbsolute(string save, string mission) { return Path.Combine(GetMissionDirectoryAbsolute(), GetCombinedFileName(save, mission)); }

        private bool TrySaveToDisk()
        {
            return TrySaveToDisk_Internal(true);
        }

        private bool TrySaveToDisk_Internal(bool overwriteOk)
        {
            try
            {
                string save = GetCurrentSaveName();
                string mission = IsNullOrWhiteSpace(_missionName) ? "Unnamed" : _missionName.Trim();
                string full = GetSaveFileAbsolute(save, mission);

                if (!Directory.Exists(GetMissionDirectoryAbsolute()))
                {
                    Directory.CreateDirectory(GetMissionDirectoryAbsolute());
                }
                if (File.Exists(full) && !overwriteOk) return false;

                var root = new ConfigNode(SAVE_ROOT_NODE);
                root.AddValue("SaveName", save);
                root.AddValue("MissionName", mission);
                root.AddValue("MissionSummary", _missionSummary ?? "");
                root.AddValue("SimpleChecklist", _simpleChecklist);


                root.AddValue("currentView", (int)currentView);

                var list = new ConfigNode(SAVE_LIST_NODE);
                root.AddNode(list);

                foreach (var r in _roots)
                    list.AddNode(r.ToConfigNodeRecursive());

                Directory.CreateDirectory(GetSaveDirectoryAbsolute());
                root.Save(full);

                ShowSaveIndicator(string.Format("Saved ✓ {0} @ {1:t}", mission, DateTime.Now), true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] Save failed: " + ex);
                ScreenMessages.PostScreenMessage("Save failed (see log).", 3f, ScreenMessageStyle.UPPER_LEFT);
                ShowSaveIndicator("Save ✗ (see log)", false);
                return false;
            }
        }

        private void ShowSaveIndicator(string text, bool success)
        {
            if (HighLogic.CurrentGame == null)
                return;
            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().showSaveIndicator
                && currentView == View.Full)
            {
                _lastSaveInfo = String.IsNullOrEmpty(text) ? (success ? "Saved ✓" : "Error ✗") : text;
                _lastSaveWasSuccess = success;
                _lastSaveShownUntil = Time.realtimeSinceStartup + SaveIndicatorSeconds;
            }
        }

        private bool TryLoadFromDisk(string fullPath, bool import = false)
        {
            try
            {
                if (!File.Exists(fullPath))
                {
                    Log.Error("tryLoadFromDisk, File: " + fullPath + " does not exist");
                    return false;
                }

                var root = ConfigNode.Load(fullPath);
                if (root == null || !root.HasNode(SAVE_LIST_NODE))
                {
                    if (root == null)
                        Log.Error("tryLoadFromDisk, root is null");
                    else
                        Log.Error("tryLoadFromDisk, root.HasNode(SAVE_LIST_NODE) not found");

                    return false;
                }

                _missionName = root.SafeLoad("MissionName", _missionName);
                _missionSummary = root.SafeLoad("MissionSummary", "");
                _simpleChecklist = root.SafeLoad("SimpleChecklist", false);
                int i = root.SafeLoad("currentView", 0);

                currentView = (View)i;

                var list = root.GetNode(SAVE_LIST_NODE);
                var newRoots = new List<StepNode>();
                foreach (var n in list.GetNodes("NODE"))
                {
                    StepNode node = StepNode.FromConfigNodeRecursive(n);

                    node.Parent = null;
                    newRoots.Add(node);
                }

                if (!import)
                    _roots.Clear();
                _roots.AddRange(newRoots);
                ReparentAll();

                ShowSaveIndicator("Loaded ✓ " + _missionName, true);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] Load failed: " + ex);
                ScreenMessages.PostScreenMessage("Load failed (see log).", 3f, ScreenMessageStyle.UPPER_LEFT);
                ShowSaveIndicator("Load ✗ (see log)", false);
                return false;
            }
        }

        private bool TryAutoLoadMostRecentForCurrentSave()
        {
            //return false;
            string save = GetCurrentSaveName();
            var list = GetAllMissionFiles(false);
            MissionFileInfo best = default(MissionFileInfo);
            bool found = false;

            foreach (var mf in list)
            {
                if (mf.SaveName.Equals(save, StringComparison.OrdinalIgnoreCase))
                {
                    if (!found)
                    {
                        best = mf;
                        found = true;
                    }
                }
            }
            if (found)
            {
                _missionName = best.MissionName;
                return TryLoadFromDisk(best.FullPath);
            }
            return false;
        }

        private void TrySaveAs()
        {
            _creatingNewMission = false;
            OpenSaveAsDialog();
        }

        private void DrawOverwriteDialogWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);
            GUILayout.Label("A mission with this name already exists.", _tinyLabel);

            GUILayout.Space(4);
            GUILayout.Label("Save: " + GetCurrentSaveName(), _hintLabel);
            GUILayout.Label("Mission: " + _pendingSaveMission, _hintLabel);
            GUILayout.Label("File: " + Path.GetFileName(_pendingSavePath), _hintLabel);

            GUILayout.Space(12);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Overwrite", ScaledGUILayoutWidth(120)))
                {
                    if (!String.IsNullOrEmpty(_pendingSaveMission))
                        _missionName = _pendingSaveMission;
                    _showOverwriteDialog = false;
                    TrySaveToDisk_Internal(true);
                }
                if (GUILayout.Button("Auto-Increment & Save", ScaledGUILayoutWidth(180)))
                {
                    string baseName = String.IsNullOrEmpty(_pendingSaveMission) ? _missionName : _pendingSaveMission;
                    string next = GetAutoIncrementName(baseName);
                    _missionName = next;
                    _pendingSaveMission = null;
                    _pendingSavePath = null;
                    _showOverwriteDialog = false;
                    TrySaveToDisk_Internal(true);
                }
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100)))
                {
                    _showOverwriteDialog = false;
                    _pendingSaveMission = null;
                    _pendingSavePath = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private string GetAutoIncrementName(string baseName)
        {
            if (IsNullOrWhiteSpace(baseName)) baseName = "Unnamed";
            string save = GetCurrentSaveName();
            string name = baseName;
            int n = 2;

            while (File.Exists(GetSaveFileAbsolute(save, name)))
            {
                string stripped = baseName;
                int p = stripped.LastIndexOf('(');
                int q = stripped.LastIndexOf(')');
                if (p >= 0 && q == stripped.Length - 1)
                {
                    string inside = stripped.Substring(p + 1, q - p - 1);
                    int parsed;
                    if (int.TryParse(inside, out parsed))
                    {
                        stripped = stripped.Substring(0, p).TrimEnd();
                        n = parsed + 1;
                    }
                }
                name = string.Format("{0} ({1})", stripped, n);
                n++;
            }
            return name;
        }


        public void LoadTrackedVesselMission(Guid guid)
        {
            string missionName = MissionVisitTracker.GetTrackedMissionName(guid);

            string save = HighLogic.SaveFolder; // GetCurrentSaveName();
            {
                var list = GetAllMissionFiles();
                MissionFileInfo best = default(MissionFileInfo);
                bool found = false;

                foreach (var mf in list)
                {
                    if (mf.SaveName.Equals(save, StringComparison.OrdinalIgnoreCase))
                    {
                        if (missionName == mf.MissionName)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                if (found)
                {
                    _missionName = missionName;
                    TryLoadFromDisk(best.FullPath);
                }
            }
        }

    }
}
