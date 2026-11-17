"""Post-produzione delle immagini generate con IA: scala di grigi, firma coperta, salvataggio in img/ia/.

Uso:  python -I strumenti/ia_dopo.py <cartella_sorgente> [nome-immagine ...]

Per ogni `purgatorio-NN-scena-K.jpg` della cartella:
- toglie il colore (immagine in grigi, come per l'Inferno);
- copre la firma che l'IA inventa in basso a sinistra o a destra dentro la cornice, copiando la trama del tratteggio
  dalla fascia subito sopra (specchiata in verticale, con il bordo sfumato): vedi FIRME;
- se la larghezza supera 960 px la riduce;
- scrive il JPEG in `img/ia/`.
FIRME elenca i lati (S = sinistra, D = destra) in cui, guardando l'immagine, c'era una firma.
"""
import json
import pathlib
import sys

import numpy as np
from PIL import Image

RADICE = pathlib.Path(__file__).resolve().parent.parent
S, D = "S", "D"
FIRME = {
    "01-scena-1": "SD", "02-scena-3": "S", "02-scena-5": "D", "03-scena-3": "S", "03-scena-4": "D", "04-scena-1": "S",
    "05-scena-1": "D", "05-scena-3": "D", "06-scena-2": "S", "06-scena-4": "S", "07-scena-2": "S", "08-scena-4": "D",
    "10-scena-2": "D", "10-scena-4": "S", "11-scena-1": "S", "11-scena-2": "S", "12-scena-4": "S", "14-scena-1": "S",
    "15-scena-1": "S", "15-scena-4": "S", "17-scena-2": "S", "17-scena-3": "D", "18-scena-1": "S", "18-scena-3": "S", "18-scena-4": "D",
    "19-scena-1": "D", "19-scena-4": "D", "21-scena-1": "D", "21-scena-2": "D", "21-scena-3": "S", "22-scena-1": "D",
    "22-scena-2": "S", "23-scena-3": "D", "24-scena-1": "D", "24-scena-2": "D", "26-scena-1": "D", "26-scena-4": "S",
    "27-scena-2": "D", "27-scena-4": "SD", "30-scena-2": "D", "30-scena-3": "D", "33-scena-1": "SD", "33-scena-2": "SD",
}


# altezza della fascia coperta (frazione dell'altezza della tavola), se quella normale (0.052) tocca un oggetto vero
ALTEZZE = {"26-scena-4": 0.032}


def cornice(g):
    """Rettangolo della tavola: righe e colonne con più della metà dei pixel non-carta (la riga della cornice)."""
    m = g < 205
    righe = np.nonzero(m.mean(axis=1) > 0.5)[0]
    colonne = np.nonzero(m.mean(axis=0) > 0.5)[0]
    return int(colonne.min()), int(righe.min()), int(colonne.max()), int(righe.max())


def copri(g, lato, altezza=0.052):
    """Copre l'angolo in basso del lato indicato con la trama della stessa fascia di righe, subito accanto verso il centro
    e specchiata in orizzontale: il tratteggio continua senza stacchi di tono, il bordo verso il centro si sfuma."""
    x0, y0, x1, y1 = cornice(g)
    pw, ph = x1 - x0, y1 - y0
    hz = max(20, int(altezza * ph))
    wz = int(0.24 * pw)
    ya, yb = y1 - 3 - hz, y1 - 3
    if lato == S:
        xa, xb = x0 + 3, x0 + 3 + wz
        sorgente = g[ya:yb, xb:xb + wz][:, ::-1].astype(np.float32)
        sfumo = np.clip((xb - np.arange(xa, xb, dtype=np.float32)) / 8.0, 0, 1)[None, :]
    else:
        xa, xb = x1 - 3 - wz, x1 - 3
        sorgente = g[ya:yb, xa - wz:xa][:, ::-1].astype(np.float32)
        sfumo = np.clip((np.arange(xa, xb, dtype=np.float32) - xa) / 8.0, 0, 1)[None, :]
    zona = g[ya:yb, xa:xb].astype(np.float32)
    g[ya:yb, xa:xb] = (sorgente * sfumo + zona * (1 - sfumo)).astype(np.uint8)


def elabora(src, dest, lati, altezza=0.052):
    im = Image.open(src).convert("L")
    g = np.array(im)
    for lato in lati:
        copri(g, lato, altezza)
    out = Image.fromarray(g)
    if out.width > 960:
        out = out.resize((960, round(out.height * 960 / out.width)), Image.LANCZOS)
    out.convert("RGB").save(dest, quality=84, optimize=True)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    cartella = pathlib.Path(sys.argv[1])
    scelte = set(sys.argv[2:])
    for f in sorted(cartella.glob("purgatorio-*.jpg")):
        if scelte and f.stem not in scelte:
            continue
        chiave = f.stem[len("purgatorio-"):]
        elabora(f, RADICE / "img" / "ia" / f.name, FIRME.get(chiave, ""), ALTEZZE.get(chiave, 0.052))
        print(f.name, FIRME.get(chiave, "-"), (RADICE / "img" / "ia" / f.name).stat().st_size // 1024, "KB")
