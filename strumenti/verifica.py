"""Controlla i dati di /data. Uso:  python -I strumenti/verifica.py [--rete]

Senza --rete (veloce, nessuna connessione):
- ogni canto: terzine consecutive e complete, scene contigue che coprono tutti i versi
  e iniziano su una terzina, luogo_id e personaggi esistenti, tavole con tutti i campi;
- personaggi: ogni apparizione punta a una scena esistente che elenca quel personaggio.
Con --rete:
- il testo di ogni canto coincide verso per verso con Wikisource;
- ogni tavola esiste su Commons (via API), con URL e licenza attesi; la miniatura risponde 200.
"""
import json
import pathlib
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

sys.path.insert(0, str(pathlib.Path(__file__).parent))
RADICE = pathlib.Path(__file__).resolve().parent.parent
DATI = RADICE / "data"
UA = "CommediaIllustrata/0.1 (prototype; static site)"
CAMPI_TAVOLA = ["autore_id", "autore", "titolo", "url", "miniatura", "fonte", "licenza"]

errori, avvisi = [], []


def carica(p):
    return json.loads(p.read_text(encoding="utf-8"))


def controlla_canto(p, luoghi, personaggi, illustratori):
    c = carica(p)
    nome = f"{c['cantica']}/{c['canto']}"
    t = c["terzine"]
    atteso = 1
    for x in t:
        a, b = x["versi"]
        righe = x["testo"].split("\n")
        if a != atteso:
            errori.append(f"{nome}: terzina che inizia a {a}, attesa {atteso}")
        if b - a + 1 != len(righe):
            errori.append(f"{nome}: terzina {a}-{b} con {len(righe)} righe")
        atteso = b + 1
    if atteso - 1 != c["numero_versi"]:
        errori.append(f"{nome}: terzine fino a {atteso - 1}, numero_versi {c['numero_versi']}")
    inizi = {x["versi"][0] for x in t}
    fine = 0
    for s in c["scene"]:
        a, b = s["versi"]
        if a != fine + 1:
            errori.append(f"{nome} {s['id']}: inizia a {a}, attesa {fine + 1}")
        if a not in inizi:
            errori.append(f"{nome} {s['id']}: il verso {a} non è l'inizio di una terzina")
        fine = b
        if s["luogo_id"] not in luoghi:
            errori.append(f"{nome} {s['id']}: luogo_id sconosciuto {s['luogo_id']}")
        for pid in s["personaggi"]:
            if pid not in personaggi:
                errori.append(f"{nome} {s['id']}: personaggio sconosciuto {pid}")
            elif f"{nome}/{s['id']}" not in personaggi[pid]["apparizioni"]:
                errori.append(f"{nome} {s['id']}: manca l'apparizione in personaggi.json per {pid}")
        for tv in s["tavole"]:
            if tv.get("autore_id") == "ia":       # immagine generata con IA: file nel repository, niente Commons
                f = RADICE / tv.get("url", "")
                if not tv.get("url", "").startswith("img/ia/") or not f.is_file():
                    errori.append(f"{nome} {s['id']}: immagine IA mancante nel repository: {tv.get('url')}")
                if "IA" not in tv.get("autore", ""):
                    errori.append(f"{nome} {s['id']}: immagine IA senza la dicitura IA nell'autore")
            elif tv.get("url", "").startswith("img/"):      # ritaglio di Doré nel repository: due misure
                for f in (tv["url"], tv["url"].replace("-960.jpg", "-500.jpg")):
                    if not tv["url"].startswith("img/dore/") or not tv["url"].endswith("-960.jpg") or not (RADICE / f).is_file():
                        errori.append(f"{nome} {s['id']}: ritaglio mancante nel repository: {f}")
            for k in CAMPI_TAVOLA:
                if not tv.get(k):
                    errori.append(f"{nome} {s['id']}: tavola senza {k}")
            if tv.get("autore_id") not in illustratori:
                errori.append(f"{nome} {s['id']}: autore_id sconosciuto {tv.get('autore_id')}")
        if not s["tavole"]:
            avvisi.append(f"{nome} {s['id']} «{s['titolo_scena']}»: nessuna tavola")
    if c["scene"] and fine != c["numero_versi"]:
        errori.append(f"{nome}: le scene arrivano al verso {fine} su {c['numero_versi']}")
    vuote = [x["versi"][0] for x in t if not x["parafrasi"]]
    if vuote:
        avvisi.append(f"{nome}: {len(vuote)} terzine senza parafrasi")
    return c


def get(url, testa=False):
    """Richiesta con pazienza: Wikimedia risponde 429 se le richieste sono troppo ravvicinate."""
    for tentativo in range(5):
        req = urllib.request.Request(url, headers={"User-Agent": UA}, method="HEAD" if testa else "GET")
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                return r.status, (None if testa else r.read())
        except urllib.error.HTTPError as e:
            if e.code != 429 or tentativo == 4:
                return e.code, None
            time.sleep(10 * (tentativo + 1))
    return None, None


def controlla_rete(canti):
    import commons
    import wikisource
    tavole = {}          # url -> (dove, tavola): ogni file si controlla una volta sola
    for c in canti:
        nome = f"{c['cantica']}/{c['canto']}"
        page = f"Divina Commedia/{c['cantica'].capitalize()}/Canto {wikisource.ROMANI[c['canto']]}"
        versi = wikisource.versi(wikisource.wikitext(page))
        nostri = [r for x in c["terzine"] for r in x["testo"].split("\n")]
        if versi != nostri:
            diff = next((i for i, (a, b) in enumerate(zip(versi, nostri)) if a != b), min(len(versi), len(nostri)))
            errori.append(f"{nome}: testo diverso da Wikisource dal verso {diff + 1}")
        else:
            print(f"  testo {nome}: {len(versi)} versi identici a Wikisource")
        time.sleep(1)
        for s in c["scene"]:
            for tv in s["tavole"]:
                if tv.get("autore_id") != "ia":
                    tavole.setdefault(tv["url"], (f"{nome} {s['id']}", tv))
    titoli = {"File:" + urllib.parse.unquote(tv["fonte"].split("/wiki/File:", 1)[1]).replace("_", " "): (dove, tv)
              for dove, tv in tavole.values()}
    trovati = {x["titolo"]: x for x in commons.info(list(titoli))}
    for titolo, (dove, tv) in titoli.items():
        x = trovati.get(titolo)
        if not x or x.get("manca"):
            errori.append(f"{dove}: file inesistente su Commons: {titolo}")
            continue
        locale = tv["url"].startswith("img/")         # ritaglio nel repository: su Commons c'è la scansione
        if not locale and x["url"] != tv["url"]:
            errori.append(f"{dove}: URL diverso da Commons per {titolo}: {x['url']}")
        if x["licenza"].lower() not in commons.PD:
            errori.append(f"{dove}: licenza non di pubblico dominio ({x['licenza']}) per {titolo}")
        if locale:
            print(f"  tavola {titolo[5:70]}: {x['licenza']}, ritaglio nel repository")
            continue
        time.sleep(2)
        stato = get(tv["miniatura"], testa=True)[0]
        if stato != 200:
            errori.append(f"{dove}: miniatura non raggiungibile ({stato})")
        print(f"  tavola {titolo[5:70]}: {x['licenza']}, miniatura {stato}")
    print(f"  {len(titoli)} file distinti controllati su Commons")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    luoghi = {l["id"]: l for f in sorted(DATI.glob("luoghi*.json")) for l in carica(f)["luoghi"]}
    personaggi = {p["id"]: p for p in carica(DATI / "personaggi.json")["personaggi"]}
    illustratori = {i["id"] for i in carica(DATI / "indice.json")["illustratori"]}
    canti = [controlla_canto(p, luoghi, personaggi, illustratori) for p in sorted(DATI.glob("*/canto-*.json"))]
    for pid, p in personaggi.items():
        for ap in p["apparizioni"]:
            cantica, n, scena = ap.split("/")
            c = next((x for x in canti if x["cantica"] == cantica and x["canto"] == int(n)), None)
            s = c and next((x for x in c["scene"] if x["id"] == scena), None)
            if not s:
                errori.append(f"personaggi {pid}: apparizione {ap} non trovata")
            elif pid not in s["personaggi"]:
                errori.append(f"personaggi {pid}: la scena {ap} non lo elenca")
    if "--rete" in sys.argv:
        controlla_rete(canti)
    for a in avvisi:
        print("avviso:", a)
    for e in errori:
        print("ERRORE:", e)
    print(f"{len(canti)} canti, {len(errori)} errori, {len(avvisi)} avvisi")
    sys.exit(1 if errori else 0)
