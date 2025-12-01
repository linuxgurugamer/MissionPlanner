using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MissionPlanner.Utils
{
    public static class CelestialBodyUtils
    {
        /// <summary>
        /// Returns true if the specified body is a moon (its parent is not the Sun).
        /// Outputs the name of the parent body (or null).
        /// </summary>
        public static bool IsMoon(string bodyName, out string parentName)
        {
            parentName = null;

            if (string.IsNullOrEmpty(bodyName))
                return false;

            var body = FlightGlobals.Bodies
                .FirstOrDefault(b => string.Equals(b.bodyName, bodyName, StringComparison.OrdinalIgnoreCase));

            if (body == null)
                return false;

            // Parent of this body
            CelestialBody parent = body.referenceBody;

            if (parent == null)
                return false;

            parentName = parent.bodyDisplayName.TrimAll();

            // In stock KSP1, the "sun" is "Sun" or "Kerbol".
            // A body is a moon if its parent is NOT the sun.
            return !string.Equals(parent.bodyName, "Sun", StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(parent.bodyName, "Kerbol", StringComparison.OrdinalIgnoreCase);
            // &&                     !parent.isHomeWorld                   ;
        }
    }
}
