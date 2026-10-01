# Alur Kerja Pengembangan

Panduan ini merangkum `01-development-workflow.md` untuk dipakai langsung di repo. Empat developer bekerja pada salinan lokal masing-masing. Tidak ada live collaborative editing pada project utama. Semua perubahan masuk lewat branch dan Pull Request (PR); `main` harus tetap playable setelah baseline Unity tersedia.

## Dari ticket ke `main`

**Issue siap → assign → branch → kerja lokal → uji di Unity → commit/push → PR → review teknis → review gameplay → merge → playtest tim.**

- Gunakan board, prioritas, dan kriteria `Ready` di [Manajemen Proyek](PROJECT_MANAGEMENT.md). Satu orang idealnya hanya punya satu task utama aktif.
- Branch, commit, dan PR mengikuti [Langkah Git](#langkah-git). Jangan push langsung ke `main`.
- PR memakai [template repo](../.github/PULL_REQUEST_TEMPLATE.md). Hubungkan issue, jelaskan perubahan, langkah uji yang benar-benar dilakukan, hasilnya, sistem terdampak, dan known issues.
- Satu reviewer minimal harus mencoba perubahan gameplay di Unity. Review kode saja tidak membuktikan feel, jitter, atau interaksi fisika.
- Jangan merge saat ada game-breaking bug yang diketahui, console error baru, acceptance criteria yang belum terpenuhi, atau tes relevan yang belum dilakukan tanpa keputusan reviewer.

## Langkah Git

`main` diproteksi: perubahan hanya masuk lewat PR dengan minimal 1 approval dari anggota lain, dan force push ke `main` ditolak.

### Nama branch

Format: `<type>/<deskripsi-singkat>`, huruf kecil, kata dipisah `-`.

| Type | Untuk | Contoh |
| --- | --- | --- |
| `feat` | Fitur atau perilaku gameplay baru | `feat/player-grab` |
| `fix` | Perbaikan bug | `fix/player-stuck` |
| `refactor` | Merapikan kode tanpa mengubah perilaku | `refactor/interaction-system` |
| `docs` | Dokumentasi saja | `docs/git-workflow` |
| `chore` | Setup, konfigurasi, tooling | `chore/unity-baseline` |

Satu branch untuk satu issue. Jangan menumpuk beberapa task di satu branch.

### Pesan commit

Format: `<type>: <apa yang berubah>` dalam bahasa Inggris, kalimat perintah, huruf kecil, tanpa titik. `type` sama dengan daftar di atas.

- Baik: `feat: add basic player grab`, `fix: stop wheelbarrow sliding on slopes`
- Buruk: `update`, `fix bug`, `WIP`, `perubahan baru`

Satu commit untuk satu perubahan yang masuk akal. Commit kecil yang sering lebih aman daripada satu commit besar di akhir.

### Mengerjakan satu task

```bash
# 1. Mulai dari main terbaru
git switch main
git pull

# 2. Buat branch task
git switch -c feat/player-grab

# 3. Kerja dan uji di Unity, lalu cek apa yang berubah
git status

# 4. Stage file yang memang bagian task, lalu commit
git add Assets/Game/Scripts/Player/PlayerGrab.cs Assets/Game/Scripts/Player/PlayerGrab.cs.meta
git commit -m "feat: add basic player grab"

# 5. Push dan buka PR (lewat GitHub atau gh)
git push -u origin feat/player-grab
gh pr create --fill
```

- Sebelum commit, baca `git status`. Pastikan `.meta` ikut, dan tidak ada `Library/`, `Temp/`, `Logs/`, build, atau file scene/settings yang tidak sengaja berubah. Kembalikan perubahan yang tidak sengaja lewat Unity atau `git restore <file>`.
- Di body PR tulis `Closes #<nomor issue>` agar issue tertutup saat merge. Pindahkan issue ke **In Review**.
- Setelah review meminta perubahan, commit lagi di branch yang sama lalu `git push`. PR terupdate otomatis.

### Mengikuti `main` dan konflik

```bash
git switch feat/player-grab
git pull origin main   # merge main ke branch task, tanpa rebase
```

Pakai merge, bukan rebase, agar tidak perlu force push. Untuk konflik di file kode, selesaikan lalu uji ulang di Unity. Untuk konflik di `.unity`, `.prefab`, atau `.asset`, jangan dipilih asal: hubungi pemilik perubahan lain dan putuskan bersama versi mana yang dipakai, lalu cek hasilnya di Editor dan Play Mode.

### Setelah merge

PR digabung dengan **Squash and merge** agar setiap task menjadi satu commit di `main`. Setelah merge:

```bash
git switch main
git pull
git branch -D feat/player-grab   # -D karena squash merge tidak dikenali -d
```

## Berbagi project Unity tanpa konflik

- Semua orang memakai versi Unity yang sama; catat versi tepatnya di `docs/DECISIONS.md` dan file proyek Unity ketika baseline dibuat.
- Pisahkan pekerjaan menurut prefab atau sistem. Scene utama merakit prefab, bukan tempat menyimpan semua logic. Ownership per task dicatat di issue/PR; jangan menetapkan satu orang permanen untuk seluruh sistem.
- Setiap developer bekerja di dev scene sendiri, `Assets/Game/Scenes/Dev/Dev_<Nama>.unity`, dan tidak mengubah dev scene orang lain. Scene utama hanya diubah oleh issue yang menyebutnya, oleh satu orang pada satu waktu.
- Pindah, rename, dan hapus asset hanya lewat Unity Editor agar GUID di `.meta` tetap terhubung. Jangan memakai File Explorer, terminal, atau `git mv` untuk file di `Assets/`.
- Koordinasikan sebelum dua orang mengubah scene, prefab inti, `ProjectSettings`, `Packages`, atau pengaturan Git/LFS yang sama. Jika terjadi konflik serialisasi Unity, minta pemilik perubahan meninjau hasil merge di Editor dan Play Mode.
- Saat project tersedia, asset Unity dan file `.meta` pasangannya harus ikut version control. Jangan commit cache atau hasil build lokal. Pola final `.gitignore` dan `.gitattributes` dikerjakan oleh issue setup, bukan ditebak dari dokumen ini.
- Letak folder dan penamaan file mengikuti [Struktur Folder dan Penamaan](PROJECT_STRUCTURE.md).

## Konvensi kode C#

Usulan awal; ubah lewat PR bila tim tidak setuju.

- Script di `Assets/Game/Scripts/<System>/`, memakai namespace `Ngecor.<System>` (contoh `Ngecor.Player`). Lihat [Struktur Folder dan Penamaan](PROJECT_STRUCTURE.md).
- Nama file sama dengan nama class (wajib di Unity untuk MonoBehaviour). Satu MonoBehaviour per file.
- Penamaan C# standar: `PascalCase` untuk class, method, property; `camelCase` untuk variabel lokal dan parameter; `_camelCase` untuk field private.
- Identifier dalam bahasa Inggris. Field yang perlu diatur di Inspector memakai `[SerializeField] private`, bukan `public`.
- Komentar hanya untuk alasan yang tidak terlihat dari kode, bukan menjelaskan baris per baris.

## Review dan Definition of Done

**Review teknis:** perubahan terfokus, tidak merusak sistem lain, dependency jelas, console bersih, mengikuti [Anggaran performa](DECISIONS.md#anggaran-performa), dan mudah dijelaskan. **Review gameplay:** reviewer memainkan alur, mencatat kemudahan kontrol, feel, jitter/fisika, serta apakah interaksi menghasilkan pengalaman yang dituju.

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
