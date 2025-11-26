using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;
public static class EngineTypeMatcher
{
    public static bool VesselHasEngineType(Vessel v, string engineTypeKey)
    {
        if (v == null) return false;
        return PartsHaveEngineType(v.Parts, engineTypeKey);
    }

    public static bool ShipHasEngineType(ShipConstruct ship, string engineTypeKey)
    {
        if (ship == null || ship.parts == null) return false;
        return PartsHaveEngineType(ship.parts, engineTypeKey);
    }

    public static bool PartsHaveEngineType(IEnumerable<Part> parts, string engineTypeKey)
    {
        if (parts == null) return false;
        if (string.IsNullOrEmpty(engineTypeKey)) return false;

        string target = NormalizeKey(engineTypeKey);

        foreach (var p in parts)
        {
            if (p == null) continue;

            var engines = p.FindModulesImplementing<ModuleEngines>();
            foreach (var me in engines)
            {
                if (me == null) continue;

                string key = GetEngineTypeKey(me);
                if (string.IsNullOrEmpty(key)) continue;
                if (NormalizeKey(key) == target)
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
    public static string GetEngineTypeKey(ModuleEngines me)
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
                if (prop.name== "IntakeAir" || prop.name == "MJPropellant")
                    continue;

                propNames.Add(prop.name);
            }
        }

        // Sort alphabetically, case-insensitive
        propNames.Sort(StringComparer.OrdinalIgnoreCase);

        var sb = new StringBuilder();

        sb.Append(me.engineType.ToString());
        sb.Append(":");

        foreach (var prop in propNames)
        {
            sb.Append(prop);
            sb.Append(":");
        }

        return sb.ToString();
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrEmpty(key)
            ? string.Empty
            : key.Trim().ToLowerInvariant();
    }
}
