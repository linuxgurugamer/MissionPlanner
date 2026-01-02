// File: MissionPlanner.cs
// Mod: MissionPlanner

using ClickThroughFix;
using KSP.Localization;
using KSP.UI.Screens;
using MissionPlanner.Scenarios;
using MissionPlanner.Utils;
using SpaceTuxUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolbarControl_NS;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{

    [KSPAddon(KSPAddon.Startup.AllGameScenes, true)]
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        const float TINY_WIDTH = 635;
        const float COMPACT_WIDTH = 650;
        const float FULL_WIDTH = 840;

        //const float MAX_TITLE_WIDTH = 600;


        internal const string MODID = "Mission Planner";
        internal const string MODNAME = "Mission Planner";
        static ToolbarControl toolbarControl = null;

        public static bool openDeltaVEditor = false;

        // ---- Windows ----
        private Rect treeRect = new Rect(220, 120, 840, 620);
        private Rect detailRect = new Rect(820, 160, 600, 400); //560
        private Rect moveRect = new Rect(760, 120, 460, 400); // 560
        private Rect loadRect = new Rect(720, 100, 560, 540);
        private Rect overwriteRect = new Rect(760, 180, 520, 220);
        private Rect deleteRect = new Rect(760, 160, 520, 180);
        private Rect partRect = new Rect(680, 140, 350, 580);
        private Rect deltaVRect = new Rect(680, 140, 1000, 580);
        private Rect moduleRect = new Rect(680, 140, 350, 580);
        private Rect SASRect = new Rect(680, 140, 350, 400); // 580
        private Rect CategoryRect = new Rect(680, 140, 300, 580);
        private Rect bodyAsteroidVesselRect = new Rect(680, 140, 350, 580);
        private Rect traitRect = new Rect(680, 140, 200, 250); // 560
        private Rect saveAsRect = new Rect(740, 200, 520, 310); // includes summary
        private Rect newConfirmRect = new Rect(760, 200, 520, 180);
        private Rect clearConfirmRect = new Rect(760, 220, 520, 170);
        private Rect summaryRect = new Rect(760, 220, 520, 320);

        private int treeWinId,
            detailWinId,
            moveWinId,
            loadWinId,
            overwriteWinId,
            deleteWinId,
            partWinId,
            resourceWinId,
            traitWinId,
            moduleWinId,
            SASWinId,
            saveAsWinId,
            newConfirmWinId,
            clearConfirmWinId,
            summaryWinId;
        private bool visible = false;
        private bool Hidden = false;
        private bool visibleBeforePause = true;

        // Skin toggle (persisted)
        internal static bool useKspSkin = true;

        // Column tuning (persisted)
        private float titleWidthPct = 0; //0.40f;  // 40–95%
        private float controlsPad = 40f;    // 0–120 px
        private float indentPerLevel = 30f;    // 10–40 px
        private float foldColWidth = 24f;    // 16–48 px

        private const string IconOnPath = "MissionPlanner/Icons/tree_on";
        private const string IconOffPath = "MissionPlanner/Icons/tree_off";
        private const string IconOnPath_24 = "MissionPlanner/Icons/tree_on-24";
        private const string IconOffPath_24 = "MissionPlanner/Icons/tree_off-24";

        public static Texture2D moveIcon, duplicateIcon;
        public const string MovePath = "MissionPlanner/Icons/move";
        public const string DuplicatePath = "MissionPlanner/Icons/duplicate";
        public const string RunningManSheet = "GameData/MissionPlanner/Icons/runningSprites.png";
        public const string SittingManIcon = "GameData/MissionPlanner/Icons/sittingMan.png";

        // Missions I/O
        internal const string SAVE_ROOT_NODE = "MISSION_PLANNER";
        private const string TRACKEDSAVE_ROOT_NODE = "MISSION_PLANNER_TRACKED_VESSEL";
        internal const string SAVE_LIST_NODE = "ROOTS";
        internal const string SAVE_MOD_FOLDER = "MissionPlanner/PluginData";
        internal const string MISSION_FOLDER = SAVE_MOD_FOLDER + "/Missions";
        internal const string ACTIVE_MISSION_FOLDER = SAVE_MOD_FOLDER + "/ActiveMissions";
        internal const string DEFAULT_MISSION_FOLDER = SAVE_MOD_FOLDER + "/DefaultMissions";
        public const string DELTA_V_FOLDER = SAVE_MOD_FOLDER + "/DeltaVTables";

        private const string SAVE_FILE_EXT = ".cfg";

        // UI persistence
        private const string UI_FILE_NAME = "UI.cfg";
        private const string UI_ROOT_NODE = "MISSION_PLANNER_UI";

        static public Mission mission = new Mission();

        // Data
        //public static readonly List<StepNode> _roots = new List<StepNode>();

        // Selection / dialogs
        private StepNode selectedNode = null;
        private StepNode detailNode = null;
        private bool showMoveDialog = false;
        private StepNode moveNode = null;
        private StepNode moveTargetParent = null;

        // Load dialog
        private bool showLoadDialog = false;
        private bool loadShowAllSaves = false;
        private bool loadActiveMissions = false;
        private Vector2 loadScroll;
        private List<MissionFileInfo> loadList = new List<MissionFileInfo>();

        // Overwrite confirm
        private bool showOverwriteDialog = false;
        private string pendingSavePath = null;
        private string pendingSaveMission = null;

        // Delete confirm
        // private bool showDeleteConfirm = false;
        private MissionFileInfo deleteTarget;

        // Part picker dialog
        private bool showPartDialog = false;
        private StepNode partTargetNode = null;
        private Vector2 partScroll;
        private string partFilter = "";
        private bool partAvailableOnly = true;

        // Resource picker dialog
        //private bool _showResourceDialog = false;
        private StepNode deltaVTargetNode = null;
        private Vector2 deltaVScroll;
        private string deltaVFilter = "";

        // Save As / New
        private static bool saveAsDefault = false;
        private string saveAsName = "";

        // New confirm
        private bool showNewConfirm = false;

        // Clear All
        //private bool showClearConfirm = false;

        private Vector2 summaryScroll;

        // Scroll
        private Vector2 scroll;
        private Vector2 moveScroll;

        // Resize handle for main window
        //        private bool _resizingTree = false;
        //private Vector2 _resizeStartLocal;
        //private Rect _resizeStartRect;
        private const float resizeHandleSize = 26f;
        private const float minTreeW = 360f;
        private const float minTreeH = 420f;

        // Styles
        private GUIStyle titleLabel = null,
            unfilledTitleLabel = null,
            selectedTitleLabel = null,
            selectedUnfilledTitleLabel = null,
            smallBtn = null,
            hintLabel = null,
            errorLabel = null,
            errorLargeLabel = null,
            tinyLabel = null,
            smallLabel = null,
            titleEdit = null,
            badge = null,
            badgeError = null,
            cornerGlyph = null;

        bool lastUseKSPSkin = true;

        // Double-click
        private int lastClickedId = -1;
        private float lastClickTime = 0f;
        private const float DoubleClickSec = 0.30f;

        // Save indicator
        private static string lastSaveInfo = "";
        private static float lastSaveShownUntil = 0f;
        private const float SaveIndicatorSeconds = 3f;
        private static bool lastSaveWasSuccess = true;

        private static bool IsNullOrWhiteSpace(string s)
        {
            return String.IsNullOrEmpty(s) || s.Trim().Length == 0;
        }

        public static GUILayoutOption ScaledGUILayoutWidth(float width)
        {
            return GUILayout.Width(width * GameSettings.UI_SCALE);
        }
        public static GUILayoutOption ScaledGUIFontLayoutWidth(float width)
        {
            return GUILayout.Width(width * HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().fontSize / 12f);
        }

        public void Awake()
        {
            treeWinId = WindowHelper.NextWindowId("treeWin");
            detailWinId = WindowHelper.NextWindowId("detailWin");
            moveWinId = WindowHelper.NextWindowId("moveWin");
            loadWinId = WindowHelper.NextWindowId("loadWin");
            overwriteWinId = WindowHelper.NextWindowId("overwriteWin");
            deleteWinId = WindowHelper.NextWindowId("deleteWin");
            partWinId = WindowHelper.NextWindowId("partWin");
            resourceWinId = WindowHelper.NextWindowId("resourceWin");
            traitWinId = WindowHelper.NextWindowId("traitWin");
            moduleWinId = WindowHelper.NextWindowId("moduleWin");
            SASWinId = WindowHelper.NextWindowId("SASWin");
            saveAsWinId = WindowHelper.NextWindowId("saveAsWin");
            newConfirmWinId = WindowHelper.NextWindowId("newConfirmWin");
            clearConfirmWinId = WindowHelper.NextWindowId("clearConfirmWin");
            summaryWinId = WindowHelper.NextWindowId("summaryWin");

            LoadUISettings();

            StartCoroutine(Initialization.BackgroundInitialize());

            ReparentAll(mission);

            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
            GameEvents.onVesselSwitching.Add(onVesselSwitching);
            GameEvents.onVesselChange.Add(onVesselChange);
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.onEditorLoad.Add(onEditorLoad);
            GameEvents.onEditorStarted.Add(onEditorStarted);

            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoadedGUIReady);
            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);

            this.resizeHandle = new ResizeHandle();
        }

        public void Start()
        {
            ToolbarControl.LoadImageFromFile(ref runningManSheet, RunningManSheet);
            ToolbarControl.LoadImageFromFile(ref sittingMan, SittingManIcon);

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(
                    onTrue: () => { visible = true; },
                    onFalse: () => { visible = false; },
                    ApplicationLauncher.AppScenes.ALWAYS,
                    MODID,
                    "MissionPlannerButton",
                    IconOnPath,
                    IconOffPath,
                    IconOnPath_24,
                    IconOffPath_24,
                    MODNAME);
            }

            TryAutoLoadMostRecentForCurrentSave();
            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            if (HighLogic.CurrentGame != null)
            {
                if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                    TrySaveToDisk_Internal(true);
            }
            SaveUISettings();

            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);
            GameEvents.onVesselSwitching.Remove(onVesselSwitching);
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.onLevelWasLoadedGUIReady.Remove(onLevelWasLoadedGUIReady);
            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
        }

        void CloseAllDialogs()
        {
            showMoveDialog =
                showLoadDialog =
                showOverwriteDialog =
                showPartDialog =
                showNewConfirm =
                showTraitDialog =
                showModuleDialog =
                showBodyAsteroidVesselDialog = false;
        }

        internal static bool newWindow = false;
        internal static void BringWindowForward(int id, bool force = false)
        {
            if (!dialogOpen)
            {
                if (newWindow || force)
                {
                    newWindow = false;
                    GUI.BringWindowToFront(id);
                }
            }
        }

        public void OnGUI()
        {
            if (HighLogic.LoadedScene <= GameScenes.MAINMENU ||
                (HighLogic.CurrentGame != null &&
                HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().hideOnPause &&
                PauseMenu.exists && PauseMenu.isOpen) ||
                Hidden)
                return;

            useKspSkin = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().useKspSkin;
            SetUpSkins();
            GUI.skin = adjustableSkin;
            EnsureStyles();

            int oldDepth = GUI.depth;
            GUI.depth = 10;
            currentEntryFieldId = 0;
            ComboBox.DrawGUI();
            if (visible)
            {

                treeRect = ClickThruBlocker.GUILayoutWindow(treeWinId, treeRect, DrawTreeWindow,
                   missionRunnerActive ? "Mission Runner" : "Mission Planner/Checklist");
                // do this here since if it's done within the window you only recieve events that are inside of the window
                this.resizeHandle.DoResize(ref this.treeRect);
                if (detailNode != null)
                {
                    if (mission.simpleChecklist)
                    {
                        detailRect.height = 200f;
                        detailRect = ClickThruBlocker.GUILayoutWindow(
                                         detailWinId, detailRect, DrawDetailWindow,
                                         string.Format("Step Details — {0}", detailNode.data.title),
                                         GUILayout.MinWidth(520), GUILayout.MinHeight(200)
                                     );
                    }
                    else
                    {
                        detailRect = ClickThruBlocker.GUILayoutWindow(
                            detailWinId, detailRect, DrawDetailWindow,
                            string.Format("Step Details — {0}", detailNode.data.title),
                            GUILayout.MinWidth(520), GUILayout.MinHeight(200)
                        );
                    }
                }

                if (showMoveDialog && moveNode != null)
                {
                    moveRect = ClickThruBlocker.GUILayoutWindow(
                        moveWinId, moveRect, DrawMoveDialogWindow,
                        string.Format("Move “{0}” — choose new parent", moveNode.data.title),
                        GUILayout.MinWidth(420), GUILayout.MinHeight(320)
                    );
                }
                if (showLoadDialog)
                {
                    loadRect = ClickThruBlocker.GUILayoutWindow(
                        loadWinId, loadRect, DrawLoadDialogWindow,
                        "Load Mission",
                        GUILayout.MinWidth(480), GUILayout.MinHeight(380)
                    );
                }
                if (showOverwriteDialog)
                {
                    overwriteRect = ClickThruBlocker.GUILayoutWindow(
                        overwriteWinId, overwriteRect, DrawOverwriteDialogWindow,
                        "Overwrite Confirmation",
                        GUILayout.MinWidth(420), GUILayout.MinHeight(180)
                    );
                }
                if (showPartDialog && partTargetNode != null)
                {
                    partRect = ClickThruBlocker.GUILayoutWindow(
                        partWinId, partRect, DrawPartPickerWindow,
                        "Select Part",
                         GUILayout.MinHeight(400)
                    );
                }

                if (showDeltaVDialog)
                {
                    deltaVRect = ClickThruBlocker.GUILayoutWindow(
                        resourceWinId, deltaVRect, DrawDeltaVPickerWindow,
                        "Select Suggested DeltaV",
                         GUILayout.MinHeight(400)
                    );
                }

                if (showTraitDialog)
                {
                    traitRect = ClickThruBlocker.GUILayoutWindow(
                        traitWinId, traitRect, DrawTraitPickerWindow,
                        "Select Trait",
                         GUILayout.MinHeight(400)
                    );
                }

                if (showModuleDialog)
                {
                    moduleRect = ClickThruBlocker.GUILayoutWindow(
                        moduleWinId, moduleRect, DrawModulePickerWindow,
                        "Select Module",
                        GUILayout.MinHeight(400)
                    );
                }

                if (showCategoryDialog)
                {
                    SASRect = ClickThruBlocker.GUILayoutWindow(
                        SASWinId, SASRect, DrawCategoryPickerWindow,
                        "Select Category",
                         GUILayout.MinHeight(400)
                    );
                }

                if (showBodyAsteroidVesselDialog)
                {
                    bodyAsteroidVesselRect = ClickThruBlocker.GUILayoutWindow(
                        moduleWinId, bodyAsteroidVesselRect, DrawBodyAsteroidVesselPickerWindow,
                        "Select Vessel/Asteroid/Body",
                        GUILayout.MinHeight(400)
                    );
                }

                if (showNewConfirm)
                {
                    newConfirmRect = ClickThruBlocker.GUILayoutWindow(
                        newConfirmWinId, newConfirmRect, DrawNewConfirmWindow,
                        "Start New Mission?",
                        GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                    );
                }

                if (showPartDialog && partTargetNode != null)
                {
                    partRect = ClickThruBlocker.GUILayoutWindow(
                        partWinId, partRect, DrawPartPickerWindow,
                        "Select Part",
                         GUILayout.MinHeight(400)
                    );
                }

                // MUST be last
                //KSPIMGUIYesNoDialog.OnGUI();
            }
            GUI.depth = oldDepth;
        }



        GUIStyle ScaleStyle(GUIStyle s, float scale)
        {
            if (s == null) return null;

            GUIStyle st = new GUIStyle(s);

            // Scale font size
            st.fontSize = Mathf.RoundToInt(s.fontSize * scale);

            // Scale style-specific fields
            st.fixedHeight *= scale;
            st.fixedWidth *= scale;
            //st.lineHeight *= scale;

            // Scale paddings and margins
            st.padding.left = Mathf.RoundToInt(st.padding.left * scale);
            st.padding.right = Mathf.RoundToInt(st.padding.right * scale);
            st.padding.top = Mathf.RoundToInt(st.padding.top * scale);
            st.padding.bottom = Mathf.RoundToInt(st.padding.bottom * scale);

            st.margin.left = Mathf.RoundToInt(st.margin.left * scale);
            st.margin.right = Mathf.RoundToInt(st.margin.right * scale);
            st.margin.top = Mathf.RoundToInt(st.margin.top * scale);
            st.margin.bottom = Mathf.RoundToInt(st.margin.bottom * scale);

            // Scale overflow
            st.overflow.left = Mathf.RoundToInt(st.overflow.left * scale);
            st.overflow.right = Mathf.RoundToInt(st.overflow.right * scale);
            st.overflow.top = Mathf.RoundToInt(st.overflow.top * scale);
            st.overflow.bottom = Mathf.RoundToInt(st.overflow.bottom * scale);

            // ContentOffset
            st.contentOffset = new Vector2(st.contentOffset.x * scale, st.contentOffset.y * scale);

            return st;
        }

        GUISkin DuplicateAndScaleSkin(GUISkin source, float scale)
        {
            if (source == null) return null;

            var newSkin = Instantiate(source);

            // Duplicate and scale all built-in styles
            newSkin.box = ScaleStyle(source.box, scale);
            newSkin.button = ScaleStyle(source.button, scale);
            newSkin.toggle = ScaleStyle(source.toggle, scale);
            newSkin.label = ScaleStyle(source.label, scale);
            newSkin.textField = ScaleStyle(source.textField, scale);
            newSkin.textArea = ScaleStyle(source.textArea, scale);
            newSkin.window = ScaleStyle(source.window, scale);

            newSkin.horizontalSlider = ScaleStyle(source.horizontalSlider, scale);
            newSkin.horizontalSliderThumb = ScaleStyle(source.horizontalSliderThumb, scale);
            newSkin.verticalSlider = ScaleStyle(source.verticalSlider, scale);
            newSkin.verticalSliderThumb = ScaleStyle(source.verticalSliderThumb, scale);

            newSkin.horizontalScrollbar = ScaleStyle(source.horizontalScrollbar, scale);
            newSkin.horizontalScrollbarThumb = ScaleStyle(source.horizontalScrollbarThumb, scale);
            newSkin.horizontalScrollbarLeftButton = ScaleStyle(source.horizontalScrollbarLeftButton, scale);
            newSkin.horizontalScrollbarRightButton = ScaleStyle(source.horizontalScrollbarRightButton, scale);

            newSkin.verticalScrollbar = ScaleStyle(source.verticalScrollbar, scale);
            newSkin.verticalScrollbarThumb = ScaleStyle(source.verticalScrollbarThumb, scale);
            newSkin.verticalScrollbarUpButton = ScaleStyle(source.verticalScrollbarUpButton, scale);
            newSkin.verticalScrollbarDownButton = ScaleStyle(source.verticalScrollbarDownButton, scale);

            newSkin.scrollView = ScaleStyle(source.scrollView, scale);

            // Custom styles
            if (source.customStyles != null)
            {
                newSkin.customStyles = source.customStyles
                    .Select(s => ScaleStyle(s, scale))
                    .ToArray();
            }

            return newSkin;
        }


        GUISkin adjustableSkin = null;
        GUISkin originalGuiSkin = null;
        bool forceRecalcStyles = true;
        int largeLabelFontSize;
        int normalLabelFontSize;
        int smallLabelFontSize;
        int tinyLabelFontSize;

        int lastFontSize;
        void SetUpSkins()
        {
            forceRecalcStyles = (lastUseKSPSkin != useKspSkin || lastFontSize != HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().fontSize);
            lastUseKSPSkin = useKspSkin;
            lastFontSize = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().fontSize;

            if (originalGuiSkin == null)
            {
                originalGuiSkin = DuplicateAndScaleSkin(GUI.skin, 1f);
                forceRecalcStyles = true;
            }
        }


        private void EnsureStyles()
        {
            if (GUI.skin == null)
                return;
            if (!forceRecalcStyles)
                return;

            GUI.skin.label.fontSize = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().fontSize;

            largeLabelFontSize = (int)((GUI.skin.label.fontSize + 2) * GameSettings.UI_SCALE);
            normalLabelFontSize = (int)(GUI.skin.label.fontSize * GameSettings.UI_SCALE);
            smallLabelFontSize = (int)((GUI.skin.label.fontSize - 1) * GameSettings.UI_SCALE);
            tinyLabelFontSize = (int)((GUI.skin.label.fontSize - 2) * GameSettings.UI_SCALE);

            //Log.Info($"normalLabelFontSize: {normalLabelFontSize}   smallLabelFontSize: {smallLabelFontSize}   tinyLabelFontSize: {tinyLabelFontSize}");
            //Log.Info($"GUI.skin.label.fontSize: {GUI.skin.label.fontSize}");

            if (useKspSkin)
                adjustableSkin = DuplicateAndScaleSkin(HighLogic.Skin, GameSettings.UI_SCALE);
            else
                adjustableSkin = DuplicateAndScaleSkin(originalGuiSkin, GameSettings.UI_SCALE);

            Utils.ComboBox.UpdateStyles(normalLabelFontSize);
            titleLabel = unfilledTitleLabel = selectedTitleLabel = selectedUnfilledTitleLabel = smallBtn =
                hintLabel = errorLabel = tinyLabel = smallLabel = titleEdit =
                badge = badgeError = cornerGlyph = null;
            forceRecalcStyles = false;

            {
                errorLabel = new GUIStyle(GUI.skin.label);
                errorLabel.fontSize = normalLabelFontSize;
                errorLabel.normal.textColor = Color.red;
            }
            {
                errorLargeLabel = new GUIStyle(GUI.skin.label);
                errorLargeLabel.fontSize = largeLabelFontSize + 2;
                errorLargeLabel.normal.textColor = Color.red;
            }
            {
                titleLabel = new GUIStyle(GUI.skin.label);
                titleLabel.fontSize = largeLabelFontSize;
            }
            {
                unfilledTitleLabel = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = titleLabel.onNormal,
                    hover = titleLabel.onHover,
                    active = titleLabel.onActive

                };
                unfilledTitleLabel.normal.textColor = Color.red;
                unfilledTitleLabel.fontSize = largeLabelFontSize;
            }
            {
                selectedTitleLabel = new GUIStyle(titleLabel)
                {
                    normal = titleLabel.onNormal,
                    hover = titleLabel.onHover,
                    active = titleLabel.onActive,
                    fontStyle = FontStyle.Bold
                };
                selectedTitleLabel.normal.textColor = Color.white;
                selectedTitleLabel.fontSize = largeLabelFontSize;
            }
            {
                selectedUnfilledTitleLabel = new GUIStyle(titleLabel)
                {
                    normal = titleLabel.onNormal,
                    hover = titleLabel.onHover,
                    active = titleLabel.onActive,
                    fontStyle = FontStyle.Bold
                };
                selectedUnfilledTitleLabel.normal.textColor = Color.red;
                selectedUnfilledTitleLabel.fontSize = largeLabelFontSize;

            }
            //_smallBtn = new GUIStyle(GUI.skin.button) { fixedWidth = 28f, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(2, 2, 2, 2) };
            smallBtn = new GUIStyle(titleLabel)
            {
                fixedWidth = 28f
            };
            hintLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
            tinyLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = tinyLabelFontSize
            };
            smallLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = smallLabelFontSize
            };
            {
                titleEdit = new GUIStyle(GUI.skin.textField)
                {
                    fontStyle = FontStyle.Bold
                };
                titleEdit.fontSize = largeLabelFontSize;
            }

            //if (_badge == null)
            {
                badge = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = {
                        textColor = new Color(0.85f, 0.9f, 1f, 1f)
                    }
                };
                badge.fontSize = normalLabelFontSize;
            }
            //if (_badgeError == null)
            {
                badgeError = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = {
                        textColor = new Color(1f, 0.35f, 0.35f, 1f)
                    }
                };
                badgeError.fontSize = normalLabelFontSize;
            }

            //if (_cornerGlyph == null)
            {
                cornerGlyph = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerRight,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.Max(GUI.skin.label.fontSize + 6, 14)
                };
                cornerGlyph.fontSize = normalLabelFontSize;
            }
        }

        private void onHideUI()
        {
            Hidden = true;
        }
        private void onShowUI()
        {
            Hidden = false;
        }

        private void OnGamePaused()
        {
            visibleBeforePause = visible;
            visible = false;
            //SyncToolbarState();
        }
        private void OnGameUnpaused()
        {
            visible = visibleBeforePause;
            //SyncToolbarState();
        }

        bool showSummary = false;
        bool showDetail = false;
        bool showVesselSpecific = false;
        View activeViewForFrame;

        static public bool missionRunnerActive = false;

        internal Texture2D runningManSheet;
        internal Texture2D sittingMan;
        private int frameIndex;
        const int MAXFRAMES = 18;
        private float frameTimer;


        private const float FrameDuration = 0.12f;

        void FixedUpdate()
        {
            frameTimer += Time.deltaTime;

            if (frameTimer >= FrameDuration)
            {
                frameTimer = 0f;
                frameIndex++;
                if (frameIndex >= MAXFRAMES)
                    frameIndex = 0;
            }
        }

        private void OnConfirmYes()
        {
            mission.missionActive = true;
        }

        void OnConfirmMissionRunYes()
        {
            if (!missionRunnerActive)
                TrySaveToDisk_Internal(true);
            else
                ActiveMissions.SaveMission(mission);

            mission = new Mission();
            missionRunnerActive = !missionRunnerActive;
            OpenLoadDialog();
        }

        private void DrawTreeWindow(int id)
        {
            GUILayout.Space(4);
            activeViewForFrame = mission.currentView;

            // Top row: Save name, Mission, controls, skin
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Mission:", ScaledGUILayoutWidth(60));
                mission.missionName = GUILayout.TextField(mission.missionName ?? "", titleEdit, ScaledGUIFontLayoutWidth(450), GUILayout.ExpandWidth(false));
                showSummary = GUILayout.Toggle(showSummary, "");

                if (!missionRunnerActive && activeViewForFrame == View.Full)
                {
                    if (showSummary)
                        GUILayout.Label("Summary Preview:", ScaledGUILayoutWidth(120));
                    else
                        GUILayout.Label("Show Summary");
                }
                if (!missionRunnerActive && !mission.missionActive)
                {
                    if (GUILayout.Button(new GUIContent("Start a mission", "Activate "), GUILayout.Width(120)))
                    {
                        YesNoDialogShow(
                              title: "Confirm Mission Start",
                              message: "Are you sure you want to activate this mission?",
                              onYes: OnConfirmYes
                          );
                    }
                }


                GUILayout.FlexibleSpace();
                Texture2D runningIcon = missionRunnerActive ? SpriteSheetIMGUI.GetFrame_LastSheet(runningManSheet,
                        frameIndex,
                        frameCount: MAXFRAMES
                    ) : sittingMan;

                //string testStr = "-\\-/";
                //GUILayout.Label(testStr[frameIndex % 4].ToString());
                GUILayout.Label(" ");
                GUILayout.FlexibleSpace();
                // _useKspSkin = GUILayout.Toggle(_useKspSkin, "Use KSP Skin", ScaledGUILayoutWidth(120));
                if (!mission.simpleChecklist)
                {
                    GUIContent content = new GUIContent(runningIcon, !missionRunnerActive ? " Switch to Mission Runner " : " Switch to Mission Planner ");
                    if (GUILayout.Button(content, buttonIconStyle, GUILayout.Width(40), GUILayout.Height(40)))
                    {
                        YesNoDialogShow(
                              title: missionRunnerActive ? "Confirm Stop Running" : "Confirm Running",
                              message: missionRunnerActive ? "Are you sure you want to save and stop running this mission?\n" +
                              "(current save will automatically be saved)" : "Are you sure you want to run a mission?\n" +
                              "(current save will automatically be saved)",
                              onYes: OnConfirmMissionRunYes
                          );
                    }
                }
            }

            GUILayout.Space(4);

            if (showSummary)
            {
                summaryScroll = GUILayout.BeginScrollView(summaryScroll, HighLogic.Skin.textArea, GUILayout.Height(110));
                mission.missionSummary = GUILayout.TextArea(string.IsNullOrEmpty(mission.missionSummary) ? "" : mission.missionSummary, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }
            using (new GUILayout.HorizontalScope())
            {
                if (!missionRunnerActive)
                {
                    GUILayout.Space(8);
                    if (GUILayout.Button("Add Objective/goal", ScaledGUILayoutWidth(160)))
                    {
                        var newRoot = new StepNode();
                        mission.roots.Add(newRoot);
                        selectedNode = newRoot;
                        OpenDetail(newRoot);
                    }
                    GUILayout.Space(40);
                }
                if (GUILayout.Button("Expand All", ScaledGUILayoutWidth(110))) SetAllExpanded(true);
                if (GUILayout.Button("Collapse All", ScaledGUILayoutWidth(110))) SetAllExpanded(false);

                GUILayout.FlexibleSpace();
                if (activeViewForFrame == View.Full)
                {
                    if (!mission.simpleChecklist)
                    {
                        showDetail = GUILayout.Toggle(showDetail, "");
                        GUILayout.Label("Show Detail");
                        GUILayout.FlexibleSpace();
                    }
                    else
                    {
                        GUILayout.Label("<B>Simple Checklist</B>");
                        GUILayout.FlexibleSpace();
                    }
                }

                GUILayout.FlexibleSpace();
                if (!missionRunnerActive)
                {
                    GUILayout.Label("View: ");

                    var names = Enum.GetNames(typeof(View));
                    mission.currentView = (View)Utils.ComboBox.Box(VIEW_COMBO, (int)mission.currentView, names, this, 100, false);
                    if (mission.currentView != activeViewForFrame)
                    {
                        switch (mission.currentView)
                        {
                            case View.Tiny: treeRect.width = TINY_WIDTH; break;
                            case View.Compact: treeRect.width = COMPACT_WIDTH; break;
                            case View.Full: treeRect.width = FULL_WIDTH; break;
                        }
                    }
                }
                else
                {
                    if (mission.currentView != View.Compact)
                    {
                        mission.currentView = View.Compact;
                        treeRect.width = COMPACT_WIDTH;
                    }
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (useKspSkin)
                    GUILayout.Space(15);
                if (mission.simpleChecklist)
                {
                    GUILayout.Label("Done", titleLabel);
                }
                else
                {
                    switch (activeViewForFrame)
                    {
                        case View.Compact:
                            if (!missionRunnerActive)
                                GUILayout.Label("Done|Lock|All | Double-click a title to edit in a separate window.", titleLabel);
                            else
                                GUILayout.Label("Done|All | Double-click a title to edit in a separate window.", titleLabel);
                            break;

                        case View.Tiny:
                            if (!missionRunnerActive)
                                GUILayout.Label("Done|Lock|All", titleLabel);
                            else
                                GUILayout.Label("Done|All", titleLabel);
                            break;

                        case View.Full:
                            if (!missionRunnerActive)
                                GUILayout.Label("Done|Lock|All | Double-click a title to edit in a separate window. Use ▲ ▼ ⤴ ⤵ Move… Dup + ✖ to manage hierarchy.", titleLabel);
                            else
                                GUILayout.Label("Done|All | Double-click a title to edit in a separate window. Use ▲ ▼ ⤴ ⤵ Move… Dup + ✖ to manage hierarchy.", titleLabel);
                            break;
                    }
                }
                {
                    GUIStyle style = GUI.skin.label;

                    var content = new GUIContent("", "Show items which might need adjustments for different vessels  ");
                    Vector2 size = style.CalcSize(content);
                    showVesselSpecific = GUILayout.Toggle(showVesselSpecific, content);
                    content = new GUIContent("Vessel Specific", "Show items which might need adjustments for different vessels  ");
                    size = style.CalcSize(content);
                    GUILayout.Label(content, GUILayout.Width(size.x));
                }
            }
            GUILayout.Space(4);

            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < mission.roots.Count; i++)
            {
                var r = mission.roots[i];
                Guid trackedVesselGuid = new Guid();
                if (r.data.stepType == CriterionType.TrackedVessel)
                    trackedVesselGuid = r.data.vesselGuid;
                DrawNodeRow(r, 0, trackedVesselGuid, mission.missionActive);
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();
#if false
            // --- Column tuning sliders (moved here to bottom) ---
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Title Width %: {0}", (int)Mathf.Round(_titleWidthPct * 100f)), ScaledGUILayoutWidth(140));
            _titleWidthPct = GUILayout.HorizontalSlider(_titleWidthPct, 0.40f, 0.95f, ScaledGUILayoutWidth(160));
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Controls Pad: {0}px", (int)_controlsPad), ScaledGUILayoutWidth(140));
            _controlsPad = Mathf.Round(GUILayout.HorizontalSlider(_controlsPad, 0f, 120f, ScaledGUILayoutWidth(160)));
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Indent/Level: {0}px", (int)_indentPerLevel), ScaledGUILayoutWidth(140));
            _indentPerLevel = Mathf.Round(GUILayout.HorizontalSlider(_indentPerLevel, 10f, 40f, ScaledGUILayoutWidth(160)));
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Fold Col: {0}px", (int)_foldColWidth), ScaledGUILayoutWidth(120));
            _foldColWidth = Mathf.Round(GUILayout.HorizontalSlider(_foldColWidth, 16f, 48f, ScaledGUILayoutWidth(160)));

            GUILayout.Space(16);
            if (GUILayout.Button("Reset", ScaledGUILayoutWidth(80)))
            {
                _titleWidthPct = 0.75f;
                _controlsPad = 40f;
                _indentPerLevel = 18f;
                _foldColWidth = 24f;
                SaveUISettings();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
#endif

            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New", ScaledGUILayoutWidth(90)))
                {
                    YesNoDialogShow(
                            title: "Confirm New Mission",
                            message: "Are you sure you want to start a new mission?\n" +
                            "(will save current mission)",
                            onYes: OnConfirmNewMissionYes
                        );

                    //showNewConfirm = true;
                    //var mp = Input.mousePosition;
                    //newConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - newConfirmRect.width - 40);
                    //newConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - newConfirmRect.height - 40);
                }

                if (GUILayout.Button("Clear All", ScaledGUILayoutWidth(90)))
                {
                    YesNoDialogShow(
                          title: "Confirm Mission Clear",
                          message: "Are you sure you want to clear this mission?",
                          onYes: OnConfirmMissionClearYes
                      );


                    //clearAddSample = false;
                    //showClearConfirm = true;
                    //var mp = Input.mousePosition;
                    //clearConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - clearConfirmRect.width - 40);
                    //clearConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - clearConfirmRect.height - 40);
                }

                if (GUILayout.Button("Save", ScaledGUILayoutWidth(90)))
                {
                    if (IsNullOrWhiteSpace(mission.missionName) || mission.missionName == "Untitled Mission")
                    {
                        TextEntryDialogShow(
                                    title: "Enter New Mission Name",
                                    message: "Please enter the mission name:",
                                    onOk: OnSaveOK
                            );

                        return;
                    }

                    TrySaveToDisk_Internal(true);
                }

                if (!missionRunnerActive)
                {
                    if (GUILayout.Button("Save As…", ScaledGUILayoutWidth(100)))
                    {
                        saveAsName = "";
                        OpenSaveAsDialog();
                    }
                }
                if (GUILayout.Button(missionRunnerActive ? "Load Active…" : "Load/Import…", ScaledGUILayoutWidth(120)))
                    OpenLoadDialog();
                GUILayout.FlexibleSpace();
                if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().deltaVEditorActive)
                {
                    if (GUILayout.Button("DeltaV Editor"))
                        openDeltaVEditor = true;

                    GUILayout.FlexibleSpace();
                }

                if (Time.realtimeSinceStartup <= lastSaveShownUntil && !String.IsNullOrEmpty(lastSaveInfo))
                {
                    var style = lastSaveWasSuccess ? badge : badgeError;
                    GUILayout.Label(lastSaveInfo, style);
                    GUILayout.FlexibleSpace();
                }

                if (activeViewForFrame != View.Tiny)
                    GUILayout.Label(string.Format("Count: {0}", CountAll()), badge);
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(70)))
                {
                    visible = false;
                    toolbarControl.SetFalse(true);
                }
                GUILayout.Space(20);
            }

            this.resizeHandle.Draw(ref this.treeRect);

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }


        private ResizeHandle resizeHandle = null;

        private void DrawNewConfirmWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);
            GUILayout.Label("Create a new mission. Save current mission first?", hintLabel);

            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", ScaledGUILayoutWidth(100)))
                {
                    TrySaveToDisk_Internal(true);
                    showNewConfirm = false;
                    OpenSaveAsDialog();
                }
                if (GUILayout.Button("Discard", ScaledGUILayoutWidth(100)))
                {
                    showNewConfirm = false;
                    OpenSaveAsDialog();
                }
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100)))
                {
                    showNewConfirm = false;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public string OneLineSummary(StepNode node)
        {
            string criteria = "";
            switch (node.data.stepType)
            {
                case CriterionType.PartGroup:
                    switch (node.data.partGroup)
                    {
                        case PartGroup.Batteries:
                            criteria = node.data.batteryCapacity.ToString() + " EC";
                            break;

                        case PartGroup.Communication:
                            criteria = node.data.antennaPower.ToString();  // may need a suffix added 
                            break;

                        case PartGroup.ControlSource:
                            criteria = $"{node.data.controlSourceQty} needed";
                            break;

                        case PartGroup.DockingPort:
                            criteria = node.data.dockingPortQty.ToString() + " docking ports";
                            break;

                        case PartGroup.Drills:
                            criteria = node.data.drillQty.ToString() + " drills";
                            break;

                        case PartGroup.Engines:
                            for (int i = 0; i < Initialization.engineTypesAr.Length; i++)
                            {
                                if (Initialization.engineTypesAr[i] == node.data.engineType)
                                {
                                    criteria = Initialization.engineTypesDisplayAr[i];
                                    break;
                                }
                            }
                            break;

                        case PartGroup.FuelCells:
                            {
                                float chargeRate = node.data.fuelCellChargeRate; //  Utils.FlightChecks.chargeRate;
                                criteria = $"Charge rate: {chargeRate}";
                                break;
                            }
                        case PartGroup.Generators:
                            criteria = node.data.generatorChargeRate.ToString() + " EC/sec";
                            break;
                        case PartGroup.Lights:
                            criteria = node.data.spotlights.ToString() + " spotlights";
                            break;
                        case PartGroup.Parachutes:
                            criteria = node.data.parachutes.ToString() + " parachutes";
                            break;

                        case PartGroup.Radiators:
                            {
                                float coolingRate = node.data.radiatorCoolingRate;
                                criteria = $"Cooling rate: {coolingRate} kW";
                                break;
                            }

                        case PartGroup.RCS:
                            for (int i = 0; i < Initialization.rcsTypesAr.Length; i++)
                            {
                                if (Initialization.rcsTypesAr[i] == node.data.rcsType)
                                {
                                    criteria = Initialization.rcsTypesDisplayAr[i];
                                    break;
                                }
                            }
                            break;

                        case PartGroup.ReactionWheels:
                            criteria = node.data.reactionWheels.ToString() + " reaction wheels";
                            break;

                        case PartGroup.SolarPanels:
                            {
                                float chargeRate = node.data.solarChargeRate; //  Utils.FlightChecks.chargeRate;
                                criteria = $"Charge rate: {chargeRate} EC/sec";
                            }
                            break;

                    }
                    criteria = " " + node.data.partGroup.ToString() + ": " + criteria;
                    break;

                case CriterionType.ChargeRateTotal:
                    criteria = node.data.chargeRateTotal.ToString() + " EC/sec";
                    break;

                case CriterionType.CrewCount:
                    criteria = node.data.crewCount.ToString() + " kerbals";
                    break;

                case CriterionType.CrewMemberTrait:
                    criteria = node.data.traitName;
                    break;


                case CriterionType.Destination:
                    switch (node.data.destType)
                    {
                        case DestinationType.Asteroid:
                            criteria = node.data.destAsteroid;
                            break;
                        case DestinationType.Body:
                            criteria = node.data.destBody;
                            break;
                        case DestinationType.Vessel:
                            criteria = node.data.destVessel;
                            break;

                    }
                    break;

                case CriterionType.Flags:
                    {
                        int flagCount = FlightChecks.flagCount;
                        criteria = $"Need to plant {flagCount} flags";
                        break;
                    }

                case CriterionType.Maneuver:
                    criteria = StringFormatter.BeautifyName(node.data.maneuver.ToString());

                    switch (node.data.maneuver)
                    {
                        case Maneuver.Launch:
                        case Maneuver.Orbit:
                            criteria += $": {node.data.maneuverBody} Target Orbit: Ap: {node.data.ap} km   Pe: {node.data.pe} km";
                            break;

                        case Maneuver.SubOrbitalLaunch:
                            criteria += $": {node.data.maneuverBody} Target Ap: {node.data.ap} km";
                            break;

                        case Maneuver.ImpactAsteroid:
                        case Maneuver.InterceptAsteroid:
                            criteria += ": " + node.data.destAsteroid;
                            break;

                        case Maneuver.FineTuneClosestApproach:
                        case Maneuver.InterceptVessel:
                        case Maneuver.MatchPlanesWithVessel:
                        case Maneuver.MatchVelocitiesWithVessel:
                            criteria += ": " + node.data.destVessel;
                            break;

                        case Maneuver.Reentry:
                        case Maneuver.Landing:
                        case Maneuver.Splashdown:
                        case Maneuver.TransferToAnotherPlanet:
                            criteria += ": " + node.data.destBody;
                            break;

                        case Maneuver.ResourceTransfer:
                            criteria += ": ";
                            for (int i = 0; i < node.data.resourceList.Count; i++)
                            {
                                ResInfo resinfo = node.data.resourceList[i];
                                if (i > 0)
                                    criteria += ", ";
                                criteria += resinfo.resourceName;
                            }
                            break;

                        default: break;
                    }
                    break;

                case CriterionType.Module:
                    criteria = node.data.moduleName;
                    break;

                case CriterionType.Part:
                    if (node.data.partName != null)
                        criteria = node.data.partTitle;
                    else
                        criteria = "Unnamed Part";
                    break;

                case CriterionType.Resource:
                    for (int i = 0; i < node.data.resourceList.Count; i++)
                    {
                        ResInfo resinfo = node.data.resourceList[i];
                        if (criteria.Length > 0)
                            criteria += ", ";
                        criteria += resinfo.resourceName;

                    }
                    break;

                case CriterionType.SAS:
                    criteria = SASUtils.SasLevelDescriptions[node.data.minSASLevel];
                    break;

                case CriterionType.Staging:
                    {
                        bool rc = StageUtility.StageHasDecouplerOrSeparator(node.data.stage, out criteria, node.data.includeDockingPort);

                        criteria = $"Stage {node.data.stage} staging via: " + (rc ? criteria : "(none)");
                        break;
                    }

                case CriterionType.TrackedVessel:
                    criteria = StringFormatter.BeautifyName(node.data.stepType.ToString()) + $": {node.data.trackedVessel}";
                    break;

                case CriterionType.VABOrganizerCategory:
                    criteria = StringFormatter.BeautifyName(node.data.vabCategory);
                    break;


                default:
                    break;
            }
            return ": " + criteria;
        }

        private void DrawNodeRow(StepNode node, int depth, Guid trackedVesselGuid, bool missionActive)
        {
            Rect titleRect = new Rect();
            bool up, down, promote, demote, moveTo, dup, add, del;
            float indent = 0;
            string criteria = "";

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    // The following "if" is formatted this way to make it easier to add additional criteria types to be associated
                    // with a specific stage
                    if (!showVesselSpecific ||
                        (node.data.stepType == CriterionType.PartGroup && node.data.partGroup == PartGroup.Engines) ||
                        node.data.stepType == CriterionType.Staging)
                    {

                        if (!useKspSkin)
                            GUILayout.Space(15);

                        if (node.data.IsStepActive)
                            GUILayout.Toggle(node.data.completed, new GUIContent("", "Indicates if step is completed"));
                        else
                            node.data.completed = GUILayout.Toggle(node.data.completed, new GUIContent("", "Mark this line as completed"));

                        if (mission.simpleChecklist)
                        {

                        }
                        else
                        {
                            if (useKspSkin)
                                GUILayout.Space(10);
                            else
                                GUILayout.Space(15);

                            if (!missionRunnerActive)
                            {
                                node.data.locked = GUILayout.Toggle(node.data.locked, new GUIContent("", "Lock this line"));
                                if (useKspSkin)
                                    GUILayout.Space(1);
                                else
                                    GUILayout.Space(5);
                            }
                            if (node.data.IsStepActive)
                            {
                                GUILayout.Toggle(node.requireAll, new GUIContent("", "Indicates if all children need to be completed for this to be completed"));
                            }
                            else
                            {
                                bool b = GUILayout.Toggle(node.requireAll, new GUIContent("", "Require all children to be fulfilled for this to be fulfilled"));
                                if (!node.data.locked)
                                    node.requireAll = b;
                            }
                        }
                        indent = 12f + indentPerLevel * depth;
                        GUILayout.Space(indent);

                        if (node.Children.Count == 0)
                        {
                            GUILayout.Label(" ", ScaledGUILayoutWidth(foldColWidth));
                        }
                        else
                        {
                            string sign = node.Expanded ? "-" : "+";
                            if (GUILayout.Button("<B>" + sign + "</B>", titleLabel, ScaledGUILayoutWidth(foldColWidth))) node.Expanded = !node.Expanded;
                        }

                        const float controlsBase = 100f;
                        float controlsWidth = controlsBase + controlsPad;
                        float available = Mathf.Max(120f, treeRect.width - indent - foldColWidth - controlsWidth - 12f);
                        float titleWidth = Mathf.Clamp(available * titleWidthPct, HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings2>().titleColumnWidth, available);

                        var style = (selectedNode == node) ? selectedTitleLabel : titleLabel;

                        bool fulfilled = mission.missionActive ? FlightChecks.CheckChildStatus(node, trackedVesselGuid: trackedVesselGuid) : true;
                        if (!fulfilled)
                        {
                            style = (selectedNode == node) ? selectedUnfilledTitleLabel : unfilledTitleLabel;
                        }

                        if (showDetail)
                        {
                            criteria = OneLineSummary(node);
                        }

                        if (HighLogic.LoadedSceneIsFlight && node.data.stepType == CriterionType.TrackedVessel && node.data.trackedVessel == "")
                        {
                            titleWidth /= 2;
                        }

                        GUIContent titleContent = new GUIContent(node.data.title, "Double-click to open edit window");
                        GUILayout.Label(titleContent, style, ScaledGUILayoutWidth(titleWidth));
                        titleRect = GUILayoutUtility.GetLastRect();

                        if (HighLogic.LoadedSceneIsFlight && missionActive && node.data.stepType == CriterionType.TrackedVessel && node.data.trackedVessel == "")
                        {
                            if (GUILayout.Button(" Activate using Current Vessel "))
                            {
                                node.data.trackedVessel = Localizer.Format(FlightGlobals.ActiveVessel.vesselName);
                                node.data.vesselGuid = FlightGlobals.ActiveVessel.id;
                                node.data.stepStatus = StepStatus.Active;

                                if (node.data.experience > 0 || node.data.reputation > 0 || node.data.funding > 0)
                                {
                                    node.data.stepStatus = StepStatus.Active;
                                }

                                criteria = OneLineSummary(node);
                                if (criteria.StartsWith(node.data.stepType.ToString()))
                                    node.data.title = criteria;
                                else
                                    node.data.title = StringFormatter.BeautifyName(node.data.stepType.ToString()) + criteria;
                            }
                        }


                        up = down = promote = demote = moveTo = dup = add = del = false;

                        GUILayout.FlexibleSpace();
                        //if (currentView == View.Full)
                        if (activeViewForFrame == View.Full)
                        {
                            up = GUILayout.Button(RegisterToolbar.upContent, smallBtn);
                            down = GUILayout.Button(RegisterToolbar.downContent, smallBtn);
                            promote = GUILayout.Button(RegisterToolbar.promoteContent, smallBtn);
                            demote = GUILayout.Button(RegisterToolbar.demoteContent, smallBtn);

                            var iconButtonWidth = GUILayout.Width(20 * GameSettings.UI_SCALE);
                            var iconButtonheight = GUILayout.Height(20 * GameSettings.UI_SCALE);

                            moveTo = GUILayout.Button(moveContent, buttonIconStyle, iconButtonWidth, iconButtonheight);
                            GUILayout.Space(20);

                            dup = GUILayout.Button(duplicateContent, buttonIconStyle, iconButtonWidth, iconButtonheight);

                            GUILayout.Space(20);
                            add = GUILayout.Button(RegisterToolbar.addContent, smallBtn); // /* "⊕" */, ScaledGUILayoutWidth(28));
                            del = GUILayout.Button(RegisterToolbar.deleteContent, smallBtn);
                            GUILayout.Space(5);
                        }


                        if (activeViewForFrame != View.Tiny)
                            HandleTitleClicks(node, titleRect);

                        //if (currentView == View.Full)
                        if (activeViewForFrame == View.Full)
                        {
                            if (up) MoveUp(node);
                            if (down) MoveDown(node);
                            if (promote) Promote(node);
                            if (demote) Demote(node);
                            if (moveTo) OpenMoveDialog(node);
                            if (dup) Duplicate(node);
                            if (add)
                            {
                                var child = node.AddChild();
                                node.Expanded = true;
                                selectedNode = child;
                                OpenDetail(child);
                            }
                            if (del) Delete(node);
                        }
                        if (showDetail)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(140 + indent);
                                if (criteria != "")
                                    GUILayout.Label("(" + StringFormatter.BeautifyName(node.data.stepType.ToString()) + " " + criteria + ")", smallLabel);
                                else
                                    GUILayout.Label("(" + StringFormatter.BeautifyName(node.data.stepType.ToString()) + ")", smallLabel);
                            }
                        }
                    }
                }
                if (node.Expanded && node.Children.Count > 0)
                {
                    GUILayout.Space(2);
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        StepNode c = node.Children[i];
                        if (c.data.stepType == CriterionType.TrackedVessel)
                            trackedVesselGuid = c.data.vesselGuid;
                        DrawNodeRow(c, depth + 1, trackedVesselGuid, missionActive);
                        GUILayout.Space(1);
                    }
                }
            }

            ToolTips.ShowToolTip(treeRect);
        }


        private void HandleTitleClicks(StepNode node, Rect titleRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && titleRect.Contains(e.mousePosition)) e.Use();
            if (e.type == EventType.MouseUp && e.button == 0 && titleRect.Contains(e.mousePosition))
            {
                if (lastClickedId == node.Id && (Time.realtimeSinceStartup - lastClickTime) <= DoubleClickSec)
                {
                    OpenDetail(node);
                    lastClickedId = -1;
                }
                else
                {
                    selectedNode = node;
                    lastClickedId = node.Id;
                    lastClickTime = Time.realtimeSinceStartup;
                }
                e.Use();
            }
        }

        private void MoveUp(StepNode n)
        {
            var list = (n.Parent == null) ? mission.roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx > 0) { list.RemoveAt(idx); list.Insert(idx - 1, n); }
        }

        private void MoveDown(StepNode n)
        {
            var list = (n.Parent == null) ? mission.roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx >= 0 && idx < list.Count - 1) { list.RemoveAt(idx); list.Insert(idx + 1, n); }
        }

        private void Promote(StepNode n)
        {
            if (n.Parent == null) return;
            var parent = n.Parent;
            var oldList = parent.Children;
            int oldIdx = oldList.IndexOf(n);
            if (oldIdx >= 0) oldList.RemoveAt(oldIdx);

            var newList = (parent.Parent == null) ? mission.roots : parent.Parent.Children;
            int parentIdx = newList.IndexOf(parent);
            int insertAt = (parentIdx >= 0) ? parentIdx + 1 : newList.Count;
            newList.Insert(insertAt, n);
            n.Parent = parent.Parent;
        }

        private void Demote(StepNode n)
        {
            var list = (n.Parent == null) ? mission.roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx <= 0) return;
            StepNode prev = list[idx - 1];

            list.RemoveAt(idx);
            prev.Children.Add(n);
            n.Parent = prev;
            prev.Expanded = true;
        }

        private void Delete(StepNode n)
        {
            Log.Info("delete, id: " + n.Id);
            if (n.Parent == null)
                Log.Info("n.Parent is null");
            if (n.Parent == null)
                mission.roots.Remove(n);
            else
            {
                for (int i = 0; i < n.Parent.Children.Count; i++)
                {
                    var v = n.Parent.Children[i];
                    Log.Info("v.Id: " + v.Id);
                    if (v.Id == n.Id)
                    {
                        Log.Info("Deleting id: " + v.Id);
                        n.Parent.Children.RemoveAt(i);
                        break;
                    }
                }
            }
            if (selectedNode == n) selectedNode = null;
            if (detailNode == n) detailNode = null;
        }

        private void Duplicate(StepNode n)
        {
            var cloned = new StepNode(n);
            cloned.data.title = (n.data.title ?? "New Step") + " (copy)";

            var list = (n.Parent == null) ? mission.roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            list.Insert(Mathf.Clamp(idx + 1, 0, list.Count), cloned);
        }

        private void OpenMoveDialog(StepNode node)
        {
            if (detailNode != null &&
                detailNode != node &&
                HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                TrySaveToDisk_Internal(true);

            moveNode = node;
            newWindow = true;
            moveTargetParent = null;
            showMoveDialog = true;

            var mp = Input.mousePosition;
            moveRect.x = Mathf.Clamp(mp.x, 40, Screen.width - moveRect.width - 40);
            moveRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - moveRect.height - 40);
        }

        private void DrawMoveDialogWindow(int id)
        {
            BringWindowForward(id);
            GUILayout.Space(6);
            GUILayout.Label("Choose a new parent (or Root). You cannot move an item under itself or its descendants.", hintLabel);

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                bool rootChosen = (moveTargetParent == null);
                if (GUILayout.Toggle(rootChosen, "⟂ Root", HighLogic.Skin.toggle, ScaledGUILayoutWidth(120)))
                    moveTargetParent = null;
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            GUILayout.Label("Or select an existing parent:", tinyLabel);

            moveScroll = GUILayout.BeginScrollView(moveScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var r in mission.roots)
                DrawMoveTargetRecursive(r, 0, moveNode);
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OK", ScaledGUILayoutWidth(100)))
                {
                    MoveToParent(moveNode, moveTargetParent);
                    CloseMoveDialog();
                }
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100))) CloseMoveDialog();
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawMoveTargetRecursive(StepNode node, int depth, StepNode moving)
        {
            if (node == moving || IsDescendant(moving, node)) return;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(12 + indentPerLevel * depth);
                bool chosen = (moveTargetParent == node);
                string label = string.Format("📁 {0}", node.data.title);
                if (GUILayout.Toggle(chosen, label, HighLogic.Skin.toggle))
                    moveTargetParent = node;
                GUILayout.FlexibleSpace();
            }

            foreach (var c in node.Children) DrawMoveTargetRecursive(c, depth + 1, moving);
        }

        private void CloseMoveDialog()
        {
            showMoveDialog = false;
            moveNode = null;
            moveTargetParent = null;
        }

        private void MoveToParent(StepNode n, StepNode newParent)
        {
            if (n == null) return;
            if (newParent == n || IsDescendant(n, newParent)) return;

            var oldList = (n.Parent == null) ? mission.roots : n.Parent.Children;
            oldList.Remove(n);

            if (newParent == null)
            {
                mission.roots.Add(n);
                n.Parent = null;
            }
            else
            {
                newParent.Children.Add(n);
                n.Parent = newParent;
                newParent.Expanded = true;
            }
        }

        private void OpenDetail(StepNode node)
        {
            if (detailNode != null &&
                detailNode != node &&
                HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                TrySaveToDisk_Internal(true);

            Log.Info("OpenDetail");
            if (node == null)
                Log.Info("OpenDetail, node is null");
            detailNode = node;
            newWindow = true;

            var mp = Input.mousePosition;
            detailRect.x = Mathf.Clamp(mp.x, 40, Screen.width - detailRect.width - 40);
            detailRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - detailRect.height - 40);
        }


        void OnSaveAsOK(string str)
        {
            saveAsName = str;
            mission.missionName = str;
            TrySaveToDisk_Internal(true);
            //OpenSaveAsDialog();
        }

        void OnSaveAsOK2(string str)
        {
            saveAsName = str;
            mission.missionName = str;
            OnConfirmNewMissionYes();
            //TrySaveToDisk_Internal(true);
            //OpenSaveAsDialog();
        }


        void OnSaveOK(string str)
        {
            mission.missionName = str;
            TrySaveToDisk_Internal(true);
        }

        private void OpenSaveAsDialog()
        {

            // if (IsNullOrWhiteSpace(saveAsName) || saveAsName == "Untitled Mission")
            {
                TextEntryDialogShow(
                            title: "Enter New Mission Name",
                            message: "Please enter the mission name:",
                            onOk: OnSaveAsOK
                    );

                return;
            }
        }

        private void SetAllExpanded(bool ex) { foreach (var r in mission.roots) SetExpandedRecursive(r, ex); }
        private void SetExpandedRecursive(StepNode n, bool ex) { n.Expanded = ex; foreach (var c in n.Children) SetExpandedRecursive(c, ex); }

        private bool IsDescendant(StepNode ancestor, StepNode maybeDescendant)
        {
            if (ancestor == null || maybeDescendant == null) return false;
            var p = maybeDescendant.Parent;
            while (p != null) { if (p == ancestor) return true; p = p.Parent; }
            return false;
        }

        private int CountAll() { int count = 0; foreach (var r in mission.roots) count += CountRecursive(r); return count; }
        private int CountRecursive(StepNode n) { int c = 1; foreach (var ch in n.Children) c += CountRecursive(ch); return c; }
        internal static void ReparentAll(Mission mission) { foreach (var r in mission.roots) { r.Parent = null; ReparentRecursive(r); } }
        private static void ReparentRecursive(StepNode n) { foreach (var c in n.Children) { c.Parent = n; ReparentRecursive(c); } }

        internal class MissionFileInfo
        {
            public string FullPath;
            public string SaveName;
            public string MissionName;

            public bool stock = false;
            public bool active = false;
        }

        IEnumerator WaitForEditor()
        {
            while (EditorLogic.fetch == null || EditorLogic.fetch.ship == null)
                yield return null;
            while (EditorLogic.fetch.ship.parts.Count == 0)
                yield return new WaitForEndOfFrame();
            while (EditorLogic.fetch.ship.vesselDeltaV == null || EditorLogic.fetch.ship.vesselDeltaV.OperatingStageInfo == null)
                yield return new WaitForEndOfFrame();
            for (int i = 0; i < 20; i++)
                yield return new WaitForEndOfFrame();

            StageInfo.Init();
            waitForEditorCoroutine = null;
        }

        private Coroutine waitForEditorCoroutine = null;

        void onEditorStarted()
        {
            if (waitForEditorCoroutine == null)
                waitForEditorCoroutine = StartCoroutine(WaitForEditor());
        }
        void onEditorLoad(ShipConstruct sc, CraftBrowserDialog.LoadType lt)
        {
            if (waitForEditorCoroutine == null)
                waitForEditorCoroutine = StartCoroutine(WaitForEditor());
        }
        void onEditorShipModified(ShipConstruct sc)
        {
            if (waitForEditorCoroutine == null)
                waitForEditorCoroutine = StartCoroutine(WaitForEditor());
        }
        void onVesselChange(Vessel v)
        {
            StageInfo.Init();
        }
        void onVesselSwitching(Vessel from, Vessel to)
        {
            LoadTrackedVesselMission(to.id);
        }

        List<StepNode> trackedVesselUpdated = new List<StepNode>();
        void onLevelWasLoadedGUIReady(GameScenes gs)
        {
            if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
            {
                trackedVesselUpdated.Clear();

                // Find all steps of Criterion.trackedVessel, if no vessel specified, then assign the active vessel
                for (int i = 0; i < mission.roots.Count; i++)
                {
                    StepNode r = mission.roots[i];
                    CheckNode(r);
                    CheckChildren(r);
                }
            }
            StageInfo.Init();
        }

        // Need to be able to revert this
        void CheckChildren(StepNode node)
        {
            for (int i = 0; i < node.Children.Count; i++)
            {
                CheckNode(node.Children[i]);
                CheckChildren(node.Children[i]);
            }
        }

        void CheckNode(StepNode c)
        {
            if (c.data.stepType == CriterionType.TrackedVessel)
            {
                //if (string.IsNullOrEmpty(c.data.trackedVessel))
                //{
                //    c.data.trackedVessel = FlightGlobals.ActiveVessel.vesselName;
                //    c.data.vesselGuid = FlightGlobals.ActiveVessel.id;
                //    trackedVesselUpdated.Add(c);
                //}
            }
        }

        void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fta)
        {
            if (fta.from == GameScenes.FLIGHT && fta.to == GameScenes.EDITOR)
            {
                foreach (var tvu in trackedVesselUpdated)
                {
                    tvu.data.trackedVessel = "";
                    tvu.data.vesselGuid = Guid.Empty;
                }
            }
            toolbarControl.SetFalse(true);
        }

        private Texture2D MakeSolidTexture(int w, int h, Color c)
        {
            var t = new Texture2D(w, h, TextureFormat.ARGB32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            t.SetPixels(px); t.Apply();
            return t;
        }

        private string GetUIFileAbsolute() { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", SAVE_MOD_FOLDER, UI_FILE_NAME); }

        private static ConfigNode RectToNode(string name, Rect r)
        {
            var n = new ConfigNode(name);
            n.AddValue("x", r.x);
            n.AddValue("y", r.y);
            n.AddValue("w", r.width);
            n.AddValue("h", r.height);
            return n;
        }

        private static Rect NodeToRect(ConfigNode n, Rect fallback)
        {
            if (n == null) return fallback;
            float x = fallback.x, y = fallback.y, w = fallback.width, h = fallback.height;

            x = n.SafeLoad("x", x);
            x = n.SafeLoad("y", y);
            x = n.SafeLoad("w", w);
            x = n.SafeLoad("h", h);

            //float.TryParse(n.GetValue("x"), out x);
            //float.TryParse(n.GetValue("y"), out y);
            //float.TryParse(n.GetValue("w"), out w);
            //float.TryParse(n.GetValue("h"), out h);

            return new Rect(x, y, w, h);
        }

        private void SaveUISettings()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(KSPUtil.ApplicationRootPath, "GameData", SAVE_MOD_FOLDER));
                var root = new ConfigNode(UI_ROOT_NODE);
                root.AddValue("UseKspSkin", useKspSkin);
                root.AddValue("TitleWidthPct", titleWidthPct);
                root.AddValue("ControlsPad", controlsPad);
                root.AddValue("IndentPerLevel", indentPerLevel);
                root.AddValue("FoldColWidth", foldColWidth);

                root.AddNode(RectToNode("treeRect", treeRect));
                root.AddNode(RectToNode("detailRect", detailRect));
                root.AddNode(RectToNode("moveRect", moveRect));
                root.AddNode(RectToNode("loadRect", loadRect));
                root.AddNode(RectToNode("overwriteRect", overwriteRect));
                root.AddNode(RectToNode("deleteRect", deleteRect));
                root.AddNode(RectToNode("partRect", partRect));

                root.AddNode(RectToNode("deltaVRect ", deltaVRect));
                root.AddNode(RectToNode("moduleRect ", moduleRect));
                root.AddNode(RectToNode("SASRect ", SASRect));
                root.AddNode(RectToNode("CategoryRect", CategoryRect));
                root.AddNode(RectToNode("bodyAsteroidVesselRect ", bodyAsteroidVesselRect));
                root.AddNode(RectToNode("traitRect ", traitRect));

                root.AddNode(RectToNode("saveAsRect", saveAsRect));
                root.AddNode(RectToNode("newConfirmRect", newConfirmRect));
                root.AddNode(RectToNode("clearConfirmRect", clearConfirmRect));
                root.AddNode(RectToNode("summaryRect", summaryRect));

                root.Save(GetUIFileAbsolute());
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] SaveUISettings failed: " + ex);
            }
        }

        private void LoadUISettings()
        {
            try
            {
                string path = GetUIFileAbsolute();
                if (!File.Exists(path)) return;

                var root = ConfigNode.Load(path);
                if (root == null || root.name != UI_ROOT_NODE) return;

                useKspSkin = root.SafeLoad("UseKspSkin", useKspSkin);
                titleWidthPct = root.SafeLoad("TitleWidthPct", titleWidthPct);
                controlsPad = root.SafeLoad("ControlsPad", controlsPad);
                indentPerLevel = root.SafeLoad("IndentPerLevel", indentPerLevel);
                foldColWidth = root.SafeLoad("FoldColWidth", foldColWidth);


                //bool.TryParse(root.GetValue("UseKspSkin"), out _useKspSkin);
                //float.TryParse(root.GetValue("TitleWidthPct"), out _titleWidthPct);
                //float.TryParse(root.GetValue("ControlsPad"), out _controlsPad);
                //float.TryParse(root.GetValue("IndentPerLevel"), out _indentPerLevel);
                //float.TryParse(root.GetValue("FoldColWidth"), out _foldColWidth);

                if (titleWidthPct <= 0f) titleWidthPct = 0.75f;
                controlsPad = Mathf.Clamp(controlsPad, 0f, 120f);
                indentPerLevel = Mathf.Clamp(indentPerLevel, 10f, 40f);
                foldColWidth = Mathf.Clamp(foldColWidth, 16f, 48f);

                treeRect = NodeToRect(root.GetNode("treeRect"), treeRect);
                detailRect = NodeToRect(root.GetNode("detailRect"), detailRect);
                moveRect = NodeToRect(root.GetNode("moveRect"), moveRect);
                loadRect = NodeToRect(root.GetNode("loadRect"), loadRect);
                overwriteRect = NodeToRect(root.GetNode("overwriteRect"), overwriteRect);
                deleteRect = NodeToRect(root.GetNode("deleteRect"), deleteRect);
                partRect = NodeToRect(root.GetNode("partRect"), partRect);

                deltaVRect = NodeToRect(root.GetNode("deltaVRect "), deltaVRect);
                moduleRect = NodeToRect(root.GetNode("moduleRect "), moduleRect);
                SASRect = NodeToRect(root.GetNode("SASRect "), SASRect);
                CategoryRect = NodeToRect(root.GetNode("CategoryRect"), CategoryRect);
                bodyAsteroidVesselRect = NodeToRect(root.GetNode("bodyAsteroidVesselRect "), bodyAsteroidVesselRect);
                traitRect = NodeToRect(root.GetNode("traitRect"), traitRect);

                saveAsRect = NodeToRect(root.GetNode("saveAsRect"), saveAsRect);
                newConfirmRect = NodeToRect(root.GetNode("newConfirmRect"), newConfirmRect);
                clearConfirmRect = NodeToRect(root.GetNode("clearConfirmRect"), clearConfirmRect);
                summaryRect = NodeToRect(root.GetNode("summaryRect"), summaryRect);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] LoadUISettings failed: " + ex);
            }
        }
    }
}
