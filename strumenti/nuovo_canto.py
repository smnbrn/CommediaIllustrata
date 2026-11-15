"""Crea lo scheletro di un canto (testo da Wikisource, terzine numerate).

Uso:  python -I strumenti/nuovo_canto.py inferno 1 "Titolo del canto"
Scrive data/<cantica>/canto-NN.json solo se non esiste già (non sovrascrive mai
scene e parafrasi scritte a mano).
"""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import formato  # noqa: E402
import wikisource  # noqa: E402

RADICE = pathlib.Path(__file__).resolve().parent.parent


def terzine(versi):
    out = []
    for i in range(0, len(versi), 3):
        gruppo = versi[i:i + 3]
        out.append({"versi": [i + 1, i + len(gruppo)], "testo": "\n".join(gruppo), "parafrasi": ""})
    return out


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    cantica, canto = sys.argv[1].lower(), int(sys.argv[2])
    titolo = sys.argv[3] if len(sys.argv) > 3 else ""
    dest = RADICE / "data" / cantica / f"canto-{canto:02d}.json"
    if dest.exists():
        raise SystemExit(f"{dest} esiste già: non lo sovrascrivo")
    page = f"Divina Commedia/{cantica.capitalize()}/Canto {wikisource.ROMANI[canto]}"
    versi = wikisource.versi(wikisource.wikitext(page))
    dati = {
        "cantica": cantica,
        "canto": canto,
        "titolo": titolo,
        "fonte_testo": "https://it.wikisource.org/wiki/" + page.replace(" ", "_"),
        "numero_versi": len(versi),
        "scene": [],
        "terzine": terzine(versi),
    }
    dest.parent.mkdir(parents=True, exist_ok=True)
    formato.scrivi(dest, dati)
    print(f"Creato {dest} ({len(versi)} versi, {len(dati['terzine'])} terzine)")
