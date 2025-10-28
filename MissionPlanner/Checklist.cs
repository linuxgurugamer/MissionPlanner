using MissionPlanner;
using System;
using System.Collections.Generic;



namespace MissionPlanner
{

    public class Checklist
    {
        public string name = "";
        //public bool editorOnly = false;
        //public bool flightOnly = false;
        public List<ChecklistItem> items = new List<ChecklistItem>();

        public static bool cfgLoaded = false;
        public static ConfigNode cfg = null;

        public void Clear()
        {
            items.Clear();
        }
    }
}
