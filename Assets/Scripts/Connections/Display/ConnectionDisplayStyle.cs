using UnityEngine;

namespace Uptime.Connections.Display
{
    public enum OrdinaryIdentityPalette { Uniform, MutedHues, Grayscale }
    public enum PerformanceColorTreatment { Blended, Uniform }

    [CreateAssetMenu(menuName = "Uptime/Connection display style")]
    public sealed class ConnectionDisplayStyle : ScriptableObject
    {
        [Header("Live palette experiment")]
        public OrdinaryIdentityPalette ordinaryIdentityPalette = OrdinaryIdentityPalette.MutedHues;
        public PerformanceColorTreatment performanceColorTreatment = PerformanceColorTreatment.Uniform;
        public Color uniformOrdinaryIdentity = new(.68f, .72f, .75f);
        public Color[] grayscaleIdentityColors = {
            new(.53f,.53f,.53f), new(.61f,.61f,.61f), new(.69f,.69f,.69f),
            new(.77f,.77f,.77f), new(.85f,.85f,.85f), new(.93f,.93f,.93f) };
        public Color uniformPerformanceBase = new(.65f, .70f, .74f);
        [Header("Stable identity families (same lifetime slot on every segment)")]
        public Color[] ordinaryIdentityColors = {
            new(.62f, .71f, .78f), new(.66f, .74f, .66f), new(.78f, .71f, .64f),
            new(.59f, .67f, .72f), new(.71f, .65f, .76f), new(.62f, .75f, .72f) };
        // Original blue/teal identity accents, now the business IMPORTANT family.
        [UnityEngine.Serialization.FormerlySerializedAs("priorityIdentityColors")]
        public Color[] importantIdentityColors = {
            new(.36f, .87f, .91f), new(.35f, .91f, .76f), new(.43f, .82f, .92f),
            new(.43f, .89f, .79f), new(.31f, .85f, .84f), new(.49f, .87f, .75f) };
        [Range(0, 1)] public float bodyFillOpacity = .34f;
        public Color ordinaryActivity = new(.68f, .74f, .77f);
        [UnityEngine.Serialization.FormerlySerializedAs("priorityActivity")]
        public Color importantActivity = new(.53f, .79f, .85f);
        [Range(0, 1)] public float activityOpacity = .60f;
        [Range(0, 1)] public float stalledActivityOpacityMultiplier = .70f;
        [Header("Uniform rail warning pulse")]
        public Color behind = new Color(.94f, .69f, .25f);
        public Color endangered = new Color(1f, .34f, .18f);
        public Color critical = new Color(1f, .12f, .25f);
        public Color exceedsNormal = new Color(1f, .28f, .57f);
        [Range(0, 1)] public float warningPulseDepth = .32f;
        [Header("Rail and activity geometry")]
        [Min(.5f)] public float railThickness = 1.7f;
        [Min(0)] public float minimumRailFeatherPixels = .75f;
        [Min(12)] public float spacingAtOneLoad = 78f;
        [Min(0)] public float densityPerLoad = .22f;
        [Min(8)] public float minimumSpacing = 22f;
        [Min(1)] public float blipLength = 9f;
        [Range(.1f, .8f)] public float blipWidthFraction = .5f;
        [Header("Departure candidates")]
        [Min(.1f)] public float completionFadeSeconds = .8f;
        [Min(.01f)] public float killFlashHalfCycle = .12f;
        [Min(.1f)] public float timeoutFadeSeconds = 1.1f;

        [Header("Gameplay danger mapping (cosmetic only)")]
        [Range(0, 1)] public float riskFloor = .3f;
        [Min(.01f)] public float rescueHeadroomFraction = .5f;
        [Min(.001f)] public float riskSoftBandFraction = .05f;
        [Min(.01f)] public float riskSoftBandSeconds = .75f;

        public float Spacing(float load) => Mathf.Max(minimumSpacing,
            spacingAtOneLoad / (1 + densityPerLoad * Mathf.Max(0, load - 1)));
        public Color IdentityColor(ConnectionDisplayState s) => IdentityColor(s.trafficClass, s.identitySlot);
        public Color IdentityColor(DisplayTrafficClass traffic, int slot)
        {
            if (traffic == DisplayTrafficClass.Ordinary && ordinaryIdentityPalette == OrdinaryIdentityPalette.Uniform)
                return uniformOrdinaryIdentity;
            Color[] family = traffic == DisplayTrafficClass.Important ? importantIdentityColors :
                ordinaryIdentityPalette == OrdinaryIdentityPalette.Grayscale ? grayscaleIdentityColors : ordinaryIdentityColors;
            return family != null && family.Length > 0 ? family[Mathf.Max(0, slot) % family.Length] : Color.gray;
        }
        public Color Body(ConnectionDisplayState s, double beats)
        { Color c = Rail(s, beats); c.a *= bodyFillOpacity; return c; }
        public Color Activity(DisplayTrafficClass traffic, bool stalled = false)
        {
            Color c = traffic == DisplayTrafficClass.Important ? importantActivity : ordinaryActivity;
            c.a *= activityOpacity * (stalled ? stalledActivityOpacityMultiplier : 1); return c;
        }
        public Color Rail(ConnectionDisplayState s, double beats)
        {
            Color baseColor = performanceColorTreatment == PerformanceColorTreatment.Uniform ? uniformPerformanceBase : IdentityColor(s);
            float danger = Mathf.Clamp01(s.dangerSeverity);
            Color warning = danger < .55f ? Color.Lerp(behind, endangered, danger / .55f) :
                Color.Lerp(endangered, critical, (danger - .55f) / .45f);
            if (s.exceedsNormalBudget) warning = Color.Lerp(warning, exceedsNormal,
                s.normalBudgetWarningMix > 0 ? s.normalBudgetWarningMix : 1);
            float pulse = .5f + .5f * Mathf.Sin((float)(beats % 1) * Mathf.PI * 2);
            Color c = Color.Lerp(baseColor, warning, danger);
            c *= 1 - warningPulseDepth * danger * (1 - pulse);
            c.a = 1;
            if (s.resolution == DisplayResolution.Killed || s.resolution == DisplayResolution.Canceled) c = critical;
            if (s.resolution == DisplayResolution.TimedOut) c = endangered;
            return c;
        }
        public float DepartureDuration(DisplayResolution resolution) => resolution == DisplayResolution.TimedOut ?
            .25f + timeoutFadeSeconds : resolution == DisplayResolution.Killed || resolution == DisplayResolution.Canceled ?
            killFlashHalfCycle * 4 + .35f : completionFadeSeconds;
        public float Opacity(ConnectionDisplayState s)
        {
            float age = Mathf.Max(0, s.resolutionAge);
            if (s.resolution == DisplayResolution.Completed || s.resolution == DisplayResolution.Withdrawn) return Mathf.Clamp01(1 - age / completionFadeSeconds);
            if (s.resolution == DisplayResolution.TimedOut) return Mathf.Clamp01(1 - Mathf.Max(0, age - .25f) / timeoutFadeSeconds);
            if (s.resolution == DisplayResolution.Killed || s.resolution == DisplayResolution.Canceled)
                return age < killFlashHalfCycle * 4 ?
                    (Mathf.FloorToInt(age / killFlashHalfCycle) % 2 == 0 ? 1 : .12f) :
                    Mathf.Clamp01(1 - (age - killFlashHalfCycle * 4) / .35f);
            return 1;
        }
    }
}
