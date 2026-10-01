# Alur Kerja Pengembangan

Panduan ini merangkum `01-development-workflow.md` untuk dipakai langsung di repo. Empat developer bekerja pada salinan lokal masing-masing. Tidak ada live collaborative editing pada project utama. Semua perubahan masuk lewat branch dan Pull Request (PR); `main` harus tetap playable setelah baseline Unity tersedia.

## Dari ticket ke `main`

**Issue siap → assign → branch → kerja lokal → uji di Unity → commit/push → PR → review teknis → review gameplay → merge → playtest tim.**

- Gunakan board, prioritas, dan kriteria `Ready` di [Manajemen Proyek](PROJECT_MANAGEMENT.md). Satu orang idealnya hanya punya satu task utama aktif.
- Branch dari `main` yang mutakhir jika working tree aman: `feat/player-grab`, `feat/wheelbarrow`, `fix/player-stuck`, atau `refactor/interaction-system`.
- Commit kecil dengan satu tujuan yang jelas, misalnya `feat: add basic player grab`. Jangan push langsung ke `main`.
- PR memakai [template repo](../.github/PULL_REQUEST_TEMPLATE.md). Hubungkan issue, jelaskan perubahan, langkah uji yang benar-benar dilakukan, hasilnya, sistem terdampak, dan known issues.
- Satu reviewer minimal harus mencoba perubahan gameplay di Unity. Review kode saja tidak membuktikan feel, jitter, atau interaksi fisika.
- Jangan merge saat ada game-breaking bug yang diketahui, console error baru, acceptance criteria yang belum terpenuhi, atau tes relevan yang belum dilakukan tanpa keputusan reviewer.

## Berbagi project Unity tanpa konflik

- Semua orang memakai versi Unity yang sama; catat versi tepatnya di `docs/DECISIONS.md` dan file proyek Unity ketika baseline dibuat.
- Pisahkan pekerjaan menurut prefab atau sistem. Scene utama merakit prefab, bukan tempat menyimpan semua logic. Ownership per task dicatat di issue/PR; jangan menetapkan satu orang permanen untuk seluruh sistem.
- Gunakan scene uji di `Assets/Game/Scenes/Dev/` bila perlu agar fitur dapat dicoba tanpa mengubah scene utama.
- Koordinasikan sebelum dua orang mengubah scene, prefab inti, `ProjectSettings`, `Packages`, atau pengaturan Git/LFS yang sama. Jika terjadi konflik serialisasi Unity, minta pemilik perubahan meninjau hasil merge di Editor dan Play Mode.
- Saat project tersedia, asset Unity dan file `.meta` pasangannya harus ikut version control. Jangan commit cache atau hasil build lokal. Pola final `.gitignore` dan `.gitattributes` dikerjakan oleh issue setup, bukan ditebak dari dokumen ini.
- Struktur awal yang diusulkan adalah `Assets/Game/{Art,Audio,Materials,Prefabs,Scenes,Scripts,Settings}`. Tambahkan subfolder saat ada asset nyata; jangan membuat hierarki kosong untuk rencana masa depan.

## Review dan Definition of Done

**Review teknis:** perubahan terfokus, tidak merusak sistem lain, dependency jelas, console bersih, dan mudah dijelaskan. **Review gameplay:** reviewer memainkan alur, mencatat kemudahan kontrol, feel, jitter/fisika, serta apakah interaksi menghasilkan pengalaman yang dituju.

Task selesai setelah acceptance criteria terpenuhi, tidak ada game-breaking bug atau console error baru yang diketahui, developer dan minimal satu reviewer sudah menguji, PR approved dan merge ke `main`. Fitur multiplayer juga harus diuji bersama host dan client sesuai scope. Target feel adalah cukup baik untuk milestone saat ini, bukan sempurna.

AI dan developer harus menyebutkan tes yang **tidak** dilakukan secara eksplisit. Jangan menandai checklist lulus berdasarkan pembacaan kode saja.

## Multiplayer dan playtest

Multiplayer tidak ditunda sampai akhir. Urutan pembuktian: movement offline → interaction offline → physics toy → 2-player networking → network interaction/physics → 4-player test. Untuk prototype, **host adalah sumber kebenaran**: client mengirim niat, host memvalidasi dan menentukan state yang direplikasi. Pilihan package belum tercatat di repo; lihat [Keputusan Proyek](DECISIONS.md).

Adakan playtest tim minimal sekali seminggu. Mainkan `main` tanpa berhenti untuk coding. Catat observasi terlebih dahulu, misalnya “gerobak terlalu stabil” atau “grab sering jitter”, lalu buat issue untuk keputusan/perbaikan. Jangan menjadikan debat solusi sebagai pengganti observasi.

## Urutan milestone

| Milestone | Hasil yang dicari |
| --- | --- |
| M0 Foundation | Project Unity, Git/LFS, struktur dasar, scene dasar, konvensi, dan perlindungan `main` |
| M1 Player Playground | Movement, camera, detection, grab, drop, throw |
| M2 Construction Toys | Wheelbarrow, bahan, mixer, scaffolding, pickup dalam scope issue bertahap |
| M3 Multiplayer Playground | Host/join, 2–4 pemain, movement, grab, dan fisika jaringan |
| M4 Core Loop | Contract → transport → mix → build → finish |
| M5 Vertical Slice | Rumah Pak Ujang yang playable selama sekitar 15–25 menit |

Setiap milestone adalah arah kerja, bukan izin untuk membuat seluruh daftar sekaligus. Pecah task besar menjadi unit yang bisa diuji dan direview.
