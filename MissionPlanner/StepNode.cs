using SpaceTuxUtility;
using System;
using System.Collections.Generic;

namespace MissionPlanner
{

    public enum Direction { StartToEnd, EndToStart };
    public class ResInfo
    {
        public string resourceName = "";
        public float resourceAmount = 0f;
        public float resourceCapacity = 0f;
        public bool locked = false;

        public float startingAmount { get { return resourceAmount; } set { resourceAmount = value; } }
        public float endingAmount { get { return resourceCapacity; } set { resourceCapacity = value; } }
        public Direction direction = Direction.StartToEnd;
        public ResInfo() { }
        public ResInfo(string resourceName) { this.resourceName = resourceName; }
        public ResInfo(ResInfo r)
        {
            this.resourceName = r.resourceName;
            this.resourceAmount = r.resourceAmount;
            this.resourceCapacity = r.resourceCapacity;
            this.locked = r.locked;
            this.direction = r.direction;
        }
    }

    public enum StepStatus { Inactive, Active, Completed};

    [Serializable]
    public class Step
    {
        public bool locked { get { return _locked | HierarchicalStepsWindow.missionRunnerActive; } set { _locked = value; } }

        public string title = "New Step";
        public string descr = "";

        public bool completed = false;
        public bool _locked = false;

        public CriterionType stepType = CriterionType.ChecklistItem;
        public Maneuver maneuver = Maneuver.None;
        public string maneuverBody = "";

        public bool toggle = false;
        public bool initialToggleValue = false;

        public string traitName = "";

        public List<ResInfo> resourceList = new List<ResInfo>();
        public List<ResInfo> engineResourceList = new List<ResInfo>();
        public List<ResInfo> rcsResourceList = new List<ResInfo>();

        public float batteryCapacity = 0f;
        public double chargeRateTotal = 0f;
        public double antennaPower = 0f;
        public float solarChargeRate = 0f;
        public SolarUtils.Tracking solarPaneltracking = SolarUtils.Tracking.both;
        public float fuelCellChargeRate = 0f;
        public float generatorChargeRate = 0f;
        public float radiatorCoolingRate = 0f;
        public int spotlights = 0;
        public int parachutes = 0;
        public int reactionWheels = 0;
        public bool torquePitchRollYawEqual = true;
        public double torquePitch = 0;
        public double torqueYaw = 0;
        public double torqueRoll = 0;

        public double ap = 0;
        public double pe = 0;
        public float marginOfError = 10f; // percentage
        public bool peMatchesAp = true;

        public string moduleName = "";
        public int minSASLevel = 0;

        public string flagBody = "";
        public string destBody = "";
        public string destBiome = "";
        public string biome = "";
        public DestinationType destType = 0;
        public string destAsteroid = "";
        public string destVessel = "";

        public string trackedVessel = "";
        public int experience = 0;
        public int reputation = 0;
        public int funding = 0;
        public float science = 0f;

        public StepStatus stepStatus = StepStatus.Inactive;
        public bool IsStepActive {  get {  return stepStatus == StepStatus.Active; } }

        public bool stepCompleted = false;

        public Guid vesselGuid = Guid.Empty;

        public string vabCategory = "";

        public bool requiresLanding = false;
        public bool requiresDocking = false;
        public bool hasDocked = false;

        public int flagCnt = 0;
        public int crewCount = 0;

        public int engineQty = 0;
        public string engineType = "";
        public bool engineGimbaled = false;
        public string rcsType = "";

        public int controlSourceQty = 0;
        public int dockingPortQty = 0;
        public int drillQty = 0;
        public double deltaV = 0d;
        public float TWR = 0f;
        public int stage = 0;
        public bool includeDockingPort = false;
        public bool asl = true; // at sea level

        // Part selection
        public string partName = "";        // internal name (AvailablePart.name)
        public string partTitle = "";       // display title (AvailablePart.title)
        public bool partOnlyAvailable = true;
        public PartGroup partGroup;

        public Step()
        {
        }

        public Step(Step step)
        {
            title = step.title;
            descr = step.descr;

            completed = step.completed;
            _locked = step._locked;

            stepType = step.stepType;
            maneuver = step.maneuver;
            maneuverBody = step.maneuverBody;

            toggle = step.toggle;
            initialToggleValue = step.initialToggleValue;

            traitName = step.traitName;

            resourceList = new List<ResInfo>();
            engineResourceList = new List<ResInfo>();
            rcsResourceList = new List<ResInfo>();

            foreach (var r in step.resourceList)
                resourceList.Add(new ResInfo(r));
            foreach (var r in step.engineResourceList)
                engineResourceList.Add(new ResInfo(r));
            foreach (var r in step.rcsResourceList)
                rcsResourceList.Add(new ResInfo(r));

            batteryCapacity = step.batteryCapacity;
            chargeRateTotal = step.chargeRateTotal;
            antennaPower = step.antennaPower;
            solarChargeRate = step.solarChargeRate;
            solarPaneltracking = step.solarPaneltracking;
            fuelCellChargeRate = step.fuelCellChargeRate;
            generatorChargeRate = step.generatorChargeRate;
            radiatorCoolingRate = step.radiatorCoolingRate;
            spotlights = step.spotlights;
            parachutes = step.parachutes;
            reactionWheels = step.reactionWheels;
            torquePitchRollYawEqual = step.torquePitchRollYawEqual;
            torquePitch = step.torquePitch;
            torqueYaw = step.torqueYaw;
            torqueRoll = step.torqueRoll;

            ap = step.ap;
            pe = step.pe;
            marginOfError = step.marginOfError;
            peMatchesAp = step.peMatchesAp;

            moduleName = step.moduleName;
            minSASLevel = step.minSASLevel;

            flagBody = step.flagBody;
            destBody = step.destBody;
            destBiome = step.destBiome;
            biome = step.biome;
            destType = step.destType;
            destAsteroid = step.destAsteroid;
            destVessel = step.destVessel;
            trackedVessel = step.trackedVessel;
            experience = step.experience;
            reputation = step.reputation;
            funding = step.funding;
            science = step.science;
            stepCompleted = step.stepCompleted;
            stepStatus = step.stepStatus;

            vesselGuid = step.vesselGuid;

            vabCategory = step.vabCategory;

            requiresLanding = step.requiresLanding;
            requiresDocking = step.requiresDocking;
            hasDocked = step.hasDocked;

            flagCnt = step.flagCnt;
            crewCount = step.crewCount;

            engineQty = step.engineQty;
            engineType = step.engineType;
            engineGimbaled = step.engineGimbaled;
            rcsType = step.rcsType;

            controlSourceQty = step.controlSourceQty;
            dockingPortQty = step.dockingPortQty;
            drillQty = step.drillQty;
            deltaV = step.deltaV;
            TWR = step.TWR;
            stage = step.stage;
            includeDockingPort = step.includeDockingPort;
            asl = step.asl;

            // Part selection
            partName = step.partName;
            partTitle = step.partTitle;
            partOnlyAvailable = step.partOnlyAvailable;

            partGroup = step.partGroup;
        }

        public bool CheckCrew(out int crew)
        {
            crew = FlightGlobals.ActiveVessel.GetCrewCount();
            return crew >= crewCount;
        }

        public bool CheckPart()
        {
            return PartLookupUtils.ShipHasPartByInternalName(partName);
        }

        public void CheckResource(string resourceName, bool locked, out double amt, out double capacity, bool onlyLocked = false)
        {

            if (HighLogic.LoadedSceneIsEditor)
            {
                VesselResourceQuery.TryGet(EditorLogic.fetch.ship, resourceName, out amt, out capacity, locked, onlyLocked);
            }
            else
            {
                VesselResourceQuery.TryGet(FlightGlobals.ActiveVessel, resourceName, out amt, out capacity, locked, onlyLocked);
            }
        }


        public ConfigNode ToConfigNode(bool _saveAsDefault)
        {
            var n = new ConfigNode("STEP");
            n.AddValue("title", title ?? "");
            n.AddValue("descr", descr ?? "");
            n.AddValue("completed", completed);
            n.AddValue("locked", _locked);
            n.AddValue("stepType", stepType.ToString());
            n.AddValue("maneuver", maneuver.ToString());
            n.AddValue("maneuverBody", maneuverBody);
            n.AddValue("toggle", toggle);
            n.AddValue("initialToggleValue", initialToggleValue);

            n.AddValue("traitName", traitName);

            if (resourceList.Count > 0)
            {
                foreach (var r in resourceList)
                {
                    ConfigNode node = new ConfigNode("RESOURCE_LIST");
                    node.AddValue("resourceName", r.resourceName);
                    node.AddValue("resourceAmount", r.resourceAmount);
                    node.AddValue("resourceCapacity", r.resourceCapacity);
                    node.AddValue("locked", r.locked);
                    node.AddValue("direction", r.direction.ToString());
                    n.AddNode(node);
                }
            }
            if (engineResourceList.Count > 0)
            {
                foreach (var r in engineResourceList)
                {
                    ConfigNode node = new ConfigNode("ENGINE_RESOURCE_LIST");
                    node.AddValue("resourceName", r.resourceName);
                    node.AddValue("resourceAmount", r.resourceAmount);
                    node.AddValue("resourceCapacity", r.resourceCapacity);
                    node.AddValue("locked", r.locked);
                    node.AddValue("direction", r.direction.ToString());
                    n.AddNode(node);
                }
            }
            if (rcsResourceList.Count > 0)
            {
                foreach (var r in rcsResourceList)
                {
                    ConfigNode node = new ConfigNode("RCS_RESOURCE_LIST");
                    node.AddValue("resourceName", r.resourceName);
                    node.AddValue("resourceAmount", r.resourceAmount);
                    node.AddValue("resourceCapacity", r.resourceCapacity);
                    node.AddValue("locked", r.locked);
                    node.AddValue("direction", r.direction.ToString());
                    n.AddNode(node);
                }
            }
            n.AddValue("batteryCapacity", batteryCapacity);
            n.AddValue("chargeRateTotal", chargeRateTotal);

            n.AddValue("antennaPower", antennaPower);
            n.AddValue("solarChargeRate", solarChargeRate);
            n.AddValue("solarPaneltracking", solarPaneltracking);
            n.AddValue("fuelCellChargeRate", fuelCellChargeRate);
            n.AddValue("generatorChargeRate", generatorChargeRate);
            n.AddValue("radiatorCoolingRate", radiatorCoolingRate);

            n.AddValue("spotlights", spotlights);
            n.AddValue("parachutes", parachutes);
            n.AddValue("reactionWheels", reactionWheels);
            n.AddValue("torquePitchRollYawEqual", torquePitchRollYawEqual);


            n.AddValue("torquePitch", torquePitch);
            n.AddValue("torqueYaw", torqueYaw);
            n.AddValue("torqueRoll", torqueRoll);

            n.AddValue("ap", ap);
            n.AddValue("pe", pe);
            n.AddValue("marginOfError", marginOfError);
            n.AddValue("peMatchesAp", peMatchesAp);

            n.AddValue("moduleName", moduleName);
            n.AddValue("minSASLevel", minSASLevel);

            n.AddValue("flagBody", flagBody);
            n.AddValue("destBody", destBody);
            n.AddValue("destBiome", destBiome);
            n.AddValue("biome", biome);

            n.AddValue("destType", destType);
            n.AddValue("destAsteroid", destAsteroid);

            n.AddValue("destVessel", destVessel);

            if (_saveAsDefault)
            {
                n.AddValue("trackedVessel", "");
                n.AddValue("experience", 0);
                n.AddValue("reputation", 0);
                n.AddValue("funding", 0);
                n.AddValue("science", 0f);
                n.AddValue("vesselGuid", Guid.Empty);
                n.AddValue("stepCompleted", false);
                n.AddValue("stepStatus", StepStatus.Inactive.ToString());
            }
            else
            {
                n.AddValue("trackedVessel", trackedVessel);
                n.AddValue("experience", experience);
                n.AddValue("reputation", reputation);
                n.AddValue("funding", funding);
                n.AddValue("science", 0f);
                n.AddValue("vesselGuid", vesselGuid);
                n.AddValue("stepCompleted", stepCompleted);
                n.AddValue("stepStatus", stepStatus.ToString());
            }
            n.AddValue("vabCategory", vabCategory);
            n.AddValue("requiresLanding", requiresLanding);

            n.AddValue("requiresDocking", requiresDocking);
            n.AddValue("hasDocked", hasDocked);
            n.AddValue("flagCnt", flagCnt);
            n.AddValue("crewCount", crewCount);
            n.AddValue("engineQty", engineQty);
            n.AddValue("engineType", engineType);
            n.AddValue("engineGimbaled", engineGimbaled);

            n.AddValue("rcsType", rcsType);

            n.AddValue("controlSourceQty", controlSourceQty);
            n.AddValue("dockingPortQty", dockingPortQty);
            n.AddValue("drillQty", drillQty);
            n.AddValue("deltaV", deltaV);
            n.AddValue("TWR", TWR);
            n.AddValue("stage", stage);
            n.AddValue("includeDockingPort", includeDockingPort);
            n.AddValue("asl", asl);

            n.AddValue("partName", partName ?? "");
            n.AddValue("partTitle", partTitle ?? "");
            n.AddValue("partOnlyAvailable", partOnlyAvailable);

            n.AddValue("partGroup", partGroup);

            return n;
        }

        public static Step FromConfigNode(ConfigNode n)
        {
            var s = new Step();
            s.title = n.SafeLoad("title", s.title);
            s.descr = n.SafeLoad("descr", s.descr);
            s.completed = n.SafeLoad("completed", s.completed);
            s._locked = n.SafeLoad("locked", s._locked);

            if (Enum.TryParse(n.GetValue("stepType"), out CriterionType t)) s.stepType = t;

            if (Enum.TryParse(n.GetValue("maneuver"), out Maneuver m)) s.maneuver = m;
            s.maneuverBody = n.SafeLoad("maneuverBody", "");
            s.toggle = n.SafeLoad("toggle", s.toggle);
            s.initialToggleValue = n.SafeLoad("initialToggleValue", s.initialToggleValue);

            s.traitName = n.SafeLoad("traitName", s.traitName);

            Direction d;
            var nodes = n.GetNodes("RESOURCE_LIST");
            foreach (var node in nodes)
            {
                ResInfo ri = new ResInfo();
                ri.resourceName = node.SafeLoad("resourceName", ri.resourceName);
                ri.resourceAmount = node.SafeLoad("resourceAmount", ri.resourceAmount);
                ri.resourceCapacity = node.SafeLoad("resourceCapacity", ri.resourceCapacity);
                ri.locked = node.SafeLoad("locked", ri.locked);
                if (Enum.TryParse(n.GetValue("direction"), out d)) ri.direction = d;

                s.resourceList.Add(ri);
            }
            nodes = n.GetNodes("ENGINE_RESOURCE_LIST");
            foreach (var node in nodes)
            {
                ResInfo ri = new ResInfo();
                ri.resourceName = node.SafeLoad("resourceName", ri.resourceName);
                ri.resourceAmount = node.SafeLoad("resourceAmount", ri.resourceAmount);
                ri.resourceCapacity = node.SafeLoad("resourceCapacity", ri.resourceCapacity);
                ri.locked = node.SafeLoad("locked", ri.locked);
                if (Enum.TryParse(n.GetValue("direction"), out d)) ri.direction = d;

                s.engineResourceList.Add(ri);
            }
            nodes = n.GetNodes("RCS_RESOURCE_LIST");
            foreach (var node in nodes)
            {
                ResInfo ri = new ResInfo();
                ri.resourceName = node.SafeLoad("resourceName", ri.resourceName);
                ri.resourceAmount = node.SafeLoad("resourceAmount", ri.resourceAmount);
                ri.resourceCapacity = node.SafeLoad("resourceCapacity", ri.resourceCapacity);
                ri.locked = node.SafeLoad("locked", ri.locked);
                if (Enum.TryParse(n.GetValue("direction"), out d)) ri.direction = d;

                s.rcsResourceList.Add(ri);
            }

            s.batteryCapacity = n.SafeLoad("batteryCapacity", s.batteryCapacity);
            s.chargeRateTotal = n.SafeLoad("chargeRateTotal", s.chargeRateTotal);

            s.antennaPower = n.SafeLoad("antennaPower", s.antennaPower);
            s.solarChargeRate = n.SafeLoad("solarChargeRate", s.solarChargeRate);
            string str = n.SafeLoad("solarPaneltracking", "both");
            s.solarPaneltracking = (SolarUtils.Tracking)Enum.Parse(typeof(SolarUtils.Tracking), str, true);

            s.fuelCellChargeRate = n.SafeLoad("fuelCellChargeRate", s.fuelCellChargeRate);
            s.generatorChargeRate = n.SafeLoad("generatorChargeRate", s.generatorChargeRate);
            s.radiatorCoolingRate = n.SafeLoad("radiatorCoolingRate", s.radiatorCoolingRate);

            s.spotlights = n.SafeLoad("spotlights", s.spotlights);
            s.parachutes = n.SafeLoad("parachutes", s.parachutes);
            s.reactionWheels = n.SafeLoad("reactionWheels", s.reactionWheels);
            s.torquePitchRollYawEqual = n.SafeLoad("torquePitchRollYawEqual", true);

            s.torquePitch = n.SafeLoad("torquePitch", s.torquePitch);
            s.torqueYaw = n.SafeLoad("torqueYaw", s.torqueYaw);
            s.torqueRoll = n.SafeLoad("torqueRoll", s.torqueRoll);

            s.ap = n.SafeLoad("ap", s.ap);
            s.pe = n.SafeLoad("pe", s.pe);
            s.marginOfError = n.SafeLoad("marginOfError", s.marginOfError);
            s.peMatchesAp = n.SafeLoad("peMatchesAp", s.peMatchesAp);

            s.moduleName = n.SafeLoad("moduleName", s.moduleName);
            s.minSASLevel = n.SafeLoad("minSASLevel", s.minSASLevel);

            s.flagBody = n.SafeLoad("flagBody", s.flagBody);
            s.destBody = n.SafeLoad("destBody", s.destBody);
            s.destBiome = n.SafeLoad("destBiome", s.destBiome);
            s.biome = n.SafeLoad("biome", s.biome);

            if (Enum.TryParse(n.GetValue("destType"), out DestinationType dt)) s.destType = dt;

            s.destAsteroid = n.SafeLoad("destAsteroid", s.destAsteroid);

            s.destVessel = n.SafeLoad("destVessel", s.destVessel);
            s.trackedVessel = n.SafeLoad("trackedVessel", s.trackedVessel);
            s.experience = n.SafeLoad("experience", s.experience);
            s.reputation = n.SafeLoad("reputation", s.reputation);
            s.funding = n.SafeLoad("funding", s.funding);
            s.science = n.SafeLoad("science", s.science);
            s.stepCompleted = n.SafeLoad("stepCompleted", s.stepCompleted);

            if (Enum.TryParse(n.GetValue("StepStatus"), out StepStatus ss))
                s.stepStatus = ss;

            s.vesselGuid = n.SafeLoad("vesselGuid", Guid.Empty);
            s.vabCategory = n.SafeLoad("vabCategory", s.vabCategory);
            s.requiresLanding = n.SafeLoad("requiresLanding", s.requiresLanding);

            s.requiresDocking = n.SafeLoad("requiresDocking", s.requiresDocking);
            s.hasDocked = n.SafeLoad("hasDocked", s.hasDocked);
            s.flagCnt = n.SafeLoad("flagCnt", s.flagCnt);
            s.crewCount = n.SafeLoad("crewCount", s.crewCount);
            s.engineQty = n.SafeLoad("engineQty", s.engineQty);
            s.engineType = n.SafeLoad("engineType", s.engineType);
            s.engineGimbaled = n.SafeLoad("engineGimbaled", s.engineGimbaled);
            s.rcsType = n.SafeLoad("rcsType", s.rcsType);

            s.controlSourceQty = n.SafeLoad("controlSourceQty", s.controlSourceQty);
            s.dockingPortQty = n.SafeLoad("dockingPortQty", s.dockingPortQty);
            s.drillQty = n.SafeLoad("drillQty", s.drillQty);
            s.deltaV = n.SafeLoad("deltaV", s.deltaV);
            s.TWR = n.SafeLoad("TWR", s.TWR);
            s.stage = n.SafeLoad("stage", s.stage);
            s.includeDockingPort = n.SafeLoad("includeDockingPort", s.includeDockingPort);
            s.asl = n.SafeLoad("asl", s.asl);

            s.partName = n.SafeLoad("partName", s.partName);
            s.partTitle = n.SafeLoad("partTitle", s.partTitle);
            s.partOnlyAvailable = n.SafeLoad("partOnlyAvailable", s.partOnlyAvailable);

            if (Enum.TryParse(n.GetValue("partGroup"), out PartGroup pg))
                s.partGroup = pg;
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
        public bool requireAll = true;
        public List<StepNode> Children = new List<StepNode>();

        public StepNode() { Id = _nextId++; }

        public StepNode(StepNode node, StepNode parent = null)
        {
            Id = _nextId++;
            data = new Step(node.data);
            Expanded = node.Expanded;
            Parent = (parent == null) ? node.Parent : parent;
            requireAll = node.requireAll;
            foreach (var c in node.Children)
            {
                Children.Add(new StepNode(c, this));
            }
        }

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

        public   static ConfigNode ToConfigNodeRecursive(StepNode node, bool saveAsDefault)
        {
            var n = new ConfigNode("NODE");
            n.AddValue("title", node.data.title);
            n.AddValue("Expanded", node.Expanded);
            n.AddValue("requireAll", node.requireAll);
            n.AddNode(node.data.ToConfigNode(saveAsDefault));
            foreach (var c in node.Children) 
                n.AddNode(ToConfigNodeRecursive(c, saveAsDefault));
            return n;
        }

        public static StepNode FromConfigNodeRecursive(ConfigNode n)
        {
            var node = new StepNode();

            node.Expanded = n.SafeLoad("Expanded", false);
            node.requireAll = n.SafeLoad("requireAll", false);

            node.data.title = n.SafeLoad("title", "noTitle");

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
