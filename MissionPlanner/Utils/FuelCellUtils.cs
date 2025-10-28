// File: FuelCellUtils.cs
// KSP1 utility: detect fuel cells (stock + common mods), list parts, and estimate EC/s
// C# 7.3 compatible (no target-typed 'new', no null checks on struct ResourceRatio)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class FuelCellUtils
{
    private const string EC = "ElectricCharge";

    // ─────────────────────────────────────────────────────────────────────────────
    // Mod “registry”: patterns and hints for fuel-cell-like modules.
    // Extend this list for additional mods or custom modules.
    // ─────────────────────────────────────────────────────────────────────────────
    private static readonly List<ModFuelCellPattern> Patterns = new List<ModFuelCellPattern>
    {
        // Generic catch-all: most mods keep "FuelCell" in the module name
        new ModFuelCellPattern(
            new[] { "FuelCell" },
            new[] { "ecps", "ecPerSec", "currentEC", "currentOutput", "outputEC", "outputRate", "flowRate", "chargeRate", "power" },
            new[] { "nominalEC", "ratedEC", "chargeRate", "outputRate", "power" },
            new[] { "LiquidFuel", "Oxidizer" }
        ),

        // Kerbalism: often exposes EC variables and H2/O2 processes
        new ModFuelCellPattern(
            new[] { "Kerbalism" },
            new[] { "ec_rate", "ecRate", "currentEC", "outputEC", "flowRate", "power" },
            new[] { "nominalEC", "ratedEC", "chargeRate", "power" },
            new[] { "Hydrogen", "Oxygen" }
        ),

        // USI / Umbra: many converters derive from MRC; keep pattern for non-MRC cases too
        new ModFuelCellPattern(
            new[] { "USI", "Umbra", "USIConverter" },
            new[] { "ecps", "ecPerSec", "currentEC", "outputEC", "outputRate", "chargeRate" },
            new[] { "nominalEC", "ratedEC", "chargeRate", "outputRate" },
            new[] { "LiquidFuel", "Oxidizer" }
        ),

        // Near Future Electrical (catch-all); some parts still MRC, others expose fields
        new ModFuelCellPattern(
            new[] { "NearFuture", "NFE", "Near Future" },
            new[] { "ecps", "ecPerSec", "currentEC", "outputEC", "outputRate", "chargeRate", "power" },
            new[] { "nominalEC", "ratedEC", "chargeRate", "outputRate", "power" },
            new[] { "LqdHydrogen", "Oxygen" }
        )
    };

    private sealed class ModFuelCellPattern
    {
        public readonly string[] NameContains;
        public readonly string[] FlightRateFields;
        public readonly string[] EditorRateFields;
        public readonly string[] InputHints;

        public ModFuelCellPattern(string[] nameContains, string[] flightRateFields, string[] editorRateFields, string[] inputHints)
        {
            NameContains = nameContains ?? new string[0];
            FlightRateFields = flightRateFields ?? new string[0];
            EditorRateFields = editorRateFields ?? new string[0];
            InputHints = inputHints ?? new string[0];
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Core matchers
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool IsStockFuelCell(PartModule m)
    {
        var mrc = m as ModuleResourceConverter;
        return mrc != null
               && mrc.outputList != null
               && mrc.outputList.Any(o => string.Equals(o.ResourceName, EC, StringComparison.OrdinalIgnoreCase) && o.Ratio > 0);
    }

    private static ModFuelCellPattern MatchPattern(PartModule m)
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

    private static bool IsModFuelCell(PartModule m) => !IsStockFuelCell(m) && MatchPattern(m) != null;

    private static IEnumerable<PartModule> FuelCellModules(Part p)
    {
        if (p == null) return Enumerable.Empty<PartModule>();
        return p.Modules.Cast<PartModule>().Where(m => IsStockFuelCell(m) || IsModFuelCell(m));
    }

    private static IEnumerable<PartModule> FuelCellModules(Vessel v)
    {
        if (v == null) return Enumerable.Empty<PartModule>();
        return v.Parts.SelectMany(FuelCellModules);
    }

    private static IEnumerable<PartModule> FuelCellModules(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return Enumerable.Empty<PartModule>();
        return ship.parts.SelectMany(FuelCellModules);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Presence + lists
    // ─────────────────────────────────────────────────────────────────────────────

    public static bool HasFuelCellsFlight(Vessel v) => FuelCellModules(v).Any();
    public static bool HasFuelCellsEditor(ShipConstruct ship) => FuelCellModules(ship).Any();

    public static List<Part> GetFuelCellPartsFlight(Vessel v)
        => v == null ? new List<Part>() : v.Parts.Where(p => FuelCellModules(p).Any()).ToList();

    public static List<Part> GetFuelCellPartsEditor(ShipConstruct ship)
        => ship == null || ship.parts == null ? new List<Part>() : ship.parts.Where(p => FuelCellModules(p).Any()).ToList();

    // ─────────────────────────────────────────────────────────────────────────────
    // Generation summaries (mirrors your solar structure)
    // ─────────────────────────────────────────────────────────────────────────────

    public struct FuelCellGenerationSummary
    {
        public double TotalECps;        // EC/s now (flight) or nominal (editor)
        public int StockContributors;
        public int ModContributors;
        public int ModUnknown;       // detected but no readable rate
        public int TotalFuelCellParts { get { return StockContributors + ModContributors + ModUnknown; } }

    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Flight: current EC/s (requires converters running)
    // ─────────────────────────────────────────────────────────────────────────────

    public static FuelCellGenerationSummary GetEstimatedECGenerationFlight(Vessel v)
    {
        var s = new FuelCellGenerationSummary();
        if (v == null) return s;

        foreach (var pm in FuelCellModules(v))
        {
            // Stock MRC
            var mrc = pm as ModuleResourceConverter;
            if (mrc != null)
            {
                bool running = false;
                try { running = mrc.IsActivated || mrc.ModuleIsActive(); } catch { }
                if (!running) continue;

                s.TotalECps += GetStockECps(mrc);
                s.StockContributors++;
                continue;
            }

            // Modded via registry + reflection
            var pat = MatchPattern(pm);
            if (pat != null)
            {
                double rate;
                if (TryReadDouble(pm, out rate, pat.FlightRateFields))
                {
                    s.TotalECps += Math.Max(0, rate);
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

    // ─────────────────────────────────────────────────────────────────────────────
    // Editor: nominal EC/s (ideal conditions; converters assumed on)
    // ─────────────────────────────────────────────────────────────────────────────

    public static FuelCellGenerationSummary GetEstimatedECGenerationEditor(ShipConstruct ship)
    {
        var s = new FuelCellGenerationSummary();
        if (ship == null || ship.parts == null) return s;

        foreach (var p in ship.parts)
        {
            foreach (var pm in FuelCellModules(p))
            {
                // Stock nominal from recipe
                var mrc = pm as ModuleResourceConverter;
                if (mrc != null)
                {
                    s.TotalECps += GetStockECps(mrc);
                    s.StockContributors++;
                    continue;
                }

                // Mod nominal via registry + reflection
                var pat = MatchPattern(pm);
                if (pat != null)
                {
                    double rate;
                    // Try editor names first, then flight ones as fallback
                    var candidates = pat.EditorRateFields.Concat(pat.FlightRateFields).ToArray();
                    if (TryReadDouble(pm, out rate, candidates))
                    {
                        s.TotalECps += Math.Max(0, rate);
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
    // Potential EC if activated (inputs present) – flight + editor
    // ─────────────────────────────────────────────────────────────────────────────

    public static double GetPotentialECIfActivatedFlight(Vessel v)
    {
        if (v == null) return 0;
        double total = 0;

        foreach (var pm in FuelCellModules(v))
        {
            var mrc = pm as ModuleResourceConverter;
            if (mrc != null)
            {
                if (HasInputsAvailableFlight(v, mrc))
                    total += GetStockECps(mrc);
                continue;
            }

            var pat = MatchPattern(pm);
            if (pat == null) continue;

            if (!InputsPresentFlight(v, pat.InputHints)) continue;

            double rate;
            var candidates = pat.EditorRateFields.Concat(pat.FlightRateFields).ToArray();
            if (TryReadDouble(pm, out rate, candidates))
                total += Math.Max(0, rate);
        }

        return total;
    }

    public static double GetPotentialECIfActivatedEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return 0;
        double total = 0;

        foreach (var p in ship.parts)
        {
            foreach (var pm in FuelCellModules(p))
            {
                var mrc = pm as ModuleResourceConverter;
                if (mrc != null)
                {
                    if (HasInputsAvailableEditor(ship, mrc))
                        total += GetStockECps(mrc);
                    continue;
                }

                var pat = MatchPattern(pm);
                if (pat == null) continue;

                if (!CapacitiesPresentEditor(ship, pat.InputHints)) continue;

                double rate;
                var candidates = pat.EditorRateFields.Concat(pat.FlightRateFields).ToArray();
                if (TryReadDouble(pm, out rate, candidates))
                    total += Math.Max(0, rate);
            }
        }

        return total;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Stock helpers (recipes + EC rate)
    // ─────────────────────────────────────────────────────────────────────────────

    private static double GetStockECps(ModuleResourceConverter mrc)
    {
        if (mrc == null) return 0;

        double ec = 0;
        if (mrc.outputList != null)
        {
            ec = mrc.outputList
                .Where(o => string.Equals(o.ResourceName, EC, StringComparison.OrdinalIgnoreCase) && o.Ratio > 0)
                .Sum(o => (double)o.Ratio);
        }

        double eff = 1.0;
        try { eff = Math.Max(0.0, mrc.EfficiencyBonus); } catch { }
        return Math.Max(0, ec * eff);
    }

    private static IEnumerable<(string name, double ratio)> GetStockInputs(ModuleResourceConverter mrc)
    {
        if (mrc == null || mrc.inputList == null) return Enumerable.Empty<(string, double)>();
        return mrc.inputList
                 .Where(i => i.Ratio > 0)
                 .Select(i => (i.ResourceName, (double)i.Ratio));
    }

    public static bool HasInputsAvailableFlight(Vessel v, ModuleResourceConverter mrc)
    {
        if (v == null || mrc == null) return false;
        foreach (var pair in GetStockInputs(mrc))
        {
            if (TotalAmountFlight(v, pair.name) <= 0) return false;
        }
        return true;
    }

    public static bool HasInputsAvailableEditor(ShipConstruct ship, ModuleResourceConverter mrc)
    {
        if (ship == null || mrc == null) return false;
        foreach (var pair in GetStockInputs(mrc))
        {
            if (TotalCapacityEditor(ship, pair.name) <= 0) return false;
        }
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Vessel/craft resource aggregations
    // ─────────────────────────────────────────────────────────────────────────────

    private static double TotalAmountFlight(Vessel v, string resourceName)
    {
        if (v == null || v.Parts == null) return 0.0;
        double total = 0.0;
        foreach (var p in v.Parts)
        {
            var r = p.Resources.Get(resourceName);
            if (r != null) total += r.amount;
        }
        return total;
    }

    private static double TotalCapacityEditor(ShipConstruct ship, string resourceName)
    {
        if (ship == null || ship.parts == null) return 0.0;
        double total = 0.0;
        foreach (var p in ship.parts)
        {
            var r = p.Resources.Get(resourceName);
            if (r != null) total += r.maxAmount;
        }
        return total;
    }

    private static bool InputsPresentFlight(Vessel v, IEnumerable<string> names)
    {
        if (v == null || names == null) return true; // no hints => don't block
        foreach (var n in names)
        {
            if (!string.IsNullOrEmpty(n) && TotalAmountFlight(v, n) <= 0) return false;
        }
        return true;
    }

    private static bool CapacitiesPresentEditor(ShipConstruct ship, IEnumerable<string> names)
    {
        if (ship == null || ship.parts == null || names == null) return true;
        foreach (var n in names)
        {
            if (!string.IsNullOrEmpty(n) && TotalCapacityEditor(ship, n) <= 0) return false;
        }
        return true;
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
