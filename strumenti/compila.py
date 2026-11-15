"""Costruisce i JSON di /data dai sorgenti in /sorgenti.

Uso:  python -I strumenti/compila.py

Legge tutti i `sorgenti/<cantica>/canto-NN.txt` (formato in `sorgente.py`) e scrive:
- `data/<cantica>/canto-NN.json` (testo, scene, tavole, parafrasi);
- `data/personaggi.json` (personaggi dichiarati nei sorgenti, apparizioni ricavate dalle scene);
- `data/indice.json` (elenco dei canti disponibili; illustratori invariati).

Le tavole di Doré si leggono da Commons (esistenza, URL, licenza) solo la prima volta: poi si riprendono
dal JSON già scritto. Il programma si ferma al primo errore e non scrive nulla.
"""
import json
import pathlib
import re
import sys
import urllib.parse

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import formato  # noqa: E402
import sorgente  # noqa: E402
import wikisource  # noqa: E402

RADICE = pathlib.Path(__file__).resolve().parent.parent
DATI = RADICE / "data"
SORGENTI = RADICE / "sorgenti"
IA = {"autore": "Illustrazione generata con IA",
      "fonte": "https://github.com/smnbrn/CommediaIllustrata/blob/main/data/illustrazioni-ia.md",
      "licenza": "Generata con Gemini nello stile di Doré, non è un'opera di Doré"}


def carica(p):
    return json.loads(p.read_text(encoding="utf-8"))


def titolo_file(fonte):
    return "File:" + urllib.parse.unquote(fonte.split("/wiki/File:", 1)[1]).replace("_", " ")


def tavole_note():
    """Tavole già risolte nei JSON esistenti, per titolo del file su Commons."""
    note = {}
    for p in DATI.glob("*/canto-*.json"):
        for s in carica(p)["scene"]:
            for tv in s["tavole"]:
                if tv["autore_id"] != "ia":
                    note[titolo_file(tv["fonte"])] = tv
    return note


def tavola(t, note, errori, dove):
    if t["autore_id"] == "ia":
        if not (RADICE / t["file"]).is_file():
            errori.append(f"{dove}: immagine IA inesistente: {t['file']}")
        return {"autore_id": "ia", "autore": IA["autore"], "titolo": t["titolo"], "url": t["file"],
                "miniatura": t["file"], "fonte": IA["fonte"], "licenza": IA["licenza"]}
    if t.get("scansione"):
        # ritaglio nel repository: i file -960 e -500 devono esistere; licenza e pagina dalla scansione su Commons
        for f in (t["file"], t["file"].replace("-960.jpg", "-500.jpg")):
            if not t["file"].endswith("-960.jpg") or not (RADICE / f).is_file():
                errori.append(f"{dove}: ritaglio mancante o con nome non valido (…-960.jpg e …-500.jpg): {f}")
                return None
        nota = note.get(t["scansione"])
        if nota is None:
            import commons
            try:
                nota = commons.tavola("dore", t["titolo"], t["scansione"])
            except SystemExit as e:
                errori.append(f"{dove}: {e}")
                return None
            note[t["scansione"]] = nota
            print(f"  Commons: {t['scansione'][5:80]}")
        return {**nota, "titolo": t["titolo"], "url": t["file"], "miniatura": t["file"]}
    if not t["file"].startswith("File:"):
        errori.append(f"{dove}: il file di Commons deve iniziare con «File:»")
        return None
    nota = note.get(t["file"])
    if nota is not None and nota["url"].startswith("img/"):
        nota = None                     # la stessa scansione usata prima per un ritaglio: si rilegge da Commons
    if nota is None:
        import commons
        try:
            nota = commons.tavola("dore", t["titolo"], t["file"])
        except SystemExit as e:
            errori.append(f"{dove}: {e}")
            return None
        note[t["file"]] = nota
        print(f"  Commons: {t['file'][5:80]}")
    return {**nota, "titolo": t["titolo"]}


def compila_canto(p, note, errori):
    cantica = p.parent.name
    n = int(re.fullmatch(r"canto-(\d+)\.txt", p.name).group(1))
    nome = f"{cantica}/{n}"
    src = sorgente.leggi(p.read_text(encoding="utf-8"), str(p.relative_to(RADICE)))
    terzine = [{"versi": t["versi"], "testo": "\n".join(t["righe"]), "parafrasi": t["parafrasi"]}
               for t in src["terzine"]]
    atteso = 1
    for t in terzine:
        if t["versi"][0] != atteso:
            errori.append(f"{nome}: terzina che inizia a {t['versi'][0]}, attesa {atteso}")
        atteso = t["versi"][1] + 1
    numero_versi = atteso - 1
    scene = []
    inizi = [s["inizio"] for s in src["scene"]]
    if inizi and inizi[0] != 1:
        errori.append(f"{nome}: la prima scena inizia al verso {inizi[0]}")
    if inizi != sorted(inizi):
        errori.append(f"{nome}: scene non in ordine")
    ids = [s["id"] for s in src["scene"]]
    if len(set(ids)) != len(ids):
        errori.append(f"{nome}: id di scena ripetuti")
    for i, s in enumerate(src["scene"]):
        fine = inizi[i + 1] - 1 if i + 1 < len(inizi) else numero_versi
        dove = f"{nome} {s['id']}"
        tv = [tavola(t, note, errori, dove) for t in s["tavole"]]
        scene.append({"id": s["id"], "versi": [s["inizio"], fine], "titolo_scena": s["titolo_scena"],
                      "sintesi": s["sintesi"], "luogo_id": s["luogo_id"], "personaggi": s["personaggi"],
                      "tavole": [x for x in tv if x]})
    page = f"Divina Commedia/{cantica.capitalize()}/Canto {wikisource.ROMANI[n]}"
    dati = {"cantica": cantica, "canto": n, "titolo": src["titolo"],
            "fonte_testo": "https://it.wikisource.org/wiki/" + page.replace(" ", "_"),
            "numero_versi": numero_versi, "scene": scene, "terzine": terzine}
    return dati, src["personaggi"]


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    errori = []
    note = tavole_note()
    canti, personaggi = [], {}
    for p in sorted(SORGENTI.glob("*/canto-*.txt")):
        if "\n## scena-1 | ? | ?\n" in p.read_text(encoding="utf-8"):
            print(f"  da scrivere, saltato: {p.relative_to(RADICE)}")
            continue
        try:
            dati, pers = compila_canto(p, note, errori)
        except sorgente.ErroreSorgente as e:
            errori.append(str(e))
            continue
        canti.append(dati)
        for x in pers:
            if x["id"] in personaggi:
                errori.append(f"personaggio {x['id']} dichiarato due volte "
                              f"({personaggi[x['id']]['_canto']} e {dati['cantica']}/{dati['canto']})")
            personaggi[x["id"]] = {**x, "_canto": f"{dati['cantica']}/{dati['canto']}", "apparizioni": []}
    ordine_cantiche = ["inferno", "purgatorio", "paradiso"]
    canti.sort(key=lambda c: (ordine_cantiche.index(c["cantica"]), c["canto"]))
    for c in canti:
        for s in c["scene"]:
            for pid in s["personaggi"]:
                if pid not in personaggi:
                    errori.append(f"{c['cantica']}/{c['canto']} {s['id']}: personaggio non dichiarato: {pid}")
                else:
                    personaggi[pid]["apparizioni"].append(f"{c['cantica']}/{c['canto']}/{s['id']}")
    luoghi = {l["id"] for l in carica(DATI / "luoghi.json")["luoghi"]}
    for c in canti:
        for s in c["scene"]:
            if s["luogo_id"] not in luoghi:
                errori.append(f"{c['cantica']}/{c['canto']} {s['id']}: luogo sconosciuto {s['luogo_id']}")
    for pid, x in personaggi.items():
        if not x["apparizioni"]:
            errori.append(f"personaggio {pid} ({x['_canto']}) senza apparizioni")
    if errori:
        for e in errori:
            print("ERRORE:", e)
        raise SystemExit(f"{len(errori)} errori: nessun file scritto")

    for c in canti:
        formato.scrivi(DATI / c["cantica"] / f"canto-{c['canto']:02d}.json", c)
    vecchio = carica(DATI / "personaggi.json")
    formato.scrivi(DATI / "personaggi.json", {
        "_nota": vecchio.get("_nota", ""),
        "personaggi": [{"id": x["id"], "nome": x["nome"], "chi_era": x["chi_era"], "dibattuto": x["dibattuto"],
                        "apparizioni": x["apparizioni"]} for x in personaggi.values()]})
    indice = carica(DATI / "indice.json")
    for cant in indice["cantiche"]:
        vecchi = {x["canto"]: x for x in cant["canti"]}
        for c in canti:
            if c["cantica"] == cant["id"]:
                vecchi[c["canto"]] = {"canto": c["canto"], "titolo": c["titolo"],
                                      "file": f"data/{c['cantica']}/canto-{c['canto']:02d}.json",
                                      "disponibile": True}
        cant["canti"] = [vecchi[k] for k in sorted(vecchi)]
    formato.scrivi(DATI / "indice.json", indice)
    print(f"{len(canti)} canti, {sum(len(c['scene']) for c in canti)} scene, {len(personaggi)} personaggi")
