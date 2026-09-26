# Skyloft Survivor Case

Android için tek arenalı bir survivor oyunu. Amaç, üç zorluk seviyesinden birini seçip üç dakika hayatta kalmak.

## Kurulum

1. Projeyi **Unity 6000.4.10f1** ile açın.
2. `Assets/Game/Scenes/Game.unity` sahnesini açın. Editor'da Play ile çalıştırabilirsiniz.
3. APK üretmek için Android hedefini seçip **Skyloft > Build > Clean Release Android APK** menüsünü kullanın. Çıktı: `Builds/Release/Skyloft-Survivor.apk`. Teslim APK'sı ayrıca paylaşılır.

Paketler `Packages/manifest.json` içinde tanımlıdır. Mesh sadeleştirmede kullanılan UnityMeshSimplifier repoya dahildir ve yalnızca Editor'da çalışır. Optimizasyon öncesi sürüm `baseline-gameplay` etiketiyle korunur.

## Kontroller

- **Hareket:** Ekrandaki sanal joystick'i sürükleyin; Editor'de fareyle kullanabilirsiniz.
- **Saldırı:** Menzildeki düşmanlara otomatik ateş edilir.
- **Yükseltme:** Yükseltme nesnelerini karakterle temas ederek toplayın. İki adet yükseltme bulunmaktadır: +%10 damage ve +%10 saldırı hızı.
- **Yeniden oynama:** Sonuç ekranındaki Replay ile zorluk seçimine dönün. Toplam öldürme sayısı oyun kapatıldığında korunur.

## Oyun ayarları ve yaklaşım

Ayarlar `Assets/Game/Settings` altındaki ScriptableObject asset'lerinde tutulur:

- **Match Settings:** Maç süresi ve kullanılacak zorluk profilleri.
- **Difficulty Profile:** Aynı anda canlı kalabilecek düşman sayısı ve bağlı dalga planı. Zorluklar aynı arenayı kullanır; ayrı level sahneleri oluşturulmaz.
- **Wave Plan:** Sırayla çalışan dalgaların süresi, düşman kotası ve spawn aralığı. İsteğe bağlı Constant Wave, maç boyunca ayrıca düşman üretir; tüm dalgalar ortak canlı düşman sınırına uyar. Dalga kotası dolunca sonraki dalganın zamanı beklenir.

Yeni ayarlar Project penceresinde **Create > Skyloft > Match Settings / Difficulty Profile / Wave Plan** üzerinden oluşturulur. Dalga planını zorluk profiline, profili maç ayarlarına bağlayın; menüdeki zorluk butonlarının referanslarını da güncelleyin.

Oyun akışını `GameManager`, dalga zamanlamasını `WaveDirector`, düşman üretimini `EnemySpawner` yönetir. Düşmanlar oyuncuyu takip eder ve separation steering ile birbirlerinden uzaklaşır. Hareket ve ateş animasyonları Animator katmanlarıyla birlikte çalışır. Düşman, mermi ve sık kullanılan efektler pooling ile yeniden kullanılır; enemy LOD'ları ve sadeleştirilmiş silah mobil geometri yükünü azaltır. Toplam öldürme sayısı PlayerPrefs ile saklanır.

## Kullanılan AI/MCP araçları

- **Codex:** Kod geliştirme, alternatiflerin değerlendirilmesi, optimizasyon ve ölçüm verilerinin analizi.
- **Unity MCP:** Editor ve asset durumunu okuma, sahne/prefab ve render ayarlarını düzenleme, Play Mode kontrolleri ve build işlemleri. Değişiklikler araç çıktıları ve cihaz testleriyle doğrulandı.

Üç önemli karar ve geliştirici değerlendirmeleri, teslimdeki **AI Çalışma Kaydı** belgesinde açıklanır.

## Bilinen eksikler

- UI ve sahnede placeholder öğeler kullanıldı; sunum minimal tutuldu.
- Cihaz doğrulaması **Samsung Galaxy A50** ile sınırlıdır; diğer cihazlar ve ekran oranları kapsamlı test edilmedi.
- Yoğun düşman sahnelerinde sabit 30 FPS garanti edilmez. Optimize Development benchmark yaklaşık 24–25 FPS, son Release APK'nın doğal oynanış ölçümü yaklaşık 30 FPS verdi; bunlar farklı test koşullarıdır.
- Separation, düşmanların yığılmasını azaltır; animasyonlu modellerin birbirine hiç girmesini garanti etmez. Komşu araması tüm aktif düşman listesini dolaştığı için daha yüksek yoğunluklarda iyileştirilebilir.
