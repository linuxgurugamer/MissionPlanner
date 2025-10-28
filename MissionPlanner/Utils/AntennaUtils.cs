using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionPlanner.Utils
{
    public static class AntennaUtils
    {
        // ---------- Core filter ----------
        private static IEnumerable<ModuleDataTransmitter> RealTransmitters(Part p)
            => p.FindModulesImplementing<ModuleDataTransmitter>()
                 .Where(tx => tx != null && tx.antennaType != AntennaType.INTERNAL);

        private static IEnumerable<ModuleDataTransmitter> RealTransmitters(Vessel v)
            => v == null ? Enumerable.Empty<ModuleDataTransmitter>()
                         : v.Parts.SelectMany(RealTransmitters);

        private static IEnumerable<ModuleDataTransmitter> RealTransmitters(ShipConstruct ship)
            => ship?.parts == null ? Enumerable.Empty<ModuleDataTransmitter>()
                                   : ship.parts.SelectMany(RealTransmitters);

        // ---------- Presence ----------
        public static bool HasAntennaFlight(Vessel v)
            => RealTransmitters(v).Any();

        public static bool HasAntennaEditor(ShipConstruct ship)
            => RealTransmitters(ship).Any();

        // ---------- Lists of parts ----------
        public static List<Part> GetAntennaPartsFlight(Vessel v)
            => v == null ? new List<Part>()
                         : v.Parts.Where(p => RealTransmitters(p).Any()).ToList();

        public static List<Part> GetAntennaPartsEditor(ShipConstruct ship)
            => ship?.parts == null ? new List<Part>()
                                   : ship.parts.Where(p => RealTransmitters(p).Any()).ToList();

        // ---------- Strongest power ----------
        public static double GetStrongestAntennaPowerFlight(Vessel v)
            => RealTransmitters(v).Select(tx => tx.antennaPower).DefaultIfEmpty(0).Max();

        public static double GetStrongestAntennaPowerEditor(ShipConstruct ship)
            => RealTransmitters(ship).Select(tx => tx.antennaPower).DefaultIfEmpty(0).Max();

        // ---------- Total (naïve) power ----------
        // NOTE: Stock CommNet doesn’t simply sum powers for range math.
        // This is a simple aggregate useful for UI summaries/logging.
        public static double GetTotalAntennaPowerFlight(Vessel v)
            => RealTransmitters(v).Sum(tx => tx.antennaPower);

        public static double GetTotalAntennaPowerEditor(ShipConstruct ship)
            => RealTransmitters(ship).Sum(tx => tx.antennaPower);

        // ---------- Count by type ----------
        public struct AntennaTypeCounts
        {
            public int Direct;
            public int Relay;
            public int Total => Direct + Relay;
        }

        public static AntennaTypeCounts GetAntennaCountsFlight(Vessel v)
        {
            var counts = new AntennaTypeCounts();
            foreach (var tx in RealTransmitters(v))
            {
                if (tx.antennaType == AntennaType.RELAY) counts.Relay++;
                else if (tx.antennaType == AntennaType.DIRECT) counts.Direct++;
            }
            return counts;
        }

        public static AntennaTypeCounts GetAntennaCountsEditor(ShipConstruct ship)
        {
            var counts = new AntennaTypeCounts();
            foreach (var tx in RealTransmitters(ship))
            {
                if (tx.antennaType == AntennaType.RELAY) counts.Relay++;
                else if (tx.antennaType == AntennaType.DIRECT) counts.Direct++;
            }
            return counts;
        }

        // ---------- Optional: strongest antenna part ----------
        public static Part GetStrongestAntennaPartFlight(Vessel v)
            => v?.Parts
                .Select(p => new { p, max = RealTransmitters(p).Select(tx => tx.antennaPower).DefaultIfEmpty(0).Max() })
                .OrderByDescending(x => x.max)
                .FirstOrDefault()?.p;

        public static Part GetStrongestAntennaPartEditor(ShipConstruct ship)
            => ship?.parts?
                .Select(p => new { p, max = RealTransmitters(p).Select(tx => tx.antennaPower).DefaultIfEmpty(0).Max() })
                .OrderByDescending(x => x.max)
                .FirstOrDefault()?.p;


        /// <summary>
        /// Formats a numeric power value into a short, human-friendly string.
        /// Examples:
        ///  500      -> "500"
        ///  12500    -> "12.5k"
        ///  2000000  -> "2M"
        ///  3500000000 -> "3.5G"
        /// </summary>
        public static string FormatPower(double power)
        {
            if (power >= 1_000_000_000)
                return (power / 1_000_000_000d).ToString("0.##") + "G";
            if (power >= 1_000_000)
                return (power / 1_000_000d).ToString("0.##") + "M";
            if (power >= 1_000)
                return (power / 1_000d).ToString("0.##") + "k";

            return power.ToString("0.##");
        }
    }
}