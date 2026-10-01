# NET-001: Multiplayer Session Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Membangun sesi multiplayer lokal sederhana menggunakan Netcode for GameObjects (NGO) 2.x + Unity Transport dalam mode host, sehingga 2 player dapat menjadi host dan join ke scene Playground, spawn secara terpisah, dan menangani disconnect tanpa membuat host crash.

**Architecture:** Menggunakan Unity Netcode for GameObjects (`NetworkManager` + `UnityTransport`). `SessionManager` membungkus inisialisasi Host dan Client pada port default 7777 (localhost). `SessionHUD` menyediakan kontrol GUI minimalis (Host / Join / Disconnect / IP input). Prefab player prototype ber-`NetworkObject` terdaftar sebagai NetworkManager default player prefab untuk auto-spawn saat terhubung.

**Tech Stack:** Unity 6000.3.25f1, Netcode for GameObjects 2.13.3 (`com.unity.netcode.gameobjects`), Unity Transport 2.7.4 (`com.unity.transport`), Universal Render Pipeline (URP), C# (.NET Standard / Mono).

**Spec:** [GitHub Issue #20: [NET-001] Multiplayer session prototype](https://github.com/GhazaGG/Ngecor/issues/20), [`docs/DECISIONS.md#2026-10-01--networking-dan-batas-authority`](file:///C:/Users/RYZEN/orca/workspaces/Ngecor/net-001-multiplayer-session-prototype/docs/DECISIONS.md#2026-10-01--networking-dan-batas-authority), dan [`AGENTS.md`](file:///C:/Users/RYZEN/orca/workspaces/Ngecor/net-001-multiplayer-session-prototype/AGENTS.md).

## Global Constraints

- **Unity Version:** Terkunci pada `6000.3.25f1` (Unity 6.3 LTS). Tidak menambah package di luar yang tercatat di `docs/DECISIONS.md`.
- **Package Exact Versions:** `com.unity.netcode.gameobjects: 2.13.3` dan `com.unity.transport: 2.7.4`. Wajib dicatat di `docs/DECISIONS.md`.
- **Authority Boundary:** Host adalah sumber kebenaran (posisi objek fisika, hasil grab, spawn, state dunia). Client hanya mengatur movement/look player miliknya sendiri.
- **Connection Scope:** LAN / Direct IP (localhost `127.0.0.1:7777`). Out of scope: Matchmaking, Steam integration, Dedicated server, Relay/Lobby (UGS).
- **Asset Integrity:** Modifikasi asset serialisasi Unity (`.unity`, `.prefab`, `.asset`, `.meta`) wajib valid dan konsisten dengan panduan di `docs/PROJECT_STRUCTURE.md`.
- **History Reporting:** Setiap perubahan harus diuji nyata (Host + Client) dan dicatat dalam laporan `.development-history/YYYY-MM-DD-HH-mm-multiplayer-session-prototype.md`.

## Review Focus

1. **Host Crash on Client Disconnect:** Ketika client keluar tiba-tiba (Alt+F4 / disconnect), `SessionManager` harus menangani callback tanpa unhandled exception atau crash di host.
2. **Duplicate NetworkManager:** Menjaga agar `NetworkManager` tidak dibuat ganda saat reload scene.
3. **Null Connection Input:** Memasukkan IP string kosong atau format port tidak valid saat menekan tombol Join tidak boleh menyebabkan silent freeze atau error crash.
4. **Player Spawn Overlap:** Dua player spawn di Playground tidak saling menjepit atau terlempar karena collision awal yang keras.
5. **Standalone/Editor Multi-instance Compatibility:** Build standalone Windows dan Editor instance dapat saling terhubung di loopback `127.0.0.1:7777`.

---

### Task 1: Package Installation & Documentation Record

**Files:**
- Modify: `Packages/manifest.json:8-10`
- Modify: `docs/DECISIONS.md:31-32,107-110`

**Interfaces:**
- Consumes: None
- Produces: NGO package references (`Unity.Netcode`, `Unity.Netcode.Transports.UTP`) available for compilation.

- [x] **Step 1: Check baseline manifest before modification**
Run: `git diff Packages/manifest.json`
Expected: Clean.

- [x] **Step 2: Add NGO and Unity Transport dependencies to `Packages/manifest.json`**
Tambahkan:
```json
"com.unity.netcode.gameobjects": "2.13.3",
"com.unity.transport": "2.7.4",
```

- [x] **Step 3: Update `docs/DECISIONS.md` with pinned package versions**
Catat versi exact `com.unity.netcode.gameobjects: 2.13.3` dan `com.unity.transport: 2.7.4` pada tabel status dan catatan keputusan Networking.

- [x] **Step 4: Verify Unity package resolution and compilation**
Run: `powershell -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -quit -projectPath . -logFile -"`
Expected: Clean exit code 0 tanpa compiler error.

- [x] **Step 5: Commit**
```bash
git add Packages/manifest.json Packages/packages-lock.json docs/DECISIONS.md
git commit -m "chore: add NGO and Unity Transport packages"
```

---

### Task 2: Core Session Management (`SessionManager.cs`)

**Files:**
- Create: `Assets/Game/Scripts/Multiplayer/SessionManager.cs`
- Create: `Assets/Game/Scripts/Multiplayer/SessionManager.cs.meta`

**Interfaces:**
- Consumes: `Unity.Netcode.NetworkManager`, `Unity.Netcode.Transports.UTP.UnityTransport`
- Produces:
  - `public bool StartHostSession(string ip = "127.0.0.1", ushort port = 7777)`
  - `public bool StartClientSession(string ip = "127.0.0.1", ushort port = 7777)`
  - `public void DisconnectSession()`
  - `public event Action<ulong> OnClientConnectedEvent`
  - `public event Action<ulong> OnClientDisconnectedEvent`
  - `public SessionState CurrentState { get; }`

- [x] **Step 1: Implement `SessionManager.cs` in `Assets/Game/Scripts/Multiplayer/`**
Implementasikan MonoBehaviour `SessionManager` di namespace `Ngecor.Multiplayer`:
- Menangani `StartHostSession` dan `StartClientSession`.
- Mengkonfigurasi `UnityTransport.SetConnectionData(ip, port)`.
- Mengaitkan event handler untuk `NetworkManager.Singleton.OnClientConnectedCallback` dan `OnClientDisconnectCallback`.
- Menangani graceful shutdown tanpa crash ketika client atau host menghentikan sesi.

- [x] **Step 2: Verify compilation and namespace formatting**
Run: `powershell -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -quit -projectPath . -logFile -"`
Expected: No script compilation errors.

- [x] **Step 3: Commit**
```bash
git add Assets/Game/Scripts/Multiplayer/SessionManager.cs Assets/Game/Scripts/Multiplayer/SessionManager.cs.meta
git commit -m "feat: implement SessionManager for host and client lifecycle"
```

---

### Task 3: Prototype Network Player Prefab (`Player_Prototype.prefab`)

**Files:**
- Create: `Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab`
- Create: `Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab.meta`

**Interfaces:**
- Consumes: `Unity.Netcode.NetworkObject`, `Unity.Netcode.Components.NetworkTransform`
- Produces: Default Player Prefab untuk `NetworkManager` yang mereplikasi posisi dasar dan warna pembeda (mis. Host vs Client) saat di-spawn.

- [x] **Step 1: Create minimal Network Player Prefab structure**
Prefab Capsule sederhana dengan:
- `CapsuleCollider`, `MeshFilter`, `MeshRenderer`
- `NetworkObject` (Synchronize Transform: true)
- `NetworkTransform`
- Material placeholder kontras agar mudah dibedakan saat pengetesan.

- [x] **Step 2: Verify prefab validation via Unity command**
Run batchmode verify untuk memastikan prefab dan GUID meta terbaca tanpa missing component.
Expected: Unity log bersih tanpa missing reference.

- [x] **Step 3: Commit**
```bash
git add Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab.meta
git commit -m "feat: add prototype network player prefab"
```

---

### Task 4: Minimal Session HUD (`SessionHUD.cs`)

**Files:**
- Create: `Assets/Game/Scripts/Multiplayer/SessionHUD.cs`
- Create: `Assets/Game/Scripts/Multiplayer/SessionHUD.cs.meta`

**Interfaces:**
- Consumes: `Ngecor.Multiplayer.SessionManager`
- Produces: User Interface (OnGUI atau minimal canvas) dengan tombol:
  - Input field IP (default `127.0.0.1`) dan Port (`7777`)
  - "Start Host"
  - "Join as Client"
  - "Disconnect"
  - Status display ("Connected as Host", "Connected as Client", "Disconnected", Player count)

- [x] **Step 1: Implement `SessionHUD.cs` in `Assets/Game/Scripts/Multiplayer/`**
Implementasikan script HUD minimalis yang memanggil method pada `SessionManager`. Menggunakan `OnGUI` ringan sesuai prinsip [Anggaran Performa](docs/DECISIONS.md#anggaran-performa) (tidak membutuhkan asset grafik tambahan untuk prototype NET-001).

- [x] **Step 2: Verify compilation**
Run: `powershell -Command "& 'C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe' -batchmode -quit -projectPath . -logFile -"`
Expected: No compiler errors.

- [x] **Step 3: Commit**
```bash
git add Assets/Game/Scripts/Multiplayer/SessionHUD.cs Assets/Game/Scripts/Multiplayer/SessionHUD.cs.meta
git commit -m "feat: add SessionHUD for host and client connection controls"
```

---

### Task 5: Playground Scene Integration & Multi-Client Verification

**Files:**
- Modify: `Assets/Game/Scenes/Playground.unity`
- Create: `.development-history/2026-10-02-01-30-multiplayer-session-prototype.md`

**Interfaces:**
- Consumes: `SessionManager`, `SessionHUD`, `Player_Prototype.prefab`, `Playground.unity`
- Produces: Playable multiplayer scene yang memenuhi seluruh Acceptance Criteria Issue #20.

- [ ] **Step 1: Assemble Network Manager in `Playground.unity`**
- Pasang GameObject `[Network]` dengan komponen `NetworkManager`, `UnityTransport`, `SessionManager`, dan `SessionHUD`.
- Pasang `Player_Prototype.prefab` ke field `NetworkManager.PlayerPrefab`.

- [ ] **Step 2: Build Standalone Windows Executable for 2-Player Local Test**
Buat build standalone Windows (`Build/Ngecor_Test.exe`) untuk mengetes Host + Client di 1 mesin:
1. Jalankan Build standalone sebagai Host.
2. Jalankan Unity Editor Play Mode sebagai Client yang bergabung ke `127.0.0.1`.
3. Verifikasi:
   - Host berhasil berjalan.
   - Client berhasil join.
   - Kedua player capsule ter-spawn di scene Playground.
   - Client disconnect -> Host tetap berjalan normal tanpa crash.

- [ ] **Step 3: Write development history report**
Tulis laporan lengkap di `.development-history/2026-10-02-01-30-multiplayer-session-prototype.md` sesuai format standar di `AGENTS.md`.

- [ ] **Step 4: Commit**
```bash
git add Assets/Game/Scenes/Playground.unity .development-history/2026-10-02-01-30-multiplayer-session-prototype.md
git commit -m "feat: wire multiplayer session into Playground and verify multi-client"
```
