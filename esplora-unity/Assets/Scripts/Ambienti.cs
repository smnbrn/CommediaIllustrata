using System.Collections.Generic;
using UnityEngine;

// Le scene dei cerchi, fatte con i modelli di Sketchfab (vedi esplora/CREDITI.md): il castello del Limbo, i massi degli avari,
// le tombe degli eretici, gli alberi morti dei violenti, il ponte di Malebolge e il ghiaccio di Cocito.
// Ogni gruppo di copie dello stesso modello è unito in una sola mesh per tenere pochi i draw call.

public struct Terrazza
{
    public float y, rEsterno, rInterno;   // quota e raggi (unità) del ripiano orizzontale
    public int cerchio;                   // cerchio a cui appartiene (0 = Antinferno)
    public float Larghezza { get { return rEsterno - rInterno; } }
}

public class GruppoModello
{
    class Parte { public Mesh mesh; public Material mat; public Matrix4x4 m; }
    class Variante { public List<Parte> parti = new List<Parte>(); public Bounds b; }

    readonly List<Variante> varianti = new List<Variante>();
    readonly Dictionary<Material, List<CombineInstance>> lotti = new Dictionary<Material, List<CombineInstance>>();
    public bool Pronto { get { return varianti.Count > 0; } }

    // pacchettoVarianti: ogni mesh del modello è un oggetto a sé (massi); altrimenti tutte le mesh formano un solo oggetto
    public GruppoModello(string risorsa, bool pacchettoVarianti)
    {
        var pf = Resources.Load<GameObject>(risorsa);
        if (pf == null) return;
        var radice = pf.transform;
        var parti = new List<Parte>();
        foreach (var mf in pf.GetComponentsInChildren<MeshFilter>())
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (mf.sharedMesh == null || mr == null || mr.sharedMaterial == null) continue;
            parti.Add(new Parte { mesh = mf.sharedMesh, mat = mr.sharedMaterial, m = radice.worldToLocalMatrix * mf.transform.localToWorldMatrix });
        }
        if (parti.Count == 0) return;
        if (pacchettoVarianti) foreach (var p in parti) varianti.Add(Crea(new List<Parte> { p }));
        else varianti.Add(Crea(parti));
    }

    static Variante Crea(List<Parte> parti)
    {
        var v = new Variante { parti = parti };
        bool primo = true;
        foreach (var p in parti)
        {
            var bb = p.mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var c = bb.center + new Vector3((i & 1) == 0 ? -bb.extents.x : bb.extents.x, (i & 2) == 0 ? -bb.extents.y : bb.extents.y, (i & 4) == 0 ? -bb.extents.z : bb.extents.z);
                var w = p.m.MultiplyPoint3x4(c);
                if (primo) { v.b = new Bounds(w, Vector3.zero); primo = false; } else v.b.Encapsulate(w);
            }
        }
        return v;
    }

    // dimensione: altezza (perAltezza) oppure lato orizzontale più lungo
    public void Aggiungi(System.Random rnd, Vector3 piede, float yawGradi, float dimensione, bool perAltezza)
    {
        var v = varianti[rnd.Next(varianti.Count)];
        float k = perAltezza ? dimensione / Mathf.Max(v.b.size.y, 0.0001f) : dimensione / Mathf.Max(v.b.size.x, v.b.size.z, 0.0001f);
        var mtx = Matrix4x4.TRS(piede, Quaternion.Euler(0f, yawGradi, 0f), Vector3.one * k)
                  * Matrix4x4.Translate(new Vector3(-v.b.center.x, -v.b.min.y, -v.b.center.z));
        foreach (var p in v.parti)
        {
            if (!lotti.TryGetValue(p.mat, out var l)) lotti[p.mat] = l = new List<CombineInstance>();
            l.Add(new CombineInstance { mesh = p.mesh, transform = mtx * p.m });
        }
    }

    public void Costruisci(Transform padre, string nome)
    {
        var radice = new GameObject(nome).transform;
        radice.SetParent(padre, false);
        foreach (var kv in lotti)
        {
            var m = new Mesh { name = nome, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.CombineMeshes(kv.Value.ToArray(), true, true);
            var go = new GameObject(nome + "_" + kv.Key.name);
            go.transform.SetParent(radice, false);
            go.AddComponent<MeshFilter>().sharedMesh = m;
            go.AddComponent<MeshRenderer>().sharedMaterial = kv.Key;
        }
    }
}

public static class Ambienti
{
    // Posiziona le scene. 'castello' è il punto del Nobile castello.
    public static Transform Costruisci(Transform padre, List<Terrazza> terrazze, Vector3 castello)
    {
        var radice = new GameObject("Ambienti").transform;
        radice.SetParent(padre, false);
        var rnd = new System.Random(4242);

        // Limbo: il Nobile castello addossato alla parete, sul ripiano del I cerchio
        var cast = new GruppoModello("Modelli/Castello/Castello", false);
        if (cast.Pronto)
        {
            float a = Mathf.Atan2(castello.z, castello.x);
            float yTer = -9.6f;
            foreach (var t in terrazze) if (t.cerchio == 1) { yTer = t.y; break; }
            var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 58.2f + Vector3.up * yTer;
            float yaw = Mathf.Atan2(-Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg;   // il fronte verso l'asse della voragine
            cast.Aggiungi(rnd, pos, yaw, 13f, false);
            cast.Costruisci(radice, "Castello");
        }

        Distribuisci(radice, rnd, terrazze, 4, new GruppoModello("Modelli/Massi/Massi", true), 14, 1.5f, 2.8f, true, false, "Massi");              // avari e prodighi
        Distribuisci(radice, rnd, terrazze, 6, new GruppoModello("Modelli/Sarcofago/Sarcofago", false), 22, 3.2f, 3.6f, false, true, "Tombe");        // eretici
        Distribuisci(radice, rnd, terrazze, 7, new GruppoModello("Modelli/AlberoMorto/AlberoMorto", false), 42, 4.5f, 6.5f, true, false, "AlberiMorti"); // violenti

        // Malebolge: ponti in pietra che attraversano il ripiano largo sopra il pozzo dei giganti
        var ponte = new GruppoModello("Modelli/Ponte/Ponte", false);
        if (ponte.Pronto)
        {
            foreach (var t in terrazze)
                if (t.cerchio == 8 && t.Larghezza > 6f)
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (35f + i * 90f) * Mathf.Deg2Rad;
                        float rc = (t.rEsterno + t.rInterno) * 0.5f;
                        ponte.Aggiungi(rnd, new Vector3(Mathf.Cos(a) * rc, t.y - 0.1f, Mathf.Sin(a) * rc), -a * Mathf.Rad2Deg, t.Larghezza + 3f, false);
                    }
            ponte.Costruisci(radice, "Ponti");
        }

        // Cocito: cristalli di ghiaccio attorno al fondo, fuori dal palco di Lucifero e dalla burella
        var ghiaccio = new GruppoModello("Modelli/Ghiaccio/Ghiaccio", false);
        if (ghiaccio.Pronto)
        {
            for (int i = 0; i < 16; i++)
            {
                float gradi = (i + (float)rnd.NextDouble() * 0.6f) / 16f * 360f;
                if (Mathf.Abs(Mathf.DeltaAngle(gradi, 315f)) < 14f) continue;      // lascia libera la burella naturale
                float a = gradi * Mathf.Deg2Rad;
                float r = 7.6f + (float)rnd.NextDouble() * 1.6f;
                ghiaccio.Aggiungi(rnd, new Vector3(Mathf.Cos(a) * r, -80f, Mathf.Sin(a) * r), (float)rnd.NextDouble() * 360f, 2.2f + 2.2f * (float)rnd.NextDouble(), true);
            }
            ghiaccio.Costruisci(radice, "Ghiaccio");
        }
        return radice;
    }

    // copie sparse lungo i ripiani di un cerchio; 'tangente': allinea il lato lungo al bordo del ripiano
    static void Distribuisci(Transform padre, System.Random rnd, List<Terrazza> terrazze, int cerchio, GruppoModello g, int quante,
                             float dimMin, float dimMax, bool perAltezza, bool tangente, string nome)
    {
        if (!g.Pronto) return;
        var utili = terrazze.FindAll(t => t.cerchio == cerchio && t.Larghezza > 0.5f);
        if (utili.Count == 0) return;
        float larghezzaTot = 0f; foreach (var t in utili) larghezzaTot += t.Larghezza;
        for (int i = 0; i < quante; i++)
        {
            float pick = (float)rnd.NextDouble() * larghezzaTot; Terrazza t = utili[0];
            foreach (var u in utili) { if (pick <= u.Larghezza) { t = u; break; } pick -= u.Larghezza; }
            float a = ((i + (float)rnd.NextDouble() * 0.7f) / quante) * Mathf.PI * 2f;
            float r = t.rInterno + t.Larghezza * (0.3f + 0.4f * (float)rnd.NextDouble());
            float yaw = tangente ? (-(a * Mathf.Rad2Deg) + 90f + (float)(rnd.NextDouble() - 0.5) * 16f) : (float)rnd.NextDouble() * 360f;
            g.Aggiungi(rnd, new Vector3(Mathf.Cos(a) * r, t.y, Mathf.Sin(a) * r), yaw, dimMin + (dimMax - dimMin) * (float)rnd.NextDouble(), perAltezza);
        }
        g.Costruisci(padre, nome);
    }
}
