using System.Collections.Generic;
using UnityEngine;

// La montagna del Purgatorio costruita dai luoghi di data/luoghi-purgatorio.json (altezza, raggio, angolo):
// una spiaggia, i balzi dell'Antipurgatorio, la porta, le sette cornici e il giardino in cima.
// Ogni luogo è un ripiano circolare (un anello piatto); tra un ripiano e il successivo sale una parete,
// con la scala che Dante e Virgilio percorrono girando attorno al monte.
public class LivelloMonte
{
    public Luogo luogo;
    public float y, centro, rInt, rEst, larghezza, angolo;
    public Color colore, colorePar;
    public int stile, stilePar;
}

public class Monte
{
    public const float R = 130f;       // raggio della base (unità)
    public const float H = 110f;       // altezza della cima sul livello della spiaggia
    public const float MARE = -0.4f;   // quota del mare
    public const float FONDO = -1.3f;  // dove la riva scende sotto l'acqua
    const int SEG = 96;

    public readonly List<LivelloMonte> liv = new List<LivelloMonte>();
    readonly List<Vector2> profilo = new List<Vector2>();   // (raggio, quota) dal centro verso il mare
    struct Tratto { public Vector2 a, b; public int livello; public int tipo; }   // tipo: 0 ripiano, 1 parete, 2 riva
    readonly List<Tratto> tratti = new List<Tratto>();

    // larghezza del ripiano per luogo (indice = ordine di salita), colori del ripiano e della parete sopra di esso
    static readonly float[] LARGH = { 10f, 6.5f, 6f, 6f, 7f, 4.6f, 4.2f, 4.2f, 4f, 4f, 4f, 3.8f, 3.8f, 0f };
    static readonly Color[] COL = {
        new Color(0.88f,0.80f,0.60f), new Color(0.56f,0.51f,0.38f), new Color(0.62f,0.54f,0.34f), new Color(0.58f,0.42f,0.35f),
        new Color(0.30f,0.58f,0.27f), new Color(0.80f,0.76f,0.68f), new Color(0.64f,0.64f,0.68f), new Color(0.46f,0.52f,0.62f),
        new Color(0.42f,0.38f,0.40f), new Color(0.62f,0.64f,0.52f), new Color(0.57f,0.47f,0.29f), new Color(0.68f,0.60f,0.41f),
        new Color(0.64f,0.35f,0.27f), new Color(0.32f,0.70f,0.33f) };
    static readonly Color[] COLPAR = {
        new Color(0.62f,0.55f,0.42f), new Color(0.52f,0.47f,0.39f), new Color(0.54f,0.48f,0.38f), new Color(0.52f,0.40f,0.36f),
        new Color(0.46f,0.50f,0.34f), new Color(0.60f,0.56f,0.52f), new Color(0.60f,0.60f,0.64f), new Color(0.40f,0.45f,0.54f),
        new Color(0.30f,0.28f,0.31f), new Color(0.53f,0.55f,0.47f), new Color(0.49f,0.41f,0.29f), new Color(0.59f,0.51f,0.37f),
        new Color(0.52f,0.29f,0.25f), new Color(0.40f,0.55f,0.34f) };
    // stile della texture del terreno (indici di TexturaTerreno) per ripiano e parete
    static readonly int[] STILE = { 1, 0, 0, 4, 1, 4, 4, 4, 5, 1, 4, 1, 0, 1 };
    static readonly int[] STILEPAR = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0 };

    public Monte(Luogo[] luoghi)
    {
        var l = new List<Luogo>(luoghi);
        l.Sort((a, b) => a.ordine.CompareTo(b.ordine));
        for (int i = 0; i < l.Count; i++)
        {
            var lu = l[i];
            int k = Mathf.Min(i, LARGH.Length - 1);
            var m = new LivelloMonte { luogo = lu, y = lu.coordinate.altezza * H, larghezza = LARGH[k], angolo = lu.coordinate.angolo,
                                       colore = COL[k], colorePar = COLPAR[k], stile = STILE[k], stilePar = STILEPAR[k] };
            float rc = lu.coordinate.raggio * R;
            bool cima = i == l.Count - 1;
            if (cima) { m.rInt = 0f; m.rEst = rc; }
            else { m.rEst = rc + m.larghezza * 0.5f; m.rInt = m.rEst - m.larghezza; }
            if (i > 0)
            {
                var p = liv[i - 1];
                float tetto = p.rInt - (cima ? 2.0f : 1.0f);          // la parete deve rientrare
                if (m.rEst > tetto) { m.rEst = tetto; if (!cima) m.rInt = m.rEst - m.larghezza; }
            }
            m.centro = cima ? m.rEst * 0.55f : (m.rInt + m.rEst) * 0.5f;
            liv.Add(m);
        }
        Costruisci();
    }

    void Costruisci()
    {
        int n = liv.Count;
        var pts = new List<Vector2>(); var tipi = new List<int>(); var liviv = new List<int>();
        pts.Add(new Vector2(0f, liv[n - 1].y));
        pts.Add(new Vector2(liv[n - 1].rEst, liv[n - 1].y)); tipi.Add(0); liviv.Add(n - 1);
        for (int i = n - 2; i >= 0; i--)
        {
            pts.Add(new Vector2(liv[i].rInt, liv[i].y)); tipi.Add(1); liviv.Add(i);          // parete sopra il ripiano i
            pts.Add(new Vector2(liv[i].rEst, liv[i].y)); tipi.Add(0); liviv.Add(i);          // ripiano i
        }
        pts.Add(new Vector2(R * 1.08f, FONDO)); tipi.Add(2); liviv.Add(0);                   // riva che scende nel mare
        profilo.AddRange(pts);
        for (int i = 0; i < pts.Count - 1; i++)
            tratti.Add(new Tratto { a = pts[i], b = pts[i + 1], livello = liviv[i], tipo = tipi[i] });
    }

    // quota della superficie del monte alla distanza r dall'asse
    public float Altezza(float r)
    {
        if (r <= profilo[0].x) return profilo[0].y;
        for (int i = 0; i < profilo.Count - 1; i++)
        {
            var a = profilo[i]; var b = profilo[i + 1];
            if (r <= b.x)
            {
                float t = Mathf.Abs(b.x - a.x) < 1e-4f ? 1f : (r - a.x) / (b.x - a.x);
                return Mathf.Lerp(a.y, b.y, t);
            }
        }
        return FONDO;
    }

    // vero se il punto è dentro la roccia o sotto l'acqua (serve alla camera)
    public bool Solido(Vector3 p, float margine)
    {
        if (p.y < MARE + margine) return true;
        float r = new Vector2(p.x, p.z).magnitude;
        return p.y < Altezza(r) + margine;
    }

    // punto su un ripiano: ang in gradi, t da 0 (bordo interno, contro la parete) a 1 (bordo esterno)
    public Vector3 Punto(int i, float angGradi, float t)
    {
        var m = liv[i];
        float r = Mathf.Lerp(m.rInt, m.rEst, t);
        float a = angGradi * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * r, m.y, Mathf.Sin(a) * r);
    }

    public static Vector3 Fuori(float angGradi) { float a = angGradi * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); }
    public static float YawVerso(Vector3 dir) { return Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; }

    // raggio della parete (senza irregolarità) tra il ripiano i e il successivo, parametro t da 0 a 1
    public float RaggioParete(int i, float t)
    {
        if (i + 1 >= liv.Count) return liv[i].rEst;
        return Mathf.Lerp(liv[i].rInt, liv[i + 1].rEst, t);
    }

    static float Rugosita(float s, float t, int seg)
    {
        float n = 0.5f * (Mathf.Sin(s * 0.83f + seg * 1.7f) + Mathf.Sin(s * 2.11f + seg * 0.9f + t * 3f));
        return n * 0.85f * Mathf.Sin(Mathf.PI * t);
    }

    // il percorso di Dante: dal centro di un ripiano al bordo interno, su per la scala nella parete, fino al ripiano successivo
    public void Percorso(List<Vector3> punti, List<int> indiciLuoghi)
    {
        punti.Clear(); indiciLuoghi.Clear();
        for (int i = 0; i < liv.Count; i++)
        {
            var m = liv[i];
            float a = m.angolo;
            if (i == 0) { indiciLuoghi.Add(punti.Count); punti.Add(Punto(0, a, 0.5f) + Vector3.up * 0.4f); }
            else
            {
                var p = liv[i - 1];
                // sale la scala (angolo da p.angolo a m.angolo)
                const int N = 22;
                for (int k = 0; k <= N; k++)
                {
                    float t = k / (float)N;
                    float ang = Mathf.LerpAngle(p.angolo, m.angolo, t) * Mathf.Deg2Rad;
                    float y = Mathf.Lerp(p.y, m.y, t);
                    float r = RaggioParete(i - 1, t) + 1.3f;
                    punti.Add(new Vector3(Mathf.Cos(ang) * r, y + 0.35f, Mathf.Sin(ang) * r));
                }
                // dall'arrivo sul bordo esterno al centro del ripiano
                indiciLuoghi.Add(punti.Count);
                punti.Add(Punto(i, m.angolo, i == liv.Count - 1 ? 0.0f : 0.5f) + Vector3.up * 0.4f);
                if (i == liv.Count - 1) punti[punti.Count - 1] = new Vector3(Mathf.Cos(m.angolo * Mathf.Deg2Rad) * m.centro, m.y + 0.4f, Mathf.Sin(m.angolo * Mathf.Deg2Rad) * m.centro);
            }
            // verso il bordo interno, dove parte la scala successiva
            if (i + 1 < liv.Count) punti.Add(Punto(i, m.angolo, 0.05f) + Vector3.up * 0.4f);
        }
    }

    // la mesh del monte: tutti i ripiani e le pareti, con colore per faccia e coordinate della texture del terreno
    public Mesh MeshMonte()
    {
        var vert = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
        var uv = new List<Vector2>(); var st = new List<Vector4>();
        float lato = TexturaTerreno.Lato;
        for (int ti = 0; ti < tratti.Count; ti++)
        {
            var t = tratti[ti];
            var m = liv[t.livello];
            bool parete = t.tipo == 1;
            Color c0 = t.tipo == 2 ? COL[0] * 0.92f : parete ? m.colorePar * 0.82f : m.colore;
            Color c1 = t.tipo == 2 ? COL[0] * 0.8f : parete ? m.colorePar * 1.08f : m.colore;
            Vector4 stile = TexturaTerreno.Stile(parete ? m.stilePar : t.tipo == 2 ? 1 : m.stile);
            if (parete) stile.w *= 0.8f;
            int rip = Mathf.Max(1, Mathf.RoundToInt(Mathf.PI * 2f * Mathf.Max(t.a.x, t.b.x) / lato));
            int bande = parete ? 3 : 1;
            for (int j = 0; j < bande; j++)
            {
                float ta = j / (float)bande, tb = (j + 1) / (float)bande;
                Vector2 pa = Vector2.Lerp(t.a, t.b, ta), pb = Vector2.Lerp(t.a, t.b, tb);
                for (int s = 0; s < SEG; s++)
                {
                    float t0 = s / (float)SEG * Mathf.PI * 2f, t1 = (s + 1) / (float)SEG * Mathf.PI * 2f;
                    float v = (s % 2 == 0) ? 1f : 0.94f;
                    float da0 = parete ? Rugosita(s % SEG, ta, ti) : 0f, da1 = parete ? Rugosita((s + 1) % SEG, ta, ti) : 0f;
                    float db0 = parete ? Rugosita(s % SEG, tb, ti) : 0f, db1 = parete ? Rugosita((s + 1) % SEG, tb, ti) : 0f;
                    int k = vert.Count;
                    vert.Add(new Vector3(Mathf.Cos(t0) * (pa.x + da0), pa.y, Mathf.Sin(t0) * (pa.x + da0)));
                    vert.Add(new Vector3(Mathf.Cos(t1) * (pa.x + da1), pa.y, Mathf.Sin(t1) * (pa.x + da1)));
                    vert.Add(new Vector3(Mathf.Cos(t1) * (pb.x + db1), pb.y, Mathf.Sin(t1) * (pb.x + db1)));
                    vert.Add(new Vector3(Mathf.Cos(t0) * (pb.x + db0), pb.y, Mathf.Sin(t0) * (pb.x + db0)));
                    Color ca = Color.Lerp(c0, c1, ta) * v, cb = Color.Lerp(c0, c1, tb) * v; ca.a = 1f; cb.a = 1f;
                    col.Add(ca); col.Add(ca); col.Add(cb); col.Add(cb);
                    for (int q = 0; q < 4; q++)
                    {
                        var p = vert[k + q];
                        if (parete) uv.Add(new Vector2((q == 0 || q == 3 ? s : s + 1) / (float)SEG * rip, p.y / lato));
                        else uv.Add(new Vector2(p.x / lato, p.z / lato));
                        st.Add(stile);
                    }
                    tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
            }
        }
        var me = new Mesh { name = "Monte", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        me.SetVertices(vert); me.SetColors(col); me.SetTriangles(tri, 0); me.SetUVs(0, uv); me.SetUVs(1, st);
        me.RecalculateBounds();
        return me;
    }

    // la scala scavata nella parete: un nastro chiaro a gradini che sale da un ripiano al successivo
    public Mesh MeshScale()
    {
        var f = new MeshFacce();
        for (int i = 0; i + 1 < liv.Count; i++)
        {
            var p = liv[i]; var m = liv[i + 1];
            const int N = 22;
            Vector3 prima = Vector3.zero, tPrima = Vector3.zero;
            for (int k = 0; k <= N; k++)
            {
                float t = k / (float)N;
                float ang = Mathf.LerpAngle(p.angolo, m.angolo, t) * Mathf.Deg2Rad;
                float y = Mathf.Lerp(p.y, m.y, t);
                float r = RaggioParete(i, t) + 0.9f;
                var c = new Vector3(Mathf.Cos(ang) * r, y + 0.08f, Mathf.Sin(ang) * r);
                var tg = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                if (k > 0)
                {
                    var cc = (k % 2 == 0) ? new Color(0.93f, 0.88f, 0.75f) : new Color(0.78f, 0.72f, 0.60f);
                    f.Quad(prima - tPrima * 0.9f, prima + tPrima * 0.9f, c + tg * 0.9f, c - tg * 0.9f, cc);
                }
                prima = c; tPrima = tg;
            }
        }
        return f.Costruisci("Scale");
    }

    // il mare: dischi concentrici, chiaro vicino alla riva e profondo al largo
    public Mesh MeshMare()
    {
        float[] anelli = { 0f, R * 1.05f, R * 1.5f, 320f, 900f, 2600f };
        Color[] colori = { new Color(0.30f, 0.72f, 0.76f), new Color(0.25f, 0.66f, 0.74f), new Color(0.13f, 0.46f, 0.68f), new Color(0.08f, 0.32f, 0.58f), new Color(0.06f, 0.25f, 0.50f), new Color(0.08f, 0.26f, 0.48f) };
        var vert = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
        const int S = 72;
        for (int r = 0; r < anelli.Length; r++)
            for (int s = 0; s < S; s++)
            {
                float a = s / (float)S * Mathf.PI * 2f;
                vert.Add(new Vector3(Mathf.Cos(a) * anelli[r], MARE, Mathf.Sin(a) * anelli[r]));
                col.Add(colori[r]);
            }
        for (int r = 0; r < anelli.Length - 1; r++)
            for (int s = 0; s < S; s++)
            {
                int a = r * S + s, b = r * S + (s + 1) % S, c = (r + 1) * S + (s + 1) % S, d = (r + 1) * S + s;
                tri.AddRange(new[] { a, c, b, a, d, c });
            }
        var me = new Mesh { name = "Mare", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        me.SetVertices(vert); me.SetColors(col); me.SetTriangles(tri, 0);
        me.RecalculateBounds();
        return me;
    }
}
