// File: MissionPlanner.cs
// Mod: MissionPlanner

using ClickThroughFix;
using KSP.UI.Screens;
using MissionPlanner.Utils;
using SpaceTuxUtility;
using System;
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

        const float MAX_TITLE_WIDTH = 300;


        internal const string MODID = "Mission Planner";
        internal const string MODNAME = "Mission Planner";
        static ToolbarControl toolbarControl = null;
        
        public static bool openDeltaVEditor = false;

        // ---- Windows ----
        private Rect _treeRect = new Rect(220, 120, 840, 620);
        private Rect _detailRect = new Rect(820, 160, 600, 400); //560
        private Rect _moveRect = new Rect(760, 120, 460, 400); // 560
        private Rect _loadRect = new Rect(720, 100, 560, 540);
        private Rect _overwriteRect = new Rect(760, 180, 520, 220);
        private Rect _deleteRect = new Rect(760, 160, 520, 180);
        private Rect _partRect = new Rect(680, 140, 350, 580);
        private Rect _deltaVRect = new Rect(680, 140, 1000, 580);
        private Rect _moduleRect = new Rect(680, 140, 350, 580);
        private Rect _SASRect = new Rect(680, 140, 350, 400); // 580
        private Rect _CategoryRect = new Rect(680, 140, 300, 580);
        private Rect _bodyAsteroidVesselRect = new Rect(680, 140, 350, 580);
        private Rect _traitRect = new Rect(680, 140, 200, 250); // 560
        private Rect _saveAsRect = new Rect(740, 200, 520, 310); // includes summary
        private Rect _newConfirmRect = new Rect(760, 200, 520, 180);
        private Rect _clearConfirmRect = new Rect(760, 220, 520, 170);
        private Rect _summaryRect = new Rect(760, 220, 520, 320);

        private int _treeWinId, _detailWinId, _moveWinId, _loadWinId, _overwriteWinId, _deleteWinId, _partWinId, _resourceWinId,
            _traitWinId, _moduleWinId, _SASWinId, _saveAsWinId, _newConfirmWinId, _clearConfirmWinId, _summaryWinId;
        private bool _visible = false;
        private bool Hidden = false;
        private bool _visibleBeforePause = true;

        // Skin toggle (persisted)
        internal static bool _useKspSkin = true;

        // Column tuning (persisted)
        private float _titleWidthPct = 0; //0.40f;  // 40–95%
        private float _controlsPad = 40f;    // 0–120 px
        private float _indentPerLevel = 30f;    // 10–40 px
        private float _foldColWidth = 24f;    // 16–48 px

        // Toolbar
        private ApplicationLauncherButton _appButton;
        private Texture2D _iconOn, _iconOff;
        private Texture2D _resizeBg, _resizeBgHover;
        private const string IconOnPath = "MissionPlanner/Icons/tree_on";
        private const string IconOffPath = "MissionPlanner/Icons/tree_off";
        private const string IconOnPath_24 = "MissionPlanner/Icons/tree_on-24";
        private const string IconOffPath_24 = "MissionPlanner/Icons/tree_off-24";

        // Missions I/O
        private const string SAVE_ROOT_NODE = "MISSION_PLANNER";
        private const string TRACKEDSAVE_ROOT_NODE = "MISSION_PLANNER_TRACKED_VESSEL";
        private const string SAVE_LIST_NODE = "ROOTS";
        internal const string SAVE_MOD_FOLDER = "MissionPlanner/PluginData";
        internal const string MISSION_FOLDER = SAVE_MOD_FOLDER + "/Missions";
        internal const string DEFAULT_MISSION_FOLDER = SAVE_MOD_FOLDER + "/DefaultMissions";
        internal const string DELTA_V_FOLDER = SAVE_MOD_FOLDER + "/DeltaVTables";

        private const string SAVE_FILE_EXT = ".cfg";

        // UI persistence
        private const string UI_FILE_NAME = "UI.cfg";
        private const string UI_ROOT_NODE = "MISSION_PLANNER_UI";

        // Mission meta
        private string _missionName = "Untitled Mission";
        private string _missionSummary = ""; // Mission summary
        private bool _simpleChecklist = false;

        // Data
        public static readonly List<StepNode> _roots = new List<StepNode>();

        // Selection / dialogs
        private StepNode _selectedNode = null;
        private StepNode _detailNode = null;
        private bool _showMoveDialog = false;
        private StepNode _moveNode = null;
        private StepNode _moveTargetParent = null;

        // Load dialog
        private bool _showLoadDialog = false;
        private bool _loadShowAllSaves = false;
        private Vector2 _loadScroll;
        private List<MissionFileInfo> _loadList = new List<MissionFileInfo>();

        // Overwrite confirm
        private bool _showOverwriteDialog = false;
        private string _pendingSavePath = null;
        private string _pendingSaveMission = null;

        // Delete confirm
        private bool _showDeleteConfirm = false;
        private MissionFileInfo _deleteTarget;

        // Part picker dialog
        private bool _showPartDialog = false;
        private StepNode _partTargetNode = null;
        private Vector2 _partScroll;
        private string _partFilter = "";
        private bool _partAvailableOnly = true;

        // Resource picker dialog
        //private bool _showResourceDialog = false;
        private StepNode _deltaVTargetNode = null;
        private Vector2 _deltaVScroll;
        private string _deltaVFilter = "";

        // Save As / New
        private bool _showSaveAs = false;
        private string _saveAsName = "";
        private bool _creatingNewMission = false;
        private bool _newMissionAddSample = true;
        private string _saveAsSummaryText = "";

        // New confirm
        private bool _showNewConfirm = false;

        // Clear All
        private bool _showClearConfirm = false;
        private bool _clearAddSample = false;

        private Vector2 _summaryScroll;

        // Scroll
        private Vector2 _scroll;
        private Vector2 _moveScroll;

        // Resize handle for main window
        //        private bool _resizingTree = false;
        //private Vector2 _resizeStartLocal;
        //private Rect _resizeStartRect;
        private const float _resizeHandleSize = 26f;
        private const float _minTreeW = 360f;
        private const float _minTreeH = 420f;

        // Styles
        private GUIStyle _titleLabel = null,
            _unfilledTitleLabel = null,
            _selectedTitleLabel = null,
            _selectedUnfilledTitleLabel = null,
            _smallBtn = null,
            _hintLabel = null,
            _errorLabel = null,
            _errorLargeLabel = null,
            _tinyLabel = null,
            _smallLabel = null,
            _titleEdit = null,
            _badge = null,
            _badgeError = null,
            _cornerGlyph = null;

        bool lastUseKSPSkin = true;

        // Double-click
        private int _lastClickedId = -1;
        private float _lastClickTime = 0f;
        private const float DoubleClickSec = 0.30f;

        // Save indicator
        private string _lastSaveInfo = "";
        private float _lastSaveShownUntil = 0f;
        private const float SaveIndicatorSeconds = 3f;
        private bool _lastSaveWasSuccess = true;
        enum View { Tiny = 0, Compact = 1, Full = 2 };
        View currentView = View.Full;
        private static bool IsNullOrWhiteSpace(string s) { return String.IsNullOrEmpty(s) || s.Trim().Length == 0; }

        public static GUILayoutOption ScaledGUILayoutWidth(float width)
        {
            return GUILayout.Width(width * GameSettings.UI_SCALE);
        }
        public static GUILayoutOption ScaledGUIFontLayoutWidth(float width)
        {
            return GUILayout.Width(width * HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().fontSize / 12f);
        }

        public void Awake()
        {
            _treeWinId = WindowHelper.NextWindowId("treeWin");
            _detailWinId = WindowHelper.NextWindowId("detailWin");
            _moveWinId = WindowHelper.NextWindowId("moveWin");
            _loadWinId = WindowHelper.NextWindowId("loadWin");
            _overwriteWinId = WindowHelper.NextWindowId("overwriteWin");
            _deleteWinId = WindowHelper.NextWindowId("deleteWin");
            _partWinId = WindowHelper.NextWindowId("partWin");
            _resourceWinId = WindowHelper.NextWindowId("resourceWin");
            _traitWinId = WindowHelper.NextWindowId("traitWin");
            _moduleWinId = WindowHelper.NextWindowId("moduleWin");
            _SASWinId = WindowHelper.NextWindowId("SASWin");
            _saveAsWinId = WindowHelper.NextWindowId("saveAsWin");
            _newConfirmWinId = WindowHelper.NextWindowId("newConfirmWin");
            _clearConfirmWinId = WindowHelper.NextWindowId("clearConfirmWin");
            _summaryWinId = WindowHelper.NextWindowId("summaryWin");

            LoadIconsOrFallback();
            LoadUISettings();

            StartCoroutine(Initialization.BackgroundInitialize());

            ReparentAll();

            GameEvents.onHideUI.Add(onHideUI);
            GameEvents.onShowUI.Add(onShowUI);
            //GameEvents.onGamePause.Add(OnGamePaused);
            //GameEvents.onGameUnpause.Add(OnGameUnpaused);

            GameEvents.onVesselSwitching.Add(onVesselSwitching);
            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoadedGUIReady);
            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);

            this.resizeHandle = new ResizeHandle();
        }

        public void Start()
        {
            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(
                    onTrue: () => { _visible = true; },
                    onFalse: () => { _visible = false; },
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
            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                TrySaveToDisk_Internal(true);
            SaveUISettings();

            GameEvents.onHideUI.Remove(onHideUI);
            GameEvents.onShowUI.Remove(onShowUI);

            //GameEvents.onGamePause.Remove(OnGamePaused);
            //GameEvents.onGameUnpause.Remove(OnGameUnpaused);

            GameEvents.onVesselSwitching.Remove(onVesselSwitching);
            GameEvents.onLevelWasLoadedGUIReady.Remove(onLevelWasLoadedGUIReady);

            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);

            if (_appButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_appButton);
                _appButton = null;
            }
        }

        void CloseAllDialogs()
        {
            _showMoveDialog = false;
            _showLoadDialog = false; // ???
            _showOverwriteDialog = false; // ???
            _showDeleteConfirm = false;
            _showPartDialog = false;
            _showSaveAs = false;    // ???
            _showNewConfirm = false; // ???
            _showClearConfirm = false;
            _showTraitDialog =
                _showModuleDialog =
                // _showSASDialog =
                _showBodyAsteroidVesselDialog = false;
        }

        bool newWindow = false;
        void BringWindowForward(int id, bool force = false)
        {
            if (newWindow || force)
            {
                newWindow = false;
                GUI.BringWindowToFront(id);
            }
        }

        public void OnGUI()
        {
            if ( /* (PauseMenu.exists && PauseMenu.isOpen) || */ Hidden)
                return;
            //if (_useKspSkin) GUI.skin = HighLogic.Skin;
            SetUpSkins();
            GUI.skin = adjustableSkin;
            EnsureStyles();

            int oldDepth = GUI.depth;
            GUI.depth = 10;

            ComboBox.DrawGUI();
            if (_visible)
            {
                _treeRect = ClickThruBlocker.GUILayoutWindow(_treeWinId, _treeRect, DrawTreeWindow, "Mission Planner/Checklist");
                // do this here since if it's done within the window you only recieve events that are inside of the window
                this.resizeHandle.DoResize(ref this._treeRect);
                if (_detailNode != null)
                {
                    if (_simpleChecklist)
                    {
                        _detailRect.height = 200f;
                        _detailRect = ClickThruBlocker.GUILayoutWindow(
                                         _detailWinId, _detailRect, DrawDetailWindow,
                                         string.Format("Step Details — {0}", _detailNode.data.title),
                                         GUILayout.MinWidth(520), GUILayout.MinHeight(200)
                                     );
                    }
                    else
                    {
                        _detailRect = ClickThruBlocker.GUILayoutWindow(
                            _detailWinId, _detailRect, DrawDetailWindow,
                            string.Format("Step Details — {0}", _detailNode.data.title),
                            GUILayout.MinWidth(520), GUILayout.MinHeight(440)
                        );
                    }
                }
                if (_showMoveDialog && _moveNode != null)
                {
                    _moveRect = ClickThruBlocker.GUILayoutWindow(
                        _moveWinId, _moveRect, DrawMoveDialogWindow,
                        string.Format("Move “{0}” — choose new parent", _moveNode.data.title),
                        GUILayout.MinWidth(420), GUILayout.MinHeight(320)
                    );
                }
                if (_showLoadDialog)
                {
                    _loadRect = ClickThruBlocker.GUILayoutWindow(
                        _loadWinId, _loadRect, DrawLoadDialogWindow,
                        "Load Mission",
                        GUILayout.MinWidth(480), GUILayout.MinHeight(380)
                    );
                }
                if (_showOverwriteDialog)
                {
                    _overwriteRect = ClickThruBlocker.GUILayoutWindow(
                        _overwriteWinId, _overwriteRect, DrawOverwriteDialogWindow,
                        "Overwrite Confirmation",
                        GUILayout.MinWidth(420), GUILayout.MinHeight(180)
                    );
                }
                if (_showDeleteConfirm)
                {
                    _deleteRect = ClickThruBlocker.GUILayoutWindow(
                        _deleteWinId, _deleteRect, DrawDeleteDialogWindow,
                        "Delete Mission?",
                        GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                    );
                }
                if (_showPartDialog && _partTargetNode != null)
                {
                    _partRect = ClickThruBlocker.GUILayoutWindow(
                        _partWinId, _partRect, DrawPartPickerWindow,
                        "Select Part",
                         GUILayout.MinHeight(400)
                    );
                }
#if false
            if (_showResourceDialog)
            {
                _resourceRect = ClickThruBlocker.GUILayoutWindow(
                    _resourceWinId, _resourceRect, DrawResourcePickerWindow,
                    "Select Resource",
                     GUILayout.MinHeight(400)
                );
            }
#endif

                if (_showDeltaVDialog)
                {
                    _deltaVRect = ClickThruBlocker.GUILayoutWindow(
                        _resourceWinId, _deltaVRect, DrawDeltaVPickerWindow,
                        "Select Suggested DeltaV",
                         GUILayout.MinHeight(400)
                    );
                }

                if (_showTraitDialog)
                {
                    _traitRect = ClickThruBlocker.GUILayoutWindow(
                        _traitWinId, _traitRect, DrawTraitPickerWindow,
                        "Select Trait",
                         GUILayout.MinHeight(400)
                    );
                }

                if (_showModuleDialog)
                {
                    _moduleRect = ClickThruBlocker.GUILayoutWindow(
                        _moduleWinId, _moduleRect, DrawModulePickerWindow,
                        "Select Module",
                        GUILayout.MinHeight(400)
                    );
                }

                if (_showCategoryDialog)
                {
                    _SASRect = ClickThruBlocker.GUILayoutWindow(
                        _SASWinId, _SASRect, DrawCategoryPickerWindow,
                        "Select Category",
                         GUILayout.MinHeight(400)
                    );
                }

                if (_showBodyAsteroidVesselDialog)
                {
                    _bodyAsteroidVesselRect = ClickThruBlocker.GUILayoutWindow(
                        _moduleWinId, _bodyAsteroidVesselRect, DrawBodyAsteroidVesselPickerWindow,
                        "Select Vessel/Asteroid/Body",
                        GUILayout.MinHeight(400)
                    );
                }

                if (_showSaveAs)
                {
                    _saveAsRect = ClickThruBlocker.GUILayoutWindow(
                        _saveAsWinId, _saveAsRect, DrawSaveAsDialogWindow,
                        _creatingNewMission ? "New Mission" : "Save As…",
                        GUILayout.MinWidth(460), GUILayout.MinHeight((_creatingNewMission ? 270 : 170))
                    );
                }
                if (_showNewConfirm)
                {
                    _newConfirmRect = ClickThruBlocker.GUILayoutWindow(
                        _newConfirmWinId, _newConfirmRect, DrawNewConfirmWindow,
                        "Start New Mission?",
                        GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                    );
                }
                if (_showClearConfirm)
                {
                    _clearConfirmRect = ClickThruBlocker.GUILayoutWindow(
                        _clearConfirmWinId, _clearConfirmRect, DrawClearConfirmWindow,
                        "Clear All Steps?",
                        GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                    );
                }
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
            forceRecalcStyles = (lastUseKSPSkin != _useKspSkin || lastFontSize != HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().fontSize);
            lastUseKSPSkin = _useKspSkin;
            lastFontSize = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().fontSize;

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

            GUI.skin.label.fontSize = HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().fontSize;

            largeLabelFontSize = (int)((GUI.skin.label.fontSize + 2) * GameSettings.UI_SCALE);
            normalLabelFontSize = (int)(GUI.skin.label.fontSize * GameSettings.UI_SCALE);
            smallLabelFontSize = (int)((GUI.skin.label.fontSize - 1) * GameSettings.UI_SCALE);
            tinyLabelFontSize = (int)((GUI.skin.label.fontSize - 2) * GameSettings.UI_SCALE);

            //Log.Info($"normalLabelFontSize: {normalLabelFontSize}   smallLabelFontSize: {smallLabelFontSize}   tinyLabelFontSize: {tinyLabelFontSize}");
            //Log.Info($"GUI.skin.label.fontSize: {GUI.skin.label.fontSize}");

            if (_useKspSkin)
                adjustableSkin = DuplicateAndScaleSkin(HighLogic.Skin, GameSettings.UI_SCALE);
            else
                adjustableSkin = DuplicateAndScaleSkin(originalGuiSkin, GameSettings.UI_SCALE);

            Utils.ComboBox.UpdateStyles(normalLabelFontSize);
            _titleLabel = _unfilledTitleLabel = _selectedTitleLabel = _selectedUnfilledTitleLabel = _smallBtn =
                _hintLabel = _errorLabel = _tinyLabel = _smallLabel = _titleEdit =
                _badge = _badgeError = _cornerGlyph = null;
            forceRecalcStyles = false;

            {
                _errorLabel = new GUIStyle(GUI.skin.label);
                _errorLabel.fontSize = normalLabelFontSize;
                _errorLabel.normal.textColor = Color.red;
            }
            {
                _errorLargeLabel = new GUIStyle(GUI.skin.label);
                _errorLargeLabel.fontSize = largeLabelFontSize + 2;
                _errorLargeLabel.normal.textColor = Color.red;
            }
            {
                _titleLabel = new GUIStyle(GUI.skin.label);
                _titleLabel.fontSize = largeLabelFontSize;
            }
            {
                _unfilledTitleLabel = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = _titleLabel.onNormal,
                    hover = _titleLabel.onHover,
                    active = _titleLabel.onActive

                };
                _unfilledTitleLabel.normal.textColor = Color.red;
                _unfilledTitleLabel.fontSize = largeLabelFontSize;
            }
            {
                _selectedTitleLabel = new GUIStyle(_titleLabel)
                {
                    normal = _titleLabel.onNormal,
                    hover = _titleLabel.onHover,
                    active = _titleLabel.onActive,
                    fontStyle = FontStyle.Bold
                };
                _selectedTitleLabel.normal.textColor = Color.white;
                _selectedTitleLabel.fontSize = largeLabelFontSize;
            }
            {
                _selectedUnfilledTitleLabel = new GUIStyle(_titleLabel)
                {
                    normal = _titleLabel.onNormal,
                    hover = _titleLabel.onHover,
                    active = _titleLabel.onActive,
                    fontStyle = FontStyle.Bold
                };
                _selectedUnfilledTitleLabel.normal.textColor = Color.red;
                _selectedUnfilledTitleLabel.fontSize = largeLabelFontSize;

            }
            //_smallBtn = new GUIStyle(GUI.skin.button) { fixedWidth = 28f, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(2, 2, 2, 2) };
            _smallBtn = new GUIStyle(_titleLabel)
            {
                fixedWidth = 28f
            };
            _hintLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
            _tinyLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = tinyLabelFontSize
            };
            _smallLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = smallLabelFontSize
            };
            {
                _titleEdit = new GUIStyle(GUI.skin.textField)
                {
                    fontStyle = FontStyle.Bold
                };
                _titleEdit.fontSize = largeLabelFontSize;
            }

            //if (_badge == null)
            {
                _badge = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = {
                        textColor = new Color(0.85f, 0.9f, 1f, 1f)
                    }
                };
                _badge.fontSize = normalLabelFontSize;
            }
            //if (_badgeError == null)
            {
                _badgeError = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = {
                        textColor = new Color(1f, 0.35f, 0.35f, 1f)
                    }
                };
                _badgeError.fontSize = normalLabelFontSize;
            }

            //if (_cornerGlyph == null)
            {
                _cornerGlyph = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerRight,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.Max(GUI.skin.label.fontSize + 6, 14)
                };
                _cornerGlyph.fontSize = normalLabelFontSize;
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
            _visibleBeforePause = _visible;
            _visible = false;
            SyncToolbarState();
        }
        private void OnGameUnpaused()
        {
            _visible = _visibleBeforePause;
            SyncToolbarState();
        }

        bool showSummary = false;
        bool showDetail = false;

        private void DrawTreeWindow(int id)
        {
            GUILayout.Space(4);

            // Top row: Save name, Mission, controls, skin
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Mission:", ScaledGUILayoutWidth(60));
                _missionName = GUILayout.TextField(_missionName ?? "", _titleEdit, ScaledGUIFontLayoutWidth(450), GUILayout.ExpandWidth(false));
                showSummary = GUILayout.Toggle(showSummary, "");

                if (showSummary)
                    GUILayout.Label("Summary Preview:", ScaledGUILayoutWidth(120));
                else
                    GUILayout.Label("Show Summary");

                GUILayout.FlexibleSpace();
                GUILayout.Label(" ");
                GUILayout.FlexibleSpace();
                _useKspSkin = GUILayout.Toggle(_useKspSkin, "Use KSP Skin", ScaledGUILayoutWidth(120));
            }

            GUILayout.Space(4);

            if (showSummary)
            {
                _summaryScroll = GUILayout.BeginScrollView(_summaryScroll, HighLogic.Skin.textArea, GUILayout.Height(110));
                _missionSummary = GUILayout.TextArea(string.IsNullOrEmpty(_missionSummary) ? "" : _missionSummary, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                if (GUILayout.Button("Add Objective/goal", ScaledGUILayoutWidth(160)))
                {
                    var newRoot = new StepNode();
                    _roots.Add(newRoot);
                    _selectedNode = newRoot;
                    OpenDetail(newRoot);
                }
                GUILayout.Space(40);
                if (GUILayout.Button("Expand All", ScaledGUILayoutWidth(110))) SetAllExpanded(true);
                if (GUILayout.Button("Collapse All", ScaledGUILayoutWidth(110))) SetAllExpanded(false);

                GUILayout.FlexibleSpace();
                if (!_simpleChecklist)
                {
                    showDetail = GUILayout.Toggle(showDetail, "");
                    GUILayout.Label("Show Detail");
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUILayout.Label("Simple Checklist");
                    GUILayout.FlexibleSpace();
                }


                GUILayout.FlexibleSpace();
                GUILayout.Label("View: ");

                View localType = currentView;
                var names = Enum.GetNames(typeof(View));
                currentView = (View)Utils.ComboBox.Box(VIEW_COMBO, (int)currentView, names, this, 100);
                if (currentView != localType)
                {
                    float width = 0;
                    switch (currentView)
                    {
                        case View.Compact: width = COMPACT_WIDTH; break;
                        case View.Tiny: width = TINY_WIDTH; break;
                        case View.Full: width = FULL_WIDTH; break;
                    }
                    _treeRect.width = width;
                }
            }
            using (new GUILayout.HorizontalScope())
            {
                if (_useKspSkin)
                    GUILayout.Space(15);
                if (_simpleChecklist)
                {
                    GUILayout.Label("Done", _titleLabel);
                }
                else
                {
                    switch (currentView)
                    {
                        case View.Compact:
                            GUILayout.Label("Done|Lock|All | Double-click a title to edit in a separate window.", _titleLabel);
                            break;

                        case View.Tiny:
                            GUILayout.Label("Done|Lock|All", _titleLabel);
                            break;

                        case View.Full:
                            GUILayout.Label("Done|Lock|All | Double-click a title to edit in a separate window. Use ▲ ▼ ⤴ ⤵ Move… Dup + ✖ to manage hierarchy.", _titleLabel);
                            break;
                    }
                }
            }
            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _roots.Count; i++)
            {
                var r = _roots[i];
                DrawNodeRow(r, 0);
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
                    _showNewConfirm = true;
                    var mp = Input.mousePosition;
                    _newConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _newConfirmRect.width - 40);
                    _newConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _newConfirmRect.height - 40);
                }

                if (GUILayout.Button("Clear All", ScaledGUILayoutWidth(90)))
                {
                    _clearAddSample = false;
                    _showClearConfirm = true;
                    var mp = Input.mousePosition;
                    _clearConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _clearConfirmRect.width - 40);
                    _clearConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _clearConfirmRect.height - 40);
                }

                if (GUILayout.Button("Save", ScaledGUILayoutWidth(90)))
                {
                    TrySaveToDisk_Internal(true);
                }

                if (GUILayout.Button("Save As…", ScaledGUILayoutWidth(100)))
                {
                    _creatingNewMission = false;
                    OpenSaveAsDialog();
                }
                GUILayout.FlexibleSpace();
                if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().deltaVEditorActive)
                {
                    if (GUILayout.Button("DeltaV Editor"))
                        openDeltaVEditor = true;

                    GUILayout.FlexibleSpace();
                }
                if (GUILayout.Button("Load/Import…", ScaledGUILayoutWidth(120)))
                    OpenLoadDialog();
                GUILayout.FlexibleSpace();

                if (Time.realtimeSinceStartup <= _lastSaveShownUntil && !String.IsNullOrEmpty(_lastSaveInfo))
                {
                    var style = _lastSaveWasSuccess ? _badge : _badgeError;
                    GUILayout.Label(_lastSaveInfo, style);
                }

                GUILayout.FlexibleSpace();
                if (currentView != View.Tiny)
                    GUILayout.Label(string.Format("Count: {0}", CountAll()), _badge);
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(70)))
                {
                    _visible = false;
                    SyncToolbarState();
                    toolbarControl.SetFalse(true);
                }
                GUILayout.Space(20);
            }

            this.resizeHandle.Draw(ref this._treeRect);

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
        private ResizeHandle resizeHandle = null;

        private void DrawNewConfirmWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);
            GUILayout.Label("Create a new mission. Save current mission first?", _hintLabel);

            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", ScaledGUILayoutWidth(100)))
                {
                    TrySaveToDisk_Internal(true);
                    _showNewConfirm = false;

                    _creatingNewMission = true;
                    _newMissionAddSample = true;
                    _saveAsName = "New Mission";
                    _saveAsSummaryText = "";
                    _simpleChecklist = false;
                    OpenSaveAsDialog();
                }
                if (GUILayout.Button("Discard", ScaledGUILayoutWidth(100)))
                {
                    _showNewConfirm = false;
                    _creatingNewMission = true;
                    _newMissionAddSample = true;
                    _saveAsName = "New Mission";
                    _saveAsSummaryText = "";
                    _simpleChecklist = false;
                    OpenSaveAsDialog();
                }
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100)))
                {
                    _showNewConfirm = false;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawClearConfirmWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);
            GUILayout.Label("This will remove ALL steps from the current mission.\n(Your mission name is kept.)", _hintLabel);

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Add sample root step", ScaledGUILayoutWidth(160));
                _clearAddSample = GUILayout.Toggle(_clearAddSample, GUIContent.none, ScaledGUILayoutWidth(22));
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", ScaledGUILayoutWidth(100)))
                {
                    _roots.Clear();
                    _selectedNode = null;
                    _detailNode = null;
                    if (_clearAddSample)
                    {
                        var root = new StepNode
                        {
                            data = new Step
                            {
                                title = "New Step"
                            },
                            Expanded = true
                        };
                        _roots.Add(root);
                        _selectedNode = root;
                        OpenDetail(root);
                    }
                    _showClearConfirm = false;
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100)))
                {
                    _showClearConfirm = false;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }



        public string OneLineSummary(StepNode node, ref GUIStyle style)
        {
            string criteria = "";
            switch (node.data.stepType)
            {
                case CriterionType.Batteries:
                    criteria = node.data.batteryCapacity.ToString() + " EC";
                    break;
                case CriterionType.ChargeRateTotal:
                    criteria = node.data.chargeRateTotal.ToString() + " EC/sec";
                    break;

#if false
                case CriterionType.ChecklistItem:
                    criteria = node.data.completed ? ": Completed" : ": Incomplete";

                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        if (_selectedNode == node)
                            style = (node.data.completed) ? _selectedTitleLabel : _selectedUnfilledTitleLabel;
                        else
                            style = (node.data.completed) ? _titleLabel : _unfilledTitleLabel;

                    }
                    break;
#endif

                case CriterionType.Communication:
                    criteria = node.data.antennaPower.ToString();  // may need a suffix added 
                    break;


                case CriterionType.ControlSource:
                    criteria = $"{node.data.controlSourceQty} needed";
                    break;

                case CriterionType.CrewCount:
                    criteria = node.data.crewCount.ToString() + " kerbals";
                    break;

                case CriterionType.CrewMemberTrait:
                    criteria = node.data.traitName;
                    break;

                case CriterionType.Destination_asteroid:
                    criteria = node.data.destAsteroid;
                    break;

                case CriterionType.Destination_body:
                    criteria = node.data.destBody;
                    break;

                case CriterionType.Destination_vessel:
                    criteria = node.data.destVessel;
                    break;

                case CriterionType.DockingPort:
                    criteria = node.data.dockingPortQty.ToString() + " docking ports";
                    break;

                case CriterionType.Drills:
                    criteria = node.data.drillQty.ToString() + " drills";
                    break;

                case CriterionType.Engines:
                    for (int i = 0; i < Initialization.engineTypesAr.Length; i++)
                    {
                        if (Initialization.engineTypesAr[i] == node.data.engineType)
                        {
                            criteria = Initialization.engineTypesDisplayAr[i];
                            break;
                        }
                    }
                    break;


                case CriterionType.Flags:
                    {
                        int flagCount = FlightChecks.flagCount;
                        criteria = $"Need to plant {flagCount} flags";
                        break;
                    }

                case CriterionType.FuelCells:
                    {
                        float chargeRate = node.data.fuelCellChargeRate; //  Utils.FlightChecks.chargeRate;
                        criteria = $"Charge rate: {chargeRate}";
                        break;
                    }

                case CriterionType.Generators:
                    criteria = node.data.generatorChargeRate.ToString() + " EC/sec";
                    break;

                case CriterionType.Lights:
                    criteria = node.data.spotlights.ToString() + " spotlights";
                    break;

                case CriterionType.Maneuver:
                    criteria = StringFormatter.BeautifyName(node.data.maneuver.ToString());

                    switch (node.data.maneuver)
                    {
                        case Maneuver.Launch:
                        case Maneuver.Orbit:
                            criteria += $": Target Orbit: Ap: {node.data.ap} km   Pe: {node.data.pe} km";
                            break;

                        case Maneuver.ImpactAsteroid:
                        case Maneuver.InterceptAsteroid:
                            criteria += ": " + node.data.destAsteroid;
                            break;

                        case Maneuver.FineTuneClosestApproachToVessel:
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

                case CriterionType.Number:
                    criteria = $"{node.data.number}";
                    break;

                case CriterionType.Part:
                    if (node.data.partName != null)
                        criteria = node.data.partTitle;
                    else
                        criteria = "Unnamed Part";
                    break;

                case CriterionType.Parachutes:
                    criteria = node.data.parachutes.ToString() + " parachutes";
                    break;

                case CriterionType.Radiators:
                    {
                        float coolingRate = node.data.radiatorCoolingRate;
                        criteria = $"Cooling rate: {coolingRate} kW";
                        break;
                    }
                case CriterionType.Range:
                    criteria = $"{node.data.minFloatRange} - {node.data.maxFloatRange}";
                    break;

                case CriterionType.RCS:
                    for (int i = 0; i < Initialization.rcsTypesAr.Length; i++)
                    {
                        if (Initialization.rcsTypesAr[i] == node.data.rcsType)
                        {
                            criteria = Initialization.rcsTypesDisplayAr[i];
                            break;
                        }
                    }
                    break;

                case CriterionType.ReactionWheels:
                    criteria = node.data.reactionWheels.ToString() + " reaction wheels";
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

                case CriterionType.SolarPanels:
                    {
                        float chargeRate = node.data.solarChargeRate; //  Utils.FlightChecks.chargeRate;
                        criteria = $"Charge rate: {chargeRate} EC/sec";
                        break;
                    }
                case CriterionType.TrackedVessel:
                    criteria = StringFormatter.BeautifyName(node.data.stepType.ToString()) + $": {node.data.trackedVessel})";
                    break;

                case CriterionType.VABOrganizerCategory:
                    criteria = StringFormatter.BeautifyName(node.data.vabCategory);
                    break;


                default:
                    break;
            }
            return ": " + criteria;
        }
        private void DrawNodeRow(StepNode node, int depth)
        {
            Rect titleRect;
            bool up, down, promote, demote, moveTo, dup, add, del;
            float indent = 0;
            string criteria = "";
            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (!_useKspSkin)
                        GUILayout.Space(10);
                    node.data.completed = GUILayout.Toggle(node.data.completed, new GUIContent("", "Mark this line as completed"));
                    if (_simpleChecklist)
                    {

                    }
                    else
                    {
                        if (!_useKspSkin)
                            GUILayout.Space(15);

                        node.data.locked = GUILayout.Toggle(node.data.locked, new GUIContent("", "Lock this line"));
                        if (!_useKspSkin)
                            GUILayout.Space(10);

                        bool b = GUILayout.Toggle(node.requireAll, new GUIContent("", "Require all children to be fulfilled for this to be fulfilled"));
                        if (!node.data.locked)
                            node.requireAll = b;
                    }
                    indent = 12f + _indentPerLevel * depth;
                    GUILayout.Space(indent);

                    if (node.Children.Count == 0)
                    {
                        GUILayout.Label(" ", ScaledGUILayoutWidth(_foldColWidth));
                    }
                    else
                    {
                        string sign = node.Expanded ? "-" : "+";
                        if (GUILayout.Button("<B>" + sign + "</B>", _titleLabel, ScaledGUILayoutWidth(_foldColWidth))) node.Expanded = !node.Expanded;
                    }

                    const float controlsBase = 100f;
                    float controlsWidth = controlsBase + _controlsPad;
                    float available = Mathf.Max(120f, _treeRect.width - indent - _foldColWidth - controlsWidth - 12f);
                    float titleWidth = Mathf.Clamp(available * _titleWidthPct, MAX_TITLE_WIDTH, available);

                    var style = (_selectedNode == node) ? _selectedTitleLabel : _titleLabel;

                    bool fulfilled = FlightChecks.CheckChildStatus(node);
                    if (!fulfilled)
                    {
                        style = (_selectedNode == node) ? _selectedUnfilledTitleLabel : _unfilledTitleLabel;
                    }

                    if (showDetail)
                    {
                        criteria = OneLineSummary(node, ref style);
#if false
                        switch (node.data.stepType)
                        {
                            case CriterionType.Resource:
                                for (int i = 0; i < node.data.resourceList.Count; i++)
                                {
                                    ResInfo resinfo = node.data.resourceList[i];
                                    if (criteria.Length > 0)
                                        criteria += ", ";
                                    criteria += resinfo.resourceName;

                                }
                                break;

                            case CriterionType.TrackedVessel:
                                criteria = StringFormatter.BeautifyName(node.data.stepType.ToString()) + $": {node.data.trackedVessel})";
                                break;

                            case CriterionType.SolarPanels:
                                {
                                    float chargeRate = node.data.solarChargeRate; //  Utils.FlightChecks.chargeRate;
                                    criteria = $"Charge rate: {chargeRate}";
                                    break;
                                }

                            case CriterionType.FuelCells:
                                {
                                    float chargeRate = node.data.fuelCellChargeRate; //  Utils.FlightChecks.chargeRate;
                                    criteria = $"Charge rate: {chargeRate}";
                                    break;
                                }

                            case CriterionType.Radiators:
                                {
                                    float coolingRate = node.data.radiatorCoolingRate;
                                    criteria = $"Cooling rate: {coolingRate}";
                                    break;
                                }

                            case CriterionType.Flags:
                                {
                                    int flagCount = FlightChecks.flagCount;
                                    criteria = $"Flag count: {flagCount}";
                                    break;
                                }

                            case CriterionType.ChecklistItem:
                                criteria = node.data.completed ? ": Completed" : ": Incomplete";

                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    if (_selectedNode == node)
                                        style = (node.data.completed) ? _selectedTitleLabel : _selectedUnfilledTitleLabel;
                                    else
                                        style = (node.data.completed) ? _titleLabel : _unfilledTitleLabel;
                                }
                                break;

                            case CriterionType.CrewMemberTrait:
                                criteria = ": " + node.data.traitName;
                                break;

                            case CriterionType.ControlSource:
                                criteria = $": {node.data.controlSourceQty} needed";
                                break;

                            case CriterionType.Engines:
                                criteria = $": {node.data.engineType}";
                                break;

                            case CriterionType.Number:
                                criteria = $": {node.data.number}";
                                break;
                            case CriterionType.Range:
                                criteria = $": {node.data.minFloatRange} - {node.data.maxFloatRange}";
                                break;

                            default:
                                break;
                        }

#endif
                    }

                    GUILayout.Label(node.data.title, style, ScaledGUILayoutWidth(titleWidth));
                    titleRect = GUILayoutUtility.GetLastRect();
                    up = down = promote = demote = moveTo = dup = add = del = false;

                    GUILayout.FlexibleSpace();
                    if (currentView == View.Full)
                    {
                        up = GUILayout.Button("▲", _smallBtn);
                        down = GUILayout.Button("▼", _smallBtn);
                        promote = GUILayout.Button("⤴", _smallBtn);
                        demote = GUILayout.Button("⤵", _smallBtn);
                        moveTo = GUILayout.Button("Move…", ScaledGUILayoutWidth(60f /* 54f */));
                        dup = GUILayout.Button("Dup", ScaledGUILayoutWidth(40f));
                        GUILayout.Space(20);
                        add = GUILayout.Button("<B>+</B>", _smallBtn /* "⊕" */, ScaledGUILayoutWidth(28));
                        del = GUILayout.Button("✖", _smallBtn);
                        GUILayout.Space(5);
                    }
                }

                if (currentView != View.Tiny)
                    HandleTitleClicks(node, titleRect);

                if (currentView == View.Full)
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
                        _selectedNode = child;
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
                            GUILayout.Label("(" + StringFormatter.BeautifyName(node.data.stepType.ToString()) + " " + criteria + ")", _smallLabel);
                        else
                            GUILayout.Label("(" + StringFormatter.BeautifyName(node.data.stepType.ToString()) + ")", _smallLabel);
                    }
                }
                if (node.Expanded && node.Children.Count > 0)
                {
                    GUILayout.Space(2);
                    for (int i = 0; i < node.Children.Count; i++)
                    {
                        StepNode c = node.Children[i];
                        DrawNodeRow(c, depth + 1);
                        GUILayout.Space(1);
                    }
                }
            }

            // Optionally display the tooltip near the mouse cursor
            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().showTooltips)
            {
                if (!string.IsNullOrEmpty(GUI.tooltip))
                {
                    Vector2 mouse = Event.current.mousePosition;
                    GUIStyle style = GUI.skin.box;
                    Vector2 size = style.CalcSize(new GUIContent(GUI.tooltip));

                    // Small offset so it doesn’t overlap the cursor
                    Rect tipRect = new Rect(mouse.x + 16f, mouse.y + 16f, size.x + 8f, size.y + 4f);
                    GUI.Box(tipRect, GUI.tooltip);
                }
            }
        }


        private void HandleTitleClicks(StepNode node, Rect titleRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && titleRect.Contains(e.mousePosition)) e.Use();
            if (e.type == EventType.MouseUp && e.button == 0 && titleRect.Contains(e.mousePosition))
            {
                if (_lastClickedId == node.Id && (Time.realtimeSinceStartup - _lastClickTime) <= DoubleClickSec)
                {
                    OpenDetail(node);
                    _lastClickedId = -1;
                }
                else
                {
                    _selectedNode = node;
                    _lastClickedId = node.Id;
                    _lastClickTime = Time.realtimeSinceStartup;
                }
                e.Use();
            }
        }

        private void MoveUp(StepNode n)
        {
            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx > 0) { list.RemoveAt(idx); list.Insert(idx - 1, n); }
        }

        private void MoveDown(StepNode n)
        {
            var list = (n.Parent == null) ? _roots : n.Parent.Children;
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

            var newList = (parent.Parent == null) ? _roots : parent.Parent.Children;
            int parentIdx = newList.IndexOf(parent);
            int insertAt = (parentIdx >= 0) ? parentIdx + 1 : newList.Count;
            newList.Insert(insertAt, n);
            n.Parent = parent.Parent;
        }

        private void Demote(StepNode n)
        {
            var list = (n.Parent == null) ? _roots : n.Parent.Children;
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
                _roots.Remove(n);
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
            if (_selectedNode == n) _selectedNode = null;
            if (_detailNode == n) _detailNode = null;
        }

        private void Duplicate(StepNode n)
        {
            var cloned = new StepNode(n);
            cloned.data.title = (n.data.title ?? "New Step") + " (copy)";

            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            list.Insert(Mathf.Clamp(idx + 1, 0, list.Count), cloned);
        }

        private void OpenMoveDialog(StepNode node)
        {
            if (_detailNode != null &&
                _detailNode != node &&
                HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                TrySaveToDisk_Internal(true);

            _moveNode = node;
            newWindow = true;
            _moveTargetParent = null;
            _showMoveDialog = true;

            var mp = Input.mousePosition;
            _moveRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _moveRect.width - 40);
            _moveRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _moveRect.height - 40);
        }

        private void DrawMoveDialogWindow(int id)
        {
            BringWindowForward(id);
            GUILayout.Space(6);
            GUILayout.Label("Choose a new parent (or Root). You cannot move an item under itself or its descendants.", _hintLabel);

            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                bool rootChosen = (_moveTargetParent == null);
                if (GUILayout.Toggle(rootChosen, "⟂ Root", HighLogic.Skin.toggle, ScaledGUILayoutWidth(120)))
                    _moveTargetParent = null;
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(6);
            GUILayout.Label("Or select an existing parent:", _tinyLabel);

            _moveScroll = GUILayout.BeginScrollView(_moveScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var r in _roots)
                DrawMoveTargetRecursive(r, 0, _moveNode);
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OK", ScaledGUILayoutWidth(100)))
                {
                    MoveToParent(_moveNode, _moveTargetParent);
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
                GUILayout.Space(12 + _indentPerLevel * depth);
                bool chosen = (_moveTargetParent == node);
                string label = string.Format("📁 {0}", node.data.title);
                if (GUILayout.Toggle(chosen, label, HighLogic.Skin.toggle))
                    _moveTargetParent = node;
                GUILayout.FlexibleSpace();
            }

            foreach (var c in node.Children) DrawMoveTargetRecursive(c, depth + 1, moving);
        }

        private void CloseMoveDialog()
        {
            _showMoveDialog = false;
            _moveNode = null;
            _moveTargetParent = null;
        }

        private void MoveToParent(StepNode n, StepNode newParent)
        {
            if (n == null) return;
            if (newParent == n || IsDescendant(n, newParent)) return;

            var oldList = (n.Parent == null) ? _roots : n.Parent.Children;
            oldList.Remove(n);

            if (newParent == null)
            {
                _roots.Add(n);
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
            if (_detailNode != null &&
                _detailNode != node &&
                HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                TrySaveToDisk_Internal(true);

            _detailNode = node;
            newWindow = true;

            var mp = Input.mousePosition;
            _detailRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _detailRect.width - 40);
            _detailRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _detailRect.height - 40);
        }



        private void OpenSaveAsDialog()
        {
            if (_creatingNewMission)
            {
                _saveAsName = IsNullOrWhiteSpace(_saveAsName) ? "New Mission" : _saveAsName.Trim();
            }
            else
            {
                _saveAsName = IsNullOrWhiteSpace(_missionName) ? "Untitled Mission" : _missionName.Trim();
            }

            var mp = Input.mousePosition;
            _saveAsRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _saveAsRect.width - 40);
            _saveAsRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _saveAsRect.height - 40);

            _showSaveAs = true;
        }

        private void DrawSaveAsDialogWindow(int id)
        {
            BringWindowForward(id, true);
            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(_creatingNewMission ? "New mission name:" : "Save as name:", ScaledGUILayoutWidth(140));
                _saveAsName = GUILayout.TextField(_saveAsName ?? "", GUILayout.MinWidth(180), GUILayout.ExpandWidth(true));
            }

            if (_creatingNewMission)
            {
                GUILayout.Space(6);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Simple Checklist", ScaledGUILayoutWidth(160));
                    _simpleChecklist = GUILayout.Toggle(_simpleChecklist, GUIContent.none, ScaledGUILayoutWidth(22));
                    GUILayout.FlexibleSpace();
                }
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Add sample root step", ScaledGUILayoutWidth(160));
                    _newMissionAddSample = GUILayout.Toggle(_newMissionAddSample, GUIContent.none, ScaledGUILayoutWidth(22));
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(6);
                GUILayout.Label("Mission summary (optional):");
                _saveAsSummaryText = GUILayout.TextArea(_saveAsSummaryText ?? "", HighLogic.Skin.textArea, GUILayout.MinHeight(80));
            }

            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("OK", ScaledGUILayoutWidth(100)))
                {
                    string proposed = IsNullOrWhiteSpace(_saveAsName) ? "Unnamed" : _saveAsName.Trim();

                    if (_creatingNewMission)
                    {
                        _roots.Clear();
                        _selectedNode = null;
                        _detailNode = null;
                        _missionName = proposed;
                        _missionSummary = _saveAsSummaryText ?? "";

                        if (_newMissionAddSample)
                        {
                            var root = new StepNode
                            {
                                data = new Step
                                {
                                    title = "New Step"
                                },
                                Expanded = true
                            };
                            _roots.Add(root);
                            _selectedNode = root;
                            OpenDetail(root);
                        }

                        _creatingNewMission = false;
                        _showSaveAs = false;
                    }
                    else
                    {
                        string save = GetCurrentSaveName();
                        if (!Directory.Exists(GetMissionDirectoryAbsolute()))
                        {
                            Directory.CreateDirectory(GetMissionDirectoryAbsolute());
                        }
                        string path = GetSaveFileAbsolute(save, proposed);

                        _pendingSaveMission = proposed;
                        _pendingSavePath = path;

                        if (File.Exists(path))
                        {
                            _showSaveAs = false;
                            _showOverwriteDialog = true;

                            var mp = Input.mousePosition;
                            _overwriteRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _overwriteRect.width - 40);
                            _overwriteRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _overwriteRect.height - 40);
                        }
                        else
                        {
                            _missionName = proposed;
                            _showSaveAs = false;
                            if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                                TrySaveToDisk_Internal(true);
                        }
                    }
                }
                if (GUILayout.Button("Cancel", ScaledGUILayoutWidth(100)))
                {
                    _creatingNewMission = false;
                    _showSaveAs = false;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void SetAllExpanded(bool ex) { foreach (var r in _roots) SetExpandedRecursive(r, ex); }
        private void SetExpandedRecursive(StepNode n, bool ex) { n.Expanded = ex; foreach (var c in n.Children) SetExpandedRecursive(c, ex); }

        private bool IsDescendant(StepNode ancestor, StepNode maybeDescendant)
        {
            if (ancestor == null || maybeDescendant == null) return false;
            var p = maybeDescendant.Parent;
            while (p != null) { if (p == ancestor) return true; p = p.Parent; }
            return false;
        }

        private int CountAll() { int count = 0; foreach (var r in _roots) count += CountRecursive(r); return count; }
        private int CountRecursive(StepNode n) { int c = 1; foreach (var ch in n.Children) c += CountRecursive(ch); return c; }

        private void ReparentAll() { foreach (var r in _roots) { r.Parent = null; ReparentRecursive(r); } }
        private void ReparentRecursive(StepNode n) { foreach (var c in n.Children) { c.Parent = n; ReparentRecursive(c); } }
        private class MissionFileInfo
        {
            public string FullPath;
            public string SaveName;
            public string MissionName;

            public bool stock = false;
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
                for (int i = 0; i < _roots.Count; i++)
                {
                    StepNode r = _roots[i];
                    CheckNode(r);
                    CheckChildren(r);
                }
            }
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
                if (string.IsNullOrEmpty(c.data.trackedVessel))
                {
                    c.data.trackedVessel = FlightGlobals.ActiveVessel.vesselName;
                    c.data.vesselGuid = FlightGlobals.ActiveVessel.id;
                    trackedVesselUpdated.Add(c);
                }
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

        private void SyncToolbarState()
        {
            if (_appButton == null) return;

            if (_visible)
            {
                if (_appButton.toggleButton.CurrentState == KSP.UI.UIRadioButton.State.False)
                    _appButton.SetTrue();
                SetButtonIcon(_iconOn);
            }
            else
            {
                if (_appButton.toggleButton.CurrentState == KSP.UI.UIRadioButton.State.True)
                    _appButton.SetFalse();
                SetButtonIcon(_iconOff);
            }
        }

        private void LoadIconsOrFallback()
        {
            _iconOn = GameDatabase.Instance?.GetTexture(IconOnPath, false);
            _iconOff = GameDatabase.Instance?.GetTexture(IconOffPath, false);
            if (_iconOn == null) _iconOn = MakeSolidTexture(38, 38, new Color(0.25f, 0.8f, 0.25f, 1f));
            if (_iconOff == null) _iconOff = MakeSolidTexture(38, 38, new Color(0.7f, 0.7f, 0.7f, 1f));
            if (_resizeBg == null) _resizeBg = MakeSolidTexture(8, 8, new Color(1f, 1f, 1f, 0.08f));
            if (_resizeBgHover == null) _resizeBgHover = MakeSolidTexture(8, 8, new Color(1f, 1f, 1f, 0.18f));
        }

        private void SetButtonIcon(Texture2D tex)
        {
            if (_appButton != null && tex != null)
                _appButton.SetTexture(tex);
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
                root.AddValue("UseKspSkin", _useKspSkin);
                root.AddValue("TitleWidthPct", _titleWidthPct);
                root.AddValue("ControlsPad", _controlsPad);
                root.AddValue("IndentPerLevel", _indentPerLevel);
                root.AddValue("FoldColWidth", _foldColWidth);

                root.AddNode(RectToNode("TreeRect", _treeRect));
                root.AddNode(RectToNode("DetailRect", _detailRect));
                root.AddNode(RectToNode("MoveRect", _moveRect));
                root.AddNode(RectToNode("LoadRect", _loadRect));
                root.AddNode(RectToNode("OverwriteRect", _overwriteRect));
                root.AddNode(RectToNode("DeleteRect", _deleteRect));
                root.AddNode(RectToNode("PartRect", _partRect));
                root.AddNode(RectToNode("SaveAsRect", _saveAsRect));
                root.AddNode(RectToNode("NewConfirmRect", _newConfirmRect));
                root.AddNode(RectToNode("ClearConfirmRect", _clearConfirmRect));
                root.AddNode(RectToNode("SummaryRect", _summaryRect));

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

                _useKspSkin = root.SafeLoad("UseKspSkin", _useKspSkin);
                _titleWidthPct = root.SafeLoad("TitleWidthPct", _titleWidthPct);
                _controlsPad = root.SafeLoad("ControlsPad", _controlsPad);
                _indentPerLevel = root.SafeLoad("IndentPerLevel", _indentPerLevel);
                _foldColWidth = root.SafeLoad("FoldColWidth", _foldColWidth);


                //bool.TryParse(root.GetValue("UseKspSkin"), out _useKspSkin);
                //float.TryParse(root.GetValue("TitleWidthPct"), out _titleWidthPct);
                //float.TryParse(root.GetValue("ControlsPad"), out _controlsPad);
                //float.TryParse(root.GetValue("IndentPerLevel"), out _indentPerLevel);
                //float.TryParse(root.GetValue("FoldColWidth"), out _foldColWidth);

                if (_titleWidthPct <= 0f) _titleWidthPct = 0.75f;
                _controlsPad = Mathf.Clamp(_controlsPad, 0f, 120f);
                _indentPerLevel = Mathf.Clamp(_indentPerLevel, 10f, 40f);
                _foldColWidth = Mathf.Clamp(_foldColWidth, 16f, 48f);

                _treeRect = NodeToRect(root.GetNode("TreeRect"), _treeRect);
                _detailRect = NodeToRect(root.GetNode("DetailRect"), _detailRect);
                _moveRect = NodeToRect(root.GetNode("MoveRect"), _moveRect);
                _loadRect = NodeToRect(root.GetNode("LoadRect"), _loadRect);
                _overwriteRect = NodeToRect(root.GetNode("OverwriteRect"), _overwriteRect);
                _deleteRect = NodeToRect(root.GetNode("DeleteRect"), _deleteRect);
                _partRect = NodeToRect(root.GetNode("PartRect"), _partRect);
                _saveAsRect = NodeToRect(root.GetNode("SaveAsRect"), _saveAsRect);
                _newConfirmRect = NodeToRect(root.GetNode("NewConfirmRect"), _newConfirmRect);
                _clearConfirmRect = NodeToRect(root.GetNode("ClearConfirmRect"), _clearConfirmRect);
                _summaryRect = NodeToRect(root.GetNode("SummaryRect"), _summaryRect);
            }
            catch (Exception ex)
            {
                Debug.LogError("[MissionPlanner] LoadUISettings failed: " + ex);
            }
        }
    }
}
