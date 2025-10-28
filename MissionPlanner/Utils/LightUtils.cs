// File: LightUtils.cs
// KSP1 utility: count spotlights (not just any light) in flight and editor
// C# 7.3 compatible

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class LightUtils
{
    // ─────────────────────────────────────────────────────────────
    // Check if a part contains at least one Spotlight
    // ─────────────────────────────────────────────────────────────

    private static bool PartHasSpotlight(Part part)
    {
        if (part == null) return false;
        // Get all Light components in the part model hierarchy
        var lights = part.GetComponentsInChildren<Light>(true);
        if (lights == null || lights.Length == 0) return false;

        foreach (var l in lights)
        {
            if (l != null && l.type == LightType.Spot)
                return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // Presence checks
    // ─────────────────────────────────────────────────────────────

    public static bool HasSpotlightsFlight(Vessel v)
    {
        if (v == null) return false;
        return v.Parts.Any(PartHasSpotlight);
    }

    public static bool HasSpotlightsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return false;
        return ship.parts.Any(PartHasSpotlight);
    }

    // ─────────────────────────────────────────────────────────────
    // Count spotlights
    // ─────────────────────────────────────────────────────────────

    public static int CountSpotlightsFlight(Vessel v)
    {
        if (v == null) return 0;
        return v.Parts.Count(PartHasSpotlight);
    }

    public static int CountSpotlightsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return 0;
        return ship.parts.Count(PartHasSpotlight);
    }

    // ─────────────────────────────────────────────────────────────
    // List spotlight parts
    // ─────────────────────────────────────────────────────────────

    public static List<Part> GetSpotlightPartsFlight(Vessel v)
    {
        if (v == null) return new List<Part>();
        return v.Parts.Where(PartHasSpotlight).ToList();
    }

    public static List<Part> GetSpotlightPartsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return new List<Part>();
        return ship.parts.Where(PartHasSpotlight).ToList();
    }

    // ─────────────────────────────────────────────────────────────
    // Optional: count active spotlights in flight
    // ─────────────────────────────────────────────────────────────

    public static int CountActiveSpotlightsFlight(Vessel v)
    {
        if (v == null) return 0;
        int count = 0;
        foreach (var p in v.Parts)
        {
            if (!p.Modules.Contains("ModuleLight")) continue;

            foreach (var lightModule in p.FindModulesImplementing<ModuleLight>())
            {
                if (!lightModule.isOn) continue;

                var lights = p.GetComponentsInChildren<Light>(true);
                foreach (var l in lights)
                {
                    if (l != null && l.type == LightType.Spot)
                        count++;
                }
            }
        }
        return count;
    }
}
