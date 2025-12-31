using System.Collections.Generic;

namespace MissionPlanner
{
    public enum View { Tiny = 0, Compact = 1, Full = 2 };
    public class Mission
    {
        // Mission meta
        public string missionName = "";
        public string missionSummary = ""; // Mission summary
        public bool simpleChecklist = false;
        public View currentView = View.Full;

        public bool missionActive = false;

        // Data
        public List<StepNode> roots = new List<StepNode>();

    }
}
