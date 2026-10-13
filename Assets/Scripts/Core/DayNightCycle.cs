using System;
using UnityEngine;

namespace ShadowLineProtocol
{
    public class DayNightCycle : MonoBehaviour
    {
        public const float DayLength = 150f;
        public const float NightLength = 60f;
        const float SunElevation = 15f;
        static readonly Color DayAmbient = new Color(0.32f, 0.32f, 0.34f);
        static readonly Color NightAmbient = new Color(0.08f, 0.09f, 0.15f);

        public static DayNightCycle Instance { get; private set; }

        public event Action NightEnded;
        public int Sol { get; private set; } = 1;
        public bool IsDay => phase < DayLength;
        public float SecondsUntilChange => IsDay ? DayLength - phase : DayLength + NightLength - phase;

        Light sun;
        float phase;
        float daylight01 = 1f;

        void Awake() => Instance = this;

        public void Init(Light sun)
        {
            this.sun = sun;
            ApplyLighting();
        }

        void Update()
        {
            phase += Time.deltaTime;
            if (phase >= DayLength + NightLength)
            {
                phase -= DayLength + NightLength;
                Sol++;
                NightEnded?.Invoke();
            }
            daylight01 = Mathf.MoveTowards(daylight01, IsDay ? 1f : 0f, Time.deltaTime / 4f);
            ApplyLighting();
        }

        // At the pole the sun circles the horizon instead of rising and setting.
        void ApplyLighting()
        {
            float yaw = phase / (DayLength + NightLength) * 360f;
            sun.transform.rotation = Quaternion.Euler(SunElevation, yaw, 0f);
            sun.intensity = Mathf.Lerp(0.05f, 1.2f, daylight01);
            RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, daylight01);
        }
    }
}
