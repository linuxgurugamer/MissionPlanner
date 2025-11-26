using SpaceTuxUtility;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        private List<MissionFileInfo> GetAllMissionFiles()
        {
            var results = new List<MissionFileInfo>();
            try
            {
                string dir = GetMissionDirectoryAbsolute();
                if (!Directory.Exists(dir)) return results;
                foreach (var file in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    string save = "", mission = "";
                    int idx = name.IndexOf("__", StringComparison.Ordinal);
                    if (idx > 0 && idx < name.Length - 2)
                    {
                        save = name.Substring(0, idx);
                        mission = name.Substring(idx + 2);
                    }
                    else
                    {
                        var cn = ConfigNode.Load(file);
                        save = cn.SafeLoad("SaveName", "UnknownSave");
                        mission = cn.SafeLoad("MissionName", name);
                        //save = cn?.GetValue("SaveName") ?? "UnknownSave";
                        //mission = cn?.GetValue("MissionName") ?? name;
                    }

                    results.Add(new MissionFileInfo
                    {
                        FullPath = file,
                        SaveName = save,
                        MissionName = mission,
                        LastWriteUtc = File.GetLastWriteTimeUtc(file)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] Listing missions failed: " + ex);
            }
            return results;
        }

        private void OpenLoadDialog()
        {
            _showLoadDialog = true;
            _loadShowAllSaves = false;
            _loadList = GetAllMissionFiles();

            var mp = Input.mousePosition;
            _loadRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _loadRect.width - 40);
            _loadRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _loadRect.height - 40);
        }

        private void DrawLoadDialogWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Show all saves", ScaledGUILayoutWidth(120));
                _loadShowAllSaves = GUILayout.Toggle(_loadShowAllSaves, GUIContent.none, ScaledGUILayoutWidth(22));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", ScaledGUILayoutWidth(90)))
                    _loadList = GetAllMissionFiles();
            }

            GUILayout.Space(4);
            string curSave = GetCurrentSaveName();
            GUILayout.Label(_loadShowAllSaves ? "All Missions" : ("Missions for save: " + curSave));

            GUILayout.Space(4);
            if (_loadShowAllSaves)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Save", ScaledGUILayoutWidth(180));
                    GUILayout.Label("Mission");
                }
            }
            _loadScroll = GUILayout.BeginScrollView(_loadScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var mf in _loadList)
            {
                if (!_loadShowAllSaves && !mf.SaveName.Equals(curSave, StringComparison.OrdinalIgnoreCase))
                    continue;


                using (new GUILayout.HorizontalScope())
                {
                    if (_loadShowAllSaves)
                        GUILayout.Label(mf.SaveName, ScaledGUILayoutWidth(180));
                    GUILayout.Label(mf.MissionName, GUILayout.ExpandWidth(true));
                    //GUILayout.Label(mf.LastWriteUtc.ToLocalTime().ToString("g"), ScaledGUILayoutWidth(140));

                    if (GUILayout.Button("Open", ScaledGUILayoutWidth(70)))
                    {
                        if (TryLoadFromDisk(mf.FullPath))
                        {
                            _missionName = mf.MissionName;
                            ScreenMessages.PostScreenMessage("Loaded mission “" + _missionName + "”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                            _showLoadDialog = false;
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage("Failed to load mission (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                        }
                    }

                    if (GUILayout.Button("Import", ScaledGUILayoutWidth(70)))
                    {
                        if (TryLoadFromDisk(mf.FullPath, true))
                        {
                            _missionName = mf.MissionName;
                            ScreenMessages.PostScreenMessage("Imported mission “" + _missionName + "”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                            _showLoadDialog = false;
                        }
                        else
                        {
                            ScreenMessages.PostScreenMessage("Failed to load mission (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                        }
                    }



                    if (GUILayout.Button("Delete", ScaledGUILayoutWidth(70)))
                    {
                        _deleteTarget = mf;
                        _showDeleteConfirm = true;

                        var mp = Input.mousePosition;
                        _deleteRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _deleteRect.width - 40);
                        _deleteRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _deleteRect.height - 40);
                    }
                }
            }
            GUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100))) _showLoadDialog = false;
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawDeleteDialogWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);
            GUILayout.Label("Delete mission:\nSave: " + _deleteTarget.SaveName + "\nMission: " + _deleteTarget.MissionName, _hintLabel);

            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Delete", ScaledGUILayoutWidth(100)))
                {
                    try
                    {
                        if (File.Exists(_deleteTarget.FullPath))
                            File.Delete(_deleteTarget.FullPath);
                        ScreenMessages.PostScreenMessage("Mission deleted.", 2f, ScreenMessageStyle.UPPER_LEFT);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[MissionPlanner] Delete failed: " + ex);
                        ScreenMessages.PostScreenMessage("Delete failed (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                    }
                    finally
                    {
                        _showDeleteConfirm = false;
                        _loadList = GetAllMissionFiles();
                    }
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100)))
                {
                    _showDeleteConfirm = false;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}