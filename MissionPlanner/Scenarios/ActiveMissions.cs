using SpaceTuxUtility;
using System.Collections.Generic;
using System.IO;
using static MissionPlanner.RegisterToolbar;


namespace MissionPlanner.Scenarios
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new[]
         {GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.EDITOR})]
    internal class ActiveMissions : ScenarioModule
    {
        const string CURRENT_MISSION = "CURRENT_MISSION";

        static Dictionary<string, Mission> activeMissions = new Dictionary<string, Mission>();
        public void Start() { }

        public override void OnSave(ConfigNode node)
        {
            ConfigNode currentMission = new ConfigNode(CURRENT_MISSION);

            currentMission.AddValue("missionName", HierarchicalStepsWindow.mission.missionName);
            currentMission.AddValue("missionActive", HierarchicalStepsWindow.mission.missionActive);
            node.AddNode(currentMission);

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
            string currentMission = "";
            bool missionActive = false;

            var current = node.GetNode(CURRENT_MISSION);
            if (current!=null)
            {
                currentMission = current.SafeLoad("missionName", currentMission);
                missionActive = current.SafeLoad("missionActive", missionActive);
                Log.Info($"ActiveMission.OnLoad, currentMission: {currentMission}");
            }


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
            if (missionActive)
                HierarchicalStepsWindow.mission = GetMission(currentMission);
            else
            {
                string save = HierarchicalStepsWindow. GetCurrentSaveName();

                var path = HierarchicalStepsWindow.GetSaveFileAbsolute(save, currentMission);
                var dir = HierarchicalStepsWindow.GetMissionDirectoryAbsolute();
                Log.Info("Fullpath: " + path);
                HierarchicalStepsWindow. TryLoadFromDisk(path, false);
                

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
            {
                Log.Info("ActiveMission.GetMission, mission found");
                return activeMissions[mission];
            }
            return new Mission();
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
