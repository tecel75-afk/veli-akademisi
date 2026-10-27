// Veli Akademisi · kayıt kancası
//
// Site bu dosyayı hikâye sayfasının en sonuna ekler. Sayfanın kendi kodu (index.html) değiştirilmez;
// bu dosya yalnızca sayfadaki iki işlevi "sarar": aynı işi yaparlar, ardından sunucuya kısa bir kayıt gönderilir.
//   render() → adım değişti   → POST /api/ilerleme {kod, hikaye, adim}
//   drawQ()  → soru çizildi    → şıklara dokunulunca POST /api/cevap {kod, hikaye, adim, secim}
// Ayrıca açılışta GET /api/durum ile başlat düğmesini "kaldığı yerden devam"a çevirir (aşağıda).
// Kayıt gönderilemezse (internet yok, sunucu cevap vermiyor) sayfa hiçbir şey olmamış gibi çalışmaya devam eder.
(function () {
  // Kod ve hikâye adresten okunur: /v/{kod}/{hikaye}/
  var m = location.pathname.match(/^\/v\/([A-Za-z0-9_-]{22})\/([a-z0-9]+)\/$/);
  if (!m) return;                                   // kodsuz açılmışsa hiçbir şey gönderilmez
  if (typeof render !== 'function' || typeof drawQ !== 'function') return;  // beklenen sayfa değilse dokunma
  var kod = m[1], hikaye = m[2];

  // Kayıt gönderir. Sonucu bekleyen bir söz (Promise) döner; hata olursa sessizce geçer.
  function gonder(yol, veri) {
    try {
      return fetch(yol, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(veri),
        keepalive: true                             // sayfa kapanırken de gitsin
      }).catch(function () {});
    } catch (e) { return Promise.resolve(); }
  }
  var sonIlerleme = Promise.resolve();   // son gönderilen ilerleme kaydı (Sonraki modüle geçmeden beklenir)

  // Tek dokunuşla devam (2b): sayfa açılınca sunucuya bu hikâyede nerede kalındığı sorulur.
  // Yarım kalmışsa başlat düğmesinin yazısı değişir ve dokununca doğrudan kaldığı adım açılır.
  // Yarım kalmış bir ilerleme yoksa düğme olduğu gibi kalır ("Hikâyeyi başlat").
  var durum;   // undefined: cevap henüz gelmedi; null: cevap alınamadı; nesne: sunucunun cevabı
  var durumSozu = fetch('/api/durum?kod=' + kod + '&hikaye=' + hikaye)
    .then(function (c) { return c.ok ? c.json() : null; })
    .catch(function () { return null; })
    .then(function (d) { durum = d; return d; });

  var basla = document.getElementById('basla');
  if (basla) {
    // Yapay zekâ notu (4b): başlat düğmesinin hemen altında küçük ve soluk bir satır.
    var yzNotu = document.createElement('p');
    yzNotu.className = 'va-yz-notu';
    yzNotu.textContent = 'Seslendirme ve görseller yapay zekâ ile hazırlanmıştır.';
    yzNotu.style.cssText = 'margin:0;font-size:14px;font-weight:500;color:var(--muted);text-align:center';
    basla.insertAdjacentElement('afterend', yzNotu);

    durumSozu.then(function (d) {
      if (d && d.yarimKaldi) basla.textContent = 'Kaldığınız yerden devam edin';
    });
    basla.onclick = function () {
      if (basla.disabled) return;
      basla.disabled = true;
      bedStart();   // ortam sesi dokunuşun içinde başlatılır (telefonlar sesi ancak dokunuşla açar)
      function baslat(d) { i = (d && d.yarimKaldi) ? d.baslangicAdimi : 0; render(); }
      if (durum !== undefined) { baslat(durum); return; }   // cevap gelmişse hemen (dokunuşun içinde)
      // Cevap henüz gelmediyse en çok 3 saniye beklenir; gelmezse hikâye baştan başlar.
      var sure = new Promise(function (ok) { setTimeout(function () { ok(null); }, 3000); });
      Promise.race([durumSozu, sure]).then(baslat);
    };
  }

  // Adım değişince: önce sayfanın kendi render'ı, sonra kayıt. Kapanış ekranındaysa hikâye sonu eklenir (4c).
  var asilRender = render;
  render = function () {
    asilRender.apply(this, arguments);
    try { if (i >= 0) sonIlerleme = gonder('/api/ilerleme', { kod: kod, hikaye: hikaye, adim: i }); } catch (e) {}
    try { if (STEPS[i] && STEPS[i].t === 'close') durumSozu.then(hikayeSonu); } catch (e) {}
  };

  // --- Hikâye sonu (4c): kapanış yazısının altında Materyaller kartı ve "Sonraki modüle geç" düğmesi ---
  var stil = document.createElement('style');
  stil.textContent =
    '.va-son{display:flex;flex-direction:column;gap:12px}' +
    '.va-izgara{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}' +
    '.va-kucuk{display:flex;flex-direction:column;gap:6px;background:none;border:0;padding:0;font:inherit;color:var(--ink);' +
    'text-align:left;cursor:pointer}' +
    '.va-kucuk img{width:100%;aspect-ratio:1/1;object-fit:cover;border-radius:12px;border:1px solid var(--line);background:var(--line)}' +
    '.va-kucuk span{font-size:15px;font-weight:700;line-height:1.3}' +
    '.va-buyuk{position:fixed;inset:0;background:rgba(10,20,22,.82);z-index:60;display:flex;flex-direction:column;' +
    'align-items:center;justify-content:center;gap:12px;padding:16px}' +
    '.va-buyuk img{max-width:100%;max-height:70vh;border-radius:12px;background:#fff}' +
    '.va-buyuk p{margin:0;color:#fff;font-weight:700;text-align:center}' +
    '.va-buyuk .va-dugmeler{display:flex;gap:10px;width:100%;max-width:420px}' +
    '.va-buyuk .va-dugmeler>*{flex:1;text-align:center;text-decoration:none;display:flex;align-items:center;justify-content:center}' +
    '.va-dis{font-weight:800;color:var(--accent)}';
  document.head.appendChild(stil);

  function oge(etiket, sinif, yazi) {
    var e = document.createElement(etiket);
    if (sinif) e.className = sinif;
    if (yazi != null) e.textContent = yazi;
    return e;
  }

  function hikayeSonu(d) {
    // Kapanış ekranında değilsek ya da zaten eklendiyse bir şey yapma.
    if (!d || !STEPS[i] || STEPS[i].t !== 'close' || stage.querySelector('.va-son')) return;
    var son = oge('div', 'va-son');
    var mat = d.materyaller || { gorseller: [] };

    if (mat.gorseller.length || mat.baglanti) {
      var kart = oge('div', 'card va-materyaller');
      kart.style.cssText = 'display:flex;flex-direction:column;gap:12px';
      kart.appendChild(oge('h2', 'h2', 'Materyaller'));
      if (mat.gorseller.length) {
        kart.appendChild(oge('p', 'note', 'Görsele dokunarak büyütebilir, “İndir” ile telefonunuza kaydedebilirsiniz.'));
        var izgara = oge('div', 'va-izgara');
        mat.gorseller.forEach(function (g) {
          var b = oge('button', 'va-kucuk');
          b.type = 'button';
          var res = document.createElement('img');
          res.src = 'materyal/' + g.dosya; res.alt = g.baslik || ''; res.loading = 'lazy';
          b.appendChild(res);
          if (g.baslik) b.appendChild(oge('span', '', g.baslik));
          b.onclick = function () { buyut(g); };
          izgara.appendChild(b);
        });
        kart.appendChild(izgara);
      }
      if (mat.baglanti) {
        var a = oge('a', 'va-dis', mat.baglanti.yazi + ' ↗');
        a.href = mat.baglanti.adres; a.target = '_blank'; a.rel = 'noopener noreferrer';
        kart.appendChild(a);
      }
      son.appendChild(kart);
    }

    if (d.sonraki) {
      var sonraki = oge('button', 'btn', 'Sonraki modüle geç');
      sonraki.id = 'va-sonraki';
      sonraki.onclick = function () {
        sonraki.disabled = true;
        // Kapanışın kaydı sunucuya ulaşmadan geçilirse sıradaki hikâye henüz açılmamış olabilir; önce o beklenir.
        sonIlerleme.then(function () { location.href = '/v/' + kod + '/' + d.sonraki + '/'; });
      };
      son.appendChild(sonraki);
    }
    if (son.children.length) stage.appendChild(son);
  }

  function buyut(g) {
    var perde = oge('div', 'va-buyuk');
    perde.setAttribute('role', 'dialog'); perde.setAttribute('aria-modal', 'true');
    var res = document.createElement('img');
    res.src = 'materyal/' + g.dosya; res.alt = g.baslik || '';
    perde.appendChild(res);
    if (g.baslik) perde.appendChild(oge('p', '', g.baslik));
    var dugmeler = oge('div', 'va-dugmeler');
    var indir = oge('a', 'btn', 'İndir');
    indir.href = 'materyal/' + g.dosya; indir.setAttribute('download', g.dosya); indir.id = 'va-indir';
    var kapat = oge('button', 'icon', 'Kapat');
    kapat.type = 'button'; kapat.id = 'va-buyuk-kapat';
    kapat.style.cssText = 'min-height:60px;border-radius:16px;font-size:18px';
    kapat.onclick = function () { perde.remove(); };
    perde.onclick = function (o) { if (o.target === perde) perde.remove(); };
    dugmeler.appendChild(indir); dugmeler.appendChild(kapat);
    perde.appendChild(dugmeler);
    document.body.appendChild(perde);
    kapat.focus();
  }

  // Soru çizilince: şık düğmelerine kayıt eklenir; sonra düğmenin kendi işi aynen yapılır.
  var asilDrawQ = drawQ;
  drawQ = function () {
    asilDrawQ.apply(this, arguments);
    try {
      var adim = i;
      stage.querySelectorAll('.opt').forEach(function (b) {
        var asilTik = b.onclick;
        b.onclick = function (olay) {
          gonder('/api/cevap', { kod: kod, hikaye: hikaye, adim: adim, secim: +b.dataset.i });
          return asilTik.call(this, olay);
        };
      });
    } catch (e) {}
  };
})();
