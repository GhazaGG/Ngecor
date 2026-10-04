# MAT-002 - Test Cases Gundukan Pasir dan Bucket

**Tiket:** [#16 - Sand pile and bucket on bulk container](https://github.com/GhazaGG/Ngecor/issues/16)

Checklist ini untuk kamu jalankan di Unity pada implementasi sekarang. Semua hasil manual dimulai dari **NOT VERIFIED**. Hasil automated test sebelumnya tidak mengisi hasil manual secara otomatis.

## 1. Scope tiket saat ini

| Termasuk MAT-002 | Batasnya |
| --- | --- |
| Gundukan pasir sebagai sumber terbatas | Satu tipe Sand, jumlah awal dapat dituning, memakai MAT-005, tanpa Rigidbody. |
| Visual pile mengikuti jumlah pasir | Gundukan mengecil secara keseluruhan, alas tetap, visual dan collider tidak aktif saat kosong. |
| Prefab bucket | Satu tipe material, kapasitas dapat dituning, memakai container MAT-005 dan generic grab INT-002. |
| Carry dan drop bucket | Menggunakan sistem interaction yang sudah ada; cek penggunaan prefab bucket dengan sistem tersebut. |
| Tuang eksplisit ke receiver dekat | Bucket harus sedang dipegang; unit bucket berkurang sebanyak kenaikan receiver. |
| Pemilihan receiver saat trigger overlap | Root receiver terdekat dipilih pada setiap permintaan tuang. Jarak kuadrat minimum sama persis membatalkan tuang; receiver terdekat penuh/menolak Sand tidak menyebabkan fallback. |
| Receiver menolak Sand | Tidak ada unit hilang karena percobaan transfer yang ditolak. |
| Kehilangan isi saat bucket miring | Menggunakan aturan tumpah MAT-005; uji saat jatuh dan, setelah tersedia, saat dilempar. |
| Binding Pour | Usulan tahan `R`, dicatat di `docs/DECISIONS.md`, terpisah dari Interact `E` dan Throw klik kiri. |
| Feedback tuang sederhana | Saat transfer berjalan, model bucket miring dan butiran pasir terlihat menuju receiver. |

**Belum termasuk:** shovel/scoop dan mengisi bucket dari pile (MAT-004 #54), cekungan lokal/bekas sekop, tumpahan menjadi pile (MAT-004 #54), animasi gundukan mengempis/membal, VFX final, texture semen/final art, resep/aduk beton, dan sinkronisasi multiplayer. Reaksi gundukan mengempis/membal yang pernah dibahas masih usulan polish; efek itu belum diimplementasikan. Butiran pasir sederhana saat tuang bucket sekarang sudah tersedia.

**Batas physics sekarang:** isi pasir belum menambah `Rigidbody.mass` bucket. Tidak ada angka kilogram per unit bulk yang disepakati. [MAT-005 #62](https://github.com/GhazaGG/Ngecor/issues/62) memperbolehkan pasir tumpah menghilang sementara sampai MAT-004 selesai. Kedua hal ini tidak menjadi expected result yang harus berubah dalam checklist ini.

## 2. Catatan sesi dan hasil

- Tester:
- Tanggal/jam:
- Branch/revisi yang diuji (baseline lokal: `feat/sand-bucket`):
- Unity version (expected `6000.3.25f1`):
- Scene:
- Console error sebelum Play:
- Console error baru setelah Play:
- Hasil keseluruhan:
- Reviewer gameplay:

| Status | Arti |
| --- | --- |
| PASS | Langkah dijalankan dan expected result benar-benar diamati. |
| FAIL | Langkah dijalankan, tetapi hasil berbeda dari expected result. |
| BLOCKED | Setup atau dependency menghalangi tes; tulis penyebabnya. |
| NOT VERIFIED | Belum dijalankan atau bukti belum cukup. |

Simpan screenshot/rekaman dan angka sebelum/sesudah pada kolom bukti. Untuk transfer, jangan menyimpulkan jumlah kekal dari visual saja.

## 3. Setup di Unity

1. Buka project lokal yang berisi perubahan MAT-002 dengan Unity `6000.3.25f1`.
2. Buka `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`. Scene ini punya pekerjaan lokal lain. Untuk perubahan layout/Inspector selama tes, gunakan **File > Save As** di Unity ke `Assets/Game/Scenes/Dev/Dev_Ghaza_MAT002.unity`. Jangan menimpa `Playground.unity` atau menerapkan override tes ke prefab asli.
3. Cari objek berikut di Hierarchy. Jangan tertukar dengan demo lama bernama `Sand Source`, `Cement Source`, atau receiver lain.

| Objek | Baseline yang diperiksa saat dokumen dibuat |
| --- | --- |
| `MAT-002 Sand Pile` | Capacity 100; Mode Single Type; Single Type Sand; Contents Sand 80; tanpa Rigidbody. |
| `MAT-002 Bucket` | Capacity 20; Contents Sand 12 pada instance scene; Rigidbody mass 2.5; spill angle 70 derajat; spill rate 10 unit/detik. Prefab bucket sendiri mulai kosong. |
| `MAT-002 Sand Receiver` | Capacity 100; menerima Sand; Contents kosong; spill rate 0. |
| `MAT-002 Reject Receiver` | Capacity 100; menerima Cement; Contents kosong; spill rate 0. |
| `Player` | Player lokal dengan kamera dan generic grab. |

4. Catat nilai Inspector sebenarnya sebelum tes; jika berbeda, gunakan baseline di atas pada **salinan scene** atau catat perbedaannya. Untuk Contents, buat satu entry dengan Type yang sesuai dan Units yang diminta. Jumlah bucket/receiver bisa dibaca dari `Bulk Material Container > Contents > Units`; jika ada beberapa entry, jumlahkan semuanya.
5. Atur isi bucket dan receiver **sebelum Play** untuk setiap kasus. Stop lalu Play ulang untuk reset; perubahan saat Play tidak disimpan sebagai baseline. Khusus tes pile TC-03, perubahan Units saat Play memang bagian tes.
6. Receiver punya child `Pour Trigger`. Gunakan Scene view dan Gizmos untuk melihat batas trigger. Saat uji satu receiver, jauhkan trigger receiver lain agar tidak saling overlap.
7. Untuk tes transfer, jaga bucket tegak dan pandangan mendekati horizontal. Menunduk terlalu jauh bisa memiringkan bucket melewati batas tumpah, sehingga kehilangan karena spill mencampuri pengukuran transfer.
8. Klik Game view sebelum menggunakan input: WASD/mouse untuk bergerak/lihat, `E` untuk grab/drop, **tahan `R`** untuk tuang. Setelah berpindah ke Inspector, klik Game view lagi. Lepaskan `R` sebelum memeriksa angka.
9. Catat Console awal dan ambil screenshot layout. Jangan memasang package atau mengubah ProjectSettings untuk menjalankan checklist ini.

**Cara mengambil angka:** lepaskan `R`, biarkan beberapa physics tick berjalan, lalu Pause. Catat bucket dan receiver pada keadaan Pause yang sama. Setelah itu Resume. Jangan membandingkan angka dari dua waktu berbeda ketika tuang masih berjalan.

## 4. Test cases

### TC-MAT002-01 - Prefab, konfigurasi, dan binding

**Setup:** Edit Mode; pilih prefab `SandPile.prefab` dan `Bucket.prefab` di `Assets/Game/Prefabs/Material/`.

**Langkah:**

1. Periksa pile: `BulkMaterialContainer`, `SandPileVisual`, child `Mound`, MeshFilter, dan MeshCollider. Pastikan tidak ada Rigidbody; mesh collider sesuai mesh visual; referensi Mound terisi.
2. Periksa bucket: Rigidbody, `GrabbableObject`, `BulkMaterialContainer`, `BucketPourAction`, `BucketPourInput`, dan `BucketPourVisual`. Ada child `Pour Model` dengan model/dasar/dinding dan lima BoxCollider, serta child `Sand Flow` dengan ParticleSystem. Referensi Model dan Sand Flow pada BucketPourVisual terisi. Tidak ada BoxCollider penuh yang menutup rongga pada root.
3. Pastikan Mode Single Type dan Single Type Sand. Pada salinan scene, ubah Capacity bucket menjadi 10 dan isi awal menjadi 8. Play: jumlah awal 8 dan tidak melebihi kapasitas. Stop dan pulihkan Capacity 20/isi 12 sebelum tes lain.
4. Buka `Assets/Game/Settings/InputSystem_Actions.inputactions`: `Player/Pour` adalah Button dengan binding `R`; Interact tetap `E`; binding Throw/Attack tetap klik kiri. Periksa referensi `PlayerPour` pada BucketPourInput dan catatan usulan di `docs/DECISIONS.md`.

**Expected:** Komponen dan referensi lengkap, jumlah/kapasitas dapat dituning, pile statis, bucket memakai generic grab, dan binding tidak bentrok. Pemeriksaan binding ini belum membuktikan tombol `R` bekerja; jalankan TC-07 juga.

**Status:** NOT VERIFIED

**Nilai aktual / bukti / catatan:** ______________________________

### TC-MAT002-02 - Bentuk gundukan dan collision statis

**Setup:** Pile Sand 80/Capacity 100; Play Mode.

**Langkah:**

1. Lihat pile dari samping, depan, dan sudut miring. Dekati dengan player, lalu coba tekan `E` pada pile.
2. Amati kontak player dengan lereng; periksa collider di Scene view.
3. Jika diperlukan, di salinan scene buat Sphere kecil dengan Rigidbody di atas lereng sebelum Play, lalu amati saat jatuh mengenai permukaan pile.

**Expected:** Terlihat seperti gundukan dengan puncak dan lereng, alas berada di tanah, dan pile tidak bergerak atau menjadi objek yang dipegang. Collision mengikuti lereng; tidak ada tembok kotak besar di sekitar visual. Bola yang mengenai lereng mendapat kontak dan boleh menggelinding turun. Tapak dari atas boleh sedikit oval/tidak beraturan; yang dinilai adalah bentuk gundukan dari samping dan kecocokan collision.

**Status:** NOT VERIFIED

**Sudut pandang / observasi kontak / rekaman:** ______________________________

### TC-MAT002-03 - Pile mengecil, kosong, dan kembali terisi

**Setup:** Pile 80/100; root pile tetap aktif. Tes ini mengubah angka lewat Inspector, bukan memakai sekop.

**Langkah:**

1. Saat Play, pilih root `MAT-002 Sand Pile`. Ubah Units pada entry Sand bertahap: 80 -> 40 -> 10 -> 0. Resume dan tunggu beberapa frame setelah tiap perubahan.
2. Amati lebar, kedalaman, tinggi, dan posisi alas di setiap tahap.
3. Pada 0, coba melewati lokasi pile. Periksa child Mound: tidak aktif, sehingga collider tidak ikut menghalangi player.
4. Ubah Units menjadi 80 lagi pada root yang sama; Resume dan amati kemunculan visual serta kontaknya.

**Expected:** Gundukan mengecil pada semua sumbu, lereng tetap serupa dan alas tidak mengambang/tenggelam. Pada 0 tidak ada visual atau collision aktif; skala collider tidak dipaksa menjadi nol. Pada 80 visual dan collision kembali. Root container boleh tetap ada saat kosong. Cekungan lokal dan animasi membal tidak diharapkan.

**Status:** NOT VERIFIED

**Bukti untuk 80 / 40 / 10 / 0 / isi kembali:** ______________________________

### TC-MAT002-04 - Rongga bucket benar-benar terbuka

**Setup:** Gunakan salinan scene. Bucket kosong, tegak, root scale (1,1,1). Untuk mengisolasi collision rongga, set Rigidbody bucket `Is Kinematic` on pada kasus ini saja.

**Langkah:**

1. Di Unity Editor buat Sphere dengan scale (0.08,0.08,0.08), Rigidbody dinamis, Use Gravity on, dan Collision Detection Continuous Dynamic.
2. Letakkan tepat di atas tengah bucket, sekitar 1 meter di atas posisi root bucket. Play dan tunggu 1-2 detik.
3. Amati apakah Sphere masuk melalui mulut bucket dan berhenti pada dasarnya.
4. Stop. Hapus Sphere melalui Unity Editor dan pulihkan Rigidbody bucket: Is Kinematic off, Use Gravity on.

**Expected:** Sphere masuk ke rongga dan ditahan dasar bucket; tidak mengambang di atas collider penutup atau menembus dasar. Untuk geometri/scale baseline, pusat Sphere berhenti sekitar 0.23 meter di bawah posisi root bucket. Kasus kinematic ini memeriksa rongga; carry/drop dinamis diuji terpisah.

**Status:** NOT VERIFIED

**Posisi berhenti / rekaman / konfigurasi:** ______________________________

### TC-MAT002-05 - Grab, carry, dan drop bucket

**Setup:** Bucket dinamis, tegak, Sand 12. Area lapang, jauh dari trigger receiver.

**Langkah:**

1. Arahkan pandangan ke bucket dari jarak dekat dan tekan `E` sekali.
2. Berjalan maju/mundur dan berputar selama sekitar 5 detik, dengan pandangan mendekati horizontal. Dekati dinding perlahan untuk menilai handling.
3. Tekan `E` sekali lagi untuk drop ke lantai lapang. Amati jatuh/kontak, lalu grab ulang.
4. Ulangi grab/drop tiga kali.

**Expected:** Bucket mengikuti hold point, tidak terlepas spontan, dan jumlah tetap selama tegak tanpa tuang. Drop melepaskan bucket serta memulihkan Rigidbody dinamis/gravity; bucket mendapat kontak lantai dan dapat diambil kembali. Tidak ada penetrasi berat, jitter terus-menerus, atau error baru. Jika bucket terjatuh miring, kehilangan isi dicatat sebagai spill, bukan sebagai transfer.

**Status:** NOT VERIFIED

**Jumlah awal/akhir / feel carry / kontak lantai / rekaman:** ______________________________

### TC-MAT002-06 - Tidak menuang tanpa syarat yang benar

**Setup:** Reset bucket Sand 12; Sand Receiver kosong; bucket selalu tegak.

**Langkah:**

1. Bawa bucket sampai salah satu collidernya overlap Pour Trigger receiver. Tunggu 1 detik tanpa `R`.
2. Di percobaan baru, biarkan bucket tidak dipegang tetapi berada dalam trigger; tekan `R` selama 1 detik.
3. Di percobaan baru, pegang bucket jauh di luar seluruh trigger; tahan `R` selama 1 detik.
4. Periksa angka pada setiap percobaan.

**Expected:** Pada ketiga kondisi, bucket tetap 12 dan receiver tetap 0. Dekat saja, tombol tanpa memegang bucket, atau memegang bucket di luar jangkauan tidak memicu transfer.

**Status:** NOT VERIFIED

**Angka tiap percobaan / bukti:** ______________________________

### TC-MAT002-07 - Tahan R untuk tuang dan jumlah tetap kekal

**Setup:** Bucket 12; Sand Receiver 0/100; tidak ada trigger receiver lain yang overlap.

**Langkah:**

1. Catat jumlah awal: bucket 12 + receiver 0 = 12.
2. Grab bucket, jaga tegak, lalu masuk ke trigger Sand Receiver. Tahan `R` sekitar 0.5 detik dan lepaskan.
3. Amati model bucket miring dan butiran pasir keluar dari bibirnya menuju receiver. Tunggu beberapa physics tick setelah melepas R, Pause, dan catat kedua jumlah pada keadaan yang sama. Amati level isi bucket dan receiver.
4. Resume; tahan `R` hingga bucket kosong. Lepaskan, lalu catat angka. Tahan `R` lagi selama 1 detik dengan bucket kosong.

**Expected:** Transfer pertama memindahkan bilangan bulat positif; penurunan bucket persis sama dengan kenaikan receiver dan total tetap 12. Visual isi mengikuti jumlah, model bucket miring, dan butiran pasir terlihat. Akhirnya bucket 0/receiver 12; percobaan tambahan tidak membuat angka negatif, menambah material, atau error. Tidak perlu menuntut tepat 5 unit pada percobaan 0.5 detik karena durasi input manual bervariasi. Animasi dan butiran adalah feedback; jumlah material tetap ditentukan container.

**Status:** NOT VERIFIED

**Bucket awal/akhir:** ________ **Receiver awal/akhir:** ________

**Total awal/akhir / bukti:** ______________________________

### TC-MAT002-08 - Tuang berhenti saat intent atau jangkauan berakhir

**Setup:** Reset seperti TC-07 sebelum setiap percobaan; sisakan unit agar sumber belum kosong.

**Langkah:**

1. Mulai tuang sebentar, lalu lepaskan `R`. Setelah beberapa physics tick, catat angka; tunggu 0.5 detik lagi dan bandingkan.
2. Reset. Tahan `R` sambil bergerak meninggalkan receiver. Dengan Gizmos, amati sampai **semua** collider bucket keluar dari trigger. Catat angka setelah beberapa physics tick dan sesudah 0.5 detik.
3. Reset. Tuang sebentar lalu drop dengan `E` sambil `R` masih ditahan. Lepaskan `R`; amati receiver setelah beberapa physics tick.

**Expected:** Setelah `R` dilepas atau seluruh collider keluar, angka transfer berhenti berubah. Selama satu collider masih overlap, bucket masih boleh dianggap dekat. Setelah drop, receiver tidak terus bertambah dari intent bucket yang sudah tidak dipegang. Bucket yang miring setelah drop boleh kehilangan isi karena spill; bedakan dari kenaikan receiver. Kasus yang sumbernya sudah kosong sebelum pemicu berhenti harus diulang.

**Status:** NOT VERIFIED

**Angka sesudah berhenti dan 0.5 detik kemudian / bukti:** ______________________________

### TC-MAT002-09 - Receiver yang menolak Sand tidak menghilangkan unit

**Setup:** Bucket Sand 12; Reject Receiver kosong dan hanya menerima Cement. Pastikan trigger Sand Receiver tidak ikut overlap.

**Langkah:**

1. Grab dan bawa bucket tegak masuk trigger Reject Receiver.
2. Tahan `R` selama 1 detik, lepaskan, lalu catat kedua jumlah.

**Expected:** Bucket tetap Sand 12; Reject Receiver tetap kosong. Percobaan yang ditolak tidak mengganti tipe material atau menghilangkan unit. Pesan penolakan/HUD baru belum menjadi syarat tiket ini.

**Status:** NOT VERIFIED

**Bucket sebelum/sesudah / receiver sebelum/sesudah / bukti:** ______________________________

### TC-MAT002-10 - Receiver penuh membatasi transfer

**Setup:** Sebelum Play, isi Sand Receiver 98/Capacity 100; bucket Sand 12.

**Langkah:**

1. Grab bucket tegak, masuk trigger Sand Receiver, tahan `R` selama 1 detik, lalu lepaskan.
2. Catat angka setelah beberapa physics tick. Tahan `R` lagi selama 1 detik dan catat ulang.

**Expected:** Receiver berhenti di 100 dan bucket tersisa 10: total awal/akhir 110. Percobaan lanjutan tidak mengubah jumlah, tidak overflow, dan tidak error. Pulihkan receiver kosong sebelum kasus lain.

**Status:** NOT VERIFIED

**Angka awal/akhir / bukti:** ______________________________

### TC-MAT002-11 - Kontrol tegak dan aturan tumpah saat miring

**Setup:** Bucket tidak dipegang, jauh dari receiver, isi 12 sebelum setiap percobaan. Untuk mengisolasi sudut, pada salinan scene set Use Gravity off dan Constraints Freeze All, dengan Is Kinematic tetap off. Pulihkan semua nilai setelah kasus ini.

**Langkah:**

1. Rotation bucket (0,0,0); Play dan tunggu 1 detik. Catat jumlah.
2. Stop. Atur Rotation (0,0,90), isi 12 lagi; Play dan amati 0.5 detik pertama hingga 2 detik.

**Expected:** Bucket tegak tetap 12. Bucket yang dipertahankan pada sudut 90 derajat melewati batas 70 derajat dan kehilangan unit bertahap, lalu mencapai 0 tanpa negatif/error. Dengan laju baseline 10 unit/detik, pengosongan kira-kira 1.2 detik waktu simulasi. Tidak ada pile baru dari tumpahan pada implementasi sekarang. Tes sudut terkontrol ini belum membuktikan handling jatuh dinamis.

**Status:** NOT VERIFIED

**Sudut / jumlah / waktu simulasi / bukti:** ______________________________

### TC-MAT002-12 - Drop dalam keadaan miring

**Setup:** Bucket kembali dinamis: Use Gravity on, Is Kinematic off, Constraints None, isi 12. Jauh dari semua receiver. Pilih lantai lapang.

**Langkah:**

1. Grab bucket, lalu tundukkan pandangan hingga bucket jelas hampir berada pada sisinya. Periksa orientasi melalui Scene view bila perlu; target kemiringan lebih dari 70 derajat.
2. Drop dengan `E`. Amati orientasi saat jatuh dan setelah mengenai lantai, serta perubahan Contents selama 1-2 detik.
3. Jika drop cepat mengembalikan bucket tegak sebelum sempat mengamati spill, ulangi di lokasi lain. Sebagai kontrol tambahan, tempatkan bucket dinamis pada sisinya (Rotation Z 90) di atas lantai lewat Edit Mode dan Play ulang.

**Expected:** Ketika bucket hasil drop berada melewati batas tumpah cukup lama, isi berkurang. Setelah kembali tegak, kehilangan karena kemiringan berhenti; jumlah tidak negatif. Bucket tidak menembus lantai atau terus jitter. Tidak setiap drop tegak harus menumpahkan pasir. Jika hasil drop tidak menghasilkan kemiringan yang cukup lama, tandai aspek spill dari drop **NOT VERIFIED**, meskipun TC-11 lulus. Kontrol yang ditempatkan lewat Editor tidak menggantikan bukti drop pemain.

**Status:** NOT VERIFIED

**Orientasi/drop / jumlah sebelum-sesudah / durasi / rekaman:** ______________________________

### TC-MAT002-13 - Throw miring setelah INT-004 tersedia

**Dependency:** Kontrol throw INT-004 belum ada pada implementasi PlayerGrab yang diperiksa di branch ini. Binding klik kiri saja tidak membuktikan aksi throw sudah tersedia.

**Langkah:**

1. Jika build yang diuji belum punya implementasi throw, tandai **BLOCKED** dan catat dependency; jangan menandai PASS.
2. Setelah tersedia, reset bucket 12, grab, arahkan agar bucket miring, dan lempar dengan klik kiri di area lapang jauh dari receiver.
3. Amati collision, orientasi, serta jumlah sebelum/sesudah lemparan.

**Expected setelah dependency tersedia:** Bucket benar-benar terlempar melalui sistem interaction. Ketika melewati batas kemiringan, unit berkurang sesuai MAT-005; tidak negatif/error. Implementasi mekanik throw sendiri berada pada INT-004, sedangkan penggunaan bucket dan aturan tumpah tetap perlu diperiksa.

**Status:** NOT VERIFIED

**Build/dependency / jumlah / rekaman:** ______________________________

### TC-MAT002-14 - Animasi bucket miring dan pasir jatuh

**Setup:** Bucket 12; Sand Receiver kosong. Pakai Game view dengan kamera/pandangan mendekati horizontal agar rotasi utama bucket tidak memicu spill yang terpisah.

**Langkah:**

1. Grab bucket dengan E, dekati receiver, lalu tahan R selama 0.5 detik. Rekam model bucket, bibir bucket, dan aliran pasir.
2. Lepas R. Amati model kembali tegak dan butiran yang sudah keluar selesai jatuh dalam sekitar 0.3 detik.
3. Reset lalu tuang sampai kosong. Pastikan efek berhenti. Ulangi dengan sisa 1 unit awal: unit terakhir juga memberi feedback yang terlihat.
4. Reset lalu coba receiver Cement, receiver yang sudah penuh, dan R jauh dari semua receiver. Periksa tidak ada pose tuang atau emisi baru saat transfer tidak terjadi.
5. Reset lalu drop dengan E saat sedang tuang. Model kembali ke pose dasar dan tidak terus mengeluarkan partikel baru. Bedakan feedback tuang aktif dari kehilangan isi karena drop miring.

**Expected:** Model bucket miring sekitar 65 derajat saat transfer dan butiran kuning/cokelat terlihat dari bibir bucket menuju receiver. Tidak ada efek tuang palsu untuk transfer yang ditolak, penuh sejak awal, atau di luar jangkauan. Setelah unit terakhir, feedback boleh berakhir dengan ekor singkat (sekitar 0.2 detik emisi dan 0.3 detik umur butiran). Jumlah bucket + receiver tetap kekal. Pose animasi ada pada child Pour Model; rotasi utama Rigidbody tidak ditambah oleh animasi. Efek ini belum menampilkan tumpahan pasif dari bucket yang jatuh atau membuat pile tanah.

**Status:** NOT VERIFIED

**Rekaman / keterbacaan butiran / angka sebelum-sesudah:** ______________________________

### TC-MAT002-15 - Dua trigger overlap: target terdekat, pergantian, dan pembatalan

**Setup:** Pada salinan dev scene, duplikat receiver melalui Unity Editor menjadi receiver A dan B yang menerima Sand, kosong, dengan capacity 100 dan spill rate 0. Letakkan root keduanya pada ketinggian yang sama. Perbesar child Pour Trigger seperlunya agar seluruh collider bucket tetap di dalam kedua trigger selama percobaan. Jauhkan trigger lain. Reset bucket Sand 12 dan receiver kosong sebelum setiap percobaan; jaga root bucket tegak.

**Langkah:**

1. Grab bucket dengan E, masuk area overlap, dan tempatkan root bucket lebih dekat ke root A daripada B. Catat posisi root dan jumlah awal. Tahan R sebentar, lalu lepaskan dan Pause untuk membaca angka.
2. Reset. Tahan R sambil bergeser di dalam area overlap sampai root bucket lebih dekat ke B. Lepaskan R dan Pause; bandingkan jumlah sebelum/sesudah pergantian. Jangan habiskan sumber sebelum berpindah.
3. Reset. Untuk jarak sama persis, grab bucket dan berhenti bergerak, lalu Pause. Catat posisi root bucket `(x,y,z)`. Atur root A ke `(x-0.75,y-0.75,z)` dan B ke `(x+0.75,y-0.75,z)` lewat Inspector saat Pause. Resume tanpa menggerakkan kamera/player, tahan R, lalu Pause untuk memeriksa target dan jumlah. Pastikan posisi aktual masih menghasilkan jarak kuadrat minimum yang **persis sama**. Jika posisi bucket bergeser, catat aspek pembatalan jarak sama sebagai NOT VERIFIED. Ulangi dengan receiver C yang lebih jauh tetapi trigger-nya ikut overlap.
4. Reset. Buat receiver yang paling dekat penuh sejak awal, sementara yang lebih jauh kosong dan menerima Sand. Tahan R selama 1 detik. Ulangi dengan receiver terdekat hanya menerima Cement, sedangkan yang lebih jauh menerima Sand.
5. Reset. Mulai transfer ke receiver terdekat, lalu keluar dari seluruh trigger sambil R tetap ditahan. Lepas R dan catat angka sesudah beberapa physics tick, lalu 0.5 detik kemudian.

**Expected:** A menerima unit pada langkah 1 dan B tetap kosong. Pada langkah 2 permintaan berikutnya memilih B, A berhenti bertambah, dan feedback mengikuti B setelah transfer positif. Jumlah bucket + semua receiver tetap kekal. Pada langkah 3 tidak ada transfer atau feedback tuang baru, termasuk ketika C tersedia; butiran lama boleh menyelesaikan umur hidupnya. Pada langkah 4 bucket tidak berkurang dan receiver yang lebih jauh tetap kosong: tidak ada fallback karena kapasitas atau tipe. Pada langkah 5 transfer berhenti dan model kembali netral. Pemilihan memakai jarak root container, bukan jarak collider, arah pandang, atau urutan registrasi. Pembatalan hanya berlaku pada jarak kuadrat minimum yang persis sama.

**Status:** NOT VERIFIED

**Posisi/jarak root / jumlah A-B-C sebelum-sesudah / target dan feedback / rekaman:** ______________________________

## 5. Ringkasan hasil

| Kasus | Status awal | Angka/observasi dan referensi bukti |
| --- | --- | --- |
| TC-01 Prefab, tuning, binding | NOT VERIFIED | |
| TC-02 Bentuk dan collision pile | NOT VERIFIED | |
| TC-03 Pile mengecil/kosong/terisi lagi | NOT VERIFIED | |
| TC-04 Rongga bucket | NOT VERIFIED | |
| TC-05 Grab/carry/drop | NOT VERIFIED | |
| TC-06 Syarat tuang | NOT VERIFIED | |
| TC-07 Tuang R dan konservasi | NOT VERIFIED | |
| TC-08 Berhenti tuang | NOT VERIFIED | |
| TC-09 Penolakan Sand | NOT VERIFIED | |
| TC-10 Receiver penuh | NOT VERIFIED | |
| TC-11 Tegak vs sudut miring | NOT VERIFIED | |
| TC-12 Drop miring | NOT VERIFIED | |
| TC-13 Throw miring, bersyarat | NOT VERIFIED | |
| TC-14 Animasi miring dan pasir jatuh | NOT VERIFIED | |
| TC-15 Receiver overlap, target, dan pembatalan | NOT VERIFIED | |

### Pemetaan acceptance criteria #16

| Acceptance criterion | Kasus |
| --- | --- |
| Pile memakai MAT-005, Sand, jumlah dapat dituning, visual mound, tanpa Rigidbody | TC-01, TC-02, TC-03 |
| Bucket container satu tipe, kapasitas tunable, generic grab | TC-01, TC-04, TC-05 |
| Tuang saat dipegang dekat receiver; penolakan tanpa unit hilang | TC-06, TC-07, TC-08, TC-09, TC-10, TC-15 |
| Bucket jatuh/lempar miring kehilangan isi via MAT-005 | TC-11, TC-12; TC-13 setelah dependency tersedia |
| Binding tuang dicatat dan tidak bentrok | TC-01, TC-05, TC-07; runtime throw setelah tersedia |

**Catatan feel kartunis:** Apakah jumlah pasir mudah dibaca? Apakah bucket mudah diambil, dibawa, dan dituang? Apakah spill terjadi karena kemiringan yang bisa dipahami pemain? Catat kejadian lucu dan kejadian menjengkelkan secara terpisah. Realisme butiran, bekas sekop, squash animation, dan kelengkapan art belum menjadi syarat PASS.

**Setelah tes:** Stop Play, pulihkan setup sementara melalui Unity Editor, simpan bukti dan hasil. Jangan menerapkan konfigurasi kontrol kinematic/frozen atau Sphere tes ke prefab asli. Catat FAIL/BLOCKED/NOT VERIFIED untuk reviewer; hasil automated suite tidak menggantikan gameplay review.

## 6. Bukti otomatis yang sudah tersedia

Run pada 2026-10-04 sekitar 04:04 WIB menghasilkan **22/22 Passed**, process exit code 0, pada `Logs/Mat002PrefabPlayModeResults.xml`. Run tambahan setelah animasi dipasang, sekitar 15:32 WIB, menghasilkan **25/25 Passed**, process exit code 0, pada `Logs/BucketPourAnimationResults.xml`. Tiga tes tambahan memeriksa pose/emisi/cancel dan konservasi jumlah, transfer yang ditolak/penuh/kosong, serta feedback unit terakhir. Render dari tes tersedia di `Logs/BucketPourAnimation-Pouring.png` dan `Logs/BucketPourAnimation-Stopped.png`; butiran saat tuang dan pose netral setelah berhenti sudah diperiksa secara visual. **Tes tersebut memicu adapter melalui API, bukan keyboard R. Semua hasil manual pada checklist ini tetap NOT VERIFIED sampai kamu menjalankannya.**

Run perbaikan review PR #86 pada 2026-10-04 **18:49:13-18:49:50 WIB** memakai Unity **6000.3.25f1**, graphics Direct3D 12 aktif, dan seluruh `Ngecor.Material.Tests`: **32/32 Passed**, process exit code **0**, tanpa failed, skipped, atau inconclusive. Hasil aktual ada di `Logs/Mat002ReviewFixFinalResults.xml`; log di `Logs/Mat002ReviewFixFinalTests.log`; exit code di `Logs/Mat002ReviewFixFinalExitCode.txt`. Tujuh regression test tambahan mencakup capture dua PNG ke folder unik yang semula tidak ada, urutan registrasi receiver, jarak minimum sama persis termasuk receiver ketiga, tanpa fallback dari receiver penuh/menolak Sand, pergantian target setelah bucket bergerak, unregister/cancel, dan referensi null/destroyed/bucket sendiri. Jarak yang hampir sama tetapi berbeda juga diperiksa. PNG tuang/berhenti pada path di atas dibuat ulang dan diperiksa secara visual. Folder unik milik regression capture sudah dibersihkan. **TC-MAT002-15 dan input keyboard/manual feel tetap NOT VERIFIED; hasil otomatis tidak menggantikan review gameplay.**

## Sumber

- [MAT-002 #16](https://github.com/GhazaGG/Ngecor/issues/16), [MAT-005 #62](https://github.com/GhazaGG/Ngecor/issues/62), [MAT-004 #54](https://github.com/GhazaGG/Ngecor/issues/54).
- [Keputusan proyek](DECISIONS.md), [alur kerja/review](WORKFLOW.md), [struktur folder](PROJECT_STRUCTURE.md), [arah gameplay](GAME_DESIGN.md).
- Riwayat lokal: `2026-10-04-00-50-mat-002-sand-bucket-finalization.md` dan `2026-10-04-04-05-correct-sand-pile-volume-and-verify-prefab-physics.md` di `.development-history/`.
