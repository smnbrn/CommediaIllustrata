using System.Collections.Generic;
using UnityEngine;

// Caronte, Cerbero e Lucifero, costruiti da forme semplici (stile "low-poly" a colori piatti).
// Ogni figura ha l'origine sulla sua base, la fronte verso +Z, e un piccolo "palco" sotto i piedi.
public static class Figure
{
    static Shader shader;
    static readonly Dictionary<Color, Material> materiali = new Dictionary<Color, Material>();
    static Mesh mCono, mAla;

    static Material Mat(Color c)
    {
        if (shader == null) shader = Resources.Load<Shader>("ColoreVertici");
        if (!materiali.TryGetValue(c, out var m)) { m = new Material(shader); m.SetColor("_Color", c); materiali[c] = m; }
        return m;
    }

    static GameObject Prim(PrimitiveType t, Transform p, Vector3 pos, Vector3 scala, Color c, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(t);
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scala; go.transform.localEulerAngles = euler;
        go.GetComponent<MeshRenderer>().sharedMaterial = Mat(c);
        return go;
    }

    static GameObject Sfera(Transform p, Vector3 pos, Vector3 scala, Color c) { return Prim(PrimitiveType.Sphere, p, pos, scala, c); }

    // capsula (o cilindro) tra due punti
    static GameObject Segmento(Transform p, Vector3 da, Vector3 a, float spessore, Color c, PrimitiveType t = PrimitiveType.Capsule)
    {
        Vector3 d = a - da; float len = d.magnitude;
        var go = Prim(t, p, (da + a) * 0.5f, new Vector3(spessore, len * 0.5f, spessore), c);
        go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d);
        return go;
    }

    static Mesh MeshCono()
    {
        if (mCono != null) return mCono;
        const int n = 14;
        var v = new List<Vector3> { new Vector3(0, 1, 0), Vector3.zero };
        for (int i = 0; i < n; i++) { float a = i * Mathf.PI * 2f / n; v.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); }
        var t = new List<int>();
        for (int i = 0; i < n; i++) { int a = 2 + i, b = 2 + (i + 1) % n; t.AddRange(new[] { 0, b, a }); t.AddRange(new[] { 1, a, b }); }
        mCono = new Mesh { name = "Cono" }; mCono.SetVertices(v); mCono.SetTriangles(t, 0); mCono.RecalculateNormals(); mCono.RecalculateBounds();
        return mCono;
    }

    // cono con la base sul punto dato e la punta verso l'alto (+Y locale)
    static GameObject Cono(Transform p, Vector3 pos, Vector3 scala, Color c, Vector3 euler = default)
    {
        var go = new GameObject("Cono");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos; go.transform.localScale = scala; go.transform.localEulerAngles = euler;
        go.AddComponent<MeshFilter>().sharedMesh = MeshCono();
        go.AddComponent<MeshRenderer>().sharedMaterial = Mat(c);
        return go;
    }

    // ala di pipistrello: membrana a ventaglio con la punta di 4 dita (nel piano XY, +X = verso l'esterno)
    static Mesh MeshAla()
    {
        if (mAla != null) return mAla;
        Vector3[] bordo =
        {
            new Vector3(13f, 7f, 0), new Vector3(10.5f, 3.2f, 0), new Vector3(15.5f, 2.5f, 0), new Vector3(11f, -1f, 0),
            new Vector3(14f, -4f, 0), new Vector3(9f, -4.5f, 0), new Vector3(8f, -8f, 0), new Vector3(3.5f, -4f, 0)
        };
        var v = new List<Vector3> { Vector3.zero };
        var col = new List<Color> { new Color(0.10f, 0.04f, 0.06f) };
        foreach (var b in bordo) { v.Add(b); col.Add(Color.Lerp(new Color(0.16f, 0.06f, 0.09f), new Color(0.32f, 0.10f, 0.14f), b.magnitude / 17f)); }
        var t = new List<int>();
        for (int i = 1; i < bordo.Length; i++) t.AddRange(new[] { 0, i, i + 1 });
        mAla = new Mesh { name = "Ala" }; mAla.SetVertices(v); mAla.SetColors(col); mAla.SetTriangles(t, 0); mAla.RecalculateBounds();
        return mAla;
    }

    static GameObject Ala(Transform p, Vector3 pos, float azimutDeg, float inclinazione, float scala, float fase)
    {
        var perno = new GameObject("Ala");
        perno.transform.SetParent(p, false);
        perno.transform.localPosition = pos;
        perno.transform.localRotation = Quaternion.Euler(0, -azimutDeg, 0) * Quaternion.Euler(0, 0, inclinazione);
        perno.transform.localScale = Vector3.one * scala;
        var m = new GameObject("Membrana");
        m.transform.SetParent(perno.transform, false);
        m.AddComponent<MeshFilter>().sharedMesh = MeshAla();
        m.AddComponent<MeshRenderer>().sharedMaterial = Mat(Color.white);
        var o = perno.AddComponent<Oscilla>(); o.asse = Vector3.forward; o.ampiezza = 9f; o.velocita = 1.3f; o.fase = fase;
        return perno;
    }

    static GameObject Palco(Transform p, float raggio, Color c)
    {
        return Prim(PrimitiveType.Cylinder, p, new Vector3(0, -0.05f, 0), new Vector3(raggio * 2f, 0.05f, raggio * 2f), c);
    }

    // ---------------- Caronte, il nocchiero ----------------
    public static GameObject Caronte()
    {
        var scena = new GameObject("Caronte").transform;
        Palco(scena, 5.5f, new Color(0.07f, 0.20f, 0.26f));
        var root = new GameObject("Barca").transform;   // la barca dondola: l'oscillazione non deve toccare la posizione della figura
        root.SetParent(scena, false);

        var legno = new Color(0.30f, 0.19f, 0.11f);
        Sfera(root, new Vector3(0, 0.55f, 0), new Vector3(2.6f, 0.9f, 7.2f), legno);
        Cono(root, new Vector3(0, 0.45f, 3.3f), new Vector3(1.0f, 2.2f, 1.0f), legno, new Vector3(75, 0, 0));   // prua
        Cono(root, new Vector3(0, 0.45f, -3.3f), new Vector3(1.0f, 2.0f, 1.0f), legno, new Vector3(-75, 0, 0)); // poppa

        var manto = new Color(0.12f, 0.12f, 0.17f);
        Prim(PrimitiveType.Capsule, root, new Vector3(0, 2.5f, -1.2f), new Vector3(1.7f, 1.7f, 1.7f), manto);
        Sfera(root, new Vector3(0, 4.7f, -1.2f), new Vector3(1.6f, 1.6f, 1.6f), manto);                          // cappuccio
        Sfera(root, new Vector3(0, 4.55f, -0.65f), new Vector3(1.0f, 1.0f, 1.0f), new Color(0.62f, 0.55f, 0.48f)); // volto
        Cono(root, new Vector3(0, 4.15f, -0.45f), new Vector3(0.7f, 1.4f, 0.7f), new Color(0.88f, 0.88f, 0.85f), new Vector3(180, 0, 0)); // barba bianca
        var occhio = new Color(1f, 0.25f, 0.10f);                                                                  // occhi di bragia
        Sfera(root, new Vector3(-0.28f, 4.75f, -0.2f), Vector3.one * 0.28f, occhio);
        Sfera(root, new Vector3(0.28f, 4.75f, -0.2f), Vector3.one * 0.28f, occhio);

        // remo che batte l'acqua
        var perno = new GameObject("Remo").transform;
        perno.SetParent(root, false); perno.localPosition = new Vector3(1.0f, 2.8f, -0.4f);
        Segmento(perno, new Vector3(0, 1.0f, 0.3f), new Vector3(0.6f, -2.8f, -0.8f), 0.14f, new Color(0.45f, 0.30f, 0.17f), PrimitiveType.Cylinder);
        Prim(PrimitiveType.Cube, perno, new Vector3(0.7f, -3.0f, -0.9f), new Vector3(0.7f, 1.8f, 0.12f), new Color(0.40f, 0.26f, 0.15f), new Vector3(10, 0, 0));
        var o = perno.gameObject.AddComponent<Oscilla>(); o.asse = Vector3.right; o.ampiezza = 14f; o.velocita = 1.1f;
        Segmento(root, new Vector3(0.5f, 3.3f, -1.2f), new Vector3(1.0f, 2.8f, -0.4f), 0.5f, manto);               // braccio
        Segmento(root, new Vector3(-0.5f, 3.3f, -1.2f), new Vector3(0.4f, 2.6f, -0.1f), 0.5f, manto);

        // lieve dondolio della barca
        var ond = root.gameObject.AddComponent<Oscilla>(); ond.asse = Vector3.forward; ond.ampiezza = 2.2f; ond.velocita = 0.9f; ond.su = 0.12f;
        return scena.gameObject;
    }

    // ---------------- Cerbero, il cane a tre teste ----------------
    static void Testa(Transform p, Vector3 pos, float yaw, float fase)
    {
        var perno = new GameObject("Testa").transform;
        perno.SetParent(p, false); perno.localPosition = pos; perno.localRotation = Quaternion.Euler(0, yaw, 0);
        var pelo = new Color(0.27f, 0.17f, 0.11f);
        var scuro = new Color(0.12f, 0.08f, 0.06f);
        Sfera(perno, Vector3.zero, new Vector3(1.8f, 1.6f, 1.8f), pelo);
        Sfera(perno, new Vector3(0, -0.15f, 1.15f), new Vector3(1.0f, 0.8f, 1.5f), scuro);                       // muso
        Sfera(perno, new Vector3(0, -0.65f, 1.1f), new Vector3(0.9f, 0.35f, 1.3f), new Color(0.55f, 0.05f, 0.05f)); // bocca spalancata
        Sfera(perno, new Vector3(0, 0.05f, 1.85f), Vector3.one * 0.32f, Color.black);                               // naso
        var occhio = new Color(1f, 0.35f, 0.08f);
        Sfera(perno, new Vector3(-0.5f, 0.35f, 0.75f), Vector3.one * 0.3f, occhio);
        Sfera(perno, new Vector3(0.5f, 0.35f, 0.75f), Vector3.one * 0.3f, occhio);
        Cono(perno, new Vector3(-0.7f, 0.6f, -0.1f), new Vector3(0.5f, 0.9f, 0.4f), scuro, new Vector3(0, 0, 25));
        Cono(perno, new Vector3(0.7f, 0.6f, -0.1f), new Vector3(0.5f, 0.9f, 0.4f), scuro, new Vector3(0, 0, -25));
        var o = perno.gameObject.AddComponent<Oscilla>(); o.asse = Vector3.right; o.ampiezza = 9f; o.velocita = 1.6f; o.fase = fase;
    }

    public static GameObject Cerbero()
    {
        var root = new GameObject("Cerbero").transform;
        Palco(root, 6f, new Color(0.22f, 0.20f, 0.09f));   // fango

        var pelo = new Color(0.22f, 0.14f, 0.10f);
        Sfera(root, new Vector3(0, 2.7f, -0.4f), new Vector3(3.0f, 2.5f, 4.8f), pelo);                           // corpo
        foreach (float x in new[] { -1.2f, 1.2f })
            foreach (float z in new[] { -2.0f, 1.2f })
            {
                Segmento(root, new Vector3(x, 2.2f, z), new Vector3(x, 0.3f, z), 0.7f, pelo);
                Sfera(root, new Vector3(x, 0.2f, z + 0.2f), new Vector3(0.9f, 0.5f, 1.2f), new Color(0.12f, 0.08f, 0.06f));
            }
        Segmento(root, new Vector3(0, 3.2f, -2.6f), new Vector3(0, 4.6f, -4.4f), 0.5f, pelo);                    // coda

        // tre colli, tre teste
        Vector3[] teste = { new Vector3(-2.3f, 4.5f, 3.9f), new Vector3(0, 5.2f, 4.4f), new Vector3(2.3f, 4.5f, 3.9f) };
        float[] yaw = { -22f, 0f, 22f };
        for (int i = 0; i < 3; i++)
        {
            Segmento(root, new Vector3(teste[i].x * 0.45f, 3.6f, 1.6f), teste[i] - new Vector3(0, 0.1f, 0.7f), 1.0f, pelo);
            Testa(root, teste[i], yaw[i], i * 2.1f);
        }
        return root.gameObject;
    }

    // ---------------- Lucifero, imprigionato nel ghiaccio ----------------
    static void Faccia(Transform p, float azimutDeg, float raggio, float y, Color pelle, Color occhio)
    {
        var perno = new GameObject("Faccia").transform;
        perno.SetParent(p, false);
        float a = azimutDeg * Mathf.Deg2Rad;
        perno.localPosition = new Vector3(Mathf.Cos(a) * raggio, y, Mathf.Sin(a) * raggio);
        perno.localRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
        Sfera(perno, Vector3.zero, new Vector3(3.2f, 3.4f, 3.0f), pelle);
        Sfera(perno, new Vector3(-0.75f, 0.45f, 1.35f), new Vector3(0.7f, 0.45f, 0.35f), occhio);
        Sfera(perno, new Vector3(0.75f, 0.45f, 1.35f), new Vector3(0.7f, 0.45f, 0.35f), occhio);
        Cono(perno, new Vector3(0, -0.1f, 1.4f), new Vector3(0.6f, 0.9f, 0.6f), pelle * 0.8f, new Vector3(90, 0, 0)); // naso
        Sfera(perno, new Vector3(0, -1.0f, 1.2f), new Vector3(1.5f, 0.55f, 0.5f), new Color(0.12f, 0.02f, 0.03f));   // bocca che maciulla
        Cono(perno, new Vector3(-0.9f, 1.4f, 0.1f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.15f, 0.12f, 0.1f), new Vector3(0, 0, 18));
        Cono(perno, new Vector3(0.9f, 1.4f, 0.1f), new Vector3(0.5f, 1.2f, 0.5f), new Color(0.15f, 0.12f, 0.1f), new Vector3(0, 0, -18));
    }

    public static GameObject Lucifero()
    {
        var root = new GameObject("Lucifero").transform;
        Prim(PrimitiveType.Cylinder, root, new Vector3(0, 0.2f, 0), new Vector3(15f, 0.2f, 15f), new Color(0.78f, 0.90f, 1.0f)); // il ghiaccio

        var pelo = new Color(0.20f, 0.07f, 0.08f);
        Sfera(root, new Vector3(0, 5.5f, 0), new Vector3(7.5f, 9.5f, 6.5f), pelo);                                // busto
        Prim(PrimitiveType.Cylinder, root, new Vector3(0, 11f, 0), new Vector3(2.8f, 1.2f, 2.8f), pelo);          // collo
        Segmento(root, new Vector3(-3.4f, 8.5f, 0), new Vector3(-6.0f, 3.5f, 1.2f), 1.8f, pelo);                  // braccia
        Segmento(root, new Vector3(3.4f, 8.5f, 0), new Vector3(6.0f, 3.5f, 1.2f), 1.8f, pelo);

        // tre facce: rossa davanti, giallastra a destra, nera a sinistra
        Faccia(root, 90f, 2.6f, 13.8f, new Color(0.78f, 0.10f, 0.10f), new Color(1f, 0.9f, 0.3f));
        Faccia(root, -30f, 2.6f, 13.8f, new Color(0.88f, 0.80f, 0.50f), new Color(0.7f, 0.1f, 0.1f));
        Faccia(root, 210f, 2.6f, 13.8f, new Color(0.10f, 0.10f, 0.17f), new Color(1f, 0.9f, 0.3f));

        // sei ali, due sotto ogni faccia
        for (int k = 0; k < 3; k++)
        {
            float az = 90f + k * 120f;
            Ala(root, new Vector3(Mathf.Cos((az - 28f) * Mathf.Deg2Rad) * 3f, 9.5f, Mathf.Sin((az - 28f) * Mathf.Deg2Rad) * 3f), az - 28f, 22f, 0.62f, k * 1.7f);
            Ala(root, new Vector3(Mathf.Cos((az + 28f) * Mathf.Deg2Rad) * 3f, 9.5f, Mathf.Sin((az + 28f) * Mathf.Deg2Rad) * 3f), az + 28f, 22f, 0.62f, k * 1.7f + 0.9f);
        }
        return root.gameObject;
    }
}
