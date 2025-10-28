#if false
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MissionPlanner
{
    // File: HierarchicalStepsWindow.cs
    // KSP1 (Unity IMGUI) – hierarchical Step list with separate detail editor.
    // Adds: Save/Load to ConfigNode and hides window when game is paused.
    //
    // Save path: GameData/YourMod/PluginData/Steps.cfg
    // (Change SAVE_MOD_FOLDER / SAVE_FILE_NAME to suit your mod.)


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

        // ---- Serialization ----
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

        // ---- Serialization ----
        public ConfigNode ToConfigNodeRecursive()
        {
            var n = new ConfigNode("NODE");
            n.AddValue("title", data.title ?? "");
            n.AddValue("expanded", Expanded);

            // Embed full Step data
            n.AddNode(data.ToConfigNode());

            // Children
            foreach (var c in Children)
                n.AddNode(c.ToConfigNodeRecursive());

            return n;
        }

        public static StepNode FromConfigNodeRecursive(ConfigNode n)
        {
            var node = new StepNode();
            node.data.title = n.GetValue("title") ?? node.data.title;
            bool ex;
            if (bool.TryParse(n.GetValue("expanded"), out ex)) node.Expanded = ex;

            // STEP child node
            var stepNode = n.GetNode("STEP");
            if (stepNode != null) node.data = Step.FromConfigNode(stepNode);

            // children
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
        private Rect _treeRect = new Rect(220, 120, 760, 560);
        private Rect _detailRect = new Rect(820, 160, 520, 520);
        private Rect _moveRect = new Rect(760, 120, 440, 480);

        private int _treeWinId, _detailWinId, _moveWinId;
        private bool _visible = true;
        private bool _visibleBeforePause = true; // remember state when paused

        // Toolbar
        private ApplicationLauncherButton _appButton;
        private Texture2D _iconOn, _iconOff;
        private const string IconOnPath = "YourMod/Icons/tree_on";
        private const string IconOffPath = "YourMod/Icons/tree_off";
        private readonly ApplicationLauncher.AppScenes _scenes =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;

        // Save/Load
        private const string SAVE_ROOT_NODE = "HIERARCHICAL_STEPS";
        private const string SAVE_LIST_NODE = "ROOTS";
        private const string SAVE_MOD_FOLDER = "YourMod/PluginData";
        private const string SAVE_FILE_NAME = "Steps.cfg";

        // Data (tree)
        private readonly List<StepNode> _roots = new List<StepNode>();

        // Selection / dialogs
        private StepNode _selectedNode = null;
        private StepNode _detailNode = null;     // shows Step Details window
        private bool _showMoveDialog = false;
        private StepNode _moveNode = null;
        private StepNode _moveTargetParent = null; // null => root

        // Scroll
        private Vector2 _scroll;
        private Vector2 _moveScroll;

        // Styles
        private GUIStyle _titleLabel, _selectedTitle, _smallBtn, _hintLabel, _tinyLabel, _titleEdit, _badge;
        private void EnsureStyles()
        {
            if (_titleLabel == null)
                _titleLabel = new GUIStyle(HighLogic.Skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(6, 6, 3, 3) };
            if (_selectedTitle == null)
            {
                _selectedTitle = new GUIStyle(_titleLabel);
                _selectedTitle.normal = _titleLabel.onNormal;
                _selectedTitle.hover = _titleLabel.onHover;
                _selectedTitle.active = _titleLabel.onActive;
            }
            if (_smallBtn == null)
                _smallBtn = new GUIStyle(HighLogic.Skin.button) { fixedWidth = 28f, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(2, 2, 2, 2) };
            if (_hintLabel == null)
                _hintLabel = new GUIStyle(HighLogic.Skin.label) { wordWrap = true };
            if (_tinyLabel == null)
                _tinyLabel = new GUIStyle(HighLogic.Skin.label) { fontSize = Mathf.Max(10, HighLogic.Skin.label.fontSize - 2) };
            if (_titleEdit == null)
                _titleEdit = new GUIStyle(HighLogic.Skin.textField) { fontStyle = FontStyle.Bold };
            if (_badge == null)
            {
                _badge = new GUIStyle(HighLogic.Skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.85f, 0.9f, 1f, 1f) }
                };
            }
        }

        private readonly KeyCode _toggleKey = KeyCode.F10;

        // Double-click tracking
        private int _lastClickedId = -1;
        private float _lastClickTime = 0f;
        private const float DoubleClickSec = 0.30f;

        // ---- Lifecycle ----
        public void Awake()
        {
            _treeWinId = GetHashCode();
            _detailWinId = _treeWinId ^ 0x5A17;
            _moveWinId = _treeWinId ^ 0x4B33;

            LoadIconsOrFallback();

            // Load previous session (if present); otherwise seed sample
            if (!TryLoadFromDisk())
            {
                SeedSample();
            }
            ReparentAll();

            // Pause / Unpause hooks
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
            // Auto-save
            TrySaveToDisk();

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
            // Extra guard: do not draw while paused
            if (PauseMenu.isOpen) return;

            if (!_visible) return;
            GUI.skin = HighLogic.Skin;
            EnsureStyles();

            _treeRect = GUILayout.Window(
                _treeWinId, _treeRect, DrawTreeWindow,
                "Steps (F10 to toggle) — only titles shown",
                GUILayout.MinWidth(680), GUILayout.MinHeight(360)
            );

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
        }

        // ---- Pause handling ----
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

        // ---- Tree Window ----
        private void DrawTreeWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Root", GUILayout.Width(100))) _roots.Add(new StepNode());
            if (GUILayout.Button("Expand All", GUILayout.Width(110))) SetAllExpanded(true);
            if (GUILayout.Button("Collapse All", GUILayout.Width(110))) SetAllExpanded(false);

            GUILayout.Space(12);
            if (GUILayout.Button("Save", GUILayout.Width(80))) { if (TrySaveToDisk()) ScreenMessages.PostScreenMessage("Steps saved.", 2f, ScreenMessageStyle.UPPER_LEFT); }
            if (GUILayout.Button("Load", GUILayout.Width(80))) { if (TryLoadFromDisk()) ScreenMessages.PostScreenMessage("Steps loaded.", 2f, ScreenMessageStyle.UPPER_LEFT); }

            GUILayout.FlexibleSpace();
            GUILayout.Label($"Count: {CountAll()}", _badge);
            if (GUILayout.Button(_visible ? "Hide" : "Show", GUILayout.Width(70)))
            {
                _visible = !_visible; SyncToolbarState();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Only the step titles are shown below. Double-click a title or click “Edit…” to modify data in a separate window. Use ▲ ▼ ⤴ ⤵ Move… ⊕ Dup ✖ to manage hierarchy. Window hides when the game is paused.", _hintLabel);

            GUILayout.Space(4);
            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var r in _roots)
            {
                DrawNodeRow(r, 0);
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();
        }

        private void DrawNodeRow(StepNode node, int depth)
        {
            GUILayout.BeginVertical(HighLogic.Skin.textArea);
            GUILayout.BeginHorizontal();

            // Indent & fold
            GUILayout.Space(12 + 18 * depth);
            if (node.Children.Count == 0)
            {
                GUILayout.Label("·", GUILayout.Width(24));
            }
            else
            {
                string sign = node.Expanded ? "−" : "+";
                if (GUILayout.Button(sign, GUILayout.Width(24)))
                    node.Expanded = !node.Expanded;
            }

            // Title only
            var style = (_selectedNode == node) ? _selectedTitle : _titleLabel;
            GUILayout.Label(node.data.title ?? "(untitled)", style, GUILayout.ExpandWidth(true));
            Rect titleRect = GUILayoutUtility.GetLastRect();

            // Row controls
            bool up = GUILayout.Button("▲", _smallBtn);
            bool down = GUILayout.Button("▼", _smallBtn);
            bool promote = GUILayout.Button("⤴", _smallBtn);
            bool demote = GUILayout.Button("⤵", _smallBtn);
            bool moveTo = GUILayout.Button("Move…", GUILayout.Width(54f));
            bool edit = GUILayout.Button("Edit…", GUILayout.Width(54f));
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
            if (edit) OpenDetail(node);
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
            if (e.type == EventType.MouseDown && e.button == 0 && titleRect.Contains(e.mousePosition))
            {
                e.Use();
            }
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
            if (idx > 0)
            {
                list.RemoveAt(idx);
                list.Insert(idx - 1, n);
            }
        }

        private void MoveDown(StepNode n)
        {
            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx >= 0 && idx < list.Count - 1)
            {
                list.RemoveAt(idx);
                list.Insert(idx + 1, n);
            }
        }

        private void Promote(StepNode n)
        {
            if (n.Parent == null) return; // already root
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
            if (idx <= 0) return; // need a previous sibling
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

        // Move dialog
        private void OpenMoveDialog(StepNode node)
        {
            _moveNode = node;
            _moveTargetParent = null; // default: move to root
            _showMoveDialog = true;

            var mp = Input.mousePosition;
            _moveRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _moveRect.width - 40);
            _moveRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _moveRect.height - 40);
        }

        private void DrawMoveDialogWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            if (_moveNode == null) { _showMoveDialog = false; return; }

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
        }

        private void DrawMoveTargetRecursive(StepNode node, int depth, StepNode moving)
        {
            if (node == moving || IsDescendant(moving, node)) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(12 + 18 * depth);
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

        // ---- Details Window (data editing) ----
        private void OpenDetail(StepNode node)
        {
            _detailNode = node;
            var mp = Input.mousePosition;
            _detailRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _detailRect.width - 40);
            _detailRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _detailRect.height - 40);
        }

        private void DrawDetailWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            if (_detailNode == null) return;

            var s = _detailNode.data;

            GUILayout.Space(6);

            // Title
            GUILayout.BeginHorizontal();
            GUILayout.Label("Title", GUILayout.Width(60));
            s.title = GUILayout.TextField(s.title ?? "", _titleEdit, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80))) { _detailNode = null; return; }
            GUILayout.EndHorizontal();

            // Description
            GUILayout.Space(6);
            GUILayout.Label("Description:");
            s.descr = GUILayout.TextArea(s.descr ?? "", HighLogic.Skin.textArea, GUILayout.MinHeight(60));

            GUILayout.Space(6);
            // Step type
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type", GUILayout.Width(60));
            var vals = (StepType[])Enum.GetValues(typeof(StepType));
            int curIdx = Array.IndexOf(vals, s.stepType);
            if (GUILayout.Button("◀", GUILayout.Width(26))) { curIdx = (curIdx - 1 + vals.Length) % vals.Length; s.stepType = vals[curIdx]; }
            GUILayout.Label(s.stepType.ToString(), GUILayout.Width(200));
            if (GUILayout.Button("▶", GUILayout.Width(26))) { curIdx = (curIdx + 1) % vals.Length; s.stepType = vals[curIdx]; }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Fields
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
                ScreenMessages.PostScreenMessage($"Saved “{s.title}”.", 2f, ScreenMessageStyle.UPPER_LEFT);
                // Optional: auto-save entire tree on detail save
                TrySaveToDisk();
            }
            if (GUILayout.Button("Close", GUILayout.Width(100))) _detailNode = null;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
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
        private void SetAllExpanded(bool ex)
        {
            foreach (var r in _roots) SetExpandedRecursive(r, ex);
        }
        private void SetExpandedRecursive(StepNode n, bool ex)
        {
            n.Expanded = ex;
            foreach (var c in n.Children) SetExpandedRecursive(c, ex);
        }

        private bool IsDescendant(StepNode ancestor, StepNode maybeDescendant)
        {
            if (ancestor == null || maybeDescendant == null) return false;
            var p = maybeDescendant.Parent;
            while (p != null)
            {
                if (p == ancestor) return true;
                p = p.Parent;
            }
            return false;
        }

        private int CountAll()
        {
            int count = 0;
            foreach (var r in _roots) count += CountRecursive(r);
            return count;
        }
        private int CountRecursive(StepNode n)
        {
            int c = 1;
            foreach (var ch in n.Children) c += CountRecursive(ch);
            return c;
        }

        private void ReparentAll()
        {
            foreach (var r in _roots) { r.Parent = null; ReparentRecursive(r); }
        }
        private void ReparentRecursive(StepNode n)
        {
            foreach (var c in n.Children) { c.Parent = n; ReparentRecursive(c); }
        }

        // ---- Sample data (only used if no save exists) ----
        private void SeedSample()
        {
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

        // ---- Save / Load (ConfigNode) ----
        private string GetSaveDirectoryAbsolute()
        {
            // GameData/<SAVE_MOD_FOLDER>
            return Path.Combine(KSPUtil.ApplicationRootPath, "GameData", SAVE_MOD_FOLDER);
        }
        private string GetSaveFileAbsolute()
        {
            return Path.Combine(GetSaveDirectoryAbsolute(), SAVE_FILE_NAME);
        }

        private bool TrySaveToDisk()
        {
            try
            {
                var root = new ConfigNode(SAVE_ROOT_NODE);
                var list = new ConfigNode(SAVE_LIST_NODE);
                root.AddNode(list);

                foreach (var r in _roots)
                    list.AddNode(r.ToConfigNodeRecursive());

                Directory.CreateDirectory(GetSaveDirectoryAbsolute());
                root.Save(GetSaveFileAbsolute());
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HierarchicalStepsWindow] Save failed: {ex}");
                ScreenMessages.PostScreenMessage("Save failed (see log).", 3f, ScreenMessageStyle.UPPER_LEFT);
                return false;
            }
        }

        private bool TryLoadFromDisk()
        {
            try
            {
                string path = GetSaveFileAbsolute();
                if (!File.Exists(path)) return false;

                var root = ConfigNode.Load(path);
                if (root == null || !root.HasNode(SAVE_LIST_NODE)) return false;

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
                Debug.LogError($"[HierarchicalStepsWindow] Load failed: {ex}");
                ScreenMessages.PostScreenMessage("Load failed (see log).", 3f, ScreenMessageStyle.UPPER_LEFT);
                return false;
            }
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
    }
}
#endif