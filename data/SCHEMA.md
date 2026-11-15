# Schema dei dati

Tutti i file sono JSON in UTF-8. Li leggono il sito (`js/`) e, in futuro, la versione 3D in `/esplora` (build WebGL di Unity): nessun dato sta nel codice.

## Indirizzi

Ogni scena ha un indirizzo stabile `cantica/canto/scena`, per esempio `inferno/1/scena-3`.

- Nel sito: `index.html#inferno/1/scena-3` apre il canto e scorre alla scena.
- La versione 3D può aprire la lettura con `../index.html#inferno/1/scena-3`.
- `personaggi.json` usa lo stesso formato nelle `apparizioni`.

Gli `id` delle scene non si rinumerano mai dopo la pubblicazione: se una scena va divisa, la nuova prende un id nuovo (`scena-3b`), così i collegamenti esterni restano validi.

## `indice.json`

- `illustratori[]`: `id`, `nome`, `breve`, `opera`. Oggi c'è solo Doré; con due o più illustratori il sito mostra da solo il selettore (e, se per una scena manca quello scelto, la tavola di un altro).
- `cantiche[]`: `id`, `nome`, `canti[]` con `canto`, `titolo`, `file`, `disponibile`.

## `luoghi.json`

`luoghi[]`, ognuno con:

| campo | significato |
|---|---|
| `id` | identificativo (`selva-oscura`, `cerchio-2`, `bolgia-5`, `caina`…) |
| `nome` | nome da mostrare |
| `tipo` | `regione`, `soglia`, `fiume`, `cerchio`, `zona` |
| `cerchio` | numero del cerchio (1–9), `0` per l'Antinferno, `null` fuori dai cerchi |
| `zona_di` | `id` del luogo che la contiene (gironi, bolge, zone di Cocito), altrimenti `null` |
| `ordine` | ordine di discesa (1 = selva) |
| `coordinate.profondita` | 0 = superficie, 1 = centro della Terra |
| `coordinate.raggio` | ampiezza relativa della voragine: 1 = bordo, 0 = asse; > 1 in superficie (fuori dal bordo) |
| `coordinate.angolo` | gradi, senso antiorario visto dall'alto: dove Dante attraversa il luogo (cresce lungo la discesa, perché procede quasi sempre a sinistra) |
| `descrizione` | una riga |
| `canti` | canti in cui il luogo compare (`inferno/3`) |

Le coordinate sono **astratte**: servono a disporre i luoghi in modo coerente (mappa in sezione nel sito, scena 3D), non sono una ricostruzione topografica. In 3D: `y = -profondita`, punto sull'anello `x = cos(angolo) · raggio`, `z = sin(angolo) · raggio`.

## `inferno/canto-NN.json`

```
{ cantica, canto, titolo, fonte_testo, numero_versi,
  scene:   [ { id, versi: [inizio, fine], titolo_scena, sintesi, luogo_id,
               personaggi: [id], tavole: [ { autore_id, autore, titolo, url, miniatura, fonte, licenza } ] } ],
  terzine: [ { versi: [n, n+2], testo, parafrasi } ] }
```

- `terzine`: testo da it.wikisource, un verso per riga (`\n`); l'ultima «terzina» è il verso finale isolato (`[136, 136]`).
- `scene`: contigue, coprono tutti i versi, **iniziano sempre su una terzina** (così ogni terzina appartiene a una sola scena).
- `tavole`: solo pubblico dominio da Wikimedia Commons (oggi solo Doré). `url` = file originale, `miniatura` = versione larga 960 px (quella che il sito carica; uguale a `url` se l'originale è più piccolo), `fonte` = pagina del file su Commons. Più tavole per una scena: il sito le sfoglia con «1 / 2». La stessa tavola può servire due scene consecutive. Array vuoto se non c'è una tavola pertinente (elenco in `scene-senza-tavola.md`).
- `parafrasi`: stringa vuota finché non è scritta (il sito mostra «non ancora disponibile»).
- Campi con `_` iniziale (`_stato`, `_nota`) sono annotazioni di lavoro, ignorate dal sito.

## `personaggi.json`

`personaggi[]`: `id`, `nome`, `chi_era` (2–3 righe), `dibattuto` (true se identificazione o significato sono discussi), `apparizioni` (indirizzi delle scene).

## Strumenti (`/strumenti`, Python, non servono al sito)

- `nuovo_canto.py inferno 6 "Titolo"` crea lo scheletro del canto con il testo da Wikisource (controlla i numeri dei versi; non sovrascrive). Il testo è quello di Wikisource con due sole normalizzazioni: apostrofi uniformati (’) e collegamenti interni tolti.
- `commons.py cerca|categoria|info|tavola` cerca file su Commons e produce il JSON di una tavola solo se il file esiste ed è di pubblico dominio.
- `verifica.py` controlla coerenza interna; con `--rete` confronta il testo con Wikisource verso per verso e verifica ogni tavola su Commons (esistenza, URL, licenza, miniatura).
