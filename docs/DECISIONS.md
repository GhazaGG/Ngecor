# Keputusan Proyek

Tujuan dokumen ini adalah mencegah AI atau anggota tim mengisi celah desain dengan asumsi. **Tercatat** berarti arah yang ada dalam brief dan workflow yang diberikan tim; **belum tercatat di repo** berarti perlu konfirmasi dari issue atau keputusan tim sebelum implementasi terkait.

## Arah yang sudah tercatat

| Topik | Arah saat ini | Sumber |
| --- | --- | --- |
| Genre dan pemain | Game konstruksi kooperatif untuk 1–4 pemain | Brief konsep |
| Gameplay utama | Logistik fisik, solusi improvisasi, alat dengan manfaat dan risiko, kegagalan yang bisa dipulihkan | Brief konsep |
| Prinsip inti 1 | Material, kapasitas angkut, dan solusi improvisasi mengikuti keadaan fisik dunia; tanpa magic inventory | Klarifikasi pemilik proyek, 2026-10-01 |
| Prinsip inti 2 | Setiap alat atau upgrade membawa tantangan baru yang sesuai dengan kemampuannya; risiko muncul melalui sebab-akibat dan dapat dikelola pemain | Klarifikasi pemilik proyek, 2026-10-01 |
| Struktur jangka panjang | Workshop permanen dan map kontrak khusus; Rumah Pak Ujang sebagai target vertical slice | Brief konsep |
| Multiplayer prototype | Host sebagai sumber kebenaran untuk objek fisika, hasil grab, dan jumlah material; movement player ditentukan client pemiliknya. Lihat [batas authority](#2026-10-01--networking-dan-batas-authority) | Workflow tim; keputusan pemilik proyek, 2026-10-01 |
| Integrasi kerja | Branch + PR, review teknis dan gameplay, `main` playable | Workflow tim |
| Versi Unity | **6000.3.25f1** (Unity 6.3 LTS), dikunci oleh `ProjectSettings/ProjectVersion.txt`; tanpa module tambahan | Keputusan pemilik proyek, 2026-10-01 (SETUP-001) |
| Performa | Game harus sangat ringan; target dan anggaran di [Anggaran performa](#anggaran-performa) | Keputusan pemilik proyek, 2026-10-01 |
| Render pipeline | URP (Universal Render Pipeline) | Keputusan pemilik proyek, 2026-10-01 |
| Lokasi project Unity | Root repo: `Assets/`, `Packages/`, `ProjectSettings/` sejajar dengan `docs/` | Keputusan pemilik proyek, 2026-10-01 |
| Git ignore dan LFS | Pola di `.gitignore` dan `.gitattributes` (gambar, model, audio, video, font, `.dll`/`.so`, `.zip` lewat LFS; YAML Unity sebagai teks LF). Visible Meta Files dan Force Text aktif | SETUP-002/003 |
| Platform, input, kamera | Windows PC, online co-op satu PC per pemain, keyboard + mouse, Input System package, first-person | Keputusan pemilik proyek, 2026-10-01 |
| Networking | Netcode for GameObjects 2.x (`com.unity.netcode.gameobjects: 2.13.3`) + Unity Transport (`com.unity.transport: 2.7.4`), mode host | Keputusan pemilik proyek, 2026-10-01; dipasang & dikunci NET-001 (#20) |
| Struktur proyek nyata | Scene awal `Assets/Game/Scenes/Playground.unity` (satu-satunya scene di Build Settings); URP, volume profile, dan input actions di `Assets/Game/Settings/` | SETUP-001 |
| Material bulk | Tipe + jumlah unit integer di container dunia; transfer hanya lewat tuang/sekop fisik; tumpahan menjadi pile; aduk manual dasar, mixer upgrade. Lihat [kontrak material bulk](#2026-10-02--kontrak-material-bulk) | Keputusan pemilik proyek, 2026-10-02 |

Baris di atas adalah arah produk, **bukan bukti fitur sudah ada atau semua rinciannya sudah dipilih**. Ubah arah melalui diskusi tim dan catat keputusan baru di bawah.

## Perlu dipastikan sebelum task terkait

| Topik | Kapan harus jelas | Yang perlu dicatat |
| --- | --- | --- |
| Relay/Lobby | Saat tim perlu join lewat internet tanpa VPN | Layanan yang dipakai dan batas biaya |
| Binding input minimum | PLAYER-001 (#7) | Tombol untuk move, look, interact, throw |

Jika sebuah topik ternyata sudah diputuskan dalam issue atau rapat, **catat keputusan dan sumbernya di sini**. Jangan menebak nilai dari contoh dokumen atau kebiasaan pribadi. Untuk hal yang tidak menghalangi task sekarang, biarkan tetap terbuka.

## Catatan keputusan

Tambahkan entri singkat ketika tim memilih sesuatu:

```text
YYYY-MM-DD — Topik
Decision: pilihan yang disetujui
Reason: alasan utama dan tradeoff
Applies from: issue/PR atau milestone
Owner/source: orang atau link keputusan
```

### 2026-10-01 — Dua prinsip inti gameplay

**Decision:** Tegaskan gameplay sepenuhnya fisik dan peningkatan yang membawa tantangan baru sebagai dua prinsip inti. Rincian dan contoh ada di [Desain Game](GAME_DESIGN.md).

**Reason:** Kapasitas, muatan, dan solusi harus mengikuti keadaan dunia; peningkatan perlu memperkenalkan konsekuensi yang sesuai agar pemain tetap berkoordinasi dan berimprovisasi.

**Applies from:** Desain dan review fitur material, transportasi, alat, serta upgrade berikutnya.

**Owner/source:** Klarifikasi langsung pemilik proyek dalam percakapan 2026-10-01. Contoh gerobak, ganjal batu, dan pengunci bak adalah ilustrasi, bukan tambahan scope implementasi.

### 2026-10-01 — Unity 6

**Decision:** Proyek memakai Unity 6 (`6000.x`). Semua developer memakai versi patch yang sama persis.

**Reason:** Pilihan pemilik proyek. Versi patch belum dipilih; pembuat baseline di SETUP-001 memilih satu versi Unity 6 lalu mencatatnya di sini dan di `ProjectSettings/ProjectVersion.txt`.

**Applies from:** SETUP-001 (#34).

**Owner/source:** Keputusan pemilik proyek dalam percakapan 2026-10-01.

### 2026-10-01 — Game sangat ringan dan URP

**Decision:** Game harus berjalan lancar di laptop kelas bawah. Render pipeline memakai URP.

**Reason:** Pemain dan developer memakai laptop yang berbeda-beda, dan co-op 4 pemain dengan banyak objek fisika sudah cukup membebani CPU. URP lebih ringan dan skalabel dibanding HDRP. Built-in Render Pipeline tidak dipakai karena Unity mengarahkan proyek baru ke URP.

**Applies from:** SETUP-001 (#34) dan semua task berikutnya.

**Owner/source:** Keputusan pemilik proyek dalam percakapan 2026-10-01.

### 2026-10-01 — Versi Unity 6000.3.25f1

**Decision:** Semua developer memakai Unity **6000.3.25f1** (6.3 LTS). Module tambahan tidak diperlukan; build Windows memakai backend Mono bawaan.

**Reason:** LTS memberi dukungan dan perbaikan bug paling lama untuk tim. Dipilih pemilik proyek saat SETUP-001.

**Applies from:** SETUP-001 (#34).

**Owner/source:** Keputusan pemilik proyek dalam percakapan 2026-10-01.

### 2026-10-01 — Platform, input, dan kamera

**Decision:**
- Platform prototype: Windows PC. Online co-op, satu PC per pemain. Tanpa split-screen atau couch co-op.
- Input: Input System package yang sudah ada di baseline (`com.unity.inputsystem` 1.20.0, Active Input Handling = Input System Package). Keyboard + mouse menjadi satu-satunya input yang wajib dites. Gamepad masuk backlog.
- Input hanya dibaca oleh player milik lokal, bukan oleh semua instance player di scene.
- Kamera: first-person.

**Reason:** Split-screen 4 kamera melanggar [Anggaran performa](#anggaran-performa) di GPU terintegrasi. Input System sudah terpasang dan aktif. Gamepad menggandakan testing dan tuning setiap PR M1. First-person memberi grab berbasis arah pandang yang paling presisi, tidak butuh rig animasi untuk pemain lokal, tidak menambah package (tanpa Cinemachine), dan cocok dengan acceptance criteria PLAYER-002.

**Revisit:** Jika gameplay review VEH-002 (#14) menunjukkan mendorong gerobak atau menyetir terasa canggung di first-person, pertimbangkan kamera third-person **khusus** saat mendorong atau menyetir, bukan mengganti perspektif seluruh game. Gamepad dibuka kembali setelah core M1–M2 terasa menyenangkan.

**Applies from:** PLAYER-001 (#7), PLAYER-002 (#8), dan semua task interaction M1.

**Owner/source:** Keputusan pemilik proyek dalam percakapan 2026-10-01.

### 2026-10-02 — Binding input minimum M1

**Decision:** Gunakan binding Input System yang sudah ada: Move = WASD dan tombol panah; Look = mouse delta; Interact = E; Throw = tombol kiri mouse (binding saat ini pada action `Attack`). Gamepad tidak wajib dan tetap di backlog. Issue PLAYER-001 mengimplementasikan Move dan Look; Interact dan Throw dicatat untuk task berikutnya.

**Reason:** Menetapkan kontrol dasar sesuai technical notes issue PLAYER-001 tanpa menambah package atau mengubah action asset bersama.

**Applies from:** PLAYER-001 (#7), PLAYER-002 (#8), dan task interaction M1.

**Owner/source:** Pemilik task PLAYER-001, mengikuti issue #7 dan keputusan M1 di atas.

### 2026-10-04 — Usulan binding tuang bucket

**Proposal:** Tambahkan action `Pour` dengan binding tahan tombol `R` saat membawa bucket. Input ini hanya meminta container bucket menuang ketika berada di trigger receiver. Interact tetap `E`, sedangkan Throw tetap tombol kiri mouse pada action `Attack`.

**Reason:** Tuang perlu aksi eksplisit dan tidak boleh berjalan hanya karena bucket menyentuh receiver. `R` tidak bentrok dengan binding minimum M1 yang sudah tercatat.

**Status:** Usulan MAT-002 (#16), menunggu review PR dan playtest feel.

**Source:** Handoff MAT-002 dari Team Lead, 2026-10-04.

### 2026-10-04 — Feedback tuang bucket sederhana

**Decision:** MAT-002 mendapat feedback tuang sederhana sesuai permintaan pemilik proyek: child model bucket miring dan butiran pasir terlihat menuju receiver saat transfer berlangsung. Efek berhenti saat intent dibatalkan, dan selesai dengan ekor singkat setelah transfer terakhir.

**Batas:** Jumlah tetap dikelola `BulkMaterialContainer`. Pose animasi tidak menambah rotasi root Rigidbody supaya aturan tumpah tidak mengurangi unit dua kali. Partikel hanya visual, maksimal 32 aktif per bucket, tanpa Rigidbody atau collision per butir. Tuang bebas dan pile tanah tetap pada MAT-004 (#54); animasi gundukan membal dan VFX final belum termasuk.

**Source:** Permintaan pemilik proyek dalam percakapan 2026-10-04: "bisa kamu execute pembuatan animasinya? sederhana dulu juga gapapa. asal ada keliatan pasir jatuh saat bucket miring".

### 2026-10-06 — Shovel dan pile material yang bisa dipulihkan

**Decision:** MAT-004 (#54) memakai action `Player/Pour` (`R`) yang sudah ada. Satu tekan saat shovel kosong meminta scoop; satu tekan saat berisi meminta dump. Menahan `R` tidak mengulangi aksi shovel. Bucket tetap menuang selama `R` ditahan, termasuk ke tanah ketika tidak ada receiver. `E` tetap grab/drop dan klik kiri tetap throw.

**Batas:** Shovel berkapasitas awal 2 unit, jarak pencarian di depan blade 1 meter, dan radius merge pile 0,5 meter; semuanya dapat dituning. Target terdekat yang penuh atau menolak tipe tidak dialihkan. Exact tie membatalkan aksi, termasuk fallback ke tanah. Tipe shovel hanya berubah ketika kosong. Deposit membutuhkan permukaan statis; jika deposit gagal, unit tetap di sumber. Pile runtime bertipe sama hanya digabung pada permukaan dan ketinggian lantai yang sama, tanpa batas kapasitas gameplay tambahan. Jumlah integer tetap dibatasi representasi `int`.

**Source:** Approved MAT-004 implementation plan from the project owner, 2026-10-06. Freshness, mixing, terrain digging, final VFX, and networking remain with their respective tickets.

### 2026-10-02 — Player mendorong objek fisika lewat kontak

**Decision:**
- Player yang berjalan menabrak Rigidbody dinamis mendorongnya. Perilaku ini generik untuk semua objek fisika, tidak khusus gerobak, dan berada di player movement.
- Objek statis dan Rigidbody kinematic tidak didorong. Objek yang sedang diinjak player tidak didorong.
- Kekuatan dorong bergantung pada arah gerak yang diinginkan player dan massa objek, sehingga objek berat terasa lebih sulit didorong.
- Mendorong lewat kontak bukan pengganti aksi push/pull yang disengaja. Memegang handle gerobak tetap dikerjakan di VEH-002 (#14).

**Reason:** Identitas game adalah kekacauan fisik yang muncul dari keputusan pemain; prop yang terasa seperti tembok bertentangan dengan itu. Cara ini juga membuat VEH-001 (#13) bisa dimainkan tanpa menunggu rantai interaction.

**Konsekuensi multiplayer:** Mengikuti [batas authority](#2026-10-01--networking-dan-batas-authority), objek fisika milik host bersifat kinematic di client, sehingga dorongan lokal dari client tidak berpengaruh. NET-003 (#22) harus membuat player client tetap bisa mendorong, misalnya dengan mengirim niat dorong ke host. Jangan membiarkan client menentukan posisi objek.

**Revisit:** Jika playtest menunjukkan dorongan kontak merusak tumpukan cargo atau susunan material terlalu mudah, pertimbangkan batas massa atau mengecualikan objek tertentu.

**Applies from:** VEH-001 (#13).

**Owner/source:** Keputusan pemilik proyek saat review PR #53, 2026-10-02.

### 2026-10-01 — Networking dan batas authority

**Decision:**
- Package: Netcode for GameObjects (NGO) 2.x (`com.unity.netcode.gameobjects: 2.13.3`) + Unity Transport (`com.unity.transport: 2.7.4`), mode host (salah satu pemain menjadi host). Versi verified di Package Manager untuk 6000.3.25f1, dipasang dan dikunci oleh NET-001 (#20).
- Koneksi M3: LAN atau direct IP. Developer di lokasi berbeda boleh memakai VPN mesh (mis. Tailscale/ZeroTier) di luar repo. Relay/Lobby (Unity Gaming Services) ditunda sampai tim perlu join lewat internet tanpa VPN.
- Authority:
  - **Host menentukan:** posisi dan state objek fisika, hasil grab/drop/throw, jumlah dan keadaan material, state proyek.
  - **Client pemilik menentukan:** movement dan arah pandang player-nya sendiri.
  - Client mengirim niat untuk semua aksi terhadap dunia ("mau grab X"); host memvalidasi lalu mereplikasi hasilnya.
- Kode interaction M1 memisahkan niat dari eksekusinya, supaya nanti bisa dirutekan lewat host tanpa ditulis ulang. Tidak perlu framework atau event bus untuk ini.

**Reason:** NGO adalah package first-party untuk Unity 6.3, gratis untuk koneksi langsung, dan Multiplayer Play Mode memungkinkan tes host + 3 client di satu laptop tanpa build. Ini penting untuk tim dengan laptop lemah. Fish-Net dan Photon Fusion 2 punya physics prediction, tetapi lebih kompleks; Fusion juga terikat CCU dan vendor. Movement milik client mencegah delay sebesar ping pada setiap langkah; objek fisika tetap di host agar state dunia konsisten.

**Revisit:** Setelah tes latency di NET-003 (#22). Jika benda yang dipegang client terasa tertinggal sebesar RTT, pertimbangkan simulasi benda yang dipegang oleh client pemegang dengan host tetap memvalidasi. Revisit juga jika NGO tanpa prediction membuat physics client tidak bisa dimainkan. Risiko yang sudah diketahui dari split authority: player milik client yang berdiri di atas atau mendorong benda yang disimulasikan host (bak pickup, scaffolding yang goyang, gerobak) bisa jitter atau desync. Uji kasus ini secara eksplisit di NET-003 sebelum memutuskan bertahan.

**Applies from:** INT-002 (#10) untuk pemisahan niat/eksekusi; NET-001 (#20) untuk install.

**Owner/source:** Keputusan pemilik proyek dalam percakapan 2026-10-01.

### 2026-10-02 — Kontrak material bulk

**Decision:**
- Material bulk (sand, cement, concrete) disimpan sebagai **tipe + jumlah unit integer** di container yang ada di dunia. Player tidak pernah menyimpan jumlah material.
- **Container** punya kapasitas, satu tipe material, dan visual level isi. Bucket, shovel, pile, spot aduk, mixer, dan construction target memakai **satu komponen container yang sama** (dibuat di MAT-005 #62; MAT-002 #16 adalah pemakai pertamanya untuk sand pile dan bucket). Spot aduk dan mixer adalah container multi-bahan.
- **Transfer hanya lewat aksi fisik:** tuang dari container yang dipegang atau scoop/dump dengan shovel, di dekat receiver. Unit berpindah dengan laju yang dapat dituning dan selalu kekal (keluar = masuk). Receiver menentukan tipe yang diterimanya.
- **Tumpah:** container yang miring melewati sudut yang dapat dituning kehilangan isi per detik. Isi yang tumpah menjadi **pile** di tanah (container tanpa Rigidbody) dan digabung ke pile tipe sama dalam radius yang dapat dituning. Pile bisa disekop lagi.
- **Sak semen** tetap benda diskret dan berubah menjadi N unit cement saat masuk spot aduk atau mixer.
- **Aduk manual dengan shovel adalah cara dasar membuat concrete** (MIX-003 #55). Mixer (MIX-001 #17) adalah upgrade di M4 yang memakai resep yang sama.
- Resep MVP: cement + sand + water (nilai di [aduk manual MIX-003](#2026-10-08--nilai-awal-aduk-manual-mix-003)). Kerikil ditambah sebagai tipe baru setelah playtest logistik, tanpa sistem baru.
- **Concrete** membawa freshness (MIX-002 #45) sejak jadi. Saat dituang freshness ikut pindah; batch tercampur memakai umur tertua. Concrete keras hanya bisa dibuang dengan mengosongkan container.
- Sand source adalah pile terbatas. Wheelbarrow tidak menjadi container bulk di MVP; ia membawa bucket dan sak.
- Multiplayer nanti: tipe, jumlah, dan freshness adalah state container milik host; tuang, scoop, dump, dan aduk adalah niat.

**Reason:** Empat tiket dengan pemilik berbeda (#16, #19, #45, #55) memindahkan jumlah material. Satu kontrak mencegah beberapa sistem quantity yang tidak cocok. Integer membuat jumlah kekal dan mudah dites. Pile dan shovel membuat tumpahan menjadi pekerjaan tambahan yang bisa dipulihkan. Aduk manual sebagai dasar membuat mixer menjadi alat yang menyelesaikan masalah (tenaga dan waktu) sekaligus membawa risiko baru (macet, overheat).

**Revisit:** Wheelbarrow sebagai container bulk (mengangkut concrete langsung di bak) setelah playtest logistik M2. Jumlah pile di scene jika Profiler menunjukkan beban.

**Applies from:** MAT-005 (#62), MAT-002 (#16), MAT-004 (#54), MIX-003 (#55), BUILD-002 (#19), MIX-002 (#45), MIX-001 (#17).

**Owner/source:** Keputusan pemilik proyek dalam sesi diskusi desain 2026-10-02.

### 2026-10-08 — Nilai awal aduk manual MIX-003

**Decision:** Aduk manual mengikuti urutan kerja di lapangan: (1) pasir dan semen dibawa ke spot (satu sak semen penuh menjadi 25 unit cement, 1 unit = 1 kg); (2) aduk kering 5 aksi untuk meratakan pasir dan semen; (3) air dituang setelah aduk kering selesai; (4) aduk basah 5 aksi per batch. Satu batch memakai 1 cement + 2 sand + 1 water dan menghasilkan 4 unit concrete. Concrete tetap berada di spot dan diambil langsung dengan shovel atau bucket; bahan mentah tidak bisa disekop keluar dari spot. Air hanya diterima setelah aduk kering selesai (air yang ditolak tetap di bucket), dan pasir atau semen yang ditambah setelah aduk kering harus diaduk kering lagi. Air berasal dari drum air berisi terbatas (200 unit) yang diambil dengan bucket. Di spot aduk, shovel kosong memakai `R` dengan dua cara: tekan singkat (kurang dari 0,25 detik) mengambil concrete, tahan `R` mengaduk (satu aksi saat tahan dimulai, lalu satu tiap 0,5 detik). Ini pengecualian aturan MAT-004 "menahan R tidak mengulangi aksi shovel", hanya saat membidik spot aduk; di tempat lain scoop/dump tetap langsung saat ditekan. Semua angka dapat dituning di Inspector. Mixer memakai resep yang sama. Ini nilai awal prototype yang dapat disesuaikan berdasarkan playtest. Air (dan kerikil) sebelumnya Out of Scope di MIX-003 (#55); air dimasukkan atas permintaan pemilik proyek, kerikil belum.

**Owner/source:** Disetujui pemilik proyek setelah review PR A MIX-003 dan usulan default dari RyoFPS, 2026-10-08. Urutan dengan air dan concrete di spot: permintaan pemilik proyek, 2026-10-09.

### 2026-10-09 — Ekonomi bahan cor Rumah Pak Ujang

**Decision:**

| Item | Nilai | Hitungan |
| --- | --- | --- |
| Target cor lantai 2 | **160** unit concrete | 8 bucket penuh (bucket 20) |
| Batch | 40 | 160 / 4 concrete per batch (1 cement + 2 sand + 1 water) |
| Kebutuhan | 40 cement, 80 sand, 40 water | 40 batch x resep |
| Sak semen di level | **3** (75 cement) | 2 sak cukup; 1 cadangan jika sak rusak atau hilang |
| Sand pile di level | **100** unit | Sisa 20 untuk tumpahan |
| Drum air dekat spot aduk | Isi awal **20** | Setengah kebutuhan; sisanya diangkut dari kran |
| Kran | Tanpa batas, di sisi lain site | Lihat MAT-006 (#100) |

- Hitungan bahan memakai `ConcreteRecipe` dan `ConcreteRecipeCalculator` (cement, sand, water). Aduk manual dan mixer memakai resep yang sama.
- Angka level dipasang di LEVEL-001 (#26) lewat Unity Editor; prefab `SandPile` (80) dan `WaterDrum` (200) tetap nilai uji dev scene.

**Reason:** Tiap bahan punya batas yang terasa tanpa membuat run buntu: pasir cukup dengan sedikit sisa, semen punya satu sak cadangan, dan air harus diangkut sehingga crew punya jalur angkut kedua. Dengan drum 200 di samping spot, air tidak pernah menjadi masalah.

**Revisit:** Setelah playtest MVP. Satu sak menghasilkan sampai 100 concrete, jadi semen hampir bukan kendala; tinjau rasio sak atau resep jika semen terasa tidak berarti.

**Applies from:** BUILD-002 (#19, target), LEVEL-001 (#26, pasokan), VEH-003 (#44, sak di bak), MAT-006 (#100, drum dan kran).

**Owner/source:** Usulan lead, disetujui pemilik proyek 2026-10-09.

### 2026-10-03 — Tenaga dorong player dan massa sak semen

**Decision:**
- Skala massa: 1 unit = 1 kg. Sak semen (sak penuh) bermassa **25 kg**, skala game dan bukan 40–50 kg asli.
- Dorongan kontak player dibatasi gaya maksimum **350 N** (`_maxPushForce` di `Player.prefab`), dapat dituning di Inspector. Nilai prototype ini disetujui pemilik proyek pada 2026-10-06 setelah uji PLAYER-003. Benda ringan (maksimum 5 kg) dibatasi agar tidak melampaui kecepatan jalan player, sementara sak 25 kg bergeser pelan selama player terus mendorong.
- Dorongan diterapkan setinggi pusat massa benda supaya kontak tinggi tidak menekan benda ke lantai.
- Sak semen tidak boleh berfungsi sebagai tembok saat ditabrak player. Friksi efektif sak terhadap lantai dijaga sekitar 0,6 sampai 0,75.
- Pekerjaan dilakukan di PLAYER-003 (#73). Massa dan friksi sak ada di MAT-001 (#15).

**Reason:** Gaya tetap sekitar 25 N tidak bisa menggeser benda yang lebih berat dari sekitar 4 kg di lantai berfriksi normal, sehingga sak 25 kg menjadi tembok. Menaikkan gaya dorong global membuat prop ringan melesat (PR #67: Strength 20 membuat `Box_8` terlalu brutal); batas gaya manusia menangani keduanya tanpa mengubah setting physics global. Uji PLAYER-003 di Unity 6000.3.25f1 pada head 17cfd79 menunjukkan 300 N gagal pada orientasi sak rebah, sedangkan 350 N adalah nilai terendah yang meluluskan 15/15 orientasi (rebah, sisi, sudut-sisi), dengan kecepatan puncak 2.058 m/s di bawah target 2.3 m/s. Regresi benda 1 kg dan suite PlayMode penuh lulus 74/74. Friksi sak tidak diturunkan karena itu berisiko membuat tumpukan sak, kargo gerobak, dan beban scaffolding mudah tergelincir.

**Revisit:** Setelah gameplay review PLAYER-003 pada wheelbarrow kosong di Ramp_Gentle dan wheelbarrow dengan tepat tiga cargo di tanah datar; pastikan muatan tetap stabil. Pertimbangkan perlambatan player saat mendorong benda berat jika hasil playtest memerlukannya.

**Applies from:** PLAYER-003 (#73), MAT-001 (#15), MAT-003 (#46), VEH-002 (#14).

**Owner/source:** Keputusan awal dari Team Lead (didelegasikan oleh pemilik proyek) saat review draft PLAYER-003, 2026-10-03. Pemilik proyek menyetujui nilai 350 N saat review PR #85 berdasarkan hasil uji pada 2026-10-06.

### 2026-10-03 — Norma tes otomatis

**Decision:** Tes otomatis wajib hanya untuk **logika murni yang kritis dan mudah rusak diam-diam**, yaitu perhitungan yang dipisah dari scene (misalnya perhitungan gaya dorong, aturan transfer container). Feel, physics, dan interaksi dites manual di Play Mode dan dilaporkan di PR. Tiket yang membutuhkan tes otomatis menuliskannya di Acceptance Criteria.

**Reason:** Developer memakai AI yang lebih lemah, yang sering membuat regresi logika tanpa disadari. Tes untuk fungsi murni murah dan stabil, sedangkan tes otomatis untuk feel fisika mahal dan rapuh. `com.unity.test-framework` sudah ada di baseline.

**Applies from:** PLAYER-003 (#73) dan tiket logika murni berikutnya.

**Owner/source:** Keputusan Team Lead (didelegasikan oleh pemilik proyek), 2026-10-03.

### 2026-10-04 — Tali masuk MVP sebagai penambat

**Decision:**
- Tali (rope) termasuk peralatan awal vertical slice Rumah Pak Ujang, dikerjakan di TOOL-001 (#81).
- Bentuk MVP adalah **penambat**: tali mengikat dua titik ikat (dua benda fisika, atau satu benda dan satu pos tetap) dan membatasi jarak maksimum keduanya. Saat longgar tidak memberi gaya. Digambar sebagai garis, bukan simulasi tali berantai.
- Mengikat dan melepas memakai tombol Interact yang sudah ada, tanpa binding baru.
- Katrol, mengangkat beban dengan tali, memutus tali, jaring muatan, menyelamatkan player, dan menarik crane **tidak** masuk MVP.
- Prioritas P2 di M5: tali tidak wajib untuk menyelesaikan satu contract dan tidak memblokir LEVEL-001. Core loop didahulukan.

**Reason:** Tali adalah alat improvisasi di konsep game dan disebut sebagai peralatan awal slice. Penambat jarak maksimum memenuhi fungsi paling berguna (menahan pickup atau gerobak yang menggelinding, menarik benda) dengan satu joint per tali, jauh lebih murah dan lebih stabil daripada tali berantai di PhysX. Alat ini membawa risiko baru yang sesuai prinsip inti: benda yang diikat saling menarik, sehingga pickup yang mundur bisa menyeret gerobak atau merobohkan scaffolding, dan semuanya bisa diperbaiki dengan melepas ikatan.

**Revisit:** Setelah playtest M5. Katrol untuk logistik vertikal adalah kandidat berikutnya jika tali terbukti menyenangkan. Jika joint dengan rasio massa besar (pickup dan sak) terus tidak stabil, pertimbangkan batas massa pada titik ikat.

**Applies from:** TOOL-001 (#81).

**Owner/source:** Keputusan pemilik proyek, 2026-10-04 ("masuk MVP"). Lingkup MVP ditentukan Team Lead.

### 2026-10-06 — Aturan tabrakan objek yang dibawa player (INT-005)

**Decision:**
1. **Terhadap dunia:** Objek yang dibawa tidak boleh menembus dinding, lantai, atau ramp. `ResolveHoldPosition` menarik objek mundur ke permukaan rintangan. Jika player terus bergerak maju menempel rintangan sehingga ruang antara player dan rintangan lebih sempit dari batas minimum objek (< 0.25 m dari center player atau terhimpit tanpa jalan keluar), objek mengalami auto-drop lembut (*soft failure*).
2. **Terhadap prop dinamis:** Objek yang dibawa memperlakukan prop dinamis lain sebagai rintangan tabrakan (tertahan di permukaannya tanpa penetrasi). Objek yang dibawa tidak memberikan gaya kinematic paksa ke prop dinamis. Dorongan fisik prop tetap dilakukan lewat kontak badan player (`CharacterController.OnControllerColliderHit`) sesuai keputusan 2026-10-02 dan PLAYER-003 (#73).
3. **Terhadap player & kamera:** Jarak terdekat objek ke player dibatasi di luar kapsul player. Rintangan dunia diprioritaskan di atas near-clip kamera: objek tidak dipaksa maju menembus dinding demi menghindari kamera near-clip. Jika objek terhimpit dan menutupi layar, batas ruang fisik memicu auto-drop agar tidak tersangkut atau menembus dinding.
4. **Saat drop:** Pendekatan "abaikan tabrakan sampai terpisah" (*ignore collision until separated*) menggantikan seluruh heuristik pergeseran menyamping (`DepenetrateOnDrop`). Saat dilepas, `Physics.IgnoreCollision` antara player dan objek dipertahankan sampai collider keduanya tidak lagi tumpang-tindih (overlap), lalu tabrakan dipulihkan otomatis. Ini menjamin objek tidak pernah terlontar akibat solver PhysX.
5. **Massa/Berat:** Membawa objek berat memperlambat pergerakan jalan player secara linier proporsional ($v_{\text{eff}} = v_{\text{base}} \times \text{clamp}(1 - \text{mass} \times 0.012, 0.6, 1.0)$). Sak semen 25 kg memperlambat player sekitar 30%, sementara objek ringan (1 kg) praktis tidak terasa perlambatannya (~1%). Jarak hold point tetap konsisten agar kontrol interaksi tetap terprediksi.
6. **Authority multiplayer:** Mengikuti batas authority 2026-10-01, objek yang dibawa disimulasikan di host. Client mengirim niat (`RequestGrab`, `RequestDrop`, `RequestThrow`); host memvalidasi dan menjalankan penempatan hold position serta pelacakan separasi tabrakan di host. Client tidak pernah menentukan posisi objek di dunia.

**Reason:** Memenuhi dua prinsip inti (sepenuhnya fisik dan kegagalan yang bisa dipulihkan). Klem kamera-prioritas sebelumnya menyebabkan objek menembus dinding saat player menempel rapat. Heuristik drop sebelumnya (~130 baris) rapuh dan rentan regresi, sedangkan *ignore-until-separated* adalah solusi native PhysX yang sederhana, bebas alokasi GC, dan matematis mencegah lontaran. Skala penalti massa memberi rasa fisik pada sak semen 25 kg tanpa mengubah tuning kontrol gerak dasar.

**Applies from:** INT-005 (#76), MAT-003 (#46), VEH-002 (#14).

**Owner/source:** Usulan meryzennn di PR #89; disetujui pemilik proyek pada 2026-10-06.

### 2026-10-08 — Feedback interaksi: outline tepian dan crosshair plus tunggal (INT-006)

**Decision:**
1. **Crosshair tunggal berbentuk tanda tambah (+):**
   - Crosshair menggunakan uGUI dengan dua bar persegi panjang bersilangan (horizontal 12×2 px, vertikal 2×12 px) berwarna putih (`Color(1, 1, 1, 0.9)`), permanen di tengah layar `(0, 0)`.
   - Menggantikan debug OnGUI. Field `_showDebugFeedback` di `InteractionDetector` dan `PlayerGrab` disetel ke `false` secara default di script dan prefab untuk menghilangkan bug tanda ganda (dua crosshair + dan lingkaran/kotak bertumpuk).
2. **Highlight tepian / edge-only silhouette (bukan solid color):**
   - Target interaksi yang sedang difokuskan kamera disorot dengan outline siluet putih tipis pada tepiannya saja (*inverted-hull pass* `Ngecor/OutlineEdge`), diekstrusi sepanjang normal vertex sebesar 0.02 m (dapat disetel dinamis via property `OutlineWidth` / `_outlineWidth` pada `InteractionHighlighter`).
   - Vertex normal pada sudut-sudut dan tepian tajam (misal: mesh Cube) dilas/dihaluskan (*smoothed normals*) pada mesh outline cache agar tidak terbelah atau bolong (*tearing/gapping*) pada ujung sudut.
   - Interior objek dan tekstur asli (albedo, normal map, roughness) 100% tetap terlihat apa adanya; tidak ada penimpaan warna kuning solid ke seluruh badan objek dan tidak ada kloning material runtime.
   - Digambar via `Graphics.DrawMesh` pada `MeshFilter` target selama `LateUpdate`.
3. **Prompt teks terpusat di bawah crosshair:**
   - Teks prompt interaksi (`CurrentTarget.InteractionPrompt`, misal: "Grab") ditampilkan tepat di bawah crosshair pada posisi `(0, -35)`.
   - Menggunakan *dirty-tracking* string sehingga zero allocation GC pada update steady-state.
   - Begitu objek diambil/digrab (`IsHeld == true`), target hilang dari pandangan, atau target hancur, outline dan prompt teks langsung hilang seketika.
4. **Authority & performa:**
   - Feedback visual sepenuhnya bersifat lokal (client-side) pada kamera pemain aktif tanpa overhead sinkronisasi jaringan. Alokasi GC steady-state 0 B/frame.
5. **Konvensi objek interaktif:**
   - Komponen `IInteractable` (seperti `InteractableObject` atau `GrabbableObject`) ditempatkan di root GameObject yang memiliki collider solid (non-trigger).
   - Raycast `InteractionDetector` menggunakan `QueryTriggerInteraction.Ignore` sehingga collider berstatus *trigger* tidak terdeteksi sebagai target interaksi.

**Reason:** Memenuhi arahan issue #77 dan masukan visual pemilik proyek: pemain membutuhkan bidikan presisi tanda tambah (+) tunggal tanpa interferensi debug GUI, serta feedback visual tepian objek yang bersih tanpa menutupi rupa fisik material asli objek.

**Applies from:** INT-006 (#77), PLAYER-003 (#73), MAT-001 (#15).

**Owner/source:** Arahan dan feedback visual pemilik proyek pada issue #77 / PR #95; diusulkan dan diimplementasikan oleh meryzennn pada 2026-10-08.

## Anggaran performa

Angka di bawah adalah titik awal. Ubah lewat PR setelah ada data Profiler, jangan diubah karena satu fitur sulit memenuhinya.

**Target:** 60 FPS stabil di 1080p dengan 4 pemain pada laptop GPU terintegrasi (setara Intel Iris Xe atau AMD Radeon Vega 8, RAM 8 GB). Sampai ada perangkat acuan, uji di laptop paling lemah di tim.

| Area | Aturan |
| --- | --- |
| Grafis | Satu directional light real-time dengan shadow jarak pendek; light lain baked atau tanpa shadow. Tanpa real-time GI, SSAO, motion blur, atau depth of field. Post-processing hanya tonemapping dan color grading ringan. MSAA maksimal 2x. |
| Art | Low-poly dan stylized. Texture maksimal 1024 px untuk objek besar, 512 px untuk props kecil. Pakai ulang material yang sama sebanyak mungkin. |
| Fisika | Collider primitif (box, sphere, capsule) untuk objek yang bergerak; mesh collider hanya untuk lingkungan statis. Rigidbody dibiarkan sleep saat diam. Material tidak disimulasikan per partikel (lihat `docs/GAME_DESIGN.md`). |
| Kode | Hindari `Find`, `GetComponent`, dan alokasi memori di `Update`/`FixedUpdate`; cache referensi saat `Awake`/`Start`. |
| Jaringan | Hanya kirim state yang dibutuhkan client. Package dan authority: lihat [Networking dan batas authority](#2026-10-01--networking-dan-batas-authority). |

**Cara memeriksa:** fitur yang menambah banyak objek, efek, atau fisika menyertakan angka FPS dan screenshot Unity Profiler (CPU dan GPU) di PR. Playtest mingguan mencatat FPS di laptop paling lemah.
