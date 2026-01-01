using ClickThroughFix;
using System;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        internal static bool textDialogOpen = false;

        /// <summary>
        /// Shows a modal dialog with a message + text entry field.
        /// onOk receives the entered text (trim/validation is up to the caller).
        /// </summary>
        public static void TextEntryDialogShow(
            string title,
            string message,
            Action<string> onOk,
            Action onCancel = null,
            string initialText = "",
            string okText = "OK",
            string cancelText = "Cancel",
            bool lockControls = true,
            bool multiLine = false,
            int maxChars = 256)
        {
            TextEntryDialog.title = title ?? "Enter Text";
            TextEntryDialog.message = message ?? "";
            TextEntryDialog.okText = okText ?? "OK";
            TextEntryDialog.cancelText = cancelText ?? "Cancel";
            TextEntryDialog.onOk = onOk;
            TextEntryDialog.onCancel = onCancel;
            TextEntryDialog.text = initialText ?? "";
            TextEntryDialog.multiLine = multiLine;
            TextEntryDialog.maxChars = Mathf.Max(1, maxChars);

            textDialogOpen = true;

            // Center (safe even before first draw)
            TextEntryDialog.rect.x = (Screen.width - TextEntryDialog.rect.width) * 0.5f;
            TextEntryDialog.rect.y = (Screen.height - TextEntryDialog.rect.height) * 0.5f;

            // Optional control lock (enable if desired)
            //if (lockControls)
            //    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, TextEntryDialog.LockId);

            var go = new GameObject("MissionPlanner_TextEntryDialog");
            go.AddComponent<TextEntryDialog>();
        }
    }

    public class TextEntryDialog : MonoBehaviour
    {
        internal static string title;
        internal static string message;
        internal static string okText;
        internal static string cancelText;

        internal static Action<string> onOk;
        internal static Action onCancel;

        internal static string text;
        internal static bool multiLine;
        internal static int maxChars;

        internal static Rect rect = new Rect(0, 0, 460, 210);

        // Pick an ID that won't collide with your other windows
        private const int WindowId = 0x4B91A2F1;

        // Optional: locks game controls while open
        internal const string LockId = "MissionPlanner_TextEntryDialogLock";

        private const string TextControlName = "MP_TextEntry_Field";
        private bool _setInitialFocus;

        public void Start()
        {
            Log.Info("TextEntryDialog.Start");
            _setInitialFocus = true;
        }

        public void Close(bool invokeCancel = false)
        {
            HierarchicalStepsWindow.textDialogOpen = false;

            if (invokeCancel)
            {
                var a = onCancel;
                a?.Invoke();
            }

            //onOk = null;
            //onCancel = null;

            //InputLockManager.RemoveControlLock(LockId);

            Log.Info("TextEntryDialog.Close");
            Destroy(gameObject);
        }

        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;

            // Full-screen modal dimmer
            var oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = oldColor;

            int oldDepth = GUI.depth;
            GUI.depth = 10;

            rect = ClickThruBlocker.GUILayoutWindow(WindowId, rect, DrawWindow, title, GUILayout.ExpandHeight(false));

            GUI.depth = oldDepth;

            GUI.BringWindowToFront(WindowId);
            GUI.FocusWindow(WindowId);
        }

        private void DrawWindow(int id)
        {
            HierarchicalStepsWindow.BringWindowForward(id, true);

            GUILayout.Space(6);

            if (!string.IsNullOrEmpty(message))
            {
                GUILayout.Label(message, HighLogic.Skin.label, GUILayout.ExpandHeight(false));
                GUILayout.Space(6);
            }

            // Text entry
            GUI.SetNextControlName(TextControlName);

            if (multiLine)
            {
                // A modest height; caller can adjust via rect if needed
                text = GUILayout.TextArea(text ?? "", maxChars, GUILayout.Height(70));
            }
            else
            {
                text = GUILayout.TextField(text ?? "", maxChars);
            }

            // Ensure focus goes into the textbox when the dialog first appears
            if (_setInitialFocus)
            {
                GUI.FocusControl(TextControlName);
                _setInitialFocus = false;
            }

            // Keyboard shortcuts
            var e = Event.current;
            if (e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    e.Use();
                    Close(invokeCancel: true);
                    return;
                }

                // Enter submits for single-line; for multi-line allow Return to add newlines
                if (!multiLine && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
                {
                    e.Use();
                    InvokeOkAndClose();
                    return;
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(okText, GUILayout.MinWidth(90)))
            {
                InvokeOkAndClose();
                return;
            }

            GUILayout.Space(10);

            if (GUILayout.Button(cancelText, GUILayout.MinWidth(90)))
            {
                Close(invokeCancel: true);
                return;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUI.DragWindow();
        }

        private void InvokeOkAndClose()
        {
            var value = text ?? "";
            var a = onOk;
            a?.Invoke(value);
            Close(invokeCancel: false);
        }
    }
}
