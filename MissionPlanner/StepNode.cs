using SpaceTuxUtility;
using System;
using System.Collections.Generic;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{

    public class ResInfo
    {
        public string resourceName = "";
        public float resourceAmount = 0f;
        public float resourceCapacity = 0f;
        public bool locked = false;

        public ResInfo() { }
        public ResInfo(string resourceName) { this.resourceName = resourceName; }
        public ResInfo(ResInfo r)
        {
            this.resourceName = r.resourceName;
            this.resourceAmount = r.resourceAmount;
            this.resourceCapacity = r.resourceCapacity;
            this.locked = r.locked;
        }
    }

    [Serializable]
    public class Step
    {
        public string title = "New Step";
        public string descr = "";

        public bool completed = false;
        public bool locked = false;

        public CriterionType stepType = CriterionType.ChecklistItem;
        public Maneuver maneuver = Maneuver.None;
        //public ChecklistItem checklistItem = null;

        public bool toggle = false;
        public bool initialToggleValue = false;

        public float minFloatRange = 0f;
        public float maxFloatRange = 1f;

        public float number = 100;

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

        public string moduleName = "";
        public int minSASLevel = 0;

        //public string bodyAsteroidVessel = "";
        public string flagBody = "";
        public string destBody = "";
        public string destBiome = "";
        public string biome = "";
        public string destAsteroid = "";
        public string destVessel = "";
        public string trackedVessel = "";
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

        // Part selection
        public string partName = "";        // internal name (AvailablePart.name)
        public string partTitle = "";       // display title (AvailablePart.title)
        public bool partOnlyAvailable = true;

        public Step()
        {
            //criterion = new Criterion();
        }

        public Step(Step step)
        {
            title = step.title;
            descr = step.descr;

            completed = step.completed;
            locked = step.locked;

            stepType = step.stepType;
            maneuver = step.maneuver;

            toggle = step.toggle;
            initialToggleValue = step.initialToggleValue;

            minFloatRange = step.minFloatRange;
            maxFloatRange = step.maxFloatRange;

            number = step.number;

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

            moduleName = step.moduleName;
            minSASLevel = step.minSASLevel;

            flagBody = step.flagBody;
            destBody = step.destBody;
            destBiome = step.destBiome;
            biome = step.biome;
            destAsteroid = step.destAsteroid;
            destVessel = step.destVessel;
            trackedVessel = step.trackedVessel;
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

            // Part selection
            partName = step.partName;
            partTitle = step.partTitle;
            partOnlyAvailable = step.partOnlyAvailable;
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



        public ConfigNode ToConfigNode()
        {
            var n = new ConfigNode("STEP");
            n.AddValue("title", title ?? "");
            n.AddValue("descr", descr ?? "");
            n.AddValue("completed", completed);
            n.AddValue("locked", locked);
            n.AddValue("stepType", stepType.ToString());
            n.AddValue("maneuver", maneuver.ToString());

            n.AddValue("toggle", toggle);
            n.AddValue("initialToggleValue", initialToggleValue);
            n.AddValue("minFloatRange", minFloatRange);
            n.AddValue("maxFloatRange", maxFloatRange);
            n.AddValue("number", number);
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

            n.AddValue("moduleName", moduleName);
            n.AddValue("minSASLevel", minSASLevel);

            n.AddValue("flagBody", flagBody);
            n.AddValue("destBody", destBody);
            n.AddValue("destBiome", destBiome);
            n.AddValue("biome", biome);
            n.AddValue("destAsteroid", destAsteroid);

            n.AddValue("destVessel", destVessel);
            n.AddValue("trackedVessel", trackedVessel);
            n.AddValue("vesselGuid", vesselGuid);
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

            n.AddValue("partName", partName ?? "");
            n.AddValue("partTitle", partTitle ?? "");
            n.AddValue("partOnlyAvailable", partOnlyAvailable);

            return n;
        }

        public static Step FromConfigNode(ConfigNode n)
        {
            var s = new Step();
            s.title = n.SafeLoad("title", s.title);
            s.descr = n.SafeLoad("descr", s.descr);
            s.completed = n.SafeLoad("completed", false);
            s.locked = n.SafeLoad("locked", false);
            CriterionType t;
            if (Enum.TryParse(n.GetValue("stepType"), out t)) s.stepType = t;

            Maneuver m;
            if (Enum.TryParse(n.GetValue("maneuver"), out m)) s.maneuver= m;
            s.toggle = n.SafeLoad("toggle", false);
            s.initialToggleValue = n.SafeLoad("initialToggleValue", false);
            s.minFloatRange = n.SafeLoad("minFloatRange", 0f);
            s.maxFloatRange = n.SafeLoad("maxFloatRange", 0f);
            s.number = n.SafeLoad("number", 0f);
            s.traitName = n.SafeLoad("traitName", "");

            var nodes = n.GetNodes("RESOURCE_LIST");
            foreach (var node in nodes)
            {
                ResInfo ri = new ResInfo();
                ri.resourceName = node.SafeLoad("resourceName", "");
                ri.resourceAmount = node.SafeLoad("resourceAmount", 0f);
                ri.resourceCapacity = node.SafeLoad("resourceCapacity", 0f);
                ri.locked = node.SafeLoad("locked", ri.locked);
                s.resourceList.Add(ri);
            }
            nodes = n.GetNodes("ENGINE_RESOURCE_LIST");
            foreach (var node in nodes)
            {
                ResInfo ri = new ResInfo();
                ri.resourceName = node.SafeLoad("resourceName", "");
                ri.resourceAmount = node.SafeLoad("resourceAmount", 0f);
                ri.resourceCapacity = node.SafeLoad("resourceCapacity", 0f);
                ri.locked = node.SafeLoad("locked", ri.locked); 
                s.engineResourceList.Add(ri);
            }
            nodes = n.GetNodes("RCS_RESOURCE_LIST");
            foreach (var node in nodes)
            {
                ResInfo ri = new ResInfo();
                ri.resourceName = node.SafeLoad("resourceName", "");
                ri.resourceAmount = node.SafeLoad("resourceAmount", 0f);
                ri.resourceCapacity = node.SafeLoad("resourceCapacity", 0f);
                ri.locked = node.SafeLoad("locked", ri.locked);
                s.rcsResourceList.Add(ri);
            }

            s.batteryCapacity = n.SafeLoad("batteryCapacity", 0f);
            s.chargeRateTotal = n.SafeLoad("chargeRateTotal", 0f);

            s.antennaPower = n.SafeLoad("antennaPower", 0f);
            s.solarChargeRate = n.SafeLoad("solarChargeRate", 0f);
            string str = n.SafeLoad("solarPaneltracking", "both");
            s.solarPaneltracking = (SolarUtils.Tracking)Enum.Parse(typeof(SolarUtils.Tracking), str, true);

            s.fuelCellChargeRate = n.SafeLoad("fuelCellChargeRate", 0f);
            s.generatorChargeRate = n.SafeLoad("generatorChargeRate", 0f);
            s.radiatorCoolingRate = n.SafeLoad("radiatorCoolingRate", 0);

            s.spotlights = n.SafeLoad("spotlights", 0);
            s.parachutes = n.SafeLoad("parachutes", 0);
            s.reactionWheels = n.SafeLoad("reactionWheels", 0);
            s.torquePitchRollYawEqual = n.SafeLoad("torquePitchRollYawEqual", true);

            s.torquePitch = n.SafeLoad("torquePitch", 0);
            s.torqueYaw = n.SafeLoad("torqueYaw", 0);
            s.torqueRoll = n.SafeLoad("torqueRoll", 0);

            s.moduleName = n.SafeLoad("moduleName", "");
            s.minSASLevel = n.SafeLoad("minSASLevel", 0);

            s.flagBody = n.SafeLoad("flagBody", "");
            s.destBody = n.SafeLoad("destBody", "");
            s.destBiome = n.SafeLoad("destBiome", "");
            s.biome = n.SafeLoad("biome", "");
            s.destAsteroid = n.SafeLoad("destAsteroid", "");

            s.destVessel = n.SafeLoad("destVessel", "");
            s.trackedVessel = n.SafeLoad("trackedVessel", "");
            s.vesselGuid = n.SafeLoad("vesselGuid", Guid.Empty);
            s.vabCategory = n.SafeLoad("vabCategory", "");
            s.requiresLanding = n.SafeLoad("requiresLanding", false);

            s.requiresDocking = n.SafeLoad("requiresDocking", false);
            s.hasDocked = n.SafeLoad("hasDocked", false);
            s.flagCnt = n.SafeLoad("flagCnt", 0);
            s.crewCount = n.SafeLoad("crewCount", 0);
            s.engineQty = n.SafeLoad("engineQty", 0);
            s.engineType = n.SafeLoad("engineType", "");
            s.engineGimbaled = n.SafeLoad("engineGimbaled", false);
            s.rcsType = n.SafeLoad("rcsType", "");

            s.controlSourceQty = n.SafeLoad("controlSourceQty", 0);
            s.dockingPortQty = n.SafeLoad("dockingPortQty", 0);
            s.drillQty = n.SafeLoad("drillQty", 0);
            s.deltaV = n.SafeLoad("deltaV", 0d);
            s.TWR = n.SafeLoad("TWR", 0f);

            s.partName = n.SafeLoad("partName", "");
            s.partTitle = n.SafeLoad("partTitle", "");
            s.partOnlyAvailable = n.SafeLoad("partOnlyAvailable", false);

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
        public  List<StepNode> Children = new List<StepNode>();

        public StepNode() { Id = _nextId++; }

        public StepNode(StepNode node, StepNode parent = null)
        {
            Id = _nextId++;
            data = new Step(node.data);
            Expanded = node.Expanded;
            Parent = parent == null ? node.Parent : parent;
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

        public ConfigNode ToConfigNodeRecursive()
        {
            var n = new ConfigNode("NODE");
            n.AddValue("title", data.title ?? "");
            n.AddValue("Expanded", Expanded);
            n.AddValue("requireAll", requireAll);
            n.AddNode(data.ToConfigNode());
            foreach (var c in Children) n.AddNode(c.ToConfigNodeRecursive());
            return n;
        }

        public static StepNode FromConfigNodeRecursive(ConfigNode n)
        {
            var node = new StepNode();
            node.data.title = n.GetValue("title") ?? node.data.title;
            bool ex;
            if (bool.TryParse(n.GetValue("Expanded"), out ex)) node.Expanded = ex;
            if (bool.TryParse(n.GetValue("requireAll"), out ex)) node.requireAll = ex;

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
