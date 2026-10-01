# Ngecor

**Yang Penting Jadi** adalah konsep game konstruksi kooperatif untuk 1–4 pemain. Pemain mengangkut material secara fisik, memakai alat yang tidak selalu dapat diandalkan, dan menyelesaikan proyek melalui kerja sama serta improvisasi. Target awalnya adalah prototype yang menyenangkan, bukan simulasi konstruksi lengkap.

## Status proyek

Repo ini masih dalam tahap **M0: project setup**. Keberadaan dokumen desain tidak berarti fitur tersebut sudah dibuat. Versi Unity, paket, platform target, dan pilihan teknis lain yang belum ditetapkan tercatat di [Keputusan Proyek](docs/DECISIONS.md). Jangan menebaknya saat membuat task atau kode.

## Mulai dari sini

1. Baca [Desain Game](docs/GAME_DESIGN.md) untuk arah produk dan target vertical slice.
2. Baca [Alur Kerja](docs/WORKFLOW.md) untuk branch, review, playtest, dan aturan file Unity.
3. Baca [Struktur Folder dan Penamaan](docs/PROJECT_STRUCTURE.md) sebelum menambah file ke project Unity.
4. Baca [Manajemen Proyek](docs/PROJECT_MANAGEMENT.md) untuk board, prioritas, milestone, dan kesiapan ticket.
5. Sebelum memulai setup Unity, selesaikan keputusan yang relevan di [Keputusan Proyek](docs/DECISIONS.md).
6. Jika memakai AI, berikan [AGENTS.md](AGENTS.md) sebagai instruksi awal ketika tool tidak membacanya otomatis.

Pekerjaan setup dilacak di issue [SETUP-001](https://github.com/GhazaGG/Ngecor/issues/34), [SETUP-002](https://github.com/GhazaGG/Ngecor/issues/35), dan [SETUP-003](https://github.com/GhazaGG/Ngecor/issues/36). Ambil task dari issue yang sudah siap, kerjakan di branch sendiri, dan ajukan PR. Jangan push langsung ke `main`.

## Setup lokal

1. Install Unity **6000.3.25f1** lewat Unity Hub, tanpa module tambahan. Versi lain tidak boleh dipakai (lihat [Keputusan Proyek](docs/DECISIONS.md)).
2. Cek instalasinya: folder `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Data\UnityReferenceAssemblies` harus ada. Hub bisa melaporkan "installed" walaupun installer gagal di tengah jalan. Kalau folder itu tidak ada, uninstall, kosongkan disk (minimal sekitar 20 GB), lalu install ulang memakai installer manual dari [Unity Download Archive](https://unity.com/releases/editor/archive) dengan Run as administrator.
3. Clone repo, lalu di Unity Hub pilih **Add → Add project from disk** dan arahkan ke folder root repo.
4. Buka `Assets/Game/Scenes/Playground.unity`, lalu tekan Play.

## Prinsip prototype

**Works → Fun → Reliable → Clean → Pretty.** Bangun satu interaksi yang bisa dimainkan dan diuji sebelum menambah konten, sistem progression, atau polish. `main` harus tetap playable setelah proyek Unity dibuat.
