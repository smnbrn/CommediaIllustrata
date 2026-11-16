using System;
using System.Collections.Generic;
using UnityEngine;

// Specchio di data/SCHEMA.md. JsonUtility ignora i campi che non dichiariamo
// (terzine, tavole, parafrasi...), quindi qui c'è solo ciò che serve al 3D.

[Serializable] public class Coordinate { public float profondita, raggio, angolo; }

[Serializable]
public class Luogo
{
    public string id, nome, tipo, zona_di, descrizione;
    public int cerchio, ordine;
    public Coordinate coordinate;
    public string[] canti;
}

[Serializable] public class FileLuoghi { public Luogo[] luoghi; }

[Serializable]
public class Tavola
{
    public string autore_id, autore, titolo, url, miniatura;
}

[Serializable]
public class Scena
{
    public string id, titolo_scena, sintesi, luogo_id;
    public int[] versi;
    public Tavola[] tavole;
}

[Serializable]
public class FileCanto
{
    public string cantica, titolo;
    public int canto;
    public Scena[] scene;
}

// Una scena con il canto a cui appartiene, per l'indirizzo #inferno/N/scena-K.
public class RiferimentoScena
{
    public int canto;
    public Scena scena;
    public string Indirizzo => $"inferno/{canto}/{scena.id}";
}
