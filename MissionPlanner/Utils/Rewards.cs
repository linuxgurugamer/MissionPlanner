using System;
using System.Collections.Generic;
using System.Reflection;

public static class RewardUtils
{
    /// <summary>
    /// Adds stock-style XP by adding FlightLog entries to every Kerbal in the vessel,
    /// then adds reputation + funds (career-only) and science (career + science mode).
    /// </summary>
    
    public static void GrantVesselRewards_StockXP(
        Vessel vessel,
        List<FlightLogEntrySpec> xpEntries,
        float reputationDelta,
        double fundsDelta,
        float scienceDelta,
        TransactionReasons reason = TransactionReasons.Mission
    )
    {
        if (vessel == null) return;

        // --- Stock XP via flight log entries, Career only ---
        if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
        {
            var crew = vessel.GetVesselCrew(); // List<ProtoCrewMember>
            if (crew != null && crew.Count > 0 && xpEntries != null)
            {
                foreach (var pcm in crew)
                {
                    if (pcm == null) continue;

                    foreach (var e in xpEntries)
                    {
                        if (e == null || string.IsNullOrEmpty(e.BodyName)) continue;

                        // Stock XP source: flight log entry
                        //if (string.IsNullOrEmpty(e.Extra))
                        pcm.flightLog.AddEntry(e.EntryType, e.BodyName);
                        //else
                        //    pcm.flightLog.AddEntry(e.EntryType, e.BodyName, e.Extra);
                    }
                }

                // Recalculate levels/stars (method name varies by KSP version)
                var game = HighLogic.CurrentGame;
                var roster = game != null ? game.CrewRoster : null;
                if (roster != null)
                {
                    InvokeFirstMatchingInstanceMethod(
                        roster,
                        "CalculateExperienceLevels",
                        "CalculateExperienceLevel",
                        "UpdateExperienceTraits",
                        "UpdateExperience"
                    );
                }
            }
        }
        // --- Funds + Reputation: career-only ---
        if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
        {
            if (Reputation.Instance != null && Math.Abs(reputationDelta) > 0f)
                Reputation.Instance.AddReputation(reputationDelta, reason);

            if (Funding.Instance != null && Math.Abs(fundsDelta) > 0.0)
                Funding.Instance.AddFunds(fundsDelta, reason);
        }

        // --- Science: career + science mode (not sandbox) ---
        // Science is global and stored in ResearchAndDevelopment.Instance
        if (HighLogic.CurrentGame != null &&
            (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX))
        {
            if (ResearchAndDevelopment.Instance != null && Math.Abs(scienceDelta) > 0f)
            {
                // Modern KSP versions have this:
                ResearchAndDevelopment.Instance.AddScience(scienceDelta, reason);
            }
        }
    }

    /// <summary>
    /// Simple spec for an XP-granting flight log entry (EntryType + body).
    /// </summary>
    public sealed class FlightLogEntrySpec
    {
        public readonly FlightLog.EntryType EntryType;
        public readonly string BodyName;
        //public readonly string Extra; // optional

        public FlightLogEntrySpec(FlightLog.EntryType entryType, string bodyName) //, string extra = null)
        {
            EntryType = entryType;
            BodyName = bodyName;
            //Extra = extra;
        }
    }

    private static void InvokeFirstMatchingInstanceMethod(object instance, params string[] methodNames)
    {
        if (instance == null || methodNames == null || methodNames.Length == 0) return;

        var t = instance.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (int i = 0; i < methodNames.Length; i++)
        {
            var mi = t.GetMethod(methodNames[i], flags, null, Type.EmptyTypes, null);
            if (mi != null)
            {
                mi.Invoke(instance, null);
                return;
            }
        }
    }
}
