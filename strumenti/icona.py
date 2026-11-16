"""Disegna l'icona dell'app e ne scrive le varie misure in img/icone/.

Un capolettera da codice miniato: la C di «Commedia» color carta dentro un
riquadro rosso rubrica con filetto d'oro, su fondo di carta.

Uso:  python strumenti/icona.py
Serve Pillow e il Palatino Linotype di Windows (pala.ttf).
"""
import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

RADICE = Path(__file__).resolve().parent.parent
USCITA = RADICE / "img" / "icone"
CARATTERE = "C:/Windows/Fonts/pala.ttf"

CARTA = (243, 234, 214)      # --carta
CARTA_BORDO = (224, 209, 175)
RUBRICA = (142, 47, 27)      # --rubrica
ORO = (214, 170, 84)
OMBRA = (201, 184, 148)

LATO = 2048                  # si disegna in grande, poi si riduce


def fondo():
    """Carta con un'ombra leggera verso i bordi."""
    piccolo = 256
    velo = Image.new("L", (piccolo, piccolo))
    px = velo.load()
    c = (piccolo - 1) / 2
    for y in range(piccolo):
        for x in range(piccolo):
            d = math.hypot(x - c, y - c) / (c * math.sqrt(2))
            px[x, y] = round(255 * min(1, max(0, (d - 0.35) / 0.65)) ** 1.6)
    velo = velo.resize((LATO, LATO), Image.BICUBIC)
    return Image.composite(Image.new("RGB", (LATO, LATO), CARTA_BORDO),
                           Image.new("RGB", (LATO, LATO), CARTA), velo)


def icona(scala=1.0):
    """scala < 1 rimpicciolisce il segno (per le icone ritagliabili)."""
    img = fondo()
    d = ImageDraw.Draw(img)
    # il riquadro del capolettera, con la sua ombra sulla carta
    m = LATO * 0.34 * scala
    a, b = LATO / 2 - m, LATO / 2 + m
    r = LATO * 0.035 * scala
    sposta = LATO * 0.008
    d.rounded_rectangle((a, a + sposta, b, b + sposta), r, fill=OMBRA)
    d.rounded_rectangle((a, a, b, b), r, fill=RUBRICA)
    # filetto d'oro
    f = LATO * 0.036 * scala
    d.rounded_rectangle((a + f, a + f, b - f, b - f), r * 0.45,
                        outline=ORO, width=max(2, round(LATO * 0.009 * scala)))
    # la lettera
    car = ImageFont.truetype(CARATTERE, round(LATO * 0.6 * scala))
    sx, su, dx, giu = car.getbbox("C")
    x0 = (LATO - (dx - sx)) / 2 - sx - LATO * 0.008 * scala
    y0 = (LATO - (giu - su)) / 2 - su
    d.text((x0, y0), "C", font=car, fill=CARTA)
    return img


def arrotonda(img, raggio=0.2):
    lato = img.size[0]
    maschera = Image.new("L", img.size, 0)
    ImageDraw.Draw(maschera).rounded_rectangle((0, 0, lato - 1, lato - 1), lato * raggio, fill=255)
    fuori = img.convert("RGBA")
    fuori.putalpha(ImageChops.multiply(maschera, Image.new("L", img.size, 255)))
    return fuori


def salva(img, nome, lato):
    USCITA.mkdir(parents=True, exist_ok=True)
    img.resize((lato, lato), Image.LANCZOS).save(USCITA / nome, optimize=True)
    print(nome, lato)


def main():
    piena = icona()                 # a tutto campo: iOS arrotonda da sé
    tonda = arrotonda(piena)        # angoli già arrotondati: schede e desktop
    ritaglio = icona(scala=0.8)     # il segno resta nella zona sicura di Android
    salva(piena, "apple-touch-icon.png", 180)
    salva(tonda, "icona-32.png", 32)
    salva(tonda, "icona-192.png", 192)
    salva(tonda, "icona-512.png", 512)
    salva(ritaglio, "icona-ritagliabile-192.png", 192)
    salva(ritaglio, "icona-ritagliabile-512.png", 512)


if __name__ == "__main__":
    main()
