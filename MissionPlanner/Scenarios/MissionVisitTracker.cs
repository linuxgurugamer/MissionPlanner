using MissionPlanner;
using MissionPlanner.MissionPlanner;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MissionPlanner.RegisterToolbar;
using static SpaceTuxUtility.ConfigNodeUtils;

// ============================================================
// Scenario module: records SOI entries per vessel (per mission)
// ============================================================
[KSPScenario(ScenarioCreationOptions.AddToAllGames, new[]
        {GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.EDITOR})]
public class MissionVisitTracker : ScenarioModule
{
    #region BodyNameLookup
    private static Dictionary<string, CelestialBody> _bodyLookup;
    private static bool _bodyLookupInitialized = false;

    /// <summary>
    /// Initialize the body dictionary (if not already).
    /// Called automatically before lookups.
    /// </summary>
    private static void EnsureBodyLookup()
    {
        if (_bodyLookupInitialized && _bodyLookup != null) return;

        _bodyLookup = new Dictionary<string, CelestialBody>(StringComparer.OrdinalIgnoreCase);

        if (FlightGlobals.Bodies != null)
        {
            foreach (var body in FlightGlobals.Bodies)
            {
                if (body == null) continue;
                if (!_bodyLookup.ContainsKey(body.bodyName))
                    _bodyLookup.Add(body.bodyName, body);
            }
        }

        _bodyLookupInitialized = true;
        Debug.Log($"[MissionVisitTracker] Body lookup dictionary initialized with {_bodyLookup.Count} entries.");
    }

    /// <summary>
    /// Returns the CelestialBody for the given name, or null if not found.
    /// Lookup is case-insensitive.
    /// </summary>
    public static CelestialBody FindBodyByName(string bodyName)
    {
        EnsureBodyLookup();
        if (string.IsNullOrEmpty(bodyName)) return null;

        return _bodyLookup.TryGetValue(bodyName, out var body) ? body : null;
    }

    /// <summary>
    /// Returns true if a body with the given name exists.
    /// </summary>
    public static bool BodyExists(string bodyName)
    {
        EnsureBodyLookup();
        return !string.IsNullOrEmpty(bodyName) && _bodyLookup.ContainsKey(bodyName);
    }

    /// <summary>
    /// Refresh the lookup table (e.g. after planet packs load).
    /// Can be called manually if needed.
    /// </summary>
    public static void RefreshBodyLookup()
    {
        _bodyLookupInitialized = false;
        EnsureBodyLookup();
    }
    #endregion

    #region BiomeVisits
    public class BiomeVisits
    {
        public string bodyName;
        HashSet<string> biomes = new HashSet<string>();

        public BiomeVisits(string bodyName)
        {
            this.bodyName = bodyName;
        }
        public void AddBiome(string biome) { biomes.Add(biome); }
        public bool BiomeVisited(string biome) { return biomes.Contains(biome); }
    }
    #endregion

    #region TrackedVessels

    public static void SaveTrackedVesselMission(Guid guid, string mission)
    {
        _trackedVessels[guid] = mission;
    }

    public static void DeleteTrackedVesselMission(Guid oldVesselGuid)
    {
        if (_trackedVessels.ContainsKey(oldVesselGuid))
            _trackedVessels.Remove(oldVesselGuid);
    }

    public static bool IsVesselTracked(Guid guid)
    {
        return _trackedVessels.ContainsKey(guid);
    }

    public static string GetTrackedMissionName(Guid guid)
    {
        if (_trackedVessels.ContainsKey(guid))
            return _trackedVessels[guid];
        return null;
    }


    #endregion

    #region Dictionaries

    // vesselID -> set of visited/landed body names
    private readonly Dictionary<Guid, HashSet<string>> _visited = new Dictionary<Guid, HashSet<string>>();
    private readonly Dictionary<Guid, HashSet<string>> _landed = new Dictionary<Guid, HashSet<string>>();

    // vessel, Dictionary<body, biome>
    private readonly Dictionary<Guid, Dictionary<string, BiomeVisits>> _landedBiomes = new Dictionary<Guid, Dictionary<string, BiomeVisits>>();


    private readonly Dictionary<Guid, HashSet<string>> _flags = new Dictionary<Guid, HashSet<string>>();
    // vesselID -> starting body name
    private readonly Dictionary<Guid, string> _startBody = new Dictionary<Guid, string>();
    // vesselID -> (bodyName -> count)
    private readonly Dictionary<Guid, Dictionary<string, int>> _visitCounts = new Dictionary<Guid, Dictionary<string, int>>();
    private readonly Dictionary<Guid, Dictionary<string, int>> _landedCounts = new Dictionary<Guid, Dictionary<string, int>>();
    private readonly Dictionary<Guid, Dictionary<string, int>> _flagCounts = new Dictionary<Guid, Dictionary<string, int>>();

    private readonly Dictionary<Guid, HashSet<Guid>> _dockings = new Dictionary<Guid, HashSet<Guid>>();

    private static readonly Dictionary<Guid, string> _trackedVessels = new Dictionary<Guid, string>();

    #endregion

    // ------------------- Public API -------------------
    public static bool HasVisitedBody(Vessel v, string bodyname, bool countStartBody = true)
    {
        return (HasVisitedBody(v, FindBodyByName(bodyname), countStartBody));
    }

    public static bool HasVisitedBody(Vessel v, CelestialBody body, bool countStartBody = true)
    {
        if (v == null || body == null) return false;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return false;

        // Optional: treat current SOI as "visited" even without prior log
        if (v.mainBody == body) return true;

        if (!inst._visited.TryGetValue(v.id, out var set)) set = null;

        if (countStartBody && inst._startBody.TryGetValue(v.id, out var start) && start == body.bodyName)
            return true;

        return set != null && set.Contains(body.bodyName);
    }


    public static bool HasVisitedVessel(Vessel v, string vesselName, bool countStartBody = true)
    {
        if (v == null || vesselName == null) return false;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return false;

        if (!inst._visited.TryGetValue(v.id, out var set)) set = null;

        if (countStartBody && inst._startBody.TryGetValue(v.id, out var start) && start == vesselName)
            return true;

        return set != null && set.Contains(vesselName);
    }

    // ---------------

    public static int VisitCount(Vessel v, CelestialBody body)
    {
        if (v == null || body == null) return 0;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return 0;
        if (!inst._visitCounts.TryGetValue(v.id, out var dict)) return 0;
        return dict.TryGetValue(body.bodyName, out var n) ? n : 0;
    }

    public static bool HasLandedOnBody(Vessel v, string bodyName, bool countStartBody = true)
    {
        return HasLandedOnBody(v, FindBodyByName(bodyName), countStartBody);
    }
    public static bool HasLandedOnBody(Vessel v, CelestialBody body, bool countStartBody = true)
    {
        if (v == null || body == null) return false;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return false;


        if (!inst._landed.TryGetValue(v.id, out var set)) set = null;

        if (countStartBody && inst._startBody.TryGetValue(v.id, out var start) && start == body.bodyName)
            return true;

        return set != null && set.Contains(body.bodyName);
    }

    public static bool HasLandedOnBodyAtBiome(Vessel v, string bodyName, string biome, bool countStartBody = true)
    {
        return HasLandedOnBodyAtBiome(v, FindBodyByName(bodyName), biome, countStartBody);
    }

    public static bool HasLandedOnBodyAtBiome(Vessel v, CelestialBody body, string biome, bool countStartBody = true)
    {
        if (v == null || body == null) return false;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return false;


        if (!inst._landedBiomes.TryGetValue(v.id, out Dictionary<string, BiomeVisits> biomes)) biomes = null;
        if (biomes == null) return false;


        if (countStartBody && inst._startBody.TryGetValue(v.id, out var start) && start == body.bodyName && biomes[body.bodyName].BiomeVisited(biome))
            return true;
        return biomes[body.bodyName].BiomeVisited(biome);
    }

    public static bool HasPlantedFlagOnBody(Vessel v, string bodyName, bool countStartBody = true)
    {
        return HasPlantedFlagOnBody(v, FindBodyByName(bodyName), countStartBody);
    }
    public static bool HasPlantedFlagOnBody(Vessel v, CelestialBody body, bool countStartBody = true)
    {
        if (v == null || body == null) return false;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return false;


        if (!inst._flags.TryGetValue(v.id, out var set)) set = null;

        if (countStartBody && inst._startBody.TryGetValue(v.id, out var start) && start == body.bodyName)
            return true;

        return set != null && set.Contains(body.bodyName);
    }

    public static int LandedCount(Vessel v, string bodyName)
    {
        return LandedCount(v, FindBodyByName(bodyName));
    }
    public static int LandedCount(Vessel v, CelestialBody body)
    {
        if (v == null || body == null) return 0;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return 0;
        if (!inst._landedCounts.TryGetValue(v.id, out var dict)) return 0;
        return dict.TryGetValue(body.bodyName, out var n) ? n : 0;
    }

    public static int FlagCount(Vessel v, string bodyName)
    {
        return FlagCount(v, FindBodyByName(bodyName));
    }
    public static int FlagCount(Vessel v, CelestialBody body)
    {
        if (v == null || body == null) return 0;
        var inst = HighLogic.FindObjectOfType<MissionVisitTracker>();
        if (inst == null) return 0;
        if (!inst._flagCounts.TryGetValue(v.id, out var dict)) return 0;
        return dict.TryGetValue(body.bodyName, out var n) ? n : 0;
    }


    #region Events
    // ------------------- Event wiring -------------------
    private void Start()
    {
        if (HighLogic.LoadedSceneIsFlight)
            StartCoroutine(SlowUpdate());
    }

    private void OnEnable()
    {
        GameEvents.onVesselSOIChanged.Add(OnSOIChanged);
        GameEvents.onVesselCreate.Add(OnVesselCreate);
        GameEvents.onVesselTerminated.Add(onVesselTerminated);
        GameEvents.onVesselRecovered.Add(OnVesselRecovered);
        GameEvents.onVesselDestroy.Add(onVesselDestroyed);
        GameEvents.onFlagPlant.Add(onFlagPlant);
        GameEvents.onVesselDocking.Add(onVesselDocking);
    }

    private void OnDisable()
    {
        GameEvents.onVesselSOIChanged.Remove(OnSOIChanged);
        GameEvents.onVesselCreate.Remove(OnVesselCreate);
        GameEvents.onVesselTerminated.Remove(onVesselTerminated);
        GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
        GameEvents.onVesselDestroy.Remove(onVesselDestroyed);
        GameEvents.onFlagPlant.Remove(onFlagPlant);
        GameEvents.onVesselDocking.Remove(onVesselDocking);
    }

    IEnumerator SlowUpdate()
    {
        if (HighLogic.LoadedSceneIsFlight)
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                var landed = (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH) || (FlightGlobals.ActiveVessel.situation == Vessel.Situations.LANDED) ||
                    (FlightGlobals.ActiveVessel.situation == Vessel.Situations.SPLASHED);
                if (landed)
                    OnLanded();

                // add code to check for all vessels which are loaded, add them to the visited list
                foreach (var v in FlightGlobals.VesselsLoaded)
                {
                    Ensure(v.id);
                    _visited[v.id].Add(v.vesselName);
                }
            }
        }
    }

    // ------------------- Event handlers -------------------
    private void Ensure(Guid id)
    {
        if (!_visited.ContainsKey(id)) _visited[id] = new HashSet<string>();
        if (!_visitCounts.ContainsKey(id)) _visitCounts[id] = new Dictionary<string, int>();
    }

    private void EnsureLanded(Vessel v, Guid id)
    {
        if (!_landed.ContainsKey(id)) _landed[id] = new HashSet<string>();
        if (!_landedCounts.ContainsKey(id)) _landedCounts[id] = new Dictionary<string, int>();

    }
    private void EnsureLandedBiome(Vessel v, Guid id, string bodyName)
    {
        if (!_landedBiomes.ContainsKey(id))
            _landedBiomes[id] = new Dictionary<string, BiomeVisits>();
        if (!_landedBiomes[id].ContainsKey(bodyName))
            _landedBiomes[id][bodyName] = new BiomeVisits(bodyName);
    }

    private void EnsureFlags(Guid id)
    {
        if (!_flags.ContainsKey(id)) _flags[id] = new HashSet<string>();
        if (!_flagCounts.ContainsKey(id)) _flagCounts[id] = new Dictionary<string, int>();
    }

    private void OnVesselCreate(Vessel v)
    {
        if (v == null) return;
        Ensure(v.id);
        if (!_startBody.ContainsKey(v.id))
            _startBody[v.id] = v.mainBody?.bodyName ?? "";
    }

    private void onFlagPlant(Vessel v)
    {
        if (v == null) return;
        EnsureFlags(v.id);
        _flags[v.id].Add(v.mainBody.bodyName);

        var dict = _flagCounts[v.id];
        dict[v.mainBody.bodyName] = dict.TryGetValue(v.mainBody.bodyName, out var n) ? n + 1 : 1;
    }

    void onVesselDocking(uint i1, uint i2)
    {
        {
            if (i1 != FlightGlobals.ActiveVessel.persistentId && i2 != FlightGlobals.ActiveVessel.persistentId)
                return;

            var firstGuid = VesselLookup.GetVesselGuidByPersistentId(i1);
            var secondGuid = VesselLookup.GetVesselGuidByPersistentId(i2);

            if (!_dockings.ContainsKey(firstGuid))
                _dockings[firstGuid] = new HashSet<Guid>();
            if (!_dockings.ContainsKey(secondGuid))
                _dockings[secondGuid] = new HashSet<Guid>();

            _dockings[firstGuid].Add(secondGuid);
            _dockings[secondGuid].Add(firstGuid);

            for (int i = 0; i < HierarchicalStepsWindow._roots.Count; i++)
            {
                var r = HierarchicalStepsWindow._roots[i];
                CheckDocking(r, firstGuid, secondGuid);
            }
        }
    }

    private void CheckDocking(StepNode r, Guid guid1, Guid guid2)
    {
        if (r.data.stepType == CriterionType.Destination && r.data.destType == DestinationType.Vessel)
        {
            if (r.data.requiresDocking && r.data.vesselGuid == guid1 ||
                r.data.requiresDocking && r.data.vesselGuid == guid2)
                r.data.hasDocked = true;
        }
        foreach (var c in r.Children)
            CheckDocking(c, guid1, guid2);
    }

    public bool HasVesselDocked(Guid guid)
    {
        return _dockings.ContainsKey(guid);
    }

    private void OnSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> data)
    {
        var v = data.host;
        var toBody = data.to;
        if (v == null || toBody == null) return;

        Ensure(v.id);
        _visited[v.id].Add(toBody.bodyName);

        var dict = _visitCounts[v.id];
        dict[toBody.bodyName] = dict.TryGetValue(toBody.bodyName, out var n) ? n + 1 : 1;
    }

    double lastTimeLanded = 0;
    string lastBodyLanded = "";
    private void OnLanded()
    {
        Vessel v = FlightGlobals.ActiveVessel;
        EnsureLanded(v, v.id);
        EnsureLandedBiome(v, v.id, v.mainBody.bodyName);
        _landed[v.id].Add(v.mainBody.bodyName);
        _landedBiomes[v.id][v.mainBody.bodyName].AddBiome(BiomeUtils.GetCurrentBiome(v));

        if (lastBodyLanded != v.mainBody.bodyName || Planetarium.GetUniversalTime() - lastTimeLanded > HighLogic.CurrentGame.Parameters.CustomParams<MissionPlannerSettings>().minTimeForLanded)
        {
            var dict = _landedCounts[v.id];
            dict[v.mainBody.bodyName] = dict.TryGetValue(v.mainBody.bodyName, out var n) ? n + 1 : 1;
        }
        lastTimeLanded = Planetarium.GetUniversalTime();
        lastBodyLanded = v.mainBody.bodyName;
    }

    // You can prune or keep data on recovered/terminated/destroyed vessels.
    // Here we *keep* (so history remains visible in Tracking Station/UI).
    private void OnVesselRecovered(ProtoVessel pv, bool _) { /* keep data */ }
    private void onVesselTerminated(ProtoVessel pv) { /* keep data */ }
    private void onVesselDestroyed(Vessel v) { /* keep data */ }
    #endregion

    #region SaveLoad
    // ------------------- Save / Load -------------------
    const string START_BODIES = "START_BODIES";
    const string VISITED = "VISITED";
    const string VISIT_COUNTS = "VISIT_COUNTS";
    const string LANDED = "LANDED";
    const string LANDED_COUNTS = "LANDED_COUNTS";
    const string FLAGS = "FLAGS";
    const string FLAG_COUNTS = "FLAG_COUNTS";
    const string DOCKINGS = "DOCKINGS";
    const string TRACKED_VESSELS = "TRACKED_VESSELS";

    public override void OnSave(ConfigNode node)
    {
        var starts = node.AddNode(START_BODIES);
        foreach (var kv in _startBody)
        {
            var n = starts.AddNode("S");
            n.AddValue("id", kv.Key.ToString());
            n.AddValue("body", kv.Value ?? "");
        }

        var visits = node.AddNode(VISITED);
        foreach (var kv in _visited)
        {
            var n = visits.AddNode("V");
            n.AddValue("id", kv.Key.ToString());
            n.AddValue("list", string.Join(",", kv.Value));
        }

        var countsRoot = node.AddNode(VISIT_COUNTS);
        foreach (var kv in _visitCounts)
        {
            var n = countsRoot.AddNode("C");
            n.AddValue("id", kv.Key.ToString());
            foreach (var bc in kv.Value)
            {
                var bn = n.AddNode("B");
                bn.AddValue("body", bc.Key);
                bn.AddValue("count", bc.Value);
            }
        }

        var landed = node.AddNode(LANDED);
        foreach (var kv in _landed)
        {
            var n = landed.AddNode("V");
            n.AddValue("id", kv.Key.ToString());
            n.AddValue("list", string.Join(",", kv.Value));
        }

        countsRoot = node.AddNode(LANDED_COUNTS);
        foreach (var kv in _landedCounts)
        {
            var n = countsRoot.AddNode("C");
            n.AddValue("id", kv.Key.ToString());
            foreach (var bc in kv.Value)
            {
                var bn = n.AddNode("B");
                bn.AddValue("body", bc.Key);
                bn.AddValue("count", bc.Value);
            }
        }
        var flags = node.AddNode(FLAGS);
        foreach (var kv in _flags)
        {
            var n = flags.AddNode("V");
            n.AddValue("id", kv.Key.ToString());
            n.AddValue("list", string.Join(",", kv.Value));
        }

        countsRoot = node.AddNode(FLAG_COUNTS);
        foreach (var kv in _flagCounts)
        {
            var n = countsRoot.AddNode("C");
            n.AddValue("id", kv.Key.ToString());
            foreach (var bc in kv.Value)
            {
                var bn = n.AddNode("B");
                bn.AddValue("body", bc.Key);
                bn.AddValue("count", bc.Value);
            }
        }

        countsRoot = node.AddNode(DOCKINGS);
        foreach (var kv in _dockings)
        {
            var n = countsRoot.AddNode("D");
            n.AddValue("guid", kv.Key.ToString());
            foreach (var bc in kv.Value)
            {
                var bn = n.AddNode("G");
                bn.AddValue("guid", bc);
            }
        }

        //    private static readonly Dictionary<Guid, string> _trackedVessels = new Dictionary<Guid, string>();
        countsRoot = node.AddNode(TRACKED_VESSELS);
        foreach (var tv in _trackedVessels)
        {
            var n = countsRoot.AddNode("TV");
            n.AddValue("guid", tv.Key.ToString());
            n.AddValue("missionName", tv.Value.ToString());
        }
    }

    public override void OnLoad(ConfigNode node)
    {
        _visited.Clear(); _visitCounts.Clear(); _startBody.Clear();
        _landed.Clear(); _landedCounts.Clear();
        _flags.Clear(); _flagCounts.Clear();

        var starts = node.GetNode(START_BODIES);
        if (starts != null)
        {
            foreach (ConfigNode s in starts.GetNodes("S"))
            {
                Guid id = s.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                    _startBody[id] = s.SafeLoad("body", "");
                else
                    _startBody[id] = "";
            }
            //foreach (ConfigNode s in starts.GetNodes("S"))
            //    if (Guid.TryParse(s.GetValue("id") ?? "", out var id))
            //        _startBody[id] = s.GetValue("body") ?? "";
        }

        var visits = node.GetNode(VISITED);
        if (visits != null)
        {
            foreach (ConfigNode v in visits.GetNodes("V"))
            {
                Guid id = v.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                {
                    HashSet<string> set = new HashSet<string>();
                    string[] list = (v.GetValue("list") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var b in list)
                        set.Add(b);
                    _visited[id] = set;
                }

                //if (!Guid.TryParse(v.GetValue("id") ?? "", out var id))
                //    continue;
                //var set = new HashSet<string>();
                //var list = (v.GetValue("list") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                //foreach (var b in list)
                //    set.Add(b);
                //_visited[id] = set;

            }
        }

        var countsRoot = node.GetNode(VISIT_COUNTS);
        if (countsRoot != null)
        {
            foreach (ConfigNode c in countsRoot.GetNodes("C"))
            {
                Guid id = c.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                {
                    var dict = new Dictionary<string, int>();
                    foreach (ConfigNode bn in c.GetNodes("B"))
                    {
                        var body = bn.SafeLoad("body", "");
                        dict[body] = bn.SafeLoad("count", 0);
                    }
                    _visitCounts[id] = dict;
                }

                //if (!Guid.TryParse(c.GetValue("id") ?? "", out var id))
                //    continue;
                //var dict = new Dictionary<string, int>();
                //foreach (ConfigNode bn in c.GetNodes("B"))
                //{
                //    var body = bn.GetValue("body") ?? "";
                //    if (int.TryParse(bn.GetValue("count") ?? "0", out var n))
                //        dict[body] = n;
                //}
                //_visitCounts[id] = dict;

            }
        }

        var landed = node.GetNode(LANDED);
        if (landed != null)
        {
            foreach (ConfigNode v in visits.GetNodes("V"))
            {
                Guid id = v.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                {
                    var set = new HashSet<string>();
                    var list = (v.GetValue("list") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var b in list)
                        set.Add(b);
                    _landed[id] = set;
                }
                //if (!Guid.TryParse(v.GetValue("id") ?? "", out var id)) 
                //    continue;
                //var set = new HashSet<string>();
                //var list = (v.GetValue("list") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                //foreach (var b in list) set.Add(b);
                //_landed[id] = set;
            }
        }

        countsRoot = node.GetNode(LANDED_COUNTS);
        if (countsRoot != null)
        {
            foreach (ConfigNode c in countsRoot.GetNodes("C"))
            {
                Guid id = c.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                {
                    var dict = new Dictionary<string, int>();
                    foreach (ConfigNode bn in c.GetNodes("B"))
                    {
                        string body = bn.SafeLoad("body", "");
                        dict[body] = bn.SafeLoad("count", 0);
                    }
                    _landedCounts[id] = dict;
                }
                //if (!Guid.TryParse(c.GetValue("id") ?? "", out var id)) continue;
                //var dict = new Dictionary<string, int>();
                //foreach (ConfigNode bn in c.GetNodes("B"))
                //{
                //    var body = bn.GetValue("body") ?? "";
                //    if (int.TryParse(bn.GetValue("count") ?? "0", out var n))
                //        dict[body] = n;
                //}
                //_landedCounts[id] = dict;
            }
        }

        var flags = node.GetNode(FLAGS);
        if (flags != null)
        {
            foreach (ConfigNode v in visits.GetNodes("V"))
            {
                Guid id = v.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                {
                    var set = new HashSet<string>();
                    var list = (v.GetValue("list") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var b in list) set.Add(b);
                    _flags[id] = set;
                }

                //if (!Guid.TryParse(v.GetValue("id") ?? "", out var id)) continue;
                //var set = new HashSet<string>();
                //var list = (v.GetValue("list") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                //foreach (var b in list) set.Add(b);
                //_flags[id] = set;
            }
        }

        countsRoot = node.GetNode(FLAG_COUNTS);
        if (countsRoot != null)
        {
            foreach (ConfigNode c in countsRoot.GetNodes("C"))
            {
                Guid id = c.SafeLoad("id", Guid.Empty);
                if (id != Guid.Empty)
                {
                    var dict = new Dictionary<string, int>();
                    foreach (ConfigNode bn in c.GetNodes("B"))
                    {
                        var body = bn.SafeLoad("body", "");
                        dict[body] = bn.SafeLoad("count", 0);
                    }
                    _flagCounts[id] = dict;
                }

                //if (!Guid.TryParse(c.GetValue("id") ?? "", out var id)) continue;
                //var dict = new Dictionary<string, int>();
                //foreach (ConfigNode bn in c.GetNodes("B"))
                //{
                //    var body = bn.GetValue("body") ?? "";
                //    if (int.TryParse(bn.GetValue("count") ?? "0", out var n))
                //        dict[body] = n;
                //}
                //_flagCounts[id] = dict;
            }
        }


        var countDockings = node.GetNode(DOCKINGS);
        if (countsRoot != null)
        {
            foreach (ConfigNode c in countDockings.GetNodes("D"))
            {
                Guid id = c.SafeLoad("guid", Guid.Empty);
                if (id != Guid.Empty)
                {
                    var dict = new HashSet<Guid>();
                    foreach (ConfigNode bn in c.GetNodes("G"))
                    {
                        Guid id2 = bn.SafeLoad("guid", Guid.Empty);
                        _dockings[id].Add(id2);
                    }
                    _dockings[id] = dict;
                }

                //if (!Guid.TryParse(c.GetValue("guid") ?? "", out var id)) continue;
                //var dict = new HashSet<Guid>();
                //foreach (ConfigNode bn in c.GetNodes("G"))
                //{
                //    if (Guid.TryParse(bn.GetValue("guid") ?? "", out var id2))
                //        _dockings[id].Add(id2);
                //}
                //_dockings[id] = dict;
            }
        }

        var countTrackedVessels = node.GetNode(TRACKED_VESSELS);
        foreach (ConfigNode tv in countTrackedVessels.GetNodes("TV"))
        {
            Guid id = tv.SafeLoad("guid", Guid.Empty);
            if (id != Guid.Empty)
            {
                string m = tv.SafeLoad("missionName", "");
                if (!string.IsNullOrEmpty(m))
                    _trackedVessels[id] = m;
            }


            //if (!Guid.TryParse(tv.GetValue("guid") ?? "", out var id))
            //    continue;
            //string m = tv.GetValue("missionName");
            //if (!string.IsNullOrEmpty(m))
            //{
            //    _trackedVessels[id] = m;
            //}


        }
    }
    #endregion
}