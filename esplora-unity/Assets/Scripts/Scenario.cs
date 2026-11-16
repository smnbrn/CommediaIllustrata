using System.Collections.Generic;
using UnityEngine;

// Scenografia "fuori dall'Inferno": il coperchio che chiude la voragine (coperto dalla selva oscura),
// la selva attorno al luogo di partenza, la montagna del colle e la porta dell'Inferno.
// Tutto è fatto di facce piatte con colore per faccia (shader ColoreVertici), in poche mesh.
public class MeshFacce
{
    readonly List<Vector3> v = new List<Vector3>();
    readonly List<Color> c = new List<Color>();
    readonly List<int> t = new List<int>();

    public void Tri(Vector3 a, Vector3 b, Vector3 d, Color col)
    {
        int k = v.Count;
        v.Add(a); v.Add(b); v.Add(d);
        c.Add(col); c.Add(col); c.Add(col);
        t.Add(k); t.Add(k + 1); t.Add(k + 2);
    }

    public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e, Color col) { Tri(a, b, d, col); Tri(a, d, e, col); }

    static Color Ombra(Color col, float f) { return new Color(col.r * f, col.g * f, col.b * f, 1f); }

    public void Cono(Vector3 baseC, float r, float h, int lati, Color col, System.Random rnd)
    {
        Vector3 punta = baseC + Vector3.up * h;
        for (int i = 0; i < lati; i++)
        {
            float a0 = i / (float)lati * Mathf.PI * 2f, a1 = (i + 1) / (float)lati * Mathf.PI * 2f;
            var p0 = baseC + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r;
            var p1 = baseC + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r;
            Tri(p0, punta, p1, Ombra(col, 0.82f + 0.3f * (float)rnd.NextDouble()));
        }
    }

    public void Scatola(Vector3 centro, Vector3 dim, Quaternion rot, Color col)
    {
        Vector3 e = dim * 0.5f;
        Vector3[] p = new Vector3[8];
        for (int i = 0; i < 8; i++)
            p[i] = centro + rot * new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
        Quad(p[0], p[1], p[3], p[2], Ombra(col, 0.8f));   // dietro
        Quad(p[4], p[5], p[7], p[6], Ombra(col, 1.0f));   // davanti
        Quad(p[0], p[4], p[6], p[2], Ombra(col, 0.7f));
        Quad(p[1], p[5], p[7], p[3], Ombra(col, 0.9f));
        Quad(p[2], p[3], p[7], p[6], Ombra(col, 1.15f));  // sopra
        Quad(p[0], p[1], p[5], p[4], Ombra(col, 0.6f));
    }

    public void Albero(Vector3 piede, float altezza, System.Random rnd)
    {
        float g = 0.75f + 0.5f * (float)rnd.NextDouble();
        Color foglie = new Color(0.07f * g, 0.20f * g, 0.08f * g);
        if (rnd.NextDouble() < 0.25) foglie = new Color(0.10f * g, 0.16f * g, 0.05f * g);
        Scatola(piede + Vector3.up * altezza * 0.12f, new Vector3(altezza * 0.07f, altezza * 0.24f, altezza * 0.07f), Quaternion.identity, new Color(0.25f, 0.15f, 0.08f));
        Cono(piede + Vector3.up * altezza * 0.18f, altezza * 0.30f, altezza * 0.55f, 6, foglie, rnd);
        Cono(piede + Vector3.up * altezza * 0.50f, altezza * 0.22f, altezza * 0.50f, 6, Ombra(foglie, 1.1f), rnd);
    }

    // montagna irregolare a fasce: terra, roccia, neve
    public void Montagna(Vector3 centro, float raggio, float altezza, int lati, System.Random rnd, Color[] colori = null)
    {
        float[] fr = { 1f, 0.66f, 0.34f, 0f };
        float[] fh = { 0f, 0.38f, 0.74f, 1f };
        Color[] fasce = colori ?? new Color[] { new Color(0.30f, 0.26f, 0.17f), new Color(0.42f, 0.38f, 0.34f), new Color(0.72f, 0.74f, 0.78f) };
        var sv = new float[lati]; for (int i = 0; i < lati; i++) sv[i] = 0.8f + 0.4f * (float)rnd.NextDouble();
        Vector3 Punto(int anello, int i)
        {
            if (anello == fr.Length - 1) return centro + Vector3.up * altezza;
            float a = i / (float)lati * Mathf.PI * 2f;
            float rr = raggio * fr[anello] * (anello == 0 ? sv[i % lati] : 0.9f + 0.1f * sv[i % lati]);
            return centro + new Vector3(Mathf.Cos(a) * rr, altezza * fh[anello], Mathf.Sin(a) * rr);
        }
        for (int an = 0; an < fr.Length - 1; an++)
            for (int i = 0; i < lati; i++)
            {
                Color col = Ombra(fasce[an], 0.78f + 0.4f * (float)rnd.NextDouble());
                if (an == fr.Length - 2) Tri(Punto(an, i), Punto(an + 1, 0), Punto(an, i + 1), col);
                else Quad(Punto(an, i), Punto(an + 1, i), Punto(an + 1, i + 1), Punto(an, i + 1), col);
            }
    }

    public Mesh Costruisci(string nome)
    {
        var m = new Mesh { name = nome, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }
}

// Alberi dal modello Sketchfab (Resources/Modelli/Alberi): tanti alberi uniti in una sola mesh per tenere pochi i draw call
public class AlberiModello
{
    readonly List<Mesh> varianti = new List<Mesh>();
    public Material materiale;
    public bool Pronto { get { return varianti.Count > 0 && materiale != null; } }

    public AlberiModello()
    {
        var pf = Resources.Load<GameObject>("Modelli/Alberi/Alberi");
        if (pf == null) return;
        var tutti = new List<KeyValuePair<Mesh, Material>>();
        float maxH = 0f;
        foreach (var mf in pf.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            var mr = mf.GetComponent<MeshRenderer>();
            tutti.Add(new KeyValuePair<Mesh, Material>(mf.sharedMesh, mr != null ? mr.sharedMaterial : null));
            maxH = Mathf.Max(maxH, mf.sharedMesh.bounds.size.y);
        }
        foreach (var kv in tutti)
            if (kv.Key.bounds.size.y >= maxH * 0.5f) { varianti.Add(kv.Key); if (materiale == null) materiale = kv.Value; }
        if (materiale != null)
        {
            materiale = new Material(materiale);
            materiale.SetColor("_Tint", new Color(0.72f, 0.82f, 0.70f));   // un po' più scuri, per il buio dell'Inferno
        }
    }

    public void Aggiungi(List<CombineInstance> dove, System.Random rnd, Vector3 piede, float altezza)
    {
        var m = varianti[rnd.Next(varianti.Count)];
        var b = m.bounds;
        float s = altezza / Mathf.Max(b.size.y, 0.0001f);
        var mtx = Matrix4x4.TRS(piede, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f), new Vector3(s * 1.7f, s, s * 1.7f))
                  * Matrix4x4.Translate(new Vector3(-b.center.x, -b.min.y, -b.center.z));
        dove.Add(new CombineInstance { mesh = m, transform = mtx });
    }

    public Mesh Unisci(List<CombineInstance> l, string nome)
    {
        var m = new Mesh { name = nome, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.CombineMeshes(l.ToArray(), true, true);
        return m;
    }
}

public static class Scenario
{
    const float YAW_BOCCA = 0f;   // calibrazione: rotazione per far guardare la bocca del modello della caverna verso il colle

    static Bounds Limiti(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // mette un modello: lato lungo orientato lungo 'asseLungo' (direzione orizzontale), lungo 'lunghezza', con il centro in
    // 'centro' e il punto più basso a quota -affondo (così il bordo tagliato resta sotto terra)
    static GameObject Metti(string risorsa, Transform padre, Vector3 centro, float yaw, float lunghezza, float affondo)
    {
        var pf = Resources.Load<GameObject>(risorsa);
        if (pf == null) return null;
        var go = Object.Instantiate(pf, padre);
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var b = Limiti(go);
        float k = lunghezza / Mathf.Max(b.size.x, b.size.z, 0.0001f);
        go.transform.localScale = Vector3.one * k;
        b = Limiti(go);
        go.transform.position += new Vector3(centro.x - b.center.x, -affondo - b.min.y, centro.z - b.center.z);
        return go;
    }

    static GameObject Oggetto(string nome, Transform padre, Mesh mesh, Material mat, bool collider)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    static float DistPuntoSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector2.Distance(p, a + ab * t);
    }

    // Costruisce la scenografia. 'selva', 'colle' e 'porta' sono le posizioni dei luoghi (la porta viene spostata
    // sotto l'arco). Restituisce il coperchio (da far sparire quando si apre la porta) e la nuova posizione della porta.
    public static Transform Costruisci(Transform padre, Material mat, Color terreno, float raggioBordo,
                                       Vector3 selva, Vector3 colle, ref Vector3 porta, out Transform portaModello)
    {
        var rnd = new System.Random(1307);
        var rootFisso = new GameObject("Scenario").transform;
        rootFisso.SetParent(padre, false);

        // ---- coperchio: disco sopra la voragine, coperto di alberi (tranne lungo il tratto dritto del percorso)
        var coperchio = new GameObject("Coperchio").transform;
        coperchio.SetParent(padre, false);
        float yTerra = -0.15f;
        var disco = new MeshFacce();
        const int N = 56;
        for (int i = 0; i < N; i++)
        {
            float a0 = i / (float)N * Mathf.PI * 2f, a1 = (i + 1) / (float)N * Mathf.PI * 2f;
            float f = (i % 2 == 0) ? 1f : 0.93f;
            var col = new Color(terreno.r * 0.7f * f, terreno.g * 0.85f * f, terreno.b * 0.6f * f);
            var o = new Vector3(0, yTerra, 0);
            var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * raggioBordo + Vector3.up * yTerra;
            var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * raggioBordo + Vector3.up * yTerra;
            disco.Tri(o, p0, p1, col);
            disco.Tri(o, p1, p0, col);   // doppio verso: il collider deve fermare i raggi da sopra e da sotto
        }
        var modelloAlberi = new AlberiModello();
        var alberi = new MeshFacce();
        var alberiM = new List<CombineInstance>();
        Vector2 sa = new Vector2(selva.x, selva.z), co = new Vector2(colle.x, colle.z);
        for (int n = 0; n < (modelloAlberi.Pronto ? 420 : 520); n++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rr = Mathf.Sqrt((float)rnd.NextDouble()) * (raggioBordo - 2.5f);
            var p = new Vector2(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr);
            if (DistPuntoSegmento(p, sa, co) < 4f) continue;   // lascia libero il sentiero
            float h = 4f + 5f * (float)rnd.NextDouble();
            if (modelloAlberi.Pronto) modelloAlberi.Aggiungi(alberiM, rnd, new Vector3(p.x, yTerra, p.y), h * 1.3f);
            else alberi.Albero(new Vector3(p.x, yTerra, p.y), h, rnd);
        }
        Oggetto("Disco", coperchio, disco.Costruisci("DiscoCoperchio"), mat, true);
        if (modelloAlberi.Pronto) Oggetto("Alberi", coperchio, modelloAlberi.Unisci(alberiM, "AlberiCoperchio"), modelloAlberi.materiale, false);
        else Oggetto("Alberi", coperchio, alberi.Costruisci("AlberiCoperchio"), mat, false);

        // ---- selva oscura attorno al punto di partenza (fuori dal coperchio, resta sempre)
        var selvaMesh = new MeshFacce();
        var selvaM = new List<CombineInstance>();
        for (int n = 0; n < 110; n++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float rr = 3.5f + Mathf.Sqrt((float)rnd.NextDouble()) * 17f;
            var p = sa + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rr;
            if (p.magnitude < raggioBordo + 2.5f) continue;            // non sopra il coperchio
            float h = 5f + 6f * (float)rnd.NextDouble();
            if (modelloAlberi.Pronto) modelloAlberi.Aggiungi(selvaM, rnd, new Vector3(p.x, 0f, p.y), h * 1.3f);
            else selvaMesh.Albero(new Vector3(p.x, 0f, p.y), h, rnd);
        }
        if (modelloAlberi.Pronto) Oggetto("Selva", rootFisso, modelloAlberi.Unisci(selvaM, "Selva"), modelloAlberi.materiale, false);
        else Oggetto("Selva", rootFisso, selvaMesh.Costruisci("Selva"), mat, false);

        // ---- il colle: una montagna dietro il luogo, vista dalla voragine
        Vector3 fuori = new Vector3(colle.x, 0f, colle.z).normalized;
        Vector3 tang = new Vector3(-fuori.z, 0f, fuori.x);
        var colleModello = Metti("Modelli/Colle/Colle", rootFisso, new Vector3(colle.x, 0f, colle.z) + fuori * 16f,
                                 Mathf.Atan2(tang.x, tang.z) * Mathf.Rad2Deg - 90f, 40f, 1.0f);
        if (colleModello != null) colleModello.name = "Colle";
        else
        {
            var monte = new MeshFacce();
            Vector3 cm = new Vector3(colle.x, 0f, colle.z) + fuori * 15f;   // dietro il segnaposto, che resta davanti alla montagna
            monte.Montagna(cm, 10f, 20f, 9, rnd);
            monte.Montagna(cm + tang * 9f + fuori * 2f, 7f, 12f, 8, rnd);
            monte.Montagna(cm - tang * 8f + fuori * 1f, 6f, 10f, 8, rnd);
            Oggetto("Colle", rootFisso, monte.Costruisci("Colle"), mat, false);
        }

        // ---- l'ingresso dell'Inferno: una caverna in una collinetta di roccia, con la bocca rivolta verso chi arriva dal colle
        // (il percorso si ferma davanti alla bocca, senza attraversarla, e poi scende nel terreno verso l'interno)
        Vector3 bordo = new Vector3(porta.x, 0f, porta.z);
        bordo += bordo.normalized * 13f;                                            // fuori dal coperchio, sul terreno
        Vector3 dir = bordo - new Vector3(colle.x, 0f, colle.z); dir.y = 0f; dir.Normalize();
        Vector3 centro = bordo;
        Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
        float yawBocca = Mathf.Atan2(-dir.x, -dir.z) * Mathf.Rad2Deg + YAW_BOCCA;   // la bocca del modello guarda verso chi arriva dal colle
        var grottaModello = Metti("Modelli/Caverna/Caverna", rootFisso, bordo + dir * 11f, yawBocca, 24f, 0.3f);
        if (grottaModello != null) { grottaModello.name = "Caverna"; portaModello = grottaModello.transform; }
        else
        {
            var grotta = new MeshFacce();
            Color[] roccia = { new Color(0.27f, 0.22f, 0.17f), new Color(0.36f, 0.32f, 0.30f), new Color(0.30f, 0.27f, 0.27f) };
            grotta.Montagna(rot * new Vector3(0, 0, 7.5f), 8.5f, 9f, 10, rnd, roccia);
            Color pietra = new Color(0.30f, 0.27f, 0.27f);
            grotta.Scatola(rot * new Vector3(0, 2.1f, 0.2f), new Vector3(6.4f, 4.2f, 3.6f), rot, new Color(0.03f, 0.02f, 0.03f));     // la bocca, scura
            grotta.Scatola(rot * new Vector3(-3.9f, 2.6f, 0.3f), new Vector3(1.8f, 5.2f, 3.2f), rot, pietra);
            grotta.Scatola(rot * new Vector3(3.9f, 2.6f, 0.3f), new Vector3(1.8f, 5.2f, 3.2f), rot, pietra);
            grotta.Scatola(rot * new Vector3(0, 4.9f, 0.3f), new Vector3(9.6f, 1.6f, 3.4f), rot, new Color(0.34f, 0.3f, 0.3f));
            grotta.Cono(rot * new Vector3(-6f, 0, -0.5f), 2.4f, 3f, 6, new Color(0.33f, 0.3f, 0.29f), rnd);
            grotta.Cono(rot * new Vector3(6.2f, 0, -0.2f), 2.1f, 2.6f, 6, new Color(0.31f, 0.28f, 0.27f), rnd);
            var goPorta = Oggetto("Caverna", rootFisso, grotta.Costruisci("Caverna"), mat, false);
            goPorta.transform.position = centro;
            portaModello = goPorta.transform;
        }
        porta = centro - dir * 3.5f + Vector3.up * 0.8f;   // punto del percorso: davanti alla bocca

        return coperchio;
    }
}
