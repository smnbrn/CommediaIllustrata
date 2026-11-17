/* Commedia Illustrata — lettore. Indirizzi: #inferno/1 e #inferno/1/scena-3 */
(function () {
  'use strict';

  var $ = function (id) { return document.getElementById(id); };
  var stato = {
    indice: null, mappa: null, personaggi: {},
    cantica: null, n: null, canto: null,
    scena: null, illustratore: leggi('ci-illustratore') || 'dore',
    pagine: {},           // scena -> indice della tavola mostrata (se un autore ne ha più d'una)
    imgAttiva: 'img-a', richiestaImg: 0,
  };

  function leggi(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
  function scrivi(k, v) { try { localStorage.setItem(k, v); } catch (e) { /* niente memoria: va bene lo stesso */ } }
  function esc(s) { return String(s).replace(/[&<>"]/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]; }); }
  function mobile() { return window.matchMedia('(max-width: 899px)').matches; }
  var PERSONAGGI_VISIBILI = 6;   // oltre questo numero: «e altri N»
  var IA = 'ia';                 // autore_id delle immagini generate con IA (sempre segnalate come tali)

  /* ---------- indirizzi ---------- */
  function leggiIndirizzo() {
    var p = location.hash.replace(/^#\/?/, '').split('/');
    return { cantica: p[0] || 'inferno', n: parseInt(p[1], 10) || 1, scena: p[2] || null };
  }
  function indirizzo(scena) {
    return '#' + stato.cantica + '/' + stato.n + (scena ? '/' + scena : '');
  }

  /* ---------- avvio ---------- */
  Promise.all([CI.dati.indice(), CI.dati.luoghi(), CI.dati.personaggi(), CI.dati.luoghiPurgatorio()]).then(function (r) {
    stato.indice = r[0];
    if (!r[0].illustratori.some(function (i) { return i.id === stato.illustratore; })) stato.illustratore = r[0].illustratori[0].id;
    stato.mappe = { inferno: CI.mappa.crea($('mappa-svg'), r[1]), purgatorio: CI.mappa.creaMontagna($('mappa-svg-pur'), r[3]) };
    r[2].personaggi.forEach(function (p) { stato.personaggi[p.id] = p; });
    riempiSceltaCanto();
    preparaTema();
    preparaMappa();
    preparaLente();
    window.addEventListener('hashchange', vai);
    window.addEventListener('scroll', programmaControllo, { passive: true });
    window.addEventListener('resize', programmaControllo);
    vai();
  }).catch(errore);

  function errore(e) {
    console.error(e);
    $('testo').innerHTML = '<p class="errore">Non riesco a caricare i dati (' + esc(e.message) + ').<br>Se hai aperto il file direttamente, avvia un piccolo server locale.</p>';
  }

  function cantica(id) { return stato.indice.cantiche.find(function (c) { return c.id === id; }); }
  function voceCanto(cid, n) { var c = cantica(cid); return c && c.canti.find(function (x) { return x.canto === n; }); }

  /* selettori: la cantica (Inferno / Purgatorio) e, accanto, i canti di quella cantica */
  function riempiSceltaCanto() {
    var sc = $('scelta-cantica'), sel = $('scelta-canto');
    stato.indice.cantiche.forEach(function (c) {
      var o = document.createElement('option');
      o.value = c.id; o.textContent = c.nome;
      o.disabled = !c.canti.some(function (k) { return k.disponibile; });
      sc.appendChild(o);
    });
    sc.addEventListener('change', function () { location.hash = '#' + sc.value + '/1'; });
    sel.addEventListener('change', function () { location.hash = '#' + sc.value + '/' + sel.value; });
  }

  function riempiCanti(idCantica) {
    var sel = $('scelta-canto');
    if (sel.dataset.cantica === idCantica) return;
    sel.dataset.cantica = idCantica;
    sel.innerHTML = '';
    cantica(idCantica).canti.forEach(function (k) {
      var o = document.createElement('option');
      o.value = k.canto;
      o.textContent = 'Canto ' + CI.romano(k.canto) + (k.disponibile ? '' : ' (in preparazione)');
      o.disabled = !k.disponibile;
      sel.appendChild(o);
    });
  }

  /* ---------- navigazione ---------- */
  function vai() {
    var d = leggiIndirizzo();
    var voce = voceCanto(d.cantica, d.n);
    if (!voce || !voce.disponibile) {
      $('testo').innerHTML = '<p class="errore">Questo canto non è ancora disponibile.<br><a href="#inferno/1">Torna all\'Inferno, canto I</a></p>';
      return;
    }
    if (d.cantica === stato.cantica && d.n === stato.n && stato.canto) {
      if (d.scena && document.getElementById(d.scena)) vaiAScena(d.scena, true);
      else if (d.scena) history.replaceState(null, '', indirizzo(stato.scena));
      return;
    }
    CI.dati.canto(d.cantica, d.n).then(function (canto) {
      stato.cantica = d.cantica; stato.n = d.n; stato.canto = canto; stato.scena = null;
      coloriCantica(d.cantica);
      $('scelta-cantica').value = d.cantica;
      riempiCanti(d.cantica);
      $('scelta-canto').value = d.n;
      document.title = cantica(d.cantica).nome + ' ' + CI.romano(d.n) + ' · Commedia Illustrata';
      disegnaCanto(canto);
      var esiste = d.scena && canto.scene.some(function (s) { return s.id === d.scena; });
      var prima = esiste ? d.scena : canto.scene[0].id;
      if (!esiste) {
        // nessuna scena o scena inesistente: inizio del canto, con l'indirizzo pulito
        if (d.scena) history.replaceState(null, '', indirizzo(null));
        window.scrollTo(0, 0); attiva(prima, false); return;
      }
      vaiAScena(prima, true);
      // il carattere da libro arriva dopo e cambia l'altezza del testo: si riallinea alla scena
      if (document.fonts && document.fonts.load) {
        var attesa = new Promise(function (ok) { setTimeout(ok, 2000); });
        Promise.race([document.fonts.load('400 1em "EB Garamond"'), attesa])
          .then(function () { return document.fonts.ready; })
          .then(function () { vaiAScena(prima, true); }, function () {});
      }
    }).catch(errore);
  }

  function vaiAScena(id, salta) {
    var s = document.getElementById(id);
    if (!s) return;
    stato.bloccoFino = Date.now() + 900;      // il controllo dello scorrimento aspetta l'arrivo
    // posizione calcolata a mano: sotto la tavola (telefono) o sotto la testata (computer)
    var sopra = mobile() ? document.querySelector('.pannello').getBoundingClientRect().height
      : document.querySelector('.testata').getBoundingClientRect().height;
    window.scrollTo({ top: s.getBoundingClientRect().top + window.scrollY - sopra - 12, behavior: salta ? 'auto' : 'smooth' });
    attiva(id, false);
  }

  /* ---------- testo ---------- */
  function disegnaCanto(canto) {
    var nomeCantica = cantica(canto.cantica).nome;
    var h = '<div class="testo-canto">' +
      '<header class="intestazione-canto"><p class="cantica">' + esc(nomeCantica) + '</p>' +
      '<h1>Canto ' + CI.romano(canto.canto) + '</h1>' +
      (canto.titolo ? '<p class="titolo">' + esc(canto.titolo) + '</p>' : '') + '</header>';

    canto.scene.forEach(function (s, i) {
      var luogo = (stato.mappe[canto.cantica] || stato.mappe.inferno).luogo(s.luogo_id);
      h += '<section class="scena" id="' + esc(s.id) + '" data-scena="' + esc(s.id) + '">' +
        '<header class="scena-testa">' +
        '<p class="scena-numero"><a href="' + indirizzo(s.id) + '">Scena ' + (i + 1) + ' · vv. ' + s.versi[0] + '–' + s.versi[1] + '</a></p>' +
        '<h2 class="scena-titolo">' + esc(s.titolo_scena) + '</h2>' +
        '<p class="scena-sintesi">' + esc(s.sintesi) + '</p>' +
        '<div class="scena-info">' + (luogo ? '<span class="luogo">' + esc(luogo.nome) + '</span>' : '') +
        s.personaggi.map(function (pid, k) {
          var p = stato.personaggi[pid];
          if (!p) return '';
          return '<button type="button" class="chip' + (p.dibattuto ? ' dibattuto' : '') + '" data-personaggio="' + esc(pid) + '" aria-expanded="false"' +
            (k >= PERSONAGGI_VISIBILI ? ' hidden' : '') + '>' + esc(p.nome) + '</button>';
        }).join('') +
        (s.personaggi.length > PERSONAGGI_VISIBILI
          ? '<button type="button" class="chip-altri">e altri ' + (s.personaggi.length - PERSONAGGI_VISIBILI) + '</button>' : '') +
        '</div><div class="scheda-posto"></div></header>';

      canto.terzine.forEach(function (t, j) {
        if (t.versi[0] < s.versi[0] || t.versi[0] > s.versi[1]) return;
        var righe = t.testo.split('\n');
        var pid = 'parafrasi-' + t.versi[0];
        h += '<button type="button" class="terzina" aria-expanded="false" aria-controls="' + pid + '" data-v="' + t.versi[0] + '">' +
          righe.map(function (r, k) {
            var num = t.versi[0] + k;
            return '<span class="verso' + (j === 0 && k === 0 ? ' capolettera' : '') + '">' + esc(r) +
              (num % 3 === 0 || num === canto.numero_versi ? '<span class="numero-verso" aria-hidden="true">' + num + '</span>' : '') + '</span>';
          }).join('') + '</button>' +
          '<div class="parafrasi' + (t.parafrasi ? '' : ' mancante') + '" id="' + pid + '" hidden>' +
          '<span class="etichetta-parafrasi">Parafrasi · vv. ' + t.versi[0] + (t.versi[1] > t.versi[0] ? '–' + t.versi[1] : '') + '</span>' +
          esc(t.parafrasi || 'Parafrasi non ancora disponibile.') + '</div>';
      });
      h += '</section>';
    });

    var c = cantica(canto.cantica), i = c.canti.findIndex(function (k) { return k.canto === canto.canto; });
    function link(k, testo) {
      if (!k) return '';
      return k.disponibile ? '<a href="#' + c.id + '/' + k.canto + '">' + testo + '</a>' : '<span class="disattivo">' + testo + ' (in preparazione)</span>';
    }
    h += '<footer class="fine-canto"><p>Fine del canto ' + CI.romano(canto.canto) + '</p><nav>' +
      link(c.canti[i - 1], '← Canto ' + (i > 0 ? CI.romano(c.canti[i - 1].canto) : '')) +
      link(c.canti[i + 1], 'Canto ' + (c.canti[i + 1] ? CI.romano(c.canti[i + 1].canto) : '') + ' →') +
      '</nav><p class="fonti">Testo: <a href="' + esc(canto.fonte_testo) + '" rel="noopener">Wikisource</a> (ed. Petrocchi). Tavole: Gustave Doré, da Wikimedia Commons, pubblico dominio.</p></footer></div>';

    $('testo').innerHTML = h;
  }

  // terzine e personaggi: un solo ascoltatore per tutto il testo
  $('testo').addEventListener('click', function (e) {
    var t = e.target.closest('.terzina');
    if (t) {
      var aperta = t.getAttribute('aria-expanded') === 'true';
      t.setAttribute('aria-expanded', String(!aperta));
      $(t.getAttribute('aria-controls')).hidden = aperta;
      return;
    }
    var altri = e.target.closest('.chip-altri');
    if (altri) {
      altri.parentNode.querySelectorAll('.chip[hidden]').forEach(function (x) { x.hidden = false; });
      altri.remove();
      return;
    }
    var c = e.target.closest('.chip');
    if (c) {
      var testa = c.closest('.scena-testa'), posto = testa.querySelector('.scheda-posto');
      var gia = c.getAttribute('aria-expanded') === 'true';
      testa.querySelectorAll('.chip').forEach(function (x) { x.setAttribute('aria-expanded', 'false'); });
      posto.innerHTML = '';
      if (!gia) {
        var p = stato.personaggi[c.dataset.personaggio];
        c.setAttribute('aria-expanded', 'true');
        posto.innerHTML = '<p class="scheda-personaggio"><b>' + esc(p.nome) + '.</b> ' + esc(p.chi_era) +
          (p.dibattuto ? '<span class="nota">* Identificazione o significato discussi dagli studiosi.</span>' : '') + '</p>';
      }
    }
  });

  /* ---------- scena attiva durante la lettura ---------- */
  var programmato = false;
  function programmaControllo() {
    if (programmato) return;
    programmato = true;
    requestAnimationFrame(function () { programmato = false; controlla(); });
  }
  function lineaDiLettura() {
    if (mobile()) {
      var b = document.querySelector('.pannello').getBoundingClientRect().bottom;
      return b + (window.innerHeight - b) * 0.3;
    }
    return window.innerHeight * 0.38;
  }
  function controlla() {
    if (!stato.canto || (stato.bloccoFino && Date.now() < stato.bloccoFino)) return;
    // un altro canto sta arrivando (indirizzo già cambiato): non riscrivere l'indirizzo con quello vecchio
    var d = leggiIndirizzo();
    if (d.cantica !== stato.cantica || d.n !== stato.n) return;
    var linea = lineaDiLettura(), scelta = null;
    document.querySelectorAll('.scena').forEach(function (s) {
      if (s.getBoundingClientRect().top <= linea) scelta = s.dataset.scena;
    });
    attiva(scelta || stato.canto.scene[0].id, true);
  }

  /* colori della cantica (CSS: [data-cantica]) e barra del browser; mini-mappa giusta (imbuto o montagna) */
  var COLORI_BARRA = { inferno: ['#f3ead6', '#14110d'], purgatorio: ['#f2eef9', '#15111e'] };
  function nascondi(e, si) { if (si) e.setAttribute('hidden', ''); else e.removeAttribute('hidden'); }   // gli svg non hanno la proprietà hidden
  function coloriCantica(id) {
    var el = document.documentElement;
    if (id === 'inferno') delete el.dataset.cantica; else el.dataset.cantica = id;
    var c = COLORI_BARRA[id] || COLORI_BARRA.inferno;
    document.querySelectorAll('meta[name="theme-color"]').forEach(function (m) {
      m.setAttribute('content', m.media && m.media.indexOf('dark') >= 0 ? c[1] : c[0]);
    });
    nascondi($('mappa-svg'), id !== 'inferno');
    nascondi($('mappa-svg-pur'), id !== 'purgatorio');
    $('mappa-3d').hidden = id !== 'inferno';   // il 3D c'è solo per l'Inferno
  }

  function scenaDati(id) { return stato.canto.scene.find(function (s) { return s.id === id; }); }

  function attiva(id, aggiornaIndirizzo) {
    if (id === stato.scena) return;
    stato.scena = id;
    document.querySelectorAll('.scena').forEach(function (s) { s.classList.toggle('attiva', s.dataset.scena === id); });
    var s = scenaDati(id);
    var k = stato.canto.scene.indexOf(s);
    $('posizione').innerHTML = '<span>Scena ' + (k + 1) + ' di ' + stato.canto.scene.length + '</span> ' + esc(s.titolo_scena);
    mostraTavola(s);
    var l = (stato.mappe[stato.cantica] || stato.mappe.inferno).segna(s.luogo_id);
    $('mappa-luogo').textContent = l ? l.nome : '';
    $('mappa').setAttribute('aria-label', 'Mappa ' + (stato.cantica === 'purgatorio' ? 'del Purgatorio' : 'dell\'Inferno') + ', Dante si trova qui: ' + (l ? l.nome : '') + '. Ingrandisci');
    if (aggiornaIndirizzo) history.replaceState(null, '', indirizzo(id));
    precarica(id);
  }

  /* ---------- tavole e selettore ---------- */
  function ordineAutori() { return stato.indice.illustratori.map(function (i) { return i.id; }); }
  function nomeAutore(id) { var a = stato.indice.illustratori.find(function (i) { return i.id === id; }); return a ? a.breve : id; }

  function sceltaPer(s) {
    var tav = s.tavole || [];
    if (!tav.length) return { lista: [], autore: null };
    var autore = tav.some(function (t) { return t.autore_id === stato.illustratore; })
      ? stato.illustratore
      : ordineAutori().find(function (a) { return tav.some(function (t) { return t.autore_id === a; }); }) || tav[0].autore_id;
    return { lista: tav.filter(function (t) { return t.autore_id === autore; }), autore: autore };
  }

  function disegnaSelettore(s, mostrato) {
    var box = $('illustratori');
    // il selettore serve solo se almeno una scena del canto ha tavole di due autori diversi
    box.hidden = !stato.canto.scene.some(function (x) {
      var a = {}; (x.tavole || []).forEach(function (t) { a[t.autore_id] = true; });
      return Object.keys(a).length > 1;
    });
    if (box.hidden) return;
    var usati = {};
    stato.canto.scene.forEach(function (x) { (x.tavole || []).forEach(function (t) { usati[t.autore_id] = true; }); });
    var presenti = {};
    (s.tavole || []).forEach(function (t) { presenti[t.autore_id] = true; });
    box.innerHTML = stato.indice.illustratori.filter(function (a) { return !a.secondario || usati[a.id]; }).map(function (a) {
      var cl = 'illustratore' + (presenti[a.id] ? '' : ' assente') + (a.id === mostrato ? ' mostrato' : '');
      return '<button type="button" role="radio" class="' + cl + '" data-autore="' + a.id + '" aria-checked="' + (a.id === stato.illustratore) + '"' +
        ' title="' + esc(a.nome + ' — ' + a.opera + (presenti[a.id] ? '' : ' (nessuna tavola per questa scena)')) + '">' + esc(a.breve) + '</button>';
    }).join('');
  }

  $('illustratori').addEventListener('click', function (e) {
    var b = e.target.closest('.illustratore');
    if (!b) return;
    stato.illustratore = b.dataset.autore;
    scrivi('ci-illustratore', stato.illustratore);
    mostraTavola(scenaDati(stato.scena));
  });
  $('illustratori').addEventListener('keydown', function (e) {
    if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
    var tutti = Array.prototype.slice.call(this.querySelectorAll('.illustratore'));
    var i = tutti.indexOf(document.activeElement);
    if (i < 0) return;
    var n = tutti[(i + (e.key === 'ArrowRight' ? 1 : tutti.length - 1)) % tutti.length];
    n.click(); this.querySelector('[data-autore="' + n.dataset.autore + '"]').focus();
    e.preventDefault();
  });

  $('tavola-pagina').addEventListener('click', function () {
    var s = scenaDati(stato.scena), c = sceltaPer(s), k = (stato.pagine[s.id] || 0) + 1;
    stato.pagine[s.id] = k % c.lista.length;
    mostraTavola(s);
  });

  function mostraTavola(s) {
    var c = sceltaPer(s);
    disegnaSelettore(s, c.autore);
    var avviso = '';
    if (c.autore === IA) avviso = nomeAutore(stato.illustratore) + ' non ha illustrato questa scena: immagine generata con IA nello stile di Doré.';
    else if (c.autore && c.autore !== stato.illustratore) avviso = nomeAutore(stato.illustratore) + ' non ha illustrato questa scena: tavola di ' + nomeAutore(c.autore) + '.';
    $('avviso').textContent = avviso;
    $('tavola').dataset.ia = c.autore === IA ? '1' : '';

    var vuota = !c.lista.length;
    $('tavola-vuota').hidden = !vuota;
    $('tavola-vuota-titolo').textContent = vuota ? s.titolo_scena : '';
    var ill = stato.indice.illustratori.filter(function (a) { return !a.secondario; });
    $('tavola-vuota-testo').textContent = ill.length === 1 ? 'Nessuna tavola di ' + ill[0].breve + ' per questa scena.' : 'Nessuna tavola per questa scena.';
    var pag = $('tavola-pagina');
    if (vuota) {
      ['img-a', 'img-b'].forEach(function (i) { $(i).classList.remove('attiva'); });
      $('didascalia').innerHTML = '<span>Scena senza tavola</span>';
      pag.hidden = true;
      stato.richiestaImg++;
      stato.corrente = null;
      return;
    }
    var k = (stato.pagine[s.id] || 0) % c.lista.length, t = c.lista[k];
    stato.corrente = t;
    pag.hidden = c.lista.length < 2;
    pag.textContent = (k + 1) + ' / ' + c.lista.length;
    pag.setAttribute('aria-label', 'Tavola ' + (k + 1) + ' di ' + c.lista.length + ': mostra la successiva');
    $('didascalia').innerHTML = t.autore_id === IA
      ? '<b>Immagine generata con IA</b> (Gemini), <i>' + esc(t.titolo) + '</i> · nello stile di Doré, non è un\'opera di Doré' +
        ' · <a href="' + esc(t.fonte) + '" target="_blank" rel="noopener">come è fatta</a>'
      : '<b>' + esc(t.autore) + '</b>, <i>' + esc(t.titolo) + '</i> · ' + esc(t.licenza) +
        ' · <a href="' + esc(t.fonte) + '" target="_blank" rel="noopener">Commons</a>';
    carica(t);
  }

  function carica(t) {
    var numero = ++stato.richiestaImg;
    var prossima = stato.imgAttiva === 'img-a' ? 'img-b' : 'img-a';
    var img = $(prossima), attuale = $(stato.imgAttiva);
    if (attuale.dataset.src === (t.miniatura || t.url) && attuale.classList.contains('attiva')) return;
    img.onload = function () {
      if (numero !== stato.richiestaImg) return;
      img.classList.add('attiva');
      attuale.classList.remove('attiva');
      stato.imgAttiva = prossima;
    };
    img.onerror = function () {
      if (img.srcset) { img.removeAttribute('srcset'); img.src = t.miniatura || t.url; return; }  // prima la miniatura da 960 px
      if (img.src !== t.url && t.miniatura) { img.src = t.url; return; }   // poi il file originale
      if (numero === stato.richiestaImg) $('didascalia').innerHTML += ' · <span>immagine non raggiungibile</span>';
    };
    img.alt = t.autore + ', ' + t.titolo;
    img.dataset.src = t.miniatura || t.url;
    imposta(img, t);
  }

  // 500 px per gli schermi piccoli, 960 px per gli altri (Wikimedia accetta solo larghezze standard;
  // i ritagli in img/dore/ hanno le due versioni …-960.jpg e …-500.jpg)
  var DIMENSIONI = '(max-width: 899px) 60vw, 46vw';
  function imposta(img, t) {
    var grande = t.miniatura || t.url, piccola = null;
    if (grande.indexOf('/960px-') >= 0) piccola = grande.replace('/960px-', '/500px-');
    else if (/^img\/dore\/.*-960\.jpg$/.test(grande)) piccola = grande.replace(/-960\.jpg$/, '-500.jpg');  // ritagli nel repository
    else if (/^https:\/\/upload\.wikimedia\.org\/wikipedia\/commons\/[0-9a-f]\//.test(t.url)) {
      piccola = t.url.replace('/commons/', '/commons/thumb/') + '/500px-' + t.url.split('/').pop();
    }
    img.sizes = DIMENSIONI;
    if (piccola) img.srcset = piccola + ' 500w, ' + grande + ' 960w'; else img.removeAttribute('srcset');
    img.src = grande;
  }

  function precarica(id) {
    var i = stato.canto.scene.findIndex(function (s) { return s.id === id; });
    var dopo = stato.canto.scene[i + 1];
    if (!dopo) return;
    var c = sceltaPer(dopo);
    if (c.lista[0]) imposta(new Image(), c.lista[0]);
  }

  /* ---------- mappa ingrandibile ---------- */
  function preparaMappa() {
    var m = $('mappa');
    function chiudi() { m.classList.remove('aperta'); m.setAttribute('aria-expanded', 'false'); }
    m.setAttribute('aria-expanded', 'false');
    m.addEventListener('click', function (e) {
      e.stopPropagation();
      if (m.classList.contains('aperta') && e.target.closest && e.target.closest('#mappa-3d')) { location.href = 'esplora/'; return; }
      var aperta = m.classList.toggle('aperta');
      m.setAttribute('aria-expanded', String(aperta));
    });
    document.addEventListener('click', function () { if (m.classList.contains('aperta')) chiudi(); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') chiudi(); });
  }

  /* ---------- tavola ingrandita ---------- */
  function preparaLente() {
    var lente = $('lente'), scorri = $('lente-scorri'), img = $('lente-img'), cornice = $('tavola-cornice');
    var chiudiBtn = $('lente-chiudi'), prima = null, numero = 0;

    function adatta() { img.classList.remove('zoom'); img.style.width = ''; scorri.scrollLeft = 0; scorri.scrollTop = 0; }
    function apri() {
      var t = stato.corrente;
      if (!t) return;
      prima = document.activeElement;
      var n = ++numero;
      adatta();
      img.alt = t.autore + ', ' + t.titolo;
      img.src = t.miniatura || t.url;                      // subito quella già in cache
      if (t.url && t.url !== (t.miniatura || t.url)) {     // poi il file originale, più nitido da vicino
        var g = new Image();
        g.onload = function () { if (n === numero && !lente.hidden) img.src = t.url; };
        g.src = t.url;
      }
      $('lente-didascalia').innerHTML = $('didascalia').innerHTML;
      lente.dataset.ia = t.autore_id === IA ? '1' : '';
      lente.hidden = false;
      document.documentElement.classList.add('lente-aperta');
      chiudiBtn.focus();
    }
    function chiudi() {
      if (lente.hidden) return;
      lente.hidden = true; numero++;
      document.documentElement.classList.remove('lente-aperta');
      img.removeAttribute('src');
      if (prima && prima.focus) prima.focus();
    }
    function zoom(e) {
      var r = img.getBoundingClientRect();
      if (img.classList.contains('zoom')) { adatta(); return; }
      var fx = (e.clientX - r.left) / r.width, fy = (e.clientY - r.top) / r.height;
      img.style.width = Math.round(r.width * 2.2) + 'px';
      img.classList.add('zoom');
      var s = scorri.getBoundingClientRect();
      scorri.scrollLeft = fx * img.offsetWidth - (e.clientX - s.left);
      scorri.scrollTop = fy * img.offsetHeight - (e.clientY - s.top);
    }

    cornice.addEventListener('click', function (e) {
      if (e.target.closest('#tavola-pagina')) return;
      apri();
    });
    cornice.addEventListener('keydown', function (e) {
      if (e.target !== cornice || (e.key !== 'Enter' && e.key !== ' ')) return;
      e.preventDefault(); apri();
    });
    img.addEventListener('click', function (e) { e.stopPropagation(); zoom(e); });
    scorri.addEventListener('click', function (e) { if (e.target === scorri) chiudi(); });   // tocco sullo sfondo
    chiudiBtn.addEventListener('click', chiudi);
    document.addEventListener('keydown', function (e) {
      if (lente.hidden) return;
      if (e.key === 'Escape') { e.stopPropagation(); chiudi(); }
      else if (e.key === 'Tab') { e.preventDefault(); chiudiBtn.focus(); }     // il focus resta nel riquadro
    }, true);
  }

  /* ---------- modalità notte ---------- */
  function preparaTema() {
    var b = $('bottone-tema'), mq = window.matchMedia('(prefers-color-scheme: dark)');
    function scuro() { var t = document.documentElement.dataset.theme; return t ? t === 'dark' : mq.matches; }
    function icona() {
      var s = scuro();
      b.setAttribute('aria-pressed', String(s));
      b.title = s ? 'Modalità giorno' : 'Modalità notte';
      b.setAttribute('aria-label', b.title);
      b.innerHTML = s
        ? '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4.2"/><path d="M12 2.5v2.2M12 19.3v2.2M2.5 12h2.2M19.3 12h2.2M5.3 5.3l1.6 1.6M17.1 17.1l1.6 1.6M5.3 18.7l1.6-1.6M17.1 6.9l1.6-1.6"/></svg>'
        : '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20.5 14.5A8.5 8.5 0 0 1 9.5 3.5a8.5 8.5 0 1 0 11 11Z"/></svg>';
    }
    b.addEventListener('click', function () {
      var nuovo = scuro() ? 'light' : 'dark';
      document.documentElement.dataset.theme = nuovo;
      scrivi('ci-tema', nuovo);
      icona();
    });
    mq.addEventListener('change', icona);
    icona();
  }
})();
