# Scene senza tavola

Il sito usa solo le incisioni di **Gustave Doré** (scelta del 15/11/2026). Doré ha inciso 18 tavole per i canti I–V: sono tutte nel dataset (20 abbinamenti, perché due tavole servono due scene consecutive). Le scene qui sotto non hanno una sua incisione pertinente e restano con `tavole: []`; `python -I strumenti/verifica.py` le segnala come avvisi.

| Scena | Versi | Titolo | Nota |
|---|---|---|---|
| `inferno/1/scena-2` | 13–30 | Il colle illuminato dal sole | La tavola 1 mostra Dante nella selva, la 2 già la lonza: per il colle e la similitudine del naufrago non c'è un'incisione. |
| `inferno/1/scena-6` | 91–111 | La profezia del veltro | Il discorso di Virgilio sul veltro non è illustrato. |
| `inferno/2/scena-2` | 10–42 | Il dubbio di Dante | Doré illustra l'inizio del canto (tavola 6) e Beatrice (tavola 7): per il dubbio di Dante non c'è un'incisione. |
| `inferno/2/scena-5` | 115–142 | Dante ritrova il coraggio | Nessuna incisione per la fine del canto (i fioretti, la ripresa del cammino). |
| `inferno/3/scena-2` | 22–69 | Gli ignavi | Doré passa dalla porta (tavola 8) a Caronte (tavola 9): gli ignavi non sono illustrati. |
| `inferno/3/scena-5` | 130–136 | Il terremoto e lo svenimento | Il terremoto e lo svenimento non sono illustrati. |
| `inferno/4/scena-1` | 1–24 | Il risveglio sull'orlo dell'abisso | Il risveglio sull'orlo dell'abisso non è illustrato. |
| `inferno/4/scena-4` | 106–129 | Il nobile castello e gli spiriti magni | Doré non ha inciso il nobile castello. La tavola 12 (i poeti) mostra il gruppo che vi entra con Dante: si potrebbe riusare qui, ma raffigura l'incontro, non il castello. |
| `inferno/4/scena-5` | 130–151 | I filosofi; la compagnia si divide | I filosofi non sono illustrati. |

Totale: 9 scene su 28.

**Decisione del proprietario (15/11/2026): restano vuote.** Il sito mostra il riquadro «Nessuna tavola di Doré per questa scena»; non si riusa la tavola della scena vicina.

## Tavole usate per due scene

- Tavola 7 (Beatrice e Virgilio): `inferno/2/scena-3` e `inferno/2/scena-4`, perché il dialogo tra i due continua.
- Tavola 14 (la bufera): `inferno/5/scena-2` e `inferno/5/scena-3`, perché le anime nominate da Virgilio sono quelle trascinate dalla bufera.

## Definizione delle tavole

- Tavola 16 (Paolo e Francesca davanti a Dante e Virgilio): 979×801 px, sufficiente (il sito mostra al massimo 960 px).
- Tavola 17 (la lettura e il bacio): il file della serie «Plate N» era 715×1083 px; sostituito con `Gustave Doré - The Inferno, Canto 5-2.jpg` (1808×2261), stessa incisione.

## Altri illustratori (non usati)

Durante la ricerca sono state verificate su Commons, tutte in pubblico dominio, anche tavole di Blake, Flaxman (incisioni di Piroli, esemplare Gallica), Botticelli (solo canto I) e Stradano (scansioni `Stradano Inferno Canto 0N.jpg`). Le foto di Sailko dei disegni di Stradano sono invece CC BY 3.0. Con più illustratori diverse scene scoperte avrebbero una tavola (per esempio gli ignavi con Blake, il nobile castello con Stradano): lo strumento `strumenti/commons.py` e il selettore nel sito restano pronti (basta aggiungere l'illustratore in `data/indice.json`).
