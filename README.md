## Inferius Quality of Life

Modularni QoL balicek pro Subnauticu. Jeden mod obsahuje vsechna zlepseni nize - kazde je konfigurovatelne v hlavnim menu (Options -> Mods -> Inferius Quality of Life) a lze zapinat/vypinat samostatne.

## Featury

| Feature | Co dela |
|-|-|
| **Locker resize** | Zvetsi vanilla Locker i Wall Locker na konfigurovatelnou velikost (default 6x8 / 4x5). Runtime - zmena v Options ihned vidi ve hre. |
| **Inventory resize** | Pridava konfigurovatelny pocet radku/sloupcu do inventare hrace. Runtime update. |
| **Batohy (3 tiery)** | Osazuji se do Chip slotu, kazdy tier pridava N radku extra. Progressive recipe chain. |
| **Seamoth Turbo (3 tiery)** | Upgrade modul pro Seamoth. Sprint key + modul = boost rychlosti s vyssi spotrebou. Surface falloff, Power Upgrade Module discount. |
| **Merged Tanks (4 tiery)** | Kyslikove lahve klonovane z Plasteel. T1 spoji 2 lahve, dalsi tiery jsou sequential upgrade chain. T4 je lightweight bez speed penalty. |
| **Reinforced + Hyper baterie** | Nove tiery baterii/power cellu s vyssi kapacitou (default 250/500 mid, 1500/3000 endgame). |
| **Inventory Compressor chip** *(Experimental, od v0.3.1 docasne skryty z craftingu)* | Chip v Chip slotu - zmensi vsechny ne-blacklistovane items na 1x1. Per-instance persistent marker, funguje napric save/load a napric kontejnery. **Docasne skryt z craftingu** - lze zapnout v Options (`Craftable (Experimental)` toggle) nebo spawnout `spawn InferiusCompressor` v konzoli. **Pozor**: po osazeni a kompresii polozek se vytvori persistentni markery (`compressed-items.json`). Pri uninstalu modu bez predchoziho `qol_compressor_decompress_all` muze dojit k nepredvidatelnemu chovani/ztrate polozek. Pokud chip nikdy neosadis, zadna persistentni data se nevytvori - bezpecne uninstallovatelne. |
| **Teleport Beacon** | Buildable teleport zarizeni. UI menu s pojmenovanim, vyberem cile, vzdalenosti, cenou. 3 efficiency chipy pro snizeni energy cost. Model mini-Aurory. |
| **Locker Mover** *(od v0.2.0)* | Stehovani plnych skrini. Zamer na Locker/Wall Locker/Waterproof Locker/Carryall + stisk klavesy (default `G`) -> obsah do clipboardu. Stara skrin prazdna = deconstruct bezne. Opakovany stisk na nove prazdne skrini stejneho typu = obsah se presype zpet. Plne kompatibilni s Inventory Compressorem (slisovane polozky zustanou 1x1). Single-slot, in-memory (v1 neperzistuje pres save/quit). |
| **AutoCraft** *(od v0.3.0, port EasyCraft)* | Fabricator/Workbench/Modification Station/Habitat Builder cerpaji suroviny z okolnich skrini (default Range 100m, konfigurovatelne 50-500m). Rekurzivni auto-craft chybejicich sub-ingredienci (hloubka 5). Lepsi ingredient tooltipy s aktualnim pockem. Shift+klik = x5 batch, Ctrl+klik = x10 batch. Craft speed slider 50-500% (zrychleni = vyssi spotreba). Return surplus do inventare nebo Auto-sorter lockeru. |
| **Oxygen Auto-Refill** *(od v0.3.0)* | Rychlejsi doplneni O2 tanku pri vynoru nad hladinu nebo v moonpoolu/habitatu. Konfigurovatelna rate (vanilla 30/s, default 120/s). Toggle pro refill vsech lahvi v inventari (ne jen equipnute). |
| **Inventory Viewer** *(od v0.4.1)* | Aggregate prehled napric inventarem + okolnimi skrinemi. Hotkey toggle (default `I`). Filter, count per TechType, count containeru. Iteruje i inactive Carryally drzene v inventari. |
| **Scanner Room drillables + Time Capsules** | Integrace DrillableScan: skenerova mistnost rozlisuje male sbiratelne suroviny od 14 velkych vrtatelnych lozisek, vcetne Kyanite, a umi vyhledavat Time Capsules. |
| **Mobile Resource Scanner** | Equipnutelny Chip pro mobilni resource tracking. S drzenym Scannerem otevri menu pres konfigurovatelny mod input (default leve tlacitko mysi), vyber resource, pak drz v ruce nabity Scanner - HUD tracker skenuje kolem hrace a spotrebovava baterii Scanneru. Konfigurovatelny range, interval, energy use %, modifier, seznam tech typu a require-scanned filtr. |

## Detekce konfliktu

Pri startu detekujeme nainstalovane mody:
- `RamunesCustomizedStorage` -> vlastni locker resize se nezapne (kompatibilita)
- `AdvancedInventory` -> vlastni scrollable container se nezapne
- `BagEquipment` -> vlastni batohy se nezapnou
- `SlotExtender` -> detekovany, batohy mohou vyuzit extra Chip sloty

## Konzoleove prikazy (`~` konzole)

- `qol_status` - prehled vsech featur + stav detekce
- `qol_log_level <None|Info|Debug|Trace>` - runtime zmena verbosity
- `qol_apply` - force reapply config (inventory)
- `qol_compressor_decompress_all` - vymaze vsechny Compressor markery. **Spust pred uninstalem modu** jinak se komprimovane polozky pokusi vanilla Subnautica vratit na puvodni velikost a ty co se nevejdou zmizi.
- Dalsi `qol_*` prikazy pro diagnostiku

## Bezpecny uninstall

Pri odebrani modu:
1. Pred vypnuti Compressoru / uninstalem: spust `qol_compressor_decompress_all` v konzoli.
2. Uvolni misto v inventari + skrinich - polozky se budou snazit vratit na vanilla velikost (napr. Quartz z 1x1 zpet na 1x1, ale veci ve skrinich se nemusi vejit pokud bylo napacovane).
3. Save + reload. Polozky se rozbali na plnou velikost. Pokud nekde nezbyde misto, ztrati se.

## Konfigurace

**V Options menu**: kazda feature ma vlastni sekci s tooglem enabled + slidery. Zmeny se ukladaji do `BepInEx/config/InferiusQoL/config.json` a aplikuji runtime.

**Extra JSON soubory** v `BepInEx/plugins/InferiusQoL/`:
- `Data/CompressorBlacklist.json` - TechTypes ktere **nelze** lisovat (baterie, lahve, ryby, vajicka). User muze editovat.
- `compressed-items.json` - perzistentni seznam slisovanych items (auto-generovany).
- `beacons.json` - perzistentni beacon data (jmena, efficiency tier) (auto-generovany).
- `LanguageFiles/English.json`, `Czech.json` - lokalizacni soubory.

## Lokalizace

Subnautica podporuje vice jazyku. Vsechny texty modu (nazvy itemu, tooltipy, konzoleove hlasky) jsou dostupne v Angličtine a Češtine. Dalsi jazyky muzou byt pridany vytvorenim noveho JSON souboru v `LanguageFiles/`.

## Zavislosti

- Subnautica (ne Below Zero)
- BepInEx 5.x
- Nautilus (ex-SMLHelper 2.x)

## Source Code

| Component | File |
|-|-|
| Plugin entry point | [Plugin.cs](InferiusQoL/Plugin.cs) |
| Config | [InferiusConfig.cs](InferiusQoL/Config/InferiusConfig.cs) |
| Locker resize | [LockerResizePatch.cs](InferiusQoL/Features/LockerResize/LockerResizePatch.cs) |
| Inventory resize | [InventoryResizePatch.cs](InferiusQoL/Features/InventoryResize/InventoryResizePatch.cs) |
| Batohy | [BackpackItems.cs](InferiusQoL/Features/Backpacks/BackpackItems.cs) |
| Seamoth Turbo | [SeamothTurboItems.cs](InferiusQoL/Features/SeamothTurbo/SeamothTurboItems.cs), [SeamothTurboPatch.cs](InferiusQoL/Features/SeamothTurbo/SeamothTurboPatch.cs) |
| Merged Tanks | [TankWelderItems.cs](InferiusQoL/Features/TankWelder/TankWelderItems.cs) |
| Baterie | [BatteryItems.cs](InferiusQoL/Features/Batteries/BatteryItems.cs) |
| Compressor | [CompressorItem.cs](InferiusQoL/Features/Compressor/CompressorItem.cs), [CompressorSizePatch.cs](InferiusQoL/Features/Compressor/CompressorSizePatch.cs) |
| Teleport Beacon | [TeleportBeaconItem.cs](InferiusQoL/Features/TeleportBeacon/TeleportBeaconItem.cs), [TeleportBeaconBehavior.cs](InferiusQoL/Features/TeleportBeacon/TeleportBeaconBehavior.cs) |
| Locker Mover | [LockerMoverFeature.cs](InferiusQoL/Features/LockerMover/LockerMoverFeature.cs), [LockerMoverManager.cs](InferiusQoL/Features/LockerMover/LockerMoverManager.cs), [LockerMoverClipboard.cs](InferiusQoL/Features/LockerMover/LockerMoverClipboard.cs) |
| AutoCraft | [AutoCraftMain.cs](InferiusQoL/Features/AutoCraft/AutoCraftMain.cs), [AutoCraftPatches.cs](InferiusQoL/Features/AutoCraft/AutoCraftPatches.cs), [ClosestItemContainers.cs](InferiusQoL/Features/AutoCraft/ClosestItemContainers.cs), [ClosestFabricators.cs](InferiusQoL/Features/AutoCraft/ClosestFabricators.cs) |
| Oxygen Refill | [OxygenRefillPatch.cs](InferiusQoL/Features/OxygenRefill/OxygenRefillPatch.cs) |
| Scanner Room drillables | [DrillableScanFeature.cs](InferiusQoL/Features/ScannerRoom/DrillableScanFeature.cs) |
| Mobile Resource Scanner | [MobileResourceScannerFeature.cs](InferiusQoL/Features/MobileResourceScanner/MobileResourceScannerFeature.cs), [MobileResourceScannerItem.cs](InferiusQoL/Features/MobileResourceScanner/MobileResourceScannerItem.cs) |

## Changelog

Zmeny + bug fixy per verzi: [CHANGELOG.md](CHANGELOG.md).

## Licence

TBD.
