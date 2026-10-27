// Tarayıcı testi: m1 hikâye sayfasını gerçek bir Chrome'da açar ve kayıt kancasını (kanca.js) sınar.
// Kullanım: node test.mjs <site adresi> <veli kodu> <ilk sorunun adım numarası>
// Bir kontrol tutmazsa hata koduyla biter.
import puppeteer from 'puppeteer';

const [ADRES, KOD, SORU] = process.argv.slice(2);
const SORU_ADIMI = Number(SORU);
let hata = 0;
function kontrol(aciklama, beklenen, gelen) {
  if (String(beklenen) === String(gelen)) console.log(`  TAMAM  ${aciklama}`);
  else {
    console.log(`  HATA   ${aciklama} — beklenen: [${beklenen}], gelen: [${gelen}]`);
    console.log(`::error::Tarayıcı: ${aciklama} — beklenen: [${beklenen}], gelen: [${gelen}]`);
    hata = 1;
  }
}

const tarayici = await puppeteer.launch({ args: ['--no-sandbox', '--autoplay-policy=no-user-gesture-required'] });
try {
  const sayfa = await tarayici.newPage();

  // Sayfada yakalanmamış bir JavaScript hatası olursa not edilir (kanca sayfayı bozmamalı).
  const sayfaHatalari = [];
  sayfa.on('pageerror', h => sayfaHatalari.push(String(h)));

  // Sitenin dışındaki bir sunucuya giden her istek not edilir (olmamalı: yazı tipleri dahil her şey siteden gelir).
  const disIstekler = [];
  sayfa.on('request', r => { const u = new URL(r.url()); if (u.origin !== new URL(ADRES).origin && u.protocol.startsWith('http')) disIstekler.push(u.host); });

  // Tarayıcının gönderdiği kayıt isteklerini ve sunucunun cevaplarını dinle.
  const kayitlar = [];
  sayfa.on('response', async c => {
    const u = new URL(c.url());
    if (u.pathname.startsWith('/api/') && c.request().method() === 'POST') {   // yalnızca kayıtlar
      kayitlar.push({ yol: u.pathname, durum: c.status(), veri: JSON.parse(c.request().postData() || '{}') });
    }
  });
  const kayitBekle = (yol, kosul) => sayfa.waitForResponse(
    c => new URL(c.url()).pathname === yol && kosul(JSON.parse(c.request().postData() || '{}')), { timeout: 15000 });

  const durumBekle = () => sayfa.waitForResponse(c => new URL(c.url()).pathname === '/api/durum', { timeout: 15000 });

  console.log('a) Sayfa açılıyor (yeni veli)');
  let durumCevabi = durumBekle();
  await sayfa.goto(`${ADRES}/v/${KOD}/m1/`, { waitUntil: 'load' });
  kontrol('açılışta durum soruldu ve cevap geldi', 200, (await durumCevabi).status());
  kontrol('başlat düğmesi görünüyor', true, !!(await sayfa.$('#basla')));
  kontrol('yarım ilerleme yok: düğme yazısı değişmedi', 'Hikâyeyi başlat', await sayfa.$eval('#basla', b => b.textContent.trim()));
  kontrol('açılışta henüz kayıt gönderilmedi', 0, kayitlar.length);
  // Yazı tipleri sitenin kendi sunucusundan yüklendi mi ve Türkçe harfleri (ğ ş İ ı ç ö ü) kapsıyor mu?
  const yazitipi = await sayfa.evaluate(async () => {
    await Promise.all([document.fonts.load('700 32px "Baloo 2"', 'ğşİıçöü'), document.fonts.load('800 18px "Nunito"', 'ğşİıçöü'),
                       document.fonts.load('500 18px "Nunito"', 'ğşİıçöü')]);
    return [...document.fonts].filter(f => f.status === 'loaded').map(f => f.family.replace(/"/g, '') + ' ' + f.weight).sort().join(',');
  });
  kontrol('yazı tipleri yüklendi (Baloo 2 ve Nunito, Türkçe harflerle)', true,
    ['Baloo 2 700', 'Nunito 500', 'Nunito 800'].every(x => yazitipi.includes(x)));

  console.log('b) "Hikâyeyi başlat" düğmesine basılıyor');
  const ilkKayit = kayitBekle('/api/ilerleme', v => v.adim === 0);
  await sayfa.click('#basla');
  const c1 = await ilkKayit;
  kontrol('adım 0 için ilerleme kaydı gönderildi ve kabul edildi', 204, c1.status());

  console.log(`c) İlk soruya (adım ${SORU_ADIMI}) geçiliyor`);
  // Sayfanın kendi değişkeni ve işlevi kullanılır: i (adım) ve render(). Ses beklenmesin diye doğrudan atlanır.
  const soruKaydi = kayitBekle('/api/ilerleme', v => v.adim === SORU_ADIMI);
  await sayfa.evaluate(`i = ${SORU_ADIMI}; render();`);
  kontrol(`adım ${SORU_ADIMI} için ilerleme kaydı kabul edildi`, 204, (await soruKaydi).status());
  kontrol('soru ekranında 3 şık var', 3, (await sayfa.$$('.opt')).length);

  console.log('d) B şıkkına dokunuluyor');
  const cevapKaydi = kayitBekle('/api/cevap', v => v.adim === SORU_ADIMI);
  await sayfa.click('.opt[data-i="1"]');
  const c3 = await cevapKaydi;
  kontrol('cevap kaydı kabul edildi', 204, c3.status());
  kontrol('gönderilen seçim B (1)', 1, JSON.parse(c3.request().postData()).secim);
  // Sayfanın kendi davranışı da sürmeli: seçilen şık işaretlenir, geri bildirim görünür.
  kontrol('sayfa B şıkkını işaretledi', true, await sayfa.$eval('.opt[data-i="1"]', b => b.classList.contains('on')));
  kontrol('sayfa geri bildirimi gösterdi', true, !!(await sayfa.$('.fb')));

  console.log('e) Fikir değiştirilip C şıkkına dokunuluyor');
  const cevapKaydi2 = kayitBekle('/api/cevap', v => v.adim === SORU_ADIMI && v.secim === 2);
  await sayfa.click('.opt[data-i="2"]');
  kontrol('ikinci cevap kaydı kabul edildi', 204, (await cevapKaydi2).status());

  console.log('f) Sayfa kapatılıp yeniden açılıyor (kaldığı yerden devam)');
  durumCevabi = durumBekle();
  await sayfa.goto(`${ADRES}/v/${KOD}/m1/`, { waitUntil: 'load' });
  await durumCevabi;
  await sayfa.waitForFunction(() => document.getElementById('basla').textContent.trim() !== 'Hikâyeyi başlat', { timeout: 5000 })
    .catch(() => {});
  kontrol('düğme yazısı', 'Kaldığınız yerden devam edin', await sayfa.$eval('#basla', b => b.textContent.trim()));
  const devamKaydi = kayitBekle('/api/ilerleme', v => true);
  await sayfa.click('#basla');
  const dk = await devamKaydi;
  kontrol(`dokununca doğrudan kaldığı adım (${SORU_ADIMI}) açıldı`, SORU_ADIMI, JSON.parse(dk.request().postData()).adim);
  kontrol('açılan ekran soru ekranı (3 şık)', 3, (await sayfa.$$('.opt')).length);

  console.log('g) Modüller listesi');
  kontrol('Modüller düğmesi üst çubukta', true, !!(await sayfa.$('.top #moduller')));
  await sayfa.click('#moduller');
  await sayfa.waitForSelector('.va-satir', { timeout: 10000 });
  const satirlar = await sayfa.$$eval('.va-satir', s => s.map(x => x.dataset.hikaye + ':' + x.querySelector('.va-durum').textContent
    + (x.getAttribute('aria-disabled') === 'true' ? ':kapalı' : '')));
  kontrol('listede 8 hikâye var', 8, satirlar.length);
  kontrol('m1 "Devam ediyor", tıklanabilir', 'm1:Devam ediyor', satirlar[0]);
  kontrol('m2a "Kilitli", tıklanamaz', 'm2a:Kilitli:kapalı', satirlar[1]);
  kontrol('bulunulan hikâye işaretli', 'm1', await sayfa.$eval('.va-satir.su-an', x => x.dataset.hikaye));
  kontrol('liste açılınca hikâye duraklatıldı', 'Devam et', await sayfa.$eval('#dur', b => b.textContent.trim()));
  await sayfa.click('.va-satir[data-hikaye="m2a"]');
  await new Promise(r => setTimeout(r, 500));
  kontrol('kilitli hikâyeye dokununca sayfa değişmedi', `/v/${KOD}/m1/`, new URL(sayfa.url()).pathname);
  await sayfa.click('#va-kapat');
  kontrol('Kapat ile liste kapandı', false, !!(await sayfa.$('.va-perde')));
  await sayfa.click('#moduller');
  await sayfa.waitForSelector('.va-satir', { timeout: 10000 });
  await Promise.all([sayfa.waitForNavigation({ waitUntil: 'load' }), sayfa.click('.va-satir[data-hikaye="m1"]')]);
  kontrol('açık hikâyeye dokununca o hikâye açıldı', `/v/${KOD}/m1/`, new URL(sayfa.url()).pathname);

  console.log('h) Telefon ekranı (iPhone 13 taklidi)');
  {
    const telefon = await tarayici.newPage();
    // iPhone 13 ekranı: 390×844, mobil tarayıcı (isMobile: sayfadaki ekran satırını dikkate alır)
    await telefon.setViewport({ width: 390, height: 844, deviceScaleFactor: 3, isMobile: true, hasTouch: true });
    await telefon.goto(`${ADRES}/v/${KOD}/m1/`, { waitUntil: 'load' });
    const olcu = await telefon.evaluate(() => ({ genislik: window.innerWidth,
      yatayKayma: document.documentElement.scrollWidth > window.innerWidth }));
    kontrol('sayfa ekran genişliğinde (390 px) çiziliyor', 390, olcu.genislik);
    kontrol('sayfa yana kaymıyor', false, olcu.yatayKayma);
    await telefon.close();
  }

  kontrol('dışarıdaki hiçbir sunucuya istek gitmedi (Google dahil)', '', [...new Set(disIstekler)].join(', '));
  kontrol('sayfada yakalanmamış JavaScript hatası yok', '', sayfaHatalari.join(' | '));
  kontrol('reddedilen kayıt isteği yok', '', kayitlar.filter(k => k.durum !== 204).map(k => `${k.yol} ${k.durum}`).join(', '));
} catch (h) {
  console.log(`::error::Tarayıcı testi yarıda kaldı: ${h.message}`);
  hata = 1;
} finally {
  await tarayici.close();
}
process.exit(hata);
