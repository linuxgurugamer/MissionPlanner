using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DeltaVEditor
{

    public static class DeltaVCsv
    {
        private const string Header =
            //"Origin,Destination,dV_to_low_orbit,ejection_dV,capture_dV,dV_low_orbit_to_surface,plane_change_dV,parent,isMoon";
            "Origin,Destination,dV_to_low_orbit,ejection_dV,capture_dV,transfer_to_low_orbit_dV,total_capture_dV,dV_low_orbit_to_surface,ascent_dV,plane_change_dV,parent,isMoon";
        public static List<DeltaV> Load(string path)
        {
            var list = new List<DeltaV>();

            if (!File.Exists(path))
            {
                UnityEngine.Debug.LogError("[DeltaVCsv] File not found: " + path);
                return list;
            }

            using (var reader = new StreamReader(path))
            {
                bool firstLine = true;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // skip header
                    if (firstLine)
                    {
                        firstLine = false;
                        // if it doesn't look like a header, fall through and try to parse
                        if (line.StartsWith("Origin", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    var cols = line.Split(',');
                    if (cols.Length < 5)
                    {
                        UnityEngine.Debug.LogWarning("[DeltaVCsv] Bad row (expected 6 columns): " + line);
                        continue;
                    }


                    DeltaV dv = null;
                    try
                    {
                        dv = new DeltaV
                        {
                            Origin = cols[0].Trim(),
                            Destination = cols[1].Trim(),
                            dV_to_low_orbit = Math.Max(0f, ParseFloat(cols[2])),
                            injection_dV = Math.Max(0f, ParseFloat(cols[3])),
                            capture_dV = Math.Max(0f, ParseFloat(cols[4])),
                            transfer_to_low_orbit_dV = Math.Max(0f, ParseFloat(cols[5])),
                            total_capture_dV = Math.Max(0f, ParseFloat(cols[6])),
                            dV_low_orbit_to_surface = Math.Max(0f, ParseFloat(cols[7])),
                            ascent_dV = Math.Max(0f, ParseFloat(cols[8])),
                            plane_change_dV = Math.Max(0f, ParseFloat(cols[9])),
                            parent = cols[10].Trim(),
                            isMoon = bool.Parse(cols[11].Trim())
                        };
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("[DeltaVCsv] Error parsing row: " + line + "\n" + ex);
                    }
                    //dv.isMoon = CelestialBodyUtils.IsMoon(dv.Destination, out dv.parent);
                    list.Add(dv);
                }

            }

            return list;
        }

        public static void Save(string path, List<DeltaV> rows)
        {
            using (var writer = new StreamWriter(path, false))
            {
                writer.WriteLine(Header);

                foreach (var dv in rows)
                {
                    string line = string.Join(",",
                        dv.Origin,
                        dv.Destination,
                        dv.dV_to_low_orbit.ToString(CultureInfo.InvariantCulture),
                        dv.injection_dV.ToString(CultureInfo.InvariantCulture),
                        dv.capture_dV.ToString(CultureInfo.InvariantCulture),
                        dv.transfer_to_low_orbit_dV.ToString(CultureInfo.InvariantCulture),
                        dv.total_capture_dV.ToString(CultureInfo.InvariantCulture),
                        dv.dV_low_orbit_to_surface.ToString(CultureInfo.InvariantCulture),
                        dv.ascent_dV.ToString(CultureInfo.InvariantCulture),
                        dv.plane_change_dV.ToString(CultureInfo.InvariantCulture),
                        dv.parent,
                        dv.isMoon
                    );
                    writer.WriteLine(line);
                }
            }

            UnityEngine.Debug.Log("[DeltaVCsv] Saved " + rows.Count + " rows to " + path);
        }

        private static float ParseFloat(string s)
        {
            return float.Parse(
                s.Trim(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture);
        }
    }
}