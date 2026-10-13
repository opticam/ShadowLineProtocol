using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    // Stockpiles, the power grid and life support for the player's colony.
    public class Colony : MonoBehaviour
    {
        public const float BatteryCapacity = 300f;
        public const float OxygenPerColonist = 0.08f;
        public const float BrownoutOxygenLeak = 0.5f;

        public static Colony Instance { get; private set; }

        public readonly List<Building> Buildings = new List<Building>();
        public readonly List<Colonist> Colonists = new List<Colonist>();

        public float Battery { get; private set; } = 150f;
        public float PowerGeneration { get; private set; }
        public float PowerDemand { get; private set; }
        public bool Brownout { get; private set; }
        public float OxygenProduced { get; private set; }
        public float OxygenUsePerSecond { get; private set; }

        readonly Dictionary<ResourceType, float> stock = new Dictionary<ResourceType, float>
        {
            { ResourceType.Regolith, 30f },
            { ResourceType.Ice, 0f },
            { ResourceType.Oxygen, 150f },
        };

        void Awake() => Instance = this;

        public float GetExact(ResourceType type) => stock[type];
        public int Get(ResourceType type) => Mathf.FloorToInt(stock[type]);
        public void Add(ResourceType type, float amount) => stock[type] = Mathf.Max(0f, stock[type] + amount);

        public bool TryConsume(ResourceType type, float amount)
        {
            if (stock[type] < amount) return false;
            stock[type] -= amount;
            return true;
        }

        public void AddProducedOxygen(float amount)
        {
            Add(ResourceType.Oxygen, amount);
            OxygenProduced += amount;
        }

        public bool CanAfford(BuildingDef def) =>
            stock[ResourceType.Regolith] >= def.RegolithCost && stock[ResourceType.Ice] >= def.IceCost;

        public void Spend(BuildingDef def)
        {
            stock[ResourceType.Regolith] -= def.RegolithCost;
            stock[ResourceType.Ice] -= def.IceCost;
        }

        void Update()
        {
            bool daylight = DayNightCycle.Instance.IsDay;
            float generation = 0f, demand = 0f;
            foreach (var b in Buildings)
            {
                if (!b.IsComplete) continue;
                if (!b.Def.NeedsSunlight || daylight) generation += b.Def.PowerOutput;
                demand += b.Def.PowerDemand;
            }
            PowerGeneration = generation;
            PowerDemand = demand;

            float dt = Time.deltaTime;
            Battery = Mathf.Clamp(Battery + (generation - demand) * dt, 0f, BatteryCapacity);
            Brownout = Battery <= 0f && generation < demand;

            OxygenUsePerSecond = Colonists.Count * OxygenPerColonist + (Brownout ? BrownoutOxygenLeak : 0f);
            Add(ResourceType.Oxygen, -OxygenUsePerSecond * dt);
        }
    }
}
