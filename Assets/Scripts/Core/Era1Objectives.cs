using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    // A cut-down version of the Era 1 "Landfall" milestones from Docs/Story arc.md.
    public class Era1Objectives : MonoBehaviour
    {
        public const int SponsorReviewSol = 3;
        public const float OxygenTarget = 60f;

        public class Objective
        {
            public string Title;
            public string Detail;
            public Func<bool> IsMet;
            public Action OnMet;
            public bool Done;
        }

        public readonly List<Objective> Objectives = new List<Objective>();
        public Objective Current => Objectives.Find(o => !o.Done);

        GameObject cargoPod;
        int nightsSurvived;
        bool brownoutTonight;

        public void Init(GameObject cargoPod)
        {
            this.cargoPod = cargoPod;
            Building.Completed += OnBuildingCompleted;
            DayNightCycle.Instance.NightEnded += OnNightEnded;

            Objectives.Add(new Objective
            {
                Title = "Landfall: recover the stray cargo pod",
                Detail = "A cargo pod bounced off course on landing. Send a colonist to the orange crate.",
                IsMet = () => AnyColonistWithin(this.cargoPod.transform.position, 3f),
                OnMet = RecoverPod,
            });
            Objectives.Add(new Objective
            {
                Title = "Build a Habitat",
                Detail = "Mine regolith, then press 1 and place a Habitat near the lander.",
                IsMet = () => HasCompleted(BuildingType.Habitat),
            });
            Objectives.Add(new Objective
            {
                Title = "Build a Solar Array",
                Detail = "Solar arrays power the colony in daylight and extend your power range.",
                IsMet = () => HasCompleted(BuildingType.SolarArray),
            });
            Objectives.Add(new Objective
            {
                Title = "Build a Comms Tower",
                Detail = "Earth needs a link to the colony before the sponsor review.",
                IsMet = () => HasCompleted(BuildingType.CommsTower),
            });
            Objectives.Add(new Objective
            {
                Title = "First Night: survive on stored power",
                Detail = "Solar arrays go dark at night. Keep the battery charged so life support holds.",
                IsMet = () => nightsSurvived > 0,
            });
            Objectives.Add(new Objective
            {
                Title = "Build an Oxygen Plant",
                Detail = "The plant turns ice into oxygen. Ice is in the permanent shadow of Shackleton crater.",
                IsMet = () => HasCompleted(BuildingType.OxygenPlant),
            });
            Objectives.Add(new Objective
            {
                Title = $"Sponsor Review: produce {OxygenTarget:0} oxygen by the end of Sol {SponsorReviewSol}",
                Detail = "Earth wants proof the colony can make its own air. Keep the plant fed with ice.",
                IsMet = () => Colony.Instance.OxygenProduced >= OxygenTarget,
            });
        }

        void OnDestroy()
        {
            Building.Completed -= OnBuildingCompleted;
        }

        void Update()
        {
            var game = Era1Game.Instance;
            if (game.IsOver) return;

            foreach (var objective in Objectives)
            {
                if (objective.Done || !objective.IsMet()) continue;
                objective.Done = true;
                objective.OnMet?.Invoke();
                HUD.Instance.Toast("Objective complete: " + objective.Title);
            }

            var colony = Colony.Instance;
            if (!DayNightCycle.Instance.IsDay && colony.Brownout) brownoutTonight = true;

            if (Objectives.TrueForAll(o => o.Done))
            {
                game.End(true, "Sponsor Review passed",
                    "Oxygen production is online and Earth keeps your launch priority.\n\n" +
                    "Era 2 would open here: rival colonies, the water-rights dispute, and the first hints of sabotage.");
            }
            else if (colony.GetExact(ResourceType.Oxygen) <= 0f)
            {
                game.End(false, "Life support failed",
                    "The colony ran out of oxygen.\n\nGet the Oxygen Plant running sooner, keep it fed with ice, and keep the power on through the night.");
            }
            else if (DayNightCycle.Instance.Sol > SponsorReviewSol)
            {
                game.End(false, "Sponsor Review failed",
                    $"Sol {SponsorReviewSol} ended without {OxygenTarget:0} oxygen produced. Earth cuts your launch priority.\n\n" +
                    "In the full game this opens the independence storyline instead of ending the run.");
            }
        }

        void RecoverPod()
        {
            Colony.Instance.Add(ResourceType.Regolith, 60);
            Colony.Instance.Add(ResourceType.Ice, 20);
            Destroy(cargoPod);
        }

        void OnBuildingCompleted(Building building)
        {
            HUD.Instance.Toast(building.Def.Name + " complete");
        }

        void OnNightEnded()
        {
            nightsSurvived++;
            HUD.Instance.Toast(brownoutTonight
                ? "Dawn. You made it, but the power failed overnight. The crew is shaken."
                : "Dawn. The colony made it through the night on stored power.");
            brownoutTonight = false;
        }

        static bool HasCompleted(BuildingType type) =>
            Colony.Instance.Buildings.Exists(b => b.Def.Type == type && b.IsComplete);

        static bool AnyColonistWithin(Vector3 position, float distance)
        {
            foreach (var c in Colony.Instance.Colonists)
            {
                Vector3 delta = c.transform.position - position;
                delta.y = 0f;
                if (delta.magnitude <= distance) return true;
            }
            return false;
        }
    }
}
