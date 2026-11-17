using System;
using System.Collections.Generic;
using UnityEngine;

// Le scene dei ripiani del Purgatorio. Modelli di Sketchfab (vedi esplora/CREDITI.md) per angeli, barca, serpente, arco, scale,
// piante, rocce, la donna del Paradiso terrestre, il carro e le candele; Catone e i penitenti sono disegnati dal codice.
// Le copie dello stesso modello si uniscono in una mesh per materiale (GruppoModello), le figure semplici in una mesh per ripiano.
public static class AmbientiPurgatorio
{
    // rotazioni di calibrazione per far guardare ogni modello in avanti (+Z)
    const float YAW_ANGELO = 0f, YAW_BARCA = 90f, YAW_SERPENTE = 0f, YAW_ARCO = 0f, YAW_DONNA = 0f, YAW_CARRO = 0f, YAW_MONACO = 0f, YAW_CATONE = 0f, YAW_SCALA = 0f;
    public const float ALTEZZA_SCALA = 1.5f;

    static Transform Nodo(Transform padre, string nome) { var g = new GameObject(nome); g.transform.SetParent(padre, false); return g.transform; }

    static GameObject MeshObj(Transform padre, string nome, Mesh me, Material mat)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = me;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static float Casuale(System.Random r, float a, float b) { return a + (b - a) * (float)r.NextDouble(); }

    public static Dictionary<string, GameObject> Costruisci(Transform padre, Monte m, Material matColore, Dictionary<string, Vector3> luoghiEffetti, List<Bounds> ingombri)
    {
        var rnd = new System.Random(2024);
        var radice = Nodo(padre, "Ambienti");
        var prot = new Dictionary<string, GameObject>();
        var nodi = new Transform[m.liv.Count];
        var figure = new FacceM[m.liv.Count];
        for (int i = 0; i < m.liv.Count; i++) { nodi[i] = Nodo(radice, "Scena " + m.liv[i].luogo.id); figure[i] = new FacceM(); prot[m.liv[i].luogo.id] = nodi[i].gameObject; }

        // da lunghezza d'arco (unità) a gradi, su un ripiano
        float Gradi(int i, float arco) { return arco / Mathf.Max(m.liv[i].centro, 8f) * Mathf.Rad2Deg; }
        Vector3 P(int i, float arco, float t) { return m.Punto(i, m.liv[i].angolo + Gradi(i, arco), t); }
        float Yaw(int i, float arco, bool dentro = false) { var d = Monte.Fuori(m.liv[i].angolo + Gradi(i, arco)); return Monte.YawVerso(dentro ? -d : d); }
        Vector3 Tang(int i) { float a = m.liv[i].angolo * Mathf.Deg2Rad; return new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a)); }

        // gruppi di modelli ripetuti
        var rocce = new GruppoModello("Modelli/Rocce/Rocce", true);
        var bambu = new GruppoModello("Modelli/Bambu/Bambu", false);
        var rose = new GruppoModello("Modelli/Rosa/Rosa", false);
        var piante = new GruppoModello("Modelli/Piante/Piante", true);
        var fiori = new GruppoModello("Modelli/AlberiFiori/AlberiFiori", false);
        var querce = new GruppoModello("Modelli/Querce/Querce", false);
        var cespugli = new GruppoModello("Modelli/AlberiCespugli/AlberiCespugli", true);

        // zone di rispetto (centro, raggio): il percorso di Dante e gli elementi principali. Niente può essere piazzato dentro una zona.
        var zone = new List<Vector4>();
        void Zona(Vector3 c, float r) { zone.Add(new Vector4(c.x, c.y, c.z, r)); }
        bool Libero(Vector3 p, float r)
        {
            foreach (var z in zone) if (Vector3.Distance(p, new Vector3(z.x, z.y, z.z)) < z.w + r) return false;
            return true;
        }
        {
            var perc = new List<Vector3>(); var idxl = new List<int>();
            m.Percorso(perc, idxl);
            for (int k = 1; k < perc.Count; k++)
            {
                float d = Vector3.Distance(perc[k - 1], perc[k]); int n = Mathf.Max(1, Mathf.CeilToInt(d / 1.2f));
                for (int q = 0; q <= n; q++) Zona(Vector3.Lerp(perc[k - 1], perc[k], q / (float)n), 2.4f);
            }
        }

        void Rocce(int i, int n, float arcoMax, float tMin, float tMax, float dMin, float dMax)
        {
            if (!rocce.Pronto) return;
            for (int k = 0; k < n; k++)
                for (int tentativo = 0; tentativo < 8; tentativo++)
                {
                    float dim = Casuale(rnd, dMin, dMax);
                    var p = P(i, Casuale(rnd, -arcoMax, arcoMax), Casuale(rnd, tMin, tMax)) - Vector3.up * 0.15f;
                    if (!Libero(p, dim * 0.55f)) continue;
                    rocce.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, false);
                    Zona(p, dim * 0.55f);
                    break;
                }
        }
        void Angelo(int i, string modello, float arco, float t, float altezza, Color? alone = null)
        {
            var g = Props.Metti(modello, nodi[i], P(i, arco, t), Yaw(i, arco) + YAW_ANGELO, altezza * 1.7f, true, alone ?? new Color(1f, 0.9f, 0.7f), 0.7f);
            if (g != null) { prot[m.liv[i].luogo.id + "#angelo"] = g; Zona(P(i, arco, t), 2.2f); }
        }
        var monaci = new GruppoModello[m.liv.Count];
        var vesteLiv = new Color[m.liv.Count];
        void Gruppo(int i, int n, float arcoMax, float tMin, float tMax, Color veste, int stato, bool dentro = false)
        {
            if (monaci[i] == null) monaci[i] = new GruppoModello("Modelli/Monaco/Monaco", false);
            vesteLiv[i] = veste;
            for (int k = 0; k < n; k++)
            {
                float arco = n == 1 ? 0f : Mathf.Lerp(-arcoMax, arcoMax, (k + (float)rnd.NextDouble() * 0.5f) / n);
                float yaw = Yaw(i, arco, dentro) + Casuale(rnd, -35, 35);
                var pos = P(i, arco, Casuale(rnd, tMin, tMax));
                for (int tentativo = 0; tentativo < 8 && !Libero(pos, 1.3f); tentativo++) pos = P(i, arco + Casuale(rnd, -4f, 4f), Casuale(rnd, tMin, tMax));
                if (!Libero(pos, 1.3f)) continue;
                Zona(pos, 1.3f);
                float alt = Casuale(rnd, 0.92f, 1.08f);
                if (monaci[i].Pronto)
                {
                    // il monaco del modello, messo in posa: in piedi, curvo sotto il masso, seduto, prono o in cammino
                    Quaternion rot = Quaternion.Euler(0f, yaw + YAW_MONACO, 0f); Vector3 sc = Vector3.one; float h = 3.4f * alt;
                    switch (stato)
                    {
                        case 1: rot = Quaternion.Euler(40f, yaw + YAW_MONACO, 0f); break;
                        case 2: sc = new Vector3(1.15f, 0.62f, 1.15f); break;
                        case 3: rot = Quaternion.Euler(88f, yaw + YAW_MONACO, 0f); pos += Vector3.up * 0.5f; break;
                        case 4: rot = Quaternion.Euler(10f, yaw + YAW_MONACO, 0f); break;
                    }
                    monaci[i].AggiungiPosa(pos, rot, sc, h);
                    if (stato == 1 && rocce.Pronto) rocce.Aggiungi(rnd, pos + Vector3.up * 2.0f, Casuale(rnd, 0, 360), 1.7f, false);
                }
                else FigureSemplici.Penitente(figure[i], pos, yaw, veste * Casuale(rnd, 0.85f, 1.1f), stato, alt * 1.9f);
            }
        }

        // ---------- 0 spiaggia dell'isola: giunchi, la barca dell'angelo, Catone ----------
        {
            int i = 0; var lv = m.liv[i];
            if (bambu.Pronto)
                for (int k = 0; k < 5; k++)
                    bambu.Aggiungi(rnd, P(i, (k % 2 == 0 ? 1 : -1) * Casuale(rnd, 14, 48), Casuale(rnd, 0.85f, 1.0f)), Casuale(rnd, 0, 360), Casuale(rnd, 4.5f, 6.5f), true);
            Rocce(i, 7, 40, 0.1f, 0.9f, 1.4f, 2.8f);
            // la barca sull'acqua, davanti alla spiaggia, con l'angelo nocchiero
            float a = lv.angolo + 5f;
            Vector3 acqua = Monte.Fuori(a) * (Monte.R * 1.05f);
            float yawBarca = Monte.YawVerso(-Monte.Fuori(a)) + YAW_BARCA;
            var barca = Props.Metti("Barca", nodi[i], new Vector3(acqua.x, Monte.MARE - 0.05f, acqua.z), yawBarca, 14f, false, new Color(0.9f, 0.85f, 0.7f), 0.7f);
            var angelo = Props.Metti("AngeloMarmo", nodi[i], new Vector3(acqua.x, Monte.MARE + 0.55f, acqua.z), Monte.YawVerso(-Monte.Fuori(a)) + YAW_ANGELO, 8.6f, true, new Color(1f, 0.92f, 0.75f), 0.75f);
            if (angelo != null) prot["spiaggia#angelo"] = angelo;
            var catone = Props.Metti("Catone", nodi[i], P(i, -6f, 0.45f), Monte.YawVerso(Monte.Fuori(a)) + 20f + YAW_CATONE, 4.6f, true, new Color(1f, 0.92f, 0.75f), 0.75f);
            if (catone != null) prot["spiaggia#catone"] = catone;
            else FigureSemplici.Catone(figure[i], P(i, -6f, 0.45f), Monte.YawVerso(Monte.Fuori(a)) + 20f, 2.4f);
        }

        // ---------- 1-3 Antipurgatorio ----------
        { int i = 1; Rocce(i, 9, 24, 0.0f, 0.9f, 1.8f, 4.2f); Gruppo(i, 7, 12, 0.3f, 0.8f, new Color(0.62f, 0.60f, 0.64f), 0); }                        // scomunicati
        { int i = 2; Rocce(i, 8, 22, 0.0f, 0.9f, 1.6f, 3.6f); Gruppo(i, 6, 10, 0.3f, 0.8f, new Color(0.64f, 0.56f, 0.40f), 2); }                        // pigri, seduti
        { int i = 3; Rocce(i, 7, 24, 0.0f, 0.9f, 1.4f, 3.2f); Gruppo(i, 8, 14, 0.25f, 0.8f, new Color(0.62f, 0.34f, 0.32f), 4); }                       // morti violenti

        // ---------- 4 la valletta dei principi: fiori, due angeli, il serpente ----------
        {
            int i = 4;
            Angelo(i, "AngeloOttone", -9f, 0.6f, 4.2f, new Color(0.7f, 1f, 0.7f));
            Angelo(i, "AngeloArgento", 9f, 0.6f, 4.2f, new Color(0.7f, 1f, 0.7f));
            var s = Props.Metti("Serpente", nodi[i], P(i, 20f, 0.5f), Casuale(rnd, 0, 360) + YAW_SERPENTE, 2.2f, false, new Color(0.6f, 0.9f, 0.5f), 0.6f);
            if (s != null) { prot["valletta-principi#serpente"] = s; Zona(P(i, 20f, 0.5f), 2.5f); }
            Gruppo(i, 5, 7, 0.25f, 0.7f, new Color(0.66f, 0.46f, 0.70f), 2);
            for (int k = 0; k < 70; k++)
            {
                float arco = Casuale(rnd, -42, 42), t = Casuale(rnd, 0.05f, 0.98f);
                var p = P(i, arco, t);
                int sorta = rnd.Next(3);
                float dim = sorta == 0 ? Casuale(rnd, 1.6f, 3.0f) : sorta == 1 ? Casuale(rnd, 3.5f, 6f) : Casuale(rnd, 1.6f, 2.6f);
                if (!Libero(p, dim * 0.4f)) continue;
                if (sorta == 0 && rose.Pronto) rose.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, true);
                else if (sorta == 1 && fiori.Pronto) fiori.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, false);
                else if (piante.Pronto) piante.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, true);
            }
        }

        // ---------- 5 la porta del Purgatorio: l'arco con tre gradini e l'angelo guardiano ----------
        {
            int i = 5; var lv = m.liv[i];
            Vector3 fuori = Monte.Fuori(lv.angolo);
            Vector3 interno = m.Punto(i, lv.angolo, 0f);                 // bordo interno del ripiano, contro la parete
            float yaw = Monte.YawVerso(fuori) + YAW_ARCO;
            // i tre gradini (marmo bianco, pietra scura e crepata, porfido rosso), poi l'arco che sta in cima
            // la scalinata (modello Sketchfab) adattata al ripiano: larga 7.2 lungo il bordo, profonda 4.2 verso l'esterno, alta 1.5;
            // sale verso la parete, dove sta l'arco
            var scala = Props.MettiScatola("Scala", nodi[i], interno + fuori * 2.4f, Monte.YawVerso(fuori) + YAW_SCALA, 7.2f, 4.2f, ALTEZZA_SCALA, false, new Color(1f, 0.92f, 0.75f), 0.75f);
            var arco = Props.Metti("Arco", nodi[i], interno + fuori * 0.85f + Vector3.up * ALTEZZA_SCALA, yaw + 90f, 9f, true, new Color(1f, 0.92f, 0.75f), 0.75f);
            Zona(interno + fuori * 2.6f, 4.2f);
            Angelo(i, "AngeloArgento", 6f, 0.8f, 4.8f, new Color(1f, 0.95f, 0.8f));
            Rocce(i, 4, 14, 0.5f, 0.9f, 1.2f, 2.4f);
        }

        // ---------- 6-12 le sette cornici ----------
        string[] angeli = { "AngeloMarmo", "AngeloArgento", "AngeloOttone", "AngeloMarmo", "AngeloArgento", "AngeloOttone", "AngeloMarmo" };
        for (int c = 0; c < 7; c++)
        {
            int i = 6 + c;
            Angelo(i, angeli[c], 7f, 0.7f, 4.4f, new Color(1f, 0.95f, 0.8f));
        }
        { int i = 6; Rocce(i, 5, 14, 0.2f, 0.9f, 2.2f, 3.6f); Gruppo(i, 5, 11, 0.35f, 0.8f, new Color(0.68f, 0.68f, 0.72f), 1); }       // superbi sotto i massi
        { int i = 7; Gruppo(i, 7, 12, 0.1f, 0.35f, new Color(0.46f, 0.52f, 0.66f), 2, true); }                                         // invidiosi contro la parete
        { int i = 8; Gruppo(i, 6, 11, 0.3f, 0.8f, new Color(0.40f, 0.37f, 0.40f), 0); }                                                // iracondi nel fumo
        { int i = 9; Gruppo(i, 7, 15, 0.3f, 0.8f, new Color(0.64f, 0.66f, 0.54f), 4); }                                                // accidiosi che corrono
        { int i = 10; Gruppo(i, 7, 12, 0.3f, 0.8f, new Color(0.58f, 0.48f, 0.30f), 3); }                                               // avari prostrati
        {   // golosi: l'albero con i frutti e gli spiriti magri
            int i = 11;
            if (querce.Pronto) querce.Aggiungi(rnd, P(i, -3f, 0.45f), 40f, 11f, true);
            Gruppo(i, 6, 12, 0.3f, 0.8f, new Color(0.72f, 0.64f, 0.45f), 0);
        }
        { int i = 12; Gruppo(i, 5, 7, 0.1f, 0.5f, new Color(0.78f, 0.42f, 0.30f), 0); }                                                // lussuriosi nel fuoco

        // ---------- 13 il Paradiso terrestre ----------
        {
            int i = 13; var lv = m.liv[i];
            float re = lv.rEst, a0 = lv.angolo;
            Vector3 E(float ang, float frac) { float a = ang * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a) * re * frac, lv.y, Mathf.Sin(a) * re * frac); }
            // prima le figure e il corteo, con la loro zona di rispetto: la vegetazione si piazza dopo, attorno
            Vector3 pm = E(a0 + 25f, 0.5f);
            var matelda = Props.Metti("DonnaFuse", nodi[i], pm, Monte.YawVerso(Monte.Fuori(a0 + 25f)) + YAW_DONNA, 3.6f, true, new Color(1f, 0.9f, 0.7f), 0.7f);
            if (matelda != null) { prot["paradiso-terrestre#matelda"] = matelda; Zona(pm + Vector3.up * 1.5f, 3.2f); }
            Vector3 pc = E(a0 - 60f, 0.55f);
            Vector3 fc = Monte.Fuori(a0 - 60f);
            var carro = Props.Metti("Carro", nodi[i], pc, Monte.YawVerso(fc + Vector3.Cross(Vector3.up, fc)) + YAW_CARRO, 11f, false, new Color(1f, 0.9f, 0.7f), 0.7f);
            if (carro != null)
            {
                var bc = Props.Limiti(carro);
                // Beatrice in piedi sul carro: un raggio verso il basso su collider temporanei trova il punto d'appoggio
                Vector3 appoggio = new Vector3(bc.center.x, bc.center.y, bc.center.z);
                var tmp = new List<MeshCollider>();
                foreach (var mf in carro.GetComponentsInChildren<MeshFilter>()) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; tmp.Add(mc); }
                Physics.SyncTransforms();
                if (Physics.Raycast(new Vector3(bc.center.x, bc.max.y + 4f, bc.center.z), Vector3.down, out var colpo, 30f)) appoggio = colpo.point;
                foreach (var mc in tmp) UnityEngine.Object.Destroy(mc);
                var beatrice = Props.Metti("DonnaRuby", nodi[i], appoggio, Monte.YawVerso(fc) + YAW_DONNA, 3.8f, true, new Color(1f, 0.85f, 0.8f), 0.75f);
                if (beatrice != null) prot["paradiso-terrestre#beatrice"] = beatrice;
                Zona(bc.center, Mathf.Max(bc.extents.x, bc.extents.z) + 3f);
            }
            for (int k = 0; k < 7; k++)
            {
                var pcand = E(a0 - 160f + k * 6f, 0.5f);
                if (!Libero(pcand + Vector3.up * 1.5f, 1.4f)) continue;      // mai addosso al carro o al percorso
                var cand = Props.Metti("Candele", nodi[i], pcand, 0f, 3.8f, true, new Color(1f, 0.85f, 0.5f), 0.8f);
                if (cand != null) Zona(pcand + Vector3.up * 1.5f, 1.4f);
            }
            luoghiEffetti["letè"] = E(a0 + 80f, 0.5f);
            // la selva divina lungo il bordo, i fiori ovunque, senza toccare figure, carro, candele e percorso
            if (querce.Pronto) for (int k = 0; k < 9; k++)
            {
                var pq = E(a0 + k * 40f + Casuale(rnd, -10, 10), Casuale(rnd, 0.72f, 0.9f)); float dq = Casuale(rnd, 11f, 15f);
                if (Libero(pq, 3f)) { querce.Aggiungi(rnd, pq, Casuale(rnd, 0, 360), dq, true); Zona(pq + Vector3.up * 2f, 3f); }
            }
            if (cespugli.Pronto) for (int k = 0; k < 16; k++)
            {
                var pb = E(Casuale(rnd, 0, 360), Casuale(rnd, 0.3f, 0.95f)); float db = Casuale(rnd, 3f, 5.5f);
                if (Libero(pb, db * 0.5f)) { cespugli.Aggiungi(rnd, pb, Casuale(rnd, 0, 360), db, true); Zona(pb + Vector3.up * 1f, db * 0.5f); }
            }
            for (int k = 0; k < 120; k++)
            {
                var p = E(Casuale(rnd, 0, 360), Mathf.Sqrt((float)rnd.NextDouble()) * 0.95f);
                int sorta = rnd.Next(3);
                float dim = sorta == 0 ? Casuale(rnd, 1.6f, 3.0f) : sorta == 1 ? Casuale(rnd, 3.5f, 6f) : Casuale(rnd, 1.6f, 2.5f);
                if (!Libero(p + Vector3.up * 0.5f, dim * 0.4f)) continue;
                if (sorta == 0 && rose.Pronto) rose.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, true);
                else if (sorta == 1 && fiori.Pronto) fiori.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, false);
                else if (piante.Pronto) piante.Aggiungi(rnd, p, Casuale(rnd, 0, 360), dim, true);
            }
        }

        // affioramenti di roccia e cespugli sulle pareti (fuori dal tratto della scala)
        for (int i = 0; i + 1 < m.liv.Count; i++)
        {
            float a0 = m.liv[i + 1].angolo, a1 = m.liv[i].angolo;      // la scala va da a1 ad a0 (l'angolo cala)
            int nr = i < 5 ? 16 : 11;
            for (int k = 0; k < nr; k++)
            {
                float ang = Casuale(rnd, 0, 360);
                float t = Casuale(rnd, 0.05f, 0.95f);
                float r = m.RaggioParete(i, t) - 0.3f; float y = Mathf.Lerp(m.liv[i].y, m.liv[i + 1].y, t);
                var pos = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * r, y - 0.6f, Mathf.Sin(ang * Mathf.Deg2Rad) * r);
                float dr = Casuale(rnd, 2.2f, 5.0f);
                if (!Libero(pos + Vector3.up * dr * 0.4f, dr * 0.6f)) continue;     // lontano dalla scala, dal percorso e dalle altre cose
                if (rocce.Pronto) { rocce.Aggiungi(rnd, pos, Casuale(rnd, 0, 360), dr, false); Zona(pos + Vector3.up * dr * 0.4f, dr * 0.55f); }
                if (i < 5 && cespugli.Pronto && k % 2 == 0)
                {
                    var pb2 = pos + Vector3.up * 0.4f + Vector3.up * dr; float db2 = Casuale(rnd, 2f, 3.6f);
                    if (Libero(pos + Vector3.up * 0.4f, db2 * 0.5f)) cespugli.Aggiungi(rnd, pos + Vector3.up * 0.4f, Casuale(rnd, 0, 360), db2, true);
                }
            }
        }

        // montagne lontane, all'orizzonte, nella foschia
        {
            var lontane = new GruppoModello("Modelli/Montagna/Montagna", false);
            if (lontane.Pronto)
            {
                float[] ang = { 15f, 70f, 125f, 190f, 240f, 300f, 345f };
                foreach (float a in ang)
                    lontane.Aggiungi(rnd, Monte.Fuori(a + Casuale(rnd, -8, 8)) * Casuale(rnd, 1000f, 1250f) + Vector3.up * (Monte.MARE - 6f), Casuale(rnd, 0, 360), Casuale(rnd, 420f, 700f), false);
                lontane.Costruisci(radice, "Montagne lontane");
            }
        }

        // unione dei gruppi e delle figure semplici
        rocce.Costruisci(radice, "Rocce"); bambu.Costruisci(radice, "Giunchi"); rose.Costruisci(radice, "Rose"); piante.Costruisci(radice, "Piante");
        fiori.Costruisci(radice, "Fiori"); querce.Costruisci(radice, "Querce"); cespugli.Costruisci(radice, "Cespugli");
        foreach (var nome in new[] { "Querce", "Cespugli" })
        {
            var t = radice.Find(nome);
            if (t == null) continue;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
                foreach (var mt in r.materials) if (mt.HasProperty("_Tint")) mt.SetColor("_Tint", new Color(0.62f, 1f, 0.58f));
        }
        for (int i = 0; i < monaci.Length; i++)
        {
            if (monaci[i] == null || !monaci[i].Pronto) continue;
            monaci[i].Costruisci(nodi[i], "Monaci");
            var tm = nodi[i].Find("Monaci");
            if (tm == null) continue;
            Color tinta = Color.Lerp(Color.white, vesteLiv[i] * 1.6f, 0.55f); tinta.a = 1f;
            foreach (var r in tm.GetComponentsInChildren<Renderer>())
                foreach (var mt in r.materials) if (mt.HasProperty("_Tint")) mt.SetColor("_Tint", tinta);
        }
        for (int i = 0; i < figure.Length; i++)
            if (!figure[i].Vuota) MeshObj(nodi[i], "Figure", figure[i].Costruisci("Figure " + m.liv[i].luogo.id), matColore);
        // ingombri per la camera: tutto ciò che è abbastanza grande da essere attraversato
        foreach (var g in new[] { rocce, querce, cespugli, bambu }) foreach (var b in g.ingombri) { var bb = b; bb.Expand(0.6f); ingombri.Add(bb); }
        foreach (var g in monaci) if (g != null) foreach (var b in g.ingombri) { var bb = b; bb.Expand(0.5f); ingombri.Add(bb); }
        foreach (var n in nodi)
            foreach (Transform c in n)
            {
                if (c.name == "Monaci" || c.name == "Figure") continue;
                var b = Props.Limiti(c.gameObject); b.Expand(0.5f); ingombri.Add(b);
            }
        return prot;
    }
}
