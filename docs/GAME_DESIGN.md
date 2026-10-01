# Yang Penting Jadi — Desain Game

Dokumen ini membawa arah dari `game_concept_yang_penting_jadi.md` ke repo agar semua anggota tim dan AI bekerja dengan konteks yang sama. **Yang Penting Jadi** masih working title. Contoh di bawah adalah ruang desain; implementasi konkret mengikuti issue dan hasil playtest.

## Janji game

Game party konstruksi kooperatif untuk **1–4 pemain**. Crew kecil mengerjakan kontrak bangunan dengan mengangkut material, mengoperasikan alat yang tidak selalu andal, dan mencari solusi sendiri ketika rencana gagal. Struktur tidak perlu cantik; proyek harus selesai.

Keseruan datang dari **logistik fisik, koordinasi manusia, improvisasi, dan reaksi berantai sistem**. Komedi muncul dari akibat tindakan pemain, bukan dari lelucon tertulis atau kejadian acak yang mengambil kendali pemain.

## Loop utama

**Pilih kontrak → siapkan alat → angkut material → produksi bahan → bangun → tangani masalah → selesaikan pekerjaan → dapat bayaran/reputasi → buka pilihan baru.**

Fase awal pengembangan membuktikan interaksi pendek yang menyenangkan. Workshop, ekonomi, reputasi, dan banyak kontrak adalah arah game, bukan syarat untuk playground pertama.

## Pilar desain

1. **Logistik fisik.** Tidak ada magic inventory. Semen, pasir, kerikil, bata, beton, dan scaffolding harus dipindahkan, dimuat, dilempar, disekop, atau diangkat di dunia game.
2. **Solusi bebas.** Berikan masalah, bukan satu cara benar. Tangga, scaffolding, pulley, crane, ramp, lemparan antarpemain, atau kendaraan sebagai platform boleh menjadi solusi bila sistem memungkinkan.
3. **Alat memberi manfaat dan risiko.** Setiap alat idealnya memecahkan satu masalah dan membuka paling tidak satu masalah baru. Contoh: gerobak membawa banyak barang tetapi bisa terguling; pickup mengangkut lebih banyak tetapi bisa overload atau terjebak.
4. **Kegagalan lunak.** Kesalahan menimbulkan pekerjaan tambahan yang dapat dipulihkan: tarik pickup dari lumpur, aduk manual saat mixer rusak, bangun ulang scaffolding. Hindari game over sebagai respons pertama.
5. **Tekanan situasional.** Hujan, beton yang mulai mengeras, supplier yang menunggu, atau alat yang bermasalah menciptakan pilihan prioritas. Hindari mengandalkan timer arcade sederhana.
6. **Kontrol sederhana, kombinasi kaya.** Aksi yang dipertimbangkan: grab, carry, push/pull, throw, pour, shovel, hammer, connect, drive, climb. Kompleksitas berasal dari interaksi antarsistem, bukan banyak tombol.

## Bahan, alat, dan lingkungan

Konstruksi bersifat **semi-realistis**. Contoh resep: semen + pasir + kerikil + air menghasilkan beton; beton memiliki waktu kerja sebelum mengeras. Sak semen bisa pecah atau rusak karena hujan, muatan bisa jatuh, dan pasir atau beton bisa tumpah. Detail angka, formula, dan simulasi fisika belum ditetapkan.

Mainan awal yang dipertimbangkan: wheelbarrow, pickup tua, concrete mixer kecil, scaffolding, rope, shovel, dan papan kayu. Rope dapat dipakai untuk towing, pulley, mengikat muatan, rescue, atau menahan struktur. Forklift, crane, concrete pump, excavator, dan generator adalah kemungkinan progression; **jangan memasukkannya ke prototype awal tanpa issue**.

Lokasi membentuk tantangan: tanjakan, jalan sempit, lumpur, hujan, pasir, tanah lembek, atau ruang vertikal. Arah dunia adalah **workshop permanen + map kontrak khusus**. Map tidak perlu procedural penuh; variasi cuaca, posisi material, kondisi alat, jalur, dan permintaan klien dapat menghasilkan cerita baru pada map yang sama.

## Vertical slice: Rumah Pak Ujang

**Tujuan:** cor lantai dua sebelum hujan besar. **Target durasi:** 15–25 menit. **Peralatan awal yang dibayangkan:** pickup tua, wheelbarrow, mixer kecil, scaffolding, papan kayu, rope, dan shovel.

1. Bongkar material dari pickup.
2. Tentukan posisi mixer, scaffolding, dan jalur angkut.
3. Campur bahan menjadi beton.
4. Bawa beton ke lantai dua sebelum kualitasnya turun.
5. Adaptasi saat hujan membuat lokasi lebih sulit.
6. Hadapi satu masalah terakhir yang muncul dari sistem, misalnya mixer macet atau listrik mati.

Keberhasilan slice berarti pemain dapat menyelesaikan proyek bersama dan menceritakan kekacauan yang muncul dari keputusan mereka sendiri. Contoh momen yang dicari: pickup mundur saat overload, gerobak meluncur, sak semen pecah, mixer macet, scaffolding roboh, pickup tersangkut, shortcut berisiko yang efektif, atau satu kesalahan memicu masalah lain. **Tidak semua contoh wajib ada**; beberapa momen alami lebih berharga daripada semua kejadian yang di-script.

## Batas scope prototype

Urutan kerja: **Works → Fun → Reliable → Clean → Pretty**. Jangan memulai final art, customization, achievement, open world, database, Steam integration, atau banyak map sebelum interaksi inti terbukti menyenangkan saat playtest.

Perubahan desain sebaiknya dicatat sebagai observasi playtest dahulu, lalu diputuskan lewat issue atau keputusan tim. Jangan mengubah prinsip inti diam-diam hanya karena satu implementasi sulit.
