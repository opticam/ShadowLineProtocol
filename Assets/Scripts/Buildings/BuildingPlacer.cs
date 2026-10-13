using UnityEngine;

namespace ShadowLineProtocol
{
    public class BuildingPlacer : MonoBehaviour
    {
        static readonly Color ValidColor = new Color(0.3f, 1f, 0.4f);
        static readonly Color InvalidColor = new Color(1f, 0.3f, 0.25f);

        public static BuildingPlacer Instance { get; private set; }

        public BuildingDef Current { get; private set; }
        public bool IsPlacing => Current != null;
        public string InvalidReason { get; private set; }
        // Lets SelectionManager ignore the click that placed or cancelled a building.
        public int LastActionFrame { get; private set; } = -1;

        Camera cam;
        GameObject ghost;
        Renderer[] ghostRenderers;
        float yaw;

        void Awake() => Instance = this;

        public void Init(Camera cam) => this.cam = cam;

        // Disabled while a mission runs.
        void OnDisable() => Cancel();

        public void Begin(BuildingDef def)
        {
            if (Era1Game.Instance.IsOver) return;
            Cancel();
            Current = def;
            ghost = new GameObject("Placement ghost: " + def.Name);
            ghost.transform.SetParent(transform, false);
            ghost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Cutouts.BuildingVisual(def, ghost.transform, false);
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
        }

        public void Cancel()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
            Current = null;
            InvalidReason = null;
        }

        void Update()
        {
            if (Era1Game.Instance.IsOver) { Cancel(); return; }

            foreach (var def in BuildingDef.Buildable)
            {
                if (Input.GetKeyDown(def.Hotkey)) Begin(def);
            }
            if (!IsPlacing) return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                Cancel();
                LastActionFrame = Time.frameCount;
                return;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                yaw = (yaw + 90f) % 360f;
                ghost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (!TryGetGroundPoint(out Vector3 position)) return;
            ghost.transform.position = position;

            bool valid = Validate(position, out string reason);
            InvalidReason = reason;
            foreach (var r in ghostRenderers) r.material.color = valid ? ValidColor : InvalidColor;

            if (Input.GetMouseButtonDown(0) && !HUD.Instance.IsOverHUD(Input.mousePosition))
            {
                LastActionFrame = Time.frameCount;
                if (valid) Place(position);
                else HUD.Instance.Toast(reason);
            }
        }

        void Place(Vector3 position)
        {
            var def = Current;
            var colony = Colony.Instance;
            colony.Spend(def);
            var building = WorldBuilder.CreateBuilding(def, position, Quaternion.Euler(0f, yaw, 0f), false, Era1Game.Instance.transform);

            var builders = SelectionManager.Instance.Selected;
            if (builders.Count > 0)
            {
                foreach (var colonist in builders) colonist.Construct(building);
            }
            else
            {
                HUD.Instance.Toast("Foundation placed. Select colonists and right-click it to build.");
            }

            bool keepPlacing = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && colony.CanAfford(def);
            if (!keepPlacing) Cancel();
        }

        bool Validate(Vector3 position, out string reason)
        {
            var size = Current.Size;
            float edge = WorldBuilder.MapHalfSize - Mathf.Max(size.x, size.z);
            if (Mathf.Abs(position.x) > edge || Mathf.Abs(position.z) > edge)
            {
                reason = "Too close to the edge of the map";
                return false;
            }
            if (!InPowerRange(position))
            {
                reason = "Out of power range: build near the lander or a solar array";
                return false;
            }

            var halfExtents = new Vector3(size.x * 0.5f + 0.3f, size.y * 0.5f, size.z * 0.5f + 0.3f);
            var center = position + Vector3.up * (size.y * 0.5f + 0.1f);
            foreach (var hit in Physics.OverlapBox(center, halfExtents, Quaternion.Euler(0f, yaw, 0f)))
            {
                if (hit.GetComponentInParent<Building>() != null || hit.GetComponentInParent<ResourceNode>() != null)
                {
                    reason = "Blocked by another structure or deposit";
                    return false;
                }
            }

            if (!Colony.Instance.CanAfford(Current))
            {
                reason = "Not enough resources";
                return false;
            }

            reason = null;
            return true;
        }

        static bool InPowerRange(Vector3 position)
        {
            foreach (var b in Colony.Instance.Buildings)
            {
                if (b.Def.PowerRange <= 0f) continue;
                Vector3 delta = b.transform.position - position;
                delta.y = 0f;
                if (delta.magnitude <= b.Def.PowerRange) return true;
            }
            return false;
        }

        bool TryGetGroundPoint(out Vector3 point)
        {
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }
            point = default;
            return false;
        }
    }
}
