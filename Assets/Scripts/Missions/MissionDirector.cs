using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadowLineProtocol
{
    public enum MissionPhase
    {
        None,
        ReadyRoom,
        TransitOut,
        Tactical,
        Debrief,
        TransitBack,
    }

    // Runs a mission end to end: ready room, walk out, turn-based map, debrief, walk home.
    // The colony clock is paused except while the team is walking.
    public class MissionDirector : MonoBehaviour
    {
        const float TransitTimeScale = 6f;
        static readonly Vector3 ReadyRoomOrigin = new Vector3(-3000f, 0f, 0f);
        static readonly Vector3 TacticalOrigin = new Vector3(3000f, 0f, 0f);

        public static MissionDirector Instance { get; private set; }

        public MissionPhase Phase { get; private set; }
        public bool Active => Phase != MissionPhase.None;

        Camera cam;
        GameObject cargoPod;
        MissionDef mission;
        ReadyRoom readyRoom;
        TacticalMission tactical;
        MissionResult result;
        List<CrewLoadout> squad;
        readonly List<Colonist> walkers = new List<Colonist>();
        readonly List<Vector3> walkFrom = new List<Vector3>();
        readonly List<Vector3> walkTo = new List<Vector3>();
        float transitElapsed;
        Vector3 site;

        void Awake() => Instance = this;

        public void Init(Camera cam, GameObject cargoPod)
        {
            this.cam = cam;
            this.cargoPod = cargoPod;
        }

        public void OpenReadyRoom(IEnumerable<Colonist> preselect)
        {
            if (Active || Era1Game.Instance.IsOver) return;
            if (cargoPod == null)
            {
                HUD.Instance.Toast("There's nothing left to recover.");
                return;
            }
            if (Colony.Instance.Colonists.Count == 0) return;

            mission = MissionDef.CargoPodRecovery;
            site = cargoPod.transform.position;
            SetColonyControls(false);
            Time.timeScale = 0f;

            var room = new GameObject("Ready room");
            room.transform.SetParent(transform, false);
            room.transform.position = ReadyRoomOrigin;
            readyRoom = room.AddComponent<ReadyRoom>();
            readyRoom.Init(mission, cam, preselect ?? Enumerable.Empty<Colonist>());
            readyRoom.Departed += OnDeparted;
            readyRoom.Cancelled += OnReadyRoomCancelled;
            DayNightCycle.Instance.OverrideLighting(Quaternion.Euler(55f, 160f, 0f), 0.6f, new Color(0.45f, 0.45f, 0.48f));
            Phase = MissionPhase.ReadyRoom;
        }

        void OnReadyRoomCancelled()
        {
            Destroy(readyRoom.gameObject);
            readyRoom = null;
            ReturnToColony();
        }

        void OnDeparted(List<CrewLoadout> chosen)
        {
            squad = chosen;
            Destroy(readyRoom.gameObject);
            readyRoom = null;
            DayNightCycle.Instance.ClearOverride();

            // Suit up at the lander airlock, then walk out to the site.
            Vector3 airlock = Vector3.back * 3f;
            var crew = squad.Select(l => l.Colonist).ToList();
            foreach (var c in crew) c.SetOnMission(true);
            StartWalk(crew, airlock, site);
            Phase = MissionPhase.TransitOut;
        }

        void StartWalk(List<Colonist> crew, Vector3 from, Vector3 to)
        {
            walkers.Clear();
            walkFrom.Clear();
            walkTo.Clear();
            for (int i = 0; i < crew.Count; i++)
            {
                Vector3 offset = new Vector3((i - (crew.Count - 1) * 0.5f) * 1.4f, 0f, 0f);
                crew[i].gameObject.SetActive(true);
                crew[i].transform.position = from + offset;
                walkers.Add(crew[i]);
                walkFrom.Add(from + offset);
                walkTo.Add(to + offset + (to - from).normalized * -2.5f);
            }
            transitElapsed = 0f;
            Time.timeScale = TransitTimeScale;
        }

        void Update()
        {
            if (Active && Era1Game.Instance.IsOver)
            {
                AbortToColony();
                return;
            }
            if (Phase == MissionPhase.TransitOut || Phase == MissionPhase.TransitBack) UpdateTransit();
        }

        void UpdateTransit()
        {
            transitElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(transitElapsed / mission.TransitSeconds);
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < walkers.Count; i++)
            {
                if (walkers[i] == null) continue;
                walkers[i].transform.position = Vector3.Lerp(walkFrom[i], walkTo[i], t);
                centroid += walkers[i].transform.position;
            }
            if (walkers.Count > 0) centroid /= walkers.Count;

            var rotation = Quaternion.Euler(50f, 20f, 0f);
            cam.transform.SetPositionAndRotation(centroid - rotation * Vector3.forward * 32f, rotation);

            if (t < 1f) return;
            if (Phase == MissionPhase.TransitOut) StartTactical();
            else FinishReturn();
        }

        void StartTactical()
        {
            Time.timeScale = 0f;
            foreach (var c in walkers) c.gameObject.SetActive(false);

            var arena = new GameObject("Mission: " + mission.Site);
            arena.transform.SetParent(transform, false);
            arena.transform.position = TacticalOrigin;
            tactical = arena.AddComponent<TacticalMission>();
            tactical.Init(mission, squad, cam);
            tactical.Finished += OnTacticalFinished;
            DayNightCycle.Instance.OverrideLighting(Quaternion.Euler(24f, 200f, 0f), 1.15f, new Color(0.3f, 0.3f, 0.33f));
            Phase = MissionPhase.Tactical;
        }

        void OnTacticalFinished(MissionResult missionResult)
        {
            result = missionResult;
            Phase = MissionPhase.Debrief;
        }

        void AcceptDebrief()
        {
            ApplyResult();
            Destroy(tactical.gameObject);
            tactical = null;
            DayNightCycle.Instance.ClearOverride();

            var survivors = result.Crew.Where(r => r.Fate != CrewFate.Lost).Select(r => r.Colonist).ToList();
            if (survivors.Count == 0)
            {
                FinishReturn();
                return;
            }
            StartWalk(survivors, site, Vector3.back * 3f);
            Phase = MissionPhase.TransitBack;
        }

        void ApplyResult()
        {
            var colony = Colony.Instance;
            foreach (var report in result.Crew)
            {
                if (report.Fate == CrewFate.Lost)
                {
                    report.Colonist.Remove();
                    continue;
                }
                report.Colonist.AddDose(report.Dose + mission.TransitDose);
                if (report.Fate == CrewFate.Injured) report.Colonist.Injure();
            }
            foreach (var spent in result.GearSpent) colony.Gear.Add(spent.Key, -spent.Value);
            foreach (var found in result.GearFound) colony.Gear.Add(found.Key, found.Value);
            colony.Add(ResourceType.Regolith, result.Regolith);
            colony.Add(ResourceType.Ice, result.Ice);

            if (result.Pod != PodState.StillOutThere)
            {
                Era1Objectives.Instance.ResolveCargoPod(result.Pod == PodState.Recovered);
                cargoPod = null;
            }
        }

        void FinishReturn()
        {
            foreach (var c in walkers)
            {
                if (c == null) continue;
                c.SetOnMission(false);
                c.MoveTo(c.transform.position + Vector3.back * 1.5f);
            }
            walkers.Clear();
            string summary = result != null ? $"Mission over: {result.Title}." : null;
            result = null;
            squad = null;
            ReturnToColony();
            if (summary != null) HUD.Instance.Toast(summary);
        }

        void ReturnToColony()
        {
            Phase = MissionPhase.None;
            if (!Era1Game.Instance.IsOver) Time.timeScale = 1f;
            DayNightCycle.Instance.ClearOverride();
            SetColonyControls(true);
        }

        // The colony ran out of air (or similar) while the team was away.
        void AbortToColony()
        {
            if (readyRoom != null) Destroy(readyRoom.gameObject);
            if (tactical != null) Destroy(tactical.gameObject);
            readyRoom = null;
            tactical = null;
            if (squad != null)
            {
                foreach (var l in squad)
                {
                    if (l.Colonist == null) continue;
                    l.Colonist.gameObject.SetActive(true);
                    l.Colonist.SetOnMission(false);
                }
            }
            walkers.Clear();
            squad = null;
            result = null;
            Phase = MissionPhase.None;
            DayNightCycle.Instance.ClearOverride();
            SetColonyControls(true);
        }

        void SetColonyControls(bool on)
        {
            GetComponent<RTSCamera>().enabled = on;
            GetComponent<SelectionManager>().enabled = on;
            GetComponent<BuildingPlacer>().enabled = on;
            GetComponent<HUD>().enabled = on;
        }

        // ---- UI for the walking legs and the debrief ----

        void OnGUI()
        {
            if (Phase != MissionPhase.TransitOut && Phase != MissionPhase.TransitBack && Phase != MissionPhase.Debrief) return;
            UiKit.Ensure();
            GUI.depth = -50;
            if (Phase == MissionPhase.Debrief) DrawDebrief();
            else DrawTransit();
        }

        void DrawTransit()
        {
            var rect = new Rect((Screen.width - 520f) / 2f, 20f, 520f, 92f);
            UiKit.Panel(rect);
            bool outbound = Phase == MissionPhase.TransitOut;
            string names = string.Join(", ", walkers.Where(w => w != null).Select(w => w.ShortName));
            float left = Mathf.Max(0f, mission.TransitSeconds - transitElapsed);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f),
                outbound ? $"<b>On foot to the {mission.Site.ToLower()}</b>: {names}" : $"<b>Walking home</b>: {names}", UiKit.Text);
            UiKit.Bar(new Rect(rect.x + 12f, rect.y + 36f, rect.width - 24f, 10f), transitElapsed / mission.TransitSeconds, new Color(0.5f, 0.8f, 1f));

            var colony = Colony.Instance;
            var cycle = DayNightCycle.Instance;
            GUI.Label(new Rect(rect.x + 12f, rect.y + 52f, rect.width - 24f, 34f),
                $"<color=#bbbbbb>Colony time x{TransitTimeScale:0}: {left:0} s left.  Sol {cycle.Sol} {(cycle.IsDay ? "day" : "night")}   " +
                $"Oxygen {colony.Get(ResourceType.Oxygen)}   Battery {colony.Battery:0}</color>", UiKit.Wrap);
        }

        void DrawDebrief()
        {
            UiKit.Dim(0.55f);
            var rect = new Rect((Screen.width - 600f) / 2f, (Screen.height - 470f) / 2f, 600f, 470f);
            UiKit.Panel(rect);
            UiKit.Panel(rect);
            GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 14f, rect.width - 40f, rect.height - 28f));
            GUILayout.Label("DEBRIEF", UiKit.Title);
            string color = result.Succeeded ? "#77dd77" : result.Outcome == MissionOutcome.Withdrew ? "#ffd966" : "#ff8866";
            GUILayout.Label($"<color={color}>{result.Title}</color>", UiKit.BigTitle);
            GUILayout.Label(result.Summary, UiKit.Wrap);
            GUILayout.Space(8f);

            GUILayout.Label("CREW", UiKit.Title);
            foreach (var r in result.Crew)
            {
                string fate = r.Fate == CrewFate.Lost ? "<color=#ff5544>LOST</color>" : r.Fate == CrewFate.Injured ? "<color=#ffaa55>Injured</color>" : "<color=#77dd77>OK</color>";
                string dose = r.Fate == CrewFate.Lost ? "" : $"  +{r.Dose + mission.TransitDose:0} mSv";
                GUILayout.Label($"<b>{r.Colonist.ShortName}</b>: {fate}{dose}. {r.Note}", UiKit.Wrap);
            }
            GUILayout.Space(6f);

            GUILayout.Label("RECOVERED", UiKit.Title);
            var gains = new List<string>();
            if (result.Regolith > 0) gains.Add($"{result.Regolith} regolith");
            if (result.Ice > 0) gains.Add($"{result.Ice} ice");
            gains.AddRange(result.GearFound.Select(g => $"{g.Value} x {GearDef.Get(g.Key).Name}"));
            GUILayout.Label(gains.Count > 0 ? string.Join(", ", gains) : "Nothing.", UiKit.Wrap);
            if (result.GearSpent.Count > 0)
                GUILayout.Label("<color=#bbbbbb>Gear used or lost: " + string.Join(", ", result.GearSpent.Select(g => $"{g.Value} x {GearDef.Get(g.Key).Name}")) + "</color>", UiKit.Wrap);

            GUILayout.Space(6f);
            string pod = result.Pod == PodState.Recovered ? "The pod is recovered."
                : result.Pod == PodState.Destroyed ? "The pod is destroyed. The Landfall objective is marked failed, but the colony carries on."
                : "The pod is still out there. You can mount another attempt from the colony map.";
            GUILayout.Label($"<i>{pod}</i>", UiKit.Wrap);

            GUILayout.FlexibleSpace();
            bool anyHome = result.Crew.Any(r => r.Fate != CrewFate.Lost);
            string button = anyHome ? $"Walk home ({mission.TransitSeconds:0} s of colony time)" : "Return to the colony";
            if (GUILayout.Button(button, UiKit.Button, GUILayout.Height(36f))) AcceptDebrief();
            GUILayout.EndArea();
        }
    }
}
