using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    public static class PaletteExperimentWitness
    {
        public const string Evidence="Docs/Review/ConnectionsBusinessImportanceV0/IdentityMarker";
        private static readonly List<string> checks=new();
        private static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);if(!ok)throw new Exception(label);}
        [MenuItem("Tools/Connections/Run Identity Presentation Witness")]
        public static void Run()
        {
            checks.Clear();Directory.CreateDirectory(Evidence);
            var source=AssetDatabase.LoadAssetAtPath<ConnectionDisplayStyle>(ConnectionDisplayLabAuthoring.StylePath);var style=UnityEngine.Object.Instantiate(source);
            Color[] historical={new(.36f,.87f,.91f),new(.35f,.91f,.76f),new(.43f,.82f,.92f),new(.43f,.89f,.79f),new(.31f,.85f,.84f),new(.49f,.87f,.75f)};
            try
            {
                Check(style.identityColors.SequenceEqual(historical),"single exact historical palette");
                foreach(PerformanceColorTreatment treatment in Enum.GetValues(typeof(PerformanceColorTreatment)))
                {
                    style.performanceColorTreatment=treatment;
                    foreach(float danger in new[]{0f,.001f,.35f,.8f,1f})
                    for(int slot=0;slot<6;slot++)
                    {
                        var ordinary=new ConnectionDisplayState{identity="a0",identitySlot=slot,dangerSeverity=danger,productiveSpeed=.35f};
                        var important=ordinary;important.trafficClass=DisplayTrafficClass.Important;
                        Check(style.IdentityColor(ordinary)==historical[slot]&&style.IdentityColor(important)==historical[slot],"importance independent identity / "+slot);
                        Check(style.Rail(ordinary,.25)==style.Rail(important,.25),"importance independent performance / "+treatment+" / "+danger+" / "+slot);
                        if(danger==0)Check(style.Rail(ordinary,.25)==historical[slot],"healthy rails retain identity / "+treatment+" / "+slot);
                        else if(treatment==PerformanceColorTreatment.Uniform)
                        {var other=ordinary;other.identitySlot=(slot+1)%6;Check(style.Rail(ordinary,.25)==style.Rail(other,.25),"uniform warnings independent of identity / "+danger+" / "+slot);}
                        var stopped=ordinary;stopped.productiveSpeed=0;Check(style.Rail(stopped,.25)==style.Rail(ordinary,.25),"movement independent of warning color");
                    }
                    var sample=new ConnectionDisplayState{identitySlot=0,dangerSeverity=.35f};var actual=style.Rail(sample,.25);
                    var expected=treatment==PerformanceColorTreatment.Blended?new Color(.5763636f,.7290455f,.6634091f,1):new Color(.9781818f,.4672727f,.2054545f,1);
                    Check(Vector4.Distance(actual,expected)<.0001f,"exact preserved blend / warm warning sample: "+treatment);
                }
                foreach(string path in new[]{"Assets/Prefabs/Connections/Display/DisplayIdentityPill.prefab","Assets/Prefabs/Connections/IdentityPill.prefab"})
                {
                    var canvas=new GameObject("Marker witness Canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                    var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),canvas.transform,false);Canvas.ForceUpdateCanvases();
                    try
                    {
                        var pill=go.GetComponent<IdentityPillView>();var text=(TMP_Text)new SerializedObject(pill).FindProperty("idText").objectReferenceValue;var size=pill.Size;
                        Check(text.font.HasCharacter('!'),path+" authored font supports !");
                        foreach(float progress in new[]{0f,.25f,.5f,.75f,1f})
                        {
                            pill.Bind("a0",historical[0]);pill.SetImportant(true);
                            if(pill.UsesSharedDisplay)pill.SetDisplayState(new ConnectionDisplayState{identity="a0",trafficClass=DisplayTrafficClass.Important,workProgress=progress});
                            Canvas.ForceUpdateCanvases();text.ForceMeshUpdate(true,true);
                            Check(pill.DisplayedId=="a0"&&text.GetParsedText()=="a0!"&&pill.Size==size,path+" marker independent from ID and packing / "+progress+" / raw="+pill.DisplayedId+" / parsed="+text.GetParsedText()+" / size="+pill.Size);
                            Check(text.GetPreferredValues(text.text).x<=text.rectTransform.rect.width+.1f,path+" marker fits existing width / "+progress);
                            Check(text.color.r>=.85f&&text.color.g>=.95f&&text.color.b>=.9f,path+" bright text / "+progress);
                            if(pill.UsesSharedDisplay)Check(pill.ProgressPlate.progress==progress,path+" marker preserves actual WORK fill / "+progress);
                            pill.SetDisplayedId("b2");Canvas.ForceUpdateCanvases();text.ForceMeshUpdate(true,true);Check(pill.DisplayedId=="b2"&&text.GetParsedText()=="b2!",path+" alias preserves marker without changing raw ID");
                            pill.SetImportant(false);Canvas.ForceUpdateCanvases();text.ForceMeshUpdate(true,true);Check(text.GetParsedText()=="b2",path+" ordinary identity has no marker");
                        }
                    }
                    finally{UnityEngine.Object.DestroyImmediate(canvas.gameObject);}
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(style);File.WriteAllLines(Evidence+"/deterministic-checks.txt",checks);}
            Debug.Log("IdentityPresentationWitness PASS: "+checks.Count+" assertions.");
        }
        public static void Regressions()
        {
            Run();BusinessImportanceWitness.Run();ConnectionDisplayLabWitness.Run();ConnectionVisualIntegrationWitness.Run();
            RecorderPolicyEvidenceWitness.Regressions();LogsOutputWitness.Run();
        }
    }
}
