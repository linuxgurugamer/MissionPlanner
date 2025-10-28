#if false
using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissionPlanner
{
    // File: StepListWindow.cs
    // KSP1 (Unity IMGUI) – window that edits a list of Steps.
    // Each row is a Step with fields shown based on StepType.
    // Buttons: Add, Duplicate, Remove, Move Up/Down. Titles and descriptions are editable.
    // Numeric fields are entered via text fields with parsing & validation.
    //
    // Drop the compiled DLL into GameData/YourMod/Plugins.




    public enum StepType
    {
        toggle,
        intNotGreaterThan,
        intNotLessThan,
        floatNotGreaterThan,
        floatNotLessThan,
        intRange,
        floatRange
    }

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

        public int intNotGreaterThan = 100;  // (<=)
        public int intNotLessThan = 0;       // (>=)
        public float floatNotGreaterThan = 100f; // (<=)
        public float floatNotLessThan = 0f;      // (>=)
    }

    [KSPAddon(KSPAddon.Startup.FlightAndEditor, false)]
    public class StepListWindow : MonoBehaviour
    {
        private Rect _winRect = new Rect(200, 120, 820, 560);
        private int _winId;
        private bool _visible = true;
        private Vector2 _scroll;

        // Toolbar button (optional toggle)
        private ApplicationLauncherButton _appButton;
        private Texture2D _iconOn, _iconOff;
        private const string IconOnPath = "YourMod/Icons/tree_on";
        private const string IconOffPath = "YourMod/Icons/tree_off";
        private readonly ApplicationLauncher.AppScenes _scenes =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.SPH;

        // Data
        private readonly List<Step> _steps = new List<Step>();

        // UI helpers / styles
        private GUIStyle _titleField, _descrField, _smallBtn, _badge, _warnLabel;
        private void EnsureStyles()
        {
            if (_titleField == null)
            {
                _titleField = new GUIStyle(HighLogic.Skin.textField) { fontStyle = FontStyle.Bold };
            }
            if (_descrField == null)
            {
                _descrField = new GUIStyle(HighLogic.Skin.textArea) { wordWrap = true };
            }
            if (_smallBtn == null)
            {
                _smallBtn = new GUIStyle(HighLogic.Skin.button)
                {
                    fixedWidth = 26f,
                    padding = new RectOffset(2, 2, 2, 2),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }
            if (_badge == null)
            {
                _badge = new GUIStyle(HighLogic.Skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.85f, 0.9f, 1f, 1f) }
                };
            }
            if (_warnLabel == null)
            {
                _warnLabel = new GUIStyle(HighLogic.Skin.label)
                {
                    normal = { textColor = new Color(1f, 0.6f, 0.2f, 1f) },
                    wordWrap = true
                };
            }
        }

        // Keyboard toggle
        private readonly KeyCode _toggleKey = KeyCode.F9;

        public void Awake()
        {
            _winId = GetHashCode();
            LoadIconsOrFallback();
            SeedDemoData();
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

            _winRect = GUILayout.Window(
                _winId, _winRect, DrawWindow, "Steps Editor (F9 to toggle)",
                GUILayout.MinWidth(640), GUILayout.MinHeight(360)
            );
        }

        private void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 24));
            GUILayout.Space(4);

            // Toolbar
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Step", GUILayout.Width(100))) _steps.Add(new Step());
            if (GUILayout.Button("Expand All", GUILayout.Width(110))) { /* no collapses here, placeholder */ }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Total: {_steps.Count}", _badge);
            if (GUILayout.Button(_visible ? "Hide" : "Show", GUILayout.Width(70)))
            {
                _visible = !_visible; SyncToolbarState();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Each line below is a Step. Choose a StepType to reveal the relevant fields. Integers/floats are typed into the small boxes; invalid numbers keep the old value.", HighLogic.Skin.label);

            GUILayout.Space(4);
            _scroll = GUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _steps.Count; i++)
            {
                DrawStepRow(i, _steps[i]);
                GUILayout.Space(6);
            }

            GUILayout.EndScrollView();
        }

        private void DrawStepRow(int index, Step s)
        {
            GUILayout.BeginVertical(HighLogic.Skin.textArea);

            // Header row: title + controls
            GUILayout.BeginHorizontal();
            GUILayout.Label($"#{index + 1}", GUILayout.Width(36));
            GUILayout.Label("Title", GUILayout.Width(40));
            s.title = GUILayout.TextField(s.title ?? "", _titleField, GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));

            // Row controls
            if (GUILayout.Button("▲", _smallBtn)) MoveUp(index);
            if (GUILayout.Button("▼", _smallBtn)) MoveDown(index);
            if (GUILayout.Button("Dup", GUILayout.Width(40))) Duplicate(index);
            if (GUILayout.Button("✖", _smallBtn)) { _steps.RemoveAt(index); GUILayout.EndHorizontal(); GUILayout.EndVertical(); return; }
            GUILayout.EndHorizontal();

            // Description
            GUILayout.BeginHorizontal();
            GUILayout.Label("Descr", GUILayout.Width(40));
            s.descr = GUILayout.TextArea(s.descr ?? "", _descrField, GUILayout.MinHeight(40), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Step type selector (simple prev/next cycle + label)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type", GUILayout.Width(40));
            if (GUILayout.Button("◀", _smallBtn)) s.stepType = PrevType(s.stepType);
            GUILayout.Label(s.stepType.ToString(), GUILayout.Width(180));
            if (GUILayout.Button("▶", _smallBtn)) s.stepType = NextType(s.stepType);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Fields depending on type
            GUILayout.Space(2);
            switch (s.stepType)
            {
                case StepType.toggle:
                    GUILayout.BeginHorizontal();
                    s.initialToggleValue = GUILayout.Toggle(s.initialToggleValue, "Initial Value", GUILayout.Width(120));
                    GUILayout.Space(12);
                    s.toggle = GUILayout.Toggle(s.toggle, "Current Value", GUILayout.Width(120));
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
                        GUILayout.Label("Warning: minIntRange is greater than maxIntRange.", _warnLabel);
                    break;

                case StepType.floatRange:
                    FloatRangeFields(ref s.minFloatRange, ref s.maxFloatRange);
                    if (s.minFloatRange > s.maxFloatRange)
                        GUILayout.Label("Warning: minFloatRange is greater than maxFloatRange.", _warnLabel);
                    break;
            }

            GUILayout.EndVertical();
        }

        // Helpers: type cycling
        private StepType NextType(StepType t)
        {
            var vals = (StepType[])Enum.GetValues(typeof(StepType));
            int i = Array.IndexOf(vals, t);
            i = (i + 1) % vals.Length;
            return vals[i];
        }
        private StepType PrevType(StepType t)
        {
            var vals = (StepType[])Enum.GetValues(typeof(StepType));
            int i = Array.IndexOf(vals, t);
            i = (i - 1 + vals.Length) % vals.Length;
            return vals[i];
        }

        // Helpers: fields
        private void IntField(string label, ref int value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            string buf = GUILayout.TextField(value.ToString(), GUILayout.Width(100));
            if (int.TryParse(buf, out int parsed)) value = parsed;
            GUILayout.EndHorizontal();
        }

        private void FloatField(string label, ref float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(90));
            string buf = GUILayout.TextField(value.ToString("G"), GUILayout.Width(100));
            if (float.TryParse(buf, out float parsed)) value = parsed;
            GUILayout.EndHorizontal();
        }

        private void IntRangeFields(ref int min, ref int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min (int)", GUILayout.Width(90));
            string minBuf = GUILayout.TextField(min.ToString(), GUILayout.Width(100));
            GUILayout.Space(12);
            GUILayout.Label("Max (int)", GUILayout.Width(90));
            string maxBuf = GUILayout.TextField(max.ToString(), GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (int.TryParse(minBuf, out int pmin)) min = pmin;
            if (int.TryParse(maxBuf, out int pmax)) max = pmax;
        }

        private void FloatRangeFields(ref float min, ref float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min (float)", GUILayout.Width(90));
            string minBuf = GUILayout.TextField(min.ToString("G"), GUILayout.Width(100));
            GUILayout.Space(12);
            GUILayout.Label("Max (float)", GUILayout.Width(90));
            string maxBuf = GUILayout.TextField(max.ToString("G"), GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (float.TryParse(minBuf, out float pmin)) min = pmin;
            if (float.TryParse(maxBuf, out float pmax)) max = pmax;
        }

        // Helpers: list ops
        private void MoveUp(int idx)
        {
            if (idx <= 0) return;
            var t = _steps[idx];
            _steps.RemoveAt(idx);
            _steps.Insert(idx - 1, t);
        }
        private void MoveDown(int idx)
        {
            if (idx < 0 || idx >= _steps.Count - 1) return;
            var t = _steps[idx];
            _steps.RemoveAt(idx);
            _steps.Insert(idx + 1, t);
        }
        private void Duplicate(int idx)
        {
            if (idx < 0 || idx >= _steps.Count) return;
            var src = _steps[idx];
            // Shallow copy is fine because fields are value types/strings.
            var copy = new Step
            {
                title = src.title + " (copy)",
                descr = src.descr,
                stepType = src.stepType,
                toggle = src.toggle,
                initialToggleValue = src.initialToggleValue,
                minIntRange = src.minIntRange,
                maxIntRange = src.maxIntRange,
                minFloatRange = src.minFloatRange,
                maxFloatRange = src.maxFloatRange,
                intNotGreaterThan = src.intNotGreaterThan,
                intNotLessThan = src.intNotLessThan,
                floatNotGreaterThan = src.floatNotGreaterThan,
                floatNotLessThan = src.floatNotLessThan
            };
            _steps.Insert(idx + 1, copy);
        }

        // Seed a couple example rows
        private void SeedDemoData()
        {
            _steps.Clear();
            _steps.Add(new Step
            {
                title = "Enable SAS",
                descr = "Turn SAS on at launch.",
                stepType = StepType.toggle,
                initialToggleValue = true,
                toggle = true
            });
            _steps.Add(new Step
            {
                title = "Max Gs allowed",
                descr = "Abort if G-load exceeds threshold.",
                stepType = StepType.floatNotGreaterThan,
                floatNotGreaterThan = 5.5f
            });
            _steps.Add(new Step
            {
                title = "Throttle window",
                descr = "Maintain throttle between these integer bounds.",
                stepType = StepType.intRange,
                minIntRange = 10,
                maxIntRange = 75
            });
        }

        // ----- Toolbar / AppLauncher -----
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
            if (tex != null) _appButton.SetTexture(tex);
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