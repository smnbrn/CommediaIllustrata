/* Caricamento dei dati JSON (gli stessi che leggerà la versione 3D in /esplora). */
window.CI = window.CI || {};

CI.dati = (function () {
  var cache = new Map();

  function json(percorso) {
    if (!cache.has(percorso)) {
      cache.set(percorso, fetch(percorso + '?v=' + encodeURIComponent(CI.versione || '0')).then(function (r) {
        if (!r.ok) throw new Error(percorso + ': ' + r.status);
        return r.json();
      }));
    }
    return cache.get(percorso);
  }

  function due(n) { return String(n).padStart(2, '0'); }

  return {
    indice: function () { return json('data/indice.json'); },
    luoghi: function () { return json('data/luoghi.json'); },
    luoghiPurgatorio: function () { return json('data/luoghi-purgatorio.json'); },
    personaggi: function () { return json('data/personaggi.json'); },
    canto: function (cantica, n) { return json('data/' + cantica + '/canto-' + due(n) + '.json'); },
  };
})();

CI.romano = function (n) {
  var v = [[10, 'X'], [9, 'IX'], [5, 'V'], [4, 'IV'], [1, 'I']], s = '';
  v.forEach(function (c) { while (n >= c[0]) { s += c[1]; n -= c[0]; } });
  return s;
};
