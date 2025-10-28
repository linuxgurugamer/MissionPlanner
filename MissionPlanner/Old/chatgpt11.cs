#if false
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace MissionPlanner
{
    // File: RecursiveCollapsibleList.cs
    // KSP1 (Unity IMGUI) – recursive tree WITHOUT drag/drop.
    // Per-row controls: ▲ Up, ▼ Down, ⤴ Promote (one level up), Move… (choose new parent), ⊕ Add child.
    // Also: expand/collapse (+/−), double-click label opens a details window, toolbar button toggle.
    //
    // Build against KSP1 assemblies; drop DLL in GameData/YourMod/Plugins.
    // Optional icons (no extension in path):
    //   GameData/YourMod/Icons/tree_on.png   -> "YourMod/Icons/tree_on"
    //   GameData/YourMod/Icons/tree_off.png  -> "YourMod/Icons/tree_off"




    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class RecursiveCollapsibleList : MonoBehaviour
    {
        private Rect _treeRect = new Rect(240, 140, 700, 560);
        private Rect _detailRect = new Rect(820, 180, 440, 340);
        private Rect _addRect = new Rect(760, 120, 360, 160);
        private Rect _moveRect = new Rect(720, 100, 420, 460);

        private int _treeWinId, _detailWinId, _addWinId, _moveWinId;
        private bool _visible = true;
        private Vector2 _scroll;
        private Vector2 _moveScroll;

        // Toolbar (AppLauncher)
        private ApplicationLauncherButton _appButton;
        private Texture2D _iconOn, _iconOff;
        private const string IconOnPath = "YourMod/Icons/tree_on";
        private const string IconOffPath = "YourMod/Icons/tree_off";
        private readonly ApplicationLauncher.AppScenes _scenes =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;

        // Styles
        private GUIStyle _labelLikeButton, _selectedLabel, _smallBtn, _tinyLabel, _hintLabel;
        private void EnsureStyles()
        {
            if (_labelLikeButton == null)
                _labelLikeButton = new GUIStyle(HighLogic.Skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(6, 6, 3, 3) };

            if (_selectedLabel == null)
            {
                _selectedLabel = new GUIStyle(_labelLikeButton);
                _selectedLabel.normal = _labelLikeButton.onNormal;
                _selectedLabel.hover = _labelLikeButton.onHover;
                _selectedLabel.active = _labelLikeButton.onActive;
            }

            if (_smallBtn == null)
            {
                _smallBtn = new GUIStyle(HighLogic.Skin.button) { fixedWidth = 28f, padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(2, 2, 2, 2) };
            }

            if (_tinyLabel == null)
            {
                _tinyLabel = new GUIStyle(HighLogic.Skin.label) { fontSize = Mathf.Max(10, HighLogic.Skin.label.fontSize - 2) };
            }

            if (_hintLabel == null)
            {
                _hintLabel = new GUIStyle(HighLogic.Skin.label) { wordWrap = true, fontSize = Mathf.Max(11, HighLogic.Skin.label.fontSize - 1) };
            }
        }

        // Click & double-click tracking
        private int _lastClickedNodeId = -1;
        private float _lastClickTime = 0f;
        private const float DoubleClickThreshold = 0.30f; // seconds

        // Selection state
        private Node _selectedNode = null;

        // Details & Add dialogs
        private Node _detailNode = null;
        private bool _showAddDialog = false;
        private Node _addTarget = null;
        private string _addText = "";

        // Move dialog (choose a new parent)
        private bool _showMoveDialog = false;
        private Node _moveNode = null;
        private Node _moveTargetParent = null; // null means "to root"

        [Serializable]
        public class Node
        {
            private static int _nextId = 1;
            public readonly int Id;
            public string Name;
            public bool Expanded;
            public List<Node> Children = new List<Node>();
            public string Notes = "";
            public Node Parent = null;

            public Node(string name, bool expanded = false)
            {
                Id = _nextId++;
                Name = name;
                Expanded = expanded;
            }

            public Node AddChild(string name, bool expanded = false)
            {
                var child = new Node(name, expanded) { Parent = this };
                Children.Add(child);
                return child;
            }
        }

        private readonly List<Node> _roots = new List<Node>();
        private readonly KeyCode _toggleKey = KeyCode.F8;

        // ---------- Lifecycle ----------
        public void Awake()
        {
            _treeWinId = GetHashCode();
            _detailWinId = _treeWinId ^ 0x5A17;
            _addWinId = _treeWinId ^ 0x71C3;
            _moveWinId = _treeWinId ^ 0x4B33;

            LoadIconsOrFallback();
            BuildSampleData();
            RebuildParentsForAllRoots();
        }

        public void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);
            if (ApplicationLauncher.Instance != null) OnAppLauncherReady();
        }

        public void OnDestroy()
        {
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
            if (!_visible) return;
            GUI.skin = HighLogic.Skin;
            EnsureStyles();

            _treeRect = GUILayout.Window(
                _treeWinId, _treeRect, DrawTreeWindow,
                "Recursive List (F8 to toggle)",
                GUILayout.MinWidth(560), GUILayout.MinHeight(340)
            );

            if (_detailNode != null)
            {
                _detailRect = GUILayout.Window(
                    _detailWinId, _detailRect, DrawDetailWindow,
                    $"Details — {_detailNode.Name}",
                    GUILayout.MinWidth(360), GUILayout.MinHeight(240)
                );
            }

            if (_showAddDialog && _addTarget != null)
            {
                _addRect = GUILayout.Window(
                    _addWinId, _addRect, DrawAddDialogWindow,
                    $"Add subitem to '{_addTarget.Name}'",
                    GUILayout.MinWidth(320), GUILayout.MinHeight(140)
                );
            }

            if (_showMoveDialog && _moveNode != null)
            {
                _moveRect = GUILayout.Window(
                    _moveWinId, _moveRect, DrawMoveDialogWindow,
                    $"Move '{_moveNode.Name}' — choose new parent",
                    GUILayout.MinWidth(380), GUILayout.MinHeight(320)
                );
            }
        }

        // ---------- Window: Tree ----------
        private void DrawTreeWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All", GUILayout.Width(110))) SetAllExpanded(true);
            if (GUILayout.Button("Collapse All", GUILayout.Width(110))) SetAllExpanded(false);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_visible ? "Hide" : "Show", GUILayout.Width(70)))
            {
                _visible = !_visible;
                SyncToolbarState();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Tips: ▲/▼ move among siblings. ⤴ promotes one level up. “Move…” lets you choose any parent (including Root). Double-click a label to open its details. ⊕ adds a child to that row.", _hintLabel);

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var root in _roots)
            {
                DrawNodeRecursive(root, 0);
                GUILayout.Space(2);
            }
            GUILayout.EndScrollView();
        }

        private void DrawNodeRecursive(Node node, int depth)
        {
            GUILayout.BeginVertical(HighLogic.Skin.textArea);

            GUILayout.BeginHorizontal();

            // Indent
            GUILayout.Space(12 + 18 * depth);

            // Fold (+/−)
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

            // Label (select / double-click opens details)
            var style = (_selectedNode == node) ? _selectedLabel : _labelLikeButton;
            var labelRectBefore = GUILayoutUtility.GetLastRect(); // not used; keeps layout aligned
            GUILayout.Label(node.Name, style, GUILayout.ExpandWidth(true));
            Rect labelRect = GUILayoutUtility.GetLastRect();

            // Row action buttons: Up / Down / Promote / Move… / Add child
            bool up = GUILayout.Button("▲", _smallBtn);
            bool down = GUILayout.Button("▼", _smallBtn);
            bool promote = GUILayout.Button("⤴", _smallBtn);
            bool moveTo = GUILayout.Button("Move…", GUILayout.Width(54f));
            bool add = GUILayout.Button("⊕", GUILayout.Width(26f));

            GUILayout.EndHorizontal();

            // Handle label clicks (single/double)
            HandleLabelClicks(node, labelRect);

            // Actions
            if (up) MoveUp(node);
            if (down) MoveDown(node);
            if (promote) Promote(node);
            if (moveTo) OpenMoveDialog(node);
            if (add) OpenAddDialog(node);

            // Children
            if (node.Expanded && node.Children.Count > 0)
            {
                GUILayout.Space(2);
                foreach (var child in node.Children)
                {
                    DrawNodeRecursive(child, depth + 1);
                    GUILayout.Space(1);
                }
            }

            GUILayout.EndVertical();
        }

        private void HandleLabelClicks(Node node, Rect labelRect)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && labelRect.Contains(e.mousePosition))
            {
                // select on mouse up to allow double-click detection timing
                e.Use();
            }
            if (e.type == EventType.MouseUp && e.button == 0 && labelRect.Contains(e.mousePosition))
            {
                if (_lastClickedNodeId == node.Id &&
                    (Time.realtimeSinceStartup - _lastClickTime) <= DoubleClickThreshold)
                {
                    OpenDetailFor(node);
                    _lastClickedNodeId = -1;
                }
                else
                {
                    _selectedNode = node;
                    _lastClickedNodeId = node.Id;
                    _lastClickTime = Time.realtimeSinceStartup;
                }
                e.Use();
            }
        }

        // ---------- Move operations ----------
        private void MoveUp(Node n)
        {
            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx > 0)
            {
                list.RemoveAt(idx);
                list.Insert(idx - 1, n);
            }
        }

        private void MoveDown(Node n)
        {
            var list = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = list.IndexOf(n);
            if (idx >= 0 && idx < list.Count - 1)
            {
                list.RemoveAt(idx);
                list.Insert(idx + 1, n);
            }
        }

        // Promote one level up: remove from parent.Children and insert right after the parent in its list
        private void Promote(Node n)
        {
            if (n.Parent == null) return; // already at root level
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

        // Move to a specific parent (null => root)
        private void MoveToParent(Node n, Node newParent)
        {
            if (n == null) return;
            if (newParent == n || IsDescendant(n, newParent)) return; // prevent cycles

            // remove from old
            var oldList = (n.Parent == null) ? _roots : n.Parent.Children;
            int oldIdx = oldList.IndexOf(n);
            if (oldIdx >= 0) oldList.RemoveAt(oldIdx);

            // add to new
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

        // ---------- Move dialog ----------
        private void OpenMoveDialog(Node node)
        {
            _moveNode = node;
            _moveTargetParent = null; // default to root
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
            GUILayout.Label("Choose a new parent for this entry (or Root). The entry cannot be moved under itself or its descendants.", _hintLabel);
            GUILayout.Space(6);

            // Root option
            GUILayout.BeginHorizontal();
            bool toRoot = (_moveTargetParent == null);
            if (GUILayout.Toggle(toRoot, "⟂ Root", HighLogic.Skin.toggle, GUILayout.Width(120)))
                _moveTargetParent = null;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Or pick an existing entry to be the new parent:", _tinyLabel);

            _moveScroll = GUILayout.BeginScrollView(_moveScroll, HighLogic.Skin.textArea, GUILayout.ExpandHeight(true));
            foreach (var root in _roots)
                DrawMoveTargetRecursive(root, 0, _moveNode);
            GUILayout.EndScrollView();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(100)))
            {
                MoveToParent(_moveNode, _moveTargetParent);
                CloseMoveDialog();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                CloseMoveDialog();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawMoveTargetRecursive(Node node, int depth, Node movingNode)
        {
            // Exclude self and descendants
            if (node == movingNode || IsDescendant(movingNode, node)) return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(12 + 18 * depth);

            bool chosen = (_moveTargetParent == node);
            string label = $"📁 {node.Name}";
            if (GUILayout.Toggle(chosen, label, HighLogic.Skin.toggle))
                _moveTargetParent = node;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                    DrawMoveTargetRecursive(child, depth + 1, movingNode);
            }
        }

        private void CloseMoveDialog()
        {
            _showMoveDialog = false;
            _moveNode = null;
            _moveTargetParent = null;
        }

        // ---------- Add child dialog ----------
        private void DrawAddDialogWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            if (_addTarget == null) { _showAddDialog = false; return; }

            GUILayout.Space(6);
            GUILayout.Label("New subitem name:");
            GUI.SetNextControlName("AddTextField");
            _addText = GUILayout.TextField(_addText ?? "");

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK", GUILayout.Width(90)))
            {
                var name = (_addText ?? "").Trim();
                if (!string.IsNullOrEmpty(name))
                {
                    _addTarget.AddChild(name);
                    _addTarget.Expanded = true;
                    ScreenMessages.PostScreenMessage($"Added '{name}' under '{_addTarget.Name}'.", 3f, ScreenMessageStyle.UPPER_LEFT);
                }
                CloseAddDialog();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(90))) CloseAddDialog();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "AddTextField")
                GUI.FocusControl("AddTextField");
        }

        private void OpenAddDialog(Node node)
        {
            _addTarget = node; _addText = ""; _showAddDialog = true;
            var mp = Input.mousePosition;
            _addRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _addRect.width - 40);
            _addRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _addRect.height - 40);
        }

        private void CloseAddDialog()
        {
            _showAddDialog = false; _addTarget = null; _addText = "";
        }

        // ---------- Details ----------
        private void DrawDetailWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            if (_detailNode == null) return;

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Item:", GUILayout.Width(60));
            GUILayout.Label(_detailNode.Name, HighLogic.Skin.label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80))) { _detailNode = null; return; }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Notes (editable):");
            _detailNode.Notes = GUILayout.TextArea(_detailNode.Notes, GUILayout.ExpandHeight(true));

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save"))
                ScreenMessages.PostScreenMessage($"Saved notes for '{_detailNode.Name}'.", 3f, ScreenMessageStyle.UPPER_LEFT);
            if (GUILayout.Button("Clear"))
                _detailNode.Notes = "";
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // ---------- Helpers ----------
        private void OpenDetailFor(Node node)
        {
            _detailNode = node;
            var mp = Input.mousePosition;
            _detailRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _detailRect.width - 40);
            _detailRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _detailRect.height - 40);
        }

        private void HandleLabelDoubleClick(Node node)
        {
            OpenDetailFor(node);
        }

        private void SetAllExpanded(bool expanded)
        {
            foreach (var r in _roots) SetExpandedRecursive(r, expanded);
        }

        private void SetExpandedRecursive(Node n, bool expanded)
        {
            n.Expanded = expanded;
            foreach (var c in n.Children) SetExpandedRecursive(c, expanded);
        }

        public void SetData(IEnumerable<Node> roots)
        {
            _roots.Clear();
            if (roots != null) _roots.AddRange(roots);
            RebuildParentsForAllRoots();
        }

        private void RebuildParentsForAllRoots()
        {
            foreach (var r in _roots) { r.Parent = null; RebuildParentsRecursive(r); }
        }

        private void RebuildParentsRecursive(Node n)
        {
            foreach (var c in n.Children)
            {
                c.Parent = n;
                RebuildParentsRecursive(c);
            }
        }

        private bool IsDescendant(Node ancestor, Node maybeDescendant)
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

        private Rect UnionRects(Rect a, Rect b)
        {
            if (a.width <= 0f || a.height <= 0f) return b;
            if (b.width <= 0f || b.height <= 0f) return a;
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        // ---------- Sample data ----------
        private void BuildSampleData()
        {
            _roots.Clear();

            var engines = new Node("Engines");
            engines.AddChild("LV-T45 Swivel");
            engines.AddChild("LV-909 Terrier");
            var advanced = engines.AddChild("Advanced Engines");
            advanced.AddChild("Vector");
            advanced.AddChild("Mammoth").AddChild("Mammoth Mk2");

            var tanks = new Node("Fuel Tanks");
            tanks.AddChild("FL-T200");
            tanks.AddChild("FL-T400");
            var xSeries = tanks.AddChild("X-Series");
            xSeries.AddChild("X200-16");
            xSeries.AddChild("X200-32");

            var science = new Node("Science");
            var field = science.AddChild("Field Experiments");
            field.AddChild("Seismic Accelerometer");
            field.AddChild("Mystery Goo™");
            science.AddChild("Science Jr.");

            _roots.Add(engines);
            _roots.Add(tanks);
            _roots.Add(science);
        }

        // ---------- Toolbar ----------
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
                if (_appButton.toggleButton.CurrentState == KSP.UI.UIRadioButton.State.False) _appButton.SetTrue();
                SetButtonIcon(_iconOn);
            }
            else
            {
                if (_appButton.toggleButton.CurrentState == KSP.UI.UIRadioButton.State.True) _appButton.SetFalse();
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
            if ( tex != null) _appButton.SetTexture(tex);
        }

        private Texture2D MakeSolidTexture(int w, int h, Color c)
        {
            var t = new Texture2D(w, h, TextureFormat.ARGB32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            t.SetPixels(pixels); t.Apply();
            return t;
        }
    }
}
#endif