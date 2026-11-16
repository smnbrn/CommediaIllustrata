using System.Collections.Generic;
using UnityEngine;

// Sfera e cilindro costruiti a mano, senza collider. GameObject.CreatePrimitive aggiunge anche un collider che nella
// build WebGL (con il codice inutilizzato tolto) non esiste e riempie la console di errori; qui non serve.
public static class Forme
{
    static Mesh sfera, cilindro, quadro;

    // sfera di diametro 1, come quella di Unity
    public static Mesh Sfera()
    {
        if (sfera != null) return sfera;
        const int fasce = 12, spicchi = 20;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= fasce; i++)
        {
            float a = Mathf.PI * i / fasce;
            for (int j = 0; j <= spicchi; j++)
            {
                float b = 2f * Mathf.PI * j / spicchi;
                var p = new Vector3(Mathf.Sin(a) * Mathf.Cos(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Sin(b));
                v.Add(p * 0.5f); n.Add(p);
            }
        }
        for (int i = 0; i < fasce; i++)
            for (int j = 0; j < spicchi; j++)
            {
                int k = i * (spicchi + 1) + j, k2 = k + spicchi + 1;
                t.Add(k); t.Add(k + 1); t.Add(k2);
                t.Add(k + 1); t.Add(k2 + 1); t.Add(k2);
            }
        sfera = new Mesh { name = "Sfera" };
        sfera.SetVertices(v); sfera.SetNormals(n); sfera.SetTriangles(t, 0);
        sfera.RecalculateBounds();
        return sfera;
    }

    // cilindro di diametro 1 e altezza 2, come quello di Unity
    public static Mesh Cilindro()
    {
        if (cilindro != null) return cilindro;
        const int lati = 28;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
        for (int j = 0; j <= lati; j++)
        {
            float b = 2f * Mathf.PI * j / lati;
            var d = new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b));
            v.Add(d * 0.5f + Vector3.up); n.Add(d);
            v.Add(d * 0.5f - Vector3.up); n.Add(d);
        }
        for (int j = 0; j < lati; j++)
        {
            int k = j * 2;
            t.Add(k); t.Add(k + 2); t.Add(k + 1);
            t.Add(k + 1); t.Add(k + 2); t.Add(k + 3);
        }
        int c0 = v.Count; v.Add(Vector3.up); n.Add(Vector3.up);        // coperchio sopra
        int c1 = v.Count; v.Add(-Vector3.up); n.Add(-Vector3.up);      // e sotto
        for (int j = 0; j < lati; j++)
        {
            int k = j * 2;
            t.Add(c0); t.Add(k + 2); t.Add(k);
            t.Add(c1); t.Add(k + 1); t.Add(k + 3);
        }
        cilindro = new Mesh { name = "Cilindro" };
        cilindro.SetVertices(v); cilindro.SetNormals(n); cilindro.SetTriangles(t, 0);
        cilindro.RecalculateBounds();
        return cilindro;
    }

    // quadrato orizzontale di lato 1 con le coordinate della texture, per l'ombra sotto le figure
    public static Mesh Quadro()
    {
        if (quadro != null) return quadro;
        quadro = new Mesh { name = "Quadro" };
        quadro.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(-0.5f, 0, 0.5f) });
        quadro.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
        quadro.SetTriangles(new List<int> { 0, 2, 1, 0, 3, 2 }, 0);
        quadro.RecalculateBounds();
        return quadro;
    }

    public static GameObject Crea(string nome, Mesh mesh)
    {
        var go = new GameObject(nome);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>();
        return go;
    }
}
