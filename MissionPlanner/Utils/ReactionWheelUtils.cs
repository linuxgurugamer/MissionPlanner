// File: ReactionWheelUtils.cs
// KSP1 utility: detect reaction wheels (stock + common mods), list parts, and summarize torque
// C# 7.3 compatible (no target-typed 'new')

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class ReactionWheelUtils
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Mod matcher: many mods keep "ReactionWheel" in module name even if not stock type
    // ─────────────────────────────────────────────────────────────────────────────

    private static bool IsStockWheel(PartModule m) => m is ModuleReactionWheel;

    private static bool IsModWheel(PartModule m)
    {
        if (m == null || IsStockWheel(m)) return false;
        string name = m.moduleName ?? m.GetType().Name;
        return name.IndexOf("ReactionWheel", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsAnyWheel(PartModule m) => IsStockWheel(m) || IsModWheel(m);

    private static IEnumerable<PartModule> WheelModules(Part p)
    {
        if (p == null) return Enumerable.Empty<PartModule>();
        return p.Modules.Cast<PartModule>().Where(IsAnyWheel);
    }

    private static IEnumerable<PartModule> WheelModules(Vessel v)
        => v == null ? Enumerable.Empty<PartModule>() : v.Parts.SelectMany(WheelModules);

    private static IEnumerable<PartModule> WheelModules(ShipConstruct ship)
        => ship == null || ship.parts == null ? Enumerable.Empty<PartModule>() : ship.parts.SelectMany(WheelModules);

    // ─────────────────────────────────────────────────────────────────────────────
    // Presence & counts
    // ─────────────────────────────────────────────────────────────────────────────

    public static bool HasReactionWheelsFlight(Vessel v) => WheelModules(v).Any();
    public static bool HasReactionWheelsEditor(ShipConstruct ship) => WheelModules(ship).Any();

    public static int CountReactionWheelsFlight(Vessel v)
    {
        if (v == null) return 0;
        return v.Parts.Sum(p => WheelModules(p).Count());
    }

    public static int CountReactionWheelsEditor(ShipConstruct ship)
    {
        if (ship == null || ship.parts == null) return 0;
        int n = 0;
        foreach (var p in ship.parts) n += WheelModules(p).Count();
        return n;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Part lists
    // ─────────────────────────────────────────────────────────────────────────────

    public static List<Part> GetReactionWheelPartsFlight(Vessel v)
        => v == null ? new List<Part>() : v.Parts.Where(p => WheelModules(p).Any()).ToList();

    public static List<Part> GetReactionWheelPartsEditor(ShipConstruct ship)
        => ship == null || ship.parts == null ? new List<Part>() : ship.parts.Where(p => WheelModules(p).Any()).ToList();

    // ─────────────────────────────────────────────────────────────────────────────
    // Torque summaries (Pitch/Yaw/Roll)
    // Editor: nominal torque (design-time)
    // Flight: sum only from modules currently enabled (best-effort)
    // ─────────────────────────────────────────────────────────────────────────────

    public struct TorqueSummary
    {
        public double Pitch;
        public double Yaw;
        public double Roll;
        public int StockContributors;
        public int ModContributors;
        public int ModUnknown;
        public double TotalAxisMax => Math.Max(Pitch, Math.Max(Yaw, Roll));
        public int Total { get { return StockContributors + ModContributors + ModUnknown; } }
    }

    // Editor: nominal (reads PitchTorque/YawTorque/RollTorque)
    public static TorqueSummary GetNominalTorqueEditor(ShipConstruct ship)
    {
        var s = new TorqueSummary();
        if (ship == null || ship.parts == null) return s;

        foreach (var p in ship.parts)
        {
            foreach (var pm in WheelModules(p))
            {
                var stock = pm as ModuleReactionWheel;
                if (stock != null)
                {
                    s.Pitch += SafeTorque(stock.PitchTorque);
                    s.Yaw += SafeTorque(stock.YawTorque);
                    s.Roll += SafeTorque(stock.RollTorque);
                    s.StockContributors++;
                    continue;
                }

                // Mod wheel via reflection
                double pitch, yaw, roll;
                if (TryGetTorques(pm, out pitch, out yaw, out roll))
                {
                    s.Pitch += pitch; s.Yaw += yaw; s.Roll += roll;
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

    // Flight: best-effort "current" (only enabled modules counted)
    // (KSP1 doesn't expose per-axis live throttling for wheels; we approximate with nominal when enabled)
    public static TorqueSummary GetEnabledTorqueFlight(Vessel v)
    {
        var s = new TorqueSummary();
        if (v == null) return s;

        foreach (var pm in WheelModules(v))
        {
            // Skip disabled/broken parts if possible
            if (pm == null || pm.part == null) continue;
            if (!pm.isEnabled) continue; // PartModule toggle in PAW
            if (pm.part.State == PartStates.DEAD) continue;

            var stock = pm as ModuleReactionWheel;
            if (stock != null)
            {
                s.Pitch += SafeTorque(stock.PitchTorque);
                s.Yaw += SafeTorque(stock.YawTorque);
                s.Roll += SafeTorque(stock.RollTorque);
                s.StockContributors++;
                continue;
            }

            // Mod wheel via reflection
            double pitch, yaw, roll;
            if (TryGetTorques(pm, out pitch, out yaw, out roll))
            {
                s.Pitch += pitch; s.Yaw += yaw; s.Roll += roll;
                s.ModContributors++;
            }
            else
            {
                s.ModUnknown++;
            }
        }

        return s;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Optional: per-part breakdown (flight)
    // ─────────────────────────────────────────────────────────────────────────────

    public static IEnumerable<(Part part, double pitch, double yaw, double roll)> GetEnabledWheelBreakdownFlight(Vessel v)
    {
        if (v == null) yield break;

        foreach (var p in v.Parts)
        {
            foreach (var pm in WheelModules(p))
            {
                if (pm == null || !pm.isEnabled || pm.part.State == PartStates.DEAD) continue;

                var stock = pm as ModuleReactionWheel;
                if (stock != null)
                {
                    yield return (p, SafeTorque(stock.PitchTorque), SafeTorque(stock.YawTorque), SafeTorque(stock.RollTorque));
                    continue;
                }

                double pitch, yaw, roll;
                if (TryGetTorques(pm, out pitch, out yaw, out roll))
                    yield return (p, pitch, yaw, roll);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────

    private static double SafeTorque(float t) => t < 0 ? 0 : (double)t;

    // Try to read torque fields from a non-stock module via reflection
    // Common names across mods (case-insensitive).
    private static bool TryGetTorques(PartModule m, out double pitch, out double yaw, out double roll)
    {
        pitch = yaw = roll = 0;
        if (m == null) return false;

        // Preferred names matching stock
        bool okP = TryReadDouble(m, out pitch, "PitchTorque", "pitchTorque", "pitch", "torquePitch");
        bool okY = TryReadDouble(m, out yaw, "YawTorque", "yawTorque", "yaw", "torqueYaw");
        bool okR = TryReadDouble(m, out roll, "RollTorque", "rollTorque", "roll", "torqueRoll");

        return okP | okY | okR; // at least one axis found
        // (You can add vector-style fields here if a mod exposes a combined struct)
    }

    // Reflection reader: double/float on public fields or props (case-insensitive)
    private static bool TryReadDouble(PartModule m, out double value, params string[] names)
    {
        value = 0;
        if (m == null || names == null || names.Length == 0) return false;

        var t = m.GetType();

        // Properties first
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

        // Then fields
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
}
