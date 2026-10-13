using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadowLineProtocol
{
    public class SelectionManager : MonoBehaviour
    {
        const float DragThreshold = 8f;
        const float MenuWidth = 300f;
        static readonly Color BoxColor = new Color(0.3f, 1f, 0.4f);

        class MenuOption
        {
            public string Label;
            public Action Run;
        }

        public static SelectionManager Instance { get; private set; }

        public readonly List<Colonist> Selected = new List<Colonist>();
        public Building SelectedBuilding { get; private set; }
        public bool HasSelection => Selected.Count > 0 || SelectedBuilding != null;
        // GUI coordinates (origin top-left) of the open action menu.
        public Rect MenuRect => menuOpen ? menuRect : Rect.zero;

        Camera cam;
        bool dragging;
        Vector2 dragStart;
        Selectable hovered;

        bool menuOpen;
        Rect menuRect;
        Vector2 menuAnchor;
        string menuTitle;
        string menuSubtitle;
        readonly List<MenuOption> menuOptions = new List<MenuOption>();
        GUIStyle menuTitleStyle, menuTextStyle, menuButtonStyle;

        void Awake() => Instance = this;

        public void Init(Camera cam) => this.cam = cam;

        void Update()
        {
            if (Era1Game.Instance.IsOver)
            {
                CloseMenu();
                SetHovered(null);
                return;
            }

            var placer = BuildingPlacer.Instance;
            if (placer.IsPlacing) CloseMenu();
            bool blocked = placer.IsPlacing || placer.LastActionFrame == Time.frameCount;
            Vector2 mouse = Input.mousePosition;
            bool overHud = HUD.Instance.IsOverHUD(mouse);

            UpdateHover(mouse, blocked || overHud);

            if (menuOpen && Input.GetKeyDown(KeyCode.Escape)) CloseMenu();

            if (Input.GetMouseButtonDown(0))
            {
                var guiMouse = new Vector2(mouse.x, Screen.height - mouse.y);
                if (menuOpen && !menuRect.Contains(guiMouse)) CloseMenu();
                if (!blocked && !overHud)
                {
                    dragging = true;
                    dragStart = mouse;
                }
            }
            if (Input.GetMouseButtonUp(0) && dragging)
            {
                dragging = false;
                if ((mouse - dragStart).magnitude > DragThreshold) BoxSelect(dragStart, mouse);
                else ClickSelect(mouse);
            }
            if (Input.GetMouseButtonDown(1) && !blocked && !overHud)
            {
                CloseMenu();
                IssueCommand(mouse);
            }
        }

        public void ClearSelection()
        {
            foreach (var c in Selected) c.SetSelected(false);
            Selected.Clear();
            SelectedBuilding = null;
        }

        static bool ShiftHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        void UpdateHover(Vector2 mouse, bool suppressed)
        {
            Selectable target = null;
            if (!suppressed && !dragging && Raycast(mouse, out RaycastHit hit))
                target = hit.collider.GetComponentInParent<Selectable>();
            SetHovered(target);
        }

        void SetHovered(Selectable target)
        {
            if (target == hovered) return;
            if (hovered != null) hovered.SetHovered(false);
            hovered = target;
            if (hovered != null) hovered.SetHovered(true);
        }

        void BoxSelect(Vector2 a, Vector2 b)
        {
            if (!ShiftHeld) ClearSelection();
            SelectedBuilding = null;
            var rect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            foreach (var c in Colony.Instance.Colonists)
            {
                Vector3 screen = cam.WorldToScreenPoint(c.transform.position + Vector3.up);
                if (screen.z > 0f && rect.Contains(screen) && !c.Selected) Add(c);
            }
        }

        void ClickSelect(Vector2 mouse)
        {
            if (Raycast(mouse, out RaycastHit hit))
            {
                var target = hit.collider.GetComponentInParent<Selectable>();
                if (target != null)
                {
                    OnClicked(target, mouse);
                    return;
                }
            }
            if (!ShiftHeld) ClearSelection();
        }

        void OnClicked(Selectable target, Vector2 mouse)
        {
            switch (target.Kind)
            {
                case SelectableKind.Colonist:
                    var colonist = target.GetComponent<Colonist>();
                    if (!ShiftHeld) ClearSelection();
                    SelectedBuilding = null;
                    if (colonist.Selected && ShiftHeld)
                    {
                        colonist.SetSelected(false);
                        Selected.Remove(colonist);
                        return;
                    }
                    if (!colonist.Selected) Add(colonist);
                    OpenColonistMenu(mouse);
                    break;

                case SelectableKind.Deposit:
                    OpenDepositMenu(target.GetComponent<ResourceNode>(), mouse);
                    break;

                case SelectableKind.Building:
                    var building = target.GetComponent<Building>();
                    if (building.IsComplete)
                    {
                        ClearSelection();
                        SelectedBuilding = building;
                    }
                    else
                    {
                        OpenFoundationMenu(building, mouse);
                    }
                    break;

                case SelectableKind.CargoPod:
                    OpenCargoPodMenu(target.transform, mouse);
                    break;
            }
        }

        void Add(Colonist c)
        {
            c.SetSelected(true);
            Selected.Add(c);
        }

        void IssueCommand(Vector2 mouse)
        {
            if (Selected.Count == 0 || !Raycast(mouse, out RaycastHit hit)) return;
            var crew = Selected.ToList();

            var target = hit.collider.GetComponentInParent<Selectable>();
            if (target != null && target.Kind == SelectableKind.Deposit)
            {
                var deposit = target.GetComponent<ResourceNode>();
                Order(crew, c => c.Mine(deposit), $"mining {Lower(deposit.Type)}");
                return;
            }
            if (target != null && target.Kind == SelectableKind.Building)
            {
                var building = target.GetComponent<Building>();
                if (!building.IsComplete)
                {
                    Order(crew, c => c.Construct(building), $"building the {building.Def.Name}");
                    return;
                }
            }
            if (target != null && target.Kind == SelectableKind.CargoPod)
            {
                Vector3 pod = target.transform.position;
                Order(crew, c => c.MoveTo(pod), "recovering the cargo pod");
                return;
            }

            for (int i = 0; i < crew.Count; i++)
            {
                Vector3 offset = i == 0 ? Vector3.zero : Quaternion.Euler(0f, 360f * i / (crew.Count - 1), 0f) * Vector3.forward * 1.4f;
                crew[i].MoveTo(hit.point + offset);
            }
        }

        // ---- Action menu ----

        void OpenColonistMenu(Vector2 mouse)
        {
            var crew = Selected.ToList();
            var first = crew[0];
            string title = crew.Count == 1 ? first.DisplayName : $"{crew.Count} colonists selected";
            string subtitle = crew.Count == 1
                ? $"{first.Specialty}. {first.Describe()}.\nRight-click the ground to move."
                : "Right-click the ground to move them.";
            BeginMenu(title, subtitle, mouse);

            AddOption("Mine nearest regolith", () => Order(crew, c => MineNearest(c, ResourceType.Regolith), "mining regolith"));
            AddOption("Mine nearest ice", () => Order(crew, c => MineNearest(c, ResourceType.Ice), "mining ice"));
            if (Colony.Instance.Buildings.Any(b => !b.IsComplete))
                AddOption("Build nearest foundation", () => Order(crew, BuildNearest, "building"));
            var pod = Selectable.All.FirstOrDefault(s => s.Kind == SelectableKind.CargoPod);
            if (pod != null)
            {
                Vector3 podPosition = pod.transform.position;
                AddOption("Recover the cargo pod", () => Order(crew, c => c.MoveTo(podPosition), "recovering the cargo pod"));
            }
            AddOption("Stop", () => Order(crew, c => c.Stop(), "stopped"));
        }

        void OpenDepositMenu(ResourceNode deposit, Vector2 mouse)
        {
            string type = Lower(deposit.Type);
            BeginMenu($"{deposit.Type} deposit", $"{deposit.Amount} left. Colonists keep mining and hauling it to the lander or a habitat.", mouse);
            AddCrewOptions($"Mine {type}", deposit.transform.position, c => c.Mine(deposit), $"mining {type}");
        }

        void OpenFoundationMenu(Building building, Vector2 mouse)
        {
            BeginMenu($"{building.Def.Name} foundation", $"{Mathf.RoundToInt(building.Progress01 * 100f)}% built.", mouse);
            AddCrewOptions("Build", building.transform.position, c => c.Construct(building), $"building the {building.Def.Name}");
        }

        void OpenCargoPodMenu(Transform pod, Vector2 mouse)
        {
            Vector3 position = pod.position;
            BeginMenu("Stray cargo pod", "Bounced off course on landing. Recover it for 60 regolith and 20 ice.", mouse);
            AddCrewOptions("Recover", position, c => c.MoveTo(position), "recovering the cargo pod");
        }

        void AddCrewOptions(string verb, Vector3 target, Action<Colonist> order, string doing)
        {
            var all = Colony.Instance.Colonists.ToList();
            var crew = Selected.ToList();
            if (crew.Count > 0)
                AddOption($"{verb} with {Names(crew)}", () => Order(crew, order, doing));

            var nearest = all.Where(c => !crew.Contains(c))
                .OrderBy(c => c.State == ColonistState.Idle ? 0 : 1)
                .ThenBy(c => (c.transform.position - target).sqrMagnitude)
                .FirstOrDefault();
            if (nearest != null)
            {
                string label = nearest.State == ColonistState.Idle ? "nearest idle colonist" : "nearest colonist";
                AddOption($"{verb} with {label} ({nearest.ShortName})", () => Order(new List<Colonist> { nearest }, order, doing));
            }

            if (all.Count > 1 && crew.Count < all.Count)
                AddOption($"{verb} with all colonists", () => Order(all, order, doing));
        }

        void Order(List<Colonist> crew, Action<Colonist> order, string doing)
        {
            foreach (var c in crew) order(c);
            HUD.Instance.Toast($"{Names(crew)}: {doing}");
        }

        static void MineNearest(Colonist c, ResourceType type)
        {
            var node = ResourceNode.FindNearest(type, c.transform.position, float.MaxValue);
            if (node != null) c.Mine(node);
        }

        static void BuildNearest(Colonist c)
        {
            var site = Colony.Instance.Buildings
                .Where(b => !b.IsComplete)
                .OrderBy(b => (b.transform.position - c.transform.position).sqrMagnitude)
                .FirstOrDefault();
            if (site != null) c.Construct(site);
        }

        static string Names(List<Colonist> crew) =>
            crew.Count <= 3 ? string.Join(", ", crew.Select(c => c.ShortName)) : $"{crew.Count} colonists";

        static string Lower(ResourceType type) => type.ToString().ToLower();

        void BeginMenu(string title, string subtitle, Vector2 mouse)
        {
            menuOpen = true;
            menuTitle = title;
            menuSubtitle = subtitle;
            menuOptions.Clear();
            menuAnchor = new Vector2(mouse.x + 14f, Screen.height - mouse.y);
            menuRect = new Rect(menuAnchor, Vector2.zero);
        }

        void AddOption(string label, Action run) => menuOptions.Add(new MenuOption { Label = label, Run = run });

        void CloseMenu()
        {
            menuOpen = false;
            menuOptions.Clear();
        }

        void OnGUI()
        {
            GUI.depth = -100;
            DrawDragBox();
            DrawMenu();
        }

        void DrawMenu()
        {
            if (!menuOpen) return;
            if (menuTitleStyle == null)
            {
                menuTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
                menuTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
                menuButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 8, 4, 4) };
            }

            const float pad = 10f, buttonHeight = 28f, spacing = 4f;
            float inner = MenuWidth - pad * 2f;
            float titleHeight = menuTitleStyle.CalcHeight(new GUIContent(menuTitle), inner);
            float subtitleHeight = menuTextStyle.CalcHeight(new GUIContent(menuSubtitle), inner);
            int buttons = menuOptions.Count + 1;
            float height = pad + titleHeight + subtitleHeight + 6f + buttons * (buttonHeight + spacing) + pad;

            float x = Mathf.Min(menuAnchor.x, Screen.width - MenuWidth - 4f);
            float y = Mathf.Clamp(menuAnchor.y - 10f, 4f, Screen.height - height - 4f);
            menuRect = new Rect(x, y, MenuWidth, height);

            GUI.Box(menuRect, GUIContent.none);
            GUI.Box(menuRect, GUIContent.none);
            float cy = y + pad;
            GUI.Label(new Rect(x + pad, cy, inner, titleHeight), menuTitle, menuTitleStyle);
            cy += titleHeight;
            GUI.Label(new Rect(x + pad, cy, inner, subtitleHeight), menuSubtitle, menuTextStyle);
            cy += subtitleHeight + 6f;

            MenuOption chosen = null;
            foreach (var option in menuOptions)
            {
                if (GUI.Button(new Rect(x + pad, cy, inner, buttonHeight), option.Label, menuButtonStyle)) chosen = option;
                cy += buttonHeight + spacing;
            }
            bool cancel = GUI.Button(new Rect(x + pad, cy, inner, buttonHeight), "Cancel", menuButtonStyle);

            if (chosen != null)
            {
                CloseMenu();
                chosen.Run();
            }
            else if (cancel)
            {
                CloseMenu();
            }
        }

        bool Raycast(Vector2 mouse, out RaycastHit hit) => Physics.Raycast(cam.ScreenPointToRay(mouse), out hit, 1000f);

        void DrawDragBox()
        {
            if (!dragging) return;
            Vector2 now = Input.mousePosition;
            if ((now - dragStart).magnitude <= DragThreshold) return;

            var rect = Rect.MinMaxRect(
                Mathf.Min(dragStart.x, now.x), Screen.height - Mathf.Max(dragStart.y, now.y),
                Mathf.Max(dragStart.x, now.x), Screen.height - Mathf.Min(dragStart.y, now.y));
            GUI.color = new Color(BoxColor.r, BoxColor.g, BoxColor.b, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = BoxColor;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1f, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.yMin, 1f, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
