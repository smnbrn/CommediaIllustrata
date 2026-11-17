/* Mini-mappe in sezione: l'imbuto dell'Inferno (data/luoghi.json) e la montagna del Purgatorio (data/luoghi-purgatorio.json).
   profondita -> asse verticale, raggio -> ampiezza della voragine,
   angolo -> posizione sull'anello (ellisse in prospettiva). */
window.CI = window.CI || {};

CI.mappa = (function () {
  var NS = 'http://www.w3.org/2000/svg';
  var L = 130, A = 150;           // viewBox
  var CX = 65, Y0 = 18, Y1 = 142, W = 50, SCHIACCIA = 0.09;

  function el(nome, attr, padre) {
    var e = document.createElementNS(NS, nome);
    Object.keys(attr || {}).forEach(function (k) { e.setAttribute(k, attr[k]); });
    if (padre) padre.appendChild(e);
    return e;
  }
  function y(p) { return Y0 + p * (Y1 - Y0); }
  function semi(r) { return r * W; }

  function punto(luogo) {
    var c = luogo.coordinate, a = c.angolo * Math.PI / 180, hw = semi(c.raggio);
    if (c.profondita === 0) return { x: CX + Math.cos(a) * hw, y: Y0 - 4 };
    return { x: CX + Math.cos(a) * hw, y: y(c.profondita) - Math.sin(a) * hw * SCHIACCIA };
  }

  function breve(nome) {
    var i = nome.indexOf('– ');
    return i >= 0 ? nome.slice(i + 2) : nome;
  }

  function crea(svg, dati) {
    var luoghi = dati.luoghi.slice().sort(function (a, b) { return a.ordine - b.ordine; });
    var perId = {};
    luoghi.forEach(function (l) { perId[l.id] = l; });
    svg.setAttribute('viewBox', '0 0 ' + L + ' ' + A);

    // superficie: linea del suolo, selva a sinistra, colle a destra
    el('path', { d: 'M2 ' + Y0 + ' H' + (L - 2), class: 'm-terra' }, svg);
    el('path', { d: 'M100 ' + Y0 + ' Q112 2 128 ' + Y0 + ' Z', class: 'm-colle' }, svg);
    [6, 12, 18, 24].forEach(function (x, i) {
      var h = 6 + (i % 2) * 2;
      el('path', { d: 'M' + (x - 3) + ' ' + Y0 + ' L' + x + ' ' + (Y0 - h) + ' L' + (x + 3) + ' ' + Y0 + ' Z', class: 'm-colle' }, svg);
    });

    // voragine a ripiani: dai livelli principali (non le zone interne)
    var livelli = luoghi.filter(function (l) {
      return !l.zona_di && l.coordinate.profondita > 0 && l.coordinate.raggio <= 1;
    });
    var sinistra = [[CX - W, Y0]], prima = W;
    livelli.forEach(function (l) {
      var yy = y(l.coordinate.profondita), hw = semi(l.coordinate.raggio);
      sinistra.push([CX - prima, yy], [CX - hw, yy]);
      prima = hw;
    });
    sinistra.push([CX, y(1)]);
    var destra = sinistra.slice(0, -1).reverse().map(function (p) { return [2 * CX - p[0], p[1]]; });
    el('path', {
      d: 'M' + sinistra.concat(destra).map(function (p) { return p[0].toFixed(1) + ' ' + p[1].toFixed(1); }).join(' L') + ' Z',
      class: 'm-voragine',
    }, svg);

    // anelli dei cerchi, numeri romani, etichette (visibili a mappa aperta)
    var anelli = {};
    luoghi.filter(function (l) { return l.tipo === 'cerchio'; }).forEach(function (l) {
      var c = l.coordinate, yy = y(c.profondita), hw = semi(c.raggio);
      anelli[l.id] = el('ellipse', { cx: CX, cy: yy.toFixed(1), rx: hw.toFixed(1), ry: (hw * SCHIACCIA).toFixed(2), class: 'm-livello' }, svg);
      var n = el('text', { x: (CX + hw + 2).toFixed(1), y: (yy + 2).toFixed(1), class: 'm-numero' }, svg);
      n.textContent = CI.romano(l.cerchio);
      var t = el('text', { x: CX, y: (yy + 2.2).toFixed(1), 'text-anchor': 'middle', class: 'etichetta' }, svg);
      t.textContent = breve(l.nome);
    });

    var percorso = el('polyline', { class: 'm-percorso', points: '' }, svg);
    var alone = el('circle', { r: 3.2, class: 'm-alone', cx: -10, cy: -10 }, svg);
    var dante = el('circle', { r: 2.6, class: 'm-dante', cx: -10, cy: -10 }, svg);

    function segna(id) {
      var l = perId[id];
      if (!l) return null;
      var p = punto(l);
      [dante, alone].forEach(function (c) { c.setAttribute('cx', p.x.toFixed(1)); c.setAttribute('cy', p.y.toFixed(1)); });
      percorso.setAttribute('points', luoghi.filter(function (x) { return x.ordine <= l.ordine; })
        .map(function (x) { var q = punto(x); return q.x.toFixed(1) + ',' + q.y.toFixed(1); }).join(' '));
      Object.keys(anelli).forEach(function (k) {
        anelli[k].classList.toggle('visitato', perId[k].ordine <= l.ordine);
      });
      return l;
    }

    return { segna: segna, luogo: function (id) { return perId[id]; } };
  }

  /* ---------- montagna del Purgatorio ---------- */
  var YM0 = 26, YM1 = 138;
  var BREVI = {
    'spiaggia': 'spiaggia', 'antipurgatorio-scomunicati': 'scomunicati', 'antipurgatorio-pigri': 'pigri',
    'antipurgatorio-morti-violenti': 'morti violenti', 'valletta-principi': 'principi', 'porta-purgatorio': 'porta',
    'cornice-1': 'superbi', 'cornice-2': 'invidiosi', 'cornice-3': 'iracondi', 'cornice-4': 'accidiosi',
    'cornice-5': 'avari', 'cornice-6': 'golosi', 'cornice-7': 'lussuriosi', 'paradiso-terrestre': 'Eden'
  };

  function creaMontagna(svg, dati) {
    var luoghi = dati.luoghi.slice().sort(function (a, b) { return a.ordine - b.ordine; });
    var perId = {};
    luoghi.forEach(function (l) { perId[l.id] = l; });
    svg.setAttribute('viewBox', '0 0 ' + L + ' ' + A);
    function ym(h) { return YM1 - h * (YM1 - YM0); }
    function pm(l) {
      var c = l.coordinate, a = c.angolo * Math.PI / 180, hw = semi(c.raggio);
      return { x: CX + Math.cos(a) * hw, y: ym(c.altezza) - Math.sin(a) * hw * SCHIACCIA };
    }

    // mare in basso
    el('path', { d: 'M2 ' + (YM1 + 5) + ' q6 -3 12 0 t12 0 t12 0 t12 0 t12 0 t12 0 t12 0 t12 0 t12 0 t12 0', class: 'm-terra' }, svg);

    // sagoma della montagna: un cono che passa per il bordo di ogni livello
    var sinistra = luoghi.map(function (l) { return [CX - semi(l.coordinate.raggio), ym(l.coordinate.altezza)]; });
    var destra = sinistra.slice().reverse().map(function (p) { return [2 * CX - p[0], p[1]]; });
    el('path', {
      d: 'M' + sinistra.concat(destra).map(function (p) { return p[0].toFixed(1) + ' ' + p[1].toFixed(1); }).join(' L') + ' Z',
      class: 'm-voragine',
    }, svg);

    // cima: il giardino del Paradiso terrestre
    var cima = luoghi[luoghi.length - 1], hc = semi(cima.coordinate.raggio);
    [-0.7, -0.25, 0.25, 0.7].forEach(function (k, i) {
      var x = CX + k * hc, h = 7 + (i % 2) * 2;
      el('path', { d: 'M' + (x - 3) + ' ' + (YM0 - 1) + ' L' + x + ' ' + (YM0 - 1 - h) + ' L' + (x + 3) + ' ' + (YM0 - 1) + ' Z', class: 'm-colle' }, svg);
    });

    // gradoni: un anello per ogni livello; numeri romani per le cornici; etichette (visibili a mappa aperta)
    var anelli = {};
    luoghi.forEach(function (l) {
      var c = l.coordinate, yy = ym(c.altezza), hw = semi(c.raggio);
      if (l.id === 'spiaggia') return;
      anelli[l.id] = el('ellipse', { cx: CX, cy: yy.toFixed(1), rx: hw.toFixed(1), ry: (hw * SCHIACCIA).toFixed(2), class: 'm-livello' }, svg);
      if (l.tipo === 'cornice') {
        var n = el('text', { x: (CX + hw + 4.5).toFixed(1), y: (yy + 2).toFixed(1), class: 'm-numero' }, svg);
        n.textContent = CI.romano(l.cerchio);
      }
      var t = el('text', { x: CX, y: (yy + 2.2).toFixed(1), 'text-anchor': 'middle', class: 'etichetta etichetta-monte' }, svg);
      t.textContent = BREVI[l.id] || l.nome;
    });

    var percorso = el('polyline', { class: 'm-percorso', points: '' }, svg);
    var alone = el('circle', { r: 3.2, class: 'm-alone', cx: -10, cy: -10 }, svg);
    var dante = el('circle', { r: 2.6, class: 'm-dante', cx: -10, cy: -10 }, svg);

    function segna(id) {
      var l = perId[id];
      if (!l) return null;
      var p = pm(l);
      [dante, alone].forEach(function (c) { c.setAttribute('cx', p.x.toFixed(1)); c.setAttribute('cy', p.y.toFixed(1)); });
      percorso.setAttribute('points', luoghi.filter(function (x) { return x.ordine <= l.ordine; })
        .map(function (x) { var q = pm(x); return q.x.toFixed(1) + ',' + q.y.toFixed(1); }).join(' '));
      Object.keys(anelli).forEach(function (k) {
        anelli[k].classList.toggle('visitato', perId[k].ordine <= l.ordine);
      });
      return l;
    }

    return { segna: segna, luogo: function (id) { return perId[id]; } };
  }

  return { crea: crea, creaMontagna: creaMontagna };
})();
