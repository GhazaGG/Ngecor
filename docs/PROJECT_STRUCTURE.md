# Struktur Folder dan Penamaan

Aturan ini menentukan di mana setiap file diletakkan dan bagaimana menamainya. Tujuannya agar empat developer dan AI masing-masing menaruh file di tempat yang sama tanpa perlu bertanya. Jika file yang kamu buat tidak cocok dengan aturan di bawah, tanyakan di issue atau PR; jangan membuat folder baru sendiri.

## Root repo

Usulan: project Unity berada langsung di root repo, sejajar dengan `docs/`. Konfirmasi dan catat di `docs/DECISIONS.md` saat SETUP-001.

```text
Ngecor/
├── .development-history/   laporan kerja (lihat AGENTS.md)
├── .github/                template issue dan PR
├── docs/                   dokumen tim
├── Assets/                 semua asset Unity (lihat di bawah)
├── Packages/               manifest package Unity (diubah hanya lewat issue)
├── ProjectSettings/        pengaturan Unity bersama (diubah hanya lewat issue)
├── AGENTS.md
└── README.md
```

`Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`, dan folder build tidak pernah di-commit.

## Isi `Assets/`

```text
Assets/
├── Game/                       semua milik tim
│   ├── Art/
│   │   ├── Models/<System>/
│   │   ├── Textures/<System>/
│   │   └── Animations/<System>/
│   ├── Audio/
│   │   ├── SFX/<System>/
│   │   └── Music/
│   ├── Materials/<System>/
│   ├── Prefabs/<System>/
│   ├── Scenes/
│   │   ├── <SceneUtama>.unity
│   │   └── Dev/
│   │       └── Dev_<Nama>.unity
│   ├── Scripts/
│   │   ├── <System>/
│   │   └── Editor/            hanya script editor (bukan bagian build)
│   └── Settings/              render pipeline, input actions, physics material bersama
└── ThirdParty/                asset dari luar (Asset Store, download)
    └── <NamaAsset>/
```

### `<System>`

Nama subfolder memakai nilai field **System** di [Manajemen Proyek](PROJECT_MANAGEMENT.md), dalam bentuk PascalCase:

`Player`, `Interaction`, `Physics`, `Vehicle`, `Material`, `Construction`, `Multiplayer`, `Level`, `UI`

Contoh: script gerobak di `Scripts/Vehicle/`, prefab gerobak di `Prefabs/Vehicle/`, model gerobak di `Art/Models/Vehicle/`. Satu sistem memakai nama yang sama di setiap folder tipe.

File yang benar-benar dipakai dua sistem atau lebih boleh ditaruh di `<Tipe>/Shared/`. "Mungkin nanti dipakai sistem lain" tidak cukup; letakkan di sistem pemiliknya dulu dan pindahkan lewat Editor saat dibutuhkan.

### Aturan

1. **Semua file buatan tim ada di `Assets/Game/`.** Jangan menaruh file langsung di `Assets/` atau di folder `ThirdParty/`.
2. **Asset pihak ketiga ada di `Assets/ThirdParty/<NamaAsset>/`** dan tidak diedit. Jika perlu mengubahnya, duplikat file ke `Assets/Game/` lalu ubah salinannya. Package dari Package Manager berada di `Packages/` dan tidak perlu dipindah. Asset pihak ketiga hanya masuk lewat issue (lihat Larangan keras di `AGENTS.md`).
3. **Jangan membuat folder kosong** untuk rencana. Folder dibuat saat file pertama masuk.
4. **Jangan memakai folder `Resources/`.** Pakai referensi langsung lewat Inspector. `Resources/` memaksa semua isinya masuk build dan menyembunyikan dependency.
5. **Folder bernama `Editor`** hanya untuk script `UnityEditor`. Jangan menaruh script gameplay di dalamnya karena tidak akan masuk build.
6. **Dev scene** milik satu orang dan tidak dimasukkan ke Build Settings. Scene utama langsung di `Scenes/`.
7. **Pindah dan rename hanya lewat Unity Editor.**
8. **Folder baru di luar struktur ini** (misalnya `Tests/`, `VFX/`, `Fonts/`) diusulkan di PR yang pertama kali membutuhkannya, lalu dokumen ini diperbarui di PR yang sama.

## Penamaan

| Jenis | Aturan | Contoh |
| --- | --- | --- |
| Folder | PascalCase, bahasa Inggris | `Vehicle`, `Construction` |
| Script C# | Nama file = nama class, PascalCase | `WheelbarrowCarry.cs` |
| Prefab | PascalCase, nama benda | `Wheelbarrow.prefab` |
| Prefab variant | `<Base>_<Varian>` | `Wheelbarrow_Rusty.prefab` |
| Scene | PascalCase | `Playground.unity`, `Dev_Ghaza.unity` |
| Material | PascalCase, nama permukaan | `WetSand.mat` |
| Texture | `<Nama>_<Map>` | `Wheelbarrow_Albedo.png`, `Wheelbarrow_Normal.png` |
| Model | PascalCase | `Wheelbarrow.fbx` |
| Audio | `<Sumber>_<Aksi>` | `Wheelbarrow_Tip.wav`, `Mixer_Jam.wav` |

- Semua nama memakai bahasa Inggris, tanpa spasi, dan tanpa karakter selain huruf, angka, `_`, atau `-`.
- Nama menjelaskan isi, bukan status. Dilarang: `New Material`, `Untitled`, `Test`, `Final`, `v2`, `Copy`, `asdf`, nama orang (kecuali dev scene).
- Namespace mengikuti folder script: `Assets/Game/Scripts/Vehicle/` → `namespace Ngecor.Vehicle`.

## Saat review

Reviewer menolak PR jika file berada di luar struktur ini, nama melanggar tabel di atas, atau ada folder kosong atau file di root `Assets/`. Perbaikan cukup dengan memindahkan atau me-rename lewat Editor di branch yang sama.
