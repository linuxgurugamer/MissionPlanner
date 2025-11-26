// File: DrillUtils.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public static class DrillUtils
{
    private static readonly HashSet<string> DrillModuleNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // ---- STOCK ----
            "ModuleResourceHarvester",
            "ModuleAsteroidDrill",

            // ---- USI / MKS / Karbonite / Regolith ----
            "USI_Harvester",
            "USI_Converter",
            "USI_ModuleResourceHarvester",
            "ModuleResourceHarvester_USI",
            "ModuleSwappableBayHarvester",
            "ModuleSwappableHarvester",
            "ModulePlanetaryHarvester",
            "ModuleAsteroidHarvester",
            "ModuleAsteroidDrill_USI",
            "KarboniteDrill",
            "KA_Drill",
            "Karbonite_Harvester",
            "MKSLDrill",

            // ---- KETHANE ----
            "KethaneExtractor",
            "KethaneHarvester",
            "KethaneDrill",
            "KethanePump",

            // ---- KPBS ----
            "ModuleKPBSDrill",
            "ModuleKPBSHarvester",

            // ---- KSPI-E ----
            "ModuleKSPIDrill",
            "ModuleExoticMiner",
            "ModuleAtmosphericExtractor",
            "ModuleSubsurfaceHarvester",
            "ModuleISRUHarvester",
            "ModuleMagneticScoop",
            "FNModuleResourceExtraction",
            "FNModuleAtmosphericExtraction",
            "FNModuleReverseOsmosis",
            "FNScoopController",
            "FNModuleRegolithHarvester",

            // ---- Other major mods ----
            "ModuleResourceScannerHarvester",
            "ELHarvester",
            "ELConverter",

            // ---- WBI: Pathfinder / MOLE / DSEV ----
            "WBIDrill",
            "WBIHarvester",
            "WBIResourceHarvester",
            "WBIModuleResourceHarvester",
            "WBIKariDrill",
            "WBIModuleAsteroidHarvester",

            // ---- KERBALISM ----
            "Harvester",
            "ModuleHarvester",
            "GreenhouseExtractor",

            // ---- Rational Resources ----
            "ModuleResourceHarvester_RR",
            "RRHarvester",
            "RRDrill",

            // ---- Misc / Indie Mods ----
            "ModuleSimpleDrill",
            "ModuleBasicHarvester",
            "ModuleUniversalHarvester",
            "ModuleResourceDigester",
            "CustomDrill",
        };

    public static void RegisterExtraDrillModule(string moduleName)
    {
        if (!string.IsNullOrEmpty(moduleName))
            DrillModuleNames.Add(moduleName);
    }

    // ===== Flight =====
    public static bool HasAnyDrill(Vessel v)
    {
        if (v == null || v.parts == null) return false;
        foreach (var p in v.parts)
            if (PartHasDrill(p)) return true;
        return false;
    }

    public static List<Part> GetDrillParts(Vessel v)
    {
        List<Part> result = new List<Part>();
        if (v == null || v.parts == null) return result;

        foreach (var p in v.parts)
            if (PartHasDrill(p)) result.Add(p);

        return result;
    }

    // ===== Editor =====
    public static bool HasAnyDrill(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return false;
        foreach (var p in ship.parts)
            if (PartHasDrill(p)) return true;
        return false;
    }

    public static List<Part> GetDrillParts(ShipConstruct ship)
    {
        List<Part> result = new List<Part>();
        if (ship == null || ship.parts == null) return result;

        foreach (var p in ship.parts)
            if (PartHasDrill(p)) result.Add(p);

        return result;
    }

    // ===== Shared =====
    public static bool PartHasDrill(Part part)
    {
        if (part == null || part.Modules == null) return false;

        foreach (PartModule m in part.Modules)
            if (IsDrillModuleByName(m)) return true;

        return false;
    }

    private static bool IsDrillModuleByName(PartModule m)
    {
        if (m == null) return false;

        return
            (!string.IsNullOrEmpty(m.ClassName) &&
                DrillModuleNames.Contains(m.ClassName))
            ||
            (!string.IsNullOrEmpty(m.moduleName) &&
                DrillModuleNames.Contains(m.moduleName));
    }
}
