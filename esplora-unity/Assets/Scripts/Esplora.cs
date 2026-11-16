using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Costruisce la voragine dell'Inferno dai file di /data e gestisce atmosfera, interfaccia e viaggio.
// Nessun dato sta nel codice: luoghi, scene e tavole si leggono da data/luoghi.json e data/inferno/canto-NN.json.
public class Esplora : MonoBehaviour
{
    const float R = 60f;    // raggio del bordo della voragine (unità)
    const float H = 80f;    // profondità totale fino a Lucifero
    const int SEGMENTI = 96;
    const int NUM_CANTI = 34;
    const int MAX_TEXTURE = 8;
    const string SITO = "https://smnbrn.github.io/CommediaIllustrata/";

    class Segnaposto { public Luogo luogo; public Vector3 pos; public Material mat; public GameObject go; public Color colore; }

    // Atmosfera di ogni cerchio (indice = numero del cerchio, 0 = Antinferno)
    struct Tema
    {
        public Color muro, particella;
        public float dim, vel, rate, vortice, rumore;
        public bool cade;
        public Tema(Color muro, Color particella, float dim, float vel, float rate, bool cade, float vortice = 0f, float rumore = 0.3f)
        { this.muro = muro; this.particella = particella; this.dim = dim; this.vel = vel; this.rate = rate; this.cade = cade; this.vortice = vortice; this.rumore = rumore; }
    }

    static readonly Tema[] TEMI =
    {
        /* 0 Antinferno  */ new Tema(new Color(0.42f,0.38f,0.30f), new Color(0.9f,0.85f,0.35f,0.9f), 0.35f, 2.5f, 90, false, 0f, 3f),
        /* 1 Limbo       */ new Tema(new Color(0.52f,0.58f,0.70f), new Color(0.8f,0.88f,1.0f,0.55f), 0.7f, 1.0f, 25, false, 0f, 0.4f),
        /* 2 Lussuriosi  */ new Tema(new Color(0.46f,0.28f,0.56f), new Color(0.95f,0.55f,0.95f,0.8f), 0.6f, 3.0f, 120, false, 1.6f, 1.2f),
        /* 3 Golosi      */ new Tema(new Color(0.34f,0.38f,0.20f), new Color(0.6f,0.75f,0.9f,0.8f), 0.3f, 14f, 260, true, 0f, 0.05f),
        /* 4 Avari       */ new Tema(new Color(0.62f,0.50f,0.24f), new Color(1f,0.85f,0.4f,0.7f), 0.5f, 1.6f, 70, false, 0.4f, 0.5f),
        /* 5 Iracondi    */ new Tema(new Color(0.20f,0.32f,0.32f), new Color(0.35f,0.6f,0.5f,0.30f), 3.0f, 1.0f, 18, false, 0.2f, 0.6f),
        /* 6 Eretici     */ new Tema(new Color(0.88f,0.40f,0.10f), new Color(1f,0.55f,0.15f,0.95f), 0.4f, 4.5f, 150, false, 0f, 1.5f),
        /* 7 Violenti    */ new Tema(new Color(0.68f,0.10f,0.10f), new Color(1f,0.45f,0.1f,0.9f), 0.5f, 6f, 160, true, 0f, 0.5f),
        /* 8 Malebolge   */ new Tema(new Color(0.46f,0.36f,0.10f), new Color(0.85f,0.95f,0.3f,0.8f), 0.4f, 3.5f, 130, false, 0f, 1.6f),
        /* 9 Cocito      */ new Tema(new Color(0.62f,0.80f,0.96f), new Color(0.95f,0.98f,1f,0.9f), 0.35f, 2.2f, 200, true, 0f, 0.6f),
    };

    struct Banda { public float profondita, raggio; public int cerchio; }

    CameraOrbita cam;
    Shader shader, shaderParticelle;
    Material matParticelle, matScia;
    readonly List<Banda> bande = new List<Banda>();
    readonly List<Segnaposto> segnaposti = new List<Segnaposto>();
    readonly Dictionary<string, List<RiferimentoScena>> scenePerLuogo = new Dictionary<string, List<RiferimentoScena>>();
    Segnaposto selezionato, precedente, lucifero;
    string stato = "Carico i dati…";

    // viaggio di Dante lungo il percorso
    readonly List<Vector3> percorso = new List<Vector3>();
    readonly List<float> lunghezze = new List<float>();
    List<Segnaposto> perOrdine = new List<Segnaposto>();
    Transform dante;
    float distPercorsa;
    bool inViaggio;
    int prossimo = 1;          // prossimo luogo da raggiungere (indice in perOrdine)
    float sosta;               // secondi ancora da trascorrere nel luogo corrente
    const float VELOCITA = 16f;
    const float SECONDI_PER_SCENA = 4.5f;

    // tavole
    readonly Dictionary<string, Texture2D> texture = new Dictionary<string, Texture2D>();
    readonly Queue<string> ordineTexture = new Queue<string>();
    readonly HashSet<string> inCaricamento = new HashSet<string>();
    readonly HashSet<string> fallite = new HashSet<string>();
    int idxScena;
    float tCiclo;

    // ---------- indirizzi ----------

    string RadiceSito()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new Uri(new Uri(Application.absoluteURL), "../").ToString();
#else
        return "file:///" + Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")).Replace('\\', '/') + "/";
#endif
    }

    string UrlLettura(string indirizzo)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new Uri(new Uri(Application.absoluteURL), "../index.html#" + indirizzo).ToString();
#else
        return SITO + "index.html#" + indirizzo;
#endif
    }

    string UrlTavola(Tavola t)
    {
        string u = string.IsNullOrEmpty(t.miniatura) ? t.url : t.miniatura;
        bool stretto = Screen.width / ScalaGui() < 560f;
        if (stretto)
        {
            u = u.Replace("/960px-", "/500px-").Replace("-960.jpg", "-500.jpg");
        }
        return u.StartsWith("http") ? u : RadiceSito() + u;
    }

    // ---------- avvio ----------

    void Awake()
    {
        shader = Resources.Load<Shader>("ColoreVertici");
        shaderParticelle = Resources.Load<Shader>("Particelle");
        var c = Camera.main;
        if (c == null) { var g = new GameObject("Main Camera"); g.tag = "MainCamera"; c = g.AddComponent<Camera>(); }
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.035f, 0.025f, 0.04f);
        c.farClipPlane = 1000f;
        cam = c.GetComponent<CameraOrbita>();
        if (cam == null) cam = c.gameObject.AddComponent<CameraOrbita>();
        cam.bersaglio = new Vector3(0, -H * 0.45f, 0);
        cam.distanza = 190f; cam.yaw = 30f; cam.pitch = 55f;
    }

    IEnumerator Start()
    {
        string baseUrl = RadiceSito() + "data/";
        FileLuoghi fl = null;
        using (var rq = UnityWebRequest.Get(baseUrl + "luoghi.json"))
        {
            yield return rq.SendWebRequest();
            if (rq.result != UnityWebRequest.Result.Success) { stato = "Errore: luoghi.json non si carica (" + rq.error + ")"; yield break; }
            fl = JsonUtility.FromJson<FileLuoghi>(rq.downloadHandler.text);
        }

        Costruisci(fl.luoghi);
        stato = "";

        // le scene dei canti: servono per sapere cosa accade in ogni luogo
        for (int n = 1; n <= NUM_CANTI; n++)
        {
            using (var rq = UnityWebRequest.Get($"{baseUrl}inferno/canto-{n:00}.json"))
            {
                yield return rq.SendWebRequest();
                if (rq.result != UnityWebRequest.Result.Success) continue;
                var fc = JsonUtility.FromJson<FileCanto>(rq.downloadHandler.text);
                if (fc == null || fc.scene == null) continue;
                foreach (var s in fc.scene)
                {
                    if (string.IsNullOrEmpty(s.luogo_id)) continue;
                    if (!scenePerLuogo.TryGetValue(s.luogo_id, out var lista)) scenePerLuogo[s.luogo_id] = lista = new List<RiferimentoScena>();
                    lista.Add(new RiferimentoScena { canto = n, scena = s });
                }
            }
        }
    }

    // ---------- geometria e colori ----------

    static Vector3 Posizione(Coordinate c)
    {
        float a = c.angolo * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a) * c.raggio * R, -c.profondita * H, Mathf.Sin(a) * c.raggio * R);
    }

    int IndiceBanda(float t)
    {
        int idx = -1;
        for (int i = 0; i < bande.Count; i++) if (bande[i].profondita <= t + 0.0001f) idx = i;
        return idx;
    }

    Color ColoreProfondita(float t, bool parete)
    {
        int i = IndiceBanda(t);
        Color c = i < 0 ? new Color(0.22f, 0.27f, 0.18f) : TEMI[Mathf.Clamp(bande[i].cerchio, 0, 9)].muro;
        c *= 1f - 0.35f * t;
        c.a = 1f;
        if (parete) { c *= 0.62f; c.a = 1f; }
        return c;
    }

    static Color ColoreMarcatore(Luogo l)
    {
        switch (l.tipo)
        {
            case "cerchio": return new Color(1f, 0.85f, 0.35f);
            case "zona": return new Color(1f, 0.6f, 0.3f);
            case "soglia": return new Color(0.95f, 0.95f, 0.95f);
            case "fiume": return new Color(0.4f, 0.7f, 1f);
            default: return new Color(0.6f, 0.9f, 0.5f);
        }
    }

    Material NuovoMateriale(Color c)
    {
        var m = new Material(shader);
        m.SetColor("_Color", c);
        return m;
    }

    void Costruisci(Luogo[] luoghi)
    {
        // bande: un cerchio (e l'Antinferno) dal suo livello fino al successivo
        bande.Clear();
        foreach (var l in luoghi)
        {
            if (!string.IsNullOrEmpty(l.zona_di)) continue;
            bool cerchio = l.tipo == "cerchio" || (l.tipo == "regione" && l.cerchio == 0 && l.coordinate.profondita > 0f);
            if (!cerchio) continue;
            bande.Add(new Banda { profondita = l.coordinate.profondita, raggio = l.coordinate.raggio, cerchio = l.cerchio });
        }
        bande.Sort((a, b) => a.profondita.CompareTo(b.profondita));

        // profilo della voragine: i luoghi "contenitori" dentro il bordo, dal più alto al più basso
        var prof = new List<Vector2>(); // (raggio, profondità)
        foreach (var l in luoghi)
        {
            if (!string.IsNullOrEmpty(l.zona_di)) continue;
            if (l.coordinate.raggio > 1f || l.coordinate.profondita <= 0f) continue;
            prof.Add(new Vector2(l.coordinate.raggio, l.coordinate.profondita));
        }
        prof.Sort((a, b) => a.y.CompareTo(b.y));
        var punti = new List<Vector2> { new Vector2(1.6f, 0f), new Vector2(1.0f, 0f) }; // terreno + bordo
        foreach (var p in prof)
        {
            Vector2 prec = punti[punti.Count - 1];
            if (p.y - prec.y < 0.001f) continue;
            punti.Add(new Vector2(prec.x, p.y));   // parete verticale
            punti.Add(new Vector2(p.x, p.y));      // cornice
        }
        { Vector2 prec = punti[punti.Count - 1]; punti.Add(new Vector2(prec.x, 1f)); punti.Add(new Vector2(0f, 1f)); }

        var vert = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
        for (int i = 0; i < punti.Count - 1; i++)
        {
            Vector2 a = punti[i], b = punti[i + 1];
            bool parete = Mathf.Approximately(a.x, b.x);
            Color ca = ColoreProfondita(a.y, parete), cb = ColoreProfondita(b.y, parete);
            if (i == 0) { ca = cb = ColoreProfondita(0, false); }
            for (int s = 0; s < SEGMENTI; s++)
            {
                float t0 = s / (float)SEGMENTI * Mathf.PI * 2f, t1 = (s + 1) / (float)SEGMENTI * Mathf.PI * 2f;
                float v = (s % 2 == 0) ? 1f : 0.93f; // lieve variazione per rendere visibili i settori
                int k = vert.Count;
                vert.Add(new Vector3(Mathf.Cos(t0) * a.x * R, -a.y * H, Mathf.Sin(t0) * a.x * R));
                vert.Add(new Vector3(Mathf.Cos(t1) * a.x * R, -a.y * H, Mathf.Sin(t1) * a.x * R));
                vert.Add(new Vector3(Mathf.Cos(t1) * b.x * R, -b.y * H, Mathf.Sin(t1) * b.x * R));
                vert.Add(new Vector3(Mathf.Cos(t0) * b.x * R, -b.y * H, Mathf.Sin(t0) * b.x * R));
                Color c1 = ca * v, c2 = cb * v; c1.a = 1f; c2.a = 1f;
                col.Add(c1); col.Add(c1); col.Add(c2); col.Add(c2);
                tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
            }
        }
        var mesh = new Mesh { name = "Voragine", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(vert); mesh.SetColors(col); mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        var vg = new GameObject("Voragine");
        vg.transform.SetParent(transform, false);
        vg.AddComponent<MeshFilter>().sharedMesh = mesh;
        vg.AddComponent<MeshRenderer>().sharedMaterial = NuovoMateriale(Color.white);
        vg.AddComponent<MeshCollider>().sharedMesh = mesh; // serve a nascondere etichette e luoghi dietro la parete

        // segnaposti
        foreach (var l in luoghi)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = l.id;
            go.transform.SetParent(transform, false);
            Vector3 pos = Posizione(l.coordinate);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (l.tipo == "zona" ? 1.3f : 2.0f);
            var col2 = ColoreMarcatore(l);
            var mat = NuovoMateriale(col2);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var sp = new Segnaposto { luogo = l, pos = pos, mat = mat, go = go, colore = col2 };
            segnaposti.Add(sp);
            if (l.id == "lucifero") lucifero = sp;
        }

        // percorso di Dante: i luoghi in ordine di discesa
        perOrdine = new List<Segnaposto>(segnaposti);
        perOrdine.Sort((a, b) => a.luogo.ordine.CompareTo(b.luogo.ordine));
        percorso.Clear(); lunghezze.Clear();
        float tot = 0f;
        for (int i = 0; i < perOrdine.Count; i++)
        {
            if (i > 0) tot += Vector3.Distance(perOrdine[i - 1].pos, perOrdine[i].pos);
            percorso.Add(perOrdine[i].pos); lunghezze.Add(tot);
        }
        var lr = new GameObject("Percorso").AddComponent<LineRenderer>();
        lr.transform.SetParent(transform, false);
        lr.positionCount = percorso.Count; lr.SetPositions(percorso.ToArray());
        lr.widthMultiplier = 0.5f; lr.useWorldSpace = true; lr.numCornerVertices = 4;
        lr.sharedMaterial = NuovoMateriale(new Color(1f, 0.95f, 0.6f));
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        PreparaMaterialiParticelle();

        // Dante, con la sua scia dorata
        var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(d.GetComponent<Collider>());
        d.name = "Dante"; d.transform.SetParent(transform, false);
        d.transform.localScale = Vector3.one * 3.2f;
        d.GetComponent<MeshRenderer>().sharedMaterial = NuovoMateriale(new Color(1f, 1f, 0.9f));
        dante = d.transform; dante.position = percorso[0];
        var scia = d.AddComponent<TrailRenderer>();
        scia.time = 3f; scia.widthMultiplier = 2.2f; scia.minVertexDistance = 0.3f;
        scia.sharedMaterial = matScia;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        scia.colorGradient = g;
        scia.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));

        CreaAtmosfera();
        PosizionaFigure();
    }

    // ---------- figure: Caronte, Cerbero, Lucifero ----------

    Segnaposto Trova(string id) { return segnaposti.Find(s => s.luogo.id == id); }

    // mette la figura sul luogo, spostata un po' verso l'asse e rivolta verso il centro della voragine
    void Metti(string idLuogo, GameObject figura, float verso_asse, float scala)
    {
        var s = Trova(idLuogo);
        if (s == null) { Destroy(figura); return; }
        Vector3 radiale = new Vector3(s.pos.x, 0f, s.pos.z);
        Vector3 dentro = radiale.sqrMagnitude > 0.01f ? -radiale.normalized : Vector3.forward;
        figura.transform.SetParent(transform, false);
        figura.transform.position = s.pos + dentro * verso_asse;
        figura.transform.rotation = Quaternion.LookRotation(dentro);
        figura.transform.localScale = Vector3.one * scala;
    }

    void PosizionaFigure()
    {
        Metti("acheronte", Figure.Caronte(), 3.2f, 1.0f);
        Metti("cerchio-3", Figure.Cerbero(), 2.5f, 0.9f);

        // Lucifero sta al centro, sul fondo della voragine, ritto: non ruotato verso l'asse
        var lu = Trova("lucifero");
        if (lu != null)
        {
            var f = Figure.Lucifero();
            f.transform.SetParent(transform, false);
            f.transform.position = new Vector3(0f, lu.pos.y, 0f);
            f.transform.localScale = Vector3.one * 0.85f;
            lu.go.GetComponent<MeshRenderer>().enabled = false; // il segnaposto resta cliccabile ma non si vede
        }
    }

    // vicino a Lucifero la camera sale e si allontana: la figura è alta e ha le ali larghe
    void AdattaCameraAFigure()
    {
        if (lucifero == null) return;
        Vector3 c = new Vector3(0f, lucifero.pos.y, 0f);
        float d = Vector3.Distance(dante.position, c);
        if (d < 32f)
        {
            float k = 1f - d / 32f;
            cam.distTarget = Mathf.Lerp(cam.distTarget, 50f, k);
            cam.pitchTarget = Mathf.Lerp(cam.pitchTarget, 18f, k);
            cam.bersaglio = dante.position + Vector3.up * (8f * k);
        }
    }

    // ---------- atmosfera: particelle diverse per ogni cerchio ----------

    void PreparaMaterialiParticelle()
    {
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 15.5f, dy = (y - 15.5f) / 15.5f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
            }
        tex.Apply();
        matParticelle = new Material(shaderParticelle) { mainTexture = tex };
        matScia = new Material(shaderParticelle) { mainTexture = tex };
    }

    void CreaAtmosfera()
    {
        for (int i = 0; i < bande.Count; i++)
        {
            var b = bande[i];
            float fine = i + 1 < bande.Count ? bande[i + 1].profondita : 1f;
            if (fine - b.profondita < 0.02f) continue;
            int idx = Mathf.Clamp(b.cerchio, 0, 9);
            CreaParticelle("cerchio " + idx, -b.profondita * H, -fine * H, b.raggio * R * 0.92f, TEMI[idx]);
        }
    }

    void CreaParticelle(string nome, float yAlto, float yBasso, float raggio, Tema t)
    {
        var go = new GameObject("Particelle " + nome);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(0, t.cade ? yAlto : yBasso, 0);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float altezza = Mathf.Abs(yAlto - yBasso);
        float vita = Mathf.Max(altezza / t.vel, 0.5f);

        var main = ps.main;
        main.loop = true; main.playOnAwake = false; main.duration = 5f;
        main.startLifetime = vita;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(t.dim * 0.6f, t.dim * 1.3f);
        main.startColor = t.particella;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Min(1200, Mathf.CeilToInt(t.rate * vita) + 20);

        var em = ps.emission; em.rateOverTime = t.rate;

        var sh = ps.shape;
        sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = raggio; sh.radiusThickness = 1f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0f);
        vel.y = new ParticleSystem.MinMaxCurve((t.cade ? -1f : 1f) * t.vel);
        vel.z = new ParticleSystem.MinMaxCurve(0f);
        if (t.vortice > 0f) { vel.orbitalY = new ParticleSystem.MinMaxCurve(t.vortice); }

        if (t.rumore > 0f)
        {
            var n = ps.noise; n.enabled = true; n.strength = t.rumore; n.frequency = 0.25f; n.quality = ParticleSystemNoiseQuality.Low;
        }

        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = matParticelle;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ps.Play();
        ps.Simulate(vita, true, false); ps.Play(); // parte già "a regime"
    }

    // ---------- logica ----------

    void Seleziona(Segnaposto s, bool muoviCamera)
    {
        selezionato = s;
        if (s != null && muoviCamera) cam.Vai(s.pos, 55f);
    }

    // vero se tra la camera e il punto c'è la parete della voragine
    bool Nascosto(Vector3 punto)
    {
        Vector3 da = cam.transform.position;
        Vector3 verso = (da - punto).normalized;
        return Physics.Linecast(da, punto + verso * 1.2f);
    }

    Segnaposto PiuVicinoAlTocco(Vector2 schermo, float soglia)
    {
        Segnaposto best = null; float bd = soglia;
        var c = cam.GetComponent<Camera>();
        foreach (var s in segnaposti)
        {
            Vector3 sp = c.WorldToScreenPoint(s.pos);
            if (sp.z <= 0 || Nascosto(s.pos)) continue;
            float d = ((Vector2)sp - schermo).magnitude;
            if (d < bd) { bd = d; best = s; }
        }
        return best;
    }

    Vector3 PuntoSulPercorso(float dist)
    {
        if (percorso.Count == 0) return Vector3.zero;
        dist = Mathf.Clamp(dist, 0f, lunghezze[lunghezze.Count - 1]);
        for (int i = 1; i < percorso.Count; i++)
            if (dist <= lunghezze[i])
            {
                float t = (dist - lunghezze[i - 1]) / Mathf.Max(lunghezze[i] - lunghezze[i - 1], 0.0001f);
                return Vector3.Lerp(percorso[i - 1], percorso[i], t);
            }
        return percorso[percorso.Count - 1];
    }

    Segnaposto PiuVicinoSulPercorso(float dist)
    {
        int best = 0; float bd = float.MaxValue;
        for (int i = 0; i < lunghezze.Count; i++)
        {
            float d = Mathf.Abs(lunghezze[i] - dist);
            if (d < bd) { bd = d; best = i; }
        }
        return perOrdine[best];
    }

    // ---------- viaggio cinematografico ----------

    void AvviaViaggio()
    {
        if (distPercorsa >= lunghezze[lunghezze.Count - 1] - 0.1f) { distPercorsa = 0f; sosta = 0f; }
        prossimo = 1;
        while (prossimo < lunghezze.Count && lunghezze[prossimo] <= distPercorsa + 0.01f) prossimo++;
        if (prossimo >= lunghezze.Count) { distPercorsa = 0f; prossimo = 1; }
        inViaggio = true;
        cam.automatica = true;
        cam.yawTarget = cam.yaw;
        if (selezionato == null) selezionato = perOrdine[Mathf.Max(prossimo - 1, 0)];
    }

    void SaltaAvanti()
    {
        if (!inViaggio) return;
        if (sosta > 0f) { sosta = 0f; return; }          // durante una sosta: riparte subito
        if (prossimo >= perOrdine.Count) return;
        distPercorsa = lunghezze[prossimo] - 0.001f;     // in cammino: arriva subito al prossimo luogo
    }

    float DurataSosta(Segnaposto s)
    {
        int n = scenePerLuogo.TryGetValue(s.luogo.id, out var l) ? l.Count : 0;
        return n == 0 ? 2.5f : Mathf.Clamp(n * SECONDI_PER_SCENA, 5f, 16f);
    }

    void AvanzaViaggio()
    {
        cam.bersaglio = dante.position; // la camera automatica si spegne da sola se l'utente la muove

        if (sosta > 0f)
        {
            // sosta in un luogo: la camera gira lentamente attorno a Dante, le scene scorrono
            sosta -= Time.deltaTime;
            // la camera sta sul lato dell'asse e guarda verso la parete: oscilla lentamente, senza uscire dalla voragine
            cam.yawTarget = YawEsterno(dante.position, cam.yawTarget) + 35f * Mathf.Sin(Time.time * 0.35f);
            cam.pitchTarget = 36f; cam.distTarget = DistanzaCamera(dante.position, 0.8f);
            AdattaCameraAFigure();
            return;
        }

        if (prossimo >= perOrdine.Count)
        {
            // arrivo in fondo: ritorno alla vista intera
            inViaggio = false; cam.automatica = false; selezionato = null;
            cam.Vai(new Vector3(0, -H * 0.45f, 0), 190f);
            cam.pitch = 55f;
            return;
        }

        float restante = lunghezze[prossimo] - distPercorsa;
        float fattore = 0.3f + 0.7f * Mathf.Clamp01(restante / 7f);   // rallenta in arrivo
        distPercorsa += VELOCITA * fattore * Time.deltaTime;

        if (distPercorsa >= lunghezze[prossimo])
        {
            distPercorsa = lunghezze[prossimo];
            dante.position = perOrdine[prossimo].pos;
            selezionato = perOrdine[prossimo];
            sosta = DurataSosta(selezionato);
            prossimo++;
            return;
        }

        dante.position = PuntoSulPercorso(distPercorsa);
        cam.bersaglio = dante.position;

        // inquadratura in movimento: dietro Dante, un po' dall'alto, guardando nella direzione di marcia
        // mescolata con lo sguardo verso l'asse della voragine (così si vede l'interno)
        Vector3 dir = perOrdine[prossimo].pos - dante.position;
        Vector3 esterno = new Vector3(dante.position.x, 0f, dante.position.z);   // dall'asse verso la parete
        Vector3 h = new Vector3(dir.x, 0f, dir.z);
        // la camera guarda soprattutto verso la parete (sta dal lato dell'asse, dentro la voragine),
        // con un po' di direzione di marcia
        Vector3 mix = (h.sqrMagnitude > 0.5f ? h.normalized : Vector3.zero) * 0.4f + (esterno.sqrMagnitude > 1f ? esterno.normalized : Vector3.zero) * 0.6f;
        if (mix.sqrMagnitude > 0.01f) cam.yawTarget = Mathf.Atan2(mix.x, mix.z) * Mathf.Rad2Deg;
        cam.pitchTarget = 32f + Mathf.Clamp(-dir.y, 0f, 20f) * 0.6f; // più alta quando scende ripida
        cam.distTarget = DistanzaCamera(dante.position, 0.9f);
        AdattaCameraAFigure();
    }

    // angolo della camera per guardare dall'asse verso la parete, nel punto dato
    static float YawEsterno(Vector3 p, float attuale)
    {
        if (p.x * p.x + p.z * p.z < 1f) return attuale;
        return Mathf.Atan2(p.x, p.z) * Mathf.Rad2Deg;
    }

    // distanza proporzionale al raggio della voragine in quel punto (più stretta in basso)
    static float DistanzaCamera(Vector3 p, float fattore)
    {
        float r = Mathf.Sqrt(p.x * p.x + p.z * p.z);
        return Mathf.Clamp(r * fattore, 14f, 42f);
    }

    float DistanzaDi(Segnaposto s)
    {
        int i = perOrdine.IndexOf(s);
        return i >= 0 ? lunghezze[i] : 0f;
    }

    void Update()
    {
        if (percorso.Count == 0) return;

        if (inViaggio) AvanzaViaggio();

        if (cam.ClicEseguito && !SuGui(cam.PosizioneClic))
        {
            float soglia = 45f * ScalaGui();
            var s = PiuVicinoAlTocco(cam.PosizioneClic, soglia);
            if (s != null)
            {
                inViaggio = false; cam.automatica = false;
                Seleziona(s, true); dante.position = s.pos; distPercorsa = DistanzaDi(s);
            }
        }

        // cambio di luogo: si riparte dalla prima scena; in viaggio le scene scorrono da sole
        if (selezionato != precedente) { precedente = selezionato; idxScena = 0; tCiclo = 0f; }
        if (inViaggio && selezionato != null && scenePerLuogo.TryGetValue(selezionato.luogo.id, out var sc) && sc.Count > 1)
        {
            tCiclo += Time.deltaTime;
            if (tCiclo > SECONDI_PER_SCENA) { tCiclo = 0f; idxScena = (idxScena + 1) % sc.Count; }
        }

        // evidenzia il selezionato; Lucifero pulsa
        foreach (var s in segnaposti)
        {
            bool sel = s == selezionato;
            float base_ = s.luogo.tipo == "zona" ? 1.3f : 2.0f;
            if (s == lucifero) base_ *= 1.6f + 0.35f * Mathf.Sin(Time.time * 2.2f);
            s.go.transform.localScale = Vector3.one * (base_ * (sel ? 1.8f : 1f));
        }
    }

    // ---------- tavole ----------

    Texture2D TexturaDi(RiferimentoScena rif)
    {
        if (rif.scena.tavole == null || rif.scena.tavole.Length == 0) return null;
        string url = UrlTavola(rif.scena.tavole[0]);
        if (texture.TryGetValue(url, out var t)) return t;
        if (!inCaricamento.Contains(url) && !fallite.Contains(url)) StartCoroutine(CaricaTexture(url));
        return null;
    }

    IEnumerator CaricaTexture(string url)
    {
        inCaricamento.Add(url);
        using (var rq = UnityWebRequestTexture.GetTexture(url, true))
        {
            yield return rq.SendWebRequest();
            if (rq.result == UnityWebRequest.Result.Success)
            {
                var t = DownloadHandlerTexture.GetContent(rq);
                texture[url] = t; ordineTexture.Enqueue(url);
                while (ordineTexture.Count > MAX_TEXTURE)
                {
                    string vecchia = ordineTexture.Dequeue();
                    if (texture.TryGetValue(vecchia, out var tv)) { Destroy(tv); texture.Remove(vecchia); }
                }
            }
            else fallite.Add(url);
        }
        inCaricamento.Remove(url);
    }

    // ---------- interfaccia (IMGUI, semplice e leggera) ----------

    Rect rBarra, rPannello;
    GUIStyle stTitolo, stTesto, stBottone, stEtichetta, stOmbra, stPiccolo;
    Vector2 scorri;

    float ScalaGui() { return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 600f, 1f, 2.6f); }

    bool SuGui(Vector2 posSchermo)
    {
        float s = ScalaGui();
        Vector2 p = new Vector2(posSchermo.x / s, (Screen.height - posSchermo.y) / s);
        return rBarra.Contains(p) || (selezionato != null && rPannello.Contains(p));
    }

    void Stili()
    {
        if (stTitolo != null) return;
        stTitolo = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
        stTesto = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        stPiccolo = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, fontStyle = FontStyle.Italic };
        stPiccolo.normal.textColor = new Color(0.8f, 0.78f, 0.7f);
        stBottone = new GUIStyle(GUI.skin.button) { fontSize = 13, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        stEtichetta = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
        stOmbra = new GUIStyle(stEtichetta);
        stEtichetta.normal.textColor = Color.white; stOmbra.normal.textColor = new Color(0, 0, 0, 0.85f);
    }

    // Il font integrato nel WebGL non ha trattini lunghi, apostrofi tipografici, ecc.: li sostituiamo.
    static string Pulisci(string t)
    {
        if (string.IsNullOrEmpty(t)) return t;
        return t.Replace('–', '-').Replace('—', '-').Replace('’', '\'').Replace('‘', '\'')
                .Replace('“', '"').Replace('”', '"').Replace("…", "...");
    }

    void OnGUI()
    {
        Stili();
        float s = ScalaGui();
        var m = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        float W = Screen.width / s, Hh = Screen.height / s;

        // etichette: il luogo scelto e i cerchi (saltando quelle che si sovrappongono)
        var c = cam.GetComponent<Camera>();
        var occupati = new List<Rect>();
        for (int pass = 0; pass < 2; pass++)
        foreach (var sp in segnaposti)
        {
            bool scelto = sp == selezionato;
            if (pass == 0 ? !scelto : (scelto || sp.luogo.tipo != "cerchio")) continue;
            Vector3 p = c.WorldToScreenPoint(sp.pos + Vector3.up * 2.5f);
            if (p.z <= 0 || Nascosto(sp.pos)) continue;
            string nome = Pulisci(sp.luogo.nome);
            var r = new Rect(p.x / s + 4, (Screen.height - p.y) / s - 10, stEtichetta.CalcSize(new GUIContent(nome)).x + 4, 20);
            if (selezionato != null && rPannello.Overlaps(r)) continue; // non sopra il pannello
            bool libero = true;
            foreach (var o in occupati) if (o.Overlaps(r)) { libero = false; break; }
            if (!libero) continue;
            occupati.Add(r);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), nome, stOmbra);
            GUI.Label(r, nome, stEtichetta);
        }

        // barra in alto
        bool largo = W >= 560f;
        rBarra = new Rect(8, 8, largo ? 520f : W - 16f, 44);
        GUI.Box(rBarra, GUIContent.none);
        float xTitolo = 0f;
        if (largo) { GUI.Label(new Rect(16, 12, 150, 36), "Inferno · 3D", stTitolo); xTitolo = 164f; }
        float bw = (rBarra.width - xTitolo - 8f - 12f) / 3f;       // tre pulsanti uguali
        float x0 = rBarra.x + xTitolo + 8f;
        if (percorso.Count > 0 && GUI.Button(new Rect(x0, 14, bw, 32), inViaggio ? "Pausa" : "Viaggio"))
        {
            if (inViaggio) { inViaggio = false; cam.automatica = false; }
            else AvviaViaggio();
        }
        GUI.enabled = inViaggio;
        if (GUI.Button(new Rect(x0 + bw + 6f, 14, bw, 32), "Avanti")) SaltaAvanti();
        GUI.enabled = true;
        if (GUI.Button(new Rect(x0 + 2f * (bw + 6f), 14, bw, 32), "Vista intera"))
        {
            inViaggio = false; cam.automatica = false; selezionato = null;
            cam.Vai(new Vector3(0, -H * 0.45f, 0), 190f);
        }
        if (inViaggio && prossimo <= perOrdine.Count)
            GUI.Label(new Rect(rBarra.x + 8f, rBarra.yMax + 2f, 260, 20), $"Tappa {Mathf.Min(prossimo, perOrdine.Count)} di {perOrdine.Count}", stPiccolo);

        if (stato != "") GUI.Label(new Rect(16, 62, W - 32, 40), stato, stTesto);

        // pannello del luogo, con la tavola della scena
        if (selezionato != null)
        {
            bool stretto = W < 560f;
            float pw = stretto ? W - 16 : 360f;
            float ph = stretto ? Mathf.Min(Hh * 0.58f, 420f) : Hh - 70f;
            rPannello = stretto ? new Rect(8, Hh - ph - 8, pw, ph) : new Rect(W - pw - 8, 60, pw, ph);
            GUI.Box(rPannello, GUIContent.none);
            GUILayout.BeginArea(new Rect(rPannello.x + 10, rPannello.y + 8, rPannello.width - 20, rPannello.height - 16));
            scorri = GUILayout.BeginScrollView(scorri);
            float w = rPannello.width - 44;

            GUILayout.Label(Pulisci(selezionato.luogo.nome), stTitolo);
            GUILayout.Label(Pulisci(selezionato.luogo.descrizione), stTesto);

            if (scenePerLuogo.TryGetValue(selezionato.luogo.id, out var lista) && lista.Count > 0)
            {
                idxScena = Mathf.Clamp(idxScena, 0, lista.Count - 1);
                var rif = lista[idxScena];
                GUILayout.Space(6);

                float ih = stretto ? 130f : Mathf.Min(w * 1.15f, Hh * 0.42f);
                var rImm = GUILayoutUtility.GetRect(w, ih);
                GUI.DrawTexture(rImm, Texture2D.blackTexture);
                var tex = TexturaDi(rif);
                if (tex != null) GUI.DrawTexture(rImm, tex, ScaleMode.ScaleToFit);
                else GUI.Label(rImm, "Carico la tavola…", stPiccolo);

                if (rif.scena.tavole != null && rif.scena.tavole.Length > 0)
                {
                    var tv = rif.scena.tavole[0];
                    string didascalia = tv.autore_id == "ia"
                        ? "Immagine generata con IA, nello stile di Doré. Non è un'opera di Doré."
                        : Pulisci(tv.autore + " · " + tv.titolo);
                    GUILayout.Label(didascalia, stPiccolo);
                }

                GUILayout.BeginHorizontal();
                GUI.enabled = lista.Count > 1;
                if (GUILayout.Button("<", stBottone, GUILayout.Width(40))) { idxScena = (idxScena + lista.Count - 1) % lista.Count; tCiclo = 0; }
                GUILayout.Label($"Scena {idxScena + 1} di {lista.Count}", stTesto);
                if (GUILayout.Button(">", stBottone, GUILayout.Width(40))) { idxScena = (idxScena + 1) % lista.Count; tCiclo = 0; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                GUILayout.Label($"Canto {rif.canto} · {Pulisci(rif.scena.titolo_scena)}", stEtichetta);
                GUILayout.Label(Pulisci(rif.scena.sintesi), stTesto);
                if (GUILayout.Button("Leggi la scena nel testo", stBottone))
                    Application.OpenURL(UrlLettura(rif.Indirizzo));
            }
            else if (scenePerLuogo.Count > 0) GUILayout.Label("Nessuna scena in questo luogo.", stTesto);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        GUI.matrix = m;
    }
}
