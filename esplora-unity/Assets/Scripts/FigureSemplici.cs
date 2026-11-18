using System.Collections.Generic;
using UnityEngine;

// Costruttore di mesh a facce piatte con una matrice corrente: serve alle figure disegnate dal codice
// (Catone, le anime penitenti), tutte unite in una sola mesh per tenere pochi i draw call.
public class FacceM
{
    readonly List<Vector3> v = new List<Vector3>();
    readonly List<Color> c = new List<Color>();
    readonly List<int> t = new List<int>();
    public Matrix4x4 M = Matrix4x4.identity;
    static readonly Vector3 LUCE = new Vector3(0.45f, 0.8f, -0.4f).normalized;

    // triangolo con luce finta: la faccia si scurisce se guarda lontano dalla luce; 'verso' è un punto interno per orientare la normale
    void Tri(Vector3 a, Vector3 b, Vector3 d, Color col, Vector3 interno)
    {
        a = M.MultiplyPoint3x4(a); b = M.MultiplyPoint3x4(b); d = M.MultiplyPoint3x4(d); interno = M.MultiplyPoint3x4(interno);
        var n = Vector3.Cross(b - a, d - a).normalized;
        if (Vector3.Dot(n, (a + b + d) / 3f - interno) < 0f) { var tmp = b; b = d; d = tmp; n = -n; }
        float f = 0.62f + 0.4f * Mathf.Clamp01(Vector3.Dot(n, LUCE) * 0.5f + 0.5f);
        var cc = new Color(col.r * f, col.g * f, col.b * f, 1f);
        int k = v.Count;
        v.Add(a); v.Add(b); v.Add(d); c.Add(cc); c.Add(cc); c.Add(cc);
        t.Add(k); t.Add(k + 1); t.Add(k + 2);
    }

    // tronco di cono (base in basso a 'piede', asse verticale in coordinate locali), 'lati' facce
    public void Tronco(Vector3 piede, float rBase, float rCima, float h, int lati, Color col)
    {
        Vector3 interno = piede + Vector3.up * h * 0.5f;
        for (int i = 0; i < lati; i++)
        {
            float a0 = i / (float)lati * Mathf.PI * 2f, a1 = (i + 1) / (float)lati * Mathf.PI * 2f;
            var b0 = piede + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rBase; var b1 = piede + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rBase;
            var t0 = piede + Vector3.up * h + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rCima; var t1 = piede + Vector3.up * h + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rCima;
            Tri(b0, b1, t1, col, interno); Tri(b0, t1, t0, col, interno);
            if (rCima > 0.001f) Tri(piede + Vector3.up * h, t0, t1, col, interno);
        }
    }

    public void Sfera(Vector3 centro, Vector3 raggi, Color col, int fasce = 4, int spicchi = 7)
    {
        for (int i = 0; i < fasce; i++)
        {
            float p0 = Mathf.PI * i / fasce, p1 = Mathf.PI * (i + 1) / fasce;
            for (int j = 0; j < spicchi; j++)
            {
                float q0 = Mathf.PI * 2f * j / spicchi, q1 = Mathf.PI * 2f * (j + 1) / spicchi;
                Vector3 P(float p, float q) { return centro + new Vector3(Mathf.Sin(p) * Mathf.Cos(q) * raggi.x, Mathf.Cos(p) * raggi.y, Mathf.Sin(p) * Mathf.Sin(q) * raggi.z); }
                Tri(P(p0, q0), P(p0, q1), P(p1, q1), col, centro); Tri(P(p0, q0), P(p1, q1), P(p1, q0), col, centro);
            }
        }
    }

    // scatola con asse locale
    public void Scatola(Vector3 centro, Vector3 dim, Color col)
    {
        Vector3 e = dim * 0.5f;
        var p = new Vector3[8];
        for (int i = 0; i < 8; i++) p[i] = centro + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
        int[][] f = { new[] {0,1,3,2}, new[] {4,5,7,6}, new[] {0,4,6,2}, new[] {1,5,7,3}, new[] {2,3,7,6}, new[] {0,1,5,4} };
        foreach (var q in f) { Tri(p[q[0]], p[q[1]], p[q[2]], col, centro); Tri(p[q[0]], p[q[2]], p[q[3]], col, centro); }
    }

    public Mesh Costruisci(string nome)
    {
        var m = new Mesh { name = nome, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
    public bool Vuota { get { return v.Count == 0; } }
}

public static class FigureSemplici
{
    // stato: 0 in piedi, 1 curvo sotto un masso, 2 seduto, 3 prono, 4 in cammino (inclinato)
    public static void Penitente(FacceM f, Vector3 pos, float yawGradi, Color veste, int stato, float scala = 1f)
    {
        var rot = Quaternion.Euler(0f, yawGradi, 0f);
        Matrix4x4 baseM = Matrix4x4.TRS(pos, rot, Vector3.one * scala);
        Color pelle = new Color(0.82f, 0.68f, 0.56f);
        Color scura = veste * 0.7f; scura.a = 1f;
        Matrix4x4 corpo = baseM;
        switch (stato)
        {
            case 1: corpo = baseM * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(38f, 0f, 0f), Vector3.one); break;
            case 3: corpo = baseM * Matrix4x4.TRS(new Vector3(0f, 0.28f, -0.2f), Quaternion.Euler(86f, 0f, 0f), Vector3.one); break;
            case 4: corpo = baseM * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(10f, 0f, 0f), Vector3.one); break;
        }
        float h = stato == 2 ? 0.8f : 1.35f;
        f.M = corpo;
        f.Tronco(Vector3.zero, stato == 2 ? 0.6f : 0.5f, 0.2f, h, 8, veste);                       // la veste
        f.Sfera(new Vector3(0f, h + 0.18f, 0.02f), new Vector3(0.26f, 0.27f, 0.26f), scura);       // il cappuccio
        f.Sfera(new Vector3(0f, h + 0.16f, 0.15f), new Vector3(0.13f, 0.15f, 0.1f), pelle, 3, 6);  // il viso
        f.Tronco(new Vector3(0f, h + 0.36f, -0.04f), 0.2f, 0.0f, 0.32f, 6, scura);                  // la punta del cappuccio
        // braccia davanti
        f.Tronco(new Vector3(-0.28f, h * 0.55f, 0.1f), 0.07f, 0.06f, 0.32f, 5, veste);
        f.Tronco(new Vector3(0.28f, h * 0.55f, 0.1f), 0.07f, 0.06f, 0.32f, 5, veste);
        if (stato == 1)
        {
            // il masso sulla schiena
            f.M = corpo;
            f.Sfera(new Vector3(0f, h * 0.95f, -0.3f), new Vector3(0.42f, 0.34f, 0.42f), new Color(0.55f, 0.55f, 0.58f), 3, 6);
        }
        f.M = Matrix4x4.identity;
    }

    // Catone: l'anziano custode dell'Antipurgatorio, con la lunga barba bianca e la veste chiara
    public static void Catone(FacceM f, Vector3 pos, float yawGradi, float scala = 1f)
    {
        f.M = Matrix4x4.TRS(pos, Quaternion.Euler(0f, yawGradi, 0f), Vector3.one * scala);
        Color veste = new Color(0.86f, 0.82f, 0.70f), bianco = new Color(0.95f, 0.95f, 0.93f), pelle = new Color(0.82f, 0.68f, 0.56f);
        f.Tronco(Vector3.zero, 0.62f, 0.3f, 1.55f, 9, veste);                                       // veste lunga
        f.Tronco(new Vector3(0f, 1.2f, 0f), 0.42f, 0.34f, 0.3f, 9, new Color(0.7f, 0.6f, 0.42f));   // mantello sulle spalle
        f.Sfera(new Vector3(0f, 1.82f, 0f), new Vector3(0.24f, 0.26f, 0.24f), pelle);                 // testa
        f.Sfera(new Vector3(0f, 1.93f, -0.02f), new Vector3(0.26f, 0.2f, 0.26f), bianco, 3, 7);       // capelli bianchi
        f.Tronco(new Vector3(0f, 1.46f, 0.2f), 0.2f, 0.02f, 0.5f, 7, bianco);                          // la barba
        f.Tronco(new Vector3(0.5f, 0f, 0.15f), 0.05f, 0.04f, 1.9f, 5, new Color(0.35f, 0.24f, 0.14f)); // il bastone
        f.Tronco(new Vector3(0.3f, 1.05f, 0.1f), 0.08f, 0.06f, 0.4f, 5, veste);                        // il braccio sul bastone
        f.M = Matrix4x4.identity;
    }
}

// Carica un modello (Resources/Modelli/<Nome>/<Nome>.fbx), lo porta all'altezza voluta e lo posa con la base sul punto dato.
public static class Props
{
    public static GameObject Metti(string nome, Transform padre, Vector3 base_, float yawGradi, float dimensione, bool perAltezza = true,
                                   Color? alone = null, float fondo = 0.55f, Color? tinta = null, bool pieno = false)
    {
        var pf = Resources.Load<GameObject>($"Modelli/{nome}/{nome}");
        if (pf == null) { Debug.LogWarning($"[Esplora] Modello '{nome}' non trovato"); return null; }
        var go = Object.Instantiate(pf, padre);
        go.name = nome;
        go.transform.rotation = Quaternion.Euler(0f, yawGradi, 0f);
        Bounds B() { var rs = go.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
        var b0 = B();
        float k = perAltezza ? dimensione / Mathf.Max(b0.size.y, 0.0001f) : dimensione / Mathf.Max(b0.size.x, b0.size.z, 0.0001f);
        go.transform.localScale = Vector3.one * k;
        var b1 = B();
        go.transform.position += new Vector3(base_.x - b1.center.x, base_.y - b1.min.y, base_.z - b1.center.z);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
            {
                if (alone.HasValue && m.HasProperty("_Alone")) m.SetColor("_Alone", alone.Value);
                if (m.HasProperty("_Fondo")) m.SetFloat("_Fondo", fondo);
                if (pieno && m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.001f);
                if (tinta.HasValue && m.HasProperty("_Tint")) m.SetColor("_Tint", tinta.Value);
            }
        return go;
    }

    // adatta il modello a una scatola (larghezza, profondità, altezza) con scale diverse per asse e lo posa con la base sul punto dato.
    // larghezzaSuX: la larghezza (lato lungo il bordo del ripiano) corrisponde all'asse X del modello, altrimenti all'asse Z.
    public static GameObject MettiScatola(string nome, Transform padre, Vector3 base_, float yawGradi, float larghezza, float profondita, float altezza,
                                          bool larghezzaSuX = false, Color? alone = null, float fondo = 0.75f, Vector3? saleVerso = null)
    {
        var pf = Resources.Load<GameObject>($"Modelli/{nome}/{nome}");
        if (pf == null) { Debug.LogWarning($"[Esplora] Modello '{nome}' non trovato"); return null; }
        var go = Object.Instantiate(pf, padre);
        go.name = nome;
        go.transform.rotation = Quaternion.identity;
        var b0 = Limiti(go);
        float kx = (larghezzaSuX ? larghezza : profondita) / Mathf.Max(b0.size.x, 0.0001f);
        float kz = (larghezzaSuX ? profondita : larghezza) / Mathf.Max(b0.size.z, 0.0001f);
        go.transform.localScale = new Vector3(kx, altezza / Mathf.Max(b0.size.y, 0.0001f), kz);
        go.transform.rotation = Quaternion.Euler(0f, yawGradi, 0f);
        if (saleVerso.HasValue)
        {
            // misura da che parte il modello sale (dove stanno i vertici alti) e lo gira, a multipli di 90 gradi, perché il lato alto guardi 'saleVerso'
            Vector3 dir = DirezioneSalita(go);
            Vector3 voluta = new Vector3(saleVerso.Value.x, 0f, saleVerso.Value.z);
            if (dir.sqrMagnitude > 1e-6f && voluta.sqrMagnitude > 1e-6f)
            {
                float delta = Vector3.SignedAngle(dir, voluta, Vector3.up);
                go.transform.rotation = Quaternion.Euler(0f, Mathf.Round(delta / 90f) * 90f, 0f) * go.transform.rotation;
            }
        }
        var b1 = Limiti(go);
        go.transform.position += new Vector3(base_.x - b1.center.x, base_.y - b1.min.y, base_.z - b1.center.z);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            foreach (var m in r.materials)
            {
                if (alone.HasValue && m.HasProperty("_Alone")) m.SetColor("_Alone", alone.Value);
                if (m.HasProperty("_Fondo")) m.SetFloat("_Fondo", fondo);
            }
        return go;
    }

    // direzione orizzontale (nel mondo) verso cui il modello sale: dal centro dei vertici verso il baricentro di quelli alti
    static Vector3 DirezioneSalita(GameObject go)
    {
        float yMin = float.MaxValue, yMax = float.MinValue;
        var punti = new System.Collections.Generic.List<Vector3>();
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var m = mf.sharedMesh; if (m == null || !m.isReadable) continue;
            foreach (var v in m.vertices)
            {
                var w = mf.transform.TransformPoint(v);
                punti.Add(w); yMin = Mathf.Min(yMin, w.y); yMax = Mathf.Max(yMax, w.y);
            }
        }
        if (punti.Count == 0 || yMax - yMin < 1e-4f) return Vector3.zero;
        Vector3 centro = Vector3.zero, alto = Vector3.zero; int n = 0;
        foreach (var w in punti) { centro += w; if (w.y > yMin + 0.7f * (yMax - yMin)) { alto += w; n++; } }
        if (n == 0) return Vector3.zero;
        var d = alto / n - centro / punti.Count; d.y = 0f;
        return d;
    }

    public static Bounds Limiti(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
