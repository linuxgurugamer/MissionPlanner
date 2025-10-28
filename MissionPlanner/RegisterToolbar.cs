using System;
using ToolbarControl_NS;
using UnityEngine;


namespace MissionPlanner
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        public static KSP_Log.Log Log;

        void Start()
        {
            //ToolbarControl.RegisterMod(TrimPlusSettings.MODID, TrimPlusSettings.MODNAME);
            Log = new KSP_Log.Log("MissionPlanner"
#if DEBUG
                , KSP_Log.Log.LEVEL.DETAIL
#endif
                    );

        }
#if false
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
            {
                TrimPlus.myLabelStyle = new GUIStyle(GUI.skin.label);
                TrimPlus.myLabelStyle.fontSize = GlobalConfig.FontSize;
                TrimPlus.myLabelStyle.richText = true;
            }
        }
#endif
    }
}