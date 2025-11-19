using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;

public static class RCSUtils
{
    /// <summary>
    /// Returns true if the vessel currently has RCS toggled ON in flight.
    /// </summary>
    public static bool IsRCSEngaged(Vessel v)
    {
        return v != null && v.ActionGroups[KSPActionGroup.RCS];
    }

    /// <summary>
    /// Returns true if the vessel has at least one RCS thruster module and is controllable in flight.
    /// </summary>
    public static bool IsRCSAvailableFlight(Vessel v)
    {
        if (v == null) return false;
        if (!v.IsControllable) return false;

        // RCS modules can be either ModuleRCS or ModuleRCSFX
        return v.Parts.Any(p =>
            p.FindModuleImplementing<ModuleRCS>() != null ||
            p.FindModuleImplementing<ModuleRCSFX>() != null
        );
    }

    /// <summary>
    /// Returns true if the editor craft has at least one RCS thruster module.
    /// </summary>
    public static bool IsRCSAvailableEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return false;

        foreach (var p in ship.parts)
        {
            if (p.FindModuleImplementing<ModuleRCS>() != null ||
                p.FindModuleImplementing<ModuleRCSFX>() != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Build engineType key:
    /// "EngineTypeEnum:SortedProp1:SortedProp2:..."
    /// Excludes IntakeAir && MJPropellant.
    /// </summary>
    public static string GetRCSTypeKey(ModuleRCS me)
    {
        if (me == null) return null;

        var propNames = new List<string>();

        if (me.propellants != null)
        {
            foreach (var prop in me.propellants)
            {
                if (prop == null || string.IsNullOrEmpty(prop.name))
                    continue;

                // Skip IntakeAir && MJPropellant
                if (prop.name == "IntakeAir" || prop.name == "MJPropellant")
                    continue;

                propNames.Add(prop.name);
            }
        }

        // Sort alphabetically, case-insensitive
        propNames.Sort(StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();

        sb.Append("rcs:");

        foreach (var prop in propNames)
        {
            sb.Append(prop);
            sb.Append(":");
        }

        return sb.ToString().ToLower();
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrEmpty(key)
            ? string.Empty
            : key.Trim().ToLowerInvariant();
    }

    public static bool PartsHaveRCSType(IEnumerable<Part> parts, string rcstypeKey)
    {
        if (parts == null) return false;

        string target = NormalizeKey(rcstypeKey);

        foreach (var p in parts)
        {
            if (p == null) continue;

            var engines = p.FindModulesImplementing<ModuleRCS>();
            foreach (var me in engines)
            {
                if (me == null) continue;

                string key = GetRCSTypeKey(me);
                if (string.IsNullOrEmpty(key)) continue;
                if (NormalizeKey(key) == target)
                    return true;
            }
        }

        return false;
    }



}
