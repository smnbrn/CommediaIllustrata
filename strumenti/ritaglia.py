"""Ritaglia l'incisione dalla scansione di una pagina e salva le due misure per il sito.

Uso:  python -I strumenti/ritaglia.py "File:Gottliche Komodie. T. 2, Das Fegefeuer 1871 (148974123).jpg" purgatorio-01-catone
      python -I strumenti/ritaglia.py ... nome --riquadro x0 y0 x1 y1     (frazioni 0-1 della pagina, se il ritaglio automatico sbaglia)

Scarica la pagina da Commons (versione larga 2400 px), cerca il rettangolo inciso (zona con tratti scuri
dentro la pagina color carta), toglie i margini e scrive `img/dore/<nome>-960.jpg` e `-500.jpg`.
Stampa il riquadro scelto: va sempre guardato il risultato.
"""
import io
import pathlib
import sys
import time
import urllib.request

from PIL import Image

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import commons  # noqa: E402

RADICE = pathlib.Path(__file__).resolve().parent.parent


def scarica(titolo, larghezza=2400):
    r = commons.call({"action": "query", "titles": titolo, "prop": "imageinfo", "iiprop": "url", "iiurlwidth": larghezza})
    u = r["query"]["pages"][0]["imageinfo"][0]["thumburl"]
    for t in range(6):
        try:
            return Image.open(io.BytesIO(urllib.request.urlopen(urllib.request.Request(u, headers={"User-Agent": commons.UA}), timeout=60).read())).convert("L")
        except Exception:
            time.sleep(10 * (t + 1))
    raise SystemExit("download non riuscito")


def corsa_piu_lunga(v, ok):
    """(inizio, fine) della sequenza più lunga di valori che soddisfano ok."""
    best, i = (0, 0), 0
    while i < len(v):
        if ok(v[i]):
            j = i
            while j < len(v) and ok(v[j]):
                j += 1
            if j - i > best[1] - best[0]:
                best = (i, j)
            i = j
        else:
            i += 1
    return best


def taglia(g, riq=None):
    """Riquadro dell'incisione: componente connessa (al centro della pagina) dei blocchetti di grigio medio,
    cioè né la carta chiara né la copertina scura, dopo aver staccato i ponti sottili (ombra della rilegatura)."""
    import numpy as np
    w, h = g.size
    if riq:
        return g.crop((int(riq[0] * w), int(riq[1] * h), int(riq[2] * w), int(riq[3] * h)))
    B = max(6, w // 400)
    a = np.asarray(g, dtype=np.float32)
    a = a[: h // B * B, : w // B * B]
    media = a.reshape(a.shape[0] // B, B, a.shape[1] // B, B).mean(axis=(1, 3))
    m = (media > 62) & (media < 203)
    def eroso(x):
        y = x.copy()
        y[1:] &= x[:-1]; y[:-1] &= x[1:]; y[:, 1:] &= x[:, :-1]; y[:, :-1] &= x[:, 1:]
        return y
    def dilata(x):
        y = x.copy()
        y[1:] |= x[:-1]; y[:-1] |= x[1:]; y[:, 1:] |= x[:, :-1]; y[:, :-1] |= x[:, 1:]
        return y
    N = 4
    e = m
    for _ in range(N):
        e = eroso(e)
    bh, bw = e.shape
    if not e.any():
        raise SystemExit("nessuna incisione trovata: usa --riquadro")
    # la componente connessa più grande (la tavola è l'oggetto più esteso della pagina)
    etichetta = np.zeros(e.shape, dtype=np.int32)
    migliore, area_migliore, n = None, 0, 0
    for y0 in range(bh):
        for x0 in range(bw):
            if e[y0, x0] and not etichetta[y0, x0]:
                n += 1
                pila = [(y0, x0)]
                etichetta[y0, x0] = n
                area = 0
                while pila:
                    y, x = pila.pop()
                    area += 1
                    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        yy, xx = y + dy, x + dx
                        if 0 <= yy < bh and 0 <= xx < bw and e[yy, xx] and not etichetta[yy, xx]:
                            etichetta[yy, xx] = n
                            pila.append((yy, xx))
                if area > area_migliore:
                    migliore, area_migliore = n, area
    visto = etichetta == migliore
    for _ in range(N):
        visto = dilata(visto)
    ys, xs = np.nonzero(visto & m)
    mrg = 1
    return g.crop(((xs.min() + mrg) * B, (ys.min() + mrg) * B, (xs.max() + 1 - mrg) * B, (ys.max() + 1 - mrg) * B))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    titolo, nome = sys.argv[1], sys.argv[2]
    riq = [float(x) for x in sys.argv[sys.argv.index("--riquadro") + 1:][:4]] if "--riquadro" in sys.argv else None
    g = scarica(titolo)
    c = taglia(g, riq)
    print(f"pagina {g.size} -> ritaglio {c.size}")
    for lar in (960, 500):
        k = lar / c.width
        c.resize((lar, round(c.height * k)), Image.LANCZOS).save(RADICE / "img" / "dore" / f"{nome}-{lar}.jpg", quality=84, optimize=True)
    print("scritti", nome)
