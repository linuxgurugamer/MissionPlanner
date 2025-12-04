// File: DeltaVUtils.cs
// KSP1 – compute delta-v + TWR for:
// - Flight: current stage, or (optionally) first stage when on the launchpad
// - Editor: first stage (bottom stage / highest inverseStage)
#if false

using System;
using System.Collections.Generic;
using UnityEngine;

public static class DeltaVUtils
{
    private const double g0 = 9.80665; // m/s^2

    public struct StageInfo
    {
        public double deltaV;   // m/s
        public double TWR;      // dimensionless, at start of burn
    }

    /// <summary>
    /// Convenience: vacuum stats (staticPressureAtm = 0).
    /// - Flight: current stage, or first stage on launchpad (if flag set).
    /// - Editor: first stage.
    /// </summary>
    public static StageInfo GetActiveStageInfo_Vac(bool useLaunchpadFirstStageIfPrelaunch = false)
    {
        return GetActiveStageInfo(0.0, useLaunchpadFirstStageIfPrelaunch);
    }

    /// <summary>
    /// Returns delta-v + TWR for:
    /// - Flight: current stage, or (optionally) first stage on launchpad
    /// - Editor: first stage (bottom stage / highest inverseStage)
    ///
    /// staticPressureAtm:
    ///   - 0.0  => vacuum
    ///   - 1.0  => Kerbin sea level
    ///   - etc.
    /// </summary>
    public static StageInfo GetActiveStageInfo(double staticPressureAtm, bool useLaunchpadFirstStageIfPrelaunch = false)
    {
        StageInfo info = new StageInfo
        {
            deltaV = 0.0,
            TWR = 0.0
        };

        if (HighLogic.LoadedSceneIsFlight)
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v == null || v.parts == null) return info;

            int stage;
            if (useLaunchpadFirstStageIfPrelaunch && v.situation == Vessel.Situations.PRELAUNCH)
            {
                // On the pad: use first stage to be activated (bottom stage)
                stage = GetFirstStageIndex(v.parts);
            }
            else
            {
                // Normal flight case: use the currentStage
                stage = v.currentStage;
            }

            if (stage < 0) return info;

            double gLocal = g0;
            if (v.mainBody != null)
            {
                double r = v.mainBody.Radius + v.altitude;
                if (r > 0.0)
                {
                    gLocal = v.mainBody.gravParameter / (r * r);
                }
                else
                {
                    gLocal = v.mainBody.GeeASL * g0;
                }
            }

            info = GetStageInfoFromParts(v.parts, stage, staticPressureAtm, gLocal);
        }
        else if (HighLogic.LoadedSceneIsEditor)
        {
            EditorLogic ed = EditorLogic.fetch;
            ShipConstruct ship = ed != null ? ed.ship : null;
            if (ship == null || ship.parts == null) return info;

            int stage = GetFirstStageIndex(ship.parts);
            if (stage < 0) return info;

            // Editor: assume home body at sea level (typically Kerbin)
            CelestialBody home = Planetarium.fetch != null ? Planetarium.fetch.Home : null;
            double gLocal = (home != null ? home.GeeASL * g0 : g0);

            info = GetStageInfoFromParts(ship.parts, stage, staticPressureAtm, gLocal);
        }

        return info;
    }

    // ======================
    //   Core calculations
    // ======================

    private static StageInfo GetStageInfoFromParts(IEnumerable<Part> parts, int stage, double staticPressureAtm, double gLocal)
    {
        StageInfo info = new StageInfo
        {
            deltaV = 0.0,
            TWR = 0.0
        };

        if (parts == null || stage < 0 || gLocal <= 0.0)
            return info;

        // 1) Collect engines in this stage and the propellants they use
        var stageEngines = new List<ModuleEngines>();
        var stagePropellants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Part p in parts)
        {
            if (p == null) continue;
            if (p.inverseStage != stage) continue;

            var engines = p.FindModulesImplementing<ModuleEngines>();
            if (engines == null || engines.Count == 0) continue;

            foreach (ModuleEngines eng in engines)
            {
                if (eng == null) continue;
                if (!eng.enabled && !eng.isEnabled) continue;
                if (eng.maxThrust <= 0f) continue;

                stageEngines.Add(eng);

                if (eng.propellants == null) continue;
                foreach (Propellant prop in eng.propellants)
                {
                    if (prop == null) continue;
                    if (string.Equals(prop.name, "IntakeAir", StringComparison.OrdinalIgnoreCase))
                        continue; // ignore intake air as "fuel"

                    stagePropellants.Add(prop.name);
                }
            }
        }

        if (stageEngines.Count == 0 || stagePropellants.Count == 0)
            return info;

        // 2) Total initial mass m0 (all parts, incl. resources)
        double m0 = 0.0;

        foreach (Part p in parts)
        {
            if (p == null) continue;

            double partMass = p.mass;
            double resMass = 0.0;

            if (p.Resources != null)
            {
                foreach (PartResource r in p.Resources)
                {
                    if (r == null || r.info == null) continue;
                    resMass += r.amount * r.info.density;
                }
            }

            m0 += partMass + resMass;
        }

        if (m0 <= 0.0)
            return info;

        // 3) Propellant mass for this stage
        double propellantMass = 0.0;

        foreach (Part p in parts)
        {
            if (p == null || p.Resources == null) continue;

            foreach (PartResource r in p.Resources)
            {
                if (r == null || r.info == null) continue;
                if (!stagePropellants.Contains(r.resourceName)) continue;

                propellantMass += r.amount * r.info.density;
            }
        }

        if (propellantMass <= 0.0)
            return info;

        double m1 = m0 - propellantMass;
        if (m1 <= 0.0 || m1 >= m0)
            return info;

        // 4) Effective Isp and total thrust for this stage
        double totalThrust = 0.0;   // kN
        double thrustOverIsp = 0.0; // kN / s
        float pFloat = (float)staticPressureAtm;

        foreach (ModuleEngines eng in stageEngines)
        {
            if (eng == null) continue;

            float isp = 0f;
            if (eng.atmosphereCurve != null)
                isp = eng.atmosphereCurve.Evaluate(pFloat);

            if (isp <= 0f) continue;

            double thrust = eng.maxThrust; // kN
            if (thrust <= 0.0) continue;

            totalThrust += thrust;
            thrustOverIsp += thrust / isp;
        }

        if (totalThrust <= 0.0 || thrustOverIsp <= 0.0)
            return info;

        double ispEff = totalThrust / thrustOverIsp; // seconds

        // 5) Rocket equation: Δv = Isp * g0 * ln(m0 / m1)
        double dv = ispEff * g0 * Math.Log(m0 / m1);

        // 6) TWR at start of burn: TWR = Thrust / (m0 * gLocal)
        //    Units: thrust (kN), mass (tons), gLocal (m/s^2)
        //    kN and tons both have a *1000 factor that cancels out.
        double twr = totalThrust / (m0 * gLocal);

        info.deltaV = dv;
        info.TWR = twr;
        return info;
    }

    /// <summary>
    /// "First stage to be activated" = bottom stage, highest inverseStage.
    /// Returns -1 if none.
    /// </summary>
    private static int GetFirstStageIndex(IEnumerable<Part> parts)
    {
        if (parts == null) return -1;

        int maxStage = -1;
        foreach (Part p in parts)
        {
            if (p == null) continue;
            if (p.inverseStage > maxStage)
                maxStage = p.inverseStage;
        }

        return maxStage;
    }
}
#endif