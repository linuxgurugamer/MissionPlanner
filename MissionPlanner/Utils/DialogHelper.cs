using ClickThroughFix;
using System;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        internal static bool dialogOpen = false;
        public static void YesNoDialogShow(
                string title,
                string message,
                Action onYes,
                Action onNo = null,
                string yesText = "Yes",
                string noText = "No",
                bool lockControls = true)
        {
            YesNoDialog.title = title ?? "Confirm";
            YesNoDialog.message = message ?? "";
            YesNoDialog.yesText = yesText ?? "Yes";
            YesNoDialog.noText = noText ?? "No";
            YesNoDialog.onYes = onYes;
            YesNoDialog.onNo = onNo;

            dialogOpen = true;

            // Center (safe even before first draw)
            YesNoDialog.rect.x = (Screen.width - YesNoDialog.rect.width) * 0.5f;
            YesNoDialog.rect.y = (Screen.height - YesNoDialog.rect.height) * 0.5f;

            //if (lockControls)
            //    InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, LockId);
            GameObject go = new GameObject();
            var windowDebug = go.AddComponent<YesNoDialog>();
        }
    }



    public class YesNoDialog : MonoBehaviour
    {
        //internal static bool open;
        internal static string title;
        internal static string message;
        internal static string yesText;
        internal static string noText;
        internal static Action onYes;
        internal static Action onNo;

        internal static Rect rect = new Rect(0, 0, 420, 160);

        // Pick an ID that won't collide with your other windows
        private const int WindowId = 0x5A17C0DE;

        // Optional: locks game controls while open
        private const string LockId = "MyMod_YesNoDialogLock";

        public void Start()
        {
            Log.Info("KSPIMGUIYesNoDialog.Start");
        }

        public void Close()
        {
            HierarchicalStepsWindow.dialogOpen = false;
            onYes = null;
            onNo = null;
            //InputLockManager.RemoveControlLock(LockId);
            Log.Info("KSPIMGUIYesNoDialog.Close");

            Destroy(this);
        }

        /// <summary>
        /// Call this from your MonoBehaviour.OnGUI() AFTER drawing your other windows.
        /// </summary>
        public void OnGUI()
        {
            //if (!open) return;

            GUI.skin = HighLogic.Skin;

            // Draw a full-screen "modal" dimmer behind the dialog (still IMGUI, so it blocks clicks)
            var oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
            GUI.color = oldColor;

            int oldDepth = GUI.depth;
            GUI.depth = 10;

            // Draw the dialog itself last = on top
            rect = ClickThruBlocker.GUILayoutWindow(WindowId, rect, DrawWindow, title, GUILayout.ExpandHeight(false));
            //rect = GUILayout.Window(WindowId, rect, DrawWindow, title, GUILayout.ExpandHeight(false));

            GUI.depth = oldDepth;

            // Ensure keyboard focus stays on this window

            GUI.BringWindowToFront(WindowId);
            GUI.FocusWindow(WindowId);
            //HierarchicalStepsWindow.newWindow = true;
            //HierarchicalStepsWindow.BringWindowForward(WindowId, true);
            //GUI.BringWindowToFront(WindowId);

        }

        private void DrawWindow(int id)
        {
            //GUI.BringWindowToFront(id);
            //GUI.FocusWindow(id);
            //HierarchicalStepsWindow.newWindow = true;
            HierarchicalStepsWindow.BringWindowForward(id, true);
            //GUI.BringWindowToFront(id);

            GUILayout.Space(6);
            GUILayout.Label(message, HighLogic.Skin.label, GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(yesText, GUILayout.MinWidth(90)))
            {
                var a = onYes;
                a?.Invoke();
                Close();
            }

            GUILayout.Space(10);

            if (GUILayout.Button(noText, GUILayout.MinWidth(90)))
            {
                var a = onNo;
                a?.Invoke();
                Close();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUI.DragWindow();
        }
    }
}