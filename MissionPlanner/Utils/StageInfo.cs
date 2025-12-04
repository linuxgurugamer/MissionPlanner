using System.Collections.Generic;
using static MissionPlanner.RegisterToolbar;

namespace MissionPlanner.Utils
{
    public class StageInfo
    {
        static public Dictionary<int, StageInfo> stageInfo = new Dictionary<int, StageInfo>();

        static public int StageCount {  get {
                if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                    return stageInfo.Count;
                else return int.MaxValue;
            }}

        DeltaVStageInfo dvStageInfo;

        public static float TWRActual(int stage)
        {
            if (stageInfo.ContainsKey(stage))
                return stageInfo[stage].dvStageInfo.TWRActual;
            return 0;
        }
        public static float DeltaVActual(int stage)
        {
            if (stageInfo.ContainsKey(stage))
                return stageInfo[stage].dvStageInfo.deltaVActual;
            return 0;
        }
        public static float TWRVac(int stage)
        {
            if (stageInfo.ContainsKey(stage))
                return stageInfo[stage].dvStageInfo.TWRVac;
            return 0;
        }
        public static float TWRASL(int stage)
        {
            if (stageInfo.ContainsKey(stage))
                return stageInfo[stage].dvStageInfo.TWRASL;
            return 0;
        }
        public static float DeltaVinVac(int stage)
        {
            if (stageInfo.ContainsKey(stage))
                return stageInfo[stage].dvStageInfo.deltaVinVac;
            return 0;
        }
        public static float DeltaVatASL(int stage)
        {
            if (stageInfo.ContainsKey(stage))
                return stageInfo[stage].dvStageInfo.deltaVatASL;
            return 0;
        }

        StageInfo(DeltaVStageInfo dvsi)
        {
            dvStageInfo = dvsi;
        }

        public static void Init()
        {
            stageInfo.Clear();
            if ((HighLogic.LoadedSceneIsEditor && EditorLogic.fetch != null && EditorLogic.fetch.ship != null && (EditorLogic.fetch.ship.vesselDeltaV != null)) ||
                HighLogic.LoadedSceneIsFlight)
            {
                VesselDeltaV vdv = null;
                string vesselName = "";
                if (HighLogic.LoadedSceneIsEditor)
                {
                    vdv = EditorLogic.fetch.ship.vesselDeltaV;
                    vesselName = EditorLogic.fetch.shipNameField.ToString();
                }
                else
                {
                    vdv = FlightGlobals.ActiveVessel.VesselDeltaV;
                    vesselName = FlightGlobals.ActiveVessel.vesselName;
                }

                Log.Info("StageInfo.Init, vessel name: " + vesselName + ", stage cnt: " + vdv.OperatingStageInfo.Count);
                foreach (DeltaVStageInfo si in vdv.OperatingStageInfo)
                {
                    stageInfo[si.stage] = new StageInfo(si);

                    Log.Info("StageInfo, Stage: " + (si.stage) + ", Stage TWR sea level:" + si.TWRASL.ToString("F1") +
                        ", Stage TWR in vacuum:" + si.TWRVac.ToString("F2") +
                        ", Stage Dv sea level: " + si.deltaVatASL.ToString("F1") +
                        ", Stage Dv vacuum: " + si.deltaVinVac.ToString("F1"));
                }
            }
        }

        /// <summary>
        /// Returns the current stage, if in the editor, returns the first stage
        /// </summary>
        /// <returns></returns>
        public static int CurrentStage(int requestedStage = -1)
        {
            int stage = 0;
            if (HighLogic.LoadedSceneIsFlight)
            {
                Vessel v = FlightGlobals.ActiveVessel;
                if (v == null || v.parts == null) return -1;

                if (v.situation == Vessel.Situations.PRELAUNCH)
                {
                    // On the pad: use first stage to be activated (bottom stage)
                    stage = GetFirstStageIndex(v.parts);
                }
                else
                {
                    // Normal flight case: use the currentStage
                    stage = v.currentStage;
                }
            }
            else
            {
                EditorLogic ed = EditorLogic.fetch;
                ShipConstruct ship = ed != null ? ed.ship : null;
                if (ship == null || ship.parts == null) return -1;

                stage = GetFirstStageIndex(ship.parts);

            }
            return stage;

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
}
