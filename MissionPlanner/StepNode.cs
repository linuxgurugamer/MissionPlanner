using LibNoise.Modifiers;
using System;
using System.Collections.Generic;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
#if false
    public enum StepTypezz
    {
#if false
        toggle,
        intLessThanOrEqual,
        intGreaterThanOrEqual,
        floatLessThanOrEqual,
        floatGreaterThanOrEqual,
        intRange,
        floatRange,
        crewCount,
        part,
        resource,
#endif
        toggle,
        number,
        range,
        crewCount,
        partID,
        resource,

        SAS,
        RCS,
        Batteries,
        Communication,
        SolarPanels,
        FuelCells,
        Radiators,
        Lights,
        Parachutes,
        ControlSource,
        ReactionWheels,
        Engines,
        Flags, // bool or cnt only
    }
#endif


    [Serializable]
    public class Step
    {
        public string title = "New Step";
        public string descr = "";

        public bool completed = false;
        public bool locked = false;

        public CriterionType stepType = CriterionType.none;
        public ChecklistItem checklistItem = null;

        public bool toggle = false;
        public bool initialToggleValue = false;

        public float minFloatRange = 0f;
        public float maxFloatRange = 1f;

        public float number = 100;

        public string traitName = "";

        public string resourceName = "";
        public float resourceAmount = 0f;
        public float resourceCapacity = 0f;

        public float batteryCapacity = 0f;
        public double antennaPower = 0f;
        public float solarChargeRate = 0f;
        public float fuelCellChargeRate = 0f;
        public float radiatorCoolingRate = 0f;
        public int spotlights = 0;
        public int parachutes = 0;
        public int reactionWheels = 0;

        public string moduleName = "";
        public int minSASLevel = 0;

        public string bodyAsteroidVessel = "";
        public int crewCount = 0;

        // Part selection
        public string partName = "";        // internal name (AvailablePart.name)
        public string partTitle = "";       // display title (AvailablePart.title)
        public bool partOnlyAvailable = true;

        public Step()
        {
            //criterion = new Criterion();
        }

        public bool CheckCrew(out int crew)
        {
            crew = FlightGlobals.ActiveVessel.GetCrewCount();
            return crew >= crewCount;
        }

        public bool CheckPart()
        {
            return PartLookup.ShipHasPartByInternalName(partName);
        }

        public double CheckResource()
        {
            double amt = 0;
            double capacity = 0;

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (VesselResourceQuery.TryGet(EditorLogic.fetch.ship, resourceName, out amt, out capacity))
                    Log.Info($"Editor {resourceName}: {amt}/{capacity}");
            }
            else
            {
                if (VesselResourceQuery.TryGet(FlightGlobals.ActiveVessel, resourceName, out amt, out capacity))
                    Log.Info($"{resourceName}: {amt:0.##}/{capacity:0.##}");
                else
                    Log.Info("No {resourceName} on this vessel.");

            }
            return amt;
        }



        public ConfigNode ToConfigNode()
        {
            var n = new ConfigNode("STEP");
            n.AddValue("title", title ?? "");
            n.AddValue("descr", descr ?? "");
            n.AddValue("completed", completed);
            n.AddValue("locked", locked);
            n.AddValue("stepType", stepType.ToString());

            n.AddValue("toggle", toggle);
            n.AddValue("initialToggleValue", initialToggleValue);

            n.AddValue("crewCount", crewCount);

            n.AddValue("minFloatRange", minFloatRange);
            n.AddValue("maxFloatRange", maxFloatRange);

            n.AddValue("number", number);

            n.AddValue("partName", partName ?? "");
            n.AddValue("partTitle", partTitle ?? "");
            n.AddValue("partOnlyAvailable", partOnlyAvailable);

            return n;
        }

        public static Step FromConfigNode(ConfigNode n)
        {
            var s = new Step();
            s.title = n.GetValue("title") ?? s.title;
            s.descr = n.GetValue("descr") ?? s.descr;

            CriterionType t;
            if (Enum.TryParse(n.GetValue("stepType"), out t)) s.stepType = t;

            bool btmp;
            if (bool.TryParse(n.GetValue("completed"), out btmp)) s.completed = btmp;
            if (bool.TryParse(n.GetValue("locked"), out btmp)) s.locked = btmp;
            if (bool.TryParse(n.GetValue("toggle"), out btmp)) s.toggle = btmp;
            if (bool.TryParse(n.GetValue("initialToggleValue"), out btmp)) s.initialToggleValue = btmp;

            int itmp;
            if (int.TryParse(n.GetValue("crewCount"), out itmp)) s.crewCount = itmp;

            float ftmp;
            if (float.TryParse(n.GetValue("number"), out ftmp)) s.number = ftmp;

            if (float.TryParse(n.GetValue("minFloatRange"), out ftmp)) s.minFloatRange = ftmp;
            if (float.TryParse(n.GetValue("maxFloatRange"), out ftmp)) s.maxFloatRange = ftmp;

            s.partName = n.GetValue("partName") ?? "";
            s.partTitle = n.GetValue("partTitle") ?? "";
            if (bool.TryParse(n.GetValue("partOnlyAvailable"), out btmp)) s.partOnlyAvailable = btmp;

            return s;
        }
    }

    [Serializable]
    public class StepNode
    {
        private static int _nextId = 1;
        public readonly int Id;
        public Step data = new Step();
        public bool Expanded = true;
        public StepNode Parent = null;
        public readonly List<StepNode> Children = new List<StepNode>();

        public StepNode() { Id = _nextId++; }

        public StepNode AddChild(Step childStep = null)
        {
            var n = new StepNode
            {
                data = childStep ?? new Step(),
                Parent = this
            };
            Children.Add(n);

            return n;
        }

        public ConfigNode ToConfigNodeRecursive()
        {
            var n = new ConfigNode("NODE");
            n.AddValue("title", data.title ?? "");
            n.AddValue("expanded", Expanded);
            n.AddNode(data.ToConfigNode());
            foreach (var c in Children) n.AddNode(c.ToConfigNodeRecursive());
            return n;
        }

        public static StepNode FromConfigNodeRecursive(ConfigNode n)
        {
            var node = new StepNode();
            node.data.title = n.GetValue("title") ?? node.data.title;
            bool ex;
            if (bool.TryParse(n.GetValue("expanded"), out ex)) node.Expanded = ex;

            var stepNode = n.GetNode("STEP");
            if (stepNode != null) node.data = Step.FromConfigNode(stepNode);

            foreach (var cn in n.GetNodes("NODE"))
            {
                var child = FromConfigNodeRecursive(cn);
                child.Parent = node;
                node.Children.Add(child);
            }
            return node;
        }
    }
}
