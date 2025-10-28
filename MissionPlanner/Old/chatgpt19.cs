#if false

// File: HierarchicalStepsWindow.cs
// Mod: MissionPlanner
// KSP1 (Unity IMGUI)
//
// This version:
// - Runtime sliders for Title Width %, Controls Pad, Indent/Level, Fold Column Width (all persisted in UI.cfg)
// - "Edit…" buttons removed (double-click title to edit)
// - Save/Load/Hide at bottom; DragWindow called last in each window
// - Persists skin + window positions and UI tuning values
// - Load dialog delete; Save overwrite w/ auto-increment
//
// Missions path: GameData/MissionPlanner/PluginData/<SaveName>__<MissionName>.cfg
// UI path:       GameData/MissionPlanner/PluginData/UI.cfg

using KSP;
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MissionPlanner
{
    public enum StepType { toggle, intNotGreaterThan, intNotLessThan, floatNotGreaterThan, floatNotLessThan, intRange, floatRange }

    [Serializable]
    public class Step
    {
        public string title = "New Step";
        public string descr = "";

        public StepType stepType = StepType.toggle;

        public bool toggle = false;
        public bool initialToggleValue = false;

        public int minIntRange = 0;
        public int maxIntRange = 10;

        public float minFloatRange = 0f;
        public float maxFloatRange = 1f;

        public int intNotGreaterThan = 100;
        public int intNotLessThan = 0;
        public float floatNotGreaterThan = 100f;
        public float floatNotLessThan = 0f;

        public ConfigNode ToConfigNode()
        {
            var n = new ConfigNode("STEP");
            n.AddValue("title", title ?? "");
            n.AddValue("descr", descr ?? "");
            n.AddValue("stepType", stepType.ToString());
            n.AddValue("toggle", toggle);
            n.AddValue("initialToggleValue", initialToggleValue);
            n.AddValue("minIntRange", minIntRange);
            n.AddValue("maxIntRange", maxIntRange);
            n.AddValue("minFloatRange", minFloatRange);
            n.AddValue("maxFloatRange", maxFloatRange);
            n.AddValue("intNotGreaterThan", intNotGreaterThan);
            n.AddValue("intNotLessThan", intNotLessThan);
            n.AddValue("floatNotGreaterThan", floatNotGreaterThan);
            n.AddValue("floatNotLessThan", floatNotLessThan);
            return n;
        }

        public static Step FromConfigNode(ConfigNode n)
        {
            var s = new Step();
            s.title = n.GetValue("title") ?? s.title;
            s.descr = n.GetValue("descr") ?? s.descr;
            if (Enum.TryParse(n.GetValue("stepType"), out StepType t)) s.stepType = t;

            bool.TryParse(n.GetValue("toggle"), out s.toggle);
            bool.TryParse(n.GetValue("initialToggleValue"), out s.initialToggleValue);

            int.TryParse(n.GetValue("minIntRange"), out s.minIntRange);
            int.TryParse(n.GetValue("maxIntRange"), out s.maxIntRange);

            float.TryParse(n.GetValue("minFloatRange"), out s.minFloatRange);
            float.TryParse(n.GetValue("maxFloatRange"), out s.maxFloatRange);

            int.TryParse(n.GetValue("intNotGreaterThan"), out s.intNotGreaterThan);
            int.TryParse(n.GetValue("intNotLessThan"), out s.intNotLessThan);

            float.TryParse(n.GetValue("floatNotGreaterThan"), out s.floatNotGreaterThan);
            float.TryParse(n.GetValue("floatNotLessThan"), out s.floatNotLessThan);

            return s;
        }
    }

    [Serializable]
    public class StepNode
    {
        private static int _nextId = 1;
        public readonly int Id;
        public Step data = new Step();
        public bool Expanded = true;
        public StepNode Parent = null;
        public List<StepNode> Children = new List<StepNode>();

        public StepNode() { Id = _nextId++; }

        public StepNode AddChild(Step childStep = null)
        {
            var n = new StepNode { data = childStep ?? new Step(), Parent = this };
            Children.Add(n);
            return n;
        }

        public ConfigNode ToConfigNodeRecursive()
        {
            var n = new ConfigNode("NODE");
            n.AddValue("title", data.title ?? "");
            n.AddValue("expanded", Expanded);
            n.AddNode(data.ToConfigNode());
            foreach (var c in Children)
                n.AddNode(c.ToConfigNodeRecursive());
            return n;
        }

        public static StepNode FromConfigNodeRecursive(ConfigNode n)
        {
            var node = new StepNode();
            node.data.title = n.GetValue("title") ?? node.data.title;
            if (bool.TryParse(n.GetValue("expanded"), out bool ex)) node.Expanded = ex;

            var stepNode = n.GetNode("STEP");
            if (stepNode != null) node.data = Step.FromConfigNode(stepNode);

            foreach (var cn in n.GetNodes("NODE"))
            {
                var child = FromConfigNodeRecursive(cn);
                child.Parent = node;
                node.Children.Add(child);
            }
            return node;
        }
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class HierarchicalStepsWindow : MonoBehaviour
    {
        // Windows
        private Rect _treeRect = new Rect(220, 120, 800, 600);
        private Rect _detailRect = new Rect(820, 160, 520, 520);
        private Rect _moveRect = new Rect(760, 120, 440, 480);
        private Rect _loadRect = new Rect(720, 100, 560, 540);
        private Rect _overwriteRect = new Rect(760, 180, 520, 220);
        private Rect _deleteRect = new Rect(760, 160, 520, 180);

        private int _treeWinId, _detailWinId, _moveWinId, _loadWinId, _overwriteWinId, _deleteWinId;
        private bool _visible = true;
        private bool _visibleBeforePause = true;

        // Skin toggle (persisted)
        private bool _useKspSkin = true;

        // Column tuning (persisted)
        private float _titleWidthPct = 0.75f;  // 40–95%
        private float _controlsPad = 40f;    // 0–120 px
        private float _indentPerLevel = 18f;    // 10–40 px
        private float _foldColWidth = 24f;    // 16–48 px

        // Toolbar
        private ApplicationLauncherButton _appButton;
        private Texture2D _iconOn, _iconOff;
        private const string IconOnPath = "MissionPlanner/Icons/tree_on";
        private const string IconOffPath = "MissionPlanner/Icons/tree_off";
        private readonly ApplicationLauncher.AppScenes _scenes =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;

        // Missions I/O
        private const string SAVE_ROOT_NODE = "MISSION_PLANNER";
        private const string SAVE_LIST_NODE = "ROOTS";
        private const string SAVE_MOD_FOLDER = "MissionPlanner/PluginData";
        private const string SAVE_FILE_EXT = ".cfg";

        // UI persistence
        private const string UI_FILE_NAME = "UI.cfg";
        private const string UI_ROOT_NODE = "MISSION_PLANNER_UI";

        // Mission meta
        private string _missionName = "Untitled Mission";

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

        // Scroll
        private Vector2 _scroll;
        private Vector2 _moveScroll;

        // Styles
        private GUIStyle _titleLabel, _selectedTitle, _smallBtn, _hintLabel, _tinyLabel, _titleEdit, _badge;
        bool lastUseKSPSkin = true;

        private readonly KeyCode _toggleKey = KeyCode.F10;

        // Double-click
        private int _lastClickedId = -1;
        private float _lastClickTime = 0f;
        private const float DoubleClickSec = 0.30f;

        public void Awake()
        {
            _treeWinId = GetHashCode();
            _detailWinId = _treeWinId ^ 0x5A17;
            _moveWinId = _treeWinId ^ 0x4B33;
            _loadWinId = _treeWinId ^ 0x23A1;
            _overwriteWinId = _treeWinId ^ 0x7321;
            _deleteWinId = _treeWinId ^ 0x19C7;

            LoadIconsOrFallback();
            LoadUISettings();

            if (!TryAutoLoadMostRecentForCurrentSave()) SeedSample();
            ReparentAll();

            GameEvents.onGamePause.Add(OnGamePaused);
            GameEvents.onGameUnpause.Add(OnGameUnpaused);
        }

        public void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);
            if (ApplicationLauncher.Instance != null) OnAppLauncherReady();
        }

        public void OnDestroy()
        {
            TrySaveToDisk_Internal(overwriteOk: true);
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

        public void Update()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _visible = !_visible;
                SyncToolbarState();
            }
        }

        public void OnGUI()
        {
            if (PauseMenu.isOpen) return;
            if (_useKspSkin) GUI.skin = HighLogic.Skin;
            EnsureStyles();

            if (_visible)
            {
                _treeRect = GUILayout.Window(
                    _treeWinId, _treeRect, DrawTreeWindow,
                    "MissionPlanner — Steps (F10 to toggle)",
                    GUILayout.MinWidth(700), GUILayout.MinHeight(380)
                );
            }
            if (_detailNode != null)
            {
                _detailRect = GUILayout.Window(
                    _detailWinId, _detailRect, DrawDetailWindow,
                    $"Step Details — {_detailNode.data.title}",
                    GUILayout.MinWidth(480), GUILayout.MinHeight(420)
                );
            }
            if (_showMoveDialog && _moveNode != null)
            {
                _moveRect = GUILayout.Window(
                    _moveWinId, _moveRect, DrawMoveDialogWindow,
                    $"Move “{_moveNode.data.title}” — choose new parent",
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
        }

        // ---- EnsureStyles (rebuilds on skin toggle) ----
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
            }

            if (_titleLabel == null)
                _titleLabel = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(6, 6, 3, 3) };
            if (_selectedTitle == null)
            {
                _selectedTitle = new GUIStyle(_titleLabel);
                _selectedTitle.normal = _titleLabel.onNormal;
                _selectedTitle.hover = _titleLabel.onHover;
                _selectedTitle.active = _titleLabel.onActive;
            }
            if (_smallBtn == null)
                _smallBtn = new GUIStyle(GUI.skin.button) { fixedWidth = 28f, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(2, 2, 2, 2) };
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
        }

        // ---- Pause handling ----
        private void OnGamePaused() { _visibleBeforePause = _visible; _visible = false; SyncToolbarState(); }
        private void OnGameUnpaused() { _visible = _visibleBeforePause; SyncToolbarState(); }

        // ---------------- Tree Window ----------------
        private void DrawTreeWindow(int id)
        {
            GUILayout.Space(4);

            // Top row: Save name, Mission, Add/Expand/Collapse, Skin toggle
            GUILayout.BeginHorizontal();
            GUILayout.Label("Save:", GUILayout.Width(40));
            string saveName = GetCurrentSaveName();
            GUI.enabled = false;
            GUILayout.TextField(saveName, GUILayout.Width(180));
            GUI.enabled = true;

            GUILayout.Space(8);
            GUILayout.Label("Mission:", GUILayout.Width(60));
            _missionName = GUILayout.TextField(_missionName ?? "", _titleEdit, GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));

            GUILayout.Space(8);
            if (GUILayout.Button("Add Root", GUILayout.Width(100))) _roots.Add(new StepNode());
            if (GUILayout.Button("Expand All", GUILayout.Width(110))) SetAllExpanded(true);
            if (GUILayout.Button("Collapse All", GUILayout.Width(110))) SetAllExpanded(false);

            GUILayout.Space(12);
            _useKspSkin = GUILayout.Toggle(_useKspSkin, "Use KSP Skin", GUILayout.Width(120));
            GUILayout.EndHorizontal();

            // Column tuning controls
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Title Width %: {(int)Mathf.Round(_titleWidthPct * 100f)}", GUILayout.Width(140));
            _titleWidthPct = GUILayout.HorizontalSlider(_titleWidthPct, 0.40f, 0.95f, GUILayout.Width(160));
            GUILayout.Space(12);
            GUILayout.Label($"Controls Pad: {(int)_controlsPad}px", GUILayout.Width(140));
            _controlsPad = Mathf.Round(GUILayout.HorizontalSlider(_controlsPad, 0f, 120f, GUILayout.Width(160)));
            GUILayout.Space(12);
            GUILayout.Label($"Indent/Level: {(int)_indentPerLevel}px", GUILayout.Width(140));
            _indentPerLevel = Mathf.Round(GUILayout.HorizontalSlider(_indentPerLevel, 10f, 40f, GUILayout.Width(160)));
            GUILayout.Space(12);
            GUILayout.Label($"Fold Col: {(int)_foldColWidth}px", GUILayout.Width(120));
            _foldColWidth = Mathf.Round(GUILayout.HorizontalSlider(_foldColWidth, 16f, 48f, GUILayout.Width(160)));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Only the step titles are shown below. Double-click a title to edit its data in a separate window. Use ▲ ▼ ⤴ ⤵ Move… ⊕ Dup ✖ to manage hierarchy.", _hintLabel);

            GUILayout.Space(4);
            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var r in _roots)
            {
                DrawNodeRow(r, 0);
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();

            // --- Bottom bar: Save / Load / Hide ---
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(90))) { TrySaveToDisk(); }
            if (GUILayout.Button("Load…", GUILayout.Width(90))) { OpenLoadDialog(); }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Count: {CountAll()}", _badge);
            if (GUILayout.Button(_visible ? "Hide" : "Show", GUILayout.Width(70))) { _visible = !_visible; SyncToolbarState(); }
            GUILayout.EndHorizontal();

            // Drag last
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawNodeRow(StepNode node, int depth)
        {
            GUILayout.BeginVertical(HighLogic.Skin.textArea);
            GUILayout.BeginHorizontal();

            // Indent & fold (runtime-tuned)
            float indent = 12f + _indentPerLevel * depth;
            GUILayout.Space(indent);

            if (node.Children.Count == 0)
            {
                GUILayout.Label("·", GUILayout.Width(_foldColWidth));
            }
            else
            {
                string sign = node.Expanded ? "−" : "+";
                if (GUILayout.Button(sign, GUILayout.Width(_foldColWidth))) node.Expanded = !node.Expanded;
            }

            // Compute title column width with runtime tuning
            // Base controls width (sum of buttons) + user padding
            const float controlsBase = 314f; // ▲(28) ▼(28) ⤴(28) ⤵(28) Move(54) Dup(40) ⊕(26) ✖(28) + margins
            float controlsWidth = controlsBase + _controlsPad;
            float available = Mathf.Max(120f, _treeRect.width - indent - _foldColWidth - controlsWidth - 12f);
            float titleWidth = Mathf.Clamp(available * _titleWidthPct, 120f, available);

            var style = (_selectedNode == node) ? _selectedTitle : _titleLabel;
            GUILayout.Label(node.data.title ?? "(untitled)", style, GUILayout.Width(titleWidth));
            Rect titleRect = GUILayoutUtility.GetLastRect();

            // Row controls (no Edit button)
            bool up = GUILayout.Button("▲", _smallBtn);
            bool down = GUILayout.Button("▼", _smallBtn);
            bool promote = GUILayout.Button("⤴", _smallBtn);
            bool demote = GUILayout.Button("⤵", _smallBtn);
            bool moveTo = GUILayout.Button("Move…", GUILayout.Width(54f));
            bool dup = GUILayout.Button("Dup", GUILayout.Width(40f));
            bool add = GUILayout.Button("⊕", GUILayout.Width(26f));
            bool del = GUILayout.Button("✖", _smallBtn);

            GUILayout.EndHorizontal();

            // Title click: select / double-click → open details
            HandleTitleClicks(node, titleRect);

            // Actions
            if (up) MoveUp(node);
            if (down) MoveDown(node);
            if (promote) Promote(node);
            if (demote) Demote(node);
            if (moveTo) OpenMoveDialog(node);
            if (dup) Duplicate(node);
            if (add) { node.AddChild(); node.Expanded = true; }
            if (del) Delete(node);

            // Children
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

        // ---- Moves / hierarchy ops ----
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
                    toggle = n.data.toggle,
                    initialToggleValue = n.data.initialToggleValue,
                    minIntRange = n.data.minIntRange,
                    maxIntRange = n.data.maxIntRange,
                    minFloatRange = n.data.minFloatRange,
                    maxFloatRange = n.data.maxFloatRange,
                    intNotGreaterThan = n.data.intNotGreaterThan,
                    intNotLessThan = n.data.intNotLessThan,
                    floatNotGreaterThan = n.data.floatNotGreaterThan,
                    floatNotLessThan = n.data.floatNotLessThan
                },
                Expanded = n.Expanded
            };

            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            list.Insert(Mathf.Clamp(idx + 1, 0, list.Count), cloned);
            cloned.Parent = n.Parent;
        }

        // ---- Move dialog ----
        private void OpenMoveDialog(StepNode node)
        {
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
            string label = $"📁 {node.data.title}";
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

        // ---- Details Window ----
        private void OpenDetail(StepNode node)
        {
            _detailNode = node;
            var mp = Input.mousePosition;
            _detailRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _detailRect.width - 40);
            _detailRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _detailRect.height - 40);
        }

        private void DrawDetailWindow(int id)
        {
            GUILayout.Space(6);

            if (_detailNode == null) { GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }

            var s = _detailNode.data;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Title", GUILayout.Width(60));
            s.title = GUILayout.TextField(s.title ?? "", _titleEdit, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80))) { _detailNode = null; GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Description:");
            s.descr = GUILayout.TextArea(s.descr ?? "", HighLogic.Skin.textArea, GUILayout.MinHeight(60));

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type", GUILayout.Width(60));
            var vals = (StepType[])Enum.GetValues(typeof(StepType));
            int curIdx = Array.IndexOf(vals, s.stepType);
            if (GUILayout.Button("◀", GUILayout.Width(26))) { curIdx = (curIdx - 1 + vals.Length) % vals.Length; s.stepType = vals[curIdx]; }
            GUILayout.Label(s.stepType.ToString(), GUILayout.Width(200));
            if (GUILayout.Button("▶", GUILayout.Width(26))) { curIdx = (curIdx + 1) % vals.Length; s.stepType = vals[curIdx]; }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            switch (s.stepType)
            {
                case StepType.toggle:
                    GUILayout.BeginHorizontal();
                    s.initialToggleValue = GUILayout.Toggle(s.initialToggleValue, "Initial Value", GUILayout.Width(140));
                    s.toggle = GUILayout.Toggle(s.toggle, "Current Value", GUILayout.Width(140));
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    break;

                case StepType.intNotGreaterThan:
                    IntField("≤ (int)", ref s.intNotGreaterThan);
                    break;

                case StepType.intNotLessThan:
                    IntField("≥ (int)", ref s.intNotLessThan);
                    break;

                case StepType.floatNotGreaterThan:
                    FloatField("≤ (float)", ref s.floatNotGreaterThan);
                    break;

                case StepType.floatNotLessThan:
                    FloatField("≥ (float)", ref s.floatNotLessThan);
                    break;

                case StepType.intRange:
                    IntRangeFields(ref s.minIntRange, ref s.maxIntRange);
                    if (s.minIntRange > s.maxIntRange)
                        GUILayout.Label("Warning: minIntRange is greater than maxIntRange.", _tinyLabel);
                    break;

                case StepType.floatRange:
                    FloatRangeFields(ref s.minFloatRange, ref s.maxFloatRange);
                    if (s.minFloatRange > s.maxFloatRange)
                        GUILayout.Label("Warning: minFloatRange is greater than maxFloatRange.", _tinyLabel);
                    break;
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(100)))
            {
                ScreenMessages.PostScreenMessage($"Saved step “{s.title}”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                TrySaveToDisk();
            }
            if (GUILayout.Button("Close", GUILayout.Width(100))) _detailNode = null;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        // ---- Field helpers ----
        private void IntField(string label, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            string buf = GUILayout.TextField(value.ToString(), GUILayout.Width(120));
            if (int.TryParse(buf, out int parsed)) value = parsed;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void FloatField(string label, ref float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            string buf = GUILayout.TextField(value.ToString("G"), GUILayout.Width(120));
            if (float.TryParse(buf, out float parsed)) value = parsed;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void IntRangeFields(ref int min, ref int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min (int)", GUILayout.Width(90));
            string minBuf = GUILayout.TextField(min.ToString(), GUILayout.Width(120));
            GUILayout.Space(12);
            GUILayout.Label("Max (int)", GUILayout.Width(90));
            string maxBuf = GUILayout.TextField(max.ToString(), GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (int.TryParse(minBuf, out int pmin)) min = pmin;
            if (int.TryParse(maxBuf, out int pmax)) max = pmax;
        }

        private void FloatRangeFields(ref float min, ref float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min (float)", GUILayout.Width(90));
            string minBuf = GUILayout.TextField(min.ToString("G"), GUILayout.Width(120));
            GUILayout.Space(12);
            GUILayout.Label("Max (float)", GUILayout.Width(90));
            string maxBuf = GUILayout.TextField(max.ToString("G"), GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (float.TryParse(minBuf, out float pmin)) min = pmin;
            if (float.TryParse(maxBuf, out float pmax)) max = pmax;
        }

        // ---- Tree helpers ----
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
            _roots.Clear();

            var preflight = new StepNode { data = new Step { title = "Preflight Checks", descr = "Before launch." }, Expanded = true };
            preflight.AddChild(new Step { title = "Batteries", stepType = StepType.toggle, initialToggleValue = true });
            preflight.AddChild(new Step { title = "Crew on board", stepType = StepType.toggle });
            var limits = preflight.AddChild(new Step { title = "Set Limits", stepType = StepType.floatRange, minFloatRange = 0f, maxFloatRange = 5f });
            limits.AddChild(new Step { title = "Max G ≤ 5.5", stepType = StepType.floatNotGreaterThan, floatNotGreaterThan = 5.5f });

            var ascent = new StepNode { data = new Step { title = "Ascent", descr = "Liftoff to orbit." }, Expanded = true };
            ascent.AddChild(new Step { title = "SAS On", stepType = StepType.toggle, initialToggleValue = true, toggle = true });
            ascent.AddChild(new Step { title = "Throttle ≥ 50%", stepType = StepType.intNotLessThan, intNotLessThan = 50 });

            _roots.Add(preflight);
            _roots.Add(ascent);
        }

        // ---- Missions I/O ----
        private string GetCurrentSaveName() => HighLogic.SaveFolder ?? "UnknownSave";

        private static string SanitizeForFile(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unnamed";
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c.ToString(), "_");
            return s.Trim();
        }

        private string GetSaveDirectoryAbsolute() => Path.Combine(KSPUtil.ApplicationRootPath, "GameData", SAVE_MOD_FOLDER);
        private string GetCombinedFileName(string save, string mission) => $"{SanitizeForFile(save)}__{SanitizeForFile(mission)}{SAVE_FILE_EXT}";
        private string GetSaveFileAbsolute(string save, string mission) => Path.Combine(GetSaveDirectoryAbsolute(), GetCombinedFileName(save, mission));

        private void TrySaveToDisk()
        {
            string save = GetCurrentSaveName();
            string mission = string.IsNullOrWhiteSpace(_missionName) ? "Unnamed" : _missionName.Trim();
            string path = GetSaveFileAbsolute(save, mission);

            if (File.Exists(path))
            {
                _pendingSavePath = path;
                _pendingSaveMission = mission;
                _showOverwriteDialog = true;

                var mp = Input.mousePosition;
                _overwriteRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _overwriteRect.width - 40);
                _overwriteRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _overwriteRect.height - 40);
            }
            else
            {
                TrySaveToDisk_Internal(overwriteOk: true);
            }
        }

        private bool TrySaveToDisk_Internal(bool overwriteOk)
        {
            try
            {
                string save = GetCurrentSaveName();
                string mission = string.IsNullOrWhiteSpace(_missionName) ? "Unnamed" : _missionName.Trim();
                string full = GetSaveFileAbsolute(save, mission);

                if (File.Exists(full) && !overwriteOk) return false;

                var root = new ConfigNode(SAVE_ROOT_NODE);
                root.AddValue("SaveName", save);
                root.AddValue("MissionName", mission);
                root.AddValue("SavedUtc", DateTime.UtcNow.ToString("o"));

                var list = new ConfigNode(SAVE_LIST_NODE);
                root.AddNode(list);

                foreach (var r in _roots)
                    list.AddNode(r.ToConfigNodeRecursive());

                Directory.CreateDirectory(GetSaveDirectoryAbsolute());
                root.Save(full);
                ScreenMessages.PostScreenMessage($"Mission saved: {Path.GetFileName(full)}", 2f, ScreenMessageStyle.UPPER_LEFT);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionPlanner] Save failed: {ex}");
                ScreenMessages.PostScreenMessage("Save failed (see log).", 3f, ScreenMessageStyle.UPPER_LEFT);
                return false;
            }
        }

        private bool TryLoadFromDisk(string fullPath)
        {
            try
            {
                if (!File.Exists(fullPath)) return false;

                var root = ConfigNode.Load(fullPath);
                if (root == null || !root.HasNode(SAVE_LIST_NODE)) return false;

                _missionName = root.GetValue("MissionName") ?? _missionName;

                var list = root.GetNode(SAVE_LIST_NODE);
                var newRoots = new List<StepNode>();
                foreach (var n in list.GetNodes("NODE"))
                {
                    var node = StepNode.FromConfigNodeRecursive(n);
                    node.Parent = null;
                    newRoots.Add(node);
                }

                _roots.Clear();
                _roots.AddRange(newRoots);
                ReparentAll();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionPlanner] Load failed: {ex}");
                ScreenMessages.PostScreenMessage("Load failed (see log).", 3f, ScreenMessageStyle.UPPER_LEFT);
                return false;
            }
        }

        private bool TryAutoLoadMostRecentForCurrentSave()
        {
            string save = GetCurrentSaveName();
            var list = GetAllMissionFiles();
            MissionFileInfo best = default;
            bool found = false;

            foreach (var mf in list)
            {
                if (mf.SaveName.Equals(save, StringComparison.OrdinalIgnoreCase))
                {
                    if (!found || mf.LastWriteUtc > best.LastWriteUtc)
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

        // ---- Load dialog data + UI ----
        private struct MissionFileInfo
        {
            public string FullPath;
            public string SaveName;
            public string MissionName;
            public DateTime LastWriteUtc;
        }

        private List<MissionFileInfo> GetAllMissionFiles()
        {
            var results = new List<MissionFileInfo>();
            try
            {
                string dir = GetSaveDirectoryAbsolute();
                if (!Directory.Exists(dir)) return results;
                foreach (var file in Directory.GetFiles(dir, "*" + SAVE_FILE_EXT, SearchOption.TopDirectoryOnly))
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
                        save = cn?.GetValue("SaveName") ?? "UnknownSave";
                        mission = cn?.GetValue("MissionName") ?? name;
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
                Debug.LogError($"[MissionPlanner] Listing missions failed: {ex}");
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
            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Show all saves", GUILayout.Width(120));
            _loadShowAllSaves = GUILayout.Toggle(_loadShowAllSaves, GUIContent.none, GUILayout.Width(22));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(90))) _loadList = GetAllMissionFiles();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            string curSave = GetCurrentSaveName();
            GUILayout.Label(_loadShowAllSaves ? "All Missions" : $"Missions for save: {curSave}", _tinyLabel);

            GUILayout.Space(4);
            _loadScroll = GUILayout.BeginScrollView(_loadScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var mf in _loadList)
            {
                if (!_loadShowAllSaves && !mf.SaveName.Equals(curSave, StringComparison.OrdinalIgnoreCase))
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Save: {mf.SaveName}", GUILayout.Width(180));
                GUILayout.Label($"Mission: {mf.MissionName}", GUILayout.ExpandWidth(true));
                GUILayout.Label(mf.LastWriteUtc.ToLocalTime().ToString("g"), GUILayout.Width(140));

                if (GUILayout.Button("Open", GUILayout.Width(70)))
                {
                    if (TryLoadFromDisk(mf.FullPath))
                    {
                        _missionName = mf.MissionName;
                        ScreenMessages.PostScreenMessage($"Loaded mission “{_missionName}”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                        _showLoadDialog = false;
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("Failed to load mission (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                    }
                }
                if (GUILayout.Button("Delete", GUILayout.Width(70)))
                {
                    _deleteTarget = mf;
                    _showDeleteConfirm = true;

                    var mp = Input.mousePosition;
                    _deleteRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _deleteRect.width - 40);
                    _deleteRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _deleteRect.height - 40);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(100))) _showLoadDialog = false;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawDeleteDialogWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label($"Delete mission:\nSave: {_deleteTarget.SaveName}\nMission: {_deleteTarget.MissionName}", _hintLabel);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete", GUILayout.Width(100)))
            {
                try
                {
                    if (File.Exists(_deleteTarget.FullPath))
                        File.Delete(_deleteTarget.FullPath);
                    ScreenMessages.PostScreenMessage("Mission deleted.", 2f, ScreenMessageStyle.UPPER_LEFT);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MissionPlanner] Delete failed: {ex}");
                    ScreenMessages.PostScreenMessage("Delete failed (see log).", 2f, ScreenMessageStyle.UPPER_LEFT);
                }
                finally
                {
                    _showDeleteConfirm = false;
                    _loadList = GetAllMissionFiles(); // refresh
                }
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _showDeleteConfirm = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void DrawOverwriteDialogWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label("A mission with this name already exists.", _tinyLabel);

            GUILayout.Space(4);
            GUILayout.Label($"Save: {GetCurrentSaveName()}", _hintLabel);
            GUILayout.Label($"Mission: {_pendingSaveMission}", _hintLabel);
            GUILayout.Label($"File: {Path.GetFileName(_pendingSavePath)}", _hintLabel);

            GUILayout.Space(12);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Overwrite", GUILayout.Width(120)))
            {
                _showOverwriteDialog = false;
                TrySaveToDisk_Internal(overwriteOk: true);
            }
            if (GUILayout.Button("Auto-Increment & Save", GUILayout.Width(180)))
            {
                string next = GetAutoIncrementName(_pendingSaveMission);
                _missionName = next;
                _pendingSaveMission = null;
                _pendingSavePath = null;
                _showOverwriteDialog = false;
                TrySaveToDisk_Internal(overwriteOk: true);
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _showOverwriteDialog = false;
                _pendingSaveMission = null;
                _pendingSavePath = null;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private string GetAutoIncrementName(string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName)) baseName = "Unnamed";
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
                    if (int.TryParse(inside, out int parsed))
                    {
                        stripped = stripped.Substring(0, p).TrimEnd();
                        n = parsed + 1;
                    }
                }
                name = $"{stripped} ({n})";
                n++;
            }
            return name;
        }

        // ---- Toolbar / AppLauncher ----
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

        // ---- UI settings persistence ----
        private string GetUIFileAbsolute()
        {
            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", SAVE_MOD_FOLDER, UI_FILE_NAME);
        }

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

                root.Save(GetUIFileAbsolute());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionPlanner] SaveUISettings failed: {ex}");
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionPlanner] LoadUISettings failed: {ex}");
            }
        }
    }
}
#endif