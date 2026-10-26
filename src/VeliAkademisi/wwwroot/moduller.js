// Veli Akademisi · Modüller düğmesi ve listesi
//
// Site bu dosyayı hikâye sayfasının sonuna (kanca.js'ten sonra) ekler. Sayfanın kendi kodu değiştirilmez.
// Üst çubuğa küçük bir "Modüller" düğmesi koyar. Dokununca 8 hikâyenin listesi açılır:
//   Tamamlandı / Devam ediyor / Yeni (açıldı, henüz başlanmadı) / Kilitli.
// Kilitli hikâyeler tıklanamaz; diğerlerine dokununca o hikâyenin sayfası açılır.
// Liste açılınca hikâye duraklar; kapanınca kendiliğinden sürmez (veli sayfanın "Devam et" düğmesine basar).
// Görünüm, sayfanın kendi renklerini ve kart biçimini kullanır.
(function () {
  var m = location.pathname.match(/^\/v\/([A-Za-z0-9_-]{22})\/([a-z0-9]+)\/$/);
  if (!m) return;
  var kod = m[1], buHikaye = m[2];
  var ust = document.querySelector('.top');
  if (!ust) return;

  var ETIKET = { tamamlandi: 'Tamamlandı', devam: 'Devam ediyor', baslanmadi: 'Yeni', kilitli: 'Kilitli' };

  // Listeye özgü görünüm (sayfanın renk değişkenleriyle; açık ve koyu temada çalışır).
  var stil = document.createElement('style');
  stil.textContent =
    '.va-perde{position:fixed;inset:0;background:rgba(10,20,22,.55);display:flex;justify-content:center;align-items:flex-start;' +
    'padding:16px;overflow-y:auto;z-index:50}' +
    '.va-panel{background:var(--bg);border-radius:20px;width:100%;max-width:560px;padding:16px;display:flex;flex-direction:column;gap:10px}' +
    '.va-baslik{display:flex;align-items:center;gap:10px}.va-baslik .h2{flex:1}' +
    '.va-satir{display:flex;align-items:center;gap:12px;width:100%;text-align:left;font:inherit;color:var(--ink);cursor:pointer;' +
    'background:var(--surface);border:2px solid var(--line);border-radius:16px;padding:12px 14px;min-height:68px}' +
    '.va-satir.su-an{border-color:var(--accent)}' +
    '.va-satir[aria-disabled="true"]{opacity:.5;cursor:not-allowed}' +
    '.va-satir .va-ad{flex:1;display:flex;flex-direction:column;gap:2px}' +
    '.va-satir .va-modul{font-size:13px;font-weight:800;letter-spacing:.06em;text-transform:uppercase;color:var(--muted)}' +
    '.va-satir .va-hikaye{font-size:18px;font-weight:800;line-height:1.3}' +
    '.va-durum{flex:none;font-size:13px;font-weight:800;border-radius:999px;padding:4px 10px;background:var(--soft);color:var(--ink)}' +
    '.va-durum.tamamlandi{background:var(--accent);color:var(--accent-ink)}' +
    '.va-satir:focus-visible{outline:3px solid var(--accent);outline-offset:3px}';
  document.head.appendChild(stil);

  // Üst çubuktaki düğme (başlangıç ekranında da görünür).
  var dugme = document.createElement('button');
  dugme.className = 'icon';
  dugme.id = 'moduller';
  dugme.textContent = 'Modüller';
  dugme.setAttribute('aria-haspopup', 'dialog');
  ust.appendChild(dugme);

  var perde = null;

  function kapat() {
    if (perde) { perde.remove(); perde = null; dugme.focus(); }
  }

  function ac() {
    // Hikâye akıyorsa duraklat (sayfanın kendi Duraklat düğmesiyle).
    try { if (i >= 0 && !paused) durBtn.click(); } catch (e) {}

    perde = document.createElement('div');
    perde.className = 'va-perde';
    perde.innerHTML =
      '<div class="va-panel" role="dialog" aria-modal="true" aria-labelledby="va-baslik">' +
      '<div class="va-baslik"><h2 class="h2" id="va-baslik">Modüller</h2>' +
      '<button class="icon" id="va-kapat">Kapat</button></div>' +
      '<p class="note" id="va-liste-durum" style="margin:0">Yükleniyor…</p>' +
      '<div id="va-liste" style="display:flex;flex-direction:column;gap:10px"></div></div>';
    document.body.appendChild(perde);
    perde.querySelector('#va-kapat').onclick = kapat;
    perde.onclick = function (o) { if (o.target === perde) kapat(); };   // panelin dışına dokununca kapanır
    perde.querySelector('#va-kapat').focus();

    // Durum her açılışta yeniden sorulur; böylece liste her zaman günceldir.
    fetch('/api/durum?kod=' + kod + '&hikaye=' + buHikaye)
      .then(function (c) { return c.ok ? c.json() : null; })
      .catch(function () { return null; })
      .then(function (d) {
        if (!perde) return;
        var bilgi = perde.querySelector('#va-liste-durum');
        if (!d) { bilgi.textContent = 'Liste şu anda yüklenemedi. Lütfen biraz sonra yeniden deneyin.'; return; }
        bilgi.remove();
        var liste = perde.querySelector('#va-liste');
        d.hikayeler.forEach(function (h) {
          var kilitli = h.durum === 'kilitli';
          var satir = document.createElement('button');
          satir.className = 'va-satir' + (h.kod === buHikaye ? ' su-an' : '');
          satir.dataset.hikaye = h.kod;
          if (kilitli) satir.setAttribute('aria-disabled', 'true');
          satir.innerHTML = '<span class="va-ad"><span class="va-modul"></span><span class="va-hikaye"></span></span>' +
            '<span class="va-durum ' + h.durum + '"></span>';
          satir.querySelector('.va-modul').textContent = h.modul;
          satir.querySelector('.va-hikaye').textContent = h.ad;
          satir.querySelector('.va-durum').textContent = ETIKET[h.durum] || '';
          satir.onclick = function () {
            if (kilitli) return;
            location.href = '/v/' + kod + '/' + h.kod + '/';
          };
          liste.appendChild(satir);
        });
      });
  }

  dugme.onclick = ac;
  document.addEventListener('keydown', function (o) { if (o.key === 'Escape') kapat(); });
})();
