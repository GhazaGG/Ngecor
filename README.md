# Ngecor

**Yang Penting Jadi** adalah konsep game konstruksi kooperatif untuk 1–4 pemain. Pemain mengangkut material secara fisik, memakai alat yang tidak selalu dapat diandalkan, dan menyelesaikan proyek melalui kerja sama serta improvisasi. Target awalnya adalah prototype yang menyenangkan, bukan simulasi konstruksi lengkap.

## Status proyek

Repo ini masih dalam tahap **M0: project setup**. Keberadaan dokumen desain tidak berarti fitur tersebut sudah dibuat. Versi Unity, paket, platform target, dan pilihan teknis lain yang belum ditetapkan tercatat di [Keputusan Proyek](docs/DECISIONS.md). Jangan menebaknya saat membuat task atau kode.

## Mulai dari sini

1. Baca [Desain Game](docs/GAME_DESIGN.md) untuk arah produk dan target vertical slice.
2. Baca [Alur Kerja](docs/WORKFLOW.md) untuk branch, review, playtest, dan aturan file Unity.
3. Baca [Manajemen Proyek](docs/PROJECT_MANAGEMENT.md) untuk board, prioritas, milestone, dan kesiapan ticket.
4. Sebelum memulai setup Unity, selesaikan keputusan yang relevan di [Keputusan Proyek](docs/DECISIONS.md).
5. Jika memakai AI, berikan [AGENTS.md](AGENTS.md) sebagai instruksi awal ketika tool tidak membacanya otomatis.

Pekerjaan setup dilacak di issue [SETUP-001](https://github.com/GhazaGG/Ngecor/issues/34), [SETUP-002](https://github.com/GhazaGG/Ngecor/issues/35), dan [SETUP-003](https://github.com/GhazaGG/Ngecor/issues/36). Ambil task dari issue yang sudah siap, kerjakan di branch sendiri, dan ajukan PR. Jangan push langsung ke `main`.

## Prinsip prototype

**Works → Fun → Reliable → Clean → Pretty.** Bangun satu interaksi yang bisa dimainkan dan diuji sebelum menambah konten, sistem progression, atau polish. `main` harus tetap playable setelah proyek Unity dibuat.
