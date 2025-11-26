using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP;

public static class PartLookupUtils
{
    /// <summary>
    /// Returns the current set of Parts from either:
    /// - Flight: the provided vessel or the ActiveVessel
    /// - Editor: the current ShipConstruct in VAB/SPH
    /// </summary>
    public static IEnumerable<Part> GetCurrentParts(Vessel vessel = null)
    {
        if (HighLogic.LoadedSceneIsFlight)
        {
            var v = vessel ?? FlightGlobals.ActiveVessel;
            return v?.parts;
        }
        else if (HighLogic.LoadedSceneIsEditor)
        {
            var ship = EditorLogic.fetch?.ship;
            return ship?.parts;
        }
        return null;
    }

    /// <summary>
    /// Check by INTERNAL part name (AvailablePart.name), e.g. "fuelTankSmallFlat".
    /// Works in Flight (loaded vessel) and in the Editor.
    /// </summary>
    public static bool ShipHasPartByInternalName(string internalPartName, Vessel vessel = null)
    {
        var parts = GetCurrentParts(vessel);
        if (parts == null) return false;

        // Prefer comparing against partInfo.name (the internal name).
        return parts.Any(p => p?.partInfo != null && string.Equals(p.partInfo.name, internalPartName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Check by display Title (AvailablePart.title). Less reliable than internal name, but sometimes convenient.
    /// </summary>
    public static bool ShipHasPartByTitle(string partTitle, Vessel vessel = null)
    {
        var parts = GetCurrentParts(vessel);
        if (parts == null) return false;

        return parts.Any(p => p?.partInfo != null && string.Equals(p.partInfo.title, partTitle, StringComparison.Ordinal));
    }

    public static int ShipModulesCount<T>(Vessel vessel = null) where T : PartModule
    {
        var parts = GetCurrentParts(vessel);
        if (parts == null) return 0;

        return vessel.FindPartModulesImplementing<T>().Count;
    }

    public static int ShipModulesCount<T>(ShipConstruct vessel = null) where T : PartModule
    {
        var parts = vessel.parts;
        if (parts == null) return 0;
        int cnt = 0;
        for (int i = 0; i < parts.Count; i++)
        {
            cnt += parts[i].FindModulesImplementing<T>().Count;
        }
        return cnt;
    }





    /// <summary>
    /// For UNLOADED vessels (e.g., from a save or when a vessel is packed), check the ProtoVessel.
    /// Compares the proto part snapshot's name (internal name).
    /// </summary>
    public static bool ProtoHasPartByInternalName(ProtoVessel proto, string internalPartName)
    {
        if (proto?.protoPartSnapshots == null) return false;
        // protoPartSnapshots[i].partName holds the INTERNAL name.
        return proto.protoPartSnapshots.Any(ps => string.Equals(ps.partName, internalPartName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Resolve an AvailablePart once and compare by reference (fast & exact).
    /// </summary>
    public static bool ShipHasPart(AvailablePart ap, Vessel vessel = null)
    {
        if (ap == null) return false;
        var parts = GetCurrentParts(vessel);
        if (parts == null) return false;

        return parts.Any(p => p?.partInfo == ap);
    }
}
