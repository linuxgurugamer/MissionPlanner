using MissionPlanner.MissionPlanner;
using System.Collections.Generic;
using System.Linq;
using static MissionPlanner.Utils.FuelCellUtils;
using static ParachuteUtils;
using static RadiatorUtils;
using static ReactionWheelUtils;
using static SolarUtils;

namespace MissionPlanner.Utils
{
    internal class FlightChecks
    {
        public static int crew;
        static public bool powerMet;
        static public int flagCount;

        public static bool CheckCrew(Step s, out int crew)
        {
            crew = FlightGlobals.ActiveVessel.GetCrewCount();
            return crew >= s.crewCount;
        }

        public static bool CheckChildStatus(StepNode s, int level = 0)
        {
            if (CheckStatus(s.data))
            {
                bool ok = true;
                foreach (StepNode c in s.Children)
                {
                    var rc = CheckChildStatus(c, level + 1);
                    if (s.requireAll && !rc)
                    {
                        return false;
                    }
                    ok |= rc;
                }
                if (ok && s.data.stepActive)
                        s.data.completed = true;
                return ok;
            }
            return false;
        }

        public static bool IsWithinPercent(double a, double b, double percent)
        {
            if (percent < 0)
                percent = -percent;  // ensure positive percentage

            if (b == 0)
                return a == 0;

            double factor = percent / 100.0;
            double lower = b * (1.0 - factor);
            double upper = b * (1.0 + factor);

            return a >= lower && a <= upper;
        }


        public static bool CheckStatus(Step s)
        {
            if ((!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null) &&
                 !HighLogic.LoadedSceneIsEditor)
                return true;
            crew = 0;
            powerMet = false;
            List<Part> partsList = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts : FlightGlobals.ActiveVessel.parts;

            switch (s.stepType)
            {
                case CriterionType.PartGroup:
                    switch (s.partGroup)
                    {
                        case PartGroup.Batteries:
                            {
                                if (HighLogic.LoadedSceneIsFlight)
                                    return (Utils.BatteryUtils.GetTotalBatteryCapacityFlight(FlightGlobals.ActiveVessel) >= s.batteryCapacity);
                                return BatteryUtils.GetTotalBatteryCapacityEditor(EditorLogic.fetch.ship) >= s.batteryCapacity;

                            }
                        case PartGroup.Communication:
                            if (HighLogic.LoadedSceneIsFlight)
                            {
                                if (AntennaUtils.HasAntennaFlight(FlightGlobals.ActiveVessel))
                                    return (Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel) >= s.antennaPower);
                                else
                                    return false;
                            }
                            else
                            {
                                if (AntennaUtils.HasAntennaEditor(EditorLogic.fetch.ship))
                                    return Utils.AntennaUtils.GetTotalAntennaPowerEditor(EditorLogic.fetch.ship) >= s.antennaPower;
                                else
                                    return false;
                            }
                        case PartGroup.ControlSource:
                            return (HighLogic.LoadedSceneIsFlight && PartLookupUtils.ShipModulesCount<ModuleCommand>(FlightGlobals.ActiveVessel) >= s.controlSourceQty) ||
                                (HighLogic.LoadedSceneIsEditor && PartLookupUtils.ShipModulesCount<ModuleCommand>(EditorLogic.fetch.ship) >= s.controlSourceQty);
                        case PartGroup.DockingPort:
                            return (HighLogic.LoadedSceneIsFlight && DockingPortUtils.GetDockingParts(FlightGlobals.ActiveVessel).Count >= s.dockingPortQty) ||
                                (HighLogic.LoadedSceneIsEditor && DockingPortUtils.GetDockingParts(EditorLogic.fetch.ship).Count >= s.dockingPortQty);
                        case PartGroup.Drills:
                            return (HighLogic.LoadedSceneIsFlight && DrillUtils.GetDrillParts(FlightGlobals.ActiveVessel).Count >= s.drillQty) ||
                                (HighLogic.LoadedSceneIsEditor && DrillUtils.GetDrillParts(EditorLogic.fetch.ship).Count >= s.drillQty);
                        case PartGroup.Engines:
                            {
                                for (int i = 0; i < s.engineResourceList.Count; i++)
                                {
                                    var resinfo = s.engineResourceList[i];
                                    s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                                    if (amt < resinfo.resourceAmount || capacity < resinfo.resourceCapacity)
                                        return false;
                                }

                                bool rc = EngineTypeMatcher.PartsHaveEngineType(partsList, s.engineType);
                                //var info = DeltaVUtils.GetActiveStageInfo_Vac(useLaunchpadFirstStageIfPrelaunch: true);

                                int realStage = (s.stage <= StageInfo.StageCount - 1) ? s.stage : StageInfo.StageCount - 1;
                                float dV = 0;
                                float twr = 0;
                                if (!s.asl)
                                {
                                    dV = StageInfo.DeltaVinVac(realStage);
                                    twr = StageInfo.TWRVac(realStage);
                                }
                                else
                                {
                                    dV = StageInfo.DeltaVatASL(realStage);
                                    twr = StageInfo.TWRASL(realStage);
                                }

                                rc &= dV >= s.deltaV & twr >= s.TWR;

                                return rc;
                            }
                        case PartGroup.FuelCells:
                            {
                                if (HighLogic.LoadedSceneIsEditor)
                                {
                                    FuelCellGenerationSummary fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                    if (fcgs.TotalFuelCellParts == 0)
                                        return false;
                                    if (EditorLogic.fetch.ship != null)
                                        return (float)fcgs.TotalECps >= s.fuelCellChargeRate;
                                    return false;
                                }
                                else
                                {
                                    FuelCellGenerationSummary fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);
                                    if (fcgs.TotalFuelCellParts == 0)
                                        return false;

                                    return (float)fcgs.TotalECps >= s.fuelCellChargeRate;
                                }
                            }
                        case PartGroup.Generators:
                            {
                                if (HighLogic.LoadedSceneIsEditor)
                                {
                                    GeneratorUtils.GeneratorSummary ggs = GeneratorUtils.GetTotalECGeneratorsEditor(EditorLogic.fetch.ship);
                                    if (ggs.generatorCnt == 0)
                                        return false;
                                    return (float)ggs.TotalECps >= s.generatorChargeRate;
                                }
                                else
                                {

                                    GeneratorUtils.GeneratorSummary ggs = GeneratorUtils.GetTotalECGeneratorsFlight(FlightGlobals.ActiveVessel);

                                    if (ggs.generatorCnt == 0)
                                        return false;
                                    return ggs.TotalECps >= s.generatorChargeRate;

                                }
                            }
                        case PartGroup.Lights:
                            {
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    return Utils.LightUtils.CountSpotlightsFlight(FlightGlobals.ActiveVessel) >= s.spotlights;
                                }
                                else
                                {
                                    if (EditorLogic.fetch.ship != null)
                                    {
                                        return Utils.LightUtils.CountSpotlightsEditor(EditorLogic.fetch.ship) >= s.spotlights;
                                    }
                                    return false;
                                }
                            }
                        case PartGroup.Parachutes:
                            {
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    ParachuteStateCounts psc = ParachuteUtils.GetParachuteStateCountsFlight(FlightGlobals.ActiveVessel);
                                    return psc.Total >= s.parachutes;
                                }
                                else
                                {
                                    ParachuteCapacitySummary psc = ParachuteUtils.GetParachuteCapacityEditor(EditorLogic.fetch.ship);

                                    if (EditorLogic.fetch.ship != null)
                                    {
                                        return psc.Total >= s.parachutes;
                                    }
                                    return false;
                                }
                            }
                        case PartGroup.Radiators:
                            {
                                RadiatorCoolingSummary rcs;

                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    rcs = RadiatorUtils.GetEstimatedCoolingFlight(FlightGlobals.ActiveVessel);
                                    return (float)rcs.TotalKW >= s.radiatorCoolingRate;
                                }
                                else
                                {
                                    rcs = RadiatorUtils.GetEstimatedCoolingEditor(EditorLogic.fetch.ship);

                                    if (EditorLogic.fetch.ship != null)
                                        return rcs.TotalKW >= s.radiatorCoolingRate;
                                    return false;
                                }
                            }
                        case PartGroup.RCS:
                            {
                                for (int i = 0; i < s.rcsResourceList.Count; i++)
                                {
                                    var resinfo = s.rcsResourceList[i];
                                    s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                                    if (amt < resinfo.resourceAmount || capacity < resinfo.resourceCapacity)
                                        return false;
                                }
                                if (RCSUtils.PartsHaveRCSType(partsList, s.rcsType))
                                    return true;
                                else
                                    return false;
                            }
                        case PartGroup.ReactionWheels:
                            {
                                TorqueSummary ts;
                                if (HighLogic.LoadedSceneIsFlight)
                                    ts = ReactionWheelUtils.GetEnabledTorqueFlight(FlightGlobals.ActiveVessel);
                                else
                                    ts = GetNominalTorqueEditor(EditorLogic.fetch.ship);

                                return (ts.Total >= s.reactionWheels &&
                                    ts.Roll >= s.torqueRoll && ts.Pitch >= s.torquePitch && ts.Yaw >= s.torqueYaw);
                            }
                        case PartGroup.SolarPanels:
                            {
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel, s.solarPaneltracking);

                                    if (sgs.TotalSolarParts == 0)
                                        return false;

                                    return (float)sgs.TotalECps >= s.solarChargeRate;
                                }
                                else
                                {
                                    SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                    if (sgs.TotalSolarParts == 0)
                                        return false;
                                    if (EditorLogic.fetch.ship != null)
                                        return (float)sgs.TotalECps >= s.solarChargeRate;
                                    return false;
                                }
                            }
                            break;
                    }
                    break;

                case CriterionType.Part:
                    return s.CheckPart();

                case CriterionType.Maneuver:

                    switch (s.maneuver)
                    {
                        case Maneuver.Launch:
                        case Maneuver.Orbit:
                            ApPeFromOrbit.ApPe aPpE = ApPeFromOrbit.ComputeApPe(FlightGlobals.ActiveVessel.orbitDriver.orbit,
                                                                                FlightGlobals.ActiveVessel.mainBody);
                            return FlightChecks.IsWithinPercent(aPpE.ApAltitude, s.ap * 1000f, s.marginOfError) &&
                                    FlightChecks.IsWithinPercent(aPpE.PeAltitude, s.pe * 1000f, s.marginOfError);

                        case Maneuver.ResourceTransfer:
                            for (int i = 0; i < s.resourceList.Count; i++)
                            {
                                var resinfo = s.resourceList[i];

                                s.CheckResource(resinfo.resourceName, resinfo.locked, out double amt, out double capacity, resinfo.locked);

                                if (resinfo.direction == Direction.StartToEnd)
                                {
                                    if (resinfo.endingAmount <= amt)
                                        return false;
                                }
                                else
                                {
                                    if (resinfo.startingAmount >= amt)
                                        return false;
                                }
                            }
                            return true;

                        default:
                            return true;
                    }

                case CriterionType.Module:
                    {
                        for (int i = 0; i < partsList.Count; i++)
                        {
                            foreach (var m in partsList[i].Modules)
                            {
                                if (m.moduleName == s.moduleName)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;

                case CriterionType.Resource:
                    {
                        for (int i = 0; i < s.resourceList.Count; i++)
                        {
                            var resinfo = s.resourceList[i];
                            s.CheckResource(resinfo.resourceName, resinfo.locked, out double amt, out double capacity, resinfo.locked);
                            if (amt < resinfo.resourceAmount || capacity < resinfo.resourceCapacity)
                                return false;
                        }
                    }
                    return true;

                case CriterionType.Staging:
                    {
                        return StageUtility.StageHasDecouplerOrSeparator(s.stage, s.includeDockingPort);
                    }

                case CriterionType.VABOrganizerCategory:
                    {
                        if (HierarchicalStepsWindow.vabOrganizer)
                        {
                            for (int i = 0; i < partsList.Count; i++)
                            {
                                var rc = VABOrganizerUtils.IsPartInCategory(s.vabCategory, partsList[i].partInfo.name);
                                if (rc)
                                    return true;
                            }
                        }
                        else
                            return true;
                        return false;
                    }


                case CriterionType.CrewMemberTrait:
                    if (HighLogic.LoadedSceneIsEditor)
                        return true;
                    var b = CrewUtils.VesselHasTrait(FlightGlobals.ActiveVessel, s.traitName);
                    return b;

                case CriterionType.CrewCount:
                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        int totalSeats = EditorLogic.fetch.ship.parts
                            .Where(p => p != null)
                            .Sum(p => p.CrewCapacity);
                        return totalSeats >= s.crewCount;
                    }
                    else
                    {
                        crew = FlightGlobals.ActiveVessel.GetCrewCount();
                        return crew >= s.crewCount;
                    }


                case CriterionType.SAS:
                    if (HighLogic.LoadedSceneIsEditor)
                        return true;
                    var sasInfo = SASUtils.GetAvailableSASModes(FlightGlobals.ActiveVessel);
                    return SASUtils.IsRequiredSASAvailable(s.minSASLevel, sasInfo);

                case CriterionType.ChargeRateTotal:
                    {
                        SolarGenerationSummary sgs;
                        FuelCellGenerationSummary fcgs;
                        GeneratorUtils.GeneratorSummary ggs;

                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            sgs = SolarUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel, Tracking.both);
                            fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);
                            ggs = GeneratorUtils.GetTotalECGeneratorsFlight(FlightGlobals.ActiveVessel);
                        }
                        else
                        {
                            sgs = SolarUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                            fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                            ggs = GeneratorUtils.GetTotalECGeneratorsEditor(EditorLogic.fetch.ship);
                        }

                        return (sgs.TotalECps + fcgs.TotalECps + ggs.TotalECps) >= s.chargeRateTotal;
                    }

                case CriterionType.Flags:
                    {
                        if (HighLogic.LoadedSceneIsEditor)
                            return true;

                        bool landed = MissionVisitTracker.HasPlantedFlagOnBody(FlightGlobals.ActiveVessel, s.flagBody, countStartBody: false);
                        flagCount = MissionVisitTracker.FlagCount(FlightGlobals.ActiveVessel, s.flagBody);

                        return s.flagCnt <= flagCount;
                    }

                case CriterionType.Destination:
                    switch (s.destType)
                    {
                        case DestinationType.Vessel:
                            if (HighLogic.LoadedSceneIsEditor)
                                return true;

                            if (!string.IsNullOrEmpty(s.destVessel))
                                return MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destVessel, countStartBody: true);
                            break;

                        case DestinationType.Asteroid:
                            if (HighLogic.LoadedSceneIsEditor)
                                return true;
                            if (!string.IsNullOrEmpty(s.destAsteroid))
                                return MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destAsteroid, countStartBody: true);
                            break;

                        case DestinationType.Body:
                            if (HighLogic.LoadedSceneIsEditor)
                                return true;
                            if (!string.IsNullOrEmpty(s.destBody))
                            {
                                if (!s.requiresLanding)

                                {
                                    return MissionVisitTracker.HasVisitedBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: true);
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(s.destBiome) || s.destBiome == BiomeUtils.ANYBIOME)
                                        return MissionVisitTracker.HasLandedOnBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: true);
                                    else
                                        return MissionVisitTracker.HasLandedOnBodyAtBiome(FlightGlobals.ActiveVessel, s.destBody, s.destBiome, countStartBody: true);
                                }
                            }
                            break;
                    }
                    break;

                default:
                    return true;
            }
            return false;
        }

        public static bool CheckStatusLanded(Step s)
        {
            switch (s.stepType)
            {
                case CriterionType.Destination:
                    if (s.destType == DestinationType.Body && !string.IsNullOrEmpty(s.destBody))
                        return MissionVisitTracker.HasLandedOnBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: true);

                    break;

                default:
                    break;
            }
            return false;
        }

    }
}
