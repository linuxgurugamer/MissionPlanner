// File: RadiatorUtils.cs
// KSP1 utility: detect radiators (stock + common mods), list parts, and estimate cooling (kW)
// C# 7.3 compatible (no target-typed 'new')

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class RadiatorUtils
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Mod “registry”: patterns and candidate fields for cooling rate (kW-ish)
    // Extend this with new entries as needed.
    // ─────────────────────────────────────────────────────────────────────────────
    private static readonly List<ModRadPattern> Patterns = new List<ModRadPattern>
    {
        // Generic catch-all: most mod radiator modules keep "Radiator" in the name
        new ModRadPattern(
            new[] { "Radiator" },
            // Flight (current) cooling field/property candidates
            new[] { "currentCooling", "coolingRate", "currentFlux", "heatRadiated", "currentHeatDissipation", "currentRadFlux" },
            // Editor (nominal) cooling field/property candidates
            new[] { "maxEnergyTransfer", "maxCooling", "radiatorMax", "ratedCooling", "nominalCooling" }
        ),

        // HeatControl / Near Future (Nertea)
        new ModRadPattern(
            new[] { "HeatControl", "NearFuture", "NFE", "Near Future" },
            new[] { "currentCooling", "coolingRate", "currentFlux", "heatRadiated" },
            new[] { "maxEnergyTransfer", "maxCooling", "ratedCooling", "nominalCooling" }
        ),

        // SystemHeat (Nertea)
        new ModRadPattern(
            new[] { "SystemHeat" },
            new[] { "currentCooling", "systemHeatCooling", "coolingKW", "currentFlux" },
            new[] { "maxEnergyTransfer", "maxCooling", "ratedCooling", "nominalCooling", "designCooling" }
        )
    };

    private sealed class ModRadPattern
    {
        public readonly string[] NameContains;
        public readonly string[] FlightRateFields;
        public readonly string[] EditorRateFields;

        public ModRadPattern(string[] nameContains, string[] flightRateFields, string[] editorRateFields)
        {
            NameContains = nameContains ?? new string[0];
            FlightRateFields = flightRateFields ?? new string[0];
            EditorRateFields = editorRateFields ?? new string[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Core matchers
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool IsStockRadiator(PartModule m)
    {
        // Stock types: ModuleActiveRadiator (fixed) and ModuleDeployableRadiator (extendable)
        return (m is ModuleActiveRadiator) || (m is ModuleDeployableRadiator);
    }

    private static ModRadPattern MatchPattern(PartModule m)
    {
        if (m == null) return null;
        string name = m.moduleName ?? m.GetType().Name;
        foreach (var pat in Patterns)
        {
            foreach (var s in pat.NameContains)
            {
                if (!string.IsNullOrEmpty(s) && name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    return pat;
            }
        }
        return null;
    }

    private static bool IsModRadiator(PartModule m) => !IsStockRadiator(m) && MatchPattern(m) != null;

    private static IEnumerable<PartModule> RadiatorModules(Part p)
    {
        if (p == null) return Enumerable.Empty<PartModule>();
        return p.Modules.Cast<PartModule>().Where(m => IsStockRadiator(m) || IsModRadiator(m));
    }

    private static IEnumerable<PartModule> RadiatorModules(Vessel v)
    {
        if (v == null) return Enumerable.Empty<PartModule>();
        return v.Parts.SelectMany(RadiatorModules);
    }

    private static IEnumerable<PartModule> RadiatorModules(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return Enumerable.Empty<PartModule>();
        return ship.parts.SelectMany(RadiatorModules);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Presence + lists
    // ─────────────────────────────────────────────────────────────────────────────

    public static bool HasRadiatorsFlight(Vessel v) => RadiatorModules(v).Any();
    public static bool HasRadiatorsEditor(ShipConstruct ship) => RadiatorModules(ship).Any();

    public static List<Part> GetRadiatorPartsFlight(Vessel v)
        => v == null ? new List<Part>() : v.Parts.Where(p => RadiatorModules(p).Any()).ToList();

    public static List<Part> GetRadiatorPartsEditor(ShipConstruct ship)
        => ship == null || ship.parts == null ? new List<Part>() : ship.parts.Where(p => RadiatorModules(p).Any()).ToList();

    // ─────────────────────────────────────────────────────────────────────────────
    // Cooling summaries (kW): Flight (current) & Editor (nominal)
    // ─────────────────────────────────────────────────────────────────────────────

    public class RadiatorCoolingSummary
    {
        public double TotalKW;        // kW-ish now (flight) or nominal (editor)
        public int StockContributors;
        public int ModContributors;
        public int ModUnknown;     // detected but no readable rate
        public int TotalRadiatorParts { get { return StockContributors + ModContributors + ModUnknown; } }

    }

    // Flight: try to read *current* cooling; fall back to nominal for stock if needed
    public static RadiatorCoolingSummary GetEstimatedCoolingFlight(Vessel v)
    {
        var s = new RadiatorCoolingSummary();
        if (v == null) return s;

        foreach (var pm in RadiatorModules(v))
        {
            // STOCK: attempt current value via reflection, else fall back to nominal fields
            if (pm is ModuleActiveRadiator || pm is ModuleDeployableRadiator)
            {
                double kw;
                // Try common "current" names first (varies across KSP versions/modules)
                if (TryReadDouble(pm, out kw, "currentCooling", "coolingRate", "currentRadFlux", "currentFlux", "heatRadiated"))
                {
                    s.TotalKW += Math.Max(0, kw);
                }
                else
                {
                    // Fallback to nominal (editor-like) fields such as maxEnergyTransfer
                    if (TryReadDouble(pm, out kw, "maxEnergyTransfer", "maxCooling", "radiatorMax", "ratedCooling"))
                        s.TotalKW += Math.Max(0, kw);
                }

                s.StockContributors++;
                continue;
            }

            // MOD: registry + reflection
            var pat = MatchPattern(pm);
            if (pat != null)
            {
                double kw;
                // Flight candidates first; then editor as fallback
                var candidates = pat.FlightRateFields.Concat(pat.EditorRateFields).ToArray();
                if (TryReadDouble(pm, out kw, candidates))
                {
                    s.TotalKW += Math.Max(0, kw);
                    s.ModContributors++;
                }
                else
                {
                    s.ModUnknown++;
                }
            }
        }

        return s;
    }

    // Editor: nominal cooling (ideal), read known nominal fields
    public static RadiatorCoolingSummary GetEstimatedCoolingEditor(ShipConstruct ship)
    {
        var s = new RadiatorCoolingSummary();
        if (ship == null || ship.parts == null) return s;

        foreach (var p in ship.parts)
        {
            foreach (var pm in RadiatorModules(p))
            {
                // STOCK: nominal via common fields
                if (pm is ModuleActiveRadiator || pm is ModuleDeployableRadiator)
                {
                    double kw;
                    if (TryReadDouble(pm, out kw, "maxEnergyTransfer", "maxCooling", "radiatorMax", "ratedCooling", "nominalCooling"))
                        s.TotalKW += Math.Max(0, kw);
                    s.StockContributors++;
                    continue;
                }

                // MOD: registry + reflection (prefer nominal candidates)
                var pat = MatchPattern(pm);
                if (pat != null)
                {
                    double kw;
                    var candidates = pat.EditorRateFields.Concat(pat.FlightRateFields).ToArray();
                    if (TryReadDouble(pm, out kw, candidates))
                    {
                        s.TotalKW += Math.Max(0, kw);
                        s.ModContributors++;
                    }
                    else
                    {
                        s.ModUnknown++;
                    }
                }
            }
        }

        return s;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Optional: per-part breakdown for stock radiators (flight current)
    // ─────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<(Part part, double kw)> GetStockRadiatorOutputsFlight(Vessel v)
    {
        if (v == null) yield break;

        foreach (var p in v.Parts)
        {
            foreach (var pm in p.Modules.Cast<PartModule>())
            {
                if (!(pm is ModuleActiveRadiator) && !(pm is ModuleDeployableRadiator)) continue;

                double kw;
                if (!TryReadDouble(pm, out kw, "currentCooling", "coolingRate", "currentRadFlux", "currentFlux", "heatRadiated"))
                {
                    // Fallback to nominal if no live field is readable
                    TryReadDouble(pm, out kw, "maxEnergyTransfer", "maxCooling", "radiatorMax", "ratedCooling");
                }

                if (kw > 0) yield return (p, kw);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Reflection reader: double/float on public fields or props (case-insensitive)
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool TryReadDouble(PartModule m, out double value, params string[] candidateNames)
    {
        value = 0;
        if (m == null || candidateNames == null || candidateNames.Length == 0) return false;

        var t = m.GetType();

        // Properties first
        for (int i = 0; i < candidateNames.Length; i++)
        {
            var n = candidateNames[i];
            var prop = t.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
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

        // Then fields
        for (int i = 0; i < candidateNames.Length; i++)
        {
            var n = candidateNames[i];
            var field = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
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
}
