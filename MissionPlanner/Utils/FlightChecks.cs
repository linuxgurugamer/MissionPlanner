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
        //static public float chargeRate;
        //static public float coolingRate;
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
                return ok;
            }
            return false;
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
#if false
                case CriterionType.DeltaV:
                    var info = DeltaVUtils.GetActiveStageInfo_Vac(useLaunchpadFirstStageIfPrelaunch: true);
                    return info.deltaV >= s.deltaV;
#endif

                case CriterionType.Drills:
                    return (HighLogic.LoadedSceneIsFlight && DrillUtils.GetDrillParts(FlightGlobals.ActiveVessel).Count >= s.drillQty) ||
                    (HighLogic.LoadedSceneIsEditor && DrillUtils.GetDrillParts(EditorLogic.fetch.ship).Count >= s.drillQty);

                case CriterionType.DockingPort:
                    return (HighLogic.LoadedSceneIsFlight && DockingPortUtils.GetDockingParts(FlightGlobals.ActiveVessel).Count >= s.dockingPortQty) ||
                    (HighLogic.LoadedSceneIsEditor && DockingPortUtils.GetDockingParts(EditorLogic.fetch.ship).Count >= s.dockingPortQty);

                case CriterionType.ControlSource:
                    return (HighLogic.LoadedSceneIsFlight && PartLookupUtils.ShipModulesCount<ModuleCommand>(FlightGlobals.ActiveVessel)  >= s.controlSourceQty)||
                        (HighLogic.LoadedSceneIsEditor && PartLookupUtils.ShipModulesCount<ModuleCommand>(EditorLogic.fetch.ship) >= s.controlSourceQty);

                case CriterionType.Part:
                    return s.CheckPart();

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

                case CriterionType.VABOrganizerCategory:
                    {
                        for (int i = 0; i < partsList.Count; i++)
                        {
                            var rc = VABOrganizerUtils.IsPartInCategory(s.vabCategory, partsList[i].partInfo.name);
                            if (rc)
                                return true;
                        }
                    }
                    return false;

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

                case CriterionType.RCS:
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

                case CriterionType.Engines:
                    {
                        for (int i = 0; i < s.engineResourceList.Count; i++)
                        {
                            var resinfo = s.engineResourceList[i];
                            s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                            if (amt < resinfo.resourceAmount || capacity < resinfo.resourceCapacity)
                                return false;
                        }

                        bool rc =  EngineTypeMatcher.PartsHaveEngineType(partsList, s.engineType);
                        var info = DeltaVUtils.GetActiveStageInfo_Vac(useLaunchpadFirstStageIfPrelaunch: true);
                        rc &=  info.deltaV >= s.deltaV & info.TWR >= s.TWR;


                        return rc;
                    }

                case CriterionType.Batteries:
                    {
                        if (HighLogic.LoadedSceneIsFlight)
                            return (Utils.BatteryUtils.GetTotalBatteryCapacityFlight(FlightGlobals.ActiveVessel) >= s.batteryCapacity);
                        return BatteryUtils.GetTotalBatteryCapacityEditor(EditorLogic.fetch.ship) >= s.batteryCapacity;

                    }

                case CriterionType.Communication:
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

                case CriterionType.SolarPanels:
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

                case CriterionType.FuelCells:
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

                case CriterionType.Generators:
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
                case CriterionType.Radiators:
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

                case CriterionType.Lights:
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

                case CriterionType.Parachutes:
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

                case CriterionType.ReactionWheels:
                    {
                        TorqueSummary ts;
                        if (HighLogic.LoadedSceneIsFlight)
                            ts = ReactionWheelUtils.GetEnabledTorqueFlight(FlightGlobals.ActiveVessel);
                        else
                            ts = GetNominalTorqueEditor(EditorLogic.fetch.ship);

                        return (ts.Total >= s.reactionWheels &&
                            ts.Roll >= s.torqueRoll && ts.Pitch >= s.torquePitch && ts.Yaw >= s.torqueYaw);
                    }

                case CriterionType.Flags:
                    {
                        if (HighLogic.LoadedSceneIsEditor)
                            return true;

                        bool landed = MissionVisitTracker.HasPlantedFlagOnBody(FlightGlobals.ActiveVessel, s.flagBody, countStartBody: false);
                        flagCount = MissionVisitTracker.FlagCount(FlightGlobals.ActiveVessel, s.flagBody);

                        return s.flagCnt <= flagCount;
                    }

                case CriterionType.Destination_vessel:
                    if (HighLogic.LoadedSceneIsEditor)
                        return true;

                    if (!string.IsNullOrEmpty(s.destVessel))
                        return MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destVessel, countStartBody: true);
                    break;

                case CriterionType.Destination_asteroid:
                    if (HighLogic.LoadedSceneIsEditor)
                        return true;
                    if (!string.IsNullOrEmpty(s.destAsteroid))
                        return MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destAsteroid, countStartBody: true);
                    break;

                case CriterionType.Destination_body:
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

                default:
                    return true;
            }
            return false;
        }

        public static bool CheckStatusLanded(Step s)
        {
            switch (s.stepType)
            {
                case CriterionType.Destination_body:
                    if (!string.IsNullOrEmpty(s.destBody))
                        return MissionVisitTracker.HasLandedOnBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: true);

                    break;

                default:
                    break;
            }
            return false;
        }

    }
}
