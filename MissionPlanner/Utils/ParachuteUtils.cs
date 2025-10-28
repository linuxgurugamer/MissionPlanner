// File: ParachuteUtils.cs
// KSP1 utility: detect parachutes (stock + RealChute), list parts, count, and summarize drag
// C# 7.3 compatible (no target-typed 'new')

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;

public static class ParachuteUtils
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Core matchers
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool IsStockParachute(PartModule m) => m is ModuleParachute;

    // RealChute and friends commonly have "RealChute" in the module name (e.g., RealChuteModule, ProceduralChute)
    private static bool IsRealChute(PartModule m)
    {
        if (m == null) return false;
        if (IsStockParachute(m)) return false;
        var name = m.moduleName ?? m.GetType().Name;
        return name.IndexOf("RealChute", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsAnyParachute(PartModule m) => IsStockParachute(m) || IsRealChute(m);

    private static IEnumerable<PartModule> ParachuteModules(Part p)
    {
        if (p == null) return Enumerable.Empty<PartModule>();
        return p.Modules.Cast<PartModule>().Where(IsAnyParachute);
    }

    private static IEnumerable<PartModule> ParachuteModules(Vessel v)
    {
        if (v == null) return Enumerable.Empty<PartModule>();
        return v.Parts.SelectMany(ParachuteModules);
    }

    private static IEnumerable<PartModule> ParachuteModules(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return Enumerable.Empty<PartModule>();
        return ship.parts.SelectMany(ParachuteModules);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Presence & counts
    // ─────────────────────────────────────────────────────────────────────────────

    public static bool HasParachutesFlight(Vessel v) => ParachuteModules(v).Any();
    public static bool HasParachutesEditor(ShipConstruct ship) => ParachuteModules(ship).Any();

    public static int CountParachutesFlight(Vessel v)
    {
        if (v == null) return 0;
        return v.Parts.Sum(p => ParachuteModules(p).Count());
    }

    public static int CountParachutesEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return 0;
        int n = 0;
        foreach (var p in ship.parts) n += ParachuteModules(p).Count();
        return n;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Part lists
    // ─────────────────────────────────────────────────────────────────────────────

    public static List<Part> GetParachutePartsFlight(Vessel v)
        => v == null ? new List<Part>() : v.Parts.Where(p => ParachuteModules(p).Any()).ToList();

    public static List<Part> GetParachutePartsEditor(ShipConstruct ship)
        => ship == null || ship.parts == null ? new List<Part>() : ship.parts.Where(p => ParachuteModules(p).Any()).ToList();

    // ─────────────────────────────────────────────────────────────────────────────
    // State breakdown (FLIGHT)
    // ─────────────────────────────────────────────────────────────────────────────

    public struct ParachuteStateCounts
    {
        public int Stowed;    // not deployed
        public int Semi;      // semi-deployed / drogue
        public int Deployed;  // fully deployed
        public int Cut;       // cut / detached
        public int Unknown;   // couldn’t determine
        public int Total => Stowed + Semi + Deployed + Cut + Unknown;
    }

    public static ParachuteStateCounts GetParachuteStateCountsFlight(Vessel v)
    {
        var c = new ParachuteStateCounts();
        if (v == null) return c;

        foreach (var pm in ParachuteModules(v))
        {
            // STOCK
            var stock = pm as ModuleParachute;
            if (stock != null)
            {
                // ModuleParachute.deploymentState is an enum; map to our buckets
                var state = stock.deploymentState; // values like STOWED, SEMIDEPLOYED, DEPLOYED, CUT
                var s = state.ToString().ToUpperInvariant();
                if (s.Contains("STOW")) c.Stowed++;
                else if (s.Contains("SEMI")) c.Semi++;
                else if (s.Contains("DEPLOY")) c.Deployed++; // covers DEPLOYED
                else if (s.Contains("CUT")) c.Cut++;
                else c.Unknown++;
                continue;
            }

            // REALCHUTE (heuristic via reflection)
            if (IsRealChute(pm))
            {
                // Common indicators we attempt:
                //  - bool deployed / isDeployed
                //  - string deploymentState (e.g., "STOWED", "DEPLOYED", "CUT")
                //  - bool isCut / cut
                string sState;
                if (TryReadString(pm, out sState, "deploymentState", "currentDeployment", "chuteState", "state"))
                {
                    var S = sState.ToUpperInvariant();
                    if (S.Contains("CUT")) c.Cut++;
                    else if (S.Contains("SEMI") || S.Contains("DROG")) c.Semi++;
                    else if (S.Contains("DEPLOY")) c.Deployed++;
                    else if (S.Contains("STOW")) c.Stowed++;
                    else c.Unknown++;
                }
                else
                {
                    bool b;
                    // Try booleans as a fallback
                    if (TryReadBool(pm, out b, "isCut", "cut") && b) { c.Cut++; continue; }
                    if (TryReadBool(pm, out b, "isDeployed", "deployed") && b) { c.Deployed++; continue; }
                    if (TryReadBool(pm, out b, "isArmed", "armed") && b) { c.Semi++; continue; } // "armed" approximated as semi/ready
                    c.Stowed++; // default bucket when nothing readable suggests deployment
                }
            }
        }

        return c;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // “Capacity” summary: sum of fully-deployed drag (EDITOR & FLIGHT nominal)
    // ─────────────────────────────────────────────────────────────────────────────

    public struct ParachuteCapacitySummary
    {
        public double TotalFullDrag;    // sum of fully deployed drag across all chutes
        public int StockContributors;
        public int RealChuteContributors;
        public int RealChuteUnknown;    // detected RealChute with no readable drag

        public int Total {  get {  return StockContributors + RealChuteContributors + RealChuteUnknown; } }
    }

    /// <summary>Nominal fully-deployed drag in the editor (ideal / design-time).</summary>
    public static ParachuteCapacitySummary GetParachuteCapacityEditor(ShipConstruct ship)
    {
        var s = new ParachuteCapacitySummary();
        if (ship == null || ship.parts == null) return s;

        foreach (var p in ship.parts)
        {
            foreach (var pm in ParachuteModules(p))
            {
                var stock = pm as ModuleParachute;
                if (stock != null)
                {
                    s.TotalFullDrag += Math.Max(0, stock.fullyDeployedDrag);
                    s.StockContributors++;
                    continue;
                }

                if (IsRealChute(pm))
                {
                    double drag;
                    // RealChute often exposes deployed / semi / reference area/drag; common names below
                    if (TryReadDouble(pm, out drag, "fullyDeployedDrag", "deployedDrag", "fullDrag", "fullDeployedDrag", "dragFullyDeployed", "deployedArea"))
                    {
                        s.TotalFullDrag += Math.Max(0, drag);
                        s.RealChuteContributors++;
                    }
                    else
                    {
                        s.RealChuteUnknown++;
                    }
                }
            }
        }

        return s;
    }

    /// <summary>Nominal fully-deployed drag in flight (does not require being deployed).</summary>
    public static ParachuteCapacitySummary GetParachuteCapacityFlight(Vessel v)
    {
        var s = new ParachuteCapacitySummary();
        if (v == null) return s;

        foreach (var pm in ParachuteModules(v))
        {
            var stock = pm as ModuleParachute;
            if (stock != null)
            {
                s.TotalFullDrag += Math.Max(0, stock.fullyDeployedDrag);
                s.StockContributors++;
                continue;
            }

            if (IsRealChute(pm))
            {
                double drag;
                if (TryReadDouble(pm, out drag, "fullyDeployedDrag", "deployedDrag", "fullDrag", "fullDeployedDrag", "dragFullyDeployed", "deployedArea"))
                {
                    s.TotalFullDrag += Math.Max(0, drag);
                    s.RealChuteContributors++;
                }
                else
                {
                    s.RealChuteUnknown++;
                }
            }
        }

        return s;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Reflection helpers (public fields/properties, case-insensitive)
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool TryReadDouble(PartModule m, out double value, params string[] names)
    {
        value = 0;
        if (m == null || names == null || names.Length == 0) return false;
        var t = m.GetType();

        // properties
        for (int i = 0; i < names.Length; i++)
        {
            var prop = t.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    var val = prop.GetValue(m, null);
                    if (val is double) { value = (double)val; return true; }
                    if (val is float) { value = (float)val; return true; }
                }
                catch { }
            }
        }
        // fields
        for (int i = 0; i < names.Length; i++)
        {
            var field = t.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null)
            {
                try
                {
                    var val = field.GetValue(m);
                    if (val is double) { value = (double)val; return true; }
                    if (val is float) { value = (float)val; return true; }
                }
                catch { }
            }
        }
        return false;
    }

    private static bool TryReadString(PartModule m, out string value, params string[] names)
    {
        value = null;
        if (m == null || names == null || names.Length == 0) return false;
        var t = m.GetType();

        // properties
        for (int i = 0; i < names.Length; i++)
        {
            var prop = t.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    var val = prop.GetValue(m, null);
                    if (val != null) { value = val.ToString(); return true; }
                }
                catch { }
            }
        }
        // fields
        for (int i = 0; i < names.Length; i++)
        {
            var field = t.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null)
            {
                try
                {
                    var val = field.GetValue(m);
                    if (val != null) { value = val.ToString(); return true; }
                }
                catch { }
            }
        }
        return false;
    }

    private static bool TryReadBool(PartModule m, out bool value, params string[] names)
    {
        value = false;
        if (m == null || names == null || names.Length == 0) return false;
        var t = m.GetType();

        // properties
        for (int i = 0; i < names.Length; i++)
        {
            var prop = t.GetProperty(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanRead && prop.PropertyType == typeof(bool))
            {
                try { value = (bool)prop.GetValue(m, null); return true; } catch { }
            }
        }
        // fields
        for (int i = 0; i < names.Length; i++)
        {
            var field = t.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null && field.FieldType == typeof(bool))
            {
                try { value = (bool)field.GetValue(m); return true; } catch { }
            }
        }
        return false;
    }
}
