# Instruksi AI untuk Ngecor

File ini berlaku untuk semua AI yang membantu empat developer Ngecor. Jika tool tidak membaca `AGENTS.md` otomatis, developer perlu menyertakannya dalam konteks task. Instruksi langsung dari developer dan acceptance criteria issue yang disetujui menentukan scope task; catat konflik dengan dokumen proyek sebelum mengubah desain.

**Jika AI tidak bisa membaca repo** (chat web/aplikasi tanpa akses file), developer menempelkan: isi file ini, isi issue, baris relevan dari `docs/DECISIONS.md`, dan file yang akan diubah. AI tidak boleh menebak isi file yang tidak ditempel; minta developer menempelkannya.

## Larangan keras

Aturan ini berlaku tanpa pengecualian kecuali developer dan issue menyatakan lain secara eksplisit.

1. Jangan memasang package, plugin, atau asset store apa pun (termasuk networking seperti Mirror, Photon, Fish-Net, NGO) yang belum tercatat di `docs/DECISIONS.md`.
2. Jangan memilih versi Unity, render pipeline, input system, atau networking. Jika belum tercatat di `docs/DECISIONS.md`, berhenti dan tanyakan.
3. Pindah, rename, dan hapus asset hanya lewat Unity Editor (jendela Project). Jangan memakai File Explorer, terminal, atau `git mv` untuk file di `Assets/`; GUID di `.meta` bisa putus dan referensi hilang tanpa error.
4. Jangan mengedit `.unity`, `.prefab`, `.asset`, atau `.meta` dengan teks editor. Ubah lewat Unity Editor.
5. Jangan membuat folder atau menaruh file di luar `docs/PROJECT_STRUCTURE.md`. Jika tidak ada tempat yang cocok, tanyakan.
6. Jangan mengubah scene utama kecuali issue menyebutnya. Uji fitur di dev scene sendiri (lihat `docs/WORKFLOW.md`).
7. Jangan push ke `main`, force push, `git reset --hard`, atau menghapus branch orang lain.
8. Jangan membuat manager/singleton global, event bus, atau framework umum kecuali issue membutuhkannya.
9. Jangan menulis kode multiplayer yang membuat client menentukan state dunia (posisi objek fisika, hasil grab, jumlah material). Client hanya mengirim niat. Satu-satunya pengecualian: movement dan arah pandang player milik client itu sendiri.
10. Jangan menyatakan "sudah dites" atau mencentang checklist jika belum dijalankan di Unity. Tulis apa yang belum dites.
11. Jangan menulis kode networking (`NetworkBehaviour`, `NetworkObject`, RPC, `NetworkVariable`, atau API NGO lain) di luar tiket `NET-*`, atau tiket yang secara eksplisit memintanya. Fitur lain tetap offline; tiket NET-lah yang menyambungkannya ke jaringan.
12. Jangan meng-commit plan, spec, atau checklist buatan tool AI (misalnya `docs/superpowers/`, `docs/<TIKET>-plan.md`). Simpan di luar repo. Catatan kerja ditulis di `.development-history/`; desain yang perlu disetujui diposting sebagai komentar di issue.

## Baca sebelum bekerja

1. Baca `README.md`, bagian yang relevan dari `docs/GAME_DESIGN.md`, `docs/WORKFLOW.md`, `docs/PROJECT_STRUCTURE.md`, dan `docs/DECISIONS.md`.
2. Baca issue yang dikerjakan, file terkait, serta laporan relevan di `.development-history/`.
3. Periksa branch dan working tree. Jangan menghapus, me-reset, atau menimpa pekerjaan orang lain.
4. Jika `.codegraph/` ada di root repo, gunakan CodeGraph sebelum pencarian teks untuk memahami atau mencari kode. Jangan membuat index sendiri.
5. Telusuri alur yang akan diubah sebelum memilih implementasi. Pakai komponen dan pola yang sudah ada; buat perubahan sekecil mungkin yang memenuhi acceptance criteria.

## Status dan sumber kebenaran

- Repo masih pada tahap setup. Jangan mengklaim scene, prefab, paket, test, atau fitur gameplay sudah ada tanpa memeriksa repo dan Unity.
- `docs/GAME_DESIGN.md` adalah arah desain saat ini. Contoh alat, kontrak, dan kejadian kacau adalah bahan desain, bukan perintah untuk mengimplementasikan semuanya sekaligus.
- `docs/DECISIONS.md` memisahkan keputusan yang sudah disepakati dari hal yang belum diputuskan. Jangan memilih versi Unity, render pipeline, input system, networking package, atau platform secara diam-diam.
- Issue menentukan outcome dan batas task; `docs/PROJECT_MANAGEMENT.md` menentukan status board; `docs/WORKFLOW.md` menentukan cara kerja tim dan review.
- Bila dokumen, issue, dan implementasi tidak cocok, laporkan perbedaannya. Jangan mengubah keputusan produk di luar task.

## Aturan implementasi

- Bekerja di branch task sendiri. Jangan push langsung ke `main` dan jangan merge PR tanpa review tim.
- Utamakan satu perubahan yang bisa dimainkan dan diuji. Hindari framework, abstraksi, dependency, atau konten yang belum dibutuhkan task.
- Game harus sangat ringan. Ikuti [Anggaran performa](docs/DECISIONS.md#anggaran-performa); jangan menambah efek grafis, texture besar, atau objek fisika berlebihan di luar anggaran tanpa persetujuan.
- Terapkan prinsip fisik di `docs/GAME_DESIGN.md`: material harus benar-benar berada dan dipindahkan di dunia; kapasitas mengikuti ruang dan muatan fisik. Jangan menambahkan magic inventory atau transfer material tanpa pengangkutan. Biarkan solusi improvisasi bekerja jika keadaan fisik memungkinkan.
- Untuk setiap alat atau upgrade, jelaskan masalah yang diatasi dan risiko baru yang berasal dari perubahan tersebut. Risiko harus memiliki pemicu yang masuk akal serta cara dicegah atau dipulihkan pemain; jangan memaksakan bencana acak setiap kali upgrade dipakai.
- Jaga scene tetap sebagai tempat merakit prefab. Koordinasikan perubahan scene bersama; gunakan scene uji terpisah jika cocok.
- Saat proyek Unity tersedia, pertahankan pasangan asset dan `.meta`-nya. Jangan menghapus atau membuat ulang `.meta` yang sudah dipakai, dan jangan mengedit file Unity hasil serialisasi secara spekulatif.
- Jangan mengubah aturan Git/LFS, pengaturan Unity bersama, package, atau sistem multiplayer lintas tim tanpa issue dan kesepakatan teknis.
- Untuk multiplayer, ikuti keputusan prototype **host sebagai sumber kebenaran**. Client mengirim niat; host memvalidasi dan menentukan state. Package (NGO + Unity Transport) dan batas authority tercatat di [Networking dan batas authority](docs/DECISIONS.md#2026-10-01--networking-dan-batas-authority).
- Hindari random failure yang merampas agency pemain. Kegagalan gameplay sebaiknya menghasilkan masalah yang bisa dipulihkan, sesuai desain game.

## Verifikasi dan serah terima

- Uji acceptance criteria di Unity saat proyek tersedia. Untuk perubahan gameplay, sertakan langkah reproduksi, scene, hasil Play Mode, dan observasi feel.
- Untuk perubahan multiplayer, uji setidaknya host dan client sesuai scope. Jangan menyatakan lulus multiplayer dari tes offline saja.
- Jika tidak bisa menjalankan Unity, jelaskan apa yang diperiksa dan apa yang belum terverifikasi. Jangan mencentang checklist PR berdasarkan dugaan.
- PR mengikuti `.github/PULL_REQUEST_TEMPLATE.md`; sertakan issue, sistem yang terdampak, hasil test, known issues, dan media singkat bila membantu review gameplay.
- Review teknis dan gameplay diperlukan sebelum task gameplay dinyatakan selesai. Ikuti Definition of Done di `docs/WORKFLOW.md`.

## Development history

Gunakan `.development-history/` sebagai catatan proyek. Sebelum mengubah apa pun, baca laporan yang relevan dan periksa kembali klaim lama terhadap keadaan repo saat ini. Setelah task selesai, buat laporan baru bernama `YYYY-MM-DD-HH-mm-judul-singkat.md` memakai waktu lokal proyek; jangan menimpa laporan lama.

Tulis laporan dalam bahasa Inggris dan cantumkan: task summary, relevant previous context, changes made, files affected, technical decisions, verification performed, final result, known limitations, serta unresolved issues or follow-up work. Catat hanya tindakan yang benar-benar dilakukan; jangan masukkan rahasia. Jika hanya menyelidiki tanpa mengubah file, catat temuan yang bermakna. Selesaikan urutan **review history → inspect current state → implement → verify → create report → final response**.
