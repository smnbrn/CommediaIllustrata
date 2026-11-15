# PROGRESS — Inferno

Il prototipo (canti I–V) è servito a fissare la qualità; ora l'Inferno è completo.

## Checklist

- [x] **Fase 1 – Struttura** — repo, `luoghi.json` (37 luoghi), schema dei canti, interfaccia con il Canto I di prova (15/11/2026)
- [x] **Fase 2 – Dataset** — canti I–V: testo, scene, tavole di Doré verificate, parafrasi, personaggi (15/11/2026)
- [x] **Fase 3 – Controllo** — computer e telefono, immagini, indirizzi (15/11/2026)
- [x] **Fase 4 – Inferno completo** — canti VI–XXXIV con lo stesso livello dei primi cinque (15/11/2026)
- [x] **Fase 5 – Pubblicazione e controllo dell'Inferno completo nel browser** (15/11/2026)

## Fase 5 – Pubblicazione e controllo (15/11/2026)

Pubblicato con il commit `fb67b2d` (versione `2026-11-15.7`). Prove sul sito pubblicato, nel pannello del browser visibile:

| Prova | Computer 1280×800 | Telefono 375×812 |
|---|---|---|
| Ogni scena aperta dal suo indirizzo: tavola giusta caricata, oppure il riquadro «Nessuna tavola» | 145/145 | 145/145 |
| Scorrendo ogni canto dall'inizio alla fine, le scene si attivano tutte e nell'ordine giusto | 34/34 canti | 34/34 canti |
| Tavole sfogliabili (Ugolino 1/3–3/3, finale del canto XXXIV 1/2–2/2) | sì | — |
| Scorrimento orizzontale della pagina | no | no |
| Errori nella console del sito | nessuno | nessuno |

- Sul telefono si caricano le versioni da 500 px (70 scene su 72 con tavola; nelle altre due il browser ha scelto quella da 960 px, forse perché era già in memoria dalla prova sul computer).
- Con il pannello del browser nascosto la pagina non si ridisegna e lo scorrimento non aggiorna l'indirizzo: le prove vanno fatte col pannello visibile.
- **Non provato**: un telefono vero, Safari su iPhone.

## Fase 4 – Inferno completo (15/11/2026)

| | Canti I–V | Canti VI–XXXIV | Totale |
|---|---|---|---|
| Versi | 707 | 4013 | 4720 |
| Terzine con parafrasi | 239 | 1357 | 1596 |
| Scene | 28 | 117 | 145 |
| Tavole di Doré | 18 | 55 (26 ritagliate) | 73 (+1 IA) |
| Scene senza tavola | 8 | 65 | 73 |
| Personaggi | 71 | 124 | 195 |

- **Formato sorgente (punto 5 qui sotto, risolto)**: i canti si scrivono in `sorgenti/inferno/canto-NN.txt` (versi, scene, tavole, parafrasi, personaggi in un solo file di testo) e `strumenti/compila.py` genera i JSON. I canti I–V sono stati convertiti e ricompilati: JSON identici byte per byte. Le apparizioni dei personaggi ora si ricavano dalle scene. `nuovo_canto.py` è sostituito da `prepara.py`; `riempi.py` inserisce scene e parafrasi da una bozza.
- **Testo**: da it.wikisource. Dal canto VI la pagina usa marcature nuove (`{{r|N}}` minuscolo, `{{Ac|…}}`, una citazione `{{§§/Tc|…}}` al verso VI 52): `wikisource.py` le gestisce; per i canti I–V il testo estratto non cambia. Wikisource e Commons rispondono 429 alle richieste ravvicinate: `wikisource.py` e `commons.py` ora aspettano e riprovano.
- **Tavole**: su Commons la serie «Plate N» si ferma al canto V. Come riferimento è servita l'edizione tedesca del 1870 digitalizzata da Polona (74 tavole in ordine, `Gottliche Komodie…`), guardata tavola per tavola; per ogni incisione è stata scelta la scansione migliore senza testo (dettagli in `data/scene-senza-tavola.md`). Per 25 incisioni Commons aveva solo la scansione di Project Gutenberg, con la didascalia inglese stampata in basso: sono state sostituite da **ritagli dell'edizione 1870** salvati in `img/dore/` (960 e 500 px), più una tavola nuova per il finale del canto XXXIV (Dante e Virgilio sotto le stelle).
- **Maometto (canto XXVIII)**: la tavola esiste ma non è stata messa, per la sensibilità della raffigurazione. Da decidere.
- **Scene**: luoghi presi dai 37 di `luoghi.json` (nessun luogo nuovo); le scene dove Dante è sul confine tra due zone usano quella che si vede.
- **Parafrasi**: tutte le 1357 terzine nuove, una per terzina, come nei canti I–V; tra parentesi quadre solo i nomi taciuti dal testo.
- **Personaggi**: dichiarati nel canto in cui compaiono la prima volta; `dibattuto` per identificazioni o interpretazioni discusse (per esempio Ciacco, Guido Cavalcanti, il messo celeste, Ugolino).
- **Versione nei file**: portata a `2026-11-15.7`.

### Da decidere o da fare

1. ~~Didascalie inglesi su 25 tavole~~ — fatto: ritagli dall'edizione 1870 in `img/dore/`. `compila.py`, `verifica.py` e `app.js` gestiscono le tavole nel repository (`tavola: dore | titolo | img/dore/…-960.jpg | File:…`). Le altre 47 tavole di Doré si caricano ancora da Commons: copiarle tutte è possibile con lo stesso procedimento.
2. **Tavola di Maometto**: metterla o no.
3. **Controllo nel browser** di tutti i 34 canti (scorrimento, tavole, mappa, telefono), come in Fase 3: non ancora fatto per i canti VI–XXXIV.

## Immagini generate con IA (15/11/2026)

- Il proprietario crea con Gemini, nello stile di Doré, le immagini delle scene che Doré non ha illustrato (prompt in `data/illustrazioni-ia.md`). File in `img/ia/<cantica>-<NN>-scena-<K>.jpg`, tavole con `autore_id: "ia"`.
- Prima immagine: `inferno/1/scena-2` (il colle illuminato dal sole), 765×1024, 333 KB. Scene senza tavola: da 9 a 8.
- Nel sito sono sempre dichiarate: etichetta «Immagine IA» sulla tavola, didascalia «Immagine generata con IA (Gemini) … nello stile di Doré, non è un'opera di Doré» con link al prompt, e su computer l'avviso «Doré non ha illustrato questa scena».
- Il selettore degli illustratori resta nascosto: compare solo se una scena ha tavole di due autori diversi.
- `verifica.py`: le immagini IA devono esistere in `img/ia/` e dichiarare «IA» nell'autore; non si cercano su Commons.

## Pubblicazione (15/11/2026)

- Repository: https://github.com/smnbrn/CommediaIllustrata (pubblico), ramo `main`.
- Sito: https://smnbrn.github.io/CommediaIllustrata/ — GitHub Pages da `main`, cartella principale.
- Controllo sul sito pubblicato (1280×800): tutte le 28 scene aperte scorrendo, tavola giusta o riquadro vuoto, nessun errore nella console.

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
5. ~~Scrittura dei dati~~ — fatto (sorgenti in `sorgenti/` e `compila.py`, Fase 4). In origine: scene e parafrasi oggi passano da uno script di lavoro fuori dal repository; per i canti successivi conviene un formato sorgente nel repository (o scriverle direttamente nei JSON) e lasciare a `verifica.py` i controlli.

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

- Cartella del progetto: `C:\Users\simon\Desktop\Repos\CommediaIllustrata`.- Schema: aggiunti alle tavole `autore_id` e `miniatura` (960 px, per il telefono); al canto `fonte_testo` e `numero_versi`.
- Le immagini si caricano direttamente da `upload.wikimedia.org` (nessuna copia nel repository).
