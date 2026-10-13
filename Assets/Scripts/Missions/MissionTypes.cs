using System.Collections.Generic;

namespace ShadowLineProtocol
{
    public class MissionDef
    {
        public string Title;
        public string Site;
        public string Briefing;
        public string[] Tips;
        public int TurnLimit;
        public int MaxSquadOnFoot;
        // Colony seconds that pass on each leg of the trip. The colony clock is paused during the mission itself.
        public float TransitSeconds;
        public int TransitOxygen;
        public float TransitDose;
        public int RewardRegolith;
        public int RewardIce;

        public static readonly MissionDef CargoPodRecovery = new MissionDef
        {
            Title = "Landfall: recover the stray cargo pod",
            Site = "Cargo pod crash site",
            Briefing =
                "A cargo pod bounced off course on landing and came down hard. Its hatch is jammed, one of its propellant tanks is venting, " +
                "and the crater rim's shadow is about to sweep over the site. Get the cargo pallet back to the staging zone.",
            Tips = new[]
            {
                "The hatch is jammed: bring a pry bar or an engineer, or put two crew next to the pod to force it together.",
                "The tank vents at the end of turns 3 and 6 and ruptures at the end of turn 9, ruining any cargo still inside. A repair kit or an engineer can seal it.",
                "The cargo pallet is heavy: carrying it halves movement unless the carrier is a hauler or has a tether winch.",
                "From turn 5 the shadow line advances one row per turn. Ending a turn in shadow costs 1 suit integrity; ending it in sunlight adds radiation dose.",
                "Supply crates were thrown clear on impact. Walk near them or have a prospector scan to find them.",
            },
            TurnLimit = 12,
            MaxSquadOnFoot = 3,
            TransitSeconds = 20f,
            TransitOxygen = 2,
            TransitDose = 4f,
            RewardRegolith = 60,
            RewardIce = 20,
        };
    }

    public class CrewLoadout
    {
        public Colonist Colonist;
        public GearId Suit = GearId.StandardSuit;
        public GearId? Tool1;
        public GearId? Tool2;
        public GearId? Consumable;

        public GearDef SuitDef => GearDef.Get(Suit);

        public IEnumerable<GearId> Items()
        {
            yield return Suit;
            if (Tool1.HasValue) yield return Tool1.Value;
            if (Tool2.HasValue) yield return Tool2.Value;
            if (Consumable.HasValue) yield return Consumable.Value;
        }

        public int CountOf(GearId id)
        {
            int n = 0;
            foreach (var item in Items()) if (item == id) n++;
            return n;
        }
    }

    public static class CrewPerks
    {
        public static string Name(CrewRole role)
        {
            switch (role)
            {
                case CrewRole.Hauler: return "Hauler";
                case CrewRole.Engineer: return "Engineer";
                default: return "Prospector";
            }
        }

        public static string Describe(CrewRole role)
        {
            switch (role)
            {
                case CrewRole.Hauler: return "Carries heavy cargo or a downed teammate at full speed.";
                case CrewRole.Engineer: return "Pries hatches and seals tanks without tools.";
                default: return "Can scan for buried supply crates up to 6 cells away.";
            }
        }
    }

    public enum MissionOutcome
    {
        Success,
        CargoLost,
        OutOfTime,
        TeamDown,
        Withdrew,
    }

    public enum CrewFate
    {
        Fine,
        Injured,
        Lost,
    }

    public enum PodState
    {
        StillOutThere,
        Recovered,
        Destroyed,
    }

    public class CrewReport
    {
        public Colonist Colonist;
        public CrewFate Fate;
        public float Dose;
        public string Note;
    }

    public class MissionResult
    {
        public MissionOutcome Outcome;
        public string Title;
        public string Summary;
        public PodState Pod;
        public int TurnsTaken;
        public readonly List<CrewReport> Crew = new List<CrewReport>();
        public readonly Dictionary<GearId, int> GearSpent = new Dictionary<GearId, int>();
        public readonly Dictionary<GearId, int> GearFound = new Dictionary<GearId, int>();
        public int Regolith;
        public int Ice;

        public bool Succeeded => Outcome == MissionOutcome.Success;

        public static void Count(Dictionary<GearId, int> tally, GearId id, int amount = 1)
        {
            tally.TryGetValue(id, out int n);
            tally[id] = n + amount;
        }
    }
}
