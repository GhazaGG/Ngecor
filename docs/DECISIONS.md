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

Baris di atas adalah arah produk, **bukan bukti fitur sudah ada atau semua rinciannya sudah dipilih**. Ubah arah melalui diskusi tim dan catat keputusan baru di bawah.

## Perlu dipastikan sebelum task terkait

| Topik | Kapan harus jelas | Yang perlu dicatat |
| --- | --- | --- |
| Versi Unity dan modules | Sebelum baseline Unity dibagikan | Versi editor persis, cara install, dan file proyek yang menguncinya |
| Target platform dan bentuk gameplay awal | Sebelum menetapkan build/input/camera | OS/platform prototype, perspektif, dan perangkat input yang diuji |
| Render pipeline | Saat membuat baseline Unity | Pilihan pipeline dan alasan singkat |
| Input system | Sebelum implementasi kontrol M1 | Package/pendekatan dan binding minimum |
| Networking package/transport | Sebelum sistem jaringan M3; lebih awal jika M1/M2 bergantung padanya | Package/versi, model host, dan batas ownership objek fisika |
| Git ignore, LFS, dan file Unity | Dalam SETUP-002/003 sebelum banyak asset masuk | Pola ignore, pola LFS, batas ukuran bila ada, Visible Meta Files, Force Text, dan cara memverifikasinya |
| Struktur proyek nyata | Saat SETUP-001 selesai | Lokasi project Unity, scene bootstrap, dan folder yang benar-benar dibuat |

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
