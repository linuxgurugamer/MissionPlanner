#if false
using System;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;


namespace MissionPlanner
{

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class RecursiveCollapsibleList : MonoBehaviour
    {
        private Rect _treeRect = new Rect(240, 140, 560, 560);
        private Rect _detailRect = new Rect(820, 180, 440, 340);
        private Rect _addRect = new Rect(760, 120, 340, 140);
        private Rect _moveRect = new Rect(760, 320, 420, 420);

        private int _treeWinId, _detailWinId, _addWinId, _moveWinId;
        private bool _visible = true;
        private Vector2 _scroll;

        // ---- Toolbar (AppLauncher) ----
        private ApplicationLauncherButton _appButton;
        private Texture2D _iconOn, _iconOff;
        private const string IconOnPath = "YourMod/Icons/tree_on";
        private const string IconOffPath = "YourMod/Icons/tree_off";
        private readonly ApplicationLauncher.AppScenes _scenes =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;

        // ---- Double-click tracking ----
        private int _lastClickedNodeId = -1;
        private float _lastClickTime = 0f;
        private const float DoubleClickThreshold = 0.30f; // seconds

        // Details window
        private Node _detailNode = null;

        // Add-subitem dialog
        private bool _showAddDialog = false;
        private Node _addTarget = null;
        private string _addText = "";

        // Move-to dialog
        private bool _showMoveDialog = false;
        private Node _moveSubject = null;           // the node we're moving
        private Node _moveSelection = null;         // candidate new parent
        private Vector2 _moveScroll;

        [Serializable]
        public class Node
        {
            private static int _nextId = 1;
            public readonly int Id;
            public string Name;
            public bool Expanded;
            public List<Node> Children = new List<Node>();
            public string Notes = "";
            public Node Parent = null;              // NEW: parent tracking

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
            _moveWinId = _treeWinId ^ 0x3D55;

            LoadIconsOrFallback();
            BuildSampleData();
            RebuildParentsForAllRoots();

            GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(OnAppLauncherDestroyed);
        }

        public void Start()
        {
            if (ApplicationLauncher.Instance != null)
                OnAppLauncherReady();
        }

        public void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(OnAppLauncherDestroyed);

            if (_appButton != null)
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

            _treeRect = GUILayout.Window(
                _treeWinId,
                _treeRect,
                DrawTreeWindow,
                "Recursive List (F8 to toggle)",
                GUILayout.MinWidth(440),
                GUILayout.MinHeight(320)
            );

            if (_detailNode != null)
            {
                _detailRect = GUILayout.Window(
                    _detailWinId,
                    _detailRect,
                    DrawDetailWindow,
                    $"Details — {_detailNode.Name}",
                    GUILayout.MinWidth(340),
                    GUILayout.MinHeight(220)
                );
            }

            if (_showAddDialog && _addTarget != null)
            {
                _addRect = GUILayout.Window(
                    _addWinId,
                    _addRect,
                    DrawAddDialogWindow,
                    $"Add subitem to '{_addTarget.Name}'",
                    GUILayout.MinWidth(300),
                    GUILayout.MinHeight(120)
                );
            }

            if (_showMoveDialog && _moveSubject != null)
            {
                _moveRect = GUILayout.Window(
                    _moveWinId,
                    _moveRect,
                    DrawMoveDialogWindow,
                    $"Move '{_moveSubject.Name}' → choose parent",
                    GUILayout.MinWidth(360),
                    GUILayout.MinHeight(240)
                );
            }
        }

        // ---------- UI: Tree ----------
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
            GUILayout.Label("Controls: ± expand/collapse • double-click name = details • ⊕ add child • ↑/↓ reorder in siblings • ⟵ promote • ⇢ Move To… choose any parent.", HighLogic.Skin.label);

            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (var root in _roots)
            {
                DrawNodeRecursive(root, depth: 0);
                GUILayout.Space(2);
            }

            GUILayout.EndScrollView();
        }

        private void DrawNodeRecursive(Node node, int depth)
        {
            GUILayout.BeginVertical(HighLogic.Skin.textArea);

            GUILayout.BeginHorizontal();

            // Indentation per depth
            GUILayout.Space(12 + 18 * depth);

            // Fold handle (left)
            if (node.Children.Count == 0)
            {
                GUILayout.Label("·", GUILayout.Width(24)); // placeholder when no children
            }
            else
            {
                string sign = node.Expanded ? "−" : "+";
                if (GUILayout.Button(sign, GUILayout.Width(24)))
                    node.Expanded = !node.Expanded;
            }

            // Label (double-click detection)
            bool labelClicked = GUILayout.Button(node.Name, HighLogic.Skin.button, GUILayout.ExpandWidth(true));
            if (labelClicked)
            {
                if (_lastClickedNodeId == node.Id && (Time.realtimeSinceStartup - _lastClickTime) <= DoubleClickThreshold)
                {
                    OpenDetailFor(node);             // double-click
                    _lastClickedNodeId = -1;
                }
                else
                {
                    _lastClickedNodeId = node.Id;    // single-click
                    _lastClickTime = Time.realtimeSinceStartup;

                    if (node.Children.Count > 0)
                        node.Expanded = !node.Expanded;
                    else
                        OpenDetailFor(node);
                }
            }

            // --- Right side controls per row ---
            // Up / Down within siblings
            if (GUILayout.Button("↑", GUILayout.Width(26))) MoveUp(node);
            if (GUILayout.Button("↓", GUILayout.Width(26))) MoveDown(node);

            // Promote (one level up)
            GUI.enabled = node.Parent != null;
            if (GUILayout.Button("⟵", GUILayout.Width(26))) Promote(node);
            GUI.enabled = true;

            // Add child
            if (GUILayout.Button("⊕", GUILayout.Width(26))) OpenAddDialog(node);

            // Move To… choose any existing parent
            if (GUILayout.Button("⇢", GUILayout.Width(26))) OpenMoveDialog(node);

            GUILayout.EndHorizontal();

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

        // ---------- UI: Details ----------
        private void DrawDetailWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            if (_detailNode == null) return;

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Item:", GUILayout.Width(60));
            GUILayout.Label(_detailNode.Name, HighLogic.Skin.label);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                _detailNode = null;
                return;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Notes (editable):");
            _detailNode.Notes = GUILayout.TextArea(_detailNode.Notes, GUILayout.ExpandHeight(true));

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) ScreenMessages.PostScreenMessage($"Saved notes for '{_detailNode.Name}'.", 3f, ScreenMessageStyle.UPPER_LEFT);
            if (GUILayout.Button("Clear")) _detailNode.Notes = "";
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // ---------- UI: Add subitem dialog ----------
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
                    _addTarget.Expanded = true; // reveal new child
                    ScreenMessages.PostScreenMessage($"Added '{name}' under '{_addTarget.Name}'.", 3f, ScreenMessageStyle.UPPER_LEFT);
                }
                CloseAddDialog();
            }
            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
            {
                CloseAddDialog();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "AddTextField")
                GUI.FocusControl("AddTextField");
        }

        private void OpenAddDialog(Node node)
        {
            _addTarget = node;
            _addText = "";
            _showAddDialog = true;

            var mp = Input.mousePosition; // bottom-left origin
            _addRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _addRect.width - 40);
            _addRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _addRect.height - 40);
        }

        private void CloseAddDialog()
        {
            _showAddDialog = false;
            _addTarget = null;
            _addText = "";
        }

        // ---------- UI: Move-to dialog ----------
        private void DrawMoveDialogWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            if (_moveSubject == null) { _showMoveDialog = false; return; }

            GUILayout.Space(6);
            GUILayout.Label("Choose the new parent for this entry. (You cannot move into itself or its descendants.)");

            _moveScroll = GUILayout.BeginScrollView(_moveScroll, GUILayout.ExpandHeight(true));

            // Also offer "make root" option
            bool makeRoot = (_moveSelection == null);
            bool canMakeRoot = _moveSubject.Parent != null; // if already root, it's a no-op
            GUILayout.BeginHorizontal();
            GUI.enabled = canMakeRoot;
            bool rootChosen = GUILayout.Toggle(makeRoot, "<no parent> (make root)", HighLogic.Skin.toggle);
            GUI.enabled = true;
            if (canMakeRoot && rootChosen) _moveSelection = null;

            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            // Draw a selectable tree of all nodes
            foreach (var r in _roots)
            {
                DrawMoveTargetRecursive(r, depth: 0);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            bool okEnabled = _moveSelection == null || (!IsSameOrDescendant(_moveSelection, _moveSubject));
            GUI.enabled = okEnabled;
            if (GUILayout.Button("OK", GUILayout.Width(90)))
            {
                ApplyMoveToParent(_moveSubject, _moveSelection);
                CloseMoveDialog();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
            {
                CloseMoveDialog();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawMoveTargetRecursive(Node candidate, int depth)
        {
            // Skip illegal candidates: the subject itself and its descendants
            bool illegal = IsSameOrDescendant(candidate, _moveSubject);

            GUILayout.BeginHorizontal();
            GUILayout.Space(12 + 18 * depth);
            GUI.enabled = !illegal;

            bool chosen = GUILayout.Toggle(_moveSelection == candidate, candidate.Name, HighLogic.Skin.toggle);
            if (!illegal && chosen) _moveSelection = candidate;

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            foreach (var c in candidate.Children)
                DrawMoveTargetRecursive(c, depth + 1);
        }

        private void OpenMoveDialog(Node subject)
        {
            _moveSubject = subject;
            _moveSelection = subject.Parent; // default to current parent
            _showMoveDialog = true;

            var mp = Input.mousePosition; // bottom-left origin
            _moveRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _moveRect.width - 40);
            _moveRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _moveRect.height - 40);
        }

        private void CloseMoveDialog()
        {
            _showMoveDialog = false;
            _moveSubject = null;
            _moveSelection = null;
        }

        // ---------- Move helpers ----------
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

        // Promote: move node to the parent's parent (after the parent, for a natural outline feel)
        private void Promote(Node n)
        {
            if (n.Parent == null) return;

            var parent = n.Parent;
            var grand = parent.Parent;

            // Remove from current
            var oldList = parent.Children;
            int oldIdx = oldList.IndexOf(n);
            if (oldIdx >= 0) oldList.RemoveAt(oldIdx);

            // Insert into new list
            var newList = (grand == null) ? _roots : grand.Children;
            int parentIndex = newList.IndexOf(parent);
            int insertAt = (parentIndex >= 0) ? parentIndex + 1 : newList.Count;
            newList.Insert(insertAt, n);
            n.Parent = grand;
        }

        // Reparent to any target (or root if target == null)
        private void ApplyMoveToParent(Node n, Node newParent)
        {
            if (newParent == n || IsDescendant(n, newParent)) return; // guard

            // Remove from current list
            var oldList = (n.Parent == null) ? _roots : n.Parent.Children;
            int idx = oldList.IndexOf(n);
            if (idx >= 0) oldList.RemoveAt(idx);

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

        private bool IsSameOrDescendant(Node candidate, Node subject)
        {
            return candidate == subject || IsDescendant(subject, candidate);
        }

        // ---------- Misc helpers ----------
        private void OpenDetailFor(Node node)
        {
            _detailNode = node;
            var mp = Input.mousePosition;
            _detailRect.x = Mathf.Clamp(mp.x, 40, Screen.width - _detailRect.width - 40);
            _detailRect.y = Mathf.Clamp(Screen.height - mp.y, 40, Screen.height - _detailRect.height - 40);
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

        /// <summary>Replace demo data with your real tree at runtime.</summary>
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
                onTrue: OnToolbarToggleOn,
                onFalse: OnToolbarToggleOff,
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

        private void OnToolbarToggleOn()
        {
            _visible = true;
            SetButtonIcon(_iconOn);
        }

        private void OnToolbarToggleOff()
        {
            _visible = false;
            SetButtonIcon(_iconOff);
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
            t.SetPixels(pixels);
            t.Apply();
            return t;
        }
    }
}
#endif