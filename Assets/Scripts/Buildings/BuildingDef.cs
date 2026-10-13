using UnityEngine;

namespace ShadowLineProtocol
{
    public enum BuildingType
    {
        Lander,
        Habitat,
        SolarArray,
        CommsTower,
        OxygenPlant,
    }

    // Building stats live in code for the demo; move to ScriptableObjects once balancing starts.
    public class BuildingDef
    {
        public BuildingType Type;
        public string Name;
        public string Description;
        public int RegolithCost;
        public int IceCost;
        public float BuildTime;
        public float PowerOutput;
        public float PowerDemand;
        public bool NeedsSunlight;
        public bool IsDropOff;
        public float PowerRange;
        public Vector3 Size;
        public KeyCode Hotkey;

        public static readonly BuildingDef Lander = new BuildingDef
        {
            Type = BuildingType.Lander,
            Name = "Lander",
            Description = "Your landing craft. Stores mined material and runs a small fuel cell.",
            PowerOutput = 3f,
            IsDropOff = true,
            PowerRange = 18f,
            Size = new Vector3(4f, 4.5f, 4f),
        };

        public static readonly BuildingDef Habitat = new BuildingDef
        {
            Type = BuildingType.Habitat,
            Name = "Habitat",
            Description = "Pressurized living module. Colonists can also drop off material here.",
            RegolithCost = 40,
            BuildTime = 12f,
            PowerDemand = 2f,
            IsDropOff = true,
            Size = new Vector3(4.2f, 2.4f, 2.6f),
            Hotkey = KeyCode.Alpha1,
        };

        public static readonly BuildingDef SolarArray = new BuildingDef
        {
            Type = BuildingType.SolarArray,
            Name = "Solar Array",
            Description = "Near-vertical panels for the low polar sun. Dark at night.",
            RegolithCost = 25,
            BuildTime = 6f,
            PowerOutput = 6f,
            NeedsSunlight = true,
            PowerRange = 14f,
            Size = new Vector3(3.2f, 2.8f, 1.4f),
            Hotkey = KeyCode.Alpha2,
        };

        public static readonly BuildingDef CommsTower = new BuildingDef
        {
            Type = BuildingType.CommsTower,
            Name = "Comms Tower",
            Description = "Links the colony to Earth.",
            RegolithCost = 30,
            BuildTime = 8f,
            PowerDemand = 1f,
            Size = new Vector3(1.5f, 6f, 1.5f),
            Hotkey = KeyCode.Alpha3,
        };

        public static readonly BuildingDef OxygenPlant = new BuildingDef
        {
            Type = BuildingType.OxygenPlant,
            Name = "Oxygen Plant",
            Description = "Splits ice into oxygen: 1 ice makes 2 oxygen every 2 seconds.",
            RegolithCost = 50,
            IceCost = 10,
            BuildTime = 14f,
            PowerDemand = 4f,
            Size = new Vector3(3f, 2.2f, 3f),
            Hotkey = KeyCode.Alpha4,
        };

        public static readonly BuildingDef[] Buildable = { Habitat, SolarArray, CommsTower, OxygenPlant };
    }
}
