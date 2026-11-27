using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    public static class PaletteExperimentWitness
    {
        public const string Evidence = "Docs/Review/ConnectionsBusinessImportanceV0/PaletteExperiment";
        private static readonly List<string> checks = new();
        private static void Check(bool ok, string label) { checks.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) throw new Exception(label); }
        private static bool Near(Color a, Color b) => Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)+Mathf.Abs(a.a-b.a) < .0001f;
        [MenuItem("Tools/Connections/Run Palette Experiment Witness")]
        public static void Run()
        {
            checks.Clear(); Directory.CreateDirectory(Evidence);
            var authored = AssetDatabase.LoadAssetAtPath<ConnectionDisplayStyle>(ConnectionDisplayLabAuthoring.StylePath);
            var style = UnityEngine.Object.Instantiate(authored);
            try
            {
                Check(authored.ordinaryIdentityPalette == OrdinaryIdentityPalette.MutedHues && authored.performanceColorTreatment == PerformanceColorTreatment.Uniform, "authored Muted Hues + Uniform Performance defaults");
                var important = (Color[])style.importantIdentityColors.Clone();
                var ordinary = (Color[])style.ordinaryIdentityColors.Clone();
                foreach (OrdinaryIdentityPalette palette in Enum.GetValues(typeof(OrdinaryIdentityPalette)))
                foreach (PerformanceColorTreatment treatment in Enum.GetValues(typeof(PerformanceColorTreatment)))
                {
                    style.ordinaryIdentityPalette = palette; style.performanceColorTreatment = treatment;
                    var colors = Enumerable.Range(0,6).Select(i=>style.IdentityColor(DisplayTrafficClass.Ordinary,i)).ToArray();
                    Check(palette == OrdinaryIdentityPalette.Uniform ? colors.Distinct().Count()==1 : colors.Distinct().Count()==6, palette+" identity slot cardinality / "+treatment);
                    if (palette == OrdinaryIdentityPalette.MutedHues) Check(colors.SequenceEqual(ordinary), "muted hues exactly preserved / "+treatment);
                    if (palette == OrdinaryIdentityPalette.Grayscale) Check(colors.All(c=>c.r==c.g && c.g==c.b && c.r>=.53f && c.r<=.93f), "distinct achromatic high-contrast grays / "+treatment);
                    foreach (float speed in new[] { 0f,.35f,1f })
                    foreach (float danger in new[] { 0f,.35f,.8f,1f })
                    {
                        var reference = new ConnectionDisplayState { operation=OperationKind.Write, requestedLoad=6, productiveSpeed=speed, workProgress=.4f, dangerSeverity=danger };
                        var shared = style.Rail(reference,.25);
                        foreach (DisplayTrafficClass family in Enum.GetValues(typeof(DisplayTrafficClass)))
                        for (int slot=0;slot<6;slot++)
                        {
                            var s = reference; s.identitySlot=slot; s.trafficClass=family;
                            Color identity=style.IdentityColor(s);
                            Check(family!=DisplayTrafficClass.Important || identity==important[slot], "IMPORTANT family unaffected / "+palette+" / "+treatment+" / "+speed+" / "+danger+" / "+slot);
                            Check(treatment!=PerformanceColorTreatment.Uniform || style.Rail(s,.25)==shared, "uniform rail independent of family/slot / "+palette+" / "+speed+" / "+danger+" / "+slot);
                            var stopped=s; stopped.productiveSpeed=0; Check(style.Rail(s,.25)==style.Rail(stopped,.25) && style.IdentityColor(stopped)==identity, "activity speed does not recolor identity or alter independent danger");
                        }
                    }
                }
                style.ordinaryIdentityPalette=OrdinaryIdentityPalette.MutedHues; style.performanceColorTreatment=PerformanceColorTreatment.Blended;
                var historical = new ConnectionDisplayState { dangerSeverity=.35f };
                Check(Near(style.Rail(historical,.25),new Color(.7453636f,.6250455f,.5789091f,1)), "Blended reproduces pre-experiment ordinary rail sample exactly");
                historical.trafficClass=DisplayTrafficClass.Important;
                Check(Near(style.Rail(historical,.25),new Color(.5763636f,.7290455f,.6634091f,1)), "Blended reproduces pre-experiment IMPORTANT rail sample exactly");
                foreach (PerformanceColorTreatment treatment in Enum.GetValues(typeof(PerformanceColorTreatment)))
                {
                    style.performanceColorTreatment=treatment; historical.resolution=DisplayResolution.Killed;
                    Check(style.Rail(historical,.2)==style.critical, "unchanged killed departure target / "+treatment);
                    historical.resolution=DisplayResolution.TimedOut; Check(style.Rail(historical,.2)==style.endangered, "unchanged timeout departure target / "+treatment);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(style); File.WriteAllLines(Evidence+"/deterministic-checks.txt", checks); }
            Debug.Log("PaletteExperimentWitness PASS: "+checks.Count+" assertions.");
        }
        public static void Regressions()
        {
            Run(); BusinessImportanceWitness.Run(); ConnectionDisplayLabWitness.Run(); ConnectionVisualIntegrationWitness.Run();
            RecorderPolicyEvidenceWitness.Regressions();
        }
    }
}
