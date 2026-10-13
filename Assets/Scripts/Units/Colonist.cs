using UnityEngine;

namespace ShadowLineProtocol
{
    public enum ColonistState
    {
        Idle,
        Moving,
        ToDeposit,
        Mining,
        Hauling,
        ToBuildSite,
        Building,
    }

    // Each crew member's mission perk (see CrewPerks).
    public enum CrewRole
    {
        Hauler,
        Engineer,
        Prospector,
    }

    public class Colonist : MonoBehaviour
    {
        public const int CarryCapacity = 10;
        public const float DoseLimit = 100f;
        const float SecondsPerUnitMined = 0.4f;
        const float Speed = 5f;
        const float InjuredWorkRate = 0.5f;
        const float NextDepositSearchRadius = 25f;
        const float ArrivalTolerance = 0.05f;

        public string DisplayName { get; private set; }
        public string ShortName { get; private set; }
        public string Specialty { get; private set; }
        public CrewRole Role { get; private set; }
        public Color Accent { get; private set; }
        public ColonistState State { get; private set; }
        public int Carrying { get; private set; }
        public ResourceType CarryType { get; private set; }
        public bool Selected { get; private set; }
        // Away on a mission: the director moves them and the colony can't give them orders.
        public bool OnMission { get; private set; }
        // Career radiation dose in mSv.
        public float Dose { get; private set; }
        public int InjuredUntilSol { get; private set; }
        public bool IsInjured => DayNightCycle.Instance != null && DayNightCycle.Instance.Sol <= InjuredUntilSol;
        float WorkRate => IsInjured ? InjuredWorkRate : 1f;

        Transform board;
        GameObject ring;
        GameObject carryBox;
        Vector3 moveTarget;
        ResourceNode deposit;
        ResourceType miningType;
        Vector3 lastDepositPosition;
        Building site;
        float timer;

        public void Init(string displayName, string shortName, string specialty, CrewRole role, Color accent,
            Transform board, GameObject ring, GameObject carryBox)
        {
            DisplayName = displayName;
            ShortName = shortName;
            Specialty = specialty;
            Role = role;
            Accent = accent;
            this.board = board;
            this.ring = ring;
            this.carryBox = carryBox;
            carryBox.SetActive(false);
            SetSelected(false);
            Colony.Instance.Colonists.Add(this);
        }

        public void SetSelected(bool selected)
        {
            Selected = selected;
            ring.SetActive(selected);
            var selectable = GetComponent<Selectable>();
            if (selectable != null) selectable.SetSelected(selected);
        }

        public void SetOnMission(bool value)
        {
            if (value)
            {
                Stop();
                SetSelected(false);
            }
            OnMission = value;
        }

        public void AddDose(float mSv) => Dose += mSv;

        // Injured crew work at half speed until the end of the next sol.
        public void Injure() => InjuredUntilSol = DayNightCycle.Instance.Sol + 1;

        // Permanent loss on a mission.
        public void Remove()
        {
            Colony.Instance.Colonists.Remove(this);
            Destroy(gameObject);
        }

        public void Stop()
        {
            deposit = null;
            site = null;
            State = ColonistState.Idle;
        }

        public void MoveTo(Vector3 position)
        {
            deposit = null;
            site = null;
            moveTarget = position;
            State = ColonistState.Moving;
        }

        public void Mine(ResourceNode target)
        {
            if (target == null) return;
            site = null;
            SetDeposit(target);
            // Drop off a different material before switching jobs.
            State = Carrying > 0 && CarryType != target.Type ? ColonistState.Hauling : ColonistState.ToDeposit;
        }

        public void Construct(Building building)
        {
            deposit = null;
            site = building;
            State = ColonistState.ToBuildSite;
        }

        public string Describe()
        {
            string status = DescribeState();
            return IsInjured ? status + " (injured: half speed)" : status;
        }

        string DescribeState()
        {
            if (OnMission) return "On a mission";
            switch (State)
            {
                case ColonistState.Moving: return "Moving";
                case ColonistState.ToDeposit: return $"Heading to {miningType.ToString().ToLower()}";
                case ColonistState.Mining: return $"Mining {miningType.ToString().ToLower()} ({Carrying}/{CarryCapacity})";
                case ColonistState.Hauling: return $"Hauling {Carrying} {CarryType.ToString().ToLower()}";
                case ColonistState.ToBuildSite: return "Heading to build site";
                case ColonistState.Building: return "Building";
                default: return Carrying > 0 ? $"Idle (carrying {Carrying} {CarryType.ToString().ToLower()})" : "Idle";
            }
        }

        void Update()
        {
            if (OnMission)
            {
                FaceCamera();
                return;
            }

            switch (State)
            {
                case ColonistState.Moving:
                    if (StepTowards(moveTarget, 0.1f)) State = ColonistState.Idle;
                    break;

                case ColonistState.ToDeposit:
                    if (!DepositAvailable()) { FindNextDeposit(); break; }
                    if (StepTowards(deposit.transform.position, deposit.Radius + 0.5f))
                    {
                        State = ColonistState.Mining;
                        timer = 0f;
                    }
                    break;

                case ColonistState.Mining:
                    if (!DepositAvailable())
                    {
                        if (Carrying > 0) State = ColonistState.Hauling;
                        else FindNextDeposit();
                        break;
                    }
                    timer += Time.deltaTime * WorkRate;
                    if (timer < SecondsPerUnitMined) break;
                    timer = 0f;
                    if (Carrying == 0) CarryType = deposit.Type;
                    Carrying += deposit.Take(1);
                    RefreshCarryBox();
                    if (Carrying >= CarryCapacity) State = ColonistState.Hauling;
                    break;

                case ColonistState.Hauling:
                    var dropOff = FindNearestDropOff();
                    if (dropOff == null) { State = ColonistState.Idle; break; }
                    if (StepTowards(dropOff.transform.position, ReachOf(dropOff)))
                    {
                        Colony.Instance.Add(CarryType, Carrying);
                        Carrying = 0;
                        RefreshCarryBox();
                        if (DepositAvailable()) State = ColonistState.ToDeposit;
                        else FindNextDeposit();
                    }
                    break;

                case ColonistState.ToBuildSite:
                    if (site == null || site.IsComplete) { State = ColonistState.Idle; break; }
                    if (StepTowards(site.transform.position, ReachOf(site))) State = ColonistState.Building;
                    break;

                case ColonistState.Building:
                    if (site == null || site.IsComplete) { site = null; State = ColonistState.Idle; break; }
                    site.AddWork(Time.deltaTime * WorkRate);
                    break;
            }

            FaceCamera();
        }

        void FaceCamera()
        {
            var cam = Camera.main;
            if (cam != null) board.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
        }

        void SetDeposit(ResourceNode node)
        {
            deposit = node;
            miningType = node.Type;
            lastDepositPosition = node.transform.position;
        }

        bool DepositAvailable() => deposit != null && deposit.Amount > 0;

        // Keep working nearby deposits of the same type once one runs out.
        void FindNextDeposit()
        {
            var next = ResourceNode.FindNearest(miningType, lastDepositPosition, NextDepositSearchRadius);
            if (next != null)
            {
                SetDeposit(next);
                State = ColonistState.ToDeposit;
            }
            else
            {
                deposit = null;
                State = Carrying > 0 ? ColonistState.Hauling : ColonistState.Idle;
            }
        }

        Building FindNearestDropOff()
        {
            Building best = null;
            float bestSqr = float.MaxValue;
            foreach (var b in Colony.Instance.Buildings)
            {
                if (!b.Def.IsDropOff || !b.IsComplete) continue;
                float sqr = (b.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = b;
                }
            }
            return best;
        }

        static float ReachOf(Building b) => Mathf.Max(b.Def.Size.x, b.Def.Size.z) * 0.5f + 0.7f;

        void RefreshCarryBox()
        {
            carryBox.SetActive(Carrying > 0);
            if (Carrying > 0) carryBox.GetComponent<Renderer>().material.color = Cutouts.CarryColor(CarryType);
        }

        bool StepTowards(Vector3 target, float stopDistance)
        {
            Vector3 position = transform.position;
            target.y = position.y;
            Vector3 delta = target - position;
            float distance = delta.magnitude;
            // The tolerance matters: without it float rounding can leave a unit stuck a hair short forever.
            if (distance <= stopDistance + ArrivalTolerance) return true;
            float remaining = distance - stopDistance;
            float step = Speed * WorkRate * Time.deltaTime;
            if (step >= remaining)
            {
                transform.position = position + delta / distance * remaining;
                return true;
            }
            transform.position = position + delta / distance * step;
            return false;
        }
    }
}
