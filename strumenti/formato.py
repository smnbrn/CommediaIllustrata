"""Scrittura JSON uniforme per i file di /data (coppie di numeri su una riga)."""
import json
import re


def dumps(dati):
    testo = json.dumps(dati, ensure_ascii=False, indent=2)
    # [\n  1,\n  3\n] -> [1, 3]  (versi e coppie numeriche restano leggibili)
    testo = re.sub(r"\[\s+(-?\d+),\s+(-?\d+)\s+\]", r"[\1, \2]", testo)
    # elenchi brevi di stringhe su una riga
    testo = re.sub(r"\[\s+((?:\"[^\"\n]*\",?\s+){1,8})\]",
                   lambda m: "[" + ", ".join(s.strip().rstrip(",") for s in m.group(1).split("\n") if s.strip()) + "]",
                   testo)
    return testo + "\n"


def scrivi(percorso, dati):
    percorso.write_text(dumps(dati), encoding="utf-8")
