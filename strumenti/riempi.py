"""Inserisce scene, parafrasi e personaggi in un sorgente, senza ricopiare i versi.

Uso:  python -I strumenti/riempi.py sorgenti/inferno/canto-06.txt bozza.txt

La bozza ha lo stesso formato del sorgente, ma al posto delle terzine ha righe «N > parafrasi»
(N = primo verso della terzina). Ogni scena inizia dalla prima terzina elencata sotto di lei.
Nella bozza vanno tutte le scene (sostituiscono quelle del sorgente); le parafrasi non elencate
restano quelle già scritte. Personaggi: quelli della bozza sostituiscono quelli del sorgente con lo
stesso id, gli altri si aggiungono.
"""
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import sorgente  # noqa: E402

if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    dest, bozza = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    testo = dest.read_text(encoding="utf-8")
    intestazione = testo.splitlines()[0].lstrip("# ").strip()
    src = sorgente.leggi(testo, str(dest))
    # la bozza: le righe «N > parafrasi» diventano terzine finte di una riga, poi si legge col parser normale
    righe, parafrasi = [], {}
    for r in bozza.read_text(encoding="utf-8").splitlines():
        m = re.match(r"\s*(\d+)\s*(?:>\s?|\s)(.*)$", r)
        if m:
            parafrasi[int(m.group(1))] = m.group(2).strip()
            righe += [m.group(1), "·", "> " + m.group(2)]
        else:
            righe.append(r)
    b = sorgente.leggi("\n".join(righe), str(bozza))
    inizi = {t["versi"][0] for t in src["terzine"]}
    for n in parafrasi:
        if n not in inizi:
            raise SystemExit(f"{bozza}: {n} non è l'inizio di una terzina")
    terzine = [{"versi": t["versi"], "testo": "\n".join(t["righe"]),
                "parafrasi": parafrasi.get(t["versi"][0], t["parafrasi"])} for t in src["terzine"]]
    scene = b["scene"] or src["scene"]
    for s in scene:
        s["tavole"] = [(t["autore_id"], t["titolo"], t["file"], t.get("scansione")) for t in s["tavole"]] \
            if s["tavole"] and isinstance(s["tavole"][0], dict) else s["tavole"]
    nuovi = {p["id"]: p for p in b["personaggi"]}
    personaggi = [nuovi.pop(p["id"], p) for p in src["personaggi"]] + list(nuovi.values())
    titolo = b["titolo"] or src["titolo"]
    dest.write_text(sorgente.scrivi(intestazione, titolo, scene, terzine, personaggi), encoding="utf-8")
    vuote = sum(1 for t in terzine if not t["parafrasi"])
    print(f"{dest}: {len(scene)} scene, {len(terzine) - vuote}/{len(terzine)} parafrasi, {len(personaggi)} personaggi")
