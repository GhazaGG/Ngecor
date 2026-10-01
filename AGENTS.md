# Instruksi AI untuk Ngecor

File ini berlaku untuk semua AI yang membantu empat developer Ngecor. Jika tool tidak membaca `AGENTS.md` otomatis, developer perlu menyertakannya dalam konteks task. Instruksi langsung dari developer dan acceptance criteria issue yang disetujui menentukan scope task; catat konflik dengan dokumen proyek sebelum mengubah desain.

## Baca sebelum bekerja

1. Baca `README.md`, bagian yang relevan dari `docs/GAME_DESIGN.md`, `docs/WORKFLOW.md`, dan `docs/DECISIONS.md`.
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
- Jaga scene tetap sebagai tempat merakit prefab. Koordinasikan perubahan scene bersama; gunakan scene uji terpisah jika cocok.
- Saat proyek Unity tersedia, pertahankan pasangan asset dan `.meta`-nya. Jangan menghapus atau membuat ulang `.meta` yang sudah dipakai, dan jangan mengedit file Unity hasil serialisasi secara spekulatif.
- Jangan mengubah aturan Git/LFS, pengaturan Unity bersama, package, atau sistem multiplayer lintas tim tanpa issue dan kesepakatan teknis.
- Untuk multiplayer, ikuti keputusan prototype **host sebagai sumber kebenaran**. Client mengirim niat; host memvalidasi dan menentukan state. Rincian package dan ownership tetap menunggu keputusan teknis.
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
