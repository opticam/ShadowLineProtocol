using System.Linq;
using UnityEngine;

namespace ShadowLineProtocol
{
    public partial class TacticalMission
    {
        const float TopBarHeight = 40f;
        const float CardWidth = 236f;
        const float CardHeight = 128f;
        const float ActionsWidth = 340f;

        Rect actionsRect;

        static Rect TopBarRect => new Rect(0f, 0f, Screen.width, TopBarHeight);
        static Rect ObjectivesRect => new Rect(10f, TopBarHeight + 8f, 370f, 300f);
        static Rect LogRect => new Rect(Screen.width - 390f, TopBarHeight + 8f, 380f, 200f);
        Rect CrewRect => new Rect(10f, Screen.height - CardHeight - 10f, units.Count * (CardWidth + 6f) + 4f, CardHeight);

        bool OverUI(Vector2 screenMouse)
        {
            var p = new Vector2(screenMouse.x, Screen.height - screenMouse.y);
            if (finished) return true;
            return TopBarRect.Contains(p) || ObjectivesRect.Contains(p) || LogRect.Contains(p) || CrewRect.Contains(p) || actionsRect.Contains(p);
        }

        void OnGUI()
        {
            UiKit.Ensure();
            DrawWorldLabels();
            if (finished) return;
            DrawTopBar();
            DrawObjectives();
            DrawLog();
            DrawCrew();
            DrawActions();
            DrawHoverTip();
        }

        void DrawWorldLabels()
        {
            foreach (var u in units)
            {
                if (u.Status == UnitStatus.Lost || u.CarriedBy != null) continue;
                string status = u.Status == UnitStatus.Downed ? $" <color=#ff6655>DOWN ({u.BleedOut})</color>"
                    : u.Status == UnitStatus.Stable ? " <color=#77dd77>stable</color>" : "";
                UiKit.WorldLabel(cam, u.transform.position + Vector3.up * 3f, u.Name + status, u == selected ? new Color(0.4f, 1f, 0.5f) : Color.white);
            }
            foreach (var item in AllItems())
            {
                if (!item.OnGround) continue;
                UiKit.WorldLabel(cam, grid.CellCenter(item.Cell) + Vector3.up * 1.6f, item == cargo ? "Cargo pallet (heavy)" : "Supply crate", new Color(1f, 0.85f, 0.5f));
            }

            string pod = hatchOpen ? "Cargo pod (open)" : cargoRuined ? "Cargo pod (destroyed)" : "Cargo pod: hatch jammed";
            UiKit.WorldLabel(cam, grid.CellCenter(podCell) + Vector3.up * 2.6f, pod, new Color(1f, 0.7f, 0.4f));
            UiKit.WorldLabel(cam, grid.CellCenter(tankCell) + Vector3.up * 2f, TankStatus(), tankSealed ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.35f));
            UiKit.WorldLabel(cam, grid.CellCenter(new Vector2Int(Width / 2, 0)) + new Vector3(0f, 0.3f, TacticalGrid.CellSize * 0.5f), "STAGING ZONE", new Color(0.6f, 1f, 0.7f));
        }

        string TankStatus()
        {
            if (tankSealed) return "Tank sealed";
            if (tankRuptured) return "Tank ruptured";
            int next = VentTurns.Where(t => t >= turn).DefaultIfEmpty(RuptureTurn).Min();
            return next >= RuptureTurn ? $"Tank: RUPTURES end of turn {RuptureTurn}" : $"Tank: vents end of turn {next}";
        }

        void DrawTopBar()
        {
            var bar = TopBarRect;
            UiKit.Panel(bar);
            int rows = ShadowRowsAt(turn);
            string shadow = rows == 0 ? $"Shadow line arrives turn {ShadowStartTurn}" : $"Shadow covers {rows} row{(rows == 1 ? "" : "s")}";
            int left = def.TurnLimit - turn + 1;
            string turnText = left <= 2 ? $"<color=#ff7766>Turn {turn}/{def.TurnLimit}</color>" : $"Turn {turn}/{def.TurnLimit}";
            GUI.Label(new Rect(12f, 10f, Screen.width - 460f, 24f), $"<b>MISSION</b>  {def.Site}      {turnText}      {shadow}", UiKit.Text);

            bool busy = units.Any(u => u.Animating);
            GUI.enabled = !busy;
            if (GUI.Button(new Rect(Screen.width - 170f, 5f, 160f, 30f), "<b>End turn</b> (Space)", UiKit.Button)) EndTurn();
            GUI.enabled = !busy && CanWithdraw;
            string withdraw = confirmWithdraw ? $"<color=#ff7766>Leave {Abandoned} behind?</color>" : "Withdraw to base";
            if (GUI.Button(new Rect(Screen.width - 340f, 5f, 160f, 30f), withdraw, UiKit.Button)) Withdraw();
            GUI.enabled = true;
        }

        void DrawObjectives()
        {
            var rect = ObjectivesRect;
            UiKit.Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            GUILayout.Label("OBJECTIVES", UiKit.Title);
            Check(hatchOpen, "Open the jammed pod hatch");
            Check(cargo != null && cargo.Delivered, "Haul the cargo pallet to the staging zone");
            int found = crates.Count(c => c.Revealed), recovered = crates.Count(c => c.Delivered || (c.Carrier != null && c.Carrier.Standing));
            GUILayout.Label($"<color=#bbbbbb>Optional: supply crates {recovered} carried or recovered, {found}/{crates.Count} found</color>", UiKit.Wrap);

            GUILayout.Space(6f);
            GUILayout.Label("HAZARDS", UiKit.Title);
            GUILayout.Label($"<color=#ffaa77>{TankStatus()}</color>" + (tankSealed || tankRuptured ? "" : $"  (vents: -{VentDamage} suit within {VentRadius}; rupture: -{RuptureDamage} within {RuptureRadius})"), UiKit.Wrap);
            GUILayout.Label("<color=#99aaff>Shadow line:</color> ending a turn in shadow costs 1 suit integrity. Sunlight adds dose.", UiKit.Wrap);
            GUILayout.Label("Wreckage costs 2 movement and 1 suit integrity to cross.", UiKit.Wrap);

            GUILayout.Space(6f);
            GUILayout.Label($"<color=#aaaaaa>Ends when the pallet is home, turn {def.TurnLimit} runs out, the cargo is destroyed, or nobody is standing. " +
                            "Downed crew have 3 turns: patch them, refill their air, or carry them to staging.</color>", UiKit.Wrap);
            GUILayout.EndArea();
        }

        static void Check(bool done, string text) =>
            GUILayout.Label(done ? $"<color=#77dd77>[x] {text}</color>" : $"<color=#ffd966>[  ] {text}</color>", UiKit.Wrap);

        void DrawLog()
        {
            var rect = LogRect;
            UiKit.Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, rect.height - 12f));
            GUILayout.Label("MISSION LOG", UiKit.Title);
            foreach (var line in log.Skip(Mathf.Max(0, log.Count - 9))) GUILayout.Label(line, UiKit.Wrap);
            GUILayout.EndArea();
        }

        void DrawCrew()
        {
            var area = CrewRect;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                var rect = new Rect(area.x + i * (CardWidth + 6f), area.y, CardWidth, CardHeight);
                UiKit.Panel(rect);
                if (u == selected) UiKit.Panel(rect);
                if (u.Standing && GUI.Button(rect, GUIContent.none, GUIStyle.none)) Select(u);

                float x = rect.x + 10f, y = rect.y + 6f, w = rect.width - 20f;
                string pips = new string('●', u.ActionPoints) + new string('○', TacticalUnit.MaxActionPoints - u.ActionPoints);
                GUI.Label(new Rect(x, y, w, 20f), $"<b><color={UiKit.Hex(u.Colonist.Accent)}>[{i + 1}] {u.Name}</color></b>  {CrewPerks.Name(u.Colonist.Role)}", UiKit.Text);
                GUI.Label(new Rect(x + w - 40f, y, 40f, 20f), u.Standing ? pips : "", UiKit.Text);
                y += 24f;

                GUI.Label(new Rect(x, y, 70f, 16f), $"O2 {u.Oxygen}/{u.MaxOxygen}", UiKit.Small);
                UiKit.Bar(new Rect(x + 72f, y + 4f, w - 72f, 8f), (float)u.Oxygen / u.MaxOxygen, u.Oxygen <= 2 ? new Color(1f, 0.4f, 0.3f) : new Color(0.5f, 0.8f, 1f));
                y += 18f;
                GUI.Label(new Rect(x, y, 70f, 16f), $"Suit {u.Integrity}/{u.MaxIntegrity}", UiKit.Small);
                UiKit.Bar(new Rect(x + 72f, y + 4f, w - 72f, 8f), (float)u.Integrity / u.MaxIntegrity, u.Integrity <= 1 ? new Color(1f, 0.4f, 0.3f) : new Color(0.9f, 0.9f, 0.6f));
                y += 18f;

                string gear = string.Join(", ", new[] { u.Loadout.Tool1, u.Loadout.Tool2 }.Where(t => t.HasValue).Select(t => GearDef.Get(t.Value).Name));
                if (u.Loadout.Consumable.HasValue && !u.ConsumableUsed) gear += (gear.Length > 0 ? ", " : "") + GearDef.Get(u.Loadout.Consumable.Value).Name;
                GUI.Label(new Rect(x, y, w, 16f), $"<color=#aaaaaa>{(gear.Length > 0 ? gear : "No tools")}  |  Dose {u.Dose:0} mSv</color>", UiKit.Small);
                y += 18f;
                GUI.Label(new Rect(x, y, w, 36f), UnitStatusText(u), UiKit.Wrap);
            }
        }

        string UnitStatusText(TacticalUnit u)
        {
            switch (u.Status)
            {
                case UnitStatus.Lost: return "<color=#ff5544>LOST</color>";
                case UnitStatus.Stable: return "<color=#77dd77>Down but stable in the shelter</color>";
                case UnitStatus.Downed:
                    return $"<color=#ff6655>DOWN: {u.DownedCause}. Lost in {u.BleedOut} turn(s)</color>" + (u.CarriedBy != null ? $" (carried by {u.CarriedBy.Name})" : "");
            }
            if (u.CarriedItem != null) return $"Carrying the {u.CarriedItem.Name}";
            if (u.CarriedUnit != null) return $"Carrying {u.CarriedUnit.Name}";
            if (InShadow(u.Cell)) return "<color=#99aaff>In shadow: cold</color>";
            return InStaging(u.Cell) ? "<color=#77dd77>In the staging zone</color>" : "In sunlight";
        }

        void DrawActions()
        {
            actionsRect = Rect.zero;
            if (selected == null || !selected.Standing) return;
            var actions = BuildActions(selected);
            const float pad = 10f, buttonHeight = 40f, spacing = 4f;
            float height = pad + 24f + (actions.Count + 1) * (buttonHeight + spacing) + pad;
            actionsRect = new Rect(Screen.width - ActionsWidth - 10f, Screen.height - height - 10f, ActionsWidth, height);
            UiKit.Panel(actionsRect);

            float x = actionsRect.x + pad, y = actionsRect.y + pad, w = ActionsWidth - pad * 2f;
            GUI.Label(new Rect(x, y, w, 22f), $"<b>{selected.Name}</b>: {selected.ActionPoints} action(s) left. Click a blue cell to move ({selected.MoveRange} cells).", UiKit.Small);
            y += 24f;

            bool busy = units.Any(u => u.Animating);
            GUI.enabled = !busy && selected.ActionPoints > 0 && !selected.CarryingHeavy && selected.Oxygen > 1;
            string leapLabel = leapMode ? "<b>Leap mode ON</b>: click a purple cell (L / Esc to cancel)" : "Leap (L)\n<size=11><color=#bbbbbb>Up to 3 cells over anything. 1 action + 1 oxygen.</color></size>";
            if (GUI.Button(new Rect(x, y, w, buttonHeight), leapLabel, UiKit.ButtonLeft)) ToggleLeap();
            y += buttonHeight + spacing;

            ActionOption chosen = null;
            foreach (var a in actions)
            {
                GUI.enabled = !busy && a.Enabled;
                if (GUI.Button(new Rect(x, y, w, buttonHeight), $"{a.Label}\n<size=11><color=#bbbbbb>{a.Tip}</color></size>", UiKit.ButtonLeft)) chosen = a;
                y += buttonHeight + spacing;
            }
            GUI.enabled = true;
            chosen?.Run?.Invoke();
        }

        void DrawHoverTip()
        {
            if (!hoverCell.HasValue) return;
            var c = hoverCell.Value;
            string tip = null;
            if (grid[c] == TerrainType.Debris) tip = "Wreckage: 2 movement, -1 suit integrity";
            else if (grid[c] == TerrainType.Boulder) tip = "Boulder: impassable (leap over it)";
            if (InShadow(c)) tip = (tip != null ? tip + "\n" : "") + "In shadow: -1 suit integrity at end of turn";
            else if (ShadowNext(c)) tip = (tip != null ? tip + "\n" : "") + "Shadow arrives here next turn";
            if (tip == null) return;
            Vector2 mouse = Event.current.mousePosition;
            Vector2 size = UiKit.Small.CalcSize(new GUIContent(tip));
            var rect = new Rect(mouse.x + 18f, mouse.y + 14f, size.x + 12f, size.y + 6f);
            UiKit.Panel(rect);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 3f, size.x, size.y), tip, UiKit.Small);
        }
    }
}
