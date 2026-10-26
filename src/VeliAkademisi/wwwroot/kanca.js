// Veli Akademisi · kayıt kancası
//
// Site bu dosyayı hikâye sayfasının en sonuna ekler. Sayfanın kendi kodu (index.html) değiştirilmez;
// bu dosya yalnızca sayfadaki iki işlevi "sarar": aynı işi yaparlar, ardından sunucuya kısa bir kayıt gönderilir.
//   render() → adım değişti   → POST /api/ilerleme {kod, hikaye, adim}
//   drawQ()  → soru çizildi    → şıklara dokunulunca POST /api/cevap {kod, hikaye, adim, secim}
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
