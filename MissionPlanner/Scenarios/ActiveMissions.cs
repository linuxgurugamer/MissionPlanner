using SpaceTuxUtility;
using System.Collections.Generic;

using static MissionPlanner.RegisterToolbar;


namespace MissionPlanner.Scenarios
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new[]
         {GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.EDITOR})]
    internal class ActiveMissions : ScenarioModule
    {

        //static Dictionary<string, ConfigNode> activeMissionsCfgNodes = new Dictionary<string, ConfigNode>();
        static Dictionary<string, Mission> activeMissions = new Dictionary<string, Mission>();
        public void Start() { }

        public override void OnSave(ConfigNode node)
        {
            var current = HierarchicalStepsWindow.MakeConfigNodes();
            ConfigNode missionCfgNode = new ConfigNode("ACTIVE_MISSIONS");

            foreach (Mission m in activeMissions.Values)
            {
                var root = new ConfigNode(HierarchicalStepsWindow.SAVE_ROOT_NODE);
                root.AddValue("MissionName", m.missionName);
                root.AddValue("MissionSummary", m.missionSummary ?? "");
                root.AddValue("SimpleChecklist", m.simpleChecklist);

                root.AddValue("currentView", (int)m.currentView);

                var list = new ConfigNode(HierarchicalStepsWindow.SAVE_LIST_NODE);
                root.AddNode(list);

                foreach (var r in m.roots)
                    list.AddNode(StepNode.ToConfigNodeRecursive(r, false));
                missionCfgNode.AddNode(root);
            }

            node.AddNode(missionCfgNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            var missions = node.GetNodes("ACTIVE_MISSIONS");
            foreach (var m in missions)
            {
                var mCfg = m.GetNode("MISSION_PLANNER");
                string missionName = "";
                missionName = mCfg.SafeLoad("MissionName", missionName);
                if (!activeMissions.ContainsKey(missionName))
                {
                    activeMissions.Add(missionName, LoadMission(mCfg));
                }
                else
                    Log.Error("Duplicate mission loaded");
            }
        }

        public static List<HierarchicalStepsWindow.MissionFileInfo> GetActiveMissionsList()
        {
            var list = new List<HierarchicalStepsWindow.MissionFileInfo>();
            foreach (Mission n in activeMissions.Values)
            {
                string save = n.missionName;
                HierarchicalStepsWindow.MissionFileInfo mfi = new HierarchicalStepsWindow.MissionFileInfo
                {
                    FullPath = "",
                    SaveName = save,
                    MissionName = n.missionName,
                    stock = false,
                    active = true
                };
                list.Add(mfi);
            }
            return list;
        }

        public static Mission GetMission(string mission)
        {
            if (activeMissions.ContainsKey(mission))
                return activeMissions[mission];
            return null;
        }

        public static void SaveMission(Mission mission)
        {
            activeMissions[mission.missionName] = mission;
        }

        public Mission LoadMission(ConfigNode root)
        {
            Mission mission = new Mission();
            mission.missionName = root.SafeLoad("MissionName", HierarchicalStepsWindow.mission.missionName);
            mission.missionSummary = root.SafeLoad("MissionSummary", "");
            mission.simpleChecklist = root.SafeLoad("SimpleChecklist", false);
            int i = root.SafeLoad("currentView", 0);

            mission.currentView = (View)i;

            var list = root.GetNode(HierarchicalStepsWindow.SAVE_LIST_NODE);
            var newRoots = new List<StepNode>();
            foreach (var n in list.GetNodes("NODE"))
            {
                StepNode node = StepNode.FromConfigNodeRecursive(n);

                node.Parent = null;
                newRoots.Add(node);
            }
            mission.roots.AddRange(newRoots);
            HierarchicalStepsWindow.ReparentAll(mission);
            return mission;
        }
    }
}
