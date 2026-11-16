using UnityEngine;

// Texture del terreno generata all'avvio (nessun file da scaricare): 128x128, si ripete senza cuciture.
// R = macchie larghe, G = grana fine, B = distanza dal bordo delle celle (crepe dove è bassa).
public static class TexturaTerreno
{
    const int N = 128;
    const float LATO = 7f;   // lato in unità di una ripetizione della texture
    static Texture2D tex;
    static Material mat;

    // peso macchie, peso grana, peso crepe, contrasto, per ogni cerchio (0 = Antinferno)
    static readonly Vector4[] STILI =
    {
        new Vector4(0.9f, 0.7f, 0.25f, 0.75f),   // 0 Antinferno: terra battuta
        new Vector4(0.9f, 0.6f, 0.15f, 0.65f),   // 1 Limbo: prato pallido
        new Vector4(0.8f, 0.6f, 0.30f, 0.70f),   // 2 Lussuriosi: terra scura battuta dal vento
        new Vector4(1.0f, 0.5f, 0.30f, 0.85f),   // 3 Golosi: fango
        new Vector4(0.5f, 1.0f, 0.35f, 0.80f),   // 4 Avari: ghiaia
        new Vector4(1.0f, 0.4f, 0.20f, 0.90f),   // 5 Iracondi: palude
        new Vector4(0.5f, 0.3f, 1.00f, 0.55f),   // 6 Eretici: roccia rovente spaccata
        new Vector4(0.6f, 0.4f, 0.90f, 0.60f),   // 7 Violenti: terra rossa screpolata
        new Vector4(0.4f, 0.5f, 1.00f, 0.55f),   // 8 Malebolge: lastre di pietra
        new Vector4(0.5f, 0.2f, 0.85f, 0.45f),   // 9 Cocito: ghiaccio crepato
    };

    public static Vector4 Stile(int cerchio) { return STILI[Mathf.Clamp(cerchio, 0, STILI.Length - 1)]; }
    public static float Lato { get { return LATO; } }

    public static Material Materiale()
    {
        if (mat != null) return mat;
        var sh = Resources.Load<Shader>("Terreno");
        if (sh == null) return null;
        mat = new Material(sh);
        mat.SetTexture("_MainTex", Crea());
        return mat;
    }

    static float Hash(int x, int y, int seme, int periodo)
    {
        x = ((x % periodo) + periodo) % periodo; y = ((y % periodo) + periodo) % periodo;
        uint h = (uint)(x * 374761393 + y * 668265263 + seme * 1274126177);
        h = (h ^ (h >> 13)) * 1274126177u;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }

    // rumore di valore periodico (liscio), 'celle' = quante celle per lato
    static float Valore(float u, float v, int celle, int seme)
    {
        float x = u * celle, y = v * celle;
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        float a = Mathf.Lerp(Hash(x0, y0, seme, celle), Hash(x0 + 1, y0, seme, celle), fx);
        float b = Mathf.Lerp(Hash(x0, y0 + 1, seme, celle), Hash(x0 + 1, y0 + 1, seme, celle), fx);
        return Mathf.Lerp(a, b, fy);
    }

    // distanza dal bordo tra celle (Worley, differenza tra i due punti più vicini), periodica
    static float Bordo(float u, float v, int celle)
    {
        float x = u * celle, y = v * celle;
        int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
        float d1 = 9f, d2 = 9f;
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                float px = cx + i + 0.15f + 0.7f * Hash(cx + i, cy + j, 11, celle);
                float py = cy + j + 0.15f + 0.7f * Hash(cx + i, cy + j, 23, celle);
                float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                if (d < d1) { d2 = d1; d1 = d; } else if (d < d2) d2 = d;
            }
        return Mathf.Clamp01((d2 - d1) * 3.2f);
    }

    static Texture2D Crea()
    {
        if (tex != null) return tex;
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                float macchie = 0.5f * Valore(u, v, 4, 1) + 0.3f * Valore(u, v, 8, 2) + 0.2f * Valore(u, v, 16, 3);
                float grana = 0.5f * Valore(u, v, 32, 4) + 0.5f * Hash(x, y, 5, N);
                float celle = Bordo(u, v, 6);
                px[y * N + x] = new Color32((byte)(macchie * 255f), (byte)(grana * 255f), (byte)(celle * 255f), 255);
            }
        tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "TerrenoProcedurale", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
        tex.SetPixels32(px);
        tex.Apply(true);
        return tex;
    }
}
