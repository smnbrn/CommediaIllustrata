# /esplora — modalità 3D

Build WebGL del progetto Unity in `../esplora-unity/` (Unity 6000.6.5f1). Si apre da `esplora/index.html`.

La versione 3D legge gli stessi file di `/data` (vedi `data/SCHEMA.md`), senza dati nel codice:

- `../data/luoghi.json` per costruire la voragine (profondità, raggio, angolo);
- `../data/inferno/canto-NN.json` per sapere quali scene accadono in quale luogo e quale tavola le illustra;
- per aprire la lettura di una scena: `../index.html#inferno/1/scena-3`.

## Rifare la build

Aprire `esplora-unity/` con Unity 6000.6.5f1 e usare il menu **Esplora > Costruisci WebGL**. Scrive in questa cartella (`index.html` da `Assets/WebGLTemplates/Esplora/`, file compressi in `Build/`). Compressione gzip con «Decompression Fallback», perché GitHub Pages non manda le intestazioni di compressione.

I file in `Build/` sono binari (circa 6 MB): ogni nuova build li aggiunge alla cronologia di git.
