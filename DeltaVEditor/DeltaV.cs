using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaVEditor
{
    public class DeltaV
    {
        public string Origin;
        public string Destination;
        public float dV_to_low_orbit;
        public float injection_dV;
        public float capture_dV;
        public float transfer_to_low_orbit_dV;
        public float total_capture_dV;
        public float dV_low_orbit_to_surface;
        public float ascent_dV;
        public float plane_change_dV;

        public bool isMoon;
        public string parent;

        public string sortOrder;
        public DeltaV() { }
    }

}
