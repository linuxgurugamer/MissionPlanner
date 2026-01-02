using MissionPlanner.Scenarios;
using SpaceTuxUtility;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        private List<MissionFileInfo> GetAllMissionFiles(bool showStock = true)
        {
            var results = new List<MissionFileInfo>();
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    string dir;
                    if (i == 0)
                        dir = GetMissionDirectoryAbsolute();
                    else
                        dir = GetDefaultMissionDirectoryAbsolute();

                    if (!Directory.Exists(dir))
                    {
                        Log.Info("directory doesn't exist");
                        continue;
                    }
                    foreach (var file in Directory.GetFiles(dir, "*.cfg", SearchOption.TopDirectoryOnly))
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        string save = "", mission = "";
                        int idx = name.IndexOf("__", StringComparison.Ordinal);
                        if (i == 0)
                        {
                            if (idx > 0 && idx < name.Length - 2)
                            {
                                save = name.Substring(0, idx);
                                mission = name.Substring(idx + 2);
                            }
                            else
                            {
                                var cn = ConfigNode.Load(file);
                                save = cn.SafeLoad("SaveName", "");
                                mission = cn.SafeLoad("MissionName", name);
                            }
                        }
                        else
                        {
                            var cn = ConfigNode.Load(file);
                            save = cn.SafeLoad("SaveName", "");
                            mission = cn.SafeLoad("MissionName", name);
                        }
                        results.Add(new MissionFileInfo
                        {
                            FullPath = file,
                            SaveName = save,
                            MissionName = mission,
                            stock = (i == 1)
                        });
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] Listing missions failed: " + ex);
            }
            return results;
        }

        private void OpenLoadDialog()
        {
            showLoadDialog = true;
            loadShowAllSaves = false;
            loadList = GetAllMissionFiles();

            var mp = Input.mousePosition;
            loadRect.x = Mathf.Clamp(mp.x, 40, Screen.width - loadRect.width - 40);
            loadRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - loadRect.height - 40);
        }

        private void DrawLoadDialogWindow(int id)
        {
            //if (!showDeleteConfirm)
            BringWindowForward(id, true);
            GUILayout.Space(6);
            if (!missionRunnerActive)
            {
                loadActiveMissions = false;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Show all saves", ScaledGUILayoutWidth(120));
                    loadShowAllSaves = GUILayout.Toggle(loadShowAllSaves, GUIContent.none, ScaledGUILayoutWidth(22));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Refresh", ScaledGUILayoutWidth(90)))
                    {
                        if (loadActiveMissions)
                        {
                            loadList = Scenarios.ActiveMissions.GetActiveMissionsList();
                        }
                        else
                        {
                            loadList = GetAllMissionFiles();
                        }
                    }
                }
            }
            else
            {
                loadActiveMissions = true;
                loadList = Scenarios.ActiveMissions.GetActiveMissionsList();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Loading Active Missions");
                }
            }
            GUILayout.Space(4);
            string curSave = "";
            if (!loadActiveMissions)
            {
                curSave = GetCurrentSaveName();
                GUILayout.Label(loadShowAllSaves ? "All Missions" : ("Missions for save: " + curSave));
            }
            //            else
            //                GUILayout.Label("All Active Missions:");

            GUILayout.Space(4);
            if (loadShowAllSaves)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Save", ScaledGUILayoutWidth(180));
                    GUILayout.Label("Mission");
                }
            }
            loadScroll = GUILayout.BeginScrollView(loadScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var mf in loadList)
            {
                if (!loadActiveMissions && !loadShowAllSaves && !mf.SaveName.Equals(curSave, StringComparison.OrdinalIgnoreCase) && !mf.stock)
                    continue;


                using (new GUILayout.HorizontalScope())
                {
                    if (loadShowAllSaves)
                        GUILayout.Label(mf.SaveName, ScaledGUILayoutWidth(180));
                    GUILayout.Label(mf.MissionName + (mf.stock ? " (Stock)" : ""), GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("Open", ScaledGUILayoutWidth(70)))
                    {
                        if (loadActiveMissions)
                        {
                            mission = ActiveMissions.GetMission(mf.MissionName);
                            ScreenMessages.PostScreenMessage("Loaded mission “" + mission.missionName + "”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                            showLoadDialog = false;
                        }
                        else
                        {
                            if (TryLoadFromDisk(mf.FullPath))
                            {
                                mission.missionName = mf.MissionName;
                                ScreenMessages.PostScreenMessage("Loaded mission “" + mission.missionName + "”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                                showLoadDialog = false;
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage("Failed to load mission (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                            }
                        }
                    }

                    if (!loadActiveMissions)
                    {
                        if (GUILayout.Button("Import", ScaledGUILayoutWidth(70)))
                        {
                            if (TryLoadFromDisk(mf.FullPath, true))
                            {
                                mission.missionName = mf.MissionName;
                                ScreenMessages.PostScreenMessage("Imported mission “" + mission.missionName + "”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                                showLoadDialog = false;
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage("Failed to load mission (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                            }
                        }


                        if (GUILayout.Button("Delete", ScaledGUILayoutWidth(70)))
                        {
                            deleteTarget = mf;
                            YesNoDialogShow(
                                      title: "Confirm Mission Deletion",
                                      message: "Are you sure you want to delete this mission?",
                                      onYes: OnConfirmMissionDeleteYes
                                  );

                            var mp = Input.mousePosition;
                            deleteRect.x = Mathf.Clamp(mp.x, 40, Screen.width - deleteRect.width - 40);
                            deleteRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - deleteRect.height - 40);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Stop/Delete Active Mission", ScaledGUILayoutWidth(200)))
                        {
                            deleteTarget = mf;
                            //showDeleteConfirm = true;

                            var mp = Input.mousePosition;
                            deleteRect.x = Mathf.Clamp(mp.x, 40, Screen.width - deleteRect.width - 40);
                            deleteRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - deleteRect.height - 40);
                        }

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
                    showLoadDialog = false;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow();
        }

        private void OnConfirmMissionDeleteYes()
        {
            try
            {
                if (File.Exists(deleteTarget.FullPath))
                    File.Delete(deleteTarget.FullPath);
                ScreenMessages.PostScreenMessage("Mission deleted.", 2f, ScreenMessageStyle.UPPER_LEFT);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] Delete failed: " + ex);
                ScreenMessages.PostScreenMessage("Delete failed (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
            }
            finally
            {
                //showDeleteConfirm = false;
                loadList = GetAllMissionFiles(false);
            }
        }

        private void OnConfirmMissionClearYes()
        {
            mission = new Mission();
            selectedNode = null;
            detailNode = null;

            //if (clearAddSample)
            {
                var root = new StepNode
                {
                    data = new Step
                    {
                        title = "New Step"
                    },
                    Expanded = true
                };
                mission.roots.Add(root);
                selectedNode = root;
                OpenDetail(root);
            }
        }

        void OnFullMission()
        {
            Log.Info("OnFullMission");

            mission.simpleChecklist = false;
            OpenDetail(mission.roots[0]);
        }

        void OnSimpleChecklist()
        {
            Log.Info("OnSimpleChecklist");

            mission.simpleChecklist = true;
            OpenDetail(mission.roots[0]);
        }


        private void OnConfirmNewMissionYes()
        {
            if (!missionRunnerActive)
            {
                if (mission.missionName == "")
                {
                    TextEntryDialogShow(
                        title: "Enter Mission Name",
                        message: "Please enter the mission name for current mission:",
                        onOk: OnSaveAsOK2
                    );
                    return;
                }
                TrySaveToDisk_Internal(true);
            }
            else
            {
                ActiveMissions.SaveMission(mission);
            }

            //showNewConfirm = true;
            //var mp = Input.mousePosition;
            //newConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - newConfirmRect.width - 40);
            //newConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - newConfirmRect.height - 40);

            mission = new Mission();
            var root = new StepNode
            {
                data = new Step
                {
                    title = "New Step"
                },
                Expanded = true
            };

            mission.roots.Add(root);
            selectedNode = root;


            YesNoDialogShow(
                    title: "Full Mission or Checklist",
                    message: "Select one of the following:",
                    yesText: "Full Mission",
                    noText: "Simple Checklist",
                    onYes: OnFullMission,
                    onNo: OnSimpleChecklist,
                    vertical: true,
                    minWidth: 180f
                );


        }
    }
}