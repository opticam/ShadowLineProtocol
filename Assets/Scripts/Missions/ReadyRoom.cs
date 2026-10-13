using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadowLineProtocol
{
    // The zoomed-in suit-up room: pick who goes and what each person carries.
    public class ReadyRoom : MonoBehaviour
    {
        const float CardWidth = 300f;
        const float CardHeight = 214f;
        const float StandeeSpacing = 2.8f;
        static readonly GearId[] SuitOptions = { GearId.StandardSuit, GearId.HardenedSuit };
        static readonly GearId?[] ToolOptions = { null, GearId.PryBar, GearId.RepairKit, GearId.TetherWinch };
        static readonly GearId?[] ConsumableOptions = { null, GearId.OxygenCanister, GearId.SuitPatch };

        public event Action<List<CrewLoadout>> Departed;
        public event Action Cancelled;

        MissionDef def;
        Camera cam;
        string roomName;
        string warning;
        readonly List<CrewLoadout> loadouts = new List<CrewLoadout>();
        readonly List<bool> assigned = new List<bool>();
        readonly List<Transform> standees = new List<Transform>();
        readonly List<Renderer> pads = new List<Renderer>();

        GearLocker Locker => Colony.Instance.Gear;
        int AssignedCount => assigned.Count(a => a);

        public void Init(MissionDef def, Camera cam, IEnumerable<Colonist> preselect)
        {
            this.def = def;
            this.cam = cam;
            bool hasHab = Colony.Instance.Buildings.Any(b => b.IsComplete && b.Def.Type == BuildingType.Habitat);
            roomName = hasHab ? "Habitat ready room" : "Lander airlock";

            foreach (var c in Colony.Instance.Colonists)
            {
                loadouts.Add(new CrewLoadout { Colonist = c });
                assigned.Add(false);
            }
            BuildRoom(hasHab);
            foreach (var c in preselect)
            {
                int i = loadouts.FindIndex(l => l.Colonist == c);
                if (i >= 0) TryAssign(i);
            }
            warning = null;
            PlaceCamera();
        }

        // ---- Rules ----

        static string Ineligible(Colonist c)
        {
            if (c.IsInjured) return $"Injured: resting until the end of Sol {c.InjuredUntilSol}";
            if (c.Dose >= Colonist.DoseLimit) return "At the career dose limit";
            return null;
        }

        // How many of an item are left in the locker after everyone else's picks.
        int Free(GearId id, CrewLoadout except)
        {
            int used = 0;
            for (int i = 0; i < loadouts.Count; i++)
                if (assigned[i] && loadouts[i] != except) used += loadouts[i].CountOf(id);
            return Locker.Count(id) - used;
        }

        bool TryAssign(int i)
        {
            var l = loadouts[i];
            string why = Ineligible(l.Colonist);
            if (why != null) { warning = $"{l.Colonist.ShortName} can't go: {why.ToLower()}."; return false; }
            if (AssignedCount >= def.MaxSquadOnFoot) { warning = $"On foot, at most {def.MaxSquadOnFoot} crew can go."; return false; }

            if (Free(l.Suit, l) <= 0)
            {
                var spare = SuitOptions.Where(s => Free(s, l) > 0).ToList();
                if (spare.Count == 0) { warning = "No suits left in the locker."; return false; }
                l.Suit = spare[0];
            }
            if (l.Tool1.HasValue && Free(l.Tool1.Value, l) <= 0) l.Tool1 = null;
            if (l.Tool2.HasValue && (Free(l.Tool2.Value, l) <= 0 || l.Tool2 == l.Tool1)) l.Tool2 = null;
            if (l.Consumable.HasValue && Free(l.Consumable.Value, l) <= 0) l.Consumable = null;

            assigned[i] = true;
            warning = null;
            RebuildStandee(i);
            return true;
        }

        void Unassign(int i)
        {
            assigned[i] = false;
            RebuildStandee(i);
        }

        static GearId? Next(GearId?[] options, GearId? current, Func<GearId?, bool> allowed)
        {
            int start = Array.IndexOf(options, current);
            for (int k = 1; k <= options.Length; k++)
            {
                var option = options[(start + k + options.Length) % options.Length];
                if (allowed(option)) return option;
            }
            return current;
        }

        void CycleSuit(int i)
        {
            var l = loadouts[i];
            var options = SuitOptions.Select(s => (GearId?)s).ToArray();
            l.Suit = Next(options, l.Suit, o => o.HasValue && (o == l.Suit || Free(o.Value, l) > 0)).Value;
            RebuildStandee(i);
        }

        void CycleTool(int i, bool first)
        {
            var l = loadouts[i];
            GearId? other = first ? l.Tool2 : l.Tool1;
            GearId? current = first ? l.Tool1 : l.Tool2;
            var next = Next(ToolOptions, current, o => !o.HasValue || (o != other && (o == current || Free(o.Value, l) > 0)));
            if (first) l.Tool1 = next; else l.Tool2 = next;
            RebuildStandee(i);
        }

        void CycleConsumable(int i)
        {
            var l = loadouts[i];
            l.Consumable = Next(ConsumableOptions, l.Consumable, o => !o.HasValue || o == l.Consumable || Free(o.Value, l) > 0);
            RebuildStandee(i);
        }

        void Depart()
        {
            var squad = new List<CrewLoadout>();
            for (int i = 0; i < loadouts.Count; i++) if (assigned[i]) squad.Add(loadouts[i]);
            if (squad.Count == 0) { warning = "Assign at least one crew member."; return; }
            Departed?.Invoke(squad);
        }

        // ---- Scene ----

        void BuildRoom(bool hasHab)
        {
            var floorColor = new Color(0.33f, 0.34f, 0.36f);
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(0f, -0.1f, 0.5f), new Vector3(15f, 0.2f, 10f), floorColor);
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(0f, 2f, 5.4f), new Vector3(15f, 4f, 0.2f), Cutouts.Cardboard);
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(-7.5f, 2f, 0.5f), new Vector3(0.2f, 4f, 10f), Cutouts.DarkCardboard);
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(7.5f, 2f, 0.5f), new Vector3(0.2f, 4f, 10f), Cutouts.DarkCardboard);
            var trim = hasHab ? new Color(0.3f, 0.75f, 0.4f) : new Color(0.95f, 0.5f, 0.1f);
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(0f, 3.4f, 5.28f), new Vector3(15f, 0.15f, 0.05f), trim);

            // Airlock door on the right wall.
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(7.35f, 1.35f, 2.6f), new Vector3(0.15f, 2.7f, 2f), new Color(0.25f, 0.27f, 0.3f));
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(7.28f, 2.2f, 2.6f), new Vector3(0.05f, 0.25f, 2f), new Color(1f, 0.75f, 0.1f));

            // Lockers: suits, tools, consumables.
            float[] lockerX = { -5f, -2.6f, 2.6f, 5f };
            Color[] lockerFront = { GearDef.Get(GearId.StandardSuit).Color, GearDef.Get(GearId.PryBar).Color, GearDef.Get(GearId.TetherWinch).Color, GearDef.Get(GearId.OxygenCanister).Color };
            for (int i = 0; i < lockerX.Length; i++)
            {
                Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(lockerX[i], 1.4f, 4.9f), new Vector3(2f, 2.8f, 0.8f), Cutouts.DarkCardboard);
                Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(lockerX[i], 1.4f, 4.48f), new Vector3(1.8f, 2.6f, 0.05f), lockerFront[i] * 0.85f);
            }
            Cutouts.Part(transform, PrimitiveType.Cube, new Vector3(0f, 0.25f, 3.9f), new Vector3(3.6f, 0.5f, 0.8f), Cutouts.Cardboard);

            var light = new GameObject("Ready room light").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.localPosition = new Vector3(0f, 3.8f, 0.5f);
            light.type = LightType.Point;
            light.range = 16f;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.95f, 0.85f);

            for (int i = 0; i < loadouts.Count; i++)
            {
                var pad = Cutouts.Part(transform, PrimitiveType.Cylinder, new Vector3(SlotX(i), 0.02f, -1.4f), new Vector3(1.8f, 0.02f, 1.8f), new Color(0.2f, 0.25f, 0.22f));
                pads.Add(pad.GetComponent<Renderer>());
                var standee = new GameObject(loadouts[i].Colonist.ShortName).transform;
                standee.SetParent(transform, false);
                standee.localPosition = HomeSpot(i);
                standees.Add(standee);
                RebuildStandee(i);
            }
        }

        float SlotX(int i) => (i - (loadouts.Count - 1) * 0.5f) * StandeeSpacing;
        Vector3 HomeSpot(int i) => new Vector3(SlotX(i), 0f, 2.4f);
        Vector3 PadSpot(int i) => new Vector3(SlotX(i), 0f, -1.4f);

        void RebuildStandee(int i)
        {
            var root = standees[i];
            foreach (Transform child in root) Destroy(child.gameObject);
            var l = loadouts[i];
            bool going = assigned[i];
            Color suit = going ? l.SuitDef.Color : new Color(0.55f, 0.5f, 0.45f);

            Cutouts.Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.8f, 0.03f, 0.8f), Cutouts.DarkCardboard);
            Cutouts.Part(root, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(0.85f, 1.6f, 0.05f), suit);
            Cutouts.Part(root, PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), new Vector3(0.62f, 0.55f, 0.05f), going ? suit : l.Colonist.Accent);
            Cutouts.Part(root, PrimitiveType.Cube, new Vector3(0f, 1.95f, -0.04f), new Vector3(0.42f, 0.25f, 0.02f), going ? new Color(0.85f, 0.6f, 0.15f) : new Color(0.1f, 0.1f, 0.15f));
            Cutouts.Part(root, PrimitiveType.Cube, new Vector3(0f, 1.15f, -0.04f), new Vector3(0.6f, 0.15f, 0.02f), l.Colonist.Accent);
            if (going)
            {
                var items = new[] { l.Tool1, l.Tool2, l.Consumable };
                for (int k = 0; k < items.Length; k++)
                {
                    if (!items[k].HasValue) continue;
                    Cutouts.Part(root, PrimitiveType.Cube, new Vector3(-0.3f + k * 0.3f, 0.5f, -0.05f), new Vector3(0.2f, 0.2f, 0.05f), GearDef.Get(items[k].Value).Color);
                }
            }
            pads[i].material.color = going ? new Color(0.3f, 0.9f, 0.45f) : new Color(0.2f, 0.25f, 0.22f);
        }

        void PlaceCamera()
        {
            cam.transform.position = transform.position + new Vector3(0f, 5.4f, -9.6f);
            cam.transform.LookAt(transform.position + new Vector3(0f, 1.2f, 1.2f));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < standees.Count; i++)
            {
                Vector3 target = assigned[i] ? PadSpot(i) : HomeSpot(i);
                standees[i].localPosition = Vector3.MoveTowards(standees[i].localPosition, target, 6f * dt);
            }
            if (Input.GetKeyDown(KeyCode.Escape)) Cancelled?.Invoke();
            else if (Input.GetKeyDown(KeyCode.Return)) Depart();
        }

        void LateUpdate() => PlaceCamera();

        // ---- UI ----

        void OnGUI()
        {
            UiKit.Ensure();
            for (int i = 0; i < standees.Count; i++)
                UiKit.WorldLabel(cam, standees[i].position + Vector3.up * 2.6f, loadouts[i].Colonist.ShortName, assigned[i] ? new Color(0.5f, 1f, 0.6f) : Color.white);

            DrawTopBar();
            DrawBriefing();
            DrawCards();
            DrawLocker();
        }

        void DrawTopBar()
        {
            var bar = new Rect(0f, 0f, Screen.width, 44f);
            UiKit.Panel(bar);
            GUI.Label(new Rect(12f, 11f, Screen.width - 420f, 24f), $"<b>{roomName.ToUpper()}</b>   Mission: {def.Title}", UiKit.Text);
            if (GUI.Button(new Rect(Screen.width - 400f, 6f, 170f, 32f), "Back to colony (Esc)", UiKit.Button)) Cancelled?.Invoke();
            GUI.enabled = AssignedCount > 0;
            if (GUI.Button(new Rect(Screen.width - 220f, 6f, 210f, 32f), $"<b>Depart on foot</b> ({AssignedCount}/{def.MaxSquadOnFoot})", UiKit.Button)) Depart();
            GUI.enabled = true;
        }

        void DrawBriefing()
        {
            var rect = new Rect(10f, 52f, 390f, Mathf.Max(200f, Screen.height - CardHeight - 72f));
            UiKit.Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            GUILayout.Label("BRIEFING", UiKit.Title);
            GUILayout.Label(def.Briefing, UiKit.Wrap);
            GUILayout.Space(6f);
            foreach (var tip in def.Tips) GUILayout.Label("• " + tip, UiKit.Wrap);
            GUILayout.Space(6f);
            GUILayout.Label("TRANSPORT", UiKit.Title);
            GUILayout.Label($"On foot (no vehicles unlocked yet). Up to {def.MaxSquadOnFoot} crew. Each leg takes {def.TransitSeconds:0} s of colony time " +
                            $"and costs each person {def.TransitOxygen} oxygen and {def.TransitDose:0} mSv. The colony pauses during the mission itself.", UiKit.Wrap);
            GUILayout.Label($"Up to {def.TurnLimit} turns on site.", UiKit.Wrap);
            if (warning != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"<color=#ff8866>{warning}</color>", UiKit.Wrap);
            }
            GUILayout.EndArea();
        }

        void DrawLocker()
        {
            var rect = new Rect(Screen.width - 300f, 52f, 290f, 250f);
            UiKit.Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            GUILayout.Label("GEAR LOCKER", UiKit.Title);
            foreach (var g in GearDef.All)
            {
                int total = Locker.Count(g.Id);
                int free = Free(g.Id, null);
                GUILayout.Label($"<color={UiKit.Hex(g.Color)}>■</color> {g.Name}: {free}/{total} free", UiKit.Small);
            }
            GUILayout.Space(4f);
            string tip = string.IsNullOrEmpty(GUI.tooltip) ? "Hover a slot for details. Click a slot to cycle what's free." : GUI.tooltip;
            GUILayout.Label($"<color=#bbbbbb>{tip}</color>", UiKit.Wrap);
            GUILayout.EndArea();
        }

        void DrawCards()
        {
            float total = loadouts.Count * (CardWidth + 8f) - 8f;
            float x0 = Mathf.Max(410f, (Screen.width - total) / 2f);
            for (int i = 0; i < loadouts.Count; i++)
            {
                var l = loadouts[i];
                var c = l.Colonist;
                var rect = new Rect(x0 + i * (CardWidth + 8f), Screen.height - CardHeight - 10f, CardWidth, CardHeight);
                UiKit.Panel(rect);
                if (assigned[i]) UiKit.Panel(rect);
                float x = rect.x + 10f, y = rect.y + 8f, w = rect.width - 20f;

                GUI.Label(new Rect(x, y, w, 20f), $"<b><color={UiKit.Hex(c.Accent)}>{c.DisplayName}</color></b>", UiKit.Text);
                y += 20f;
                GUI.Label(new Rect(x, y, w, 30f), $"<b>{CrewPerks.Name(c.Role)}:</b> {CrewPerks.Describe(c.Role)}", UiKit.Wrap);
                y += 32f;
                GUI.Label(new Rect(x, y, 120f, 16f), $"Dose {c.Dose:0}/{Colonist.DoseLimit:0} mSv", UiKit.Small);
                UiKit.Bar(new Rect(x + 122f, y + 4f, w - 122f, 8f), c.Dose / Colonist.DoseLimit, new Color(0.9f, 0.8f, 0.3f));
                y += 20f;

                string why = Ineligible(c);
                if (why != null)
                {
                    GUI.Label(new Rect(x, y, w, 40f), $"<color=#ff8866>{why}</color>", UiKit.Wrap);
                    continue;
                }

                if (GUI.Button(new Rect(x, y, w, 26f), assigned[i] ? "<b>Assigned</b>: click to stand down" : "Assign to mission", UiKit.Button))
                {
                    if (assigned[i]) Unassign(i); else TryAssign(i);
                }
                y += 30f;
                if (!assigned[i]) continue;

                if (SlotButton(new Rect(x, y, w, 22f), "Suit", l.Suit)) CycleSuit(i);
                y += 24f;
                if (SlotButton(new Rect(x, y, w / 2f - 2f, 22f), "Tool", l.Tool1)) CycleTool(i, true);
                if (SlotButton(new Rect(x + w / 2f + 2f, y, w / 2f - 2f, 22f), "Tool", l.Tool2)) CycleTool(i, false);
                y += 24f;
                if (SlotButton(new Rect(x, y, w, 22f), "Consumable", l.Consumable)) CycleConsumable(i);
            }
        }

        static bool SlotButton(Rect rect, string slot, GearId? item)
        {
            var g = item.HasValue ? GearDef.Get(item.Value) : null;
            string text = g != null ? $"{slot}: <color={UiKit.Hex(g.Color)}>{g.Name}</color>" : $"{slot}: <color=#888888>none</color>";
            string tip = g != null ? $"{g.Name}: {g.Description}" : $"Empty {slot.ToLower()} slot.";
            return GUI.Button(rect, new GUIContent(text, tip), UiKit.ButtonLeft);
        }
    }
}
