using System;
using UnityEngine;

namespace ShadowLineProtocol
{
    public class Building : MonoBehaviour
    {
        const float OxygenCycleSeconds = 2f;

        public static event Action<Building> Completed;

        public BuildingDef Def { get; private set; }
        public string Status { get; private set; } = "";
        public bool IsComplete => progress >= Def.BuildTime;
        public float Progress01 => Def.BuildTime <= 0f ? 1f : progress / Def.BuildTime;

        Transform visual;
        float progress;
        float cycleTimer;

        public void Init(BuildingDef def, Transform visual, bool complete)
        {
            Def = def;
            this.visual = visual;
            progress = complete ? def.BuildTime : 0f;
            RefreshVisual();
            Colony.Instance.Buildings.Add(this);
        }

        public void AddWork(float seconds)
        {
            if (IsComplete) return;
            progress = Mathf.Min(Def.BuildTime, progress + seconds);
            RefreshVisual();
            if (IsComplete) Completed?.Invoke(this);
        }

        // Foundations rise out of the ground as they're built.
        void RefreshVisual() => visual.localScale = new Vector3(1f, Mathf.Lerp(0.1f, 1f, Progress01), 1f);

        void Update()
        {
            if (!IsComplete)
            {
                Status = $"Under construction: {Mathf.RoundToInt(Progress01 * 100f)}%";
                return;
            }

            var colony = Colony.Instance;
            if (Def.PowerDemand > 0f && colony.Brownout) { Status = "No power"; return; }
            if (Def.NeedsSunlight && !DayNightCycle.Instance.IsDay) { Status = "Night: no output"; return; }
            if (Def.Type != BuildingType.OxygenPlant) { Status = "Online"; return; }

            cycleTimer += Time.deltaTime;
            if (cycleTimer < OxygenCycleSeconds) return;
            cycleTimer = 0f;
            if (colony.TryConsume(ResourceType.Ice, 1f))
            {
                colony.AddProducedOxygen(2f);
                Status = "Producing oxygen";
            }
            else
            {
                Status = "Waiting for ice";
            }
        }

        void OnDestroy()
        {
            if (Colony.Instance != null) Colony.Instance.Buildings.Remove(this);
        }
    }
}
