"""Formato sorgente dei canti (`sorgenti/<cantica>/canto-NN.txt`): lettura e scrittura.

Un file per canto, in testo semplice, scritto a mano. Esempio:

    # Inferno, canto VI
    titolo: Cerbero, i golosi, Ciacco

    ## scena-1 | cerchio-3 | La pioggia eterna
    personaggi: dante, virgilio, cerbero
    tavola: dore | Cerbero | File:Gustave Doré - Dante Alighieri - Inferno - Plate 19 (Canto VI - Cerberus).jpg
    sintesi: Dante si risveglia nel terzo cerchio...

    1
    Al tornar de la mente, che si chiuse
    dinanzi a la pietà d’i due cognati,
    che di trestizia tutto mi confuse,
    > Quando ripresi i sensi, che si erano chiusi...

    ## personaggio | ciacco | Ciacco
    dibattuto: sì
    chi_era: Fiorentino noto per la sua golosità...

Regole:
- `## scena-K | luogo_id | titolo`: la scena va dalla sua prima terzina fino a quella prima della scena
  successiva (le scene iniziano sempre su una terzina e sono contigue per costruzione).
- `tavola:` (zero, una o più righe): `dore | titolo | File:...` (file di Commons), oppure
  `dore | titolo | img/dore/...-960.jpg | File:...` (ritaglio nel repository, con accanto la versione
  `-500.jpg`; il file di Commons è la scansione da cui è ritagliato), oppure
  `ia | titolo | img/ia/...` (immagine generata con IA, nel repository).
- terzina: una riga con il numero del primo verso, poi i versi (testo di Wikisource), poi `> parafrasi`
  (`>` da solo se la parafrasi non è ancora scritta).
- `## personaggio | id | nome`: il personaggio si dichiara una volta sola, nel canto in cui compare per
  la prima volta; `dibattuto: sì|no`, `chi_era:` su una riga. Le apparizioni si ricavano dalle scene.
- Righe vuote e righe che iniziano con `#` (una sola) sono ignorate.
"""
import re

CHIAVI_SCENA = ("personaggi", "tavola", "sintesi")


class ErroreSorgente(Exception):
    pass


def leggi(testo, nome="sorgente"):
    canto = {"titolo": "", "scene": [], "terzine": [], "personaggi": []}
    scena = personaggio = terzina = None

    def errore(n, msg):
        raise ErroreSorgente(f"{nome}:{n}: {msg}")

    def chiudi_terzina(n):
        nonlocal terzina
        if terzina is not None:
            if terzina["parafrasi"] is None:
                errore(n, f"terzina {terzina['versi'][0]} senza riga «>»")
            canto["terzine"].append(terzina)
            terzina = None

    for n, riga in enumerate(testo.splitlines(), 1):
        r = riga.rstrip()
        if terzina is not None and terzina["parafrasi"] is None:
            # dentro una terzina: versi fino alla riga «>»
            if r.startswith(">"):
                terzina["parafrasi"] = r[1:].strip()
                chiudi_terzina(n)
            elif not r:
                errore(n, f"riga vuota dentro la terzina {terzina['versi'][0]}")
            else:
                terzina["righe"].append(r.strip())
            continue
        if not r or (r.startswith("#") and not r.startswith("##")):
            continue
        if r.startswith("## "):
            parti = [p.strip() for p in r[3:].split("|")]
            personaggio = scena = None
            if parti[0] == "personaggio":
                if len(parti) != 3:
                    errore(n, "atteso «## personaggio | id | nome»")
                personaggio = {"id": parti[1], "nome": parti[2], "chi_era": "", "dibattuto": None}
                canto["personaggi"].append(personaggio)
            elif re.fullmatch(r"scena-\w+", parti[0]):
                if len(parti) != 3:
                    errore(n, "atteso «## scena-K | luogo_id | titolo»")
                scena = {"id": parti[0], "luogo_id": parti[1], "titolo_scena": parti[2], "sintesi": "",
                         "personaggi": [], "tavole": [], "inizio": None}
                canto["scene"].append(scena)
            else:
                errore(n, f"intestazione sconosciuta: {parti[0]}")
            continue
        if re.fullmatch(r"\d+", r):
            if scena is None:
                errore(n, "terzina fuori da una scena")
            terzina = {"versi": [int(r), None], "righe": [], "parafrasi": None}
            if scena["inizio"] is None:
                scena["inizio"] = int(r)
            continue
        m = re.match(r"(\w+):\s?(.*)$", r)
        if not m:
            errore(n, f"riga non riconosciuta: {r[:60]}")
        k, v = m.group(1), m.group(2).strip()
        if scena is None and personaggio is None:
            if k != "titolo":
                errore(n, f"chiave «{k}» fuori da una scena")
            canto["titolo"] = v
        elif personaggio is not None:
            if k == "chi_era":
                personaggio["chi_era"] = v
            elif k == "dibattuto":
                if v not in ("sì", "no"):
                    errore(n, "dibattuto: sì oppure no")
                personaggio["dibattuto"] = v == "sì"
            else:
                errore(n, f"chiave sconosciuta per un personaggio: {k}")
        else:
            if scena["inizio"] is not None:
                errore(n, f"«{k}:» dopo le terzine della scena: va subito sotto l'intestazione")
            if k == "personaggi":
                scena["personaggi"] = [p.strip() for p in v.split(",") if p.strip()]
            elif k == "sintesi":
                scena["sintesi"] = v
            elif k == "tavola":
                p = [x.strip() for x in v.split("|")]
                locale = len(p) == 4 and p[0] == "dore" and p[2].startswith("img/dore/") and p[3].startswith("File:")
                if not (len(p) == 3 and p[0] in ("dore", "ia") or locale):
                    errore(n, "atteso «tavola: dore|ia | titolo | file» o «tavola: dore | titolo | img/dore/… | File:…»")
                scena["tavole"].append({"autore_id": p[0], "titolo": p[1], "file": p[2],
                                        "scansione": p[3] if locale else None})
            else:
                errore(n, f"chiave sconosciuta per una scena: {k}")
    if terzina is not None:
        errore("fine", f"terzina {terzina['versi'][0]} senza riga «>»")
    for t in canto["terzine"]:
        t["versi"][1] = t["versi"][0] + len(t["righe"]) - 1
    for s in canto["scene"]:
        if s["inizio"] is None:
            raise ErroreSorgente(f"{nome}: {s['id']} senza terzine")
    for p in canto["personaggi"]:
        if not p["chi_era"] or p["dibattuto"] is None:
            raise ErroreSorgente(f"{nome}: personaggio {p['id']} senza chi_era o dibattuto")
    return canto


def scrivi(intestazione, titolo, scene, terzine, personaggi):
    """scene: dict con id, luogo_id, titolo_scena, personaggi, tavole[(autore_id, titolo, file[, scansione])],
    sintesi, inizio."""
    out = [f"# {intestazione}", f"titolo: {titolo}", ""]
    per_inizio = {s["inizio"]: s for s in scene}
    for t in terzine:
        s = per_inizio.get(t["versi"][0])
        if s:
            out += [f"## {s['id']} | {s['luogo_id']} | {s['titolo_scena']}",
                    "personaggi: " + ", ".join(s["personaggi"])]
            out += ["tavola: " + " | ".join(x for x in t if x) for t in s["tavole"]]
            out += [f"sintesi: {s['sintesi']}", ""]
        out += [str(t["versi"][0]), *t["testo"].split("\n"), ("> " + t["parafrasi"]).rstrip(), ""]
    for p in personaggi:
        out += [f"## personaggio | {p['id']} | {p['nome']}",
                "dibattuto: " + ("sì" if p["dibattuto"] else "no"),
                f"chi_era: {p['chi_era']}", ""]
    return "\n".join(out).rstrip() + "\n"
