# PROGRESS — prototipo Inferno I–V

Obiettivo del prototipo: solo Inferno, canti I–V. Prima la qualità, poi si scala.

## Checklist

- [x] **Fase 1 – Struttura** — repo, `luoghi.json` (37 luoghi), schema dei canti, interfaccia con il Canto I di prova (15/11/2026)
- [x] **Fase 2 – Dataset** — canti I–V: testo, scene, tavole di Doré verificate, parafrasi, personaggi (15/11/2026)
- [x] **Fase 3 – Controllo** — computer e telefono, immagini, indirizzi (15/11/2026)

## Ritocchi dopo la Fase 3 (15/11/2026)

- **Versione nei file**: CSS, JavaScript e JSON si caricano con `?v=<versione>` (in `index.html`, cinque occorrenze uguali). A ogni pubblicazione si cambia la versione con una sostituzione: il browser non mescola file vecchi e nuovi. Istruzioni nel README.
- **Immagini più leggere su telefono**: le tavole hanno due misure, 500 px per gli schermi piccoli e 960 px per gli altri (`srcset`); il browser sceglie da sé. Wikimedia accetta solo larghezze standard: 640 px viene rifiutata (errore 400), 500 sì. Esempio tavola 1: 166 KB invece di 671 KB.
- **Tavola 17 del canto V**: sostituita con un'altra scansione della stessa incisione, 1808×2261 invece di 715×1083 (`Gustave Doré - The Inferno, Canto 5-2.jpg`, pubblico dominio). La tavola 16 resta l'originale: 979 px bastano, perché il sito ne mostra al massimo 960; l'unica alternativa su Commons è la foto di una pagina del libro con i margini.
- **Personaggi**: si vedono i primi 6, poi «e altri N» che mostra il resto (nobile castello: «e altri 10»; filosofi: «e altri 17»).
- **Difetto corretto**: cambiando canto (dal menu o con un indirizzo) mentre la pagina stava ancora reagendo allo scorrimento, l'app poteva riscrivere l'indirizzo con il canto vecchio e il cambio non avveniva. Ora, se l'indirizzo punta già a un altro canto, lo scorrimento non lo tocca.
- **Riprovato dopo i ritocchi**: telefono 28/28 scene con la tavola giusta (miniatura da 500 px), computer 28/28 scene in ordine scorrendo (miniatura da 960 px), passaggio tra tutti i canti, «e altri N», `verifica.py` 0 errori.

## Fase 3 (15/11/2026)

Prove nel pannello del browser, visibile, su computer (1280×800) e telefono (375×812), con tutti e cinque i canti.

| Prova | Computer | Telefono |
|---|---|---|
| Scorrendo dall'inizio alla fine di ogni canto, le scene si attivano tutte e nell'ordine giusto (indirizzo aggiornato) | 28/28 | 28/28 |
| A ogni scena compare la tavola giusta, caricata, oppure il riquadro «Nessuna tavola di Doré» | 28/28 | 28/28 |
| Apertura diretta di un indirizzo di scena: la scena è sotto la testata o sotto la tavola | sì | sì |
| Parafrasi al tocco, schede dei personaggi, mappa ingrandibile con Dante sul luogo giusto | sì | sì |
| Scorrimento orizzontale della pagina | no | no |

- **Immagini**: le 18 miniature (960 px) scaricate una per una dal server di Wikimedia: 18/18, tra 327 e 664 KB ciascuna.
- **Dati**: `verifica.py --rete` 0 errori (fatto a fine Fase 2, dati non più cambiati).
- **Corretto**: un indirizzo con una scena inesistente (es. `#inferno/2/scena-99`) apriva il canto lasciando l'indirizzo sbagliato; ora torna a `#inferno/2` all'inizio del canto (o resta sulla scena attuale se il canto è già aperto).
- **Non provato**: un telefono vero (solo la vista telefono del browser di Claude, Chromium), Safari su iPhone, la pubblicazione su GitHub Pages.

### Da sistemare prima di estendere a tutto l'Inferno

1. ~~Cache dopo gli aggiornamenti~~ — fatto (versione nei file).
2. ~~Peso delle immagini su telefono~~ — fatto (500 px). Resta da decidere se copiare le miniature nel repository invece di caricarle da Wikimedia.
3. ~~Tavole 16 e 17 del canto V~~ — fatto (17 sostituita, 16 sufficiente).
4. ~~Scene con molti personaggi~~ — fatto («e altri N»).
5. **Scrittura dei dati**: scene e parafrasi oggi passano da uno script di lavoro fuori dal repository; per i canti successivi conviene un formato sorgente nel repository (o scriverle direttamente nei JSON) e lasciare a `verifica.py` i controlli.

## Fase 2 (15/11/2026)

Scelta del proprietario durante la fase: **solo le incisioni di Doré**, gli altri illustratori non servono.

| Canto | Versi | Scene | Tavole di Doré | Scene senza tavola |
|---|---|---|---|---|
| I | 136 | 7 | 5 (tavole 1–5) | 2 |
| II | 142 | 5 | 2 (tavole 6–7; la 7 serve due scene) | 2 |
| III | 136 | 5 | 3 (tavole 8–10) | 2 |
| IV | 151 | 5 | 2 (tavole 11–12) | 3 |
| V | 142 | 6 | 6 (tavole 13–18; la 14 serve due scene) | 0 |
| **Totale** | **707** | **28** | **18** | **9** |

- **Testo**: da it.wikisource, 707 versi, confrontati verso per verso dallo script (`verifica.py --rete`: identici). Due sole normalizzazioni: apostrofi uniformati e collegamenti interni tolti (nel canto IV Wikisource ha `[[w:Orfeo|Orfeo]]`).
- **Scene**: 28, ognuna con luogo, sintesi, personaggi; iniziano sempre su una terzina.
- **Tavole**: tutte le 18 incisioni di Doré per i canti I–V (serie «Gustave Doré - Dante Alighieri - Inferno - Plate N» su Commons). Per ognuna lo script ha letto da Commons esistenza, URL e licenza (pubblico dominio); le ho anche guardate una per una per controllare il soggetto.
- **Parafrasi**: 239 terzine (tutte), in italiano corrente, una per terzina; tra parentesi quadre solo i nomi che il testo tace (es. «il padre di Silvio [Enea]»).
- **Personaggi**: 71, con 2–3 righe ciascuno. Segnati come discussi: lonza, leone, lupa, veltro, colui che fece il gran rifiuto, Diogene, Zenone, Lino.
- **Scene senza tavola**: 9, elencate con il motivo in `data/scene-senza-tavola.md`. Decisione del proprietario: restano vuote (niente riuso della tavola vicina).
- **Interfaccia**: con un solo illustratore il selettore non compare; al suo posto «Scena N di M» e il titolo della scena. Il codice del selettore resta: torna da solo se in `data/indice.json` si aggiunge un altro illustratore.

### Verifiche

- `python -I strumenti/verifica.py --rete`: 5 canti, **0 errori**, 9 avvisi (le 9 scene senza tavola).
- Nel browser: aperte tutte le 28 scene con il loro indirizzo (`#inferno/N/scena-K`): tavola giusta caricata o riquadro «Nessuna tavola di Doré per questa scena», luogo corretto sulla mappa.

### Da sapere

- Commons limita le richieste ravvicinate (errore 429) sia sull'API sia sulle miniature: lo script di verifica ora aspetta e riprova. Nel browser non è successo, ma con tutto l'Inferno (76 tavole di Doré) conviene valutare una copia delle miniature nel repository.
- Tavole 16 e 17 del canto V: su Commons, in questa serie, sono a bassa definizione (979 e 715 px di larghezza).

## Fase 1 (15/11/2026)

- Struttura: `index.html`, `css/`, `js/` (`dati.js`, `mappa.js`, `app.js`), `data/`, `esplora/` (vuota, con nota), `strumenti/` (Python, solo per preparare e verificare i dati).
- `data/luoghi.json`: selva, colle, porta, Antinferno, Acheronte, cerchi I–IX con nobile castello, tre gironi, dieci bolge, burrato, pozzo dei giganti, quattro zone di Cocito, Lucifero, natural burella. Coordinate astratte (profondità, raggio, angolo) spiegate in `data/SCHEMA.md`.
- Interfaccia provata nel browser (1024×768 e telefono 375×812): cambio della tavola allo scorrere delle scene, indirizzo aggiornato, apertura diretta di una scena, parafrasi al tocco, schede dei personaggi, mappa ingrandibile, modalità notte, nessuno scorrimento orizzontale su telefono.

### Decisioni

- Cartella del progetto: `C:\Users\simon\Desktop\Repos\CommediaIllustrata`. Repository Git locale, **nessun commit e nessun remoto**.
- Schema: aggiunti alle tavole `autore_id` e `miniatura` (960 px, per il telefono); al canto `fonte_testo` e `numero_versi`.
- Le immagini si caricano direttamente da `upload.wikimedia.org` (nessuna copia nel repository).
