# Ngecor — Project Management

Dokumen singkat untuk penggunaan GitHub Project tim.

## Board Status

Gunakan alur:

`Backlog → Ready → In Progress → In Review → Done`

Tambahkan `Blocked` untuk task yang tidak dapat dilanjutkan.

### Backlog
Ide atau pekerjaan yang belum siap dikerjakan.

### Ready
Ticket sudah punya Goal, Acceptance Criteria, How to Test, dependency yang jelas, dan dapat langsung diambil developer.

### In Progress
Sedang dikerjakan. Satu developer idealnya hanya memiliki satu main task aktif.

### In Review
PR sudah dibuka dan menunggu technical + gameplay review.

### Blocked
Tidak dapat dilanjutkan karena dependency atau technical blocker. Alasan blocker wajib dicatat di issue.

### Done
Acceptance criteria terpenuhi, review selesai, dan perubahan sudah merge ke `main`.

---

## Project Fields

Buat field berikut di GitHub Project:

- **Status:** Backlog / Ready / In Progress / In Review / Blocked / Done
- **Priority:** P0 / P1 / P2 / P3
- **Type:** Epic / Feature / Task / Bug / Tech
- **System:** Setup / Player / Interaction / Physics / Vehicle / Material / Construction / Multiplayer / Level / UI
- **Milestone:** M0 Foundation / M1 Player Playground / M2 Construction Toys / M3 Multiplayer Playground / M4 Core Loop / M5 Vertical Slice
- **Estimate:** 1 / 2 / 3 / 5
- **Iteration:** Sprint mingguan
- **Assignee:** Developer owner

Estimate adalah ukuran relatif, bukan jam. Jika sebuah task terasa lebih besar dari 5, pecah task tersebut.

---

## Recommended Views

### Current Sprint
Board layout, group by **Status**, filter iteration saat ini.

### Backlog
Table layout. Tampilkan Title, Priority, Type, System, Milestone, Estimate, Assignee. Group by Milestone.

### Roadmap
Roadmap layout. Hanya tampilkan Epic/Feature besar, bukan task kecil.

### Bugs
Filter Type = Bug dan group berdasarkan Priority.

---

## Priority

- **P0:** blocker; project/core flow tidak dapat berjalan
- **P1:** wajib untuk milestone saat ini
- **P2:** penting tetapi dapat menunggu
- **P3:** nice-to-have / polish

Jika semua task menjadi P1, berarti prioritas belum benar.

---

## Weekly Iteration

Gunakan sprint 1 minggu selama fase prototype.

### Planning
Tentukan satu Sprint Goal yang berbentuk outcome, contoh:

> Player can move and interact with physics objects.

Pilih hanya ticket yang mendukung goal tersebut.

### Development
Developer mengambil task dari **Ready**, membuat feature branch, mengembangkan dan mengetes secara lokal.

### Review
Saat PR dibuka, pindahkan issue ke **In Review**. Review mencakup code dan gameplay.

### Playtest
Di akhir minggu, mainkan `main` bersama dan catat observasi sebelum menentukan pekerjaan minggu berikutnya.

---

## Ticket Rule

Task yang masuk **Ready** wajib memiliki:

1. Goal
2. Why
3. Acceptance Criteria
4. How to Test
5. Dependency
6. Out of Scope

Ticket mendeskripsikan outcome, bukan memerintah developer membuat file tertentu.

Contoh buruk:

> Create WheelbarrowController.cs

Contoh baik:

> Player dapat membawa minimal tiga physics object menggunakan wheelbarrow dari titik A ke titik B.

---

## Issue Prefix

Gunakan prefix berikut:

- `SETUP-xxx`
- `PLAYER-xxx`
- `INT-xxx`
- `VEH-xxx`
- `MAT-xxx`
- `MIX-xxx`
- `BUILD-xxx`
- `NET-xxx`
- `GAME-xxx`
- `LEVEL-xxx`
- `TOOL-xxx`
- `UI-xxx`
- `BUG-xxx`

---

## Pull Request Flow

`Issue → Branch → Local Test → PR → Technical Review → Gameplay Review → Merge → Done`

Branch contoh:

- `feat/player-grab`
- `feat/wheelbarrow`
- `fix/player-stuck`

`main` adalah kondisi project paling stabil dan harus tetap playable.
