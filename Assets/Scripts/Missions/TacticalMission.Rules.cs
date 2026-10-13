using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadowLineProtocol
{
    public partial class TacticalMission
    {
        class ActionOption
        {
            public string Label;
            public string Tip;
            public bool Enabled;
            public Action Run;
        }

        // ---- Movement ----

        void DoMove(TacticalUnit u, Vector2Int target)
        {
            var cells = moveMap.PathTo(target);
            u.ActionPoints--;
            u.WalkPath(cells.Select(c => grid.CellCenter(c)).ToList());
            u.Cell = target;
            int wreckage = cells.Count(c => grid[c] == TerrainType.Debris);
            if (wreckage > 0) Damage(u, wreckage, "snagged on jagged wreckage");
            AfterAction(u);
        }

        void DoLeap(TacticalUnit u, Vector2Int target)
        {
            u.ActionPoints--;
            u.Oxygen--;
            u.Cell = target;
            u.Leap(grid.CellCenter(target));
            Log($"{u.Name} bounds across in the low gravity (-1 oxygen).");
            if (grid[target] == TerrainType.Debris) Damage(u, 1, "landed on wreckage");
            leapMode = false;
            AfterAction(u);
        }

        void AfterAction(TacticalUnit u)
        {
            SyncCarried(u);
            if (u.Standing && InStaging(u.Cell)) Deliver(u);
            RevealNearbyCrates();

            if (cargo != null && cargo.Delivered)
            {
                Finish(MissionOutcome.Success);
                return;
            }
            if (!units.Any(x => x.Standing))
            {
                Finish(MissionOutcome.TeamDown);
                return;
            }
            leapMode = false;
            confirmWithdraw = false;
            if (selected == null || !selected.Standing || selected.ActionPoints <= 0) SelectNextReady();
            else RefreshMoveMap();
        }

        void SyncCarried(TacticalUnit u)
        {
            if (u.CarriedItem != null) u.CarriedItem.Cell = u.Cell;
            if (u.CarriedUnit != null) u.CarriedUnit.Cell = u.Cell;
        }

        void Deliver(TacticalUnit u)
        {
            if (u.CarriedItem != null)
            {
                var item = u.CarriedItem;
                item.Delivered = true;
                item.Carrier = null;
                u.CarriedItem = null;
                u.SetCarryVisual(null);
                Log(item == cargo ? $"{u.Name} hauls the cargo pallet into the staging zone!" : $"{u.Name} brings the {item.Name} back to staging.");
            }
            if (u.CarriedUnit != null)
            {
                var other = u.CarriedUnit;
                u.CarriedUnit = null;
                other.CarriedBy = null;
                other.Status = UnitStatus.Stable;
                other.Cell = FreeCellNear(u.Cell, InStaging);
                other.Teleport(grid.CellCenter(other.Cell));
                Log($"{u.Name} gets {other.Name} into the staging shelter. {other.Name} is stable.");
            }
        }

        void RevealNearbyCrates()
        {
            foreach (var crate in crates)
            {
                if (crate.Revealed) continue;
                if (units.Any(u => u.Standing && TacticalGrid.Distance(u.Cell, crate.Cell) <= RevealRange)) Reveal(crate);
            }
        }

        void Reveal(TacticalItem crate)
        {
            crate.Revealed = true;
            PlaceItemVisual(crate);
            Log($"Found a {crate.Name}.");
        }

        // ---- Damage, downed and lost ----

        void Damage(TacticalUnit u, int amount, string cause)
        {
            if (u.Status == UnitStatus.Lost) return;
            u.Integrity = Mathf.Max(0, u.Integrity - amount);
            Log($"{u.Name}: -{amount} suit integrity ({cause}).");
            if (u.Standing && u.Integrity <= 0) Down(u, "suit breach");
        }

        void Down(TacticalUnit u, string cause)
        {
            u.Status = InStaging(u.Cell) ? UnitStatus.Stable : UnitStatus.Downed;
            u.BleedOut = TacticalUnit.BleedOutTurns;
            u.DownedOnTurn = turn;
            u.DownedCause = cause;
            u.WasDowned = true;
            u.ActionPoints = 0;
            DropAll(u);
            Log(u.Status == UnitStatus.Stable
                ? $"{u.Name} is down ({cause}), but safe in the staging shelter."
                : $"{u.Name} is DOWN ({cause})! {TacticalUnit.BleedOutTurns} turns to patch them or carry them to the staging zone.");
            if (selected == u) leapMode = false;
        }

        void Lose(TacticalUnit u)
        {
            DropAll(u);
            if (u.CarriedBy != null)
            {
                u.CarriedBy.CarriedUnit = null;
                u.CarriedBy = null;
            }
            u.Status = UnitStatus.Lost;
            Log($"{u.Name}'s suit has failed. {u.Name} is lost.");
        }

        void DropAll(TacticalUnit u)
        {
            if (u.CarriedItem != null)
            {
                var item = u.CarriedItem;
                item.Carrier = null;
                item.Cell = u.Cell;
                u.CarriedItem = null;
                u.SetCarryVisual(null);
                PlaceItemVisual(item);
            }
            if (u.CarriedUnit != null)
            {
                var other = u.CarriedUnit;
                other.CarriedBy = null;
                u.CarriedUnit = null;
                other.Cell = FreeCellNear(u.Cell);
                other.Teleport(grid.CellCenter(other.Cell));
                if (InStaging(other.Cell) && other.Status == UnitStatus.Downed) other.Status = UnitStatus.Stable;
            }
        }

        void TryRevive(TacticalUnit t)
        {
            if (t.Standing || t.Status == UnitStatus.Lost || t.Oxygen <= 0 || t.Integrity <= 0) return;
            if (t.CarriedBy != null)
            {
                var carrier = t.CarriedBy;
                carrier.CarriedUnit = null;
                t.CarriedBy = null;
                t.Cell = FreeCellNear(carrier.Cell);
                t.Teleport(grid.CellCenter(t.Cell));
            }
            t.Status = UnitStatus.Standing;
            t.BleedOut = 0;
            t.ActionPoints = 0;
            Log($"{t.Name} is back on their feet (can act next turn).");
        }

        // ---- Context actions ----

        List<ActionOption> BuildActions(TacticalUnit u)
        {
            var list = new List<ActionOption>();
            if (u == null || !u.Standing) return list;
            bool ap = u.ActionPoints > 0;
            bool engineer = u.Colonist.Role == CrewRole.Engineer;

            if (!hatchOpen && Adjacent(u.Cell, podCell))
            {
                bool solo = engineer || u.HasTool(GearId.PryBar);
                var helper = units.FirstOrDefault(o => o != u && o.Standing && o.ActionPoints > 0 && Adjacent(o.Cell, podCell));
                if (solo)
                    Add(list, "Pry the hatch open", u.HasTool(GearId.PryBar) ? "Uses the pry bar." : "Engineer: no tool needed.", ap, () => OpenHatch(u, null));
                else if (helper != null)
                    Add(list, $"Force the hatch with {helper.Name}", "Teamwork: costs 1 action from each of you.", ap, () => OpenHatch(u, helper));
                else
                    Add(list, "Pry the hatch open", "Needs a pry bar, an engineer, or a teammate with an action left next to the pod.", false, null);
            }

            if (!tankSealed && !tankRuptured && Adjacent(u.Cell, tankCell))
            {
                bool can = engineer || u.HasTool(GearId.RepairKit);
                Add(list, "Seal the venting tank", can ? "Stops the vents and the rupture." : "Needs a repair kit or an engineer.", can && ap, () => SealTank(u));
            }

            if (!u.Carrying)
            {
                foreach (var item in AllItems().Where(i => i.OnGround && Adjacent(u.Cell, i.Cell)).ToList())
                {
                    string tip = !item.Heavy ? "Light: no movement penalty." : u.StrongCarrier ? "Heavy, but you can haul it at full speed." : "Heavy: halves your movement.";
                    Add(list, $"Pick up the {item.Name}", tip, ap, () => PickUp(u, item));
                }
                foreach (var other in units.Where(o => o.Status == UnitStatus.Downed && o.CarriedBy == null && Adjacent(u.Cell, o.Cell)).ToList())
                {
                    string tip = u.StrongCarrier ? "Carry them at full speed." : "Halves your movement. Reach the staging zone to stabilize them.";
                    Add(list, $"Carry {other.Name}", tip, ap, () => CarryUnit(u, other));
                }
            }
            else
            {
                string what = u.CarriedItem != null ? "the " + u.CarriedItem.Name : u.CarriedUnit.Name;
                Add(list, $"Put down {what}", "Free action.", true, () =>
                {
                    DropAll(u);
                    Log($"{u.Name} puts down {what}.");
                    AfterAction(u);
                });
            }

            foreach (var t in Treatable(u))
            {
                string who = t == u ? "yourself" : t.Name;
                if (u.HasConsumable(GearId.OxygenCanister) && t.Oxygen < t.MaxOxygen)
                    Add(list, $"Oxygen canister: {who}", t.Standing ? "+4 oxygen." : "+4 oxygen. Revives them if their suit is intact.", ap, () => UseOxygen(u, t));
                if (u.HasConsumable(GearId.SuitPatch) && t.Integrity < t.MaxIntegrity)
                    Add(list, $"Suit patch: {who}", t.Standing ? "+2 suit integrity." : "+2 suit integrity. Revives them if they have air.", ap, () => UsePatch(u, t));
            }

            if (u.Colonist.Role == CrewRole.Prospector)
                Add(list, "Scan for supply crates", $"Reveals crates within {ScanRange} cells.", ap && crates.Any(c => !c.Revealed), () => Scan(u));

            return list;
        }

        static void Add(List<ActionOption> list, string label, string tip, bool enabled, Action run) =>
            list.Add(new ActionOption { Label = label, Tip = tip, Enabled = enabled, Run = run });

        IEnumerable<TacticalUnit> Treatable(TacticalUnit u) =>
            units.Where(o => o.Status != UnitStatus.Lost && (o == u || o.CarriedBy == u || (o.CarriedBy == null && Adjacent(u.Cell, o.Cell)))).ToList();

        void OpenHatch(TacticalUnit u, TacticalUnit helper)
        {
            u.ActionPoints--;
            if (helper != null) helper.ActionPoints--;
            hatchOpen = true;
            hatchDoor.transform.localPosition = new Vector3(0f, 0.06f, -1.3f);
            hatchDoor.transform.localRotation = Quaternion.Euler(90f, 12f, 0f);
            cargo = new TacticalItem { Name = "cargo pallet", Heavy = true, Cell = hatchCell };
            cargo.Visual = CreateItemVisual("Cargo pallet", hatchCell, new Vector3(1.2f, 0.9f, 1.2f), new Color(0.35f, 0.42f, 0.55f), new Color(1f, 0.85f, 0.2f));
            Log(helper != null ? $"{u.Name} and {helper.Name} force the hatch open together. The cargo pallet is out." : $"{u.Name} pries the hatch open. The cargo pallet is out.");
            AfterAction(u);
        }

        void SealTank(TacticalUnit u)
        {
            u.ActionPoints--;
            tankSealed = true;
            tankSeal.SetActive(true);
            Log($"{u.Name} seals the venting tank. No more vents, no rupture.");
            AfterAction(u);
        }

        void PickUp(TacticalUnit u, TacticalItem item)
        {
            u.ActionPoints--;
            item.Carrier = u;
            item.Cell = u.Cell;
            u.CarriedItem = item;
            item.Visual.SetActive(false);
            u.SetCarryVisual(item.Heavy ? new Color(0.35f, 0.42f, 0.55f) : new Color(0.85f, 0.85f, 0.8f));
            Log($"{u.Name} picks up the {item.Name}.");
            AfterAction(u);
        }

        void CarryUnit(TacticalUnit u, TacticalUnit other)
        {
            u.ActionPoints--;
            u.CarriedUnit = other;
            other.CarriedBy = u;
            other.Cell = u.Cell;
            Log($"{u.Name} hoists {other.Name} onto their shoulders.");
            AfterAction(u);
        }

        void UseOxygen(TacticalUnit u, TacticalUnit t)
        {
            u.ActionPoints--;
            u.ConsumableUsed = true;
            t.Oxygen = Mathf.Min(t.MaxOxygen, t.Oxygen + 4);
            Log($"{u.Name} swaps an oxygen canister into {(t == u ? "their own" : t.Name + "'s")} suit.");
            TryRevive(t);
            AfterAction(u);
        }

        void UsePatch(TacticalUnit u, TacticalUnit t)
        {
            u.ActionPoints--;
            u.ConsumableUsed = true;
            t.Integrity = Mathf.Min(t.MaxIntegrity, t.Integrity + 2);
            Log($"{u.Name} patches {(t == u ? "their own" : t.Name + "'s")} suit.");
            TryRevive(t);
            AfterAction(u);
        }

        void Scan(TacticalUnit u)
        {
            u.ActionPoints--;
            int found = 0;
            foreach (var crate in crates)
            {
                if (crate.Revealed || TacticalGrid.Distance(u.Cell, crate.Cell) > ScanRange) continue;
                Reveal(crate);
                found++;
            }
            Log(found > 0 ? $"{u.Name}'s scan picks up {found} crate(s)." : $"{u.Name}'s scan finds nothing within {ScanRange} cells.");
            AfterAction(u);
        }

        // ---- End of turn ----

        void EndTurn()
        {
            if (finished) return;
            confirmWithdraw = false;
            leapMode = false;
            Log($"-- End of turn {turn} --");

            if (!tankSealed && !tankRuptured)
            {
                if (turn == RuptureTurn)
                {
                    tankRuptured = true;
                    tankBody.GetComponent<Renderer>().material.color = new Color(0.15f, 0.13f, 0.12f);
                    tankBody.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
                    Log(hatchOpen ? "The tank RUPTURES!" : "The tank RUPTURES and guts the pod, cargo and all!");
                    if (!hatchOpen) cargoRuined = true;
                    foreach (var u in units.Where(x => x.Status != UnitStatus.Lost && TacticalGrid.Distance(x.Cell, tankCell) <= RuptureRadius).ToList())
                        Damage(u, RuptureDamage, "caught in the rupture");
                }
                else if (VentTurns.Contains(turn))
                {
                    Log("The tank vents a jet of propellant.");
                    foreach (var u in units.Where(x => x.Status != UnitStatus.Lost && TacticalGrid.Distance(x.Cell, tankCell) <= VentRadius).ToList())
                        Damage(u, VentDamage, "venting propellant");
                }
            }

            foreach (var u in units.Where(x => x.Standing).ToList())
            {
                if (InShadow(u.Cell)) Damage(u, 1, "cold soak in the shadow");
                else u.Dose += SunDosePerTurn;
            }

            foreach (var u in units.Where(x => x.Standing).ToList())
            {
                u.Oxygen = Mathf.Max(0, u.Oxygen - 1);
                if (u.Oxygen <= 0) Down(u, "out of oxygen");
            }

            foreach (var u in units.Where(x => x.Status == UnitStatus.Downed && x.DownedOnTurn < turn).ToList())
            {
                u.BleedOut--;
                if (u.BleedOut <= 0) Lose(u);
            }

            if (cargoRuined)
            {
                Finish(MissionOutcome.CargoLost);
                return;
            }
            if (!units.Any(x => x.Standing))
            {
                Finish(MissionOutcome.TeamDown);
                return;
            }

            turn++;
            if (turn > def.TurnLimit)
            {
                turn = def.TurnLimit;
                Finish(MissionOutcome.OutOfTime);
                return;
            }

            foreach (var u in units.Where(x => x.Standing)) u.ActionPoints = TacticalUnit.MaxActionPoints;
            Log($"Turn {turn}.");
            if (ShadowRowsAt(turn) > ShadowRowsAt(turn - 1)) Log("The shadow line creeps closer.");
            if (!tankSealed && !tankRuptured && (VentTurns.Contains(turn) || turn == RuptureTurn))
                Log(turn == RuptureTurn ? "WARNING: the tank will rupture at the end of this turn!" : "Warning: the tank will vent at the end of this turn.");

            if (selected == null || !selected.Standing) SelectNextReady();
            else RefreshMoveMap();
        }

        bool CanWithdraw => units.Any(u => u.Standing) && units.Where(u => u.Standing).All(u => InStaging(u.Cell));
        int Abandoned => units.Count(u => u.Status == UnitStatus.Downed && u.CarriedBy == null);

        void Withdraw()
        {
            if (Abandoned > 0 && !confirmWithdraw)
            {
                confirmWithdraw = true;
                return;
            }
            Finish(MissionOutcome.Withdrew);
        }

        void Finish(MissionOutcome outcome)
        {
            if (finished) return;
            finished = true;
            leapMode = false;
            if (selected != null) selected.SetSelected(false);

            bool success = outcome == MissionOutcome.Success;
            bool anyStanding = units.Any(u => u.Standing);
            // Unless the team chose to withdraw (with a confirmation) or nobody is left standing, teammates bring the downed home.
            bool carryDownedHome = anyStanding && outcome != MissionOutcome.Withdrew && outcome != MissionOutcome.TeamDown;
            var result = new MissionResult { Outcome = outcome, TurnsTaken = turn };

            foreach (var u in units)
            {
                var report = new CrewReport { Colonist = u.Colonist, Dose = u.Dose };
                bool rescued = u.Status == UnitStatus.Stable || u.CarriedBy != null || (u.Status == UnitStatus.Downed && carryDownedHome);
                if (u.Status == UnitStatus.Lost || (u.Status == UnitStatus.Downed && !rescued))
                {
                    report.Fate = CrewFate.Lost;
                    report.Note = u.Status == UnitStatus.Lost ? $"Suit failed after going down ({u.DownedCause})." : "Left behind at the site.";
                    foreach (var item in u.Loadout.Items()) MissionResult.Count(result.GearSpent, item);
                }
                else
                {
                    report.Fate = u.WasDowned ? CrewFate.Injured : CrewFate.Fine;
                    report.Note = u.WasDowned
                        ? $"Went down ({u.DownedCause}). Injured: half speed until the end of next sol."
                        : $"{u.Oxygen} oxygen and {u.Integrity}/{u.MaxIntegrity} suit integrity left.";
                    if (u.ConsumableUsed && u.Loadout.Consumable.HasValue) MissionResult.Count(result.GearSpent, u.Loadout.Consumable.Value);
                }
                result.Crew.Add(report);
            }

            foreach (var crate in crates)
            {
                if (!crate.Delivered && (crate.Carrier == null || !crate.Carrier.Standing)) continue;
                result.Regolith += crate.Regolith;
                result.Ice += crate.Ice;
                if (crate.Gear.HasValue) MissionResult.Count(result.GearFound, crate.Gear.Value);
            }

            if (success)
            {
                result.Regolith += def.RewardRegolith;
                result.Ice += def.RewardIce;
            }
            result.Pod = success ? PodState.Recovered : cargoRuined ? PodState.Destroyed : PodState.StillOutThere;

            switch (outcome)
            {
                case MissionOutcome.Success:
                    result.Title = "Cargo recovered";
                    result.Summary = $"The pallet made it to the staging zone on turn {turn}: {def.RewardRegolith} regolith and {def.RewardIce} ice for the colony.";
                    break;
                case MissionOutcome.CargoLost:
                    result.Title = "Cargo lost";
                    result.Summary = "The tank ruptured with the cargo still sealed inside. The pod is a write-off.";
                    break;
                case MissionOutcome.OutOfTime:
                    result.Title = "Out of time";
                    result.Summary = "The shadow swallowed the crash site and the team had to fall back. The heavy pallet stays behind; the pod is still out there.";
                    break;
                case MissionOutcome.TeamDown:
                    result.Title = "Team down";
                    result.Summary = "Nobody was left standing. Downed crew outside the staging shelter could not be brought home.";
                    break;
                default:
                    result.Title = "Withdrew";
                    result.Summary = "The team pulled back to base. The pod is still out there; you can try again.";
                    break;
            }

            Log($"Mission over: {result.Title}.");
            Finished?.Invoke(result);
        }
    }
}
