using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Menu "Esplora > Costruisci WebGL": compila in ../esplora (servita da GitHub Pages).
public static class CostruisciWebGL
{
    [MenuItem("Esplora/Costruisci WebGL")]
    public static void Costruisci()
    {
        string uscita = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "esplora"));

        PlayerSettings.productName = "Inferno 3D";
        PlayerSettings.companyName = "CommediaIllustrata";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;   // GitHub Pages non manda le intestazioni di compressione
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.WebGL.template = "PROJECT:Esplora";
        PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
        PlayerSettings.runInBackground = true;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;   // codice più piccolo, anche se la build è più lenta
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SplashScreen.show = false;   // il logo di avvio pesava circa 2,7 MB

        var opzioni = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Esplora.unity" },
            locationPathName = uscita,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var esclusi = EsclusioneModelli.Escludi(EsclusioneModelli.PURGATORIO, false);   // niente modelli del Purgatorio in questa build
        BuildReport r;
        try { r = BuildPipeline.BuildPlayer(opzioni); }
        finally { EsclusioneModelli.Ripristina(esclusi); }
        Debug.Log($"[Esplora] Build WebGL: {r.summary.result}, {r.summary.totalSize / 1024f / 1024f:F1} MB, {r.summary.totalTime.TotalSeconds:F0} s, errori {r.summary.totalErrors} -> {uscita}");
    }
}
