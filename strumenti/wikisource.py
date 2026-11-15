"""Scarica un canto da it.wikisource e lo trasforma in versi numerati.

Uso:  python -I strumenti/wikisource.py inferno 1
Stampa un JSON: { "fonte": url, "versi": ["...", ...] } (versi[0] = verso 1).

Controlli fatti (il programma si ferma se uno fallisce):
- ogni segnaposto {{R|n}} di Wikisource deve cadere esattamente sul verso n;
- il numero totale dei versi deve essere 3k+1 (terzine più il verso finale).
"""
import json
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

API = "https://it.wikisource.org/w/api.php"
UA = "CommediaIllustrata/0.1 (prototype; static site)"
ROMANI = ["", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII",
          "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX", "XXI", "XXII", "XXIII",
          "XXIV", "XXV", "XXVI", "XXVII", "XXVIII", "XXIX", "XXX", "XXXI", "XXXII", "XXXIII", "XXXIV"]


def wikitext(page):
    q = urllib.parse.urlencode({"action": "parse", "page": page, "prop": "wikitext",
                                "format": "json", "formatversion": "2"})
    req = urllib.request.Request(API + "?" + q, headers={"User-Agent": UA})
    for tentativo in range(6):          # Wikisource risponde 429 alle richieste troppo ravvicinate
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                d = json.load(r)
            break
        except urllib.error.HTTPError as e:
            if e.code != 429 or tentativo == 5:
                raise
            time.sleep(15 * (tentativo + 1))
    if "error" in d:
        raise SystemExit(f"Errore Wikisource: {d['error']}")
    return d["parse"]["wikitext"]


def versi(wt):
    m = re.search(r"<poem>(.*?)</poem>", wt, re.S)
    if not m:
        raise SystemExit("Nessun blocco <poem> trovato")
    t = m.group(1)
    t = re.sub(r"\{\{[Rr]\|(\d+)\}\}", r"⟦\1⟧", t)       # numeri dei versi ({{R|3}} o {{r|3}})
    # citazione di un altro testo: {{§§/Tc|ancora|testo mostrato|opera|pagina}} -> testo mostrato
    t = re.sub(r"\{\{§§/Tc\|[^|{}]*\|([^|{}]*)(?:\|[^{}]*)?\}\}", r"\1", t)
    t = re.sub(r"\{\{§\|[^|{}]*\}\}", "", t)               # ancore semplici
    t = re.sub(r"\{\{§\|[^|{}]*\|", "", t)                 # ancore che racchiudono testo
    # collegamenti ad autori/opere citati: {{AutoreCitato|Nome|testo}} o {{Ac|Nome|testo}} -> testo
    t = re.sub(r"\{\{(?:AutoreCitato|TestoCitato|Ac)\|[^|{}]*\|", "", t)
    t = t.replace("}}", "")
    t = re.sub(r"<[^>]+>", "", t)                          # tag html residui
    t = re.sub(r"\[\[(?:[^\]|]*\|)?([^\]]*)\]\]", lambda m: m.group(1), t)   # collegamenti wiki: [[w:Orfeo|Orfeo]] -> Orfeo
    t = t.replace("'", "’")                           # apostrofi uniformati (Wikisource ne mescola due tipi)
    if "{{" in t:
        raise SystemExit("Template non gestito: " + t[t.index("{{"):][:80])
    righe = [r.strip() for r in t.split("\n") if r.strip()]
    out = []
    for r in righe:
        num = re.search(r"⟦(\d+)⟧", r)
        testo = re.sub(r"\s*⟦\d+⟧", "", r).strip()
        out.append(testo)
        if num and int(num.group(1)) != len(out):
            raise SystemExit(f"Numerazione sfasata: segnaposto {num.group(1)} sul verso {len(out)}")
    if len(out) % 3 != 1:
        raise SystemExit(f"Numero di versi anomalo: {len(out)}")
    return out


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    cantica, canto = sys.argv[1].capitalize(), int(sys.argv[2])
    page = f"Divina Commedia/{cantica}/Canto {ROMANI[canto]}"
    v = versi(wikitext(page))
    url = "https://it.wikisource.org/wiki/" + urllib.parse.quote(page.replace(" ", "_"))
    print(json.dumps({"fonte": url, "versi": v}, ensure_ascii=False, indent=1))
