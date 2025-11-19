using UnityEngine;

using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner
{
    using System;
    using System.Collections.Generic;

    namespace MissionPlanner
    {
        public static class BiomeUtils
        {
            public const string ANYBIOME = "Any";
            static List<string> result = new List<string>();

            public static Utils.DoubleIndexed.ItemRegistry<CBAttributeMapSO.MapAttribute> Biomes = new Utils.DoubleIndexed.ItemRegistry<CBAttributeMapSO.MapAttribute>();

            static string lastBody = "";

            /// <summary>
            /// Returns a list of all biome names for the specified body.
            /// </summary>
            public static List<string> GetBiomes(CelestialBody body)
            {             
                if (lastBody == body.bodyName)
                {
                    return result;
                }
                result.Clear();
                Biomes = new Utils.DoubleIndexed.ItemRegistry<CBAttributeMapSO.MapAttribute>();
                Biomes.Add(new Utils.DoubleIndexed.ItemEntry<CBAttributeMapSO.MapAttribute>(0, ANYBIOME, null));
                result.Add(ANYBIOME);
                if (body == null)
                {
                    Log.Warn("[PlanetBiomeUtils] Body is null");
                    return result;
                }

                if (body.BiomeMap == null || body.BiomeMap.Attributes == null)
                {
                    Log.Warn($"[PlanetBiomeUtils] {body.bodyName} has no biome map.");
                    return result;
                }

                for (int i = 0; i < body.BiomeMap.Attributes.Length; i++)
                {
                    var attr = body.BiomeMap.Attributes[i];
                    if (attr != null && !string.IsNullOrEmpty(attr.name))
                        result.Add(attr.name);
                    Biomes.Add(new Utils.DoubleIndexed.ItemEntry<CBAttributeMapSO.MapAttribute>(i+1, attr.name, attr));
                }

                return result;
            }

            /// <summary>
            /// Gets all biomes for the body by name lookup.
            /// </summary>
            public static List<string> GetBiomes(string bodyName)
            {
                if (lastBody == bodyName)
                {
                    return result;
                }
                result.Clear();
                Biomes = new Utils.DoubleIndexed.ItemRegistry<CBAttributeMapSO.MapAttribute>();

                result.Add(ANYBIOME);
                if (string.IsNullOrEmpty(bodyName)) 
                    return result;

                CelestialBody body = FlightGlobals.Bodies.Find(b =>
                    b != null &&
                    b.bodyName.Equals(bodyName, System.StringComparison.OrdinalIgnoreCase));

                return GetBiomes(body);
            }

            public static string GetCurrentBiome(Vessel vessel)
            {
                if (vessel == null || vessel.mainBody == null)
                    return "Unknown";

                // If landed or splashed, try to get biome by vessel latitude/longitude
                if (vessel.mainBody.BiomeMap != null)
                {
                    double lat = vessel.latitude;
                    double lon = vessel.longitude;

                    // Biome lookup by position
                    string biome = vessel.mainBody.BiomeMap.GetAtt(lat, lon).name;
                    if (!string.IsNullOrEmpty(biome))
                        return biome;
                }

                // If no BiomeMap or not applicable, check situation
                if (!string.IsNullOrEmpty(vessel.landedAt))
                {
                    return Vessel.GetLandedAtString(vessel.landedAt);
                }

                // For flying or orbiting cases, check the Experiment Situations
                switch (vessel.situation)
                {
                    case Vessel.Situations.FLYING:
                        return "Flying over " + vessel.mainBody.name;
                    case Vessel.Situations.ORBITING:
                    case Vessel.Situations.ESCAPING:
                    case Vessel.Situations.SUB_ORBITAL:
                        return "In space around " + vessel.mainBody.name;
                    default:
                        return "Unknown";
                }
            }
        }
    }
}