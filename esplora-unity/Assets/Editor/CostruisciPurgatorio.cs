using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu "Esplora > Crea scena Purgatorio" e "Esplora > Costruisci WebGL Purgatorio" (scrive in ../esplora-purgatorio).
public static class CostruisciPurgatorio
{
    const string SCENA = "Assets/Scenes/Purgatorio.unity";

    [MenuItem("Esplora/Crea scena Purgatorio")]
    public static void CreaScena()
    {
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera") { tag = "MainCamera" };
        cam.AddComponent<Camera>();
        cam.AddComponent<AudioListener>();
        var e = new GameObject("Esplora");
        e.AddComponent<EsploraPurgatorio>();
        EditorSceneManager.SaveScene(sc, SCENA);
        Debug.Log("[Esplora] Scena Purgatorio creata: " + SCENA);
    }

    [MenuItem("Esplora/Costruisci WebGL Purgatorio")]
    public static void Costruisci()
    {
        if (!File.Exists(SCENA)) CreaScena();
        string uscita = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "esplora-purgatorio"));
        PlayerSettings.productName = "Purgatorio 3D";
        PlayerSettings.companyName = "CommediaIllustrata";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.template = "PROJECT:Purgatorio";
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.runInBackground = true;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SplashScreen.show = false;

        var opzioni = new BuildPlayerOptions { scenes = new[] { SCENA }, locationPathName = uscita, target = BuildTarget.WebGL, options = BuildOptions.None };
        var esclusi = EsclusioneModelli.Escludi(EsclusioneModelli.INFERNO, true);   // niente modelli e musica dell'Inferno in questa build
        BuildReport r;
        try { r = BuildPipeline.BuildPlayer(opzioni); }
        finally { EsclusioneModelli.Ripristina(esclusi); }
        var sa = Path.Combine(uscita, "StreamingAssets"); if (Directory.Exists(sa)) Directory.Delete(sa, true);
        Debug.Log($"[Esplora] Build WebGL Purgatorio: {r.summary.result}, {r.summary.totalSize / 1024f / 1024f:F1} MB, {r.summary.totalTime.TotalSeconds:F0} s, errori {r.summary.totalErrors} -> {uscita}");
    }
}
