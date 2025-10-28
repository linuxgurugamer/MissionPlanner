// File: MissionPlanner.cs
// Mod: MissionPlanner
// KSP1 (Unity IMGUI)
//
// Features implemented:
// - Recursive hierarchical step tree with expand/collapse and move/promote/demote/duplicate/delete/add
// - Double-click title to open "Step Details" window
// - Toolbar button to show/hide main window 
// - Only titles shown in the main tree; detailed data edited in a separate window
// - StepType includes: toggle, intNotGreaterThan, intNotLessThan, floatNotGreaterThan, floatNotLessThan, intRange, floatRange, crewCount, part
// - Part picker dialog with search and "available only" filter; excludes Kerbals/Flags/PotatoRoid
// - Crew-count validator button (in flight) compares range vs active vessel crew count
// - Runtime tuning sliders for Title Width %, Controls Pad, Indent/Level, Fold Column Width (persisted) — now moved to the bottom
// - Skin toggle (use KSP skin) persisted
// - Persist all window rects across sessions
// - Save / Load missions as GameData/MissionPlanner/PluginData/<SaveName>__<MissionName>.cfg
// - Load dialog shows current-save missions by default; toggle to show all saves; delete mission
// - "Save" saves immediately (overwrites silently)
// - "Save As..." prompts for new mission name; if exists, shows Overwrite / Auto-increment dialog
// - "New" mission button asks to Save / Discard / Cancel, then name new mission (optionally add sample root step)
// - "Clear All" button clears all entries (with confirmation + toggle to add a sample root step)
// - Mission Summary: add/edit a mission summary text block (in New flow and via 'Summary…' window).
// - NEW: Summary preview textarea at the top of the main window with an “Expand…” button
// - Hide main window when game paused; restore after unpause
// - DragWindow from anywhere (called at end of each window draw)
// - SyncToolbarState uses KSP.UI.UIRadioButton.State checks as requested
//
// Build: Target .NET 3.5 (Unity/KSP1), reference Assembly-CSharp, UnityEngine, KSP assemblies.

using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MissionPlanner
{

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        // ---- Windows ----
        private Rect _treeRect = new Rect(220, 120, 840, 620);
        private Rect _detailRect = new Rect(820, 160, 560, 560);
        private Rect _moveRect = new Rect(760, 120, 460, 500);
        private Rect _loadRect = new Rect(720, 100, 560, 540);
        private Rect _overwriteRect = new Rect(760, 180, 520, 220);
        private Rect _deleteRect = new Rect(760, 160, 520, 180);
        private Rect _partRect = new Rect(680, 140, 600, 580);
        private Rect _resourceRect = new Rect(680, 140, 400, 580);
        private Rect _moduleRect = new Rect(680, 140, 400, 580);
        private Rect _SASRect = new Rect(680, 140, 300, 580);
        private Rect _bodyAsteroidVesselRect = new Rect(680, 140, 400, 580);
        private Rect _traitRect = new Rect(680, 140, 400, 580);
        private Rect _saveAsRect = new Rect(740, 200, 520, 310); // includes summary
        private Rect _newConfirmRect = new Rect(760, 200, 520, 180);
        private Rect _clearConfirmRect = new Rect(760, 220, 520, 170);
        private Rect _summaryRect = new Rect(760, 220, 520, 320);

        private int _treeWinId, _detailWinId, _moveWinId, _loadWinId, _overwriteWinId, _deleteWinId, _partWinId, _resourceWinId,
            _traitWinId, _moduleWinId, _SASWinId, _saveAsWinId, _newConfirmWinId, _clearConfirmWinId, _summaryWinId;
        private bool _visible = false;
        private bool _visibleBeforePause = true;

        // Skin toggle (persisted)
        private bool _useKspSkin = true;

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
        private readonly ApplicationLauncher.AppScenes _scenes =
            ApplicationLauncher.AppScenes.ALWAYS;

        // Missions I/O
        private const string SAVE_ROOT_NODE = "MISSION_PLANNER";
        private const string SAVE_LIST_NODE = "ROOTS";
        internal const string SAVE_MOD_FOLDER = "MissionPlanner/PluginData";
        private const string SAVE_FILE_EXT = ".cfg";

        // UI persistence
        private const string UI_FILE_NAME = "UI.cfg";
        private const string UI_ROOT_NODE = "MISSION_PLANNER_UI";

        // Mission meta
        private string _missionName = "Untitled Mission";
        private string _missionSummary = ""; // Mission summary

        // Data
        private readonly List<StepNode> _roots = new List<StepNode>();

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
        private bool _showResourceDialog = false;
        private StepNode _resourceTargetNode = null;
        private Vector2 _resourceScroll;
        private string _resourceFilter = "";

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

        // Summary window
        //private bool _showSummaryWindow = false;
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
        private GUIStyle _titleLabel, _selectedTitle, _smallBtn, _hintLabel, _errorLabel, _tinyLabel, _titleEdit, _badge, _badgeError, _cornerGlyph;
        bool lastUseKSPSkin = true;

        //private readonly KeyCode _toggleKey = KeyCode.F10;

        // Double-click
        private int _lastClickedId = -1;
        private float _lastClickTime = 0f;
        private const float DoubleClickSec = 0.30f;

        // Save indicator
        private string _lastSaveInfo = "";
        private float _lastSaveShownUntil = 0f;
        private const float SaveIndicatorSeconds = 3f;
        private bool _lastSaveWasSuccess = true;
        enum View { _short = 0, editable = 1 };
        View currentView = View.editable;
        private static bool IsNullOrWhiteSpace(string s) { return String.IsNullOrEmpty(s) || s.Trim().Length == 0; }

        public void Awake()
        {
            _treeWinId = GetHashCode();
            _detailWinId = _treeWinId ^ 0x5A17;
            _moveWinId = _treeWinId ^ 0x4B33;
            _loadWinId = _treeWinId ^ 0x23A1;
            _overwriteWinId = _treeWinId ^ 0x7321;
            _deleteWinId = _treeWinId ^ 0x19C7;
            _partWinId = _treeWinId ^ 0x71AF;
            _resourceWinId = _treeWinId ^ 0x72AF;
            _traitWinId = _treeWinId ^ 0x73AF;
            _moduleWinId = _treeWinId ^ 0x74AF;
            _SASWinId = _treeWinId ^ 0x75AF;
            _saveAsWinId = _treeWinId ^ 0x5EE5;
            _newConfirmWinId = _treeWinId ^ 0x6A11;
            _clearConfirmWinId = _treeWinId ^ 0x6A31;
            _summaryWinId = _treeWinId ^ 0x7B21;

            LoadIconsOrFallback();
            LoadUISettings();

            Initialization.Initialize();
            if (!TryAutoLoadMostRecentForCurrentSave())
                SeedSample();
            ReparentAll();

            GameEvents.onGamePause.Add(OnGamePaused);
            GameEvents.onGameUnpause.Add(OnGameUnpaused);

            this.resizeHandle = new ResizeHandle();

        }

        public void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);
            if (ApplicationLauncher.Instance != null) OnAppLauncherReady();
        }

        public void OnDestroy()
        {
            TrySaveToDisk_Internal(true);
            SaveUISettings();

            GameEvents.onGamePause.Remove(OnGamePaused);
            GameEvents.onGameUnpause.Remove(OnGameUnpaused);
            GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnAppLauncherDestroyed);
            if (_appButton != null && ApplicationLauncher.Instance != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(_appButton);
                _appButton = null;
            }
        }

        public void OnGUI()
        {
            if (PauseMenu.exists && PauseMenu.isOpen) return;
            if (_useKspSkin) GUI.skin = HighLogic.Skin;
            EnsureStyles();

            if (_visible)
            {
                _treeRect = GUILayout.Window(
                    _treeWinId, _treeRect, DrawTreeWindow,
                    "MissionPlanner — Steps (F10 to toggle)" //,
                                                             //GUILayout.MinWidth(720), GUILayout.MinHeight(400)
                );
                // do this here since if it's done within the window you only recieve events that are inside of the window
                this.resizeHandle.DoResize(ref this._treeRect);
            }
            if (_detailNode != null)
            {
                _detailRect = GUILayout.Window(
                    _detailWinId, _detailRect, DrawDetailWindow,
                    string.Format("Step Details — {0}", _detailNode.data.title),
                    GUILayout.MinWidth(520), GUILayout.MinHeight(440)
                );
            }
            if (_showMoveDialog && _moveNode != null)
            {
                _moveRect = GUILayout.Window(
                    _moveWinId, _moveRect, DrawMoveDialogWindow,
                    string.Format("Move “{0}” — choose new parent", _moveNode.data.title),
                    GUILayout.MinWidth(420), GUILayout.MinHeight(320)
                );
            }
            if (_showLoadDialog)
            {
                _loadRect = GUILayout.Window(
                    _loadWinId, _loadRect, DrawLoadDialogWindow,
                    "Load Mission",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(380)
                );
            }
            if (_showOverwriteDialog)
            {
                _overwriteRect = GUILayout.Window(
                    _overwriteWinId, _overwriteRect, DrawOverwriteDialogWindow,
                    "Overwrite Confirmation",
                    GUILayout.MinWidth(420), GUILayout.MinHeight(180)
                );
            }
            if (_showDeleteConfirm)
            {
                _deleteRect = GUILayout.Window(
                    _deleteWinId, _deleteRect, DrawDeleteDialogWindow,
                    "Delete Mission?",
                    GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                );
            }
            if (_showPartDialog && _partTargetNode != null)
            {
                _partRect = GUILayout.Window(
                    _partWinId, _partRect, DrawPartPickerWindow,
                    "Select Part",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(400)
                );
            }
            if (_showResourceDialog)
            {
                _resourceRect = GUILayout.Window(
                    _resourceWinId, _resourceRect, DrawResourcePickerWindow,
                    "Select Resource",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(400)
                );
            }

            if (_showTraitDialog)
            {
                _traitRect = GUILayout.Window(
                    _traitWinId, _traitRect, DrawTraitPickerWindow,
                    "Select Trait",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(400)
                );
            }

            if (_showModuleDialog)
            {
                _moduleRect = GUILayout.Window(
                    _moduleWinId, _moduleRect, DrawModulePickerWindow,
                    "Select Module",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(400)
                );
            }

            if (_showSASDialog)
            {
                _SASRect = GUILayout.Window(
                    _SASWinId, _SASRect, DrawSASPickerWindow,
                    "Select SAS",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(400)
                );
            }

            if (_showBodyAsteroidVesselDialog)
            {
                _bodyAsteroidVesselRect = GUILayout.Window(
                    _moduleWinId, _bodyAsteroidVesselRect, DrawBodyAsteroidVesselPickerWindow,
                    "Select Module",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(400)
                );
            }

            if (_showSaveAs)
            {
                _saveAsRect = GUILayout.Window(
                    _saveAsWinId, _saveAsRect, DrawSaveAsDialogWindow,
                    _creatingNewMission ? "New Mission" : "Save As…",
                    GUILayout.MinWidth(460), GUILayout.MinHeight((_creatingNewMission ? 270 : 170))
                );
            }
            if (_showNewConfirm)
            {
                _newConfirmRect = GUILayout.Window(
                    _newConfirmWinId, _newConfirmRect, DrawNewConfirmWindow,
                    "Start New Mission?",
                    GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                );
            }
            if (_showClearConfirm)
            {
                _clearConfirmRect = GUILayout.Window(
                    _clearConfirmWinId, _clearConfirmRect, DrawClearConfirmWindow,
                    "Clear All Steps?",
                    GUILayout.MinWidth(420), GUILayout.MinHeight(160)
                );
            }
#if false
            if (_showSummaryWindow)
            {
                _summaryRect = GUILayout.Window(
                    _summaryWinId, _summaryRect, DrawSummaryWindow,
                    "Mission Summary",
                    GUILayout.MinWidth(460), GUILayout.MinHeight(260)
                );
            }
#endif
        }

        private void EnsureStyles()
        {
            bool force = (lastUseKSPSkin != _useKspSkin);
            lastUseKSPSkin = _useKspSkin;
            if (force)
            {
                _titleLabel = null;
                _selectedTitle = null;
                _smallBtn = null;
                _hintLabel = null;
                _tinyLabel = null;
                _titleEdit = null;
                _badge = null;
                _badgeError = null;
                _errorLabel = null;
            }
            if (_errorLabel == null)
            {
                _errorLabel = new GUIStyle(GUI.skin.label);
                _errorLabel.normal.textColor = Color.red;
            }

            if (_titleLabel == null)
                _titleLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft /*, padding = new RectOffset(6, 6, 3, 3) */ };
            if (_selectedTitle == null)
            {
                _selectedTitle = new GUIStyle(_titleLabel);
                _selectedTitle.normal = _titleLabel.onNormal;
                _selectedTitle.normal.textColor = Color.white;
                _selectedTitle.hover = _titleLabel.onHover;
                _selectedTitle.active = _titleLabel.onActive;
                _selectedTitle.fontStyle = FontStyle.Bold;
            }
            if (_smallBtn == null)
                //_smallBtn = new GUIStyle(GUI.skin.button) { fixedWidth = 28f, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(2, 2, 2, 2) };
                _smallBtn = new GUIStyle(_titleLabel) { fixedWidth = 28f };
            if (_hintLabel == null)
                _hintLabel = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (_tinyLabel == null)
                _tinyLabel = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(10, GUI.skin.label.fontSize - 2) };
            if (_titleEdit == null)
                _titleEdit = new GUIStyle(GUI.skin.textField) { fontStyle = FontStyle.Bold };
            if (_badge == null)
            {
                _badge = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.85f, 0.9f, 1f, 1f) }
                };
            }
            if (_badgeError == null)
            {
                _badgeError = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.35f, 0.35f, 1f) }
                };
            }

            if (_cornerGlyph == null)
            {
                _cornerGlyph = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerRight,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.Max(GUI.skin.label.fontSize + 6, 14)
                };
            }
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

        private void DrawTreeWindow(int id)
        {

            GUILayout.Space(4);

            // Top row: Save name, Mission, controls, skin
    GUILayout.BeginHorizontal();
#if false
            GUILayout.Label("Save:", GUILayout.Width(40));
            string saveName = GetCurrentSaveName();
            GUI.enabled = false;
            GUILayout.TextField(saveName, GUILayout.Width(180));
            GUI.enabled = true;
            GUILayout.Space(8);
#endif
            GUILayout.Label("Mission:", GUILayout.Width(60));
            _missionName = GUILayout.TextField(_missionName ?? "", _titleEdit, GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
#if false
            if (GUILayout.Button("Summary…", GUILayout.Width(90)))
            {
                _showSummaryWindow = true;
                var mp = Input.mousePosition;
                _summaryRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _summaryRect.width - 40);
                _summaryRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _summaryRect.height - 40);
            }
#endif

            GUILayout.Space(12);
            _useKspSkin = GUILayout.Toggle(_useKspSkin, "Use KSP Skin", GUILayout.Width(120));
            GUILayout.EndHorizontal();

            // --- Mission Summary preview (read-only) ---
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            showSummary = GUILayout.Toggle(showSummary, "");

            if (showSummary)
            {
                GUILayout.Label("Summary Preview:", GUILayout.Width(120));
#if false
                if (GUILayout.Button("Expand…", GUILayout.Width(90)))
                {
                    _showSummaryWindow = true;
                    var mp2 = Input.mousePosition;
                    _summaryRect.x = Mathf.Clamp(mp2.x, 40, Screen.width - _summaryRect.width - 40);
                    _summaryRect.y = Mathf.Clamp(Screen.height - mp2.y, 40, Screen.height - _summaryRect.height - 40);
                }
#endif
            }
            else
                GUILayout.Label("Show Summary");

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (showSummary)
            {
                //GUI.enabled = false;
                _summaryScroll = GUILayout.BeginScrollView(_summaryScroll, HighLogic.Skin.textArea, GUILayout.Height(110));
                _missionSummary = GUILayout.TextArea(string.IsNullOrEmpty(_missionSummary) ? "(no summary)" : _missionSummary, GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();
                //GUI.enabled = true;
            }
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                if (GUILayout.Button("Add Objective/goal", GUILayout.Width(160)))
                {
                    var newRoot = new StepNode();
                    _roots.Add(newRoot);
                    _selectedNode = newRoot;
                    OpenDetail(newRoot); // auto-open details
                }
                if (GUILayout.Button("Expand All", GUILayout.Width(110))) SetAllExpanded(true);
                if (GUILayout.Button("Collapse All", GUILayout.Width(110))) SetAllExpanded(false);
                string str = "Short";
                if (currentView == View._short)
                    str = "Editable";
                if (GUILayout.Button(str))
                    currentView = 1 - currentView;
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(6);
            //GUILayout.Label("Double-click a title to edit in a separate window. Use ▲ ▼ ⤴ ⤵ Move… ⊕ Dup ✖ to manage hierarchy.", _hintLabel);
            if (currentView == View._short)
                GUILayout.Label("Done | Lock | Double-click a title to edit in a separate window.", _hintLabel);
            else
                GUILayout.Label("Done | Lock | Double-click a title to edit in a separate window. Use ▲ ▼ ⤴ ⤵ Move… Dup + ✖ to manage hierarchy.", _hintLabel);

            GUILayout.Space(4);

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var r in _roots)
            {
                DrawNodeRow(r, 0);
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();
#if false
            // --- Column tuning sliders (moved here to bottom) ---
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label(string.Format("Title Width %: {0}", (int)Mathf.Round(_titleWidthPct * 100f)), GUILayout.Width(140));
            _titleWidthPct = GUILayout.HorizontalSlider(_titleWidthPct, 0.40f, 0.95f, GUILayout.Width(160));
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Controls Pad: {0}px", (int)_controlsPad), GUILayout.Width(140));
            _controlsPad = Mathf.Round(GUILayout.HorizontalSlider(_controlsPad, 0f, 120f, GUILayout.Width(160)));
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Indent/Level: {0}px", (int)_indentPerLevel), GUILayout.Width(140));
            _indentPerLevel = Mathf.Round(GUILayout.HorizontalSlider(_indentPerLevel, 10f, 40f, GUILayout.Width(160)));
            GUILayout.Space(12);
            GUILayout.Label(string.Format("Fold Col: {0}px", (int)_foldColWidth), GUILayout.Width(120));
            _foldColWidth = Mathf.Round(GUILayout.HorizontalSlider(_foldColWidth, 16f, 48f, GUILayout.Width(160)));

            GUILayout.Space(16);
            if (GUILayout.Button("Reset", GUILayout.Width(80)))
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
            // --- Bottom bar: New / Clear All / Save / Save As… / Load / Hide + Save indicator ---
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("New", GUILayout.Width(90)))
            {
                _showNewConfirm = true;
                var mp = Input.mousePosition;
                _newConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _newConfirmRect.width - 40);
                _newConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _newConfirmRect.height - 40);
            }

            if (GUILayout.Button("Clear All", GUILayout.Width(90)))
            {
                _clearAddSample = false;
                _showClearConfirm = true;
                var mp = Input.mousePosition;
                _clearConfirmRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _clearConfirmRect.width - 40);
                _clearConfirmRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _clearConfirmRect.height - 40);
            }

            if (GUILayout.Button("Save", GUILayout.Width(90))) { TrySaveToDisk_Internal(true); }

            if (GUILayout.Button("Save As…", GUILayout.Width(100)))
            {
                _creatingNewMission = false;
                OpenSaveAsDialog();
            }

            if (GUILayout.Button("Load…", GUILayout.Width(90))) { OpenLoadDialog(); }

            GUILayout.FlexibleSpace();

            if (Time.realtimeSinceStartup <= _lastSaveShownUntil && !String.IsNullOrEmpty(_lastSaveInfo))
            {
                var style = _lastSaveWasSuccess ? _badge : _badgeError;
                GUILayout.Label(_lastSaveInfo, style);
            }

            GUILayout.Space(10);
            GUILayout.Label(string.Format("Count: {0}", CountAll()), _badge);
            if (GUILayout.Button("Close", GUILayout.Width(70)))
            {
                _visible = false;
                SyncToolbarState();
            }
            GUILayout.Space(20);
            GUILayout.EndHorizontal();

            this.resizeHandle.Draw(ref this._treeRect);

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
        private ResizeHandle resizeHandle;

        private void DrawNewConfirmWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label("Create a new mission. Save current mission first?", _hintLabel);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(100)))
            {
                TrySaveToDisk_Internal(true);
                _showNewConfirm = false;

                _creatingNewMission = true;
                _newMissionAddSample = true;
                _saveAsName = "New Mission";
                _saveAsSummaryText = "";
                OpenSaveAsDialog();
            }
            if (GUILayout.Button("Discard", GUILayout.Width(100)))
            {
                _showNewConfirm = false;
                _creatingNewMission = true;
                _newMissionAddSample = true;
                _saveAsName = "New Mission";
                _saveAsSummaryText = "";
                OpenSaveAsDialog();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _showNewConfirm = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawClearConfirmWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label("This will remove ALL steps from the current mission.\n(Your mission name is kept.)", _hintLabel);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Add sample root step", GUILayout.Width(160));
            _clearAddSample = GUILayout.Toggle(_clearAddSample, GUIContent.none, GUILayout.Width(22));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(100)))
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
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _showClearConfirm = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

#if false
        private void DrawSummaryWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label("Summary for mission:", _tinyLabel);
            GUILayout.Label(_missionName, _titleEdit);
            GUILayout.Space(6);

            _summaryScroll = GUILayout.BeginScrollView(_summaryScroll, HighLogic.Skin.textArea, GUILayout.MinHeight(180), GUILayout.ExpandHeight(true));
            _missionSummary = GUILayout.TextArea(_missionSummary ?? "", GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
#if false
            if (GUILayout.Button("Save", GUILayout.Width(100)))
            {
                TrySaveToDisk_Internal(true);
            }
#endif
            if (GUILayout.Button("Close", GUILayout.Width(100)))
            {
                _showSummaryWindow = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
#endif

        private void DrawNodeRow(StepNode node, int depth)
        {
            GUILayout.BeginVertical(); // HighLogic.Skin.textArea);
            GUILayout.BeginHorizontal();

            var b = GUILayout.Toggle(node.data.completed, "");
            if (!node.data.locked)
                node.data.completed = b;


            node.data.locked = GUILayout.Toggle(node.data.locked, "");

            float indent = 12f + _indentPerLevel * depth;
            GUILayout.Space(indent);

            if (node.Children.Count == 0)
            {
                GUILayout.Label("·", GUILayout.Width(_foldColWidth));
            }
            else
            {
                string sign = node.Expanded ? "−" : "+";
                if (GUILayout.Button(sign, _titleLabel, GUILayout.Width(_foldColWidth))) node.Expanded = !node.Expanded;
            }

            const float controlsBase = 100f;
            float controlsWidth = controlsBase + _controlsPad;
            float available = Mathf.Max(120f, _treeRect.width - indent - _foldColWidth - controlsWidth - 12f);
            float titleWidth = Mathf.Clamp(available * _titleWidthPct, 200, available);

            var style = (_selectedNode == node) ? _selectedTitle : _titleLabel;
            GUILayout.Label(node.data.title ?? "(untitled)", style, GUILayout.Width(titleWidth));
            Rect titleRect = GUILayoutUtility.GetLastRect();
            GUILayout.FlexibleSpace();
            bool up=false, down=false, promote=false, demote=false, moveTo=false, dup=false, add=false, del=false;
            if (currentView == View.editable)
            {
                up = GUILayout.Button("▲", _smallBtn);
                down = GUILayout.Button("▼", _smallBtn);
                promote = GUILayout.Button("⤴", _smallBtn);
                demote = GUILayout.Button("⤵", _smallBtn);
                moveTo = GUILayout.Button("Move…", GUILayout.Width(60f /* 54f */));
                dup = GUILayout.Button("Dup", GUILayout.Width(40f));
                GUILayout.Space(20);
                add = GUILayout.Button("+", _smallBtn /* "⊕" */, GUILayout.Width(28));
                del = GUILayout.Button("✖", _smallBtn);
            }
            GUILayout.EndHorizontal();

            HandleTitleClicks(node, titleRect);

            if (currentView == View.editable)
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
            if (node.Expanded && node.Children.Count > 0)
            {
                GUILayout.Space(2);
                foreach (var c in node.Children)
                {
                    DrawNodeRow(c, depth + 1);
                    GUILayout.Space(1);
                }
            }

            GUILayout.EndVertical();
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
            if (n.Parent == null) _roots.Remove(n);
            else n.Parent.Children.Remove(n);
            if (_selectedNode == n) _selectedNode = null;
            if (_detailNode == n) _detailNode = null;
        }

        private void Duplicate(StepNode n)
        {
            var cloned = new StepNode
            {
                data = new Step
                {
                    title = (n.data.title ?? "New Step") + " (copy)",
                    descr = n.data.descr,
                    stepType = n.data.stepType,
                    completed = n.data.completed,
                    locked = n.data.locked,
                    toggle = n.data.toggle,
                    initialToggleValue = n.data.initialToggleValue,
                    minFloatRange = n.data.minFloatRange,
                    maxFloatRange = n.data.maxFloatRange,
                    number = n.data.number,
                    partName = n.data.partName,
                    partTitle = n.data.partTitle,
                    partOnlyAvailable = n.data.partOnlyAvailable
                },
                Expanded = n.Expanded
            };

            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            list.Insert(Mathf.Clamp(idx + 1, 0, list.Count), cloned);
            cloned.Parent = n.Parent;
        }

        private void OpenMoveDialog(StepNode node)
        {
            if (_detailNode != null && _detailNode != node) TrySaveToDisk_Internal(true);

            _moveNode = node;
            _moveTargetParent = null;
            _showMoveDialog = true;

            var mp = Input.mousePosition;
            _moveRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _moveRect.width - 40);
            _moveRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _moveRect.height - 40);
        }

        private void DrawMoveDialogWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label("Choose a new parent (or Root). You cannot move an item under itself or its descendants.", _hintLabel);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            bool rootChosen = (_moveTargetParent == null);
            if (GUILayout.Toggle(rootChosen, "⟂ Root", HighLogic.Skin.toggle, GUILayout.Width(120)))
                _moveTargetParent = null;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Or select an existing parent:", _tinyLabel);

            _moveScroll = GUILayout.BeginScrollView(_moveScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var r in _roots) DrawMoveTargetRecursive(r, 0, _moveNode);
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(100)))
            {
                MoveToParent(_moveNode, _moveTargetParent);
                CloseMoveDialog();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100))) CloseMoveDialog();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawMoveTargetRecursive(StepNode node, int depth, StepNode moving)
        {
            if (node == moving || IsDescendant(moving, node)) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(12 + _indentPerLevel * depth);
            bool chosen = (_moveTargetParent == node);
            string label = string.Format("📁 {0}", node.data.title);
            if (GUILayout.Toggle(chosen, label, HighLogic.Skin.toggle))
                _moveTargetParent = node;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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
            if (_detailNode != null && _detailNode != node) TrySaveToDisk_Internal(true);

            _detailNode = node;
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
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label(_creatingNewMission ? "New mission name:" : "Save as name:", GUILayout.Width(140));
            GUI.SetNextControlName("SaveAsNameField");
            _saveAsName = GUILayout.TextField(_saveAsName ?? "", GUILayout.MinWidth(180), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            if (_creatingNewMission)
            {
                GUILayout.Space(6);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Add sample root step", GUILayout.Width(160));
                _newMissionAddSample = GUILayout.Toggle(_newMissionAddSample, GUIContent.none, GUILayout.Width(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                GUILayout.Label("Mission summary (optional):");
                _saveAsSummaryText = GUILayout.TextArea(_saveAsSummaryText ?? "", HighLogic.Skin.textArea, GUILayout.MinHeight(80));
            }

            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "SaveAsNameField")
                GUI.FocusControl("SaveAsNameField");

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(100)))
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
                        TrySaveToDisk_Internal(true);
                    }
                }
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _creatingNewMission = false;
                _showSaveAs = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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

        private void SeedSample()
        {
            _missionName = "Sample Mission";
            _missionSummary = "Example mission demonstrating preflight and ascent steps.";
            _roots.Clear();

            var preflight = new StepNode { data = new Step { title = "Preflight Checks", descr = "Before launch." }, Expanded = true };
            preflight.AddChild(new Step { title = "Batteries", stepType = CriterionType.toggle, initialToggleValue = true });
            preflight.AddChild(new Step { title = "Crew on board", stepType = CriterionType.toggle });
            var limits = preflight.AddChild(new Step { title = "Set Limits", stepType = CriterionType.range, minFloatRange = 0f, maxFloatRange = 5f });

            var ascent = new StepNode { data = new Step { title = "Ascent", descr = "Liftoff to orbit." }, Expanded = true };
            ascent.AddChild(new Step { title = "SAS On", stepType = CriterionType.toggle, initialToggleValue = true, toggle = true });

            preflight.AddChild(new Step { title = "Crew count 1–3", stepType = CriterionType.crewCount, crewCount = 1 });
            preflight.AddChild(new Step { title = "Has Parachute part", stepType = CriterionType.part, partName = "", partTitle = "", partOnlyAvailable = true });

            _roots.Add(preflight);
            _roots.Add(ascent);
        }

        private struct MissionFileInfo
        {
            public string FullPath;
            public string SaveName;
            public string MissionName;
            public DateTime LastWriteUtc;
        }


        private void OnAppLauncherReady()
        {
            if (ApplicationLauncher.Instance == null || _appButton != null) return;

            _appButton = ApplicationLauncher.Instance.AddModApplication(
                onTrue: () => { _visible = true; SetButtonIcon(_iconOn); },
                onFalse: () => { _visible = false; SetButtonIcon(_iconOff); },
                onHover: null, onHoverOut: null, onEnable: null, onDisable: null,
                visibleInScenes: _scenes,
                texture: _visible ? _iconOn : _iconOff
            );
            SyncToolbarState();
        }

        private void OnAppLauncherDestroyed()
        {
            if (_appButton != null && ApplicationLauncher.Instance != null)
                ApplicationLauncher.Instance.RemoveModApplication(_appButton);
            _appButton = null;
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
            float.TryParse(n.GetValue("x"), out x);
            float.TryParse(n.GetValue("y"), out y);
            float.TryParse(n.GetValue("w"), out w);
            float.TryParse(n.GetValue("h"), out h);
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

                bool.TryParse(root.GetValue("UseKspSkin"), out _useKspSkin);
                float.TryParse(root.GetValue("TitleWidthPct"), out _titleWidthPct);
                float.TryParse(root.GetValue("ControlsPad"), out _controlsPad);
                float.TryParse(root.GetValue("IndentPerLevel"), out _indentPerLevel);
                float.TryParse(root.GetValue("FoldColWidth"), out _foldColWidth);

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
