using UnityEngine;

namespace ShadowLineProtocol
{
    // Immediate-mode GUI so the demo needs no prefabs, canvases or font assets.
    public class HUD : MonoBehaviour
    {
        const float TopBarHeight = 32f;
        const float ButtonWidth = 160f;
        const float ButtonHeight = 54f;
        const float Gap = 6f;

        public static HUD Instance { get; private set; }

        Era1Objectives objectives;
        string toast;
        float toastUntil;

        GUIStyle text, small, wrap, title, bigTitle, worldLabel, button;

        void Awake() => Instance = this;

        void Start() => objectives = GetComponent<Era1Objectives>();

        public void Toast(string message)
        {
            toast = message;
            toastUntil = Time.unscaledTime + 3.5f;
        }

        static Rect TopBarRect => new Rect(0f, 0f, Screen.width, TopBarHeight);
        static Rect ObjectivesRect => new Rect(10f, TopBarHeight + 8f, 400f, 230f);
        static Rect SelectionRect => new Rect(10f, Screen.height - 160f, 340f, 150f);

        static Rect BuildBarRect
        {
            get
            {
                int count = BuildingDef.Buildable.Length;
                float width = count * ButtonWidth + (count + 1) * Gap;
                return new Rect((Screen.width - width) / 2f, Screen.height - ButtonHeight - 2f * Gap, width, ButtonHeight + 2f * Gap);
            }
        }

        // Takes Input.mousePosition (origin bottom-left); GUI rects use origin top-left.
        public bool IsOverHUD(Vector2 screenPosition)
        {
            var p = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            var selection = SelectionManager.Instance;
            return TopBarRect.Contains(p) || ObjectivesRect.Contains(p) || BuildBarRect.Contains(p)
                || selection.MenuRect.Contains(p)
                || (selection.HasSelection && SelectionRect.Contains(p));
        }

        void OnGUI()
        {
            InitStyles();
            DrawWorldLabels();
            DrawTopBar();
            DrawObjectives();
            DrawBuildBar();
            DrawSelection();
            DrawHints();
            DrawToast();
            DrawEndScreen();
        }

        void InitStyles()
        {
            if (text != null) return;
            text = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true, wordWrap = false };
            small = new GUIStyle(text) { fontSize = 12 };
            wrap = new GUIStyle(small) { wordWrap = true };
            title = new GUIStyle(text) { fontSize = 16, fontStyle = FontStyle.Bold };
            bigTitle = new GUIStyle(text) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            worldLabel = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 13, richText = true };
        }

        void DrawWorldLabels()
        {
            var cam = Camera.main;
            if (cam == null) return;

            foreach (var label in MapLabel.All)
                WorldLabel(cam, label.transform.position + Vector3.up * 2f, label.Text, new Color(1f, 0.85f, 0.5f));

            foreach (var node in ResourceNode.All)
                WorldLabel(cam, node.transform.position + Vector3.up * 1.8f, $"{node.Type} {node.Amount}", new Color(0.8f, 0.8f, 0.8f));

            foreach (var b in Colony.Instance.Buildings)
            {
                string label = b.IsComplete ? b.Def.Name : $"{b.Def.Name} {Mathf.RoundToInt(b.Progress01 * 100f)}%";
                WorldLabel(cam, b.transform.position + Vector3.up * (b.Def.Size.y + 0.8f), label, b.IsComplete ? Color.white : new Color(1f, 0.85f, 0.3f));
            }

            foreach (var c in Colony.Instance.Colonists)
                WorldLabel(cam, c.transform.position + Vector3.up * 2.5f, c.ShortName, c.Selected ? new Color(0.4f, 1f, 0.5f) : Color.white);
        }

        void WorldLabel(Camera cam, Vector3 worldPosition, string content, Color color)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f) return;
            var guiContent = new GUIContent(content);
            Vector2 size = worldLabel.CalcSize(guiContent);
            var rect = new Rect(screen.x - size.x / 2f, Screen.height - screen.y - size.y / 2f, size.x, size.y);

            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), guiContent, worldLabel);
            GUI.color = color;
            GUI.Label(rect, guiContent, worldLabel);
            GUI.color = Color.white;
        }

        void DrawTopBar()
        {
            var colony = Colony.Instance;
            var cycle = DayNightCycle.Instance;
            Panel(TopBarRect);

            string phase = cycle.IsDay ? "<color=#ffd966>Day</color>" : "<color=#88aaff>Night</color>";
            string oxygenColor = colony.GetExact(ResourceType.Oxygen) < 40f ? "#ff7777" : "#ffffff";
            string battery = $"Battery {colony.Battery:0}/{Colony.BatteryCapacity:0}";
            if (colony.Brownout) battery += "  <color=#ff5555><b>BROWNOUT</b></color>";

            string line =
                $"Sol {cycle.Sol}  {phase} {FormatTime(cycle.SecondsUntilChange)}      " +
                $"Regolith {colony.Get(ResourceType.Regolith)}      " +
                $"Ice {colony.Get(ResourceType.Ice)}      " +
                $"<color={oxygenColor}>Oxygen {colony.Get(ResourceType.Oxygen)}</color> <color=#aaaaaa>(-{colony.OxygenUsePerSecond:0.00}/s)</color>      " +
                $"Power {colony.PowerGeneration:0} made / {colony.PowerDemand:0} used      {battery}";
            GUI.Label(new Rect(12f, 6f, Screen.width - 24f, 24f), line, text);
        }

        void DrawObjectives()
        {
            if (objectives == null) return;
            var rect = ObjectivesRect;
            Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            GUILayout.Label("ERA 1 - LANDFALL", title);

            var current = objectives.Current;
            foreach (var o in objectives.Objectives)
            {
                if (o.Done) GUILayout.Label($"<color=#77dd77>[x] {o.Title}</color>", wrap);
                else if (o == current) GUILayout.Label($"<color=#ffd966>[  ] {o.Title}</color>", wrap);
                else GUILayout.Label($"<color=#bbbbbb>[  ] {o.Title}</color>", wrap);
            }
            if (current != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label($"<i>{current.Detail}</i>", wrap);
            }
            GUILayout.EndArea();
        }

        void DrawBuildBar()
        {
            var bar = BuildBarRect;
            Panel(bar);
            BuildingDef hovered = null;

            for (int i = 0; i < BuildingDef.Buildable.Length; i++)
            {
                var def = BuildingDef.Buildable[i];
                var rect = new Rect(bar.x + Gap + i * (ButtonWidth + Gap), bar.y + Gap, ButtonWidth, ButtonHeight);
                string cost = def.IceCost > 0 ? $"{def.RegolithCost} regolith, {def.IceCost} ice" : $"{def.RegolithCost} regolith";

                GUI.color = Colony.Instance.CanAfford(def) ? Color.white : new Color(1f, 0.55f, 0.55f);
                if (GUI.Button(rect, $"<b>[{i + 1}] {def.Name}</b>\n<size=11>{cost}</size>", button))
                    BuildingPlacer.Instance.Begin(def);
                GUI.color = Color.white;

                if (rect.Contains(Event.current.mousePosition)) hovered = def;
            }

            if (hovered != null)
            {
                var tip = new Rect(bar.x, bar.y - 48f, bar.width, 42f);
                Panel(tip);
                GUI.Label(new Rect(tip.x + 8f, tip.y + 4f, tip.width - 16f, tip.height - 8f), $"{hovered.Description} {PowerText(hovered)}", wrap);
            }
        }

        void DrawSelection()
        {
            var selection = SelectionManager.Instance;
            if (!selection.HasSelection) return;

            var rect = SelectionRect;
            Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            if (selection.SelectedBuilding != null)
            {
                var b = selection.SelectedBuilding;
                GUILayout.Label(b.Def.Name, title);
                GUILayout.Label(b.Status, text);
                GUILayout.Label($"{b.Def.Description} {PowerText(b.Def)}", wrap);
            }
            else
            {
                foreach (var c in selection.Selected)
                    GUILayout.Label($"<b>{c.DisplayName}</b> <color=#aaaaaa>({c.Specialty})</color>\n   {c.Describe()}", small);
            }
            GUILayout.EndArea();
        }

        void DrawHints()
        {
            var placer = BuildingPlacer.Instance;
            string hint = placer.IsPlacing
                ? $"Placing {placer.Current.Name}: left-click to place, R to rotate, hold Shift to place more, right-click or Esc to cancel"
                : "WASD pan   Q/E rotate   Scroll zoom   Left-click or drag to select   Right-click to move, mine or build   1-4 to build";
            Vector2 size = small.CalcSize(new GUIContent(hint));
            var rect = new Rect(Screen.width - size.x - 18f, TopBarHeight + 6f, size.x + 12f, size.y + 6f);
            Panel(rect);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 3f, size.x, size.y), hint, small);

            if (placer.IsPlacing && placer.InvalidReason != null)
            {
                Vector2 mouse = Event.current.mousePosition;
                GUI.Label(new Rect(mouse.x + 18f, mouse.y + 14f, 400f, 22f), $"<color=#ff7766>{placer.InvalidReason}</color>", text);
            }
        }

        void DrawToast()
        {
            if (string.IsNullOrEmpty(toast) || Time.unscaledTime > toastUntil) return;
            Vector2 size = text.CalcSize(new GUIContent(toast));
            var rect = new Rect((Screen.width - size.x) / 2f - 12f, Screen.height * 0.22f, size.x + 24f, size.y + 12f);
            Panel(rect);
            Panel(rect);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 6f, size.x, size.y), toast, text);
        }

        void DrawEndScreen()
        {
            var game = Era1Game.Instance;
            if (game == null || !game.IsOver) return;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var rect = new Rect((Screen.width - 520f) / 2f, (Screen.height - 260f) / 2f, 520f, 260f);
            Panel(rect);
            Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, rect.height - 32f));
            GUILayout.Label($"<color={(game.Won ? "#77dd77" : "#ff7777")}>{game.EndTitle}</color>", bigTitle);
            GUILayout.Space(10f);
            GUILayout.Label(game.EndMessage, new GUIStyle(wrap) { fontSize = 14 });
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Play again", button, GUILayout.Height(36f))) game.Restart();
            GUILayout.EndArea();
        }

        static void Panel(Rect rect) => GUI.Box(rect, GUIContent.none);

        static string PowerText(BuildingDef def)
        {
            string result = "";
            if (def.PowerOutput > 0f) result += $"Makes {def.PowerOutput:0} power{(def.NeedsSunlight ? " in daylight" : "")}. ";
            if (def.PowerDemand > 0f) result += $"Uses {def.PowerDemand:0} power. ";
            if (def.PowerRange > 0f) result += $"Power range {def.PowerRange:0} m.";
            return result;
        }

        static string FormatTime(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
