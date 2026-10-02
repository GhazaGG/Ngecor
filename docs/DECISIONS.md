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
| Networking | Netcode for GameObjects 2.x + Unity Transport, mode host | Keputusan pemilik proyek, 2026-10-01 |
| Struktur proyek nyata | Scene awal `Assets/Game/Scenes/Playground.unity` (satu-satunya scene di Build Settings); URP, volume profile, dan input actions di `Assets/Game/Settings/` | SETUP-001 |
| Material bulk | Tipe + jumlah unit integer di container dunia; transfer hanya lewat tuang/sekop fisik; tumpahan menjadi pile; aduk manual dasar, mixer upgrade. Lihat [kontrak material bulk](#2026-10-02--kontrak-material-bulk) | Keputusan pemilik proyek, 2026-10-02 |

Baris di atas adalah arah produk, **bukan bukti fitur sudah ada atau semua rinciannya sudah dipilih**. Ubah arah melalui diskusi tim dan catat keputusan baru di bawah.

## Perlu dipastikan sebelum task terkait

| Topik | Kapan harus jelas | Yang perlu dicatat |
| --- | --- | --- |
| Versi NGO dan Unity Transport | Saat install di NET-001 (#20) | Versi verified di Package Manager untuk 6000.3.25f1 |
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
- Package: Netcode for GameObjects (NGO) 2.x + Unity Transport, mode host (salah satu pemain menjadi host). Versi persis adalah versi verified di Package Manager untuk 6000.3.25f1, dipasang dan dicatat di sini oleh NET-001 (#20). Jangan memasangnya lebih awal.
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
- **Container** punya kapasitas, satu tipe material, dan visual level isi. Bucket, shovel, pile, spot aduk, mixer, dan construction target memakai **satu komponen container yang sama** (dibuat pertama di MAT-002 #16). Spot aduk dan mixer adalah container multi-bahan.
- **Transfer hanya lewat aksi fisik:** tuang dari container yang dipegang atau scoop/dump dengan shovel, di dekat receiver. Unit berpindah dengan laju yang dapat dituning dan selalu kekal (keluar = masuk). Receiver menentukan tipe yang diterimanya.
- **Tumpah:** container yang miring melewati sudut yang dapat dituning kehilangan isi per detik. Isi yang tumpah menjadi **pile** di tanah (container tanpa Rigidbody) dan digabung ke pile tipe sama dalam radius yang dapat dituning. Pile bisa disekop lagi.
- **Sak semen** tetap benda diskret dan berubah menjadi N unit cement saat masuk spot aduk atau mixer.
- **Aduk manual dengan shovel adalah cara dasar membuat concrete** (MIX-003 #55). Mixer (MIX-001 #17) adalah upgrade di M4 yang memakai resep yang sama.
- Resep MVP: cement + sand. Kerikil dan air ditambah sebagai tipe baru setelah playtest logistik, tanpa sistem baru.
- **Concrete** membawa freshness (MIX-002 #45) sejak jadi. Saat dituang freshness ikut pindah; batch tercampur memakai umur tertua. Concrete keras hanya bisa dibuang dengan mengosongkan container.
- Sand source adalah pile terbatas. Wheelbarrow tidak menjadi container bulk di MVP; ia membawa bucket dan sak.
- Multiplayer nanti: tipe, jumlah, dan freshness adalah state container milik host; tuang, scoop, dump, dan aduk adalah niat.

**Reason:** Empat tiket dengan pemilik berbeda (#16, #19, #45, #55) memindahkan jumlah material. Satu kontrak mencegah beberapa sistem quantity yang tidak cocok. Integer membuat jumlah kekal dan mudah dites. Pile dan shovel membuat tumpahan menjadi pekerjaan tambahan yang bisa dipulihkan. Aduk manual sebagai dasar membuat mixer menjadi alat yang menyelesaikan masalah (tenaga dan waktu) sekaligus membawa risiko baru (macet, overheat).

**Revisit:** Wheelbarrow sebagai container bulk (mengangkut concrete langsung di bak) setelah playtest logistik M2. Jumlah pile di scene jika Profiler menunjukkan beban.

**Applies from:** MAT-002 (#16), MAT-004 (#54), MIX-003 (#55), BUILD-002 (#19), MIX-002 (#45), MIX-001 (#17).

**Owner/source:** Keputusan pemilik proyek dalam sesi diskusi desain 2026-10-02.

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
