// File: DockingPortUtils.cs

using System;
using System.Collections.Generic;
using UnityEngine;   // Vessel, Part, PartModule, ShipConstruct

using static MissionPlanner.RegisterToolbar;

public static class DockingPortUtils
{
    // Known docking / docking-like PartModule names (config names).
    // Extend this list as needed or at runtime via RegisterExtraDockingModule.
    private static readonly HashSet<string> DockingModuleNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Stock
            "ModuleDockingNode",

            // USI Konstruction
            "ModuleWeldablePort",

            // KAS
            "ModuleKASPort",
            "ModuleKASJointDock",

            // SSTU
            "SSTUDockingPort",
            "SSTUAnimateControlledDockingNode",

            // Tundra Exploration
            "ModuleGimbalDockingPort",

            // B9-style
            "ModuleDockingNodeHinge",

            // Kerbal Reusability Expansion
            "ModuleKREDockingPort",

            // Add more here as you discover them, e.g.:
            // "IRDockingPort",
        };

    /// <summary>
    /// Allow other code (or config) to register additional
    /// docking PartModule class names at runtime.
    /// </summary>
    public static void RegisterExtraDockingModule(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName)) return;
        DockingModuleNames.Add(moduleName);
    }

    // ======================
    //  FLIGHT (Vessel)
    // ======================

    /// <summary>
    /// Returns true if the flight vessel has at least one docking port
    /// (stock or known modded).
    /// </summary>
    public static bool HasAnyDockingPort(Vessel v)
    {
        if (v == null || v.parts == null) return false;
        return HasAnyDockingPortOnParts(v.parts);
    }

    /// <summary>
    /// Returns a list of all Parts on the flight vessel that are considered docking ports.
    /// </summary>
    public static List<Part> GetDockingParts(Vessel v)
    {
        var result = new List<Part>();
        if (v == null || v.parts == null) return result;

        GetDockingPartsOnParts(v.parts, result);
        return result;
    }

    // ======================
    //  EDITOR (ShipConstruct)
    // ======================

    /// <summary>
    /// Returns true if the editor ship has at least one docking port
    /// (stock or known modded).
    /// </summary>
    public static bool HasAnyDockingPort(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return false;
        return HasAnyDockingPortOnParts(ship.parts);
    }

    /// <summary>
    /// Returns a list of all Parts on the editor ship that are considered docking ports.
    /// </summary>
    public static List<Part> GetDockingParts(ShipConstruct ship)
    {
        var result = new List<Part>();
        if (ship == null || ship.parts == null) return result;

        GetDockingPartsOnParts(ship.parts, result);
        return result;
    }

    /// <summary>
    /// Returns true if the specified stage on the editor ship
    /// has at least one docking port.
    /// </summary>
    public static bool StageHasDockingPort( int stage)
    {
        //Log.Info("StageHasDockingPort, stage: " + stage);
        List<Part> parts = null;
        if (HighLogic.LoadedSceneIsEditor)
            parts = EditorLogic.fetch.ship.Parts;
        if (HighLogic.LoadedSceneIsFlight)
            parts = FlightGlobals.ActiveVessel.Parts;
        if (parts == null || parts.Count == 0) return false;

        return HasDockingPortInStage(parts, stage);
    }

    // ======================
    //  SHARED IMPLEMENTATION
    // ======================

    private static bool HasAnyDockingPortOnParts(IEnumerable<Part> parts)
    {
        if (parts == null) return false;

        foreach (var p in parts)
        {
            if (PartHasDockingPort(p))
                return true;
        }

        return false;
    }

    private static void GetDockingPartsOnParts(IEnumerable<Part> parts, List<Part> output)
    {
        if (parts == null || output == null) return;

        foreach (var p in parts)
        {
            if (PartHasDockingPort(p))
                output.Add(p);
        }
    }

    /// <summary>
    /// Returns true if any part in the given collection that belongs
    /// to the specified stage has a docking port.
    /// Uses Part.inverseStage for both flight and editor.
    /// </summary>
    private static bool HasDockingPortInStage(IEnumerable<Part> parts, int stage)
    {
        if (parts == null) return false;
        //if (stage < 0) return false;

        foreach (var p in parts)
        {
            if (p == null) continue;

                //Log.Info("HasDockingPortInStage, part: " + p.name + ", p.inverseStage: " + p.inverseStage);
            // In both flight and editor, inverseStage is the staging index
            // (with higher numbers firing later).
            if (p.inverseStage + 1 == stage && PartHasDockingPort(p))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks a single part for any docking-capable module by name.
    /// </summary>
    public static bool PartHasDockingPort(Part part)
    {
        if (part == null || part.Modules == null) return false;

        foreach (PartModule m in part.Modules)
        {
            if (m == null) continue;

            if (IsDockingModuleByName(m))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Name-based check only: compares PartModule.ClassName and moduleName
    /// against the known docking module names.
    /// </summary>
    private static bool IsDockingModuleByName(PartModule m)
    {
        // KSP sets ClassName to the module name as in the config by default,
        // but some mods may override; we check both just in case.
        string className = m.ClassName;
        string moduleName = m.moduleName;

        //Log.Info($"IsDockingModuleByName, className: {className} moduleName: { moduleName} ");
        if (!string.IsNullOrEmpty(className) &&
            DockingModuleNames.Contains(className))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(moduleName) &&
            DockingModuleNames.Contains(moduleName))
        {
            return true;
        }

        return false;
    }
}