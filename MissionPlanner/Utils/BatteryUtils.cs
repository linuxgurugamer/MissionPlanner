using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MissionPlanner.Utils
{
    public static class BatteryUtils
    {
        private const string ElectricChargeName = "ElectricCharge";

        // ──────────────────────────────
        // FLIGHT
        // ──────────────────────────────

        /// <summary>
        /// Returns true if the vessel has any ElectricCharge capacity in flight.
        /// </summary>
        public static bool HasBatteryFlight(Vessel v)
        {
            if (v == null) return false;

            return v.Parts.Any(p =>
            {
                var res = p.Resources.Get(ElectricChargeName);
                return res != null && res.maxAmount > 0;
            });
        }

        /// <summary>
        /// Returns true if the vessel currently has any stored ElectricCharge in flight.
        /// </summary>
        public static bool HasChargeFlight(Vessel v)
        {
            if (v == null) return false;

            return v.Parts.Any(p =>
            {
                var res = p.Resources.Get(ElectricChargeName);
                return res != null && res.amount > 0;
            });
        }

        /// <summary>
        /// Returns total ElectricCharge capacity in flight.
        /// </summary>
        public static double GetTotalBatteryCapacityFlight(Vessel v)
        {
            if (v == null) return 0;

            return v.Parts
                .Select(p => p.Resources.Get(ElectricChargeName))
                .Where(r => r != null)
                .Sum(r => r.maxAmount);
        }

        /// <summary>
        /// Returns total ElectricCharge currently stored in flight.
        /// </summary>
        public static double GetTotalBatteryChargeFlight(Vessel v)
        {
            if (v == null) return 0;

            return v.Parts
                .Select(p => p.Resources.Get(ElectricChargeName))
                .Where(r => r != null)
                .Sum(r => r.amount);
        }

        /// <summary>
        /// Returns a list of parts that provide ElectricCharge capacity in flight.
        /// </summary>
        public static List<Part> GetBatteryPartsFlight(Vessel v)
        {
            var list = new List<Part>();
            if (v == null) return list;

            foreach (var p in v.Parts)
            {
                var res = p.Resources.Get(ElectricChargeName);
                if (res != null && res.maxAmount > 0)
                {
                    list.Add(p);
                }
            }

            return list;
        }

        // ──────────────────────────────
        // EDITOR
        // ──────────────────────────────

        /// <summary>
        /// Returns true if the editor craft has any ElectricCharge capacity.
        /// </summary>
        public static bool HasBatteryEditor(ShipConstruct ship)
        {
            if (ship == null || ship.parts == null) return false;

            foreach (var p in ship.parts)
            {
                var res = p.Resources.Get(ElectricChargeName);
                if (res != null && res.maxAmount > 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns total ElectricCharge capacity in the editor.
        /// </summary>
        public static double GetTotalBatteryCapacityEditor(ShipConstruct ship)
        {
            double total = 0;
            if (ship == null || ship.parts == null) return total;

            foreach (var p in ship.parts)
            {
                var res = p.Resources.Get(ElectricChargeName);
                if (res != null && res.maxAmount > 0)
                    total += res.maxAmount;
            }

            return total;
        }

        /// <summary>
        /// Returns total ElectricCharge currently stored in the editor.
        /// (Normally equals capacity, unless the resource is set differently in the part's default.)
        /// </summary>
        public static double GetTotalBatteryChargeEditor(ShipConstruct ship)
        {
            double total = 0;
            if (ship == null || ship.parts == null) return total;

            foreach (var p in ship.parts)
            {
                var res = p.Resources.Get(ElectricChargeName);
                if (res != null && res.amount > 0)
                    total += res.amount;
            }

            return total;
        }

        /// <summary>
        /// Returns a list of parts that provide ElectricCharge capacity in the editor.
        /// </summary>
        public static List<Part> GetBatteryPartsEditor(ShipConstruct ship)
        {
            var list = new List<Part>();
            if (ship == null || ship.parts == null) return list;

            foreach (var p in ship.parts)
            {
                var res = p.Resources.Get(ElectricChargeName);
                if (res != null && res.maxAmount > 0)
                {
                    list.Add(p);
                }
            }

            return list;
        }
    }
}