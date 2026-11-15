"""Crea il sorgente di un canto nuovo con il testo da Wikisource, pronto da completare a mano.

Uso:  python -I strumenti/prepara.py inferno 6

Scrive `sorgenti/<cantica>/canto-NN.txt` solo se non esiste già. Il file ha tutte le terzine, con la
riga «>» della parafrasi vuota, e una sola scena segnaposto all'inizio: le scene si aggiungono
inserendo le intestazioni «## scena-K | luogo_id | titolo» prima della loro prima terzina.
"""
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import sorgente  # noqa: E402
import wikisource  # noqa: E402

RADICE = pathlib.Path(__file__).resolve().parent.parent

if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    cantica, canto = sys.argv[1].lower(), int(sys.argv[2])
    dest = RADICE / "sorgenti" / cantica / f"canto-{canto:02d}.txt"
    if dest.exists():
        raise SystemExit(f"{dest} esiste già: non lo sovrascrivo")
    page = f"Divina Commedia/{cantica.capitalize()}/Canto {wikisource.ROMANI[canto]}"
    versi = wikisource.versi(wikisource.wikitext(page))
    terzine = [{"versi": [i + 1, i + len(versi[i:i + 3])], "testo": "\n".join(versi[i:i + 3]), "parafrasi": ""}
               for i in range(0, len(versi), 3)]
    scena = {"id": "scena-1", "luogo_id": "?", "titolo_scena": "?", "personaggi": [], "tavole": [],
             "sintesi": "", "inizio": 1}
    testo = sorgente.scrivi(f"{cantica.capitalize()}, canto {wikisource.ROMANI[canto]}", "", [scena],
                            terzine, [])
    dest.parent.mkdir(parents=True, exist_ok=True)
    dest.write_text(testo, encoding="utf-8")
    print(f"Creato {dest} ({len(versi)} versi, {len(terzine)} terzine)")
