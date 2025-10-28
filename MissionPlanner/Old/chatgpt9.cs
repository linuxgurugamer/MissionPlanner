#if false
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace MissionPlanner
{

    // File: RecursiveCollapsibleList.cs
    // KSP1 (Unity IMGUI) – recursive tree with reliable drag & drop
    // - DRAG HANDLE ONLY (vertical "⋮⋮" grip)
    // - Stable control IDs + screen-space hit testing
    // - Toolbar button, double-click details, per-node add dialog, hover-to-expand
    // - Drop before/after/as-child. Single-click on label selects; only ± toggles.
    //
    // Build against KSP1 assemblies; drop DLL in GameData/YourMod/Plugins.
    // Optional icons (no extension in path):
    //   GameData/YourMod/Icons/tree_on.png   -> "YourMod/Icons/tree_on"
    //   GameData/YourMod/Icons/tree_off.png  -> "YourMod/Icons/tree_off"


        [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
        public class RecursiveCollapsibleList : MonoBehaviour
        {
            private Rect _treeRect = new Rect(240, 140, 600, 560);
            private Rect _detailRect = new Rect(820, 180, 440, 340);
            private Rect _addRect = new Rect(760, 120, 340, 140);

            private int _treeWinId, _detailWinId, _addWinId;
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

            // ---- Styles ----
            private GUIStyle _labelLikeButton, _selectedLabel, _dragHandleStyle;
            private void EnsureStyles()
            {
                if (_labelLikeButton == null)
                {
                    _labelLikeButton = new GUIStyle(HighLogic.Skin.button) { alignment = TextAnchor.MiddleLeft };
                }
                if (_selectedLabel == null)
                {
                    _selectedLabel = new GUIStyle(_labelLikeButton);
                    _selectedLabel.normal = _labelLikeButton.onNormal;
                    _selectedLabel.hover = _labelLikeButton.onHover;
                    _selectedLabel.active = _labelLikeButton.onActive;
                }
                if (_dragHandleStyle == null)
                {
                    _dragHandleStyle = new GUIStyle(HighLogic.Skin.button)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fixedWidth = 18f
                    };
                    _dragHandleStyle.fontStyle = FontStyle.Normal;
                }
            }

            // ---- Click & double-click tracking ----
            private int _lastClickedNodeId = -1;
            private float _lastClickTime = 0f;
            private const float DoubleClickThreshold = 0.30f; // seconds

            // ---- Selection state ----
            private Node _selectedNode = null;

            // ---- Details & Add dialogs ----
            private Node _detailNode = null;
            private bool _showAddDialog = false;
            private Node _addTarget = null;
            private string _addText = "";

            // ---- Drag & Drop ----
            private enum DropKind { None, Before, After, AsChild }
            private Node _dragNode = null;            // node being dragged
            private Node _dragTarget = null;          // last valid target hovered
            private DropKind _dragKind = DropKind.None;
            private float _hoverStart = 0f;           // for auto-expand
            private Node _hoverNode = null;
            private const float AutoExpandDelay = 0.6f; // seconds
            private readonly Color _dropLineColor = new Color(0.9f, 0.9f, 0.2f, 0.9f);
            private readonly Color _dropFillColor = new Color(0.95f, 0.95f, 0.25f, 0.25f);

            // Per-row mouse capture (hotControl) + drag threshold
            private const float DragThresholdSqr = 6f * 6f; // ~6px
            private Vector2 _mouseDownPosScreen; // screen-space at mousedown
            private bool _pendingClick;          // true until movement exceeds threshold
            private int _activeRowControl;       // control id that owns the mouse (stable)

            private const string DragHandleGlyph = "⋮⋮"; // vertical grip

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

                if (_dragNode != null && _hoverNode != null && Time.realtimeSinceStartup - _hoverStart >= AutoExpandDelay)
                {
                    if (_hoverNode.Children.Count > 0 && !_hoverNode.Expanded)
                        _hoverNode.Expanded = true;
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
                    GUILayout.MinWidth(480), GUILayout.MinHeight(320)
                );

                if (_detailNode != null)
                {
                    _detailRect = GUILayout.Window(
                        _detailWinId, _detailRect, DrawDetailWindow,
                        $"Details — {_detailNode.Name}",
                        GUILayout.MinWidth(340), GUILayout.MinHeight(220)
                    );
                }

                if (_showAddDialog && _addTarget != null)
                {
                    _addRect = GUILayout.Window(
                        _addWinId, _addRect, DrawAddDialogWindow,
                        $"Add subitem to '{_addTarget.Name}'",
                        GUILayout.MinWidth(300), GUILayout.MinHeight(120)
                    );
                }

                // Draw drag visuals ABOVE all windows
                DrawDragVisuals();

                // finalize drop after windows processed (ensures last valid target was set this frame)
                HandleGlobalMouseUp();
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
                GUILayout.Label("Drag by the ⋮⋮ handle. TOP = before, BOTTOM = after, MIDDLE = as child. Double-click label opens details. ⊕ adds a child. Only ± toggles.", HighLogic.Skin.label);

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

                // collect rects to union into a full-row hit box
                Rect handleRect = default, foldRect = default, labelRect = default, addRect = default;

                GUILayout.BeginHorizontal();

                // Indent
                GUILayout.Space(12 + 18 * depth);

                // DRAG HANDLE (label so it doesn't eat MouseDown)
                GUILayout.Label(DragHandleGlyph, _dragHandleStyle, GUILayout.Width(18f));
                handleRect = GUILayoutUtility.GetLastRect();

                // Fold (only this toggles expand/collapse now)
                if (node.Children.Count == 0)
                {
                    GUILayout.Label("·", GUILayout.Width(24));
                    foldRect = GUILayoutUtility.GetLastRect();
                }
                else
                {
                    string sign = node.Expanded ? "−" : "+";
                    if (GUILayout.Button(sign, GUILayout.Width(24)))
                        node.Expanded = !node.Expanded;
                    foldRect = GUILayoutUtility.GetLastRect();
                }

                // LABEL (no drag start; just selection / double-click)
                var style = (_selectedNode == node) ? _selectedLabel : _labelLikeButton;
                GUILayout.Label(node.Name, style, GUILayout.ExpandWidth(true));
                labelRect = GUILayoutUtility.GetLastRect();

                // Add child
                if (GUILayout.Button("⊕", GUILayout.Width(26)))
                    OpenAddDialog(node);
                addRect = GUILayoutUtility.GetLastRect();

                GUILayout.EndHorizontal();

                // Full row rect (GUI coords → for completeness)
                Rect rowRect = UnionRects(UnionRects(UnionRects(handleRect, foldRect), labelRect), addRect);

                // Convert to SCREEN SPACE for hit testing (scroll-safe)
                Rect rowScreenRect = GUIToScreenRect(rowRect);
                Rect labelScreenRect = GUIToScreenRect(labelRect);
                Rect handleScreenRect = GUIToScreenRect(handleRect);

                HandleRowMouse(node,
                    rowRect, handleRect, labelRect,     // GUI-space rects (for stable ID)
                    rowScreenRect, handleScreenRect, labelScreenRect); // screen-space

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

            // Mouse logic per row (stable control IDs + screen-space hit testing) with HANDLE-ONLY drag
            private void HandleRowMouse(
                Node node,
                Rect rowGuiRect, Rect handleGuiRect, Rect labelGuiRect,     // GUI-space (for stable ID)
                Rect rowScreenRect, Rect handleScreenRect, Rect labelScreenRect // screen-space (for hit testing)
            )
            {
                var e = Event.current;

                // STABLE ID for this row: tie to the node.Id AND the HANDLE GUI rect
                int id = GUIUtility.GetControlID(node.Id, FocusType.Passive, handleGuiRect);

                // Use SCREEN-SPACE mouse for hit testing
                Vector2 mouseScreen = GUIUtility.GUIToScreenPoint(e.mousePosition);

                switch (e.rawType)
                {
                    case EventType.MouseDown:
                        // Start capture ONLY if click on the HANDLE (not label)
                        if (e.button == 0 && handleScreenRect.Contains(mouseScreen))
                        {
                            GUIUtility.hotControl = id;
                            _activeRowControl = id;
                            _mouseDownPosScreen = mouseScreen;
                            _pendingClick = true;
                            _dragNode = null;
                            _dragTarget = null;
                            _dragKind = DropKind.None;
                            _hoverNode = null;
                            _hoverStart = 0f;
                            GUI.FocusWindow(_treeWinId);
                            e.Use();
                        }
                        break;

                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == _activeRowControl && _activeRowControl != 0)
                        {
                            if (_pendingClick && (mouseScreen - _mouseDownPosScreen).sqrMagnitude > DragThresholdSqr)
                            {
                                _dragNode = node; // start actual drag
                                _pendingClick = false;
                            }

                            if (_dragNode != null)
                            {
                                if (rowScreenRect.Contains(mouseScreen) && !IsSameOrDescendant(node, _dragNode))
                                {
                                    _dragTarget = node;
                                    _hoverNode = node;
                                    if (_hoverStart == 0f) _hoverStart = Time.realtimeSinceStartup;

                                    float localY = mouseScreen.y - rowScreenRect.yMin;
                                    float h = rowScreenRect.height;
                                    float top = h * 0.35f;    // bigger “before/after” zones for easier one-row moves
                                    float bottom = h * 0.65f;

                                    if (localY < top) _dragKind = DropKind.Before;
                                    else if (localY > bottom) _dragKind = DropKind.After;
                                    else _dragKind = DropKind.AsChild;
                                }
                            }
                            e.Use();
                        }
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == _activeRowControl && _activeRowControl != 0 && e.button == 0)
                        {
                            // We purposely don't ApplyDrop here; global handler at end of OnGUI does it.

                            // release capture; global handler will finish state cleanup
                            GUIUtility.hotControl = 0;
                            _activeRowControl = 0;
                            _pendingClick = false;
                            e.Use();
                        }
                        else if (e.button == 0 && labelScreenRect.Contains(mouseScreen))
                        {
                            // LABEL click behavior: select / double-click open details (no drag capture)
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
                        break;
                }
            }

            // Global mouse-up (applies last valid target even if cursor is off a row)
            private void HandleGlobalMouseUp()
            {
                var e = Event.current;
                if (e.rawType == EventType.MouseUp && e.button == 0)
                {
                    if (_dragNode != null)
                    {
                        if (_dragTarget != null && _dragKind != DropKind.None)
                        {
                            ApplyDrop(_dragNode, _dragTarget, _dragKind);
                        }

                        // Clear drag state
                        _dragNode = null;
                        _dragTarget = null;
                        _dragKind = DropKind.None;
                        _hoverNode = null;
                        _hoverStart = 0f;
                    }

                    // release any capture
                    if (_activeRowControl != 0 && GUIUtility.hotControl == _activeRowControl)
                        GUIUtility.hotControl = 0;
                    _activeRowControl = 0;
                    _pendingClick = false;
                }
            }

            private void ApplyDrop(Node moving, Node target, DropKind kind)
            {
                if (moving == null || target == null) return;
                if (IsSameOrDescendant(target, moving)) return;

                // remove from old
                var oldList = (moving.Parent == null) ? _roots : moving.Parent.Children;
                int oldIdx = oldList.IndexOf(moving);
                if (oldIdx >= 0) oldList.RemoveAt(oldIdx);

                if (kind == DropKind.AsChild)
                {
                    target.Children.Add(moving);
                    moving.Parent = target;
                    target.Expanded = true;
                    ScreenMessages.PostScreenMessage($"Moved '{moving.Name}' under '{target.Name}'.", 2f, ScreenMessageStyle.UPPER_LEFT);
                    return;
                }

                var newList = (target.Parent == null) ? _roots : target.Parent.Children;
                int tIdx = newList.IndexOf(target);
                int insertAt = (kind == DropKind.Before) ? tIdx : tIdx + 1;
                newList.Insert(Mathf.Clamp(insertAt, 0, newList.Count), moving);
                moving.Parent = target.Parent;

                string pos = (kind == DropKind.Before) ? "before" : "after";
                ScreenMessages.PostScreenMessage($"Moved '{moving.Name}' {pos} '{target.Name}'.", 2f, ScreenMessageStyle.UPPER_LEFT);
            }

            // Drag visuals drawn *above* all windows (screen-space)
            private void DrawDragVisuals()
            {
                if (_dragNode == null) return;

                int oldDepth = GUI.depth;
                GUI.depth = -10000; // draw over all IMGUI windows

                // Screen-space mouse
                Vector2 mp = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);

                if (_dragTarget != null && _dragKind != DropKind.None)
                {
                    if (_dragKind == DropKind.AsChild)
                    {
                        var r = new Rect(mp.x - 40, mp.y - 10, 80, 20);
                        DrawFill(r, _dropFillColor);
                        DrawOutline(r, _dropLineColor, 2f);
                    }
                    else
                    {
                        float y = mp.y;
                        DrawLine(new Vector2(20, y), new Vector2(Screen.width - 20, y), _dropLineColor, 3f);
                    }
                }

                // Drag ghost
                var p = mp + new Vector2(12, 12);
                var size = HighLogic.Skin.button.CalcSize(new GUIContent("⤴ " + _dragNode.Name));
                GUI.Box(new Rect(p, size + new Vector2(10, 4)), "⤴ " + _dragNode.Name, HighLogic.Skin.button);

                GUI.depth = oldDepth;
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

            // ---------- Helpers ----------
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

            private bool IsSameOrDescendant(Node candidate, Node subject)
            {
                return candidate == subject || IsDescendant(subject, candidate);
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

            // Convert current GUI group's rect to SCREEN SPACE (handles ScrollView offsets)
            private Rect GUIToScreenRect(Rect r)
            {
                Vector2 min = GUIUtility.GUIToScreenPoint(new Vector2(r.xMin, r.yMin));
                Vector2 max = GUIUtility.GUIToScreenPoint(new Vector2(r.xMax, r.yMax));
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }

            // Drawing helpers (screen-space)
            private void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
            {
                var saved = GUI.color;
                GUI.color = color;

                float len = Vector2.Distance(a, b);
                float angle = Mathf.Rad2Deg * Mathf.Atan2(b.y - a.y, b.x - a.x);

                Matrix4x4 m = GUI.matrix;
                GUIUtility.RotateAroundPivot(angle, a);
                GUI.DrawTexture(new Rect(a.x, a.y - thickness / 2f, len, thickness), Texture2D.whiteTexture);
                GUI.matrix = m;

                GUI.color = saved;
            }

            private void DrawOutline(Rect r, Color color, float thickness)
            {
                DrawLine(new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), color, thickness);
                DrawLine(new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), color, thickness);
                DrawLine(new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax), color, thickness);
                DrawLine(new Vector2(r.xMin, r.yMax), new Vector2(r.xMin, r.yMin), color, thickness);
            }

            private void DrawFill(Rect r, Color color)
            {
                var saved = GUI.color; GUI.color = color;
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = saved;
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