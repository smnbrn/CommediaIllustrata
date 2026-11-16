using UnityEngine;

// Carica i modelli 3D di Caronte e Cerbero (Resources/Modelli/<Nome>/<Nome>.fbx), li porta a una
// dimensione adatta alla scena e li posa su un piccolo palco. L'origine di ogni figura è sulla sua base.
// Crediti dei modelli: vedi esplora/CREDITI.md e la pagina del 3D.
public static class Figure
{
    static Shader shaderColore, shaderOmbra;
    static Material matOmbra;

    static Material MatColore(Color c)
    {
        if (shaderColore == null) shaderColore = Resources.Load<Shader>("ColoreVertici");
        var m = new Material(shaderColore);
        m.SetColor("_Color", c);
        return m;
    }

    // ombra morbida sotto la figura, sopra il palco
    static void Ombra(Transform p, float raggio)
    {
        if (matOmbra == null)
        {
            if (shaderOmbra == null) shaderOmbra = Resources.Load<Shader>("OmbraMorbida");
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(0, 0, 0, a * a));
                }
            tex.Apply();
            matOmbra = new Material(shaderOmbra) { mainTexture = tex };
        }
        var go = Forme.Crea("Ombra", Forme.Quadro());
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(0, 0.07f, 0);
        go.transform.localScale = new Vector3(raggio * 1.7f, 1f, raggio * 1.7f);
        go.GetComponent<MeshRenderer>().sharedMaterial = matOmbra;
    }

    static GameObject Palco(Transform p, float raggio, Color c)
    {
        var go = Forme.Crea("Palco", Forme.Cilindro());
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(0, -0.05f, 0);
        go.transform.localScale = new Vector3(raggio * 2f, 0.05f, raggio * 2f);
        go.GetComponent<MeshRenderer>().sharedMaterial = MatColore(c);
        return go;
    }

    // yaw: rotazione per far guardare il modello verso +Z; lunghezza: dimensione massima orizzontale
    static GameObject Carica(string nome, float lunghezza, float yaw, Color colorePalco, float raggioPalco, float oscillazione, Color alone, float fondo)
    {
        var root = new GameObject(nome).transform;
        Palco(root, raggioPalco, colorePalco);
        Ombra(root, raggioPalco);

        var prefab = Resources.Load<GameObject>($"Modelli/{nome}/{nome}");
        if (prefab == null) { Debug.LogWarning($"[Esplora] Modello '{nome}' non trovato"); return root.gameObject; }

        var perno = new GameObject("Perno").transform;      // oscilla piano; la figura resta ferma sul palco
        perno.SetParent(root, false);
        var modello = Object.Instantiate(prefab, perno).transform;
        // luce di carattere: l'alone sui bordi e la luce di fondo cambiano da figura a figura (fredda per Caronte, calda per Cerbero, di ghiaccio per Lucifero)
        foreach (var r in modello.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials) { if (m.HasProperty("_Alone")) m.SetColor("_Alone", alone); if (m.HasProperty("_Fondo")) m.SetFloat("_Fondo", fondo); }
        modello.localRotation = Quaternion.Euler(0, yaw, 0);

        // dimensione e posizione: base al centro dell'origine
        Bounds B() { var rs = modello.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
        var b0 = B();
        modello.localScale = Vector3.one * (lunghezza / Mathf.Max(b0.size.x, b0.size.z, 0.0001f));
        var b1 = B();
        modello.position += new Vector3(-b1.center.x, -b1.min.y, -b1.center.z) + root.position;

        if (oscillazione > 0f)
        {
            var o = perno.gameObject.AddComponent<Oscilla>();
            o.asse = Vector3.forward; o.ampiezza = oscillazione; o.velocita = 0.9f; o.su = 0.06f;
        }
        return root.gameObject;
    }

    public static GameObject Caronte() { return Carica("Caronte", 7.5f, 0f, new Color(0.07f, 0.20f, 0.26f), 5.5f, 1.8f, new Color(0.25f, 0.75f, 0.85f), 0.5f); }
    public static GameObject Cerbero() { return Carica("Cerbero", 6.5f, 0f, new Color(0.22f, 0.20f, 0.09f), 5f, 0.8f, new Color(1f, 0.55f, 0.15f), 0.5f); }
    public static GameObject Lucifero() { return Carica("Lucifero", 5.2f, 0f, new Color(0.62f, 0.78f, 0.88f), 5.2f, 0f, new Color(0.45f, 0.7f, 1f), 0.6f); }
}
