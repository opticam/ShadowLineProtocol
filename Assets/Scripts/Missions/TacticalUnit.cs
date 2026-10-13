using System.Collections.Generic;
using UnityEngine;

namespace ShadowLineProtocol
{
    public enum UnitStatus
    {
        Standing,
        // Suit breached or out of air. Bleeds out unless patched, refilled or carried to the staging zone.
        Downed,
        // Downed, but safe in the staging zone shelter.
        Stable,
        Lost,
    }

    // Something that can be picked up and carried: the cargo pallet or a supply crate.
    public class TacticalItem
    {
        public string Name;
        public bool Heavy;
        public Vector2Int Cell;
        public TacticalUnit Carrier;
        public bool Revealed = true;
        public bool Delivered;
        public GameObject Visual;
        public int Regolith;
        public int Ice;
        public GearId? Gear;

        public bool OnGround => Carrier == null && !Delivered && Revealed;
    }

    // One crew member on the tactical map: their suit, action points and cardboard standee.
    public class TacticalUnit : MonoBehaviour
    {
        public const int MaxActionPoints = 2;
        public const int BleedOutTurns = 3;
        const float WalkSpeed = 7f;
        const float LeapSeconds = 0.6f;

        public CrewLoadout Loadout { get; private set; }
        public Colonist Colonist => Loadout.Colonist;
        public string Name => Colonist.ShortName;

        public Vector2Int Cell;
        public int ActionPoints;
        public int Oxygen;
        public int MaxOxygen;
        public int Integrity;
        public int MaxIntegrity;
        public float Dose;
        public UnitStatus Status = UnitStatus.Standing;
        public int BleedOut;
        public int DownedOnTurn;
        public string DownedCause;
        public bool WasDowned;
        public bool ConsumableUsed;
        public TacticalItem CarriedItem { get; set; }
        public TacticalUnit CarriedUnit;
        public TacticalUnit CarriedBy;

        public bool Standing => Status == UnitStatus.Standing;
        public bool Carrying => CarriedItem != null || CarriedUnit != null;
        public bool CarryingHeavy => CarriedUnit != null || (CarriedItem != null && CarriedItem.Heavy);
        public bool StrongCarrier => Colonist.Role == CrewRole.Hauler || HasTool(GearId.TetherWinch);
        public int MoveRange => CarryingHeavy && !StrongCarrier ? 2 : 4;
        public bool Animating => path.Count > 0 || leaping;

        readonly Queue<Vector3> path = new Queue<Vector3>();
        Transform board;
        GameObject ring;
        GameObject carryBox;
        bool leaping;
        float leapTime;
        Vector3 leapFrom;
        Vector3 leapTo;

        public bool HasTool(GearId id) => Loadout.Tool1 == id || Loadout.Tool2 == id;
        public bool HasConsumable(GearId id) => !ConsumableUsed && Loadout.Consumable == id;

        public void Init(CrewLoadout loadout, Vector2Int cell, Vector3 position, int startingOxygen, float startingDose)
        {
            Loadout = loadout;
            Cell = cell;
            transform.position = position;
            var suit = loadout.SuitDef;
            MaxOxygen = suit.Oxygen;
            Oxygen = Mathf.Clamp(startingOxygen, 1, MaxOxygen);
            MaxIntegrity = suit.Integrity;
            Integrity = MaxIntegrity;
            Dose = startingDose;
            ActionPoints = MaxActionPoints;
            BuildVisual(suit);
            SetSelected(false);
        }

        void BuildVisual(GearDef suit)
        {
            Cutouts.Part(transform, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.8f, 0.03f, 0.8f), Cutouts.DarkCardboard);
            ring = Cutouts.Part(transform, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(1.7f, 0.01f, 1.7f), new Color(0.3f, 1f, 0.4f));

            board = new GameObject("Board").transform;
            board.SetParent(transform, false);
            Color accent = Colonist.Accent;
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.75f, 1.5f, 0.05f), suit.Color);
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 1.85f, 0f), new Vector3(0.6f, 0.55f, 0.05f), suit.Color);
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 1.85f, -0.04f), new Vector3(0.42f, 0.25f, 0.02f), new Color(0.85f, 0.6f, 0.15f));
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 1.1f, -0.04f), new Vector3(0.55f, 0.14f, 0.02f), accent);
            Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.08f), new Vector3(0.5f, 0.7f, 0.12f), accent * 0.8f);

            int i = 0;
            foreach (var id in new[] { Loadout.Tool1, Loadout.Tool2 })
            {
                if (!id.HasValue) continue;
                Cutouts.Part(board, PrimitiveType.Cube, new Vector3(i == 0 ? -0.3f : 0.3f, 0.45f, -0.05f), new Vector3(0.16f, 0.16f, 0.05f), GearDef.Get(id.Value).Color);
                i++;
            }
            carryBox = Cutouts.Part(board, PrimitiveType.Cube, new Vector3(0f, 2.55f, 0f), new Vector3(0.7f, 0.45f, 0.5f), Color.white);
            carryBox.SetActive(false);
        }

        public void SetSelected(bool selected) => ring.SetActive(selected);

        public void SetCarryVisual(Color? color)
        {
            carryBox.SetActive(color.HasValue);
            if (color.HasValue) carryBox.GetComponent<Renderer>().material.color = color.Value;
        }

        public void WalkPath(List<Vector3> points)
        {
            foreach (var p in points) path.Enqueue(p);
        }

        public void Leap(Vector3 to)
        {
            leaping = true;
            leapTime = 0f;
            leapFrom = transform.position;
            leapTo = to;
        }

        public void Teleport(Vector3 position)
        {
            path.Clear();
            leaping = false;
            transform.position = position;
        }

        // Mission time is paused colony time, so animate on unscaled time.
        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (CarriedBy != null)
            {
                transform.position = CarriedBy.transform.position + Vector3.up * 1.2f;
            }
            else if (leaping)
            {
                leapTime += dt;
                float t = Mathf.Clamp01(leapTime / LeapSeconds);
                transform.position = Vector3.Lerp(leapFrom, leapTo, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.5f;
                if (t >= 1f) leaping = false;
            }
            else if (path.Count > 0)
            {
                Vector3 target = path.Peek();
                transform.position = Vector3.MoveTowards(transform.position, target, WalkSpeed * dt);
                if ((transform.position - target).sqrMagnitude < 0.0001f) path.Dequeue();
            }

            gameObject.SetActive(Status != UnitStatus.Lost);
            var cam = Camera.main;
            float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            // Downed crew lie flat; standees otherwise face the camera.
            board.rotation = Status == UnitStatus.Standing ? Quaternion.Euler(0f, yaw, 0f) : Quaternion.Euler(80f, yaw, 0f);
        }
    }
}
