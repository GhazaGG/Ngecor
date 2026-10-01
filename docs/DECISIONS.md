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
| Multiplayer prototype | Host sebagai sumber kebenaran; client mengirim niat | Workflow tim |
| Integrasi kerja | Branch + PR, review teknis dan gameplay, `main` playable | Workflow tim |
| Versi Unity | **6000.3.25f1** (Unity 6.3 LTS), dikunci oleh `ProjectSettings/ProjectVersion.txt`; tanpa module tambahan | Keputusan pemilik proyek, 2026-10-01 (SETUP-001) |
| Performa | Game harus sangat ringan; target dan anggaran di [Anggaran performa](#anggaran-performa) | Keputusan pemilik proyek, 2026-10-01 |
| Render pipeline | URP (Universal Render Pipeline) | Keputusan pemilik proyek, 2026-10-01 |
| Lokasi project Unity | Root repo: `Assets/`, `Packages/`, `ProjectSettings/` sejajar dengan `docs/` | Keputusan pemilik proyek, 2026-10-01 |
| Struktur proyek nyata | Scene awal `Assets/Game/Scenes/Playground.unity` (satu-satunya scene di Build Settings); URP, volume profile, dan input actions di `Assets/Game/Settings/` | SETUP-001 |

Baris di atas adalah arah produk, **bukan bukti fitur sudah ada atau semua rinciannya sudah dipilih**. Ubah arah melalui diskusi tim dan catat keputusan baru di bawah.

## Perlu dipastikan sebelum task terkait

| Topik | Kapan harus jelas | Yang perlu dicatat |
| --- | --- | --- |
| Target platform dan bentuk gameplay awal | Sebelum menetapkan build/input/camera | OS/platform prototype, perspektif, dan perangkat input yang diuji |
| Input system | Sebelum implementasi kontrol M1 | Baseline sudah membawa `com.unity.inputsystem` 1.20.0 dari template, dengan Active Input Handling = Input System Package. Yang perlu diputuskan: tetap memakai ini atau tidak, dan binding minimum |
| Networking package/transport | Sebelum sistem jaringan M3; lebih awal jika M1/M2 bergantung padanya | Package/versi, model host, dan batas ownership objek fisika |
| Git ignore, LFS, dan file Unity | Dalam SETUP-002/003 sebelum banyak asset masuk | Pola ignore, pola LFS, batas ukuran bila ada, Visible Meta Files, Force Text, dan cara memverifikasinya |

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

## Anggaran performa

Angka di bawah adalah titik awal. Ubah lewat PR setelah ada data Profiler, jangan diubah karena satu fitur sulit memenuhinya.

**Target:** 60 FPS stabil di 1080p dengan 4 pemain pada laptop GPU terintegrasi (setara Intel Iris Xe atau AMD Radeon Vega 8, RAM 8 GB). Sampai ada perangkat acuan, uji di laptop paling lemah di tim.

| Area | Aturan |
| --- | --- |
| Grafis | Satu directional light real-time dengan shadow jarak pendek; light lain baked atau tanpa shadow. Tanpa real-time GI, SSAO, motion blur, atau depth of field. Post-processing hanya tonemapping dan color grading ringan. MSAA maksimal 2x. |
| Art | Low-poly dan stylized. Texture maksimal 1024 px untuk objek besar, 512 px untuk props kecil. Pakai ulang material yang sama sebanyak mungkin. |
| Fisika | Collider primitif (box, sphere, capsule) untuk objek yang bergerak; mesh collider hanya untuk lingkungan statis. Rigidbody dibiarkan sleep saat diam. Material tidak disimulasikan per partikel (lihat `docs/GAME_DESIGN.md`). |
| Kode | Hindari `Find`, `GetComponent`, dan alokasi memori di `Update`/`FixedUpdate`; cache referensi saat `Awake`/`Start`. |
| Jaringan | Hanya kirim state yang dibutuhkan client. Rincian menunggu keputusan networking package. |

**Cara memeriksa:** fitur yang menambah banyak objek, efek, atau fisika menyertakan angka FPS dan screenshot Unity Profiler (CPU dan GPU) di PR. Playtest mingguan mencatat FPS di laptop paling lemah.
