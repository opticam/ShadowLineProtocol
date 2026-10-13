using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    public enum GearSlot
    {
        Suit,
        Tool,
        Consumable,
    }

    public enum GearId
    {
        StandardSuit,
        HardenedSuit,
        PryBar,
        RepairKit,
        TetherWinch,
        OxygenCanister,
        SuitPatch,
    }

    public class GearDef
    {
        public GearId Id;
        public GearSlot Slot;
        public string Name;
        public string Description;
        public Color Color;
        // Suits only: turns of oxygen and hits the suit can take before it breaches.
        public int Oxygen;
        public int Integrity;

        public static readonly GearDef[] All =
        {
            new GearDef
            {
                Id = GearId.StandardSuit, Slot = GearSlot.Suit, Name = "Standard EVA suit", Oxygen = 10, Integrity = 4,
                Description = "10 turns of oxygen, 4 suit integrity.", Color = new Color(0.92f, 0.92f, 0.88f),
            },
            new GearDef
            {
                Id = GearId.HardenedSuit, Slot = GearSlot.Suit, Name = "Hardened suit", Oxygen = 8, Integrity = 6,
                Description = "Armored layers: 6 suit integrity, but the extra effort leaves 8 turns of oxygen.", Color = new Color(0.4f, 0.45f, 0.52f),
            },
            new GearDef
            {
                Id = GearId.PryBar, Slot = GearSlot.Tool, Name = "Pry bar",
                Description = "Force a jammed hatch open without help.", Color = new Color(0.85f, 0.25f, 0.2f),
            },
            new GearDef
            {
                Id = GearId.RepairKit, Slot = GearSlot.Tool, Name = "Repair kit",
                Description = "Seal leaks and venting tanks.", Color = new Color(0.95f, 0.8f, 0.2f),
            },
            new GearDef
            {
                Id = GearId.TetherWinch, Slot = GearSlot.Tool, Name = "Tether winch",
                Description = "Haul heavy cargo or a downed teammate at full speed.", Color = new Color(0.3f, 0.5f, 0.95f),
            },
            new GearDef
            {
                Id = GearId.OxygenCanister, Slot = GearSlot.Consumable, Name = "Oxygen canister",
                Description = "+4 turns of oxygen for you or an adjacent teammate. Revives a teammate who ran out of air.", Color = new Color(0.55f, 0.85f, 1f),
            },
            new GearDef
            {
                Id = GearId.SuitPatch, Slot = GearSlot.Consumable, Name = "Suit patch",
                Description = "+2 suit integrity for you or an adjacent teammate. Revives a teammate whose suit breached.", Color = new Color(0.95f, 0.5f, 0.75f),
            },
        };

        public static GearDef Get(GearId id)
        {
            foreach (var def in All) if (def.Id == id) return def;
            return null;
        }
    }

    // The colony's shared equipment. Missions draw from it and can lose what they take.
    public class GearLocker
    {
        readonly Dictionary<GearId, int> counts = new Dictionary<GearId, int>
        {
            { GearId.StandardSuit, 3 },
            { GearId.HardenedSuit, 1 },
            { GearId.PryBar, 1 },
            { GearId.RepairKit, 1 },
            { GearId.TetherWinch, 1 },
            { GearId.OxygenCanister, 2 },
            { GearId.SuitPatch, 2 },
        };

        public int Count(GearId id) => counts.TryGetValue(id, out int n) ? n : 0;

        public void Add(GearId id, int amount) => counts[id] = Mathf.Max(0, Count(id) + amount);
    }
}
