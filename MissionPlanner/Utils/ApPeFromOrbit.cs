public static class ApPeFromOrbit
{
    public struct ApPe
    {
        public double ApAltitude;   // Apoapsis altitude above sea level
        public double PeAltitude;   // Periapsis altitude above sea level

        public ApPe(double apAlt, double peAlt)
        {
            ApAltitude = apAlt;
            PeAltitude = peAlt;
        }
    }

    /// <summary>
    /// Compute apoapsis and periapsis altitudes given semimajor axis (a)
    /// and eccentricity (e).
    /// </summary>
    public static ApPe ComputeApPe(Orbit orbit,   CelestialBody body)
    {
        // Using:
        //   PeR = a (1 - e)
        //   ApR = a (1 + e)

        double semimajorAxis = orbit.semiMajorAxis;
        double eccentricity = orbit.eccentricity;

        double peRadius = semimajorAxis * (1.0 - eccentricity);
        double apRadius = semimajorAxis * (1.0 + eccentricity);

        // Convert to altitudes above body radius
        double peAlt = peRadius - body.Radius;
        double apAlt = apRadius - body.Radius;

        return new ApPe(apAlt, peAlt);
    }
}
