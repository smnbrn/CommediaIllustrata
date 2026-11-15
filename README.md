# Commedia Illustrata

La Divina Commedia letta scena per scena: il testo scorre e accanto cambia l'incisione di Gustave Doré per quella scena, con la parafrasi a portata di tocco e una mini-mappa che mostra dove si trova Dante.

Prototipo: Inferno, canti I–V. Online: https://smnbrn.github.io/CommediaIllustrata/

Sito statico (HTML, CSS, JavaScript senza librerie né build), pensato per GitHub Pages e per il telefono.

- Testo: [it.wikisource](https://it.wikisource.org/wiki/Divina_Commedia), verificato verso per verso.
- Tavole: le incisioni di Gustave Doré per l'Inferno (1861), pubblico dominio, da [Wikimedia Commons](https://commons.wikimedia.org/), con link alla pagina di ogni file.
- Dati in `/data`, documentati in [`data/SCHEMA.md`](data/SCHEMA.md); li leggerà anche la futura versione 3D in `/esplora`.

## Provare in locale

I dati si caricano con `fetch`, quindi serve un piccolo server (aprire `index.html` con doppio clic non basta):

```
python -m http.server 8130
```

e poi `http://localhost:8130/#inferno/1`.

## Indirizzi

`#inferno/1` apre il canto, `#inferno/1/scena-3` una scena precisa.

## Controllo dei dati

```
python -I strumenti/verifica.py          # coerenza interna
python -I strumenti/verifica.py --rete   # anche testo su Wikisource e tavole su Commons
```

## Pubblicare un aggiornamento

In `index.html` la versione compare cinque volte (`?v=…` di CSS e JS e `window.CI.versione`, usata anche per i file JSON). A ogni pubblicazione si cambia con una sola sostituzione, per esempio da `2026-11-15.1` a `2026-11-20.1`: così chi ha già aperto il sito non resta con file vecchi mescolati a dati nuovi.
