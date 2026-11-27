using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Uptime.Connections.Display;

namespace Uptime.Connections.EditorTools
{
    [InitializeOnLoad] public static class PaletteExperimentCapture
    {
        private const string Pending="Uptime.PaletteExperimentCapture";
        static PaletteExperimentCapture() => EditorApplication.update += Launch;
        public static void Run() { EditorSceneManager.OpenScene(CanonicalSceneAuthoring.Scenes[0]); SessionState.SetBool(Pending,true); EditorApplication.isPlaying=true; }
        private static void Launch()
        { if (!Application.isPlaying || !SessionState.GetBool(Pending,false)) return; SessionState.SetBool(Pending,false); new GameObject("Palette comparison evidence").AddComponent<PaletteExperimentCaptureRunner>(); }
    }
    public sealed class PaletteExperimentCaptureRunner : MonoBehaviour
    {
        private readonly List<string> checks=new();
        private ConnectionDisplayStyle style;
        private OrdinaryIdentityPalette originalIdentity;
        private PerformanceColorTreatment originalPerformance;
        private void Check(bool ok,string label) { checks.Add((ok?"PASS ":"FAIL ")+label); if(!ok)throw new Exception(label); }
        private IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject); Application.runInBackground=true; Application.targetFrameRate=60; QualitySettings.vSyncCount=0;
            style=AssetDatabase.LoadAssetAtPath<ConnectionDisplayStyle>(ConnectionDisplayLabAuthoring.StylePath);
            originalIdentity=style.ordinaryIdentityPalette; originalPerformance=style.performanceColorTreatment;
            Exception failure=null; var flow=Capture();
            while(true) { bool next; try{next=flow.MoveNext();}catch(Exception e){failure=e;break;} if(!next)break;yield return flow.Current; }
            (flow as IDisposable)?.Dispose(); style.ordinaryIdentityPalette=originalIdentity; style.performanceColorTreatment=originalPerformance; Time.timeScale=1;
            Directory.CreateDirectory(PaletteExperimentWitness.Evidence); File.WriteAllLines(PaletteExperimentWitness.Evidence+"/graphical-checks.txt",checks);
            if(failure!=null)Debug.LogException(failure);else Debug.Log("PaletteExperimentCapture PASS: "+checks.Count+" actual Play Mode checks.");
            if(Application.isBatchMode)EditorApplication.Exit(failure==null?0:1);else EditorApplication.isPlaying=false;
        }
        private IEnumerator Capture()
        {
            foreach(string scene in CanonicalSceneAuthoring.Scenes)
            {
                EditorSceneManager.LoadSceneInPlayMode(scene,new LoadSceneParameters(LoadSceneMode.Single));yield return null;yield return null;
                var h=FindFirstObjectByType<SimulationHost>(); BusinessImportanceWitness.ConfigureComparison(h);
                foreach(bool important in new[]{false,true})
                {
                    var plan=BusinessImportanceWitness.Plan(h,important?"n1":"n0",important,new[]{new OperationStage(OperationKind.Write,1000,2,"Nominal AUTH",intendedDestination:h.FindNodeResources("AUTH").View)});
                    h.RegisterConnection(LogicalConnectionDefinition.FromPlan(plan));
                }
                Time.timeScale=1;
                var terminal=FindFirstObjectByType<Terminal>();var input=(TMP_InputField)new SerializedObject(terminal).FindProperty("commandInput").objectReferenceValue;
                input.onSubmit.Invoke("inspect o0 i0");yield return null;
                input.onSubmit.Invoke("load DB");yield return null;
                input.onSubmit.Invoke("work DB");yield return null;
                float until=Time.realtimeSinceStartup+150;var importantState=h.FindConnection("i0");
                while((!importantState.HasEstablished || importantState.CurrentOperationIndex<1 || importantState.IsTraversing || importantState.ServiceRemainingSeconds>12) && Time.realtimeSinceStartup<until)yield return null;
                Check(!importantState.IsComplete && importantState.IsDemanding && importantState.CurrentOperationIndex==1,scene+" real mixed-route traffic reaches service danger");
                Check(h.FindConnection("n0").Performance==LoadPerformance.AtSpeed && h.FindConnection("o0").Performance==LoadPerformance.Slowed,scene+" nominal AUTH and slowed DB coexist");
                input.onSubmit.Invoke("inspect o0 i0");yield return new WaitForSecondsRealtime(.25f);Time.timeScale=0;
                var canvas=terminal.GetComponentInParent<Canvas>();var camera=new GameObject("Palette evidence camera",typeof(Camera)).GetComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-100);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.065f,.08f);
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
                var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
                var states=h.RuntimeConnections.ToArray();var ids=FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).Select(v=>v.GetInstanceID()).OrderBy(i=>i).ToArray();
                float work=importantState.CompletedWork, time=h.SimulationTimeSeconds;var route=importantState.Route;
                foreach(OrdinaryIdentityPalette palette in Enum.GetValues(typeof(OrdinaryIdentityPalette)))
                foreach(PerformanceColorTreatment treatment in Enum.GetValues(typeof(PerformanceColorTreatment)))
                {
                    style.ordinaryIdentityPalette=palette;style.performanceColorTreatment=treatment;
                    yield return new WaitForSecondsRealtime(.3f);yield return null;
                    string name=h.gameObject.scene.name+"-"+palette+"-"+treatment;
                    CheckColors(h,name);Check(FindObjectsByType<LoadOutput>(FindObjectsSortMode.None).Length>0 && FindObjectsByType<WorkOutput>(FindObjectsSortMode.None).Length>0,name+" actual LOAD and WORK surfaces present");
                    Check(ids.SequenceEqual(FindObjectsByType<LogicalPathView>(FindObjectsSortMode.None).Select(v=>v.GetInstanceID()).OrderBy(i=>i)) && h.RuntimeConnections.SequenceEqual(states),name+" mode switch retains all view/model instances");
                    Check(importantState.CompletedWork==work && h.SimulationTimeSeconds==time && ReferenceEquals(route,importantState.Route),name+" changing color advances no WORK/time/route");
                    Save(camera,target,name);
                }
                // A resolved inspect card collapses and disables its owning output. Its pill still follows live style.
                input.onSubmit.Invoke("inspect o1");yield return null;h.KillConnection("o1");yield return new WaitForSecondsRealtime(4.5f);
                Check(FindObjectsByType<InspectOutput>(FindObjectsSortMode.None).Any(o=>!o.enabled),scene+" frozen resolved inspect history retained");
                foreach(OrdinaryIdentityPalette palette in Enum.GetValues(typeof(OrdinaryIdentityPalette)))
                { style.ordinaryIdentityPalette=palette;yield return null;yield return null;CheckColors(h,scene+" history / "+palette); }
                // Also switch while the ordinary host is running, not only while the comparison clock is paused.
                Time.timeScale=1;float before=h.SimulationTimeSeconds;style.ordinaryIdentityPalette=OrdinaryIdentityPalette.Uniform;yield return new WaitForSecondsRealtime(.2f);
                Check(h.SimulationTimeSeconds>before && ReferenceEquals(h.FindConnection("i0"),importantState),scene+" live switch needs no restart while host continues");
                camera.targetTexture=null;Destroy(target);Destroy(camera.gameObject);
            }
            EditorSceneManager.LoadSceneInPlayMode(ConnectionDisplayLabAuthoring.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));yield return null;
            var lab=FindFirstObjectByType<ConnectionDisplayLab>();lab.heartbeat.paused=true;lab.heartbeat.SetReviewTime(8);lab.RenderNow();
            var labCanvas=lab.GetComponent<Canvas>();var labCamera=FindFirstObjectByType<Camera>();labCanvas.renderMode=RenderMode.ScreenSpaceCamera;labCanvas.worldCamera=labCamera;labCanvas.planeDistance=10;labCamera.cullingMask=~0;
            var labTarget=new RenderTexture(1280,720,24);labCamera.targetTexture=labTarget;
            var views=lab.samples.Select(s=>s.view.GetInstanceID()).ToArray();
            foreach(OrdinaryIdentityPalette palette in Enum.GetValues(typeof(OrdinaryIdentityPalette)))
            foreach(PerformanceColorTreatment treatment in Enum.GetValues(typeof(PerformanceColorTreatment)))
            {
                style.ordinaryIdentityPalette=palette;style.performanceColorTreatment=treatment;lab.RenderNow();yield return null;yield return null;
                Check(views.SequenceEqual(lab.samples.Select(s=>s.view.GetInstanceID())),"Lab same sample views / "+palette+" / "+treatment);
                foreach(var sample in lab.samples)
                {
                    var actual=sample.view.pill.color;var expected=style.IdentityColor(sample.Current);
                    Check(Mathf.Abs(actual.r-expected.r)+Mathf.Abs(actual.g-expected.g)+Mathf.Abs(actual.b-expected.b)<.0001f,"Lab live identity / "+sample.Current.identity+" / "+palette+" / "+treatment);
                }
                Save(labCamera,labTarget,"Lab-"+palette+"-"+treatment);
            }
            labCamera.targetTexture=null;Destroy(labTarget);
        }
        private void CheckColors(SimulationHost h,string label)
        {
            int count=0;
            foreach(var pill in FindObjectsByType<IdentityPillView>(FindObjectsSortMode.None))
            {
                var state=h.FindConnection(pill.DisplayedId);if(state==null || !h.PresentationPalette.TryGet(state,out var expected))continue;
                var field=pill.UsesSharedDisplay?"displayIdentityColor":"baseAccent";
                var actual=(Color)typeof(IdentityPillView).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(pill);
                Check(actual==expected,label+" / "+pill.DisplayedId+" / "+pill.transform.parent.name);count++;
            }
            Check(count>=6,label+" meaningful live/frozen terminal/network coverage");
        }
        private void Save(Camera camera,RenderTexture target,string name)
        {
            Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var pic=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pic.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pic.Apply();string dir=PaletteExperimentWitness.Evidence+"/Captures";Directory.CreateDirectory(dir);File.WriteAllBytes(dir+"/"+name+".png",pic.EncodeToPNG());RenderTexture.active=old;Destroy(pic);
        }
    }
}
