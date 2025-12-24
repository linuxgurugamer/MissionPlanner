using MissionPlanner.MissionPlanner;
using MissionPlanner.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static MissionPlanner.Utils.FuelCellUtils;
using static ParachuteUtils;
using static RadiatorUtils;
using static ReactionWheelUtils;
using static SolarUtils;


namespace MissionPlanner
{
    public partial class HierarchicalStepsWindow : MonoBehaviour
    {
        const int CRITERIUM_COMBO = 1;
        const int SAS_COMBO = 2;
        const int VIEW_COMBO = 3;
        const int BIOMES_COMBO = 4;
        const int ENGINETYPE_COMBO = 5;
        const int RCSTYPE_COMBO = 6;
        const int TRACKING_COMBO = 7;
        const int MANEUVER_COMBO = 8;
        const int DESTINATION_TYPE_COMBO = 9;
        const int PARTGROUP_COMBO = 10;
        const int PLANETPACK_COMBO = 11;
        const int RESOURCE_COMBO = 20; // Needs to make sure doesn't conflict with any others since this is just a base number

        // Following two vars cache this info for use by the comboboxes
        public static string[] criterionTypeStrings;
        public static string[] maneuverStrings;
        public static string[] ResourceStrings;
        public static bool vabOrganizer = false;

        Vector2 resScroll = Vector2.zero;
        Vector2 descrScroll = new Vector2();

        private void DrawDetailWindow(int id)
        {
            BringWindowForward(id);
            GUILayout.Space(6);
            if (_detailNode == null)
            {
                GUI.DragWindow(new Rect(0, 0, 10000, 10000));
                return;
            }
            var s = _detailNode.data;
            string tmpstr;

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Title:", ScaledGUILayoutWidth(60));
                tmpstr = GUILayout.TextField(s.title ?? "", _titleEdit, ScaledGUILayoutWidth(320));
                if (!s.locked)
                    s.title = tmpstr;
            }
            GUILayout.Space(6);
            GUILayout.Label("Description:");
            //tmpstr = GUILayout.TextArea(s.descr ?? "", HighLogic.Skin.textArea, GUILayout.MinHeight(60));

            descrScroll = GUILayout.BeginScrollView(descrScroll, HighLogic.Skin.textArea, GUILayout.Height(60));
            tmpstr = GUILayout.TextArea(string.IsNullOrEmpty(s.descr) ? "" : s.descr, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();



            if (!s.locked)
                s.descr = tmpstr;
            GUILayout.Space(6);
            string ErrorMessage = "";
            string StatusMessage = "";

            if (!_simpleChecklist)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Criterion Type");

                    s.stepType = (CriterionType)ComboBox.Box(CRITERIUM_COMBO, (int)s.stepType, criterionTypeStrings, this, 200, s.locked);

                    if (!s.locked)
                    {
                        int curIdx = (int)s.stepType;

                        if (GUILayout.Button("◀", ScaledGUILayoutWidth(26)))
                        {
                            s.stepType = (CriterionType)((curIdx - 1 + criterionTypeStrings.Length) % criterionTypeStrings.Length);
                            CloseAllDialogs();
                        }
                        if (GUILayout.Button("▶", ScaledGUILayoutWidth(26)))
                        {
                            s.stepType = (CriterionType)((curIdx + 1) % criterionTypeStrings.Length);
                            CloseAllDialogs();
                        }
                    }
                    GUILayout.FlexibleSpace();
                    if (_detailNode.data.stepType != CriterionType.ChecklistItem)
                    {
                        if (!s.locked && GUILayout.Button("Update Title", GUILayout.Width(120)))
                        {
                            string criteria = OneLineSummary(_detailNode);
                            if (criteria.StartsWith(s.stepType.ToString()))
                                s.title = criteria;
                            else
                                s.title = StringFormatter.BeautifyName(s.stepType.ToString()) + criteria;
                        }
                    }
                }
                GUILayout.Space(4);
                DoCriteria(s, ref ErrorMessage, ref StatusMessage);
            }
            else
                s.stepType = CriterionType.ChecklistItem;
            GUILayout.Space(10);
            GUILayout.FlexibleSpace();
            foreach (var str in ErrorMessage.Split(':'))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (!string.IsNullOrEmpty(str))
                        GUILayout.Label(str, _errorLargeLabel);
                    GUILayout.FlexibleSpace();
                }
            }
            foreach (string str in StatusMessage.Split(':'))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (!string.IsNullOrEmpty(str))
                        GUILayout.Label(str);
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.Space(10);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Close", ScaledGUILayoutWidth(100)))
                {
                    if (HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().autosave)
                        TrySaveToDisk_Internal(true);
                    _detailNode = null;
                }
                GUILayout.FlexibleSpace();
            }
            ToolTips.ShowToolTip(_detailRect);
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }


        private void DoCriteria(Step s, ref string ErrorMessage, ref string StatusMessage) //, ChecklistItem checkListItem)
        {
            switch (s.stepType)
            {
                case CriterionType.Maneuver:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Maneuver: ");
                            s.maneuver = (Maneuver)ComboBox.Box(MANEUVER_COMBO, (int)s.maneuver, maneuverStrings, this, 250, s.locked);
                            GUILayout.FlexibleSpace();
                        }
                        switch (s.maneuver)
                        {
                            case Maneuver.Launch:
                            case Maneuver.Orbit:
                                GUILayout.Space(30);
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label("Target Orbit:");
                                    GUILayout.Space(40);
                                    DoubleField("Ap: ", ref s.ap, s.locked, " km", 90);
                                    GUILayout.Space(40);
                                    if (s.peMatchesAp)
                                        s.pe = s.ap;
                                    DoubleField("Pe: ", ref s.pe, s.locked, " km", 90);
                                    GUILayout.FlexibleSpace();
                                    s.peMatchesAp = GUILayout.Toggle(s.peMatchesAp, "");
                                    GUILayout.Label("Pe == Ap");
                                    GUILayout.Space(20);
                                }
                                FloatField("Margin of Error: ", ref s.marginOfError, 1, s.locked, " %", 50);
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    ApPeFromOrbit.ApPe aPpE = ApPeFromOrbit.ComputeApPe(FlightGlobals.ActiveVessel.orbitDriver.orbit,
                                                                                        FlightGlobals.ActiveVessel.mainBody);
                                    if (FlightChecks.IsWithinPercent(aPpE.ApAltitude * 0.001f, s.ap, s.marginOfError))
                                        StatusMessage = "Ap is within the margin of error";
                                    else
                                        ErrorMessage = "Ap outside margin of error";

                                    if (FlightChecks.IsWithinPercent(aPpE.PeAltitude * 0.001f, s.pe, s.marginOfError))
                                    {
                                        StatusMessage += (StatusMessage.Length > 0) ? ", " : "";
                                        StatusMessage += "Pe is within the margin of error";
                                    }
                                    else
                                    {
                                        ErrorMessage += (ErrorMessage.Length > 0) ? ", " : "";
                                        ErrorMessage += "Pe outside margin of error";
                                    }

                                }
                                break;

                            case Maneuver.ImpactAsteroid:
                                {
                                    GUILayout.Space(30);
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Destination Asteroid: ");
                                        GUILayout.Label(String.IsNullOrEmpty(s.destAsteroid) ? "(none)" : s.destAsteroid, GUI.skin.label, ScaledGUILayoutWidth(250));
                                        if (!s.locked)
                                        {
                                            GUILayout.FlexibleSpace();
                                            if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                            {
                                                OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.asteroid);
                                            }
                                            if (!String.IsNullOrEmpty(s.destAsteroid))
                                            {
                                                if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                                {
                                                    s.destAsteroid = "";
                                                }
                                            }
                                        }
                                        else
                                            GUILayout.FlexibleSpace();
                                    }
                                }
                                break;

                            case Maneuver.InterceptAsteroid:
                                GUILayout.Space(30);
                                SelectVessel(s.locked, ref s.destAsteroid, BodyAsteroidVessel.asteroid);
                                break;

                            case Maneuver.FineTuneClosestApproach:
                            case Maneuver.InterceptVessel:
                            case Maneuver.MatchPlanesWithVessel:
                            case Maneuver.MatchVelocitiesWithVessel:
                                GUILayout.Space(30);

                                SelectVessel(s.locked, ref s.destVessel, BodyAsteroidVessel.vessel);
                                break;

                            case Maneuver.Reentry:
                            case Maneuver.Landing:
                            case Maneuver.Splashdown:
                            case Maneuver.TransferToAnotherPlanet:
                                {
                                    GUILayout.Space(30);
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Body: ");
                                        GUILayout.Label(String.IsNullOrEmpty(s.destBody) ? "(none)" : s.destBody, GUI.skin.label, ScaledGUILayoutWidth(250));
                                        if (!s.locked)
                                        {
                                            GUILayout.FlexibleSpace();
                                            using (new GUILayout.HorizontalScope())
                                            {
                                                if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                                {
                                                    OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.body);
                                                }
                                                if (!String.IsNullOrEmpty(s.destBody))
                                                {
                                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                                    {
                                                        s.destBody = "";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case Maneuver.ResourceTransfer:
                                if (s.resourceList.Count == 0)
                                {
                                    s.resourceList.Add(new ResInfo());
                                }
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(20);
                                    GUILayout.Label("Resource", ScaledGUILayoutWidth(150));
                                    GUILayout.Space(10);
                                    GUILayout.Label("Start Amt", ScaledGUILayoutWidth(90));
                                    GUILayout.Space(20);
                                    GUILayout.Label("End Amt", ScaledGUILayoutWidth(80));
                                    GUILayout.FlexibleSpace();
                                }
                                resScroll = GUILayout.BeginScrollView(resScroll, HighLogic.Skin.textArea, GUILayout.Height(200));
                                StatusMessage = "";
                                for (int i = 0; i < s.resourceList.Count; i++)
                                {
                                    ResInfo resinfo = s.resourceList[i];
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        int resId = GetResourceId(resinfo.resourceName);
                                        resId = ComboBox.Box(RESOURCE_COMBO + i, resId, ResourceStrings, this, 150, s.locked);
                                        resinfo.resourceName = ResourceStrings[resId];
                                        GUILayout.Space(10);

                                        resinfo.startingAmount = FloatField("", resinfo.startingAmount, 0, s.locked, width: 100, flex: false);

                                        GUILayout.Space(10);
                                        resinfo.endingAmount = FloatField("", resinfo.endingAmount, 0, s.locked, width: 100, flex: false);

                                        if (resinfo.startingAmount > resinfo.endingAmount)
                                            resinfo.direction = Direction.StartToEnd;
                                        else
                                            resinfo.direction = Direction.EndToStart;

                                        GUILayout.Space(10);
                                        GUILayout.Label(StringFormatter.BeautifyName(resinfo.direction.ToString()));
                                        GUILayout.FlexibleSpace();
                                        if (GUILayout.Button("<B>+</B>", GUILayout.Width(20)))
                                            s.resourceList.Add(new ResInfo());
                                        if (s.resourceList.Count > 1)
                                        {
                                            if (GUILayout.Button("<B>-</B>", GUILayout.Width(20)))
                                                s.resourceList.Remove(s.resourceList[i]);
                                        }
                                    }
                                }
                                GUILayout.EndScrollView();
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                    {
                                        s.resourceList.Clear();
                                    }
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("Sort"))
                                    {
                                        s.resourceList = s.resourceList.OrderBy(r => r.resourceName, StringComparer.OrdinalIgnoreCase).ToList();
                                    }
                                    GUILayout.FlexibleSpace();
                                }

                                break;

                            default: break;
                        }
                    }
                    break;

                case CriterionType.Module:
                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                        GUILayout.Label(String.IsNullOrEmpty(s.moduleName) ? "(none)" : s.moduleName, HighLogic.Skin.label, ScaledGUILayoutWidth(250));
                        if (!s.locked)
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                OpenModulePicker(_detailNode);
                            if (!String.IsNullOrEmpty(s.moduleName))
                            {
                                if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                {
                                    s.moduleName = "";
                                }
                            }
                        }
                        if (HighLogic.LoadedSceneIsEditor)
                        {
                            ErrorMessage = "Part Module not found on vessel";
                            foreach (var p in EditorLogic.fetch.ship.parts)
                            {
                                foreach (var m in p.Modules)
                                {
                                    if (m.moduleName == s.moduleName)
                                    {
                                        ErrorMessage = "";
                                        StatusMessage = "Part Module found on vessel";
                                        break;
                                    }

                                }
                            }
                        }
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            ErrorMessage = "Part Module not found on vessel";
                            foreach (var p in FlightGlobals.ActiveVessel.Parts)
                            {
                                foreach (var m in p.Modules)
                                {
                                    if (m.moduleName == s.moduleName)
                                    {
                                        ErrorMessage = "";
                                        StatusMessage = "Part Module found on vessel";
                                        break;
                                    }

                                }
                            }

                        }
                    }
                    break;

                case CriterionType.VABOrganizerCategory:
                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                        GUILayout.Label(StringFormatter.BeautifyName(s.vabCategory));
                        if (!s.locked)
                        {
                            GUILayout.FlexibleSpace();

                            // Need to make new picker for this
                            if (vabOrganizer)
                            {
                                if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                    OpenCategoryPicker(_detailNode);

                                if (!String.IsNullOrEmpty(s.vabCategory))
                                {
                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                    {
                                        s.vabCategory = "";
                                    }
                                }
                            }
                        }
                    }
                    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null && vabOrganizer)
                    {
                        bool rc = false;
                        using (new GUILayout.HorizontalScope())
                        {
                            foreach (var p in FlightGlobals.ActiveVessel.parts)
                            {
                                rc = VABOrganizerUtils.IsPartInCategory(s.vabCategory, p.partInfo.name);
                                if (rc)
                                    break;
                            }

                            if (rc)
                            {
                                StatusMessage = $"There are parts in the {s.vabCategory} on the vessel";
                                //using (new GUILayout.HorizontalScope())
                                //    GUILayout.Label(StatusMessage, GUI.skin.label);
                            }
                            else
                            {
                                ErrorMessage = $"There are no parts in the {s.vabCategory} on the vessel";
                                //using (new GUILayout.HorizontalScope())
                                //    GUILayout.Label(ErrorMessage, _errorLabel);
                            }
                        }

                    }
                    if (HighLogic.LoadedSceneIsEditor && vabOrganizer)
                    {
                        if (s.vabCategory == "")
                        {
                            ErrorMessage = $"No Category has been selected";

                        }
                        else
                        {
                            bool rc = false;
                            using (new GUILayout.HorizontalScope())
                            {
                                foreach (var p in EditorLogic.fetch.ship.parts)
                                {
                                    rc = VABOrganizerUtils.IsPartInCategory(s.vabCategory, p.partInfo.name);
                                    if (rc)
                                        break;
                                }

                                if (rc)
                                {
                                    StatusMessage = $"Category: {s.vabCategory} is on vessel";
                                }
                                else
                                {
                                    ErrorMessage = $"Category: {s.vabCategory} is not on vessel";
                                }
                            }
                        }
                    }
                    break;

                case CriterionType.CrewMemberTrait:
                    {
                        GUILayout.Space(2);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                            GUILayout.Label(String.IsNullOrEmpty(s.traitName) ? "(none)" : s.traitName, HighLogic.Skin.label, ScaledGUILayoutWidth(250));
                            if (!s.locked)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                    OpenTraitPicker(_detailNode);
                                if (!String.IsNullOrEmpty(s.traitName))
                                {
                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                    {
                                        s.traitName = "";
                                    }
                                }
                            }
                        }
                        if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(4);
                                using (new GUILayout.HorizontalScope())
                                {
                                    if (CrewUtils.VesselHasTrait(FlightGlobals.ActiveVessel, s.traitName))
                                    {
                                        var c = CrewUtils.GetCrewWithTrait(FlightGlobals.ActiveVessel, s.traitName);
                                        StatusMessage = string.Format("There are {0} crew with the trait: {1}", c.Length, s.traitName);
                                        //GUILayout.Label(string.Format("There are {0} crew with the trait: {1}", c.Length, s.traitName));
                                    }
                                    else
                                    {
                                        ErrorMessage = $"There are no crew with the trait: {s.traitName}";
                                        //GUILayout.Label($"There are no crew with the trait: {s.traitName}", _errorLabel);
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }

                    }
                    break;

#if false
                case CriterionType.Number:
                    FloatField("(float)", ref s.number, 0, s.locked);
                    break;

                case CriterionType.Range:
                    FloatRangeFields(ref s.minFloatRange, ref s.maxFloatRange, s.locked);
                    if (s.minFloatRange > s.maxFloatRange)
                        GUILayout.Label("Warning: minFloatRange > maxFloatRange.", _tinyLabel);
                    break;
#endif

                case CriterionType.CrewCount:
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Crew Count: ");
                        IntField("", ref s.crewCount, s.locked);
                    }
                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        // Sum the crew capacities of all parts
                        int totalSeats = EditorLogic.fetch.ship.parts
                            .Where(p => p != null)
                            .Sum(p => p.CrewCapacity);
                        if (totalSeats < s.crewCount)
                            ErrorMessage = "Not enough crew seats";
                        else
                            StatusMessage = "Crew Seating Minimum met";
                    }
                    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                    {
                        GUILayout.Space(4);
                        if (FlightChecks.CheckCrew(s, out int crew))
                        {
                            StatusMessage = string.Format("Crew count of {0} is ok", crew);
                        }
                        else
                        {
                            ErrorMessage = string.Format("Crew count of {0} is below the miniumum required: {1}", crew, s.crewCount);
                        }
                    }
                    break;

                case CriterionType.PartGroup:
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Part Group:");
                        s.partGroup = (PartGroup)ComboBox.Box(PARTGROUP_COMBO, (int)s.partGroup, Initialization.partGroupDisplayAr, this, 150, s.locked);
                    }
                    using (new GUILayout.VerticalScope())
                    {
                        switch (s.partGroup)
                        {
                            case PartGroup.Batteries:
                                {
                                    FloatField("Min Battery Capacity: ", ref s.batteryCapacity, 0, s.locked, "EC");
                                    bool capacityMet = false;
                                    double availBatCap = 0;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        if (Utils.BatteryUtils.HasBatteryFlight(FlightGlobals.ActiveVessel))
                                            GUILayout.Label("Battery(s) are available");
                                        else
                                            GUILayout.Label("No batteries are available", _errorLabel);
                                        availBatCap = BatteryUtils.GetTotalBatteryCapacityFlight(FlightGlobals.ActiveVessel);
                                        capacityMet = (availBatCap >= s.batteryCapacity);
                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            if (Utils.BatteryUtils.HasBatteryEditor(EditorLogic.fetch.ship))
                                                StatusMessage = "Battery(s) are available";
                                            else
                                                ErrorMessage = "No batteries are available";
                                            availBatCap = BatteryUtils.GetTotalBatteryCapacityEditor(EditorLogic.fetch.ship);
                                            capacityMet = (availBatCap >= s.batteryCapacity);
                                        }
                                    }
                                    if (s.batteryCapacity > 0 &&
                                        (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                                    {
                                        GUILayout.Label($"Available Battery Capacity: {availBatCap}");
                                        if (capacityMet)
                                            StatusMessage = "Battery capacity is met";
                                        else
                                            ErrorMessage = "Not sufficient battery capacity";
                                    }
                                }
                                break;

                            case PartGroup.Communication:
                                {
                                    DoubleField("Antenna Power: ", ref s.antennaPower, s.locked, "");
                                    bool powerMet = false;
                                    double power = 0;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
#if false
                            if (Utils.AntennaUtils.HasAntennaFlight(FlightGlobals.ActiveVessel))
                                StatusMessage = "Antenna(s) are available";
                            else
                                GUILayout.Label("No antenna are available", _errorLabel);
#endif
                                        power = Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel);
                                        powerMet = (Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel) >= s.antennaPower);
                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
#if false
                                if (Utils.AntennaUtils.HasAntennaEditor(EditorLogic.fetch.ship))
                                    GUILayout.Label("Antenna(s) are available");
                                else
                                    GUILayout.Label("No antenna are available", _errorLabel);
#endif
                                            power = Utils.AntennaUtils.GetTotalAntennaPowerEditor(EditorLogic.fetch.ship);
                                            powerMet = (power >= s.antennaPower);
                                        }
                                    }
                                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                    {
                                        if (power > 0)
                                        {
                                            using (new GUILayout.HorizontalScope())
                                                GUILayout.Label("Available antenna power: " + Utils.AntennaUtils.FormatPower(power));
                                            using (new GUILayout.HorizontalScope())
                                            {
                                                if (powerMet)
                                                    StatusMessage = "Antenna power is met";
                                                else
                                                {
                                                    ErrorMessage = "Insufficient antenna power";
                                                    StatusMessage = "Antenna(s) are available";
                                                }
                                            }
                                        }
                                        else
                                            ErrorMessage = "No Antennas are available";
                                    }
                                }
                                break;

                            case PartGroup.ControlSource:
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label("Minimum Quantity: ");
                                    IntField("", ref s.controlSourceQty, s.locked);
                                }


                                if (HighLogic.LoadedSceneIsEditor)
                                {
                                    int i = PartLookupUtils.ShipModulesCount<ModuleCommand>(EditorLogic.fetch.ship);
                                    if (i >= s.controlSourceQty)
                                    {
                                        StatusMessage = "Sufficient Control Sources found";
                                    }
                                    else
                                    {
                                        if (i > 0)
                                            ErrorMessage = "Insufficient Control Sources found";
                                        else
                                            ErrorMessage = "No Control Sources found";

                                    }
                                }
                                else
                                if (HighLogic.LoadedSceneIsFlight)
                                {
                                    int i = PartLookupUtils.ShipModulesCount<ModuleCommand>(FlightGlobals.ActiveVessel);
                                    if (i >= s.controlSourceQty)
                                    {
                                        StatusMessage = "Sufficient Control Sources found";
                                    }
                                    else
                                    {
                                        if (i > 0)
                                            ErrorMessage = "Insufficient Control Sources found";
                                        else
                                            ErrorMessage = "No Control Sources found";

                                    }
                                }
                                break;

                            case PartGroup.DockingPort:
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Minimum Quantity: ");
                                        IntField("", ref s.dockingPortQty, s.locked);
                                    }


                                    if (HighLogic.LoadedSceneIsEditor)
                                    {
                                        int i = DockingPortUtils.GetDockingParts(EditorLogic.fetch.ship).Count;
                                        if (i >= s.dockingPortQty)
                                        {
                                            StatusMessage = "Sufficient Docking Ports found";
                                        }
                                        else
                                        {
                                            if (i > 0)
                                                ErrorMessage = "Insufficient Docking Ports found";
                                            else
                                                ErrorMessage = "No Docking Ports found";
                                        }
                                    }
                                    else
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        int i = DockingPortUtils.GetDockingParts(FlightGlobals.ActiveVessel).Count;
                                        if (i >= s.dockingPortQty)
                                        {
                                            StatusMessage = "Sufficient Docking Ports found";
                                        }
                                        else
                                        {
                                            if (i > 0)
                                                ErrorMessage = "Insufficient Docking Ports found";
                                            else
                                                ErrorMessage = "No Docking Ports found";
                                        }

                                    }
                                    break;
                                }

                            case PartGroup.Drills:
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Minimum Quantity: ");
                                        IntField("", ref s.drillQty, s.locked);
                                    }


                                    if (HighLogic.LoadedSceneIsEditor)
                                    {
                                        int i = DrillUtils.GetDrillParts(EditorLogic.fetch.ship).Count;
                                        if (i >= s.drillQty)
                                        {
                                            StatusMessage = "Sufficient Drills found";
                                        }
                                        else
                                        {
                                            if (i > 0)
                                                ErrorMessage = "Insufficient Drills found";
                                            else
                                                ErrorMessage = "No Drills found";
                                        }
                                    }
                                    else
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        int i = DrillUtils.GetDrillParts(FlightGlobals.ActiveVessel).Count;
                                        if (i >= s.drillQty)
                                        {
                                            StatusMessage = "Sufficient Drills found";
                                        }
                                        else
                                        {
                                            if (i > 0)
                                                ErrorMessage = "Insufficient Drills found";
                                            else
                                                ErrorMessage = "No Drills found";
                                        }

                                    }
                                    break;
                                }

                            case PartGroup.Engines:
                                {
                                    EngineTypeInfo eti = null;
                                    bool firstStage = s.stage >= StageInfo.StageCount;
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Engine: ");
                                        IntField("", ref s.engineQty, s.locked);
                                    }

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Engine Type: ", GUILayout.Width(120));
                                        int engineType = 0;
                                        for (int i = 0; i < Initialization.engineTypesAr.Length; i++)
                                        {
                                            if (Initialization.engineTypesAr[i] == s.engineType)
                                            {
                                                engineType = i;
                                                break;
                                            }
                                        }
                                        var old = engineType;
                                        engineType = ComboBox.Box(ENGINETYPE_COMBO, engineType, Initialization.engineTypesDisplayAr, this, 300, s.locked);
                                        if (old != engineType || s.engineResourceList.Count == 0)
                                        {
                                            s.engineType = Initialization.engineTypesAr[engineType];
                                            eti = Initialization.engineTypeDict[s.engineType];
                                            s.engineResourceList.Clear();
                                            foreach (var p in eti.Propellants)
                                                s.engineResourceList.Add(new ResInfo(p));
                                        }

                                        GUILayout.FlexibleSpace();
                                        int realStage = (s.stage <= StageInfo.StageCount - 1) ? s.stage : StageInfo.StageCount - 1;

                                        bool f = GUILayout.Toggle(firstStage, "");
                                        GUILayout.Label("First Stage");
                                        if (f && !firstStage)
                                        {
                                            firstStage = true;
                                            s.stage = int.MaxValue;
                                        }
                                        if (firstStage && !f)
                                        {
                                            firstStage = false;
                                            s.stage = 0;
                                        }
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        s.engineGimbaled = GUILayout.Toggle(s.engineGimbaled, "");
                                        GUILayout.Label("Gimbaled");
                                        GUILayout.FlexibleSpace();
                                        if (deltaVTableAvailable)
                                        {
                                            if (GUILayout.Button("  Delta V:"))
                                            {
                                                OpenDeltaVPicker(_detailNode);
                                            }
                                        }
                                        else
                                            GUILayout.Label("Delta V:");
                                        DoubleField("", ref s.deltaV, s.locked, "");
                                        GUILayout.FlexibleSpace();

                                        FloatField("TWR:", ref s.TWR, 2, s.locked, width: 50);

                                        if (!firstStage)
                                        {
                                            IntField(new GUIContent("Stage:"), ref s.stage, s.locked, 50, 50);
                                        }
                                        else
                                        {
                                            GUILayout.Label("Stage:", GUILayout.Width(50));
                                            GUILayout.Label("First Stage");
                                        }
                                        GUILayout.FlexibleSpace();
                                        s.asl = GUILayout.Toggle(s.asl, new GUIContent("", "Enable for Sea Level values"));
                                        GUILayout.Label(new GUIContent("ASL", "Enable for Sea Level values"));
                                    }

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Resource", ScaledGUILayoutWidth(150));
                                        GUILayout.Space(10);
                                        GUILayout.Label("Min Amt", ScaledGUILayoutWidth(90));
                                        GUILayout.Space(20);
                                        GUILayout.Label("Min Capacity", ScaledGUILayoutWidth(100));
                                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label("On Vessel", ScaledGUILayoutWidth(100));
                                        }
                                        GUILayout.FlexibleSpace();
                                    }

                                    StatusMessage = "All Resource Capacities/Amounts Met";
                                    string status = "met";

                                    foreach (var resinfo in s.engineResourceList)
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Label(resinfo.resourceName, GUILayout.Width(150));
                                            GUILayout.Space(10);
                                            FloatField("", ref resinfo.resourceAmount, 0, s.locked, width: 100, flex: false);
                                            GUILayout.Space(10);
                                            FloatField("", ref resinfo.resourceCapacity, 0, s.locked, width: 100, flex: false);
                                            resinfo.resourceCapacity = Math.Max(resinfo.resourceCapacity, resinfo.resourceAmount);
                                            GUILayout.Space(20);
                                            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                            {
                                                s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                                                if (amt >= resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                                {
                                                    GUILayout.Label("Met");
                                                }
                                                else
                                                {
                                                    if (amt < resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                                    {
                                                        GUILayout.Label("Partial");
                                                        if (status == "met")
                                                        {
                                                            status = "partial";
                                                            StatusMessage = "";
                                                            ErrorMessage = "All Resource Available and Capacities Met";
                                                        }
                                                    }
                                                    if (amt >= resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                                    {
                                                        GUILayout.Label("Partial");// , _errorLabel;
                                                        if (status == "met")
                                                        {
                                                            status = "partial";
                                                            StatusMessage = "";
                                                            ErrorMessage = "Available Met, Capacity Unmet";
                                                        }
                                                    }
                                                    if (amt < resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                                    {
                                                        GUILayout.Label("Unmet", _errorLabel);
                                                        StatusMessage = "";
                                                        ErrorMessage = "Resource Available and Capacities Unmet";
                                                    }
                                                }
                                            }
                                            GUILayout.FlexibleSpace();
                                        }
                                    }

                                    List<Part> partsList = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts :
                                        (HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel.parts : null);
                                    if (partsList != null)
                                    {
                                        if (EngineTypeMatcher.PartsHaveEngineType(partsList, s.engineType))
                                            StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Vessel has correct engine type";
                                        else
                                            ErrorMessage += (ErrorMessage.Length > 0 ? ":" : "") + "Vessel missing correct engine type";
                                    }

                                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                    {
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

                                        GUILayout.Label("Calculated Delta V: ", ScaledGUILayoutWidth(150));
                                        GUILayout.Label(dV.ToString("F0"));
                                        GUILayout.Label("Calculated TWR: ", ScaledGUILayoutWidth(150));
                                        GUILayout.Label(twr.ToString("F2"));

                                        if (dV >= s.deltaV)
                                        {
                                            if (StatusMessage.Length > 0)
                                                StatusMessage += ":";
                                            StatusMessage += "Sufficient Delta V is available";
                                        }
                                        else
                                        {
                                            if (ErrorMessage.Length > 0)
                                                ErrorMessage += ":";
                                            ErrorMessage += "Not sufficient Delta V available";
                                        }

                                        if (StageInfo.TWRASL(realStage) >= s.TWR)
                                        {
                                            if (StatusMessage.Length > 0)
                                                StatusMessage += ":";
                                            StatusMessage += "Sufficient TWR is available";
                                        }
                                        else
                                        {
                                            if (ErrorMessage.Length > 0)
                                                ErrorMessage += ":";
                                            ErrorMessage += "TWR too low";
                                        }
                                    }
                                }
                                break;

                            case PartGroup.FuelCells:
                                {
                                    FloatField("Fuel Cell Charge Rate: ", ref s.fuelCellChargeRate, 0, s.locked);
                                    bool chargeRateMet = false;
                                    float chargeRate = 0;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        FuelCellGenerationSummary fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);

                                        if (fcgs.TotalFuelCellParts > 0)
                                            StatusMessage = "Fuel Cells are available";
                                        else
                                        {
                                            ErrorMessage = "No Fuel Cells are available";
                                            break;
                                        }
                                        chargeRate = (float)fcgs.TotalECps;
                                        chargeRateMet = (chargeRate >= s.fuelCellChargeRate);

                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            FuelCellGenerationSummary fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                            if (fcgs.TotalFuelCellParts > 0)
                                                StatusMessage = "Fuel Cells are available";
                                            else
                                            {
                                                ErrorMessage = "No Fuel Cells are available";
                                                break;
                                            }

                                            if (EditorLogic.fetch.ship != null)
                                            {
                                                chargeRate = (float)fcgs.TotalECps;
                                                chargeRateMet = (chargeRate >= s.fuelCellChargeRate);
                                            }
                                        }
                                    }
                                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                    {
                                        if (s.fuelCellChargeRate > 0)
                                        {
                                            using (new GUILayout.HorizontalScope())
                                                GUILayout.Label("Max Available Fuel Cell Charge Rate: " + chargeRate);
                                            if (chargeRateMet)
                                                StatusMessage = "Charge rate is met";
                                            else
                                                ErrorMessage = "Insufficient charge rate";
                                        }
                                    }
                                }
                                break;

                            case PartGroup.Generators:
                                {
                                    FloatField("Generator Charge Rate: ", ref s.generatorChargeRate, 0, s.locked);
                                    bool chargeRateMet = false;
                                    float chargeRate = 0;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        GeneratorUtils.GeneratorSummary ggs = GeneratorUtils.GetTotalECGeneratorsFlight(FlightGlobals.ActiveVessel);

                                        if (ggs.generatorCnt > 0)
                                            StatusMessage = "Generator(s) are available";
                                        else
                                        {
                                            ErrorMessage = "No Generator(s) are available";
                                            break;
                                        }

                                        chargeRate = (float)ggs.TotalECps;
                                        chargeRateMet = (chargeRate >= s.generatorChargeRate);

                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            GeneratorUtils.GeneratorSummary ggs = GeneratorUtils.GetTotalECGeneratorsEditor(EditorLogic.fetch.ship);
                                            if (ggs.generatorCnt > 0)
                                                StatusMessage = "Generator(s) are available";
                                            else
                                            {
                                                ErrorMessage = "No Generator(s) are available";
                                                break;
                                            }

                                            chargeRate = (float)ggs.TotalECps;
                                            chargeRateMet = (chargeRate >= s.generatorChargeRate);
                                        }
                                    }
                                    if (s.generatorChargeRate > 0 &&
                                        (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                                    {
                                        using (new GUILayout.HorizontalScope())
                                            GUILayout.Label("Max Available Generator Charge Rate: " + chargeRate);

                                        if (chargeRateMet)
                                            StatusMessage = "Charge rate is met";
                                        else
                                            ErrorMessage = "Insufficient charge rate";

                                    }
                                }
                                break;

                            case PartGroup.Lights:
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        IntField("Spotlights: ", ref s.spotlights, s.locked);
                                    }
                                    bool spotlightsMet = false;
                                    int spotlights;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        int totalSpotlights = Utils.LightUtils.CountSpotlightsFlight(FlightGlobals.ActiveVessel);

                                        spotlights = totalSpotlights;
                                        spotlightsMet = (spotlights >= s.spotlights);
                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            if (EditorLogic.fetch.ship != null)
                                            {
                                                int editorSpotlights = Utils.LightUtils.CountSpotlightsEditor(EditorLogic.fetch.ship);

                                                spotlights = editorSpotlights;
                                                spotlightsMet = (spotlights >= s.spotlights);
                                            }
                                        }
                                    }
                                    if (s.spotlights > 0 &&
                                        (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            if (spotlightsMet)
                                                StatusMessage = "Spotlight count is met";
                                            else
                                                ErrorMessage = "Insufficient number of spotlights rate";
                                        }
                                    }
                                }
                                break;

                            case PartGroup.Parachutes:
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        IntField("Parachutes: ", ref s.parachutes, s.locked);
                                    }
                                    bool parachutesMet = false;
                                    float parachutes;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        ParachuteStateCounts psc = ParachuteUtils.GetParachuteStateCountsFlight(FlightGlobals.ActiveVessel);

                                        parachutes = psc.Total;
                                        parachutesMet = (parachutes >= s.parachutes);

                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            ParachuteCapacitySummary psc = ParachuteUtils.GetParachuteCapacityEditor(EditorLogic.fetch.ship);

                                            if (EditorLogic.fetch.ship != null)
                                            {
                                                parachutes = psc.Total;
                                                parachutesMet = (parachutes >= s.parachutes);
                                            }
                                        }
                                    }
                                    if (s.parachutes > 0 &&
                                        (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            if (parachutesMet)
                                                StatusMessage = "Parachute count is met";
                                            else
                                                ErrorMessage = "Insufficient Parachutes";
                                        }
                                    }
                                }
                                break;

                            case PartGroup.Radiators:
                                {
                                    FloatField("Radiator Cooling Rate: ", ref s.radiatorCoolingRate, 0, s.locked);
                                    bool coolingRateMet = false;
                                    float coolingRate = 0;
                                    RadiatorCoolingSummary rcs = null;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        rcs = RadiatorUtils.GetEstimatedCoolingFlight(FlightGlobals.ActiveVessel);

                                        coolingRate = (float)rcs.TotalKW;
                                        coolingRateMet = (coolingRate >= s.radiatorCoolingRate);
                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            rcs = RadiatorUtils.GetEstimatedCoolingEditor(EditorLogic.fetch.ship);

                                            if (EditorLogic.fetch.ship != null)
                                            {
                                                coolingRate = (float)rcs.TotalKW;
                                                coolingRateMet = (coolingRate >= s.radiatorCoolingRate);
                                            }
                                        }
                                    }

                                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                    {
                                        if (rcs.TotalRadiatorParts == 0)
                                            ErrorMessage = "No Radiators are available";
                                        else
                                        {
                                            //if (s.radiatorCoolingRate > 0)
                                            {
                                                using (new GUILayout.HorizontalScope())
                                                    GUILayout.Label("Max Radiator Cooling Rate: " + coolingRate);
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    if (coolingRateMet)
                                                        StatusMessage = "Cooling rate is met";
                                                    else
                                                        ErrorMessage = "Insufficient cooling rate";
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case PartGroup.RCS:
                                {
                                    EngineTypeInfo eti = null;

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("RCS Type:", GUILayout.Width(120));
                                        int rcsType = 0;
                                        for (int i = 0; i < Initialization.rcsTypesAr.Length; i++)
                                        {
                                            if (Initialization.rcsTypesAr[i] == s.rcsType)
                                            {
                                                rcsType = i;
                                                break;
                                            }
                                        }
                                        var old = rcsType;
                                        rcsType = ComboBox.Box(RCSTYPE_COMBO, rcsType, Initialization.rcsTypesDisplayAr, this, 300, s.locked);
                                        if (old != rcsType || s.rcsResourceList.Count == 0)
                                        {
                                            s.rcsType = Initialization.rcsTypesAr[rcsType];
                                            eti = Initialization.rcsTypeDict[s.rcsType];
                                            s.rcsResourceList.Clear();
                                            foreach (var p in eti.Propellants)
                                                s.rcsResourceList.Add(new ResInfo(p));
                                        }
                                    }

                                    // Need to check for rcs of correct enginetype

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Resource", ScaledGUILayoutWidth(150));
                                        GUILayout.Space(10);
                                        GUILayout.Label("Min Amt", ScaledGUILayoutWidth(90));
                                        GUILayout.Space(20);
                                        GUILayout.Label("Min Capacity", ScaledGUILayoutWidth(100));
                                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label("On Vessel", ScaledGUILayoutWidth(100));
                                        }
                                        GUILayout.FlexibleSpace();
                                    }

                                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                        StatusMessage = "All Resource Capacities/Amounts Met";
                                    else
                                        StatusMessage = "";
                                    string status = "met";

                                    foreach (var resinfo in s.rcsResourceList)
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Label(resinfo.resourceName, GUILayout.Width(150));
                                            GUILayout.Space(10);
                                            FloatField("", ref resinfo.resourceAmount, 0, s.locked, width: 100, flex: false);
                                            GUILayout.Space(10);
                                            FloatField("", ref resinfo.resourceCapacity, 0, s.locked, width: 100, flex: false);
                                            resinfo.resourceCapacity = Math.Max(resinfo.resourceCapacity, resinfo.resourceAmount);
                                            GUILayout.Space(20);
                                            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                            {
                                                s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                                                if (amt >= resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                                {
                                                    GUILayout.Label("Met");
                                                }
                                                else
                                                {
                                                    //Log.Info($"amt: {amt}, resourceAmount: {resinfo.resourceAmount}   capacity: {capacity}, resourceCapacity: {resinfo.resourceCapacity}");
                                                    if (amt < resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                                    {
                                                        GUILayout.Label("Partial");
                                                        if (status == "met")
                                                        {
                                                            status = "partial";
                                                            StatusMessage = "";
                                                            ErrorMessage = "All Resource Available and Capacities Met";
                                                        }
                                                    }
                                                    if (amt >= resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                                    {
                                                        GUILayout.Label("Partial");// , _errorLabel;
                                                        if (status == "met")
                                                        {
                                                            status = "partial";
                                                            StatusMessage = "";
                                                            ErrorMessage = "Available Met, Capacity Unmet";
                                                        }
                                                    }
                                                    if (amt < resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                                    {
                                                        GUILayout.Label("Unmet", _errorLabel);
                                                        StatusMessage = "";
                                                        ErrorMessage = "Resource Available and Capacities Unmet";
                                                    }
                                                }
                                            }
                                            GUILayout.FlexibleSpace();
                                        }
                                    }

                                    List<Part> partsList = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts :
                                        (HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel.parts : null);
                                    if (partsList != null)
                                    {
                                        if (RCSUtils.PartsHaveRCSType(partsList, s.rcsType))
                                            StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Vessel has correct RCS type";
                                        else
                                            ErrorMessage += (ErrorMessage.Length > 0 ? ":" : "") + "Vessel missing correct RCS type";
                                    }
                                }
                                break;

                            case PartGroup.ReactionWheels:
                                {
                                    TorqueSummary ts = new TorqueSummary();

                                    using (new GUILayout.VerticalScope())
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Label("Reaction Wheels:", GUILayout.Width(120));
                                            IntField("", ref s.reactionWheels, s.locked, 160);
                                        }
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            s.torquePitchRollYawEqual = GUILayout.Toggle(s.torquePitchRollYawEqual, "");
                                            GUILayout.Label("Pitch, Roll, Yaw all Equal");
                                        }
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label("Torque Pitch:", GUILayout.Width(90));
                                            DoubleField("", ref s.torquePitch, s.locked, width: 60);
                                            if (s.torquePitchRollYawEqual)
                                            {
                                                s.torqueYaw = s.torqueRoll = s.torquePitch;
                                            }
                                        }
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label("Torque Yaw:", GUILayout.Width(90));
                                            DoubleField("", ref s.torqueYaw, s.locked || s.torquePitchRollYawEqual, width: 60);
                                        }

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label("Torque Roll:", GUILayout.Width(90));
                                            DoubleField("", ref s.torqueRoll, s.locked || s.torquePitchRollYawEqual, width: 60);
                                        }
                                        GUILayout.Space(20);

                                        bool reactionWheelsMet = false;
                                        float reactionWheels;
                                        if (HighLogic.LoadedSceneIsFlight)
                                        {
                                            ts = ReactionWheelUtils.GetEnabledTorqueFlight(FlightGlobals.ActiveVessel);

                                            if (ts.Total > 0)
                                            {
                                                StatusMessage = $"There are {ts.Total} Reaction Wheels available";
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    GUILayout.Space(20);
                                                    GUILayout.Label("Enabled Torque:");
                                                }
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    GUILayout.Space(40);
                                                    GUILayout.Label($"Pitch: {ts.Pitch}");
                                                }
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    GUILayout.Space(40);
                                                    GUILayout.Label($"Yaw:   {ts.Yaw}");
                                                }
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    GUILayout.Space(40);
                                                    GUILayout.Label($"Roll:  {ts.Roll}");
                                                }
                                            }
                                            else
                                                ErrorMessage = "No Reaction Wheels are available";

                                            reactionWheels = ts.Total;
                                            reactionWheelsMet = (reactionWheels >= s.reactionWheels);

                                        }
                                        else
                                        {
                                            if (HighLogic.LoadedSceneIsEditor)
                                            {
                                                ts = GetNominalTorqueEditor(EditorLogic.fetch.ship);
                                                if (ts.Total > 0)
                                                {
                                                    StatusMessage = $"There are {ts.Total} Reaction Wheels available";
                                                    GUILayout.Label("Total Available Torque:");
                                                    using (new GUILayout.HorizontalScope())
                                                    {
                                                        GUILayout.Space(20);
                                                        GUILayout.Label($"Pitch: {ts.Pitch}");
                                                    }
                                                    using (new GUILayout.HorizontalScope())
                                                    {
                                                        GUILayout.Space(20);
                                                        GUILayout.Label($"Yaw:   {ts.Yaw}");
                                                    }
                                                    using (new GUILayout.HorizontalScope())
                                                    {
                                                        GUILayout.Space(20);
                                                        GUILayout.Label($"Roll:  {ts.Roll}");
                                                    }

                                                }
                                                else
                                                    ErrorMessage = "No Reaction Wheels are available";
                                                if (HighLogic.LoadedSceneIsEditor)
                                                {
                                                    if (EditorLogic.fetch.ship != null)
                                                    {
                                                        reactionWheels = ts.Total;
                                                        reactionWheelsMet = (reactionWheels >= s.reactionWheels);
                                                    }
                                                }
                                            }
                                        }
                                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                        {
                                            if (s.reactionWheels > 0)
                                            {
                                                if (reactionWheelsMet)
                                                    StatusMessage = "Reaction Wheels Count Met";
                                                else
                                                    ErrorMessage = "Insufficient Reaction Wheels";
                                                if (ts.Roll >= s.torqueRoll && ts.Pitch >= s.torquePitch && ts.Yaw >= s.torqueYaw)
                                                {
                                                    StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Sufficient Torque is available";
                                                }
                                                else
                                                {
                                                    ErrorMessage += (ErrorMessage.Length > 0 ? ":" : "") + "Insufficient torque";
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case PartGroup.SolarPanels:
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        FloatField("Solar Charge Rate: ", ref s.solarChargeRate, 0, s.locked, " EC/sec");
                                        GUILayout.FlexibleSpace();
                                        GUILayout.Label("Tracking:");
                                        var old = (int)s.solarPaneltracking;
                                        var solarPaneltracking = ComboBox.Box(TRACKING_COMBO, (int)s.solarPaneltracking, SolarUtils.trackingStr, this, 150, s.locked);
                                        if (old != solarPaneltracking)
                                        {
                                            s.solarPaneltracking = (SolarUtils.Tracking)solarPaneltracking;
                                        }
                                        GUILayout.FlexibleSpace();
                                    }

                                    bool chargeRateMet = false;
                                    float chargeRate = 0;
                                    if (HighLogic.LoadedSceneIsFlight)
                                    {
                                        SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel, s.solarPaneltracking);

                                        if (sgs.TotalSolarParts > 0)
                                            StatusMessage = "Solar Panel(s) are available";
                                        else
                                            ErrorMessage = "No Solar Panels are available";

                                        chargeRate = (float)sgs.TotalECps;
                                        chargeRateMet = (chargeRate >= s.solarChargeRate);

                                    }
                                    else
                                    {
                                        if (HighLogic.LoadedSceneIsEditor)
                                        {
                                            SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                            if (sgs.TotalSolarParts > 0)
                                                StatusMessage = "Solar Panel(s) are available";
                                            else
                                            {
                                                ErrorMessage = "No Solar Panels are available";
                                                break;
                                            }
                                            if (EditorLogic.fetch.ship != null)
                                            {
                                                chargeRate = (float)sgs.TotalECps;
                                                chargeRateMet = (chargeRate >= s.solarChargeRate);
                                            }
                                        }
                                    }
                                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                    {
                                        using (new GUILayout.HorizontalScope())
                                            GUILayout.Label("Max Available Solar Charge Rate: " + chargeRate);
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            if (chargeRateMet)
                                                StatusMessage = "Charge rate is met";
                                            else
                                                ErrorMessage = "Insufficient charge rate";
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case CriterionType.Part:
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Only available parts", ScaledGUILayoutWidth(160));
                        s.partOnlyAvailable = GUILayout.Toggle(s.partOnlyAvailable, GUIContent.none, ScaledGUILayoutWidth(22));
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(2);
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                        GUILayout.Label(String.IsNullOrEmpty(s.partTitle) ? "(none)" : s.partTitle, GUI.skin.label, ScaledGUILayoutWidth(320));
                        if (!s.locked)
                        {
                            if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                OpenPartPicker(_detailNode, s.partOnlyAvailable);
                            if (!String.IsNullOrEmpty(s.partTitle))
                            {
                                if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                {
                                    s.partName = "";
                                    s.partTitle = "";
                                }
                            }
                        }
                    }

                    GUILayout.Space(4);
                    if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        using (new GUILayout.HorizontalScope())
                        {
                            if (s.CheckPart())
                                GUILayout.Label("Part is on the vessel");
                            else
                            {
                                if (s.partName != "")
                                {
                                    ErrorMessage = "Part is not on the vessel";
                                }
                                else
                                {
                                    ErrorMessage = "No part specified";
                                }
                            }
                        }
                    break;

                case CriterionType.Resource:
                    {
                        GUILayout.Space(2);
                        if (s.resourceList.Count == 0)
                        {
                            s.resourceList.Add(new ResInfo());
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Space(20);
                            GUILayout.Label("Resource", ScaledGUILayoutWidth(150));
                            GUILayout.Space(10);
                            GUILayout.Label("Min Amt", ScaledGUILayoutWidth(90));
                            GUILayout.Space(20);
                            GUILayout.Label("Min Capacity", ScaledGUILayoutWidth(80));
                            GUILayout.Space(20);
                            GUILayout.Label("Locked", ScaledGUILayoutWidth(60));
                            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                            {
                                //GUILayout.Label("On Vessel", ScaledGUILayoutWidth(100));
                                GUILayout.Label("Status");
                            }
                            GUILayout.FlexibleSpace();
                        }
                        resScroll = GUILayout.BeginScrollView(resScroll, HighLogic.Skin.textArea, GUILayout.Height(200));
                        StatusMessage = "All Resource Capacities/Amounts Met";
                        string status = "met";
                        for (int i = 0; i < s.resourceList.Count; i++)
                        {
                            ResInfo resinfo = s.resourceList[i];
                            using (new GUILayout.HorizontalScope())
                            {
                                int resId = GetResourceId(resinfo.resourceName);
                                resId = ComboBox.Box(RESOURCE_COMBO + i, resId, ResourceStrings, this, 150, s.locked);
                                resinfo.resourceName = ResourceStrings[resId];
                                GUILayout.Space(10);
                                FloatField("", ref resinfo.resourceAmount, 0, s.locked, width: 100, flex: false);
                                GUILayout.Space(10);
                                FloatField("", ref resinfo.resourceCapacity, 0, s.locked, width: 100, flex: false);
                                resinfo.resourceCapacity = Math.Max(resinfo.resourceCapacity, resinfo.resourceAmount);
                                resinfo.locked = GUILayout.Toggle(resinfo.locked, "", GUILayout.Width(40));
                                if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                {
                                    s.CheckResource(resinfo.resourceName, resinfo.locked, out double amt, out double capacity, resinfo.locked);
                                    if (amt >= resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                    {
                                        GUILayout.Label("Met");
                                    }
                                    else
                                    {
                                        if (amt < resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Partial");
                                            if (status == "met")
                                            {
                                                status = "partial";
                                                StatusMessage = "";
                                                ErrorMessage = "All Resource Available and Capacities Met";
                                            }
                                        }
                                        if (amt >= resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Partial");// , _errorLabel;
                                            if (status == "met")
                                            {
                                                status = "partial";
                                                StatusMessage = "";
                                                ErrorMessage = "Available Met, Capacity Unmet";
                                            }
                                        }
                                        if (amt < resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Unmet", _errorLabel);
                                            StatusMessage = "";
                                            ErrorMessage = "Resource Available and Capacities Unmet";
                                        }
                                    }
                                }

                                GUILayout.Space(20);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("<B>+</B>", GUILayout.Width(20)))
                                    s.resourceList.Add(new ResInfo());
                                if (s.resourceList.Count > 1)
                                {
                                    if (GUILayout.Button("<B>-</B>", GUILayout.Width(20)))
                                        s.resourceList.Remove(s.resourceList[i]);
                                }
                            }
                        }
                        GUILayout.EndScrollView();
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                            {
                                s.resourceList.Clear();
                            }
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Sort"))
                            {
                                s.resourceList = s.resourceList.OrderBy(r => r.resourceName, StringComparer.OrdinalIgnoreCase).ToList();
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    break;

#if false
                case CriterionType.Sum:
                    GUILayout.Label("Sum of children: " + s.sumOfChildNumbers.ToString("F0"));
                    break;
#endif

                case CriterionType.SAS:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Min required SAS: ");

                            if (!s.locked)
                            {
                                int x = s.minSASLevel;
                                x = ComboBox.Box(SAS_COMBO, x, SASUtils.SasLevelDescriptions, this, 350, s.locked);
                                s.minSASLevel = x;

                                GUILayout.FlexibleSpace();
                                if (s.minSASLevel > 0)
                                {
                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                    {
                                        s.minSASLevel = 0;
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                            else
                            {
                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label(SASUtils.GetSASLevelDescription(s.minSASLevel), HighLogic.Skin.label, ScaledGUILayoutWidth(350));
                                }
                            }
                        }
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            // using (new GUILayout.HorizontalScope())
                            {

                                var sasInfo = SASUtils.GetAvailableSASModes(FlightGlobals.ActiveVessel);
                                if (sasInfo != null && sasInfo.Length > 0)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(15);
                                        GUILayout.Label("SAS modes available:");
                                    }
                                    foreach (var sas in sasInfo)
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(30);
                                            GUILayout.Label(sas.ToString());
                                        }
                                    }
                                }
                                //using (new GUILayout.HorizontalScope())
                                {
                                    if (SASUtils.IsRequiredSASAvailable(s.minSASLevel, sasInfo))
                                        StatusMessage = "All required SAS levels are available";
                                    else
                                        ErrorMessage = "Not all required SAS modes available";
                                }
                            }
                        }

                        if (HighLogic.LoadedSceneIsEditor)
                        {
                            var sasInfoEditor = SASUtils.GetSASInfoEditor(EditorLogic.fetch.ship);
                            if (sasInfoEditor.HasSAS)
                            {
                                StatusMessage = "Highest SAS on vessel: " + SASUtils.GetSASLevelDescription(sasInfoEditor.HighestServiceLevel);
                                if (sasInfoEditor.HighestServiceLevel < s.minSASLevel)
                                {
                                    ErrorMessage = StatusMessage;
                                    StatusMessage = "";
                                }
                                else
                                    ErrorMessage = "No SAS is available on vessel";
                            }
                        }
                    }

                    break;

#if false
                case CriterionType.RCS:
                    {
                        EngineTypeInfo eti = null;

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("RCS Type:", GUILayout.Width(120));
                            int rcsType = 0;
                            for (int i = 0; i < Initialization.rcsTypesAr.Length; i++)
                            {
                                if (Initialization.rcsTypesAr[i] == s.rcsType)
                                {
                                    rcsType = i;
                                    break;
                                }
                            }
                            var old = rcsType;
                            rcsType = ComboBox.Box(RCSTYPE_COMBO, rcsType, Initialization.rcsTypesDisplayAr, this, 300);
                            if (old != rcsType || s.rcsResourceList.Count == 0)
                            {
                                s.rcsType = Initialization.rcsTypesAr[rcsType];
                                eti = Initialization.rcsTypeDict[s.rcsType];
                                s.rcsResourceList.Clear();
                                foreach (var p in eti.Propellants)
                                    s.rcsResourceList.Add(new ResInfo(p));
                            }
                        }

                        // Need to check for rcs of correct enginetype

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Resource", ScaledGUILayoutWidth(150));
                            GUILayout.Space(10);
                            GUILayout.Label("Min Amt", ScaledGUILayoutWidth(90));
                            GUILayout.Space(20);
                            GUILayout.Label("Min Capacity", ScaledGUILayoutWidth(100));
                            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                            {
                                GUILayout.Space(20);
                                GUILayout.Label("On Vessel", ScaledGUILayoutWidth(100));
                            }
                            GUILayout.FlexibleSpace();
                        }

                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                            StatusMessage = "All Resource Capacities/Amounts Met";
                        else
                            StatusMessage = "";
                        string status = "met";

                        foreach (var resinfo in s.rcsResourceList)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(resinfo.resourceName, GUILayout.Width(150));
                                GUILayout.Space(10);
                                FloatField("", ref resinfo.resourceAmount, 0, s.locked, width: 100, flex: false);
                                GUILayout.Space(10);
                                FloatField("", ref resinfo.resourceCapacity, 0, s.locked, width: 100, flex: false);
                                resinfo.resourceCapacity = Math.Max(resinfo.resourceCapacity, resinfo.resourceAmount);
                                GUILayout.Space(20);
                                if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                {
                                    s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                                    if (amt >= resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                    {
                                        GUILayout.Label("Met");
                                    }
                                    else
                                    {
                                        //Log.Info($"amt: {amt}, resourceAmount: {resinfo.resourceAmount}   capacity: {capacity}, resourceCapacity: {resinfo.resourceCapacity}");
                                        if (amt < resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Partial");
                                            if (status == "met")
                                            {
                                                status = "partial";
                                                StatusMessage = "";
                                                ErrorMessage = "All Resource Available and Capacities Met";
                                            }
                                        }
                                        if (amt >= resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Partial");// , _errorLabel;
                                            if (status == "met")
                                            {
                                                status = "partial";
                                                StatusMessage = "";
                                                ErrorMessage = "Available Met, Capacity Unmet";
                                            }
                                        }
                                        if (amt < resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Unmet", _errorLabel);
                                            StatusMessage = "";
                                            ErrorMessage = "Resource Available and Capacities Unmet";
                                        }
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }

                        List<Part> partsList = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts :
                            (HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel.parts : null);
                        if (partsList != null)
                        {
                            if (RCSUtils.PartsHaveRCSType(partsList, s.rcsType))
                                StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Vessel has correct RCS type";
                            else
                                ErrorMessage += (ErrorMessage.Length > 0 ? ":" : "") + "Vessel missing correct RCS type";
                        }
                    }
                   break;

                case CriterionType.Batteries:
                    {
                        FloatField("Min Battery Capacity: ", ref s.batteryCapacity, 0, s.locked, "EC");
                        bool capacityMet = false;
                        double availBatCap = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            if (Utils.BatteryUtils.HasBatteryFlight(FlightGlobals.ActiveVessel))
                                GUILayout.Label("Battery(s) are available");
                            else
                                GUILayout.Label("No batteries are available", _errorLabel);
                            availBatCap = BatteryUtils.GetTotalBatteryCapacityFlight(FlightGlobals.ActiveVessel);
                            capacityMet = (availBatCap >= s.batteryCapacity);
                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                if (Utils.BatteryUtils.HasBatteryEditor(EditorLogic.fetch.ship))
                                    StatusMessage = "Battery(s) are available";
                                else
                                    ErrorMessage = "No batteries are available";
                                availBatCap = BatteryUtils.GetTotalBatteryCapacityEditor(EditorLogic.fetch.ship);
                                capacityMet = (availBatCap >= s.batteryCapacity);
                            }
                        }
                        if (s.batteryCapacity > 0 &&
                            (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                        {
                            GUILayout.Label($"Available Battery Capacity: {availBatCap}");
                            if (capacityMet)
                                StatusMessage = "Battery capacity is met";
                            else
                                ErrorMessage = "Not sufficient battery capacity";
                        }
                    }
                    break;

                case CriterionType.DeltaV:
                    {
                        DoubleField("Minimum DeltaV in current stage: ", ref s.deltaV, s.locked, "m/sec");

                        double dV = 0;
                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
                            var info = DeltaVUtils.GetActiveStageInfo_Vac(useLaunchpadFirstStageIfPrelaunch: true);
                            dV = info.deltaV;

                            GUILayout.Label("Calculated Delta V in current stage: " + dV.ToString("F0"));
                            if (dV >= s.deltaV)
                                StatusMessage = "Sufficient Delta V is available";
                            else
                                ErrorMessage = "Not sufficient Delta V available";
                        }

                        break;
                    }
#endif

                case CriterionType.ChargeRateTotal:
                    {
                        SolarGenerationSummary sgs;
                        FuelCellGenerationSummary fcgs;
                        GeneratorUtils.GeneratorSummary ggs;

                        DoubleField("Minimum Total Charge Rate: ", ref s.chargeRateTotal, s.locked, "EC/sec");
                        sgs.TotalECps = fcgs.TotalECps = ggs.TotalECps = 0f;

                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            sgs = SolarUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel, Tracking.both);
                            fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);
                            ggs = GeneratorUtils.GetTotalECGeneratorsFlight(FlightGlobals.ActiveVessel);
                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                sgs = SolarUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                ggs = GeneratorUtils.GetTotalECGeneratorsEditor(EditorLogic.fetch.ship);
                            }
                        }
                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
                            double totalChargeRate = sgs.TotalECps + fcgs.TotalECps + ggs.TotalECps;
                            using (new GUILayout.HorizontalScope())
                                GUILayout.Label("Total Available Charge Rate: " + totalChargeRate + " EC/sec");
                            if (totalChargeRate >= s.chargeRateTotal)
                                StatusMessage = "Minimum Charge Rate is met";
                            else
                            {
                                ErrorMessage = "Not sufficient charge rate available";
                                StatusMessage = "EC Generation Parts installed";
                            }
                        }
                    }
                    break;

#if false
                case CriterionType.Communication:
                    {
                        DoubleField("Antenna Power: ", ref s.antennaPower, s.locked, "");
                        bool powerMet = false;
                        double power = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
#if false
                            if (Utils.AntennaUtils.HasAntennaFlight(FlightGlobals.ActiveVessel))
                                StatusMessage = "Antenna(s) are available";
                            else
                                GUILayout.Label("No antenna are available", _errorLabel);
#endif
                            power = Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel);
                            powerMet = (Utils.AntennaUtils.GetTotalAntennaPowerFlight(FlightGlobals.ActiveVessel) >= s.antennaPower);
                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
#if false
                                if (Utils.AntennaUtils.HasAntennaEditor(EditorLogic.fetch.ship))
                                    GUILayout.Label("Antenna(s) are available");
                                else
                                    GUILayout.Label("No antenna are available", _errorLabel);
#endif
                                power = Utils.AntennaUtils.GetTotalAntennaPowerEditor(EditorLogic.fetch.ship);
                                powerMet = (power >= s.antennaPower);
                            }
                        }
                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
                            if (power > 0)
                            {
                                using (new GUILayout.HorizontalScope())
                                    GUILayout.Label("Available antenna power: " + Utils.AntennaUtils.FormatPower(power));
                                using (new GUILayout.HorizontalScope())
                                {
                                    if (powerMet)
                                        StatusMessage = "Antenna power is met";
                                    else
                                    {
                                        ErrorMessage = "Insufficient antenna power";
                                        StatusMessage = "Antenna(s) are available";
                                    }
                                }
                            }
                            else
                                ErrorMessage = "No Antennas are available";
                        }
                    }
                    break;

                case CriterionType.SolarPanels:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            FloatField("Solar Charge Rate: ", ref s.solarChargeRate, 0, s.locked, " EC/sec");
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("Tracking:");
                            var old = (int)s.solarPaneltracking;
                            var solarPaneltracking = ComboBox.Box(TRACKING_COMBO, (int)s.solarPaneltracking, SolarUtils.trackingStr, this, 150);
                            if (old != solarPaneltracking)
                            {
                                s.solarPaneltracking = (SolarUtils.Tracking)solarPaneltracking;
                            }
                            GUILayout.FlexibleSpace();
                        }

                        bool chargeRateMet = false;
                        float chargeRate = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel, s.solarPaneltracking);

                            if (sgs.TotalSolarParts > 0)
                                StatusMessage = "Solar Panel(s) are available";
                            else
                                ErrorMessage = "No Solar Panels are available";

                            chargeRate = (float)sgs.TotalECps;
                            chargeRateMet = (chargeRate >= s.solarChargeRate);

                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                SolarGenerationSummary sgs = SolarUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                if (sgs.TotalSolarParts > 0)
                                    StatusMessage = "Solar Panel(s) are available";
                                else
                                {
                                    ErrorMessage = "No Solar Panels are available";
                                    break;
                                }
                                if (EditorLogic.fetch.ship != null)
                                {
                                    chargeRate = (float)sgs.TotalECps;
                                    chargeRateMet = (chargeRate >= s.solarChargeRate);
                                }
                            }
                        }
                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
                            using (new GUILayout.HorizontalScope())
                                GUILayout.Label("Max Available Solar Charge Rate: " + chargeRate);
                            using (new GUILayout.HorizontalScope())
                            {
                                if (chargeRateMet)
                                    StatusMessage = "Charge rate is met";
                                else
                                    ErrorMessage = "Insufficient charge rate";
                            }
                        }
                    }
                    break;

                case CriterionType.FuelCells:
                    {
                        FloatField("Fuel Cell Charge Rate: ", ref s.fuelCellChargeRate, 0, s.locked);
                        bool chargeRateMet = false;
                        float chargeRate = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            FuelCellGenerationSummary fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationFlight(FlightGlobals.ActiveVessel);

                            if (fcgs.TotalFuelCellParts > 0)
                                StatusMessage = "Fuel Cells are available";
                            else
                            {
                                ErrorMessage = "No Fuel Cells are available";
                                break;
                            }
                            chargeRate = (float)fcgs.TotalECps;
                            chargeRateMet = (chargeRate >= s.fuelCellChargeRate);

                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                FuelCellGenerationSummary fcgs = Utils.FuelCellUtils.GetEstimatedECGenerationEditor(EditorLogic.fetch.ship);
                                if (fcgs.TotalFuelCellParts > 0)
                                    StatusMessage = "Fuel Cells are available";
                                else
                                {
                                    ErrorMessage = "No Fuel Cells are available";
                                    break;
                                }

                                if (EditorLogic.fetch.ship != null)
                                {
                                    chargeRate = (float)fcgs.TotalECps;
                                    chargeRateMet = (chargeRate >= s.fuelCellChargeRate);
                                }
                            }
                        }
                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
                            if (s.fuelCellChargeRate > 0)
                            {
                                using (new GUILayout.HorizontalScope())
                                    GUILayout.Label("Max Available Fuel Cell Charge Rate: " + chargeRate);
                                if (chargeRateMet)
                                    StatusMessage = "Charge rate is met";
                                else
                                    ErrorMessage = "Insufficient charge rate";
                            }
                        }
                    }
                    break;

                case CriterionType.Generators:
                    {
                        FloatField("Generator Charge Rate: ", ref s.generatorChargeRate, 0, s.locked);
                        bool chargeRateMet = false;
                        float chargeRate = 0;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            GeneratorUtils.GeneratorSummary ggs = GeneratorUtils.GetTotalECGeneratorsFlight(FlightGlobals.ActiveVessel);

                            if (ggs.generatorCnt > 0)
                                StatusMessage = "Generator(s) are available";
                            else
                            {
                                ErrorMessage = "No Generator(s) are available";
                                break;
                            }

                            chargeRate = (float)ggs.TotalECps;
                            chargeRateMet = (chargeRate >= s.generatorChargeRate);

                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                GeneratorUtils.GeneratorSummary ggs = GeneratorUtils.GetTotalECGeneratorsEditor(EditorLogic.fetch.ship);
                                if (ggs.generatorCnt > 0)
                                    StatusMessage = "Generator(s) are available";
                                else
                                {
                                    ErrorMessage = "No Generator(s) are available";
                                    break;
                                }

                                chargeRate = (float)ggs.TotalECps;
                                chargeRateMet = (chargeRate >= s.generatorChargeRate);
                            }
                        }
                        if (s.generatorChargeRate > 0 &&
                            (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                        {
                            using (new GUILayout.HorizontalScope())
                                GUILayout.Label("Max Available Generator Charge Rate: " + chargeRate);

                            if (chargeRateMet)
                                StatusMessage = "Charge rate is met";
                            else
                                ErrorMessage = "Insufficient charge rate";

                        }
                    }
                    break;

                case CriterionType.Radiators:
                    {
                        FloatField("Radiator Cooling Rate: ", ref s.radiatorCoolingRate, 0, s.locked);
                        bool coolingRateMet = false;
                        float coolingRate = 0;
                        RadiatorCoolingSummary rcs = null;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            rcs = RadiatorUtils.GetEstimatedCoolingFlight(FlightGlobals.ActiveVessel);

                            coolingRate = (float)rcs.TotalKW;
                            coolingRateMet = (coolingRate >= s.radiatorCoolingRate);
                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                rcs = RadiatorUtils.GetEstimatedCoolingEditor(EditorLogic.fetch.ship);

                                if (EditorLogic.fetch.ship != null)
                                {
                                    coolingRate = (float)rcs.TotalKW;
                                    coolingRateMet = (coolingRate >= s.radiatorCoolingRate);
                                }
                            }
                        }

                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
                            if (rcs.TotalRadiatorParts == 0)
                                ErrorMessage = "No Radiators are available";
                            else
                            {
                                //if (s.radiatorCoolingRate > 0)
                                {
                                    using (new GUILayout.HorizontalScope())
                                        GUILayout.Label("Max Radiator Cooling Rate: " + coolingRate);
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        if (coolingRateMet)
                                            StatusMessage = "Cooling rate is met";
                                        else
                                            ErrorMessage = "Insufficient cooling rate";
                                    }
                                }
                            }
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
                        int spotlights;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            int totalSpotlights = Utils.LightUtils.CountSpotlightsFlight(FlightGlobals.ActiveVessel);

                            spotlights = totalSpotlights;
                            spotlightsMet = (spotlights >= s.spotlights);
                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                if (EditorLogic.fetch.ship != null)
                                {
                                    int editorSpotlights = Utils.LightUtils.CountSpotlightsEditor(EditorLogic.fetch.ship);

                                    spotlights = editorSpotlights;
                                    spotlightsMet = (spotlights >= s.spotlights);
                                }
                            }
                        }
                        if (s.spotlights > 0 &&
                            (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                if (spotlightsMet)
                                    StatusMessage = "Spotlight count is met";
                                else
                                    ErrorMessage = "Insufficient number of spotlights rate";
                            }
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
                        float parachutes;
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            ParachuteStateCounts psc = ParachuteUtils.GetParachuteStateCountsFlight(FlightGlobals.ActiveVessel);

                            parachutes = psc.Total;
                            parachutesMet = (parachutes >= s.parachutes);

                        }
                        else
                        {
                            if (HighLogic.LoadedSceneIsEditor)
                            {
                                ParachuteCapacitySummary psc = ParachuteUtils.GetParachuteCapacityEditor(EditorLogic.fetch.ship);

                                if (EditorLogic.fetch.ship != null)
                                {
                                    parachutes = psc.Total;
                                    parachutesMet = (parachutes >= s.parachutes);
                                }
                            }
                        }
                        if (s.parachutes > 0 &&
                            (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight))
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                if (parachutesMet)
                                    StatusMessage = "Parachute count is met";
                                else
                                    ErrorMessage = "Insufficient Parachutes";
                            }
                        }
                    }
                    break;

                case CriterionType.ReactionWheels:
                    {
                        TorqueSummary ts = new TorqueSummary();

                        using (new GUILayout.VerticalScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Reaction Wheels:", GUILayout.Width(120));
                                IntField("", ref s.reactionWheels, s.locked, 160);
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(20);
                                s.torquePitchRollYawEqual = GUILayout.Toggle(s.torquePitchRollYawEqual, "");
                                GUILayout.Label("Pitch, Roll, Yaw all Equal");
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(20);
                                GUILayout.Label("Torque Pitch:", GUILayout.Width(90));
                                DoubleField("", ref s.torquePitch, s.locked, width: 60);
                                if (s.torquePitchRollYawEqual)
                                {
                                    s.torqueYaw = s.torqueRoll = s.torquePitch;
                                }
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(20);
                                GUILayout.Label("Torque Yaw:", GUILayout.Width(90));
                                DoubleField("", ref s.torqueYaw, s.locked || s.torquePitchRollYawEqual, width: 60);
                            }

                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Space(20);
                                GUILayout.Label("Torque Roll:", GUILayout.Width(90));
                                DoubleField("", ref s.torqueRoll, s.locked || s.torquePitchRollYawEqual, width: 60);
                            }
                            GUILayout.Space(20);

                            bool reactionWheelsMet = false;
                            float reactionWheels;
                            if (HighLogic.LoadedSceneIsFlight)
                            {
                                ts = ReactionWheelUtils.GetEnabledTorqueFlight(FlightGlobals.ActiveVessel);

                                if (ts.Total > 0)
                                {
                                    StatusMessage = $"There are {ts.Total} Reaction Wheels available";
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(20);
                                        GUILayout.Label("Enabled Torque:");
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(40);
                                        GUILayout.Label($"Pitch: {ts.Pitch}");
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(40);
                                        GUILayout.Label($"Yaw:   {ts.Yaw}");
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Space(40);
                                        GUILayout.Label($"Roll:  {ts.Roll}");
                                    }
                                }
                                else
                                    ErrorMessage = "No Reaction Wheels are available";

                                reactionWheels = ts.Total;
                                reactionWheelsMet = (reactionWheels >= s.reactionWheels);

                            }
                            else
                            {
                                if (HighLogic.LoadedSceneIsEditor)
                                {
                                    ts = GetNominalTorqueEditor(EditorLogic.fetch.ship);
                                    if (ts.Total > 0)
                                    {
                                        StatusMessage = $"There are {ts.Total} Reaction Wheels available";
                                        GUILayout.Label("Total Available Torque:");
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label($"Pitch: {ts.Pitch}");
                                        }
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label($"Yaw:   {ts.Yaw}");
                                        }
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Space(20);
                                            GUILayout.Label($"Roll:  {ts.Roll}");
                                        }

                                    }
                                    else
                                        ErrorMessage = "No Reaction Wheels are available";
                                    if (HighLogic.LoadedSceneIsEditor)
                                    {
                                        if (EditorLogic.fetch.ship != null)
                                        {
                                            reactionWheels = ts.Total;
                                            reactionWheelsMet = (reactionWheels >= s.reactionWheels);
                                        }
                                    }
                                }
                            }
                            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                            {
                                if (s.reactionWheels > 0)
                                {
                                    if (reactionWheelsMet)
                                        StatusMessage = "Reaction Wheels Count Met";
                                    else
                                        ErrorMessage = "Insufficient Reaction Wheels";
                                    if (ts.Roll >= s.torqueRoll && ts.Pitch >= s.torquePitch && ts.Yaw >= s.torqueYaw)
                                    {
                                        StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Sufficient Torque is available";
                                    }
                                    else
                                    {
                                        ErrorMessage += (ErrorMessage.Length > 0 ? ":" : "") + "Insufficient torque";
                                    }
                                }
                            }
                        }
                    }
                    break;

                case CriterionType.Drills:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Minimum Quantity: ");
                            IntField("", ref s.drillQty, s.locked);
                        }


                        if (HighLogic.LoadedSceneIsEditor)
                        {
                            int i = DrillUtils.GetDrillParts(EditorLogic.fetch.ship).Count;
                            if (i >= s.drillQty)
                            {
                                StatusMessage = "Sufficient Drills found";
                            }
                            else
                            {
                                if (i > 0)
                                    ErrorMessage = "Insufficient Drills found";
                                else
                                    ErrorMessage = "No Drills found";
                            }
                        }
                        else
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            int i = DrillUtils.GetDrillParts(FlightGlobals.ActiveVessel).Count;
                            if (i >= s.drillQty)
                            {
                                StatusMessage = "Sufficient Drills found";
                            }
                            else
                            {
                                if (i > 0)
                                    ErrorMessage = "Insufficient Drills found";
                                else
                                    ErrorMessage = "No Drills found";
                            }

                        }
                        break;
                    }

                case CriterionType.DockingPort:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Minimum Quantity: ");
                            IntField("", ref s.dockingPortQty, s.locked);
                        }


                        if (HighLogic.LoadedSceneIsEditor)
                        {
                            int i = DockingPortUtils.GetDockingParts(EditorLogic.fetch.ship).Count;
                            if (i >= s.dockingPortQty)
                            {
                                StatusMessage = "Sufficient Docking Ports found";
                            }
                            else
                            {
                                if (i > 0)
                                    ErrorMessage = "Insufficient Docking Ports found";
                                else
                                    ErrorMessage = "No Docking Ports found";
                            }
                        }
                        else
                        if (HighLogic.LoadedSceneIsFlight)
                        {
                            int i = DockingPortUtils.GetDockingParts(FlightGlobals.ActiveVessel).Count;
                            if (i >= s.dockingPortQty)
                            {
                                StatusMessage = "Sufficient Docking Ports found";
                            }
                            else
                            {
                                if (i > 0)
                                    ErrorMessage = "Insufficient Docking Ports found";
                                else
                                    ErrorMessage = "No Docking Ports found";
                            }

                        }
                        break;
                    }
                case CriterionType.ControlSource:
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Minimum Quantity: ");
                        IntField("", ref s.controlSourceQty, s.locked);
                    }


                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        int i = PartLookupUtils.ShipModulesCount<ModuleCommand>(EditorLogic.fetch.ship);
                        if (i >= s.controlSourceQty)
                        {
                            StatusMessage = "Sufficient Control Sources found";
                        }
                        else
                        {
                            if (i > 0)
                                ErrorMessage = "Insufficient Control Sources found";
                            else
                                ErrorMessage = "No Control Sources found";

                        }
                    }
                    else
                    if (HighLogic.LoadedSceneIsFlight)
                    {
                        int i = PartLookupUtils.ShipModulesCount<ModuleCommand>(FlightGlobals.ActiveVessel);
                        if (i >= s.controlSourceQty)
                        {
                            StatusMessage = "Sufficient Control Sources found";
                        }
                        else
                        {
                            if (i > 0)
                                ErrorMessage = "Insufficient Control Sources found";
                            else
                                ErrorMessage = "No Control Sources found";

                        }
                    }
                    break;
#endif

                case CriterionType.Staging:
                    using (new GUILayout.HorizontalScope())
                    {
                        IntField(new GUIContent("Stage:"), ref s.stage, s.locked, 50, 50);
                        s.includeDockingPort = GUILayout.Toggle(s.includeDockingPort, "");
                        GUILayout.Label("Include Docking Port");
                        GUILayout.FlexibleSpace();
                        if (StageUtility.StageHasDecouplerOrSeparator(s.stage, out string moduleType, s.includeDockingPort))
                            StatusMessage = moduleType + " available for staging" +
                                "+";
                        else
                            ErrorMessage = "Staging not available";
                        break;
                    }

#if false
                case CriterionType.Engines:
                    {
                        EngineTypeInfo eti = null;
                        bool firstStage = s.stage >= StageInfo.StageCount;
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Engine: ");
                            IntField("", ref s.engineQty, s.locked);
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Engine Type: ", GUILayout.Width(120));
                            int engineType = 0;
                            for (int i = 0; i < Initialization.engineTypesAr.Length; i++)
                            {
                                if (Initialization.engineTypesAr[i] == s.engineType)
                                {
                                    engineType = i;
                                    break;
                                }
                            }
                            var old = engineType;
                            engineType = ComboBox.Box(ENGINETYPE_COMBO, engineType, Initialization.engineTypesDisplayAr, this, 300);
                            if (old != engineType || s.engineResourceList.Count == 0)
                            {
                                s.engineType = Initialization.engineTypesAr[engineType];
                                eti = Initialization.engineTypeDict[s.engineType];
                                s.engineResourceList.Clear();
                                foreach (var p in eti.Propellants)
                                    s.engineResourceList.Add(new ResInfo(p));
                            }

                            GUILayout.FlexibleSpace();
                            int realStage = (s.stage <= StageInfo.StageCount - 1) ? s.stage : StageInfo.StageCount - 1;

                            bool f = GUILayout.Toggle(firstStage, "");
                            GUILayout.Label("First Stage");
                            if (f && !firstStage)
                            {
                                firstStage = true;
                                s.stage = int.MaxValue;
                            }
                            if (firstStage && !f)
                            {
                                firstStage = false;
                                s.stage = 0;
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            s.engineGimbaled = GUILayout.Toggle(s.engineGimbaled, "");
                            GUILayout.Label("Gimbaled");
                            GUILayout.FlexibleSpace();
                            if (deltaVTableAvailable)
                            {
                                if (GUILayout.Button("  Delta V:"))
                                {
                                    OpenDeltaVPicker(_detailNode);
                                }
                            }
                            else
                                GUILayout.Label("Delta V:");
                            DoubleField("", ref s.deltaV, s.locked, "");
                            GUILayout.FlexibleSpace();

                            FloatField("TWR:", ref s.TWR, 2, s.locked, width: 50);

                            if (!firstStage)
                            {
                                IntField(new GUIContent("Stage:"), ref s.stage, s.locked, 50, 50);
                            }
                            else
                            {
                                GUILayout.Label("Stage:", GUILayout.Width(50));
                                GUILayout.Label("First Stage");
                            }
                            GUILayout.FlexibleSpace();
                            s.asl = GUILayout.Toggle(s.asl, new GUIContent("", "Enable for Sea Level values"));
                            GUILayout.Label(new GUIContent("ASL", "Enable for Sea Level values"));
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Resource", ScaledGUILayoutWidth(150));
                            GUILayout.Space(10);
                            GUILayout.Label("Min Amt", ScaledGUILayoutWidth(90));
                            GUILayout.Space(20);
                            GUILayout.Label("Min Capacity", ScaledGUILayoutWidth(100));
                            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                            {
                                GUILayout.Space(20);
                                GUILayout.Label("On Vessel", ScaledGUILayoutWidth(100));
                            }
                            GUILayout.FlexibleSpace();
                        }

                        StatusMessage = "All Resource Capacities/Amounts Met";
                        string status = "met";

                        foreach (var resinfo in s.engineResourceList)
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label(resinfo.resourceName, GUILayout.Width(150));
                                GUILayout.Space(10);
                                FloatField("", ref resinfo.resourceAmount, 0, s.locked, width: 100, flex: false);
                                GUILayout.Space(10);
                                FloatField("", ref resinfo.resourceCapacity, 0, s.locked, width: 100, flex: false);
                                resinfo.resourceCapacity = Math.Max(resinfo.resourceCapacity, resinfo.resourceAmount);
                                GUILayout.Space(20);
                                if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                                {
                                    s.CheckResource(resinfo.resourceName, false, out double amt, out double capacity);
                                    if (amt >= resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                    {
                                        GUILayout.Label("Met");
                                    }
                                    else
                                    {
                                        if (amt < resinfo.resourceAmount && capacity >= resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Partial");
                                            if (status == "met")
                                            {
                                                status = "partial";
                                                StatusMessage = "";
                                                ErrorMessage = "All Resource Available and Capacities Met";
                                            }
                                        }
                                        if (amt >= resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Partial");// , _errorLabel;
                                            if (status == "met")
                                            {
                                                status = "partial";
                                                StatusMessage = "";
                                                ErrorMessage = "Available Met, Capacity Unmet";
                                            }
                                        }
                                        if (amt < resinfo.resourceAmount && capacity < resinfo.resourceCapacity)
                                        {
                                            GUILayout.Label("Unmet", _errorLabel);
                                            StatusMessage = "";
                                            ErrorMessage = "Resource Available and Capacities Unmet";
                                        }
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }

                        List<Part> partsList = HighLogic.LoadedSceneIsEditor ? EditorLogic.fetch.ship.parts :
                            (HighLogic.LoadedSceneIsFlight ? FlightGlobals.ActiveVessel.parts : null);
                        if (partsList != null)
                        {
                            if (EngineTypeMatcher.PartsHaveEngineType(partsList, s.engineType))
                                StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Vessel has correct engine type";
                            else
                                ErrorMessage += (ErrorMessage.Length > 0 ? ":" : "") + "Vessel missing correct engine type";
                        }

                        if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                        {
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

                            GUILayout.Label("Calculated Delta V: ", ScaledGUILayoutWidth(150));
                            GUILayout.Label(dV.ToString("F0"));
                            GUILayout.Label("Calculated TWR: ", ScaledGUILayoutWidth(150));
                            GUILayout.Label(twr.ToString("F2"));

                            if (dV >= s.deltaV)
                            {
                                if (StatusMessage.Length > 0)
                                    StatusMessage += ":";
                                StatusMessage += "Sufficient Delta V is available";
                            }
                            else
                            {
                                if (ErrorMessage.Length > 0)
                                    ErrorMessage += ":";
                                ErrorMessage += "Not sufficient Delta V available";
                            }

                            if (StageInfo.TWRASL(realStage) >= s.TWR)
                            {
                                if (StatusMessage.Length > 0)
                                    StatusMessage += ":";
                                StatusMessage += "Sufficient TWR is available";
                            }
                            else
                            {
                                if (ErrorMessage.Length > 0)
                                    ErrorMessage += ":";
                                ErrorMessage += "TWR too low";
                            }
                        }
                    }
                    break;
#endif
                case CriterionType.Flags:
                    {
                        GUILayout.Space(2);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Selected body:", ScaledGUILayoutWidth(120));
                            GUILayout.Label(String.IsNullOrEmpty(s.flagBody) ? "(none)" : s.flagBody, HighLogic.Skin.label, ScaledGUILayoutWidth(250));
                            if (!s.locked)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                {
                                    OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.body);
                                }
                                if (!String.IsNullOrEmpty(s.flagBody))
                                {
                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                    {
                                        s.flagBody = "";
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            IntField("Flags required: ", ref s.flagCnt, s.locked);
                        }

                        if (s.flagCnt > 0 &&
                            HighLogic.LoadedSceneIsFlight &&
                            //s.stepType == CriterionType.Destination_body &&
                            !string.IsNullOrEmpty(s.flagBody))
                        {
                            int count;
                            using (new GUILayout.HorizontalScope())
                            {
                                bool landed = MissionVisitTracker.HasPlantedFlagOnBody(FlightGlobals.ActiveVessel, s.flagBody, countStartBody: false);
                                count = MissionVisitTracker.FlagCount(FlightGlobals.ActiveVessel, s.flagBody);

                                if (landed)
                                    GUILayout.Label($"Body: {s.flagBody} has planted {count} flags");
                                else
                                    GUILayout.Label($"Body: {s.flagBody} has not had any flags planted", _errorLabel);
                            }
                            //using (new GUILayout.HorizontalScope())
                            {
                                if (s.flagCnt <= count)
                                    StatusMessage = "Planted Flag count is met";
                                else
                                    ErrorMessage = "Planted Flags count not reached";
                            }
                        }
                    }
                    break;

                case CriterionType.TrackedVessel:
                    {
                        string oldTrackedVessel = s.trackedVessel;
                        Guid oldVesselGuid = s.vesselGuid;
                        SelectVessel(s.locked, ref s.trackedVessel, BodyAsteroidVessel.trackedVessel, s);
                        if (s.trackedVessel != ""  &&
                            (s.experience > 0 || s.reputation > 0 || s.kerbucks > 0))
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Step Active: " + s.stepActive.ToString());
                                if (!s.stepActive)
                                {
                                    if (GUILayout.Button(new GUIContent("Activate", "Activate step rewards"), GUILayout.Width(90)))
                                    {
                                        s.stepActive = true;
                                    }
                                }
                            }
                        }
                        GUILayout.Space(20);
                        GUILayout.Label("Completion Rewards");

                        using (new GUILayout.HorizontalScope())
                        {
                            IntField(new GUIContent("Experience:", ""),
                                        ref s.experience,
                                        s.locked | s.stepActive
                                        );

                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            IntField(new GUIContent("Reputation:", ""),
                                ref s.reputation,
                                s.locked | s.stepActive
                                );
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            IntField(new GUIContent("Kerbucks:", ""),
                                ref s.kerbucks,
                                s.locked | s.stepActive
                                );
                        }
                    }
                    break;

                case CriterionType.Destination:
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Destination Type: ");
                            var dt = ComboBox.Box(DESTINATION_TYPE_COMBO, (int)s.destType, Initialization.destTypesAr, this, 200, s.locked);
                            s.destType = (DestinationType)dt;
                        }
                        switch (s.destType)
                        {
                            case DestinationType.Vessel:
                                {
                                    SelectVessel(s.locked, ref s.destVessel, BodyAsteroidVessel.vessel);
                                    bool b = false;

                                    using (new GUILayout.HorizontalScope())
                                    {
                                        b = GUILayout.Toggle(s.requiresDocking, "");
                                        if (!s.locked && !s.hasDocked)
                                            s.requiresDocking = b;
                                        GUILayout.Label("Docking Required");
                                        if (s.hasDocked)
                                            GUILayout.Label(", Docking completed");
                                        GUILayout.FlexibleSpace();
                                    }
                                    if (HighLogic.LoadedSceneIsFlight && !string.IsNullOrEmpty(s.destVessel))
                                    {
                                        // if (!s.requiresDocking)
                                        {
                                            bool visited = MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destVessel, countStartBody: true);
                                            if (visited)
                                                StatusMessage = $"{s.destVessel} has been visited";
                                            else
                                                ErrorMessage = $"{s.destVessel} has not been visited";
                                        }

                                    }
                                }
                                break;
                            case DestinationType.Asteroid:
                                {
                                    GUILayout.Space(2);
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                                        GUILayout.Label(String.IsNullOrEmpty(s.destAsteroid) ? "(none)" : s.destAsteroid, GUI.skin.label, ScaledGUILayoutWidth(250));
                                        if (!s.locked)
                                        {
                                            GUILayout.FlexibleSpace();
                                            if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                            {
                                                OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.asteroid);
                                            }
                                            if (!String.IsNullOrEmpty(s.destAsteroid))
                                            {
                                                if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                                {
                                                    s.destAsteroid = "";
                                                }
                                            }
                                        }
                                        if (HighLogic.LoadedSceneIsFlight && !string.IsNullOrEmpty(s.destVessel))
                                        {
                                            bool visited = MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destVessel, countStartBody: true);
                                            if (visited)
                                                StatusMessage = $"{s.destVessel} has been visited";
                                            else
                                                ErrorMessage = $"{s.destVessel} has not been visited";
                                        }
                                    }
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        s.requiresLanding = GUILayout.Toggle(s.requiresLanding, "");
                                        GUILayout.Label("Landing Required");
                                        GUILayout.FlexibleSpace();
                                    }
                                    if (HighLogic.LoadedSceneIsFlight &&
                                        !string.IsNullOrEmpty(s.destAsteroid))
                                    {
                                        //using (new GUILayout.HorizontalScope())
                                        {
                                            bool visited = MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destAsteroid, countStartBody: true);
                                            if (visited)
                                                StatusMessage = $"Body: {s.destAsteroid} has been visited";
                                            else
                                                ErrorMessage = $"Body: {s.destAsteroid} has not been visited";
                                        }
                                    }
                                }
                                break;
                            case DestinationType.Body:
                                {
                                    GUILayout.Space(2);
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                                        GUILayout.Label(String.IsNullOrEmpty(s.destBody) ? "(none)" : s.destBody, HighLogic.Skin.label, ScaledGUILayoutWidth(250));
                                        if (!s.locked)
                                        {
                                            GUILayout.FlexibleSpace();
                                            using (new GUILayout.VerticalScope())
                                            {
                                                using (new GUILayout.HorizontalScope())
                                                {
                                                    if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                                    {
                                                        OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.body);
                                                    }
                                                    if (!String.IsNullOrEmpty(s.destBody))
                                                    {
                                                        if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                                        {
                                                            s.destBody = "";
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if (!s.locked && !string.IsNullOrEmpty(s.destBody))
                                    {
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            s.requiresLanding = GUILayout.Toggle(s.requiresLanding, "");
                                            GUILayout.Label("Require Landing");
                                            GUILayout.FlexibleSpace();
                                        }
                                        using (new GUILayout.HorizontalScope())
                                        {
                                            GUILayout.Label("Biome: ", ScaledGUILayoutWidth(60));
                                            var biomes = BiomeUtils.GetBiomes(s.destBody).ToArray();
                                            var b = BiomeUtils.Biomes.GetByName(s.destBiome);
                                            int x = 0;

                                            if (b != null)
                                                x = ComboBox.Box(BIOMES_COMBO, b.Id, biomes, this, 200, s.locked);
                                            else
                                                x = ComboBox.Box(BIOMES_COMBO, x, biomes, this, 200, s.locked);
                                            s.destBiome = BiomeUtils.Biomes.GetById(x).Name;
                                        }
                                        GUILayout.FlexibleSpace();
                                    }
                                    if (!String.IsNullOrEmpty(s.destBody) && HighLogic.LoadedSceneIsFlight)
                                    {
                                        bool visited = MissionVisitTracker.HasVisitedBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: true);
                                        if (visited)
                                            StatusMessage = $"Vessel has visited {s.destBody}";
                                        else
                                            ErrorMessage = $"Vessel not has visited {s.destBody}";

                                        if (visited && s.requiresLanding)
                                        {
                                            bool landed = MissionVisitTracker.HasLandedOnBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: false);
                                            int count = MissionVisitTracker.LandedCount(FlightGlobals.ActiveVessel, s.destBody);
                                            if (landed)
                                            {
                                                if (s.destBiome == "" || s.destBiome == BiomeUtils.ANYBIOME)
                                                {
                                                    StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Has landed {count} times";
                                                }
                                                else
                                                {
                                                    if (MissionVisitTracker.HasLandedOnBodyAtBiome(FlightGlobals.ActiveVessel, s.destBody, s.destBiome))
                                                        StatusMessage = $"Vessel has landed on {s.destBody}, in the {s.destBiome} biome";
                                                    else
                                                        ErrorMessage = $"Vessel has landed on {s.destBody}, has not yet landeed in {s.destBiome} biome";
                                                }
                                            }
                                            else
                                            {
                                                ErrorMessage = $"Body: {s.destBody} has not been landed on";
                                            }
                                        }
                                    }
                                }
                                break;


                        }
                    }
                    break;

#if false
                case CriterionType.Destination_vessel:
                    {
                        SelectVessel(s.locked, ref s.destVessel, BodyAsteroidVessel.vessel);
                        bool b = false;

                        using (new GUILayout.HorizontalScope())
                        {
                            b = GUILayout.Toggle(s.requiresDocking, "");
                            if (!s.locked && !s.hasDocked)
                                s.requiresDocking = b;
                            GUILayout.Label("Docking Required");
                            if (s.hasDocked)
                                GUILayout.Label(", Docking completed");
                            GUILayout.FlexibleSpace();
                        }
                        if (HighLogic.LoadedSceneIsFlight && !string.IsNullOrEmpty(s.destVessel))
                        {
                            // if (!s.requiresDocking)
                            {
                                bool visited = MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destVessel, countStartBody: true);
                                if (visited)
                                    StatusMessage = $"{s.destVessel} has been visited";
                                else
                                    ErrorMessage = $"{s.destVessel} has not been visited";
                            }

                        }
                    }
                    break;

                case CriterionType.Destination_asteroid:
                    {
                        GUILayout.Space(2);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                            GUILayout.Label(String.IsNullOrEmpty(s.destAsteroid) ? "(none)" : s.destAsteroid, GUI.skin.label, ScaledGUILayoutWidth(250));
                            if (!s.locked)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                {
                                    OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.asteroid);
                                }
                                if (!String.IsNullOrEmpty(s.destAsteroid))
                                {
                                    if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                    {
                                        s.destAsteroid = "";
                                    }
                                }
                            }
                            if (HighLogic.LoadedSceneIsFlight && !string.IsNullOrEmpty(s.destVessel))
                            {
                                bool visited = MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destVessel, countStartBody: true);
                                if (visited)
                                    StatusMessage = $"{s.destVessel} has been visited";
                                else
                                    ErrorMessage = $"{s.destVessel} has not been visited";
                            }
                        }
                        using (new GUILayout.HorizontalScope())
                        {
                            s.requiresLanding = GUILayout.Toggle(s.requiresLanding, "");
                            GUILayout.Label("Landing Required");
                            GUILayout.FlexibleSpace();
                        }
                        if (HighLogic.LoadedSceneIsFlight &&
                            !string.IsNullOrEmpty(s.destAsteroid))
                        {
                            //using (new GUILayout.HorizontalScope())
                            {
                                bool visited = MissionVisitTracker.HasVisitedVessel(FlightGlobals.ActiveVessel, s.destAsteroid, countStartBody: true);
                                if (visited)
                                    StatusMessage = $"Body: {s.destAsteroid} has been visited";
                                else
                                    ErrorMessage = $"Body: {s.destAsteroid} has not been visited";
                            }
                        }
                    }
                    break;

                case CriterionType.Destination_body:
                    {
                        GUILayout.Space(2);
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                            GUILayout.Label(String.IsNullOrEmpty(s.destBody) ? "(none)" : s.destBody, HighLogic.Skin.label, ScaledGUILayoutWidth(250));
                            if (!s.locked)
                            {
                                GUILayout.FlexibleSpace();
                                using (new GUILayout.VerticalScope())
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                                        {
                                            OpenBodyAsteroidVesselPicker(_detailNode, BodyAsteroidVessel.body);
                                        }
                                        if (!String.IsNullOrEmpty(s.destBody))
                                        {
                                            if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                                            {
                                                s.destBody = "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (!s.locked && !string.IsNullOrEmpty(s.destBody))
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                s.requiresLanding = GUILayout.Toggle(s.requiresLanding, "");
                                GUILayout.Label("Require Landing");
                                GUILayout.FlexibleSpace();
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Biome: ", ScaledGUILayoutWidth(60));
                                var biomes = BiomeUtils.GetBiomes(s.destBody).ToArray();
                                var b = BiomeUtils.Biomes.GetByName(s.destBiome);
                                int x = 0;

                                if (b != null)
                                    x = ComboBox.Box(BIOMES_COMBO, b.Id, biomes, this, 200);
                                else
                                    x = ComboBox.Box(BIOMES_COMBO, x, biomes, this, 200);
                                s.destBiome = BiomeUtils.Biomes.GetById(x).Name;
                            }
                            GUILayout.FlexibleSpace();
                        }
                        if (!String.IsNullOrEmpty(s.destBody) && HighLogic.LoadedSceneIsFlight)
                        {
                            bool visited = MissionVisitTracker.HasVisitedBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: true);
                            if (visited)
                                StatusMessage = $"Vessel has visited {s.destBody}";
                            else
                                ErrorMessage = $"Vessel not has visited {s.destBody}";

                            if (visited && s.requiresLanding)
                            {
                                bool landed = MissionVisitTracker.HasLandedOnBody(FlightGlobals.ActiveVessel, s.destBody, countStartBody: false);
                                int count = MissionVisitTracker.LandedCount(FlightGlobals.ActiveVessel, s.destBody);
                                if (landed)
                                {
                                    if (s.destBiome == "" || s.destBiome == BiomeUtils.ANYBIOME)
                                    {
                                        StatusMessage += (StatusMessage.Length > 0 ? ":" : "") + "Has landed {count} times";
                                    }
                                    else
                                    {
                                        if (MissionVisitTracker.HasLandedOnBodyAtBiome(FlightGlobals.ActiveVessel, s.destBody, s.destBiome))
                                            StatusMessage = $"Vessel has landed on {s.destBody}, in the {s.destBiome} biome";
                                        else
                                            ErrorMessage = $"Vessel has landed on {s.destBody}, has not yet landeed in {s.destBiome} biome";
                                    }
                                }
                                else
                                {
                                    ErrorMessage = $"Body: {s.destBody} has not been landed on";
                                }
                            }
                        }
                    }
                    break;
#endif
                default:
                    break;
            }
        }

        void SelectVessel(bool locked, ref string vessel, BodyAsteroidVessel vesselType, Step s = null)
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Selected:", ScaledGUILayoutWidth(60));
                GUILayout.Label(String.IsNullOrEmpty(vessel) ? "(none)" : vessel, GUI.skin.label, ScaledGUILayoutWidth(250));
                if (!locked && (s == null || !s.stepActive || (s.stepActive && !s.stepCompleted)))
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Select…", ScaledGUILayoutWidth(90)))
                    {
                        OpenBodyAsteroidVesselPicker(_detailNode, vesselType);
                    }
                    if (!String.IsNullOrEmpty(vessel))
                    {
                        if (GUILayout.Button("Clear", ScaledGUILayoutWidth(70)))
                        {
                            vessel = "";
                        }
                    }
                }
            }
        }
    }
}
