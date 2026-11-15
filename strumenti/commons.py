"""Ricerca e verifica di file su Wikimedia Commons.

Uso:
  python -I strumenti/commons.py cerca "Blake Inferno Minos"
  python -I strumenti/commons.py categoria "Inferno Canto 05"
  python -I strumenti/commons.py info "File:A.jpg" "File:B.jpg"
  python -I strumenti/commons.py tavola dore "Titolo italiano" "File:A.jpg"   -> JSON pronto per il canto

`info` e `tavola` leggono dall'API: esistenza, URL originale, dimensioni, licenza, autore.
`tavola` si rifiuta di produrre il JSON se la licenza non è di pubblico dominio.
"""
import json
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

API = "https://commons.wikimedia.org/w/api.php"
UA = "CommediaIllustrata/0.1 (prototype; static site)"
PD = ("public domain", "pd", "cc0", "public domain mark", "pd-art", "pdm")
NOMI = {"dore": "Gustave Doré", "botticelli": "Sandro Botticelli", "blake": "William Blake",
        "flaxman": "John Flaxman", "stradano": "Giovanni Stradano"}


def call(params):
    params = {**params, "format": "json", "formatversion": "2"}
    req = urllib.request.Request(API + "?" + urllib.parse.urlencode(params), headers={"User-Agent": UA})
    for tentativo in range(6):          # Commons risponde 429 alle richieste troppo ravvicinate
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            if e.code != 429 or tentativo == 5:
                raise
            time.sleep(15 * (tentativo + 1))


def pulisci(s):
    return re.sub(r"\s+", " ", re.sub(r"<[^>]+>", "", s or "")).strip()


def info(titoli):
    out = []
    for i in range(0, len(titoli), 40):
        d = call({"action": "query", "titles": "|".join(titoli[i:i + 40]), "prop": "imageinfo",
                  "iiprop": "url|extmetadata|size"})
        for p in d["query"]["pages"]:
            if p.get("missing") or "imageinfo" not in p:
                out.append({"titolo": p["title"], "manca": True})
                continue
            ii = p["imageinfo"][0]
            md = ii.get("extmetadata", {})
            g = lambda k: pulisci((md.get(k) or {}).get("value"))  # noqa: E731
            out.append({"titolo": p["title"], "url": ii["url"].split("?")[0], "pagina": ii["descriptionurl"],
                        "dimensioni": [ii.get("width"), ii.get("height")], "licenza": g("LicenseShortName"),
                        "autore": g("Artist")[:80], "descrizione": g("ImageDescription")[:200]})
    return out


def cerca(q, limite=40):
    d = call({"action": "query", "list": "search", "srnamespace": "6", "srsearch": q, "srlimit": str(limite)})
    return [s["title"] for s in d["query"]["search"]]


def categoria(nome, limite=200):
    d = call({"action": "query", "list": "categorymembers", "cmtitle": "Category:" + nome,
              "cmlimit": str(limite), "cmtype": "file|subcat"})
    return [m["title"] for m in d["query"]["categorymembers"]]


def tavola(autore_id, titolo, file):
    x = info([file])[0]
    if x.get("manca"):
        raise SystemExit(f"File inesistente su Commons: {file}")
    if x["licenza"].lower() not in PD:
        raise SystemExit(f"Licenza non di pubblico dominio ({x['licenza']}): {file}")
    url = x["url"]
    nome = url.rsplit("/", 1)[1]
    larghezza = x["dimensioni"][0] or 0
    mini = url if larghezza <= 960 else url.replace("/commons/", "/commons/thumb/") + "/960px-" + nome
    return {"autore_id": autore_id, "autore": NOMI[autore_id], "titolo": titolo, "url": url,
            "miniatura": mini, "fonte": x["pagina"], "licenza": "Pubblico dominio"}


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    cmd, arg = sys.argv[1], sys.argv[2:]
    if cmd == "cerca":
        print("\n".join(cerca(" ".join(arg))))
    elif cmd == "categoria":
        print("\n".join(categoria(arg[0])))
    elif cmd == "info":
        for x in info(arg):
            print(json.dumps(x, ensure_ascii=False))
    elif cmd == "tavola":
        print(json.dumps(tavola(*arg), ensure_ascii=False, indent=2))
