using System;
using UnityEngine;
using static FuelCellUtils;
using static MissionPlanner.RegisterToolbar;
using static ParachuteUtils;
using static RadiatorUtils;
using static ReactionWheelUtils;
using static SolarUtils;

namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        private void DrawDetailWindow(int id)
        {
            GUILayout.Space(6);
            if (_detailNode == null) { GUI.DragWindow(new Rect(0, 0, 10000, 10000)); return; }
            var s = _detailNode.data;
            string tmpstr;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Title", GUILayout.Width(60));
                tmpstr = GUILayout.TextField(s.title ?? "", _titleEdit, GUILayout.Width(320));
                if (!s.locked)
                    s.title = tmpstr;
            }
            GUILayout.Space(6);
            GUILayout.Label("Description:");
            tmpstr = GUILayout.TextArea(s.descr ?? "", HighLogic.Skin.textArea, GUILayout.MinHeight(60));
            if (!s.locked)
                s.descr = tmpstr;

            GUILayout.Space(6);

            using (new GUILayout.HorizontalScope())
            {

                GUILayout.Label("Type", GUILayout.Width(60));
                var vals = (CriterionType[])Enum.GetValues(typeof(CriterionType));
                int curIdx = Array.IndexOf(vals, s.stepType);
                if (!s.locked)
                {
                    if (GUILayout.Button("◀", GUILayout.Width(26)))
                    {
                        curIdx = (curIdx - 1 + vals.Length) % vals.Length;
                        s.stepType = vals[curIdx];
                        GetCriterium(s.stepType, ref s);
                        //s.criterion = 

                    }
                }
                GUILayout.Label(s.stepType.ToString(), GUILayout.Width(180));
                if (!s.locked)
                {
                    if (GUILayout.Button("▶", GUILayout.Width(26)))
                    {
                        curIdx = (curIdx + 1) % vals.Length;
                        s.stepType = vals[curIdx];
                        GetCriterium(s.stepType, ref s);
                    }
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(4);

            DoCriteria(s, s.checklistItem);


            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Close", GUILayout.Width(100)))
                {
                    TrySaveToDisk_Internal(true);
                    _detailNode = null;
                }
                GUILayout.FlexibleSpace();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        void GetCriterium(CriterionType criterionType, ref Step step)
        {
            Log.Info("GetCriterium, ChecklistSystem.ActiveChecklist.items count: " + ChecklistSystem.ActiveChecklist.items.Count);

            foreach (MissionPlanner.ChecklistItem checkListItem in ChecklistSystem.ActiveChecklist.items)
            {
                Log.Info($"GetCriterium, {criterionType}  a.id: {checkListItem.id}");
                if (criterionType.ToString() == checkListItem.id)
                {

                    step.checklistItem = checkListItem;
                    Log.Info("Criterium found: " + checkListItem.id + "   " + checkListItem.name);

                    break;
                }
            }
        }

        private void DoCriteria(Step s, ChecklistItem checkListItem)
        {
#if false
            if (checkListItem == null)
            {
                Log.Info("DoCriteria, checkListItem is null");
                return;
            }
#endif
            //GUILayout.Label(criteria.type.ToString());
            switch (s.stepType)
            {
                case CriterionType.Module:
                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected:", GUILayout.Width(60));
                        GUILayout.Label(String.IsNullOrEmpty(s.moduleName) ? "(none)" : s.moduleName, HighLogic.Skin.label, GUILayout.Width(250));
                        if (!s.locked)
                        {
                            if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                OpenModulePicker(_detailNode);
                            if (!String.IsNullOrEmpty(s.resourceName))
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                {
                                    s.moduleName = "";
                                }
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                    break;

                case CriterionType.CrewMemberTrait:
                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected:", GUILayout.Width(60));
                        GUILayout.Label(String.IsNullOrEmpty(s.traitName) ? "(none)" : s.traitName, HighLogic.Skin.label, GUILayout.Width(250));
                        if (!s.locked)
                        {
                            if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                OpenTraitPicker(_detailNode);
                            if (!String.IsNullOrEmpty(s.resourceName))
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                {
                                    s.traitName = "";
                                }
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                    break;

                case CriterionType.toggle:
                    using (new GUILayout.HorizontalScope())
                    {
                        bool b = GUILayout.Toggle(s.initialToggleValue, "Initial Value", GUILayout.Width(140));
                        if (!s.locked)
                            s.initialToggleValue = b;
                        b = GUILayout.Toggle(s.toggle, "Current Value", GUILayout.Width(140));
                        if (!s.locked)
                            s.toggle = b;

                        GUILayout.FlexibleSpace();
                    }
                    break;
                case CriterionType.number:
                    FloatField("(float)", ref s.number, s.locked);
                    break;
                case CriterionType.range:
                    FloatRangeFields(ref s.minFloatRange, ref s.maxFloatRange, s.locked);
                    if (s.minFloatRange > s.maxFloatRange)
                        GUILayout.Label("Warning: minFloatRange > maxFloatRange.", _tinyLabel);
                    break;
                case CriterionType.crewCount:
                    GUILayout.Label("Crew Count:");
                    IntField("", ref s.crewCount, s.locked);

                    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                    {
                        GUILayout.Space(4);
                        using (new GUILayout.HorizontalScope())
                        {
                            if (s.CheckCrew(out int crew))
                                GUILayout.Label(string.Format("Crew count : {0} is ok", crew));
                            else
                                GUILayout.Label(string.Format("Crew count : {0} below miniumum required: {1}", crew, s.crewCount));

                            GUILayout.FlexibleSpace();
                        }
                    }
                    break;

                case CriterionType.part:
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Only available parts", GUILayout.Width(160));
                        s.partOnlyAvailable = GUILayout.Toggle(s.partOnlyAvailable, GUIContent.none, GUILayout.Width(22));
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected:", GUILayout.Width(60));
                        GUILayout.Label(String.IsNullOrEmpty(s.partTitle) ? "(none)" : s.partTitle, HighLogic.Skin.label, GUILayout.Width(320));
                        if (!s.locked)
                        {
                            if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                OpenPartPicker(_detailNode, s.partOnlyAvailable);
                            if (!String.IsNullOrEmpty(s.partTitle))
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                {
                                    s.partName = "";
                                    s.partTitle = "";
                                }
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(4);
                    using (new GUILayout.HorizontalScope())
                    {
                        if (s.CheckPart())
                            GUILayout.Label("Part is on the vessel");
                        else
                        {
                            if (s.partName != "")
                                GUILayout.Label("Part is not on the vessel", _errorLabel);
                            else
                                GUILayout.Label("No part specified", _errorLabel);
                        }
                        GUILayout.FlexibleSpace();
                    }
                    break;

                case CriterionType.resource:
                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected:", GUILayout.Width(60));
                        GUILayout.Label(String.IsNullOrEmpty(s.resourceName) ? "(none)" : s.resourceName, HighLogic.Skin.label, GUILayout.Width(160));
                        if (!s.locked)
                        {
                            if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                OpenResourcePicker(_detailNode);
                            if (!String.IsNullOrEmpty(s.resourceName))
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                {
                                    s.resourceName = "";
                                    s.resourceAmount = s.resourceCapacity = 0;
                                }
                            }
                        }
                        GUILayout.FlexibleSpace();
                    }
                    using (new GUILayout.HorizontalScope())
                        FloatField("Min Resource Amount: ", ref s.resourceAmount, s.locked);
                    using (new GUILayout.HorizontalScope())
                        FloatField("Min Resource Capacity: ", ref s.resourceCapacity, s.locked);

                    GUILayout.Space(4);
                    using (new GUILayout.HorizontalScope())
                    {
                        if (s.CheckResource() > 0)
                            GUILayout.Label("Resource is on the vessel");
                        else
                        {
                            if (s.resourceName != "")
                                GUILayout.Label("Resource is not on the vessel", _errorLabel);
                            else
                                GUILayout.Label("No resource specified", _errorLabel);
                        }
                        GUILayout.FlexibleSpace();
                    }
                    break;

                case CriterionType.SAS:
                    if (HighLogic.LoadedSceneIsFlight)
                    {

                        var sasInfo = SASUtils.GetAvailableSASModes(FlightGlobals.ActiveVessel);
                        if (sasInfo != null && sasInfo.Length > 0)
                        {
                            using (new GUILayout.HorizontalScope())
                                GUILayout.Label("SAS modes available:");
                            foreach (var sas in sasInfo)
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(15);
                                    GUILayout.Label(sas.ToString());
                                }
                            }
                        }
                        else
                        {
                            using (new GUILayout.HorizontalScope())
                                GUILayout.Label("No SAS modes available", _errorLabel);
                        }
                    }
                    else
                    //if (HighLogic.LoadedSceneIsEditor)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Min required SAS:", GUILayout.Width(130));
                            GUILayout.Label(SASUtils.GetSASLevelDescription(s.minSASLevel), HighLogic.Skin.label, GUILayout.Width(250));
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            if (!s.locked)
                            {
                                if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                    OpenSASPicker(_detailNode);
                                if (s.minSASLevel > 0)
                                {
                                    if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                    {
                                        s.minSASLevel = 0;
                                    }
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                        if (HighLogic.LoadedSceneIsEditor)
                        {
                            var sasInfoEditor = SASUtils.GetSASInfoEditor(EditorLogic.fetch.ship);
                            if (sasInfoEditor.HasSAS)
                            {
                                using (new GUILayout.HorizontalScope())
                                    GUILayout.Label("Highest SAS Service Level: " + SASUtils.GetSASLevelDescription(sasInfoEditor.HighestServiceLevel));
                            }
                        }
                    }
                    break;

                case CriterionType.RCS:
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        if (RCSUtils.IsRCSAvailableFlight(FlightGlobals.ActiveVessel))
                            GUILayout.Label("RCS is available");
                        else
                            GUILayout.Label("RCS is not available", _errorLabel);
                    }
                    else
                    {
                        if (RCSUtils.IsRCSAvailableEditor(EditorLogic.fetch.ship))
                            GUILayout.Label("RCS is available");
                        else
                            GUILayout.Label("RCS is not available", _errorLabel);

                    }
                    break;

                case CriterionType.Batteries:
                    using (new GUILayout.HorizontalScope())
                    {
                        FloatField("Min Battery Capacity: ", ref s.batteryCapacity, s.locked);
                        GUILayout.Label(" EC");
                    }
                    bool capacityMet = false;
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        if (Utils.BatteryUtils.HasBatteryFlight(FlightGlobals.ActiveVessel))
                            GUILayout.Label("Battery(s) are available");
                        else
                            GUILayout.Label("No batteries are available", _errorLabel);
                        capacityMet = (Utils.BatteryUtils.GetTotalBatteryCapacityFlight(FlightGlobals.ActiveVessel) >= s.batteryCapacity);
                    }
                    else
                    {
                        if (Utils.BatteryUtils.HasBatteryEditor(EditorLogic.fetch.ship))
                            GUILayout.Label("Battery(s) are available");
                        else
                            GUILayout.Label("No batteries are available", _errorLabel);
                        capacityMet = (Utils.BatteryUtils.GetTotalBatteryCapacityEditor(EditorLogic.fetch.ship) >= s.batteryCapacity);
                    }
                    if (capacityMet)
                        GUILayout.Label("Battery capacity is met");
                    else
                        GUILayout.Label("Not sufficient battery capacity", _errorLabel);
                    break;

                case CriterionType.Communication:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            DoubleField("Antenna Power: ", ref s.antennaPower, s.locked);
                            GUILayout.Label(" m");
                        }
                        bool powerMet = false;
                        double power = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            if (Utils.AntennaUtils.HasAntennaFlight(FlightGlobals.ActiveVessel))
                                GUILayout.Label("Antenna(s) are available");
                            else
                                GUILayout.Label("No antenna are available", _errorLabel);
                            power = Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel);
                            powerMet = (Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel) >= s.antennaPower);
                        }
                        else
                        {
                            if (Utils.AntennaUtils.HasAntennaEditor(EditorLogic.fetch.ship))
                                GUILayout.Label("Antenna(s) are available");
                            else
                                GUILayout.Label("No antenna are available", _errorLabel);
                            power = Utils.AntennaUtils.GetTotalAntennaPowerEditor(EditorLogic.fetch.ship);
                            powerMet = (power >= s.antennaPower);
                        }
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label("Available antenna power: " + Utils.AntennaUtils.FormatPower(power));
                        using (new GUILayout.HorizontalScope())
                        {
                            if (powerMet)
                                GUILayout.Label("Antenna power is met");
                            else
                                GUILayout.Label("Insufficient antenna power", _errorLabel);
                        }
                    }
                    break;

                case CriterionType.SolarPanels:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            FloatField("Solar Charge Rate: ", ref s.solarChargeRate, s.locked);
                        }
                        bool chargeRateMet = false;
                        float chargeRate = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);

                            if (sgs.TotalSolarParts > 0)
                                GUILayout.Label("Solar Panel(s) are available");
                            else
                                GUILayout.Label("No Solar Panels are available", _errorLabel);

                            chargeRate = (float)sgs.TotalECps;
                            chargeRateMet = (chargeRate >= s.solarChargeRate);

                        }
                        else
                        {
                            SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                            if (sgs.TotalSolarParts > 0)
                                GUILayout.Label("Solar Panel(s) are available");
                            else
                                GUILayout.Label("No Solar Panels are available", _errorLabel);

                            if (EditorLogic.fetch.ship != null)
                            {
                                chargeRate = (float)sgs.TotalECps;
                                chargeRateMet = (chargeRate >= s.solarChargeRate);
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label("Max Solar Charge Rate: " + chargeRate);
                        using (new GUILayout.HorizontalScope())
                        {
                            if (chargeRateMet)
                                GUILayout.Label("Charge rate is met");
                            else
                                GUILayout.Label("Insufficient charge rate", _errorLabel);
                        }
                    }

                    break;

                case CriterionType.FuelCells:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            FloatField("Fuel Cell Charge Rate: ", ref s.fuelCellChargeRate, s.locked);
                        }
                        bool chargeRateMet = false;
                        float chargeRate = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            FuelCellGenerationSummary fcgs = FuelCellUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);

                            if (fcgs.TotalFuelCellParts > 0)
                                GUILayout.Label("Fuel Cells are available");
                            else
                                GUILayout.Label("No Fuel Cells are available", _errorLabel);

                            chargeRate = (float)fcgs.TotalECps;
                            chargeRateMet = (chargeRate >= s.fuelCellChargeRate);

                        }
                        else
                        {
                            FuelCellGenerationSummary fcgs = FuelCellUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                            if (fcgs.TotalFuelCellParts > 0)
                                GUILayout.Label("Solar Panel(s) are available");
                            else
                                GUILayout.Label("No Solar Panels are available", _errorLabel);

                            if (EditorLogic.fetch.ship != null)
                            {
                                chargeRate = (float)fcgs.TotalECps;
                                chargeRateMet = (chargeRate >= s.fuelCellChargeRate);
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label("Max Fuel Cell Charge Rate: " + chargeRate);
                        using (new GUILayout.HorizontalScope())
                        {
                            if (chargeRateMet)
                                GUILayout.Label("Charge rate is met");
                            else
                                GUILayout.Label("Insufficient charge rate", _errorLabel);
                        }
                    }

                    break;

                case CriterionType.Radiators:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            FloatField("Radiator Cooling Rate: ", ref s.radiatorCoolingRate, s.locked);
                        }
                        bool coolingRateMet = false;
                        float coolingRate = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            RadiatorCoolingSummary rcs = RadiatorUtils.GetEstimatedCoolingFlight(FlightGlobals.ActiveVessel);

                            if (rcs.TotalRadiatorParts > 0)
                                GUILayout.Label("Radiator(s) are available");
                            else
                                GUILayout.Label("No Radiators are available", _errorLabel);

                            coolingRate = (float)rcs.TotalKW;
                            coolingRateMet = (coolingRate >= s.radiatorCoolingRate);
                        }

                        else
                        {
                            RadiatorCoolingSummary rcs = RadiatorUtils.GetEstimatedCoolingEditor(EditorLogic.fetch.ship);
                            if (rcs.TotalRadiatorParts > 0)
                                GUILayout.Label("Radiator(s) are available");
                            else
                                GUILayout.Label("No Radiators are available", _errorLabel);

                            if (EditorLogic.fetch.ship != null)
                            {
                                coolingRate = (float)rcs.TotalKW;
                                coolingRateMet = (coolingRate >= s.radiatorCoolingRate);
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                            GUILayout.Label("Max Radiator Cooling Rate: " + coolingRate);
                        using (new GUILayout.HorizontalScope())
                        {
                            if (coolingRateMet)
                                GUILayout.Label("Cooling rate is met");
                            else
                                GUILayout.Label("Insufficient cooling rate", _errorLabel);
                        }
                    }

                    break;

                case CriterionType.Lights:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            IntField("Spotlights: ", ref s.spotlights, s.locked);
                        }
                        bool spotlightsMet = false;
                        int spotlights = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            int totalSpotlights = LightUtils.CountSpotlightsFlight(FlightGlobals.ActiveVessel);
                            //int activeSpotlights = LightUtils.CountActiveSpotlightsFlight(FlightGlobals.ActiveVessel);

                            if (totalSpotlights > 0)
                                GUILayout.Label("Spotlight(s) are available");
                            else
                                GUILayout.Label("No spotlights are available", _errorLabel);

                            spotlights = totalSpotlights;
                            spotlightsMet = (spotlights >= s.spotlights);
                        }

                        else
                        {
                            int editorSpotlights = LightUtils.CountSpotlightsEditor(EditorLogic.fetch.ship);

                            if (editorSpotlights > 0)
                                GUILayout.Label("Spotlight(s) are available");
                            else
                                GUILayout.Label("No spotlights are available", _errorLabel);

                            if (EditorLogic.fetch.ship != null)
                            {
                                spotlights = editorSpotlights;
                                spotlightsMet = (spotlights >= s.spotlights);
                            }
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            if (spotlightsMet)
                                GUILayout.Label("Spotlight count is met");
                            else
                                GUILayout.Label("Insufficient number of spotlights rate", _errorLabel);
                        }
                    }

                    break;

                case CriterionType.Parachutes:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            IntField("Parachutes: ", ref s.parachutes, s.locked);
                        }
                        bool parachutesMet = false;
                        float parachutes = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            ParachuteStateCounts psc = ParachuteUtils.GetParachuteStateCountsFlight(FlightGlobals.ActiveVessel);

                            if (psc.Total > 0)
                                GUILayout.Label("Parachutes are available");
                            else
                                GUILayout.Label("No Parachutes are available", _errorLabel);

                            parachutes = psc.Total;
                            parachutesMet = (parachutes >= s.parachutes);

                        }
                        else
                        {
                            ParachuteCapacitySummary psc = ParachuteUtils.GetParachuteCapacityEditor(EditorLogic.fetch.ship);
                            if (psc.Total > 0)
                                GUILayout.Label("Parachutes are available");
                            else
                                GUILayout.Label("No Parachutes are available", _errorLabel);

                            if (EditorLogic.fetch.ship != null)
                            {
                                parachutes = psc.Total;
                                parachutesMet = (parachutes >= s.parachutes);
                            }
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            if (parachutesMet)
                                GUILayout.Label("Parachute count is met");
                            else
                                GUILayout.Label("Insufficient Parachutes", _errorLabel);
                        }
                    }

                    break;

#warning Need to do ControlSource
                case CriterionType.ControlSource:
                    break;

                case CriterionType.ReactionWheels:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            IntField("Reaction Wheels: ", ref s.reactionWheels, s.locked);
                        }
                        bool reactionWheelsMet = false;
                        float reactionWheels = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            TorqueSummary ts = ReactionWheelUtils.GetEnabledTorqueFlight(FlightGlobals.ActiveVessel);

                            if (ts.Total > 0)
                                GUILayout.Label("Reaction Wheels are available");
                            else
                                GUILayout.Label("No Reaction Wheels are available", _errorLabel);

                            reactionWheels = ts.Total;
                            reactionWheelsMet = (reactionWheels >= s.reactionWheels);

                        }
                        else
                        {
                            TorqueSummary ts = GetNominalTorqueEditor(EditorLogic.fetch.ship);
                            if (ts.Total > 0)
                                GUILayout.Label("Reaction Wheels are available");
                            else
                                GUILayout.Label("No Reaction Wheels are available", _errorLabel);

                            if (EditorLogic.fetch.ship != null)
                            {
                                reactionWheels = ts.Total;
                                reactionWheelsMet = (reactionWheels >= s.reactionWheels);
                            }
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            if (reactionWheelsMet)
                                GUILayout.Label("Reaction Wheels count is met");
                            else
                                GUILayout.Label("Insufficient Reaction Wheels", _errorLabel);
                        }
                    }
                    break;

#warning Need to do Engines
                case CriterionType.Engines:
                    break;

#warning Need to do Flags
                case CriterionType.Flags:
                    {
                        GUILayout.Space(2);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Selected:", GUILayout.Width(60));
                            GUILayout.Label(String.IsNullOrEmpty(s.bodyAsteroidVessel) ? "(none)" : s.bodyAsteroidVessel, HighLogic.Skin.label, GUILayout.Width(250));
                            if (!s.locked)
                            {
                                if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                {
                                    OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.body);
                                }
                                if (!String.IsNullOrEmpty(s.resourceName))
                                {
                                    if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                    {
                                        s.bodyAsteroidVessel = "";
                                    }
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    break;

                case CriterionType.Dest_vessel:
                case CriterionType.Dest_asteroid:
                case CriterionType.Dest_body:
                    {
                        GUILayout.Space(2);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Selected:", GUILayout.Width(60));
                            GUILayout.Label(String.IsNullOrEmpty(s.bodyAsteroidVessel) ? "(none)" : s.bodyAsteroidVessel, HighLogic.Skin.label, GUILayout.Width(250));
                            if (!s.locked)
                            {
                                if (GUILayout.Button("Select…", GUILayout.Width(90)))
                                {
                                    switch (s.stepType)
                                    {
                                        case CriterionType.Dest_vessel:
                                            OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.vessel);
                                            break;

                                        case CriterionType.Dest_asteroid:
                                            OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.asteroid);
                                            break;

                                        case CriterionType.Dest_body:
                                            OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.body);
                                            break;
                                    }
                                }
                                if (!String.IsNullOrEmpty(s.resourceName))
                                {
                                    if (GUILayout.Button("Clear", GUILayout.Width(70)))
                                    {
                                        s.bodyAsteroidVessel = "";
                                    }
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }

                    }
                    break;


                default:
                    break;
            }
        }

        private void IntField(string label, ref int value, bool locked)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(90));
                string buf = GUILayout.TextField(value.ToString(), GUILayout.Width(120));
                int parsed;
                if (!locked && int.TryParse(buf, out parsed)) value = parsed;
                GUILayout.FlexibleSpace();
            }
        }

        private void FloatField(string label, ref float value, bool locked)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(90));
                string buf = GUILayout.TextField(value.ToString("G"), GUILayout.Width(120));
                float parsed;
                if (!locked && float.TryParse(buf, out parsed))
                    value = parsed; GUILayout.FlexibleSpace();
            }
        }

        private void DoubleField(string label, ref double value, bool locked)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(90));

                string buf = GUILayout.TextField(Utils.AntennaUtils.FormatPower(value), GUILayout.Width(120));

                if (!locked)
                {
                    if (TryParseWithSuffix(buf, out double parsed))
                    {
                        value = parsed;
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// Parses a string that may contain a suffix (k, M, G) and converts it to a float.
        /// Examples:
        ///   "500"  -> 500
        ///   "1.5k" -> 1500
        ///   "2M"   -> 2000000
        ///   "3G"   -> 3000000000
        /// </summary>
        private bool TryParseWithSuffix(string input, out double result)
        {
            result = 0f;
            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();
            char suffix = char.ToUpperInvariant(input[input.Length - 1]);

            float multiplier = 1f;
            string numericPart = input;

            switch (suffix)
            {
                case 'K':
                    multiplier = 1_000f;
                    numericPart = input.Substring(0, input.Length - 1);
                    break;
                case 'M':
                    multiplier = 1_000_000f;
                    numericPart = input.Substring(0, input.Length - 1);
                    break;
                case 'G':
                    multiplier = 1_000_000_000f;
                    numericPart = input.Substring(0, input.Length - 1);
                    break;
            }

            float baseValue;
            if (float.TryParse(numericPart, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out baseValue))
            {
                result = baseValue * multiplier;
                return true;
            }

            return false;
        }


#if false
        private void IntRangeFields(ref int min, ref int max, bool locked)
        {
            using (new GUILayout.HorizontalScope())
            {
            GUILayout.Label("Min (int)", GUILayout.Width(90));
            string minBuf = GUILayout.TextField(min.ToString(), GUILayout.Width(120));
            GUILayout.Space(12);
            GUILayout.Label("Max (int)", GUILayout.Width(90));
            string maxBuf = GUILayout.TextField(max.ToString(), GUILayout.Width(120));
            GUILayout.FlexibleSpace();
            }

            if (!locked)
            {
                int pmin, pmax;
                if (int.TryParse(minBuf, out pmin)) min = pmin;
                if (int.TryParse(maxBuf, out pmax)) max = pmax;
            }
        }
#endif

        private void FloatRangeFields(ref float min, ref float max, bool locked)
        {
            string minBuf, maxBuf;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Min (float)", GUILayout.Width(90));
                minBuf = GUILayout.TextField(min.ToString("G"), GUILayout.Width(120));
                GUILayout.Space(12);
                GUILayout.Label("Max (float)", GUILayout.Width(90));
                maxBuf = GUILayout.TextField(max.ToString("G"), GUILayout.Width(120));
                GUILayout.FlexibleSpace();
            }

            if (!locked)
            {
                float pmin, pmax;
                if (float.TryParse(minBuf, out pmin)) min = pmin;
                if (float.TryParse(maxBuf, out pmax)) max = pmax;
            }
        }



    }
}
