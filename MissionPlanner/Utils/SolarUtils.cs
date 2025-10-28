using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class SolarUtils
{
    // ---------- core helpers ----------

    private static bool IsStockSolarModule(PartModule m) => m is ModuleDeployableSolarPanel;

    private static bool IsModSolarModule(PartModule m)
        => !IsStockSolarModule(m)
           && m != null
           && !string.IsNullOrEmpty(m.moduleName)
           && m.moduleName.IndexOf("SolarPanel", StringComparison.OrdinalIgnoreCase) >= 0;

    private static IEnumerable<PartModule> SolarModules(Part p)
        => p == null ? Enumerable.Empty<PartModule>()
                     : p.Modules.Cast<PartModule>().Where(m => IsStockSolarModule(m) || IsModSolarModule(m));

    private static IEnumerable<PartModule> SolarModules(Vessel v)
        => v == null ? Enumerable.Empty<PartModule>()
                     : v.Parts.SelectMany(SolarModules);

    private static IEnumerable<PartModule> SolarModules(ShipConstruct ship)
        => ship?.parts == null ? Enumerable.Empty<PartModule>()
                               : ship.parts.SelectMany(SolarModules);

    // ---------- EC generation summary struct ----------
    public struct SolarGenerationSummary
    {
        public double TotalECps;
        public int StockContributors;
        public int ModContributors;
        public int ModUnknown;

        public int TotalSolarParts {  get { return StockContributors + ModContributors + ModUnknown; } }
    }

    // ---------- flight estimate ----------
    public static SolarGenerationSummary GetEstimatedECGenerationFlight(Vessel v)
    {
        var summary = new SolarGenerationSummary();
        if (v == null) return summary;

        foreach (var pm in SolarModules(v))
        {
            // Stock
            if (pm is ModuleDeployableSolarPanel sp)
            {
                if (sp.deployState.ToString().Equals("BROKEN", StringComparison.OrdinalIgnoreCase))
                    continue;

                summary.TotalECps += Math.Max(0, sp.flowRate);
                summary.StockContributors++;
                continue;
            }

            // Mod
            if (IsModSolarModule(pm))
            {
                if (TryGetDoubleByName(pm, out var value,
                    "flowRate", "currentOutput", "outputRate", "ecRate", "ecOutput", "chargeRate", "power", "currentEc"))
                {
                    summary.TotalECps += Math.Max(0, value);
                    summary.ModContributors++;
                }
                else
                {
                    summary.ModUnknown++;
                }
            }
        }

        return summary;
    }

    // ---------- editor estimate ----------
    public static SolarGenerationSummary GetEstimatedECGenerationEditor(ShipConstruct ship)
    {
        var summary = new SolarGenerationSummary();
        if (ship == null || ship.parts == null) return summary;

        foreach (var p in ship.parts)
        {
            foreach (var pm in SolarModules(p))
            {
                // Stock
                if (pm is ModuleDeployableSolarPanel sp)
                {
                    summary.TotalECps += Math.Max(0, sp.chargeRate);
                    summary.StockContributors++;
                    continue;
                }

                // Mod
                if (IsModSolarModule(pm))
                {
                    if (TryGetDoubleByName(pm, out var value,
                        "chargeRate", "flowRate", "nominalOutput", "ecRate", "outputRate", "power"))
                    {
                        summary.TotalECps += Math.Max(0, value);
                        summary.ModContributors++;
                    }
                    else
                    {
                        summary.ModUnknown++;
                    }
                }
            }
        }

        return summary;
    }

    // ---------- reflection helper ----------
    private static bool TryGetDoubleByName(PartModule m, out double value, params string[] names)
    {
        value = 0;
        if (m == null) return false;

        var type = m.GetType();
        foreach (var n in names)
        {
            var prop = type.GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    object val = prop.GetValue(m);
                    if (val is double d) { value = d; return true; }
                    if (val is float f) { value = f; return true; }
                }
                catch { }
            }

            var field = type.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field != null)
            {
                try
                {
                    object val = field.GetValue(m);
                    if (val is double d) { value = d; return true; }
                    if (val is float f) { value = f; return true; }
                }
                catch { }
            }
        }
        return false;
    }
}
