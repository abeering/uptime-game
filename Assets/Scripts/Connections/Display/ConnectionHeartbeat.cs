using System;
using UnityEngine;

namespace Uptime.Connections.Display
{
    public enum HeartbeatEasing { Linear, SmoothStep, SmootherStep }

    // An unscaled presentation clock. Never reads or advances SimulationHost.
    public sealed class ConnectionHeartbeat : MonoBehaviour
    {
        [Range(30, 240)] public float bpm = 120;
        [Min(1)] public float stepDistance = 18;
        [Range(.1f, .9f)] public float movingFraction = .58f;
        public HeartbeatEasing easing = HeartbeatEasing.SmoothStep;
        public bool paused;
        public bool externallyDriven;
        public double Beats { get; private set; }
        public double ElapsedSeconds { get; private set; }
        private void Update() { if (!externallyDriven) Advance(Time.unscaledDeltaTime); }
        public void Advance(double seconds)
        {
            if (paused || seconds <= 0) return;
            ElapsedSeconds += seconds;
            Beats += seconds * Mathf.Clamp(bpm, 30, 240) / 60;
        }
        public void TogglePause() => paused = !paused;
        public void Replay() { Beats = ElapsedSeconds = 0; }
        public void SetReviewTime(double seconds)
        { ElapsedSeconds = Math.Max(0, seconds); Beats = ElapsedSeconds * bpm / 60; }
        public int Cadence(float speed) => speed >= .85f ? 1 : speed >= .4f ? 2 : 4;
        public double Travel(double beats, float speed, float phaseBeats = 0, int subdivision = 1)
        {
            if (speed <= .0001f) return 0;
            subdivision = Mathf.Clamp(subdivision, 1, 4);
            int cadence = Cadence(speed);
            double beat = Math.Max(0, beats * subdivision + phaseBeats);
            double step = Math.Floor(beat / cadence);
            float t = Mathf.Clamp01((float)(beat - step * cadence) / movingFraction);
            if (easing == HeartbeatEasing.SmoothStep) t = t * t * (3 - 2 * t);
            else if (easing == HeartbeatEasing.SmootherStep) t = t * t * t * (t * (6 * t - 15) + 10);
            return (step + t) * stepDistance * speed * cadence / subdivision;
        }
    }
}
