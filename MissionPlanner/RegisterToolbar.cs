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
        }
    }
}