using ToolbarControl_NS;
using UnityEngine;

namespace MissionPlanner
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        public static KSP_Log.Log Log;

        static public GUIContent upContent = new GUIContent("▲", "Move up");
        static public GUIContent downContent = new GUIContent("▼", "Move down");
        static public GUIContent promoteContent = new GUIContent("⤴", "Promote");
        static public GUIContent demoteContent = new GUIContent("⤵", "Demote");
        static public GUIContent addContent = new GUIContent("<B>+</B>", "Add child");
        static public GUIContent deleteContent = new GUIContent("✖", "Delete");
        static public GUIContent moveContent;
        static public GUIContent duplicateContent;

        static public GUIStyle buttonIconStyle;

        void Start()
        {
            ToolbarControl.RegisterMod(HierarchicalStepsWindow.MODID, HierarchicalStepsWindow.MODNAME);
            Log = new KSP_Log.Log("MissionPlanner"
#if DEBUG
                , KSP_Log.Log.LEVEL.DETAIL
#endif
                    );

        }
        bool initted = false;
        void OnGUI()
        {
            if (!initted)
            {
                InitStyle();
                initted = true;
            }
        }
        internal static void InitStyle()
        {
            upContent = new GUIContent("▲", "Move up");
            downContent = new GUIContent("▼", "Move down");
            promoteContent = new GUIContent("⤴", "Promote");
            demoteContent = new GUIContent("⤵", "Demote");
            addContent = new GUIContent("<B>+</B>", "Add child");
            deleteContent = new GUIContent("✖", "Delete");

            HierarchicalStepsWindow.moveIcon = GameDatabase.Instance?.GetTexture(HierarchicalStepsWindow.MovePath, false);
            HierarchicalStepsWindow.duplicateIcon = GameDatabase.Instance?.GetTexture(HierarchicalStepsWindow.DuplicatePath, false);
            moveContent = new GUIContent(HierarchicalStepsWindow.moveIcon, "Move…");
            duplicateContent = new GUIContent(HierarchicalStepsWindow.duplicateIcon, "Duplicate");

            buttonIconStyle = new GUIStyle(GUI.skin.button);
            buttonIconStyle.normal.background = null;
            buttonIconStyle.hover.background = null;
            buttonIconStyle.active.background = null;
            buttonIconStyle.border = new RectOffset(0, 0, 0, 0);
            buttonIconStyle.margin = new RectOffset(0, 0, 0, 0);
            buttonIconStyle.padding = new RectOffset(0, 0, 5, 0);

        }
    }
}