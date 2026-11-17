using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Menu "Esplora > Prepara figure": per ogni modello in Assets/Resources/Modelli/<Nome>/<Nome>.fbx
// crea un materiale per ogni texture in <Nome>/textures (shader Esplora/TexturaLuce) e lo associa
// ai materiali del modello con lo stesso nome. Così nella build non entra lo shader Standard.
public static class PreparaFigure
{
    const string RADICE = "Assets/Resources/Modelli";
    static readonly List<string> VEGETAZIONE = new List<string> { "AlberiCespugli", "AlberiFiori", "Querce", "Piante", "Rocce", "Bambu", "Rosa", "Montagna", "Arco", "Carro", "Candele" };

    [MenuItem("Esplora/Prepara figure")]
    public static void Prepara()
    {
        var shader = Shader.Find("Esplora/TexturaLuce");
        if (shader == null) { Debug.LogError("[Esplora] Shader Esplora/TexturaLuce non trovato"); return; }

        foreach (var cartella in AssetDatabase.GetSubFolders(RADICE))
        {
            string nome = Path.GetFileName(cartella);
            string fbx = $"{cartella}/{nome}.fbx";
            if (!File.Exists(fbx)) continue;

            // materiali: uno per texture
            string cartellaMat = cartella + "/Materiali";
            if (!AssetDatabase.IsValidFolder(cartellaMat)) AssetDatabase.CreateFolder(cartella, "Materiali");
            var perNome = new Dictionary<string, Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { cartella + "/textures" }))
            {
                string pt = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(pt);
                // texture più leggere: i dettagli piccoli (occhi, denti, lingua) a 256-512, il resto a 1024, tutte compresse in crunch
                long peso = new FileInfo(pt).Length;
                int lato = peso < 40000 ? 128 : peso < 150000 ? 512 : 1024;
                if (VEGETAZIONE.Contains(nome)) lato = Mathf.Min(lato, 512);   // piante e rocce si vedono da lontano: texture più piccole
                if (imp.maxTextureSize != lato || !imp.crunchedCompression || imp.compressionQuality != 55 || imp.mipmapEnabled == false)
                {
                    imp.maxTextureSize = lato;
                    imp.mipmapEnabled = true;
                    imp.textureCompression = TextureImporterCompression.Compressed;
                    imp.crunchedCompression = true; imp.compressionQuality = 55;
                    imp.SaveAndReimport();
                }
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pt);
                string mn = Path.GetFileNameWithoutExtension(pt);
                string pm = $"{cartellaMat}/{mn}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(pm);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, pm); }
                m.shader = shader; m.SetTexture("_MainTex", tex);
                EditorUtility.SetDirty(m);
                perNome[mn] = m;
            }
            AssetDatabase.SaveAssets();

            // associa ai materiali del modello (per nome)
            var importer = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.meshCompression = ModelImporterMeshCompression.Medium;       // vertici quantizzati: file più piccoli
            importer.importTangents = ModelImporterTangents.None;                 // gli shader non usano le tangenti
            importer.importBlendShapes = false; importer.importCameras = false; importer.importLights = false;
            importer.optimizeMeshPolygons = true; importer.optimizeMeshVertices = true;
            importer.isReadable = true;   // gli alberi, i massi, le tombe ecc. si uniscono in una sola mesh a runtime
            importer.SaveAndReimport();
            int collegati = 0, senza = 0;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
            {
                if (!(o is Material orig)) continue;
                if (perNome.TryGetValue(orig.name, out var mio))
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), orig.name), mio);
                    collegati++;
                }
                else { senza++; Debug.LogWarning($"[Esplora] {nome}: materiale '{orig.name}' senza texture"); }
            }
            importer.SaveAndReimport();
            Debug.Log($"[Esplora] {nome}: {collegati} materiali collegati, {senza} senza texture");
        }
    }
}
