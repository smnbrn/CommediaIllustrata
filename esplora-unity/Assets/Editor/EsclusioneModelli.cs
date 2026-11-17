using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

// Tutto ciò che sta in una cartella Resources entra nella build. Le due build (Inferno e Purgatorio) hanno modelli diversi:
// prima di costruire, i modelli dell'altra cantica (e la musica dell'Inferno, per il Purgatorio) si spostano in Assets/Esclusi
// e a fine build tornano al loro posto.
public static class EsclusioneModelli
{
    public static readonly string[] INFERNO = { "Alberi", "AlberoMorto", "Caronte", "Castello", "Caverna", "Cerbero", "Colle", "Ghiaccio", "Lucifero", "Massi", "Ponte", "Sarcofago" };
    public static readonly string[] PURGATORIO = { "AlberiCespugli", "AlberiFiori", "AngeloArgento", "AngeloMarmo", "AngeloOttone", "Arco", "Bambu", "Barca", "Candele", "Carro", "Catone", "DonnaFuse", "DonnaRuby", "Monaco", "Montagna", "Piante", "Querce", "Rocce", "Rosa", "Scala", "Serpente" };
    const string RADICE = "Assets/Resources/Modelli";
    const string ESCLUSI = "Assets/Esclusi";

    public class Spostati { public readonly List<string[]> coppie = new List<string[]>(); }

    public static Spostati Escludi(string[] modelli, bool musica)
    {
        var s = new Spostati();
        if (!AssetDatabase.IsValidFolder(ESCLUSI)) AssetDatabase.CreateFolder("Assets", "Esclusi");
        foreach (var n in modelli)
        {
            string da = RADICE + "/" + n, a = ESCLUSI + "/" + n;
            if (!AssetDatabase.IsValidFolder(da)) continue;
            string err = AssetDatabase.MoveAsset(da, a);
            if (string.IsNullOrEmpty(err)) s.coppie.Add(new[] { da, a }); else UnityEngine.Debug.LogWarning("[Esplora] Non riesco a escludere " + n + ": " + err);
        }
        if (musica && AssetDatabase.IsValidFolder("Assets/StreamingAssets/Audio"))
        {
            string err = AssetDatabase.MoveAsset("Assets/StreamingAssets/Audio", ESCLUSI + "/Audio");
            if (string.IsNullOrEmpty(err)) s.coppie.Add(new[] { "Assets/StreamingAssets/Audio", ESCLUSI + "/Audio" });
        }
        AssetDatabase.Refresh();
        return s;
    }

    public static void Ripristina(Spostati s)
    {
        foreach (var c in s.coppie)
        {
            string err = AssetDatabase.MoveAsset(c[1], c[0]);
            if (!string.IsNullOrEmpty(err)) UnityEngine.Debug.LogError("[Esplora] Non riesco a rimettere " + c[1] + ": " + err);
        }
        AssetDatabase.Refresh();
    }
}
