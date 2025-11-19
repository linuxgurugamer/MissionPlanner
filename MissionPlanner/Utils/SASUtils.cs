using System.Collections.Generic;
using System.Linq;

using static MissionPlanner.RegisterToolbar;

public static class SASUtils
{

    /// <summary>
    /// Summary of SAS capabilities detected in the Editor (no pilots/EC/comm logic here).
    /// </summary>
    public struct EditorSASInfo
    {
        /// True if any part provides SAS (i.e., has a ModuleSAS enabled).
        public bool HasSAS;

        /// Highest ModuleSAS.SASServiceLevel found across all parts. 0 means "no SAS".
        public int HighestServiceLevel;
    }


    internal static readonly string[] SasLevelDescriptions =
    {
        "No SAS",                                    // 0
        "Stability Assist only",                     // 1
        "Prograde and Retrograde",                   // 2
        "Normal, Antinormal, Radial In, Radial Out", // 3
        "Target and Anti-Target",                    // 4
        "Maneuver Hold (Full SAS)"                   // 5
    };

    internal static List<VesselAutopilot.AutopilotMode>[] sasModes = new List<VesselAutopilot.AutopilotMode>[]
    {

        new List<VesselAutopilot.AutopilotMode> { } ,   // No SAS
        new List<VesselAutopilot.AutopilotMode> { VesselAutopilot.AutopilotMode.StabilityAssist } ,   // Stability Assist Only
        new List<VesselAutopilot.AutopilotMode> { VesselAutopilot.AutopilotMode.StabilityAssist,
                                                                VesselAutopilot.AutopilotMode.Prograde,
                                                                VesselAutopilot.AutopilotMode.Retrograde} , // Prograde and Retrograde
        new List<VesselAutopilot.AutopilotMode> { VesselAutopilot.AutopilotMode.StabilityAssist,
                                                                VesselAutopilot.AutopilotMode.Prograde,
                                                                VesselAutopilot.AutopilotMode.Retrograde,
                                                                VesselAutopilot.AutopilotMode.Normal,
                                                                VesselAutopilot.AutopilotMode.Antinormal,
                                                                VesselAutopilot.AutopilotMode.RadialIn,
                                                                VesselAutopilot.AutopilotMode.RadialOut, } , // Normal, Antinormal, Radial In, Radial Out
        new List<VesselAutopilot.AutopilotMode> { VesselAutopilot.AutopilotMode.StabilityAssist,
                                                                VesselAutopilot.AutopilotMode.Prograde,
                                                                VesselAutopilot.AutopilotMode.Retrograde,
                                                                VesselAutopilot.AutopilotMode.Normal,
                                                                VesselAutopilot.AutopilotMode.Antinormal,
                                                                VesselAutopilot.AutopilotMode.RadialIn,
                                                                VesselAutopilot.AutopilotMode.RadialOut,
                                                                VesselAutopilot.AutopilotMode.Target,
                                                                VesselAutopilot.AutopilotMode.AntiTarget,} , // Target and Anti-Target
        new List<VesselAutopilot.AutopilotMode> { VesselAutopilot.AutopilotMode.StabilityAssist,
                                                                VesselAutopilot.AutopilotMode.Prograde,
                                                                VesselAutopilot.AutopilotMode.Retrograde,
                                                                VesselAutopilot.AutopilotMode.Normal,
                                                                VesselAutopilot.AutopilotMode.Antinormal,
                                                                VesselAutopilot.AutopilotMode.RadialIn,
                                                                VesselAutopilot.AutopilotMode.RadialOut,
                                                                VesselAutopilot.AutopilotMode.Target,
                                                                VesselAutopilot.AutopilotMode.AntiTarget,
                                                                VesselAutopilot.AutopilotMode.Maneuver } , // Maneuver Hold (Full SAS)
    };


    /// <summary>
    /// Returns true if SAS is available in flight.
    /// </summary>
    public static bool IsSASAvailableFlight(Vessel v)
    {
        if (v == null || v.Autopilot == null) return false;
        if (!v.IsControllable) return false;

        return v.Autopilot.CanSetMode(VesselAutopilot.AutopilotMode.StabilityAssist);
    }

    /// <summary>
    /// Editor-side probe of SAS availability and maximum service level.
    /// Looks for ModuleSAS on any part and returns the max ModuleSAS.SASServiceLevel.
    /// </summary>
    public static EditorSASInfo GetSASInfoEditor(ShipConstruct ship)
    {
        var info = new EditorSASInfo { HasSAS = false, HighestServiceLevel = 0 };
        if (ship == null || ship.parts == null) return info;

        foreach (var p in ship.parts)
        {
            // If the part exposes SAS, it will have ModuleSAS (even on command pods/probe cores).
            var sas = p.FindModuleImplementing<ModuleSAS>();
            if (sas == null) continue;

            // In the editor, honor the module's enabled flag.
            if (!sas.isEnabled) continue;

            info.HasSAS = true;
            if (sas.SASServiceLevel > info.HighestServiceLevel)
                info.HighestServiceLevel = sas.SASServiceLevel;
        }

        return info;
    }

    public static bool IsRequiredSASAvailable(int SASServiceLevel)
    {
        var sasInfo = SASUtils.GetAvailableSASModes(FlightGlobals.ActiveVessel);
        return IsRequiredSASAvailable(SASServiceLevel, sasInfo);
    }

    public static bool IsRequiredSASAvailable(int SASServiceLevel, VesselAutopilot.AutopilotMode[] sasInfo)
    {
        foreach (var sas in sasModes[SASServiceLevel])
        {
            bool found = false;
            foreach (var p in sasInfo)
            {
                if (p.Equals(sas))
                {
                    found = true;
                    break;
                }
            }
            if (!found)         
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Returns a friendly string description for a given SAS service level.
    /// Matches stock KSP1 progression.
    /// </summary>
    /// 
    public static string GetSASLevelDescription(int level)
    {
        if (level >= 0 && level < SasLevelDescriptions.Length)
            return SasLevelDescriptions[level];

        return $"Unknown SAS level ({level})";
    }
    /// <summary>
    /// SAS currently engaged in flight.
    /// </summary>
    public static bool IsSASEngaged(Vessel v)
    {
        return v != null && v.ActionGroups[KSPActionGroup.SAS];
    }

    /// <summary>
    /// Returns which SAS modes are available in flight.
    /// </summary>
    public static VesselAutopilot.AutopilotMode[] GetAvailableSASModes(Vessel v)
    {
        if (v == null || v.Autopilot == null || !v.IsControllable)
            return new VesselAutopilot.AutopilotMode[0];

        var modes = new[]
        {
            VesselAutopilot.AutopilotMode.StabilityAssist,
            VesselAutopilot.AutopilotMode.Prograde,
            VesselAutopilot.AutopilotMode.Retrograde,
            VesselAutopilot.AutopilotMode.Normal,
            VesselAutopilot.AutopilotMode.Antinormal,
            VesselAutopilot.AutopilotMode.RadialIn,
            VesselAutopilot.AutopilotMode.RadialOut,
            VesselAutopilot.AutopilotMode.Target,
            VesselAutopilot.AutopilotMode.AntiTarget,
            VesselAutopilot.AutopilotMode.Maneuver
        };

        return modes.Where(m => v.Autopilot.CanSetMode(m)).ToArray();
    }
}
