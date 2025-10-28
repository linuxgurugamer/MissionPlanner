using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static MissionPlanner.RegisterToolbar;
using KSP;

public static class BodyAndAsteroidUtils
{
    // -------------------------- Celestial bodies --------------------------

    public static List<CelestialBody> GetAllCelestialBodies() => FlightGlobals.Bodies;

    // -------------------------- Asteroids (loaded + proto) --------------------------
    
    public static List<Vessel> GetLoadedAsteroids()
    {
        //return new List<Vessel>();
        List<Vessel> rc = new List<Vessel>();
        foreach (var v in FlightGlobals.Vessels
            .Where(v => v != null && v.vesselType == VesselType.SpaceObject))
        {
            int discoveryLevels = int.Parse(v.protoVessel.discoveryInfo.GetValue("state"));
            if (discoveryLevels == 29)
                rc.Add(v);
        }
        return rc;
    }

    public static List<ProtoVessel> GetProtoAsteroids()
    {
        if (HighLogic.CurrentGame?.flightState == null)
            return new List<ProtoVessel>();

        List<ProtoVessel> rc = new List<ProtoVessel>();
        foreach (var p in HighLogic.CurrentGame.flightState.protoVessels
            .Where(pv => pv != null && pv.vesselType == VesselType.SpaceObject))
        {
            int discoveryLevels = int.Parse(p.discoveryInfo.GetValue("state"));
            if (discoveryLevels == 29) 
                rc.Add(p);
        }
        return rc;
    }

    private static CelestialBody GetBodyFromIndex(int index)
    {
        var bodies = FlightGlobals.Bodies;
        return (index >= 0 && index < bodies.Count) ? bodies[index] : null;
    }

    // -------------------------- SOI / proximity --------------------------

    /// True if the vessel is currently in the SOI of the specified body.
    public static bool IsInSOI(Vessel v, CelestialBody body)
    {
        if (v == null || body == null) return false;
        return v.mainBody == body; // orbit.referenceBody also works while on rails
    }

    /// True if within maxDistanceMeters of a loaded asteroid. Outputs the distance.
    public static bool IsNearAsteroid(Vessel v, Vessel asteroid, double maxDistanceMeters, out double distance)
    {
        distance = double.NaN;
        if (v == null || asteroid == null) return false;
        if (asteroid.vesselType != VesselType.SpaceObject) return false;

        Vector3d p1 = v.GetWorldPos3D();
        Vector3d p2 = asteroid.GetWorldPos3D();
        distance = Vector3d.Distance(p1, p2);
        return distance <= Math.Max(0.0, maxDistanceMeters);
    }

    /// Unified check where target may be a CelestialBody or asteroid Vessel.
    public static bool IsInSOIOrNear(Vessel v, object target, double maxDistanceMeters, out double distance)
    {
        distance = double.NaN;

        if (target is CelestialBody cb) return IsInSOI(v, cb);

        if (target is Vessel ast && ast.vesselType == VesselType.SpaceObject)
            return IsNearAsteroid(v, ast, maxDistanceMeters, out distance);

        return false;
    }

    // Optional: find nearest loaded asteroid to the given vessel.
    public static (Vessel asteroid, double distance) FindNearestLoadedAsteroid(Vessel v)
    {
        Vessel best = null;
        double bestDist = double.MaxValue;

        foreach (var a in GetLoadedAsteroids())
        {
            double d = Vector3d.Distance(v.GetWorldPos3D(), a.GetWorldPos3D());
            if (d < bestDist)
            {
                bestDist = d;
                best = a;
            }
        }
        return (best, best == null ? double.NaN : bestDist);
    }

    // -------------------------- ASTEROID SUMMARY --------------------------

    /// Human-readable summary lines for all asteroids (loaded + proto).
    /// For proto asteroids we resolve the body from orbitSnapShot.ReferenceBodyIndex.
    public static List<string> GetAsteroidSummary()
    {
        var lines = new List<string>();
        HashSet<string> unique = new HashSet<string>();

        // Loaded
        foreach (var v in GetLoadedAsteroids())
        {
            string body = v.mainBody?.bodyName ?? "Unknown";
            //lines.Add($"[Loaded] {v.vesselName}");
            unique.Add(v.vesselName);
        }

        // Proto
        foreach (var pv in GetProtoAsteroids())
        {
            string name = string.IsNullOrEmpty(pv.vesselName) ? "(Unnamed)" : pv.vesselName;
            //string body = "Unknown";
            //if (pv.orbitSnapShot != null)
            //{
            //    var cb = GetBodyFromIndex(pv.orbitSnapShot.ReferenceBodyIndex);
            //    if (cb != null) body = cb.bodyName;
            //}
            //lines.Add($"[Proto ] {name}");
            unique.Add(name);
        }
        return unique.ToList(); ;
        return lines;
    }

    /// Counts asteroids per body (loaded + proto). Uses CelestialBody as key.
    public static Dictionary<CelestialBody, int> GetAsteroidCountsPerBody()
    {
        var counts = new Dictionary<CelestialBody, int>();

        // Count loaded
        foreach (var v in GetLoadedAsteroids())
        {
            var cb = v.mainBody;
            if (cb == null) continue;
            counts[cb] = counts.TryGetValue(cb, out var n) ? n + 1 : 1;
        }

        // Count proto (by ReferenceBodyIndex)
        foreach (var pv in GetProtoAsteroids())
        {
            if (pv.orbitSnapShot == null) continue;
            var cb = GetBodyFromIndex(pv.orbitSnapShot.ReferenceBodyIndex);
            if (cb == null) continue;
            counts[cb] = counts.TryGetValue(cb, out var n) ? n + 1 : 1;
        }

        return counts;
    }

    // Convenience logger: bodies + asteroid lists + counts.
    static bool logged = false;
    public static void LogAll()
    {
        if (logged) return;
        logged = true;
        Debug.Log("[BodyAndAsteroidUtils] ---- Celestial Bodies ----");
        foreach (var cb in GetAllCelestialBodies())
            Debug.Log($"[Body] {cb.bodyName} | radius={cb.Radius:F0} m | mass={cb.Mass:E2}");

        Debug.Log("[BodyAndAsteroidUtils] ---- Asteroids (Loaded + Proto) ----");
        var lines = GetAsteroidSummary();
        if (lines.Count == 0) Debug.Log("[Asteroids] None");
        else foreach (var s in lines) Debug.Log(s);

        Debug.Log("[BodyAndAsteroidUtils] ---- Asteroid Counts Per Body ----");
        var counts = GetAsteroidCountsPerBody();
        if (counts.Count == 0) Debug.Log("[Counts] None");
        else
        {
            foreach (var kv in counts.OrderByDescending(kv => kv.Value))
                Debug.Log($"[Count] {kv.Key.bodyName}: {kv.Value}");
        }
    }
}

