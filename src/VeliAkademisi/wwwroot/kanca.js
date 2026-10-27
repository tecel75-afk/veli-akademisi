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

  function gonder(yol, veri) {
    try {
      fetch(yol, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(veri),
        keepalive: true                             // sayfa kapanırken de gitsin
      }).catch(function () {});
    } catch (e) {}
  }

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

  // Adım değişince: önce sayfanın kendi render'ı, sonra kayıt.
  var asilRender = render;
  render = function () {
    asilRender.apply(this, arguments);
    try { if (i >= 0) gonder('/api/ilerleme', { kod: kod, hikaye: hikaye, adim: i }); } catch (e) {}
  };

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
