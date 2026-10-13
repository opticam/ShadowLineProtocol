using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowLineProtocol
{
    // Entry point: builds the whole Era 1 demo at runtime, so pressing Play in any scene starts it.
    public class Era1Game : MonoBehaviour
    {
        public static Era1Game Instance { get; private set; }
        public static bool AutoStart = true;

        public bool IsOver { get; private set; }
        public bool Won { get; private set; }
        public string EndTitle { get; private set; }
        public string EndMessage { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoCreate()
        {
            if (AutoStart && Instance == null) Create();
        }

        public static void Create()
        {
            new GameObject("Shadow Line Protocol - Era 1").AddComponent<Era1Game>();
        }

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;

            gameObject.AddComponent<Colony>();
            var cycle = gameObject.AddComponent<DayNightCycle>();
            cycle.Init(SetUpSun());
            var cam = SetUpCamera();

            var cargoPod = WorldBuilder.Build(transform);

            gameObject.AddComponent<RTSCamera>().Init(cam);
            gameObject.AddComponent<SelectionManager>().Init(cam);
            gameObject.AddComponent<BuildingPlacer>().Init(cam);
            gameObject.AddComponent<HUD>();
            gameObject.AddComponent<Era1Objectives>().Init(cargoPod);
            gameObject.AddComponent<MissionDirector>().Init(cam, cargoPod);
        }

        public void End(bool won, string title, string message)
        {
            if (IsOver) return;
            IsOver = true;
            Won = won;
            EndTitle = title;
            EndMessage = message;
            Time.timeScale = 0f;
        }

        public void Restart()
        {
            Destroy(gameObject);
            Instance = null;
            Create();
        }

        // Camera and sun stay outside the game object so they survive a restart.
        static Camera SetUpCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 600f;
            return cam;
        }

        static Light SetUpSun()
        {
            Light sun = null;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) { sun = light; break; }
            }
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;
            sun.color = new Color(1f, 0.97f, 0.92f);

            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            return sun;
        }
    }
}
