using System;
using System.Linq;
using UnityEngine;

public static class VesselResourceQuery
{
    // ===== Public entry points =====

    // Pass a resource by NAME (e.g., "LiquidFuel")
    public static bool TryGet(Vessel v, string resourceName, out double amount, out double capacity, bool includeLocked = true)
    {
        amount = capacity = 0;
        if (v == null || string.IsNullOrEmpty(resourceName)) return false;

        var lib = PartResourceLibrary.Instance;
        var def = lib?.GetDefinition(resourceName);
        if (def == null) return false;

        return TryGet(v, def.id, out amount, out capacity, includeLocked);
    }

    // Pass a resource by ID (slightly faster if you already have it)
    public static bool TryGet(Vessel v, int resourceId, out double amount, out double capacity, bool includeLocked = true)
    {
        amount = capacity = 0;
        if (v == null) return false;

        // If the vessel is loaded (has live Part objects), use the fast loaded path.
        if (v.loaded && v.parts != null && v.parts.Count > 0)
            return TryGetLoaded(v, resourceId, out amount, out capacity, includeLocked);

        // Otherwise, fall back to the ProtoVessel snapshot.
        return TryGet(v.protoVessel, resourceId, out amount, out capacity, includeLocked);
    }

    // Unloaded vessel (ProtoVessel) by NAME
    public static bool TryGet(ProtoVessel pv, string resourceName, out double amount, out double capacity, bool includeLocked = true)
    {
        amount = capacity = 0;
        if (pv == null || string.IsNullOrEmpty(resourceName)) return false;

        bool found = false;
        foreach (var p in pv.protoPartSnapshots)
        {
            if (p == null) continue;
            foreach (var r in p.resources)
            {
                if (!string.Equals(r.resourceName, resourceName, StringComparison.Ordinal)) continue;
                if (!includeLocked && !r.flowState) continue;

                found = true;
                amount += r.amount;
                capacity += r.maxAmount;
            }
        }
        return found;
    }

    // Editor ship (VAB/SPH)
    public static bool TryGet(ShipConstruct ship, string resourceName, out double amount, out double capacity, bool includeLocked = true)
    {
        amount = capacity = 0;
        if (ship == null || string.IsNullOrEmpty(resourceName)) return false;

        bool found = false;
        foreach (var p in ship.parts)
        {
            if (p == null || p.Resources == null) continue;

            var pr = p.Resources[resourceName];
            if (pr == null) continue;
            if (!includeLocked && pr.flowState == false) continue;

            found = true;
            amount += pr.amount;
            capacity += pr.maxAmount;
        }
        return found;
    }

    // ===== Private helpers =====

    private static bool TryGetLoaded(Vessel v, int resourceId, out double amount, out double capacity, bool includeLocked)
    {
        amount = capacity = 0;
        bool found = false;

        foreach (var p in v.parts)
        {
            if (p == null || p.Resources == null) continue;

            // Fast: lookup by id
            var pr = p.Resources.Get(resourceId);
            if (pr == null) continue;

            if (!includeLocked && pr.flowState == false) continue;

            found = true;
            amount += pr.amount;
            capacity += pr.maxAmount;
        }
        return found;
    }

    private static bool TryGet(ProtoVessel pv, int resourceId, out double amount, out double capacity, bool includeLocked)
    {
        amount = capacity = 0;
        if (pv == null) return false;

        // Map id -> name once for proto comparison
        var def = PartResourceLibrary.Instance?.GetDefinition(resourceId);
        if (def == null) return false;
        string targetName = def.name;

        bool found = false;
        foreach (var p in pv.protoPartSnapshots)
        {
            if (p == null) continue;
            foreach (var r in p.resources)
            {
                if (!string.Equals(r.resourceName, targetName, StringComparison.Ordinal)) continue;
                if (!includeLocked && !r.flowState) continue;

                found = true;
                amount += r.amount;
                capacity += r.maxAmount;
            }
        }
        return found;
    }
}
