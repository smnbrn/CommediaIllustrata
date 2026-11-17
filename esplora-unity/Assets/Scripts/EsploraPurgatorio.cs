#if UNITY_WEBGL && !UNITY_EDITOR
#define UI_HTML   // nel browser barra e pannello sono HTML (esplora-purgatorio/index.html); nell'editor resta una barra minima
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// La montagna del Purgatorio. Come per l'Inferno nessun dato sta nel codice: luoghi, scene e tavole si leggono da
// data/luoghi-purgatorio.json e data/purgatorio/canto-NN.json. Il GameObject si chiama "Esplora" perché la pagina HTML
// gli manda i comandi con SendMessage("Esplora", "Comando", ...).
public class EsploraPurgatorio : MonoBehaviour
{
    const int NUM_CANTI = 33;
    const int MAX_TEXTURE = 8;
    const string SITO = "https://smnbrn.github.io/CommediaIllustrata/";
    const float VELOCITA = 18f;
    const float SECONDI_PER_SCENA = 4.5f;

    class Segnaposto { public Luogo luogo; public int indice; public Vector3 pos; public Material mat; public GameObject go; public Color colore; }

    CameraOrbita cam;
    Camera camera_;
    Shader shader, shaderParticelle;
    Material matParticelle, matScia, matColore;
    Monte monte;
    readonly List<Segnaposto> segnaposti = new List<Segnaposto>();
    readonly Dictionary<string, List<RiferimentoScena>> scenePerLuogo = new Dictionary<string, List<RiferimentoScena>>();
    Segnaposto selezionato, precedente;
    readonly List<Bounds> ingombri = new List<Bounds>();   // rocce, alberi, figure ecc.: la camera non ci entra
    bool InIngombro(Vector3 p) { for (int i = 0; i < ingombri.Count; i++) if (ingombri[i].Contains(p)) return true; return false; }
    string stato = "Carico i dati…";

    // viaggio di Dante lungo la scala
    readonly List<Vector3> percorso = new List<Vector3>();
    readonly List<float> lunghezze = new List<float>();
    readonly List<int> indiciLuoghi = new List<int>();
    Transform dante;
    float distPercorsa;
    bool inViaggio;
    int prossimo = 1;
    float sosta;
    Dictionary<string, GameObject> protagonisti = new Dictionary<string, GameObject>();
    Material matCielo, matAcqua;
    Transform cupola;

    // suono procedurale: tre atmosfere (mare, monte, giardino) generate al volo, nessun file da scaricare
    AudioSource[] voci; AudioClip[] tracce; bool suonoAvviato, suonoAcceso = true; int vocePrincipale, tracciaCorrente = -1;

    // tavole
    readonly Dictionary<string, Texture2D> texture = new Dictionary<string, Texture2D>();
    readonly Queue<string> ordineTexture = new Queue<string>();
    readonly HashSet<string> inCaricamento = new HashSet<string>();
    readonly HashSet<string> fallite = new HashSet<string>();
    int idxScena; float tCiclo;

    // ---------- indirizzi ----------

    string RadiceSito()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return new Uri(new Uri(Application.absoluteURL), "../").ToString();
#else
        return "file:///" + Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")).Replace('\\', '/') + "/";
#endif
    }

    string UrlTavola(Tavola t)
    {
        string u = string.IsNullOrEmpty(t.miniatura) ? t.url : t.miniatura;
        bool stretto = Screen.width / ScalaGui() < 560f || Screen.height > Screen.width;
        if (stretto) u = u.Replace("/960px-", "/500px-").Replace("-960.jpg", "-500.jpg");
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
        c.backgroundColor = new Color(0.86f, 0.76f, 0.82f);
        c.farClipPlane = 4000f;
        c.nearClipPlane = 0.3f;
        if (c.GetComponent<AudioListener>() == null) c.gameObject.AddComponent<AudioListener>();
        camera_ = c;
        cam = c.GetComponent<CameraOrbita>();
        if (cam == null) cam = c.gameObject.AddComponent<CameraOrbita>();
        cam.bersaglio = new Vector3(0, Monte.H * 0.45f, 0);
        cam.distanza = 330f; cam.yaw = 205f; cam.pitch = 20f;
        cam.distMin = 6f; cam.distMax = 620f; cam.raggioMax = 190f; cam.yMin = 0.5f; cam.yMax = Monte.H + 40f;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.00045f;
    }

    IEnumerator Start()
    {
        string baseUrl = RadiceSito() + "data/";
        FileLuoghi fl = null;
        using (var rq = UnityWebRequest.Get(baseUrl + "luoghi-purgatorio.json"))
        {
            yield return rq.SendWebRequest();
            if (rq.result != UnityWebRequest.Result.Success) { stato = "Errore: luoghi-purgatorio.json non si carica (" + rq.error + ")"; yield break; }
            fl = JsonUtility.FromJson<FileLuoghi>(rq.downloadHandler.text);
        }
        Costruisci(fl.luoghi);
        stato = "";

        for (int n = 1; n <= NUM_CANTI; n++)
        {
            using (var rq = UnityWebRequest.Get($"{baseUrl}purgatorio/canto-{n:00}.json"))
            {
                yield return rq.SendWebRequest();
                if (rq.result != UnityWebRequest.Result.Success) continue;
                var fc = JsonUtility.FromJson<FileCanto>(rq.downloadHandler.text);
                if (fc == null || fc.scene == null) continue;
                foreach (var s in fc.scene)
                {
                    if (string.IsNullOrEmpty(s.luogo_id)) continue;
                    if (!scenePerLuogo.TryGetValue(s.luogo_id, out var lista)) scenePerLuogo[s.luogo_id] = lista = new List<RiferimentoScena>();
                    lista.Add(new RiferimentoScena { canto = n, scena = s, cantica = "purgatorio" });
                }
            }
        }
    }

    // ---------- costruzione ----------

    Material NuovoMateriale(Color c) { var m = new Material(shader); m.SetColor("_Color", c); return m; }

    GameObject Oggetto(string nome, Transform padre, Mesh mesh, Material mat, bool collider)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    void Costruisci(Luogo[] luoghi)
    {
        monte = new Monte(luoghi);
        matColore = NuovoMateriale(Color.white);
        cam.Ostacolo = p => monte.Solido(p, 1.2f) || InIngombro(p);

        // il monte (con collider: nasconde le etichette dietro la roccia), le scale, il mare
        var matSuolo = TexturaTerreno.Materiale() ?? matColore;
        Oggetto("Monte", transform, monte.MeshMonte(), matSuolo, true);
        // (il nastro chiaro della scala nella parete, monte.MeshScale(), è tolto: da vicino si vedeva come frammenti sottili sotto la linea)
        var shAcqua = Resources.Load<Shader>("Acqua");
        matAcqua = shAcqua != null ? new Material(shAcqua) : matColore;
        Oggetto("Mare", transform, monte.MeshMare(), matAcqua, false);

        // cielo: una cupola che segue la camera
        var shCielo = Resources.Load<Shader>("Cielo");
        if (shCielo != null)
        {
            matCielo = new Material(shCielo);
            var cu = new GameObject("Cielo"); cupola = cu.transform; cu.transform.SetParent(transform, false);
            cu.AddComponent<MeshFilter>().sharedMesh = Forme.Sfera();
            cu.AddComponent<MeshRenderer>().sharedMaterial = matCielo;
            cu.transform.localScale = Vector3.one * 3000f;
        }

        // segnaposti
        for (int i = 0; i < monte.liv.Count; i++)
        {
            var lv = monte.liv[i];
            var go = Forme.Crea(lv.luogo.id, Forme.Sfera());
            go.transform.SetParent(transform, false);
            Vector3 pos = lv.angolo == 0 && false ? Vector3.zero : new Vector3(Mathf.Cos(lv.angolo * Mathf.Deg2Rad) * lv.centro, lv.y + 1.4f, Mathf.Sin(lv.angolo * Mathf.Deg2Rad) * lv.centro);
            go.transform.position = pos;
            var colore = lv.luogo.tipo == "cornice" ? new Color(0.78f, 0.62f, 1f) : lv.luogo.tipo == "soglia" ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.62f, 0.95f, 0.6f);
            var mat = NuovoMateriale(colore);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            segnaposti.Add(new Segnaposto { luogo = lv.luogo, indice = i, pos = pos, mat = mat, go = go, colore = colore });
        }

        // scenografia: modelli e figure
        var effetti = new Dictionary<string, Vector3>();
        protagonisti = AmbientiPurgatorio.Costruisci(transform, monte, matColore, effetti, ingombri);

        // percorso: dal centro di un ripiano alla scala fino al successivo
        monte.Percorso(percorso, indiciLuoghi);
        AddolciscePercorso();
        lunghezze.Clear();
        float tot = 0f;
        for (int i = 0; i < percorso.Count; i++)
        {
            if (i > 0) tot += Vector3.Distance(percorso[i - 1], percorso[i]);
            lunghezze.Add(tot);
        }
        for (int i = 0; i < segnaposti.Count && i < indiciLuoghi.Count; i++) { segnaposti[i].pos = percorso[indiciLuoghi[i]] + Vector3.up * 1.0f; segnaposti[i].go.transform.position = segnaposti[i].pos; }
        var lr = new GameObject("Percorso").AddComponent<LineRenderer>();
        lr.transform.SetParent(transform, false);
        lr.positionCount = percorso.Count; lr.SetPositions(percorso.ToArray());
        lr.widthMultiplier = 0.45f; lr.useWorldSpace = true; lr.numCornerVertices = 6; lr.numCapVertices = 4;
        lr.sharedMaterial = NuovoMateriale(new Color(1f, 0.93f, 0.55f));
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        PreparaMaterialiParticelle();
        Atmosfera(effetti);

        // Dante, con la sua scia dorata
        var d = Forme.Crea("Dante", Forme.Sfera());
        d.transform.SetParent(transform, false);
        d.GetComponent<MeshRenderer>().sharedMaterial = NuovoMateriale(new Color(1f, 1f, 0.9f));
        dante = d.transform; dante.position = percorso[0];
        var scia = d.AddComponent<TrailRenderer>();
        scia.time = 3f; scia.widthMultiplier = 2.2f; scia.minVertexDistance = 0.3f;
        scia.sharedMaterial = matScia;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f), new GradientColorKey(new Color(0.8f, 0.6f, 1f), 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        scia.colorGradient = g;
        scia.widthCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
    }

    // Spezzata → curva morbida (Catmull-Rom) che passa per tutti i punti, luoghi compresi: niente spigoli alle svolte.
    // Gli indici dei luoghi vengono aggiornati sulla nuova curva.
    void AddolciscePercorso()
    {
        // 1) angoli arrotondati (taglio degli spigoli di Chaikin, 3 passate); 2) campionamento regolare ogni ~0.7 unità;
        // 3) ogni luogo prende il punto della curva più vicino alla sua posizione originale
        var orig = new List<Vector3>(percorso); var luoghi = new List<Vector3>(); foreach (int v in indiciLuoghi) luoghi.Add(orig[v]);
        var pts = new List<Vector3>(orig);
        for (int it = 0; it < 3; it++)
        {
            var q = new List<Vector3> { pts[0] };
            for (int k = 0; k < pts.Count - 1; k++) { q.Add(Vector3.Lerp(pts[k], pts[k + 1], 0.25f)); q.Add(Vector3.Lerp(pts[k], pts[k + 1], 0.75f)); }
            q.Add(pts[pts.Count - 1]); pts = q;
        }
        var nuovo = new List<Vector3> { pts[0] };
        float passo = 0.7f, resto = passo;
        for (int k = 1; k < pts.Count; k++)
        {
            Vector3 a = nuovo[nuovo.Count - 1], b = pts[k]; float d = Vector3.Distance(a, b);
            while (d >= resto) { a = Vector3.Lerp(a, b, resto / d); nuovo.Add(a); d = Vector3.Distance(a, b); resto = passo; }
            resto -= d;
        }
        nuovo.Add(pts[pts.Count - 1]);
        // la curva non scende mai sotto il terreno
        for (int k = 0; k < nuovo.Count; k++)
        {
            var p = nuovo[k]; float h = monte.Altezza(new Vector2(p.x, p.z).magnitude) + 0.35f;
            if (p.y < h) { p.y = h; nuovo[k] = p; }
        }
        percorso.Clear(); percorso.AddRange(nuovo);
        indiciLuoghi.Clear();
        int ultimo = 0;
        foreach (var lp in luoghi)
        {
            int best = ultimo; float bd = float.MaxValue;
            for (int k = ultimo; k < nuovo.Count; k++) { float d = (nuovo[k] - lp).sqrMagnitude; if (d < bd) { bd = d; best = k; } }
            indiciLuoghi.Add(best); ultimo = best;
        }
    }

    // ---------- atmosfera: polline nel giardino, fumo nella terza cornice, fiamme nella settima, lucciole nella valletta ----------

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

    ParticleSystem Emettitore(string nome, Vector3 pos, float raggio, Color colore, float dim, float velSu, float rate, float vita, float rumore, bool cono = false, float spessore = 1f)
    {
        var go = new GameObject("Particelle " + nome);
        go.transform.SetParent(transform, false);
        go.transform.position = pos; go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = false; main.duration = 5f;
        main.startLifetime = vita; main.startSpeed = velSu;
        main.startSize = new ParticleSystem.MinMaxCurve(dim * 0.6f, dim * 1.3f);
        main.startColor = colore;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Min(1500, Mathf.CeilToInt(rate * vita) + 20);
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.enabled = true;
        if (cono) { sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 14f; sh.radius = raggio; }
        else { sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = raggio; sh.radiusThickness = Mathf.Clamp(spessore, 0.02f, 1f); }
        if (rumore > 0f) { var n = ps.noise; n.enabled = true; n.strength = rumore; n.frequency = 0.3f; n.quality = ParticleSystemNoiseQuality.Low; }
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = matParticelle; rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play(); ps.Simulate(vita, true, false); ps.Play();
        return ps;
    }

    void Atmosfera(Dictionary<string, Vector3> effetti)
    {
        // giardino in cima: polline dorato
        var eden = monte.liv[monte.liv.Count - 1];
        Emettitore("polline", new Vector3(0, eden.y + 0.5f, 0), eden.rEst * 0.9f, new Color(1f, 0.95f, 0.6f, 0.9f), 0.35f, 0.8f, 60, 8f, 0.6f);
        // valletta: lucciole
        var vl = monte.liv[4];
        Emettitore("lucciole", new Vector3(0, vl.y + 0.4f, 0), vl.rEst, new Color(0.8f, 1f, 0.5f, 0.9f), 0.35f, 0.5f, 70, 6f, 0.8f, false, vl.larghezza / vl.rEst);
        // terza cornice: fumo scuro che sale dal ripiano
        var ir = monte.liv[8];
        Emettitore("fumo", new Vector3(0, ir.y + 0.4f, 0), ir.rEst, new Color(0.35f, 0.33f, 0.36f, 0.6f), 2.6f, 1.2f, 70, 6f, 1f, false, ir.larghezza / ir.rEst);
        // settima cornice: muro di fiamme sul bordo esterno
        var lu = monte.liv[12];
        Emettitore("fiamme", new Vector3(0, lu.y + 0.2f, 0), lu.rEst, new Color(1f, 0.5f, 0.12f, 0.95f), 1.5f, 3.4f, 520, 1.5f, 0.9f, false, lu.larghezza / lu.rEst);
        Emettitore("fiamme-nucleo", new Vector3(0, lu.y + 0.2f, 0), lu.rEst - 0.5f, new Color(1f, 0.85f, 0.3f, 0.95f), 0.8f, 3f, 300, 1.0f, 0.5f, false, (lu.larghezza - 1f) / lu.rEst);
        // spiaggia: pulviscolo di mare
        Emettitore("brezza", new Vector3(0, 1.5f, 0), Monte.R * 0.95f, new Color(1f, 1f, 1f, 0.35f), 0.4f, 0.4f, 40, 8f, 1f);
    }

    // ---------- logica ----------

    void Seleziona(Segnaposto s, bool muoviCamera)
    {
        selezionato = s;
        if (s != null && muoviCamera) cam.Vai(s.pos, 45f);
    }

    bool Nascosto(Vector3 punto)
    {
        Vector3 da = cam.transform.position;
        Vector3 verso = (da - punto).normalized;
        return Physics.Linecast(da, punto + verso * 1.2f);
    }

    Segnaposto PiuVicinoAlTocco(Vector2 schermo, float soglia)
    {
        Segnaposto best = null; float bd = soglia;
        foreach (var s in segnaposti)
        {
            Vector3 sp = camera_.WorldToScreenPoint(s.pos);
            if (sp.z <= 0 || Nascosto(s.pos)) continue;
            float d = ((Vector2)sp - schermo).magnitude;
            if (d < bd) { bd = d; best = s; }
        }
        return best;
    }

    Vector3 PuntoSulPercorso(float dist)
    {
        dist = Mathf.Clamp(dist, 0f, lunghezze[lunghezze.Count - 1]);
        for (int i = 1; i < percorso.Count; i++)
            if (dist <= lunghezze[i])
            {
                float t = (dist - lunghezze[i - 1]) / Mathf.Max(lunghezze[i] - lunghezze[i - 1], 0.0001f);
                return Vector3.Lerp(percorso[i - 1], percorso[i], t);
            }
        return percorso[percorso.Count - 1];
    }

    float DistanzaDi(Segnaposto s) { return lunghezze[indiciLuoghi[s.indice]]; }

    // ---------- viaggio ----------

    void AvviaViaggio()
    {
        float fine = lunghezze[lunghezze.Count - 1];
        if (distPercorsa >= fine - 0.1f) { distPercorsa = 0f; sosta = 0f; }
        prossimo = 0;
        while (prossimo < indiciLuoghi.Count && lunghezze[indiciLuoghi[prossimo]] <= distPercorsa + 0.01f) prossimo++;
        if (prossimo >= indiciLuoghi.Count) { distPercorsa = 0f; prossimo = 1; }
        inViaggio = true;
        cam.automatica = true; cam.yawTarget = cam.yaw;
        if (selezionato == null) selezionato = segnaposti[Mathf.Max(prossimo - 1, 0)];
    }

    void SaltaAvanti()
    {
        if (!inViaggio) return;
        if (sosta > 0f) { sosta = 0f; return; }
        if (prossimo >= indiciLuoghi.Count) return;
        distPercorsa = lunghezze[indiciLuoghi[prossimo]] - 0.001f;
    }

    float DurataSosta(Segnaposto s)
    {
        int n = scenePerLuogo.TryGetValue(s.luogo.id, out var l) ? l.Count : 0;
        return n == 0 ? 2.5f : Mathf.Clamp(n * SECONDI_PER_SCENA, 5f, 16f);
    }

    // la camera sta fuori dal monte e guarda verso l'asse: yaw che porta lo sguardo dal punto p verso il centro
    static float YawVersoAsse(Vector3 p, float attuale)
    {
        if (p.x * p.x + p.z * p.z < 1f) return attuale;
        return Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg;
    }

    GameObject FiguraDi(Segnaposto s)
    {
        if (s == null) return null;
        string id = s.luogo.id;
        foreach (var chiave in new[] { id + "#catone", id + "#angelo", id + "#matelda", id + "#serpente" })
            if (protagonisti.TryGetValue(chiave, out var g) && g != null) return g;
        return null;
    }

    void AvanzaViaggio()
    {
        cam.bersaglio = dante.position;
        if (sosta > 0f)
        {
            sosta -= Time.deltaTime;
            cam.yawTarget = YawVersoAsse(dante.position, cam.yawTarget) + 30f * Mathf.Sin(Time.time * 0.35f);
            cam.pitchTarget = 27f; cam.distTarget = 38f;
            var fig = FiguraDi(selezionato);
            if (fig != null)
            {
                var b = Props.Limiti(fig);
                cam.bersaglio = Vector3.Lerp(dante.position, b.center, 0.5f);
                cam.distTarget = Mathf.Clamp(b.size.magnitude * 2.6f, 24f, 40f);
            }
            return;
        }
        if (prossimo >= indiciLuoghi.Count)
        {
            inViaggio = false; cam.automatica = false; selezionato = null;
            VistaIntera();
            return;
        }
        float fineTappa = lunghezze[indiciLuoghi[prossimo]];
        float restante = fineTappa - distPercorsa;
        float fattore = 0.35f + 0.65f * Mathf.Clamp01(restante / 8f);
        distPercorsa += VELOCITA * fattore * Time.deltaTime;
        if (distPercorsa >= fineTappa)
        {
            distPercorsa = fineTappa;
            dante.position = percorso[indiciLuoghi[prossimo]];
            selezionato = segnaposti[prossimo];
            sosta = DurataSosta(selezionato);
            prossimo++;
            return;
        }
        dante.position = PuntoSulPercorso(distPercorsa);
        cam.bersaglio = dante.position;
        Vector3 avanti = PuntoSulPercorso(distPercorsa + 3f) - dante.position;
        Vector3 vs = new Vector3(avanti.x, 0f, avanti.z);
        float yaw = YawVersoAsse(dante.position, cam.yawTarget);
        if (vs.sqrMagnitude > 0.3f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(vs.x, vs.z) * Mathf.Rad2Deg, 0.25f);
        cam.yawTarget = yaw;
        cam.pitchTarget = 29f + Mathf.Clamp(avanti.y, 0f, 6f);
        cam.distTarget = 48f;
    }

    // per le prove: porta la camera su un luogo (indice di salita)
    public void VaiA(int i)
    {
        if (i < 0 || i >= segnaposti.Count) return;
        var sp = segnaposti[i];
        inViaggio = false; cam.automatica = false;
        Seleziona(sp, true); dante.position = sp.pos - Vector3.up; distPercorsa = DistanzaDi(sp);
        cam.yaw = YawVersoAsse(sp.pos, cam.yaw); cam.pitch = 22f; cam.distanza = 28f;
    }

    void VistaIntera()
    {
        cam.Vai(new Vector3(0, Monte.H * 0.45f, 0), 330f);
        cam.pitch = 20f;
    }

    void Update()
    {
        InviaUi();
        if (percorso.Count == 0) return;
        if (inViaggio && cam.Mosso) { inViaggio = false; cam.automatica = false; }
        if (inViaggio) AvanzaViaggio();
        AggiornaAtmosfera();
        AggiornaSuono();

        if (cam.ClicEseguito && !SuGui(cam.PosizioneClic))
        {
            var s = PiuVicinoAlTocco(cam.PosizioneClic, 45f * ScalaGui());
            if (s != null)
            {
                inViaggio = false; cam.automatica = false;
                Seleziona(s, true); dante.position = s.pos - Vector3.up * 1f; distPercorsa = DistanzaDi(s);
            }
        }

        if (selezionato != precedente) { precedente = selezionato; idxScena = 0; tCiclo = 0f; }
        if (inViaggio && selezionato != null && scenePerLuogo.TryGetValue(selezionato.luogo.id, out var sc) && sc.Count > 1)
        {
            tCiclo += Time.deltaTime;
            if (tCiclo > SECONDI_PER_SCENA) { tCiclo = 0f; idxScena = (idxScena + 1) % sc.Count; }
        }

        float distCam = Vector3.Distance(cam.transform.position, dante.position);
        dante.localScale = Vector3.one * 3.0f * Mathf.Clamp(distCam / 120f, 0.35f, 1.3f);
        foreach (var s in segnaposti)
        {
            bool sel = s == selezionato;
            float vicino = Mathf.Clamp(Vector3.Distance(cam.transform.position, s.go.transform.position) / 120f, 0.35f, 1.3f);
            s.go.transform.localScale = Vector3.one * (2.2f * vicino * (sel ? 1.8f : 1f));
        }
    }

    // ---------- cielo, nebbia, suono ----------

    static readonly Color ALBA = new Color(0.90f, 0.75f, 0.78f), GIORNO = new Color(0.74f, 0.82f, 0.94f), ORO = new Color(0.95f, 0.90f, 0.72f);
    static readonly Color ZENIT_BASSO = new Color(0.40f, 0.52f, 0.80f), ZENIT_ALTO = new Color(0.35f, 0.58f, 0.90f);

    void AggiornaAtmosfera()
    {
        float t = Mathf.Clamp01(cam.bersaglio.y / Monte.H);
        Color orizzonte = t < 0.5f ? Color.Lerp(ALBA, GIORNO, t * 2f) : Color.Lerp(GIORNO, ORO, (t - 0.5f) * 2f);
        Color zenit = Color.Lerp(ZENIT_BASSO, ZENIT_ALTO, t);
        RenderSettings.fogColor = orizzonte;
        camera_.backgroundColor = orizzonte;
        if (matCielo != null) { matCielo.SetColor("_Orizzonte", orizzonte); matCielo.SetColor("_Zenit", zenit); }
        if (matAcqua != null && matAcqua.HasProperty("_Orizzonte")) matAcqua.SetColor("_Orizzonte", orizzonte);
        if (cupola != null) cupola.position = cam.transform.position;
    }

    // tre atmosfere: 0 mare e spiaggia, 1 il monte, 2 il giardino
    AudioClip GeneraTraccia(int tipo)
    {
        const int FREQ = 22050, SEC = 24, N = FREQ * SEC;
        var dati = new float[N];
        float[] note = tipo == 0 ? new[] { 110f, 164.8f, 220f } : tipo == 1 ? new[] { 98f, 146.8f, 196f, 293.7f } : new[] { 130.8f, 196f, 261.6f, 329.6f, 392f };
        var rnd = new System.Random(77 + tipo);
        for (int k = 0; k < note.Length; k++)
        {
            float f = Mathf.Round(note[k] * SEC) / SEC;      // numero intero di cicli: il ciclo si chiude senza scatto
            float fase = (float)rnd.NextDouble() * 6.28f;
            float lfo = Mathf.Round((0.07f + 0.05f * k) * SEC) / SEC;
            float amp = 0.12f / (1f + k * 0.4f);
            for (int i = 0; i < N; i++)
            {
                float t = i / (float)FREQ;
                float env = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * lfo * t + fase);
                dati[i] += amp * env * (Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * f * t + 1f));
            }
        }
        if (tipo == 0)
        {   // onde: rumore filtrato con ampiezza lenta (si chiude con una dissolvenza)
            float y = 0f; var onda = new float[N];
            for (int i = 0; i < N; i++)
            {
                y += ((float)rnd.NextDouble() * 2f - 1f - y) * 0.08f;
                float t = i / (float)FREQ;
                float lenta = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (4f / SEC) * t);
                onda[i] = y * lenta * 0.55f;
            }
            int fade = FREQ * 2;
            for (int i = 0; i < fade; i++) { float a = i / (float)fade; onda[i] = onda[i] * a + onda[N - fade + i] * (1f - a); }
            for (int i = 0; i < N; i++) dati[i] += onda[i];
        }
        else
        {   // rintocchi: note singole che si spengono, a intervalli regolari
            float[] camp = tipo == 1 ? new[] { 392f, 440f, 349.2f } : new[] { 523.3f, 659.3f, 784f, 880f };
            for (int b = 0; b < 6; b++)
            {
                float f = camp[(b * 5 + tipo) % camp.Length];
                int inizio = (int)(b * (N / 6f) + FREQ * 0.4f);
                for (int i = 0; i < FREQ * 3 && inizio + i < N; i++)
                {
                    float t = i / (float)FREQ;
                    dati[inizio + i] += 0.07f * Mathf.Exp(-t * 1.6f) * Mathf.Sin(2f * Mathf.PI * f * t);
                }
            }
        }
        float massimo = 0.0001f; for (int i = 0; i < N; i++) massimo = Mathf.Max(massimo, Mathf.Abs(dati[i]));
        for (int i = 0; i < N; i++) dati[i] *= 0.8f / massimo;
        var clip = AudioClip.Create("Purgatorio" + tipo, N, 1, FREQ, false);
        clip.SetData(dati, 0);
        return clip;
    }

    void AggiornaSuono()
    {
        if (tracce == null)
        {
            tracce = new AudioClip[3];
            voci = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Voce " + i); go.transform.SetParent(transform, false);
                voci[i] = go.AddComponent<AudioSource>();
                voci[i].loop = true; voci[i].playOnAwake = false; voci[i].spatialBlend = 0f; voci[i].volume = 0f;
            }
        }
        if (!suonoAvviato)
        {
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown) suonoAvviato = true; else return;
        }
        int livello = 0; float best = float.MaxValue;
        for (int i = 0; i < monte.liv.Count; i++) { float d = Mathf.Abs(monte.liv[i].y - cam.bersaglio.y); if (d < best) { best = d; livello = i; } }
        int tr = livello <= 3 ? 0 : livello == 13 ? 2 : 1;
        if (tr != tracciaCorrente && tracce[tr] == null) tracce[tr] = GeneraTraccia(tr);   // si genera solo quando serve
        if (tr != tracciaCorrente && tracce[tr] != null)
        {
            vocePrincipale = 1 - vocePrincipale;
            voci[vocePrincipale].clip = tracce[tr]; voci[vocePrincipale].Play();
            tracciaCorrente = tr;
        }
        for (int i = 0; i < 2; i++)
        {
            float obiettivo = (i == vocePrincipale && suonoAcceso) ? 0.5f : 0f;
            voci[i].volume = Mathf.MoveTowards(voci[i].volume, obiettivo, Time.deltaTime * 0.3f);
            if (voci[i].isPlaying && voci[i].volume <= 0.001f && i != vocePrincipale) voci[i].Stop();
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

    // ---------- ponte con l'interfaccia HTML (solo nel browser) ----------

    [Serializable]
    class StatoUi
    {
        public string stato = "", nome = "", descrizione = "", titolo = "", sintesi = "", url = "", didascalia = "", indirizzo = "";
        public bool haPercorso, viaggio, suono, haLuogo, haScene, ia;
        public int tappa, totale, scena, numScene, canto;
    }
    string ultimoStatoUi = ""; float tempoUi;

#if UI_HTML
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void EsploraUiAggiorna(string json);
#endif

    void InviaUi()
    {
#if UI_HTML
        if (Time.unscaledTime - tempoUi < 0.1f) return;
        tempoUi = Time.unscaledTime;
        var u = new StatoUi { stato = stato, haPercorso = percorso.Count > 0, viaggio = inViaggio, suono = suonoAcceso, totale = segnaposti.Count };
        if (inViaggio && prossimo <= segnaposti.Count) u.tappa = Mathf.Min(prossimo, segnaposti.Count);
        if (selezionato != null)
        {
            u.haLuogo = true; u.nome = selezionato.luogo.nome ?? ""; u.descrizione = selezionato.luogo.descrizione ?? "";
            if (scenePerLuogo.TryGetValue(selezionato.luogo.id, out var lista) && lista.Count > 0)
            {
                idxScena = Mathf.Clamp(idxScena, 0, lista.Count - 1);
                var rif = lista[idxScena];
                u.haScene = true; u.scena = idxScena + 1; u.numScene = lista.Count; u.canto = rif.canto;
                u.titolo = rif.scena.titolo_scena ?? ""; u.sintesi = rif.scena.sintesi ?? ""; u.indirizzo = rif.Indirizzo;
                if (rif.scena.tavole != null && rif.scena.tavole.Length > 0)
                {
                    var tv = rif.scena.tavole[0];
                    u.url = UrlTavola(tv); u.ia = tv.autore_id == "ia";
                    u.didascalia = u.ia ? "Immagine generata con IA, nello stile di Doré. Non è un'opera di Doré." : tv.autore + " · " + tv.titolo;
                }
            }
        }
        string json = JsonUtility.ToJson(u);
        if (json == ultimoStatoUi) return;
        ultimoStatoUi = json;
        EsploraUiAggiorna(json);
#endif
    }

    public void Comando(string c)
    {
        suonoAvviato = true;
        switch (c)
        {
            case "viaggio":
                if (percorso.Count == 0) break;
                if (inViaggio) { inViaggio = false; cam.automatica = false; } else AvviaViaggio();
                break;
            case "avanti": SaltaAvanti(); break;
            case "intera": inViaggio = false; cam.automatica = false; selezionato = null; VistaIntera(); break;
            case "suono": suonoAcceso = !suonoAcceso; break;
            case "chiudi": selezionato = null; break;
            case "prec": case "succ":
                if (selezionato != null && scenePerLuogo.TryGetValue(selezionato.luogo.id, out var l) && l.Count > 1)
                { idxScena = (idxScena + (c == "succ" ? 1 : l.Count - 1)) % l.Count; tCiclo = 0f; }
                break;
        }
        tempoUi = -1f;
    }

    // ---------- interfaccia nell'editor (barra minima) e etichette ----------

    GUIStyle stEtichetta, stOmbra;
    float ScalaGui() { return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / (Input.touchSupported ? 400f : 600f), 1f, 2.6f); }

    bool SuGui(Vector2 posSchermo)
    {
#if UI_HTML
        return false;
#else
        return posSchermo.y > Screen.height - 50f * ScalaGui();
#endif
    }

    static string Pulisci(string t)
    {
        if (string.IsNullOrEmpty(t)) return t;
        return t.Replace('–', '-').Replace('—', '-').Replace('’', '\'').Replace('‘', '\'').Replace('“', '"').Replace('”', '"').Replace("…", "...");
    }

    void OnGUI()
    {
        if (stEtichetta == null)
        {
            stEtichetta = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, wordWrap = false };
            stOmbra = new GUIStyle(stEtichetta);
            stEtichetta.normal.textColor = Color.white; stOmbra.normal.textColor = new Color(0.15f, 0.05f, 0.25f, 0.9f);
        }
        float s = ScalaGui();
        var m = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
        if (camera_ != null)
        {
            var occupati = new List<Rect>();
            for (int pass = 0; pass < 2; pass++)
                foreach (var sp in segnaposti)
                {
                    bool scelto = sp == selezionato;
                    if (pass == 0 ? !scelto : (scelto || sp.luogo.tipo != "cornice")) continue;
                    Vector3 p = camera_.WorldToScreenPoint(sp.pos + Vector3.up * 2.5f);
                    if (p.z <= 0 || Nascosto(sp.pos)) continue;
                    string nome = Pulisci(sp.luogo.nome);
                    var r = new Rect(p.x / s + 4, (Screen.height - p.y) / s - 10, stEtichetta.CalcSize(new GUIContent(nome)).x + 4, 20);
                    bool libero = true;
                    foreach (var o in occupati) if (o.Overlaps(r)) { libero = false; break; }
                    if (!libero) continue;
                    occupati.Add(r);
                    GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), nome, stOmbra);
                    GUI.Label(r, nome, stEtichetta);
                }
        }
#if !UI_HTML
        float W = Screen.width / s, Hh = Screen.height / s;
        GUI.Box(new Rect(8, Hh - 44, W - 16, 36), GUIContent.none);
        if (percorso.Count > 0 && GUI.Button(new Rect(14, Hh - 40, 90, 28), inViaggio ? "Pausa" : "Viaggio")) { if (inViaggio) { inViaggio = false; cam.automatica = false; } else AvviaViaggio(); }
        if (GUI.Button(new Rect(110, Hh - 40, 90, 28), "Avanti")) SaltaAvanti();
        if (GUI.Button(new Rect(206, Hh - 40, 110, 28), "Vista intera")) Comando("intera");
        if (selezionato != null) GUI.Label(new Rect(330, Hh - 38, W - 340, 24), Pulisci(selezionato.luogo.nome), stEtichetta);
        if (stato != "") GUI.Label(new Rect(16, 8, W - 32, 24), stato, stEtichetta);
#endif
        GUI.matrix = m;
    }
}
