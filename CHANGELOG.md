# Changelog

Vsechny zmeny v Inferius Quality of Life modu.

## v0.4.1

### Pridano
- **Inventory Viewer** - aggregate prehled vsech polozek napric Inventory + StorageContainery (lockery, carryally vc. drzenych v inventari) v dosahu. Hotkey toggle (default `I`, konfigurovatelny v Options). IMGUI okno s filtrem, refreshem, pocty per TechType + pocty containeru. Iteruje pres `Resources.FindObjectsOfTypeAll<StorageContainer>` aby zachytil i inactive instances.
- **Teleport zdarma v Creative/Freedom modu** - automaticky detekuje pres `GameModeUtils.RequiresPower()`, skip energy check + drain. Take novy toggle `Always free (no energy cost)` v Options pro Survival/Hardcore (default OFF).
- **Scanner Room drillable deposits + Time Capsules** - integrovany DrillableScan jako samostatne konfigurovatelna feature. Skenerova mistnost rozlisuje male sbiratelne suroviny od velkych vrtatelnych lozisek, umi vyhledavat Time Capsules a pouziva vlastni ikony.
- **Mobile Resource Scanner** - integrovany chip podle MobileResourceScanner modu. Po equipnuti umozni vybrat resource pres konfigurovatelny mod input (default leve tlacitko mysi), ale menu i samotny tracking funguji jen kdyz hrac drzi Scanner; pri kazdem refreshi spotrebovava jeho baterii. Options obsahuji range, interval, energy use 0-200%, modifier, require-scanned filtr a volbu zobrazit vsechny TechTypes.

### Opraveno
- **Teleport Beacon byl navic v Mobile Vehicle Bay** - smazan `WithFabricatorType(CraftTree.Type.Constructor)` ktery duplikoval beacon do MVB craft tree. Habitat Builder zahrnuti zustava pres `TechGroup.InteriorPieces`.
- **Cursor unlock pri otevreni Inventory Viewer okna** - prvotne pouzite `Cursor.lockState` nestacilo (Subnautica FPS input modul re-locknul kazdy frame). Fix: `UWE.Utils.lockCursor` flag (stejny pattern jako vanilla PDA / TeleportBeaconUI).
- **DrillableScan Kyanite bug** - puvodni mod pouzival pevny rozsah enum hodnot a preskocil `DrillableKyanite`, ktere ma v aktualni assembly hodnotu 85. Nahradeno explicitni mapou 14 existujicich velkych lozisek. Mercury je cut content a Sulfur nema velke lozisko, proto nejsou v seznamu.

## v0.3.3

### Opraveno
- **Reinforced/Hyper baterie + power celly nesly do nabijecek** - `BatteryCharger.compatibleTech` + `PowerCellCharger.compatibleTech` jsou staticky HashSets s vanilla TechTypes; nase custom typy nebyly v listu. Navic Nautilus `CloneTemplate` neprenasi `EquipmentType` z TechData. Fix: `BatteryItems.InjectIntoChargers()` pridava nase TT do obou HashSetu + explicitni `prefab.SetEquipment(EquipmentType.BatteryCharger / PowerCellCharger)`.
- **Workbench taby se prekryvaji bez radial menu modu** - kdyz hrac nema radial-menu mod (`RadialTabs` / `BetterCraftMenu` / podobne), Modification Station UI prekryva nase custom taby. Fix: detekce radial menu v Plugin.Awake, pri absenci se Workbench taby NEvytvari a upgrady (Backpacks Medium/Large, Hyper baterie, Compressor, Seamoth Turbo MK2/MK3, Tank Welder, Teleport Efficiency chipy) jdou primo do rootu Workbenche. S radial menu modem se chova standardne s organizovanymi taby.

### Zmeneno
- **Inventory Compressor docasne skryt z craftingu** - novy toggle `Craftable (Experimental)` v Options (default OFF). Chip uz neni v craft tree ani v PDA. TechType se stale registruje, takze `spawn InferiusCompressor` v konzoli funguje. Pokud hrac chip nikdy neosadi, zadna persistentni data se nevytvori = bezpecne k odinstalaci. Pokud ho osadi a zkomprimuje polozky, pri odinstalaci bez predchoziho `qol_compressor_decompress_all` hrozi ztrata polozek - oznaceno jako experimentalni.

## v0.3.0

### Pridano
- **AutoCraft** (port + integrace EasyCraft modu) - Fabricator/Workbench/Modification Station/Habitat Builder/Mobile Vehicle Bay cerpaji suroviny z okolnich skrini. Rekurzivni auto-craft chybejicich sub-ingredienci (hloubka 5). Vylepsene tooltipy ingredienci s aktualnim pockem.
  - Konfigurovatelny storage mode: **Off** / **Inside base-pod** / **Range** (default Range).
  - Range slider 50-500m (default 100).
  - Return-surplus cil: Inventory nebo Lockers (auto-sorter label supported).
  - **Batch craft** (Shift = x5, Ctrl = x10, konfigurovatelne) - integrovano do AutoCraft flow. Pokud chybi suroviny pro plny batch, scale-down na max affordable.
  - **Craft speed** slider 50-500% (default 100% = vanilla). Zrychleni = vyssi energy cost v pomeru.
- **Oxygen Auto-Refill** - rychlejsi doplneni kysliku pri vynoru na hladinu nebo v moonpoolu/habitatu. Konfigurovatelna rychlost (default 120/sec, vanilla 30/sec). Option doplnuje i **vsechny volne lahve v inventari**, ne jen equipnutou.
- **Locker Mover** - (pridan v 0.2.0, v 0.3.0 zustava) - presun plne skrine pres keybind G do in-memory clipboardu, pak na prazdnou skrin stejneho typu.
- Konzoleovy prikaz `qol_compressor_decompress_all` - bezpecna dekomprese vsech oznacenych polozek pred uninstalem modu. Dropne je do sveta u hrace + vymaze markery. Iteruje i INACTIVE StorageContainery (Carryally/Waterproof Lockery drzene v inventari).
- Detekce `EasyCraft` modu - pokud puvodni EasyCraft instalovan, WARNING at ho uzivatel odinstaluje (patche se biji, nas AutoCraft je plnohodnotna nahrada).

### Opraveno
- **Harmony PatchAll robustness** - patche se ted aplikuji per-class s try/catch. Drive selhani jednoho patche (napr. OxygenManager.Awake ktery neexistuje) shodilo vsechny nasledujici patche -> prestal fungovat Inventory Resize + Compressor -> polozky se ztracely. Ted jeden bad patch nenabourava ostatni, viditelne v logu jako `Harmony patches applied (ok=X, failed=Y)`.
- **Compressor tag jen veci co se realne zmensi** - pri pickupu novych polozek Compressor chip tagoval vsechny polozky vcetne 1x1 (Quartz, Creepvine, ruda). Nyni se taguji jen polozky > 1x1. Mene polozek v `compressed-items.json`, mene problemu pri decompress.
- **Compressor uninstall data-loss riziko** - dokumentovano uninstall pravidlo, pridan decompress command. Drive pri odebrani modu se komprimovane polozky pokusily vratit na vanilla velikost a pokud se nevesly do current layout containeru, ztratily se.
- **Config slider reset bug** - pri zmene jednoho slideru (napr. AutoCraft range) se inventar resetoval na vanilla velikost. Pricina: Nautilus pri save pouziva fresh instance configu s defaulty pro nezmenene pole; my jsme pak `ApplyRuntime(__instance)` propsali defaults misto user values. Ted singleton se reloaduje z JSON pred ApplyRuntime.

### Zmeneno
- Stary jednoduchy `BatchCraft` (Shift x5 / Ctrl x10) smazan - nahrazen AutoCraft portem s plnou funkcionalitou (nearby storage + recursive auto-craft + batch + speed).

---

## v0.2.0

### Pridano
- **Locker Mover** - presun plneho Lockeru / Wall Lockeru / Waterproof Lockeru / Carryallu. Zamer na plnou skrin + klavesa (default G) -> obsah do clipboardu. Opakovany stisk na prazdnou skrin stejneho typu -> obsah zpet.
- Plna kompatibilita s Inventory Compressorem (slisovane polozky zustavaji 1x1 pres cely transfer).

### Technicky detail
- Clipboard pouziva skryty `ItemsContainer` 16x16 misto plain GO - Pickupable se korektne deaktivuji pres vanilla AddItem/RemoveItem path (zadne duplicity na zemi).
- Init pres Harmony postfix na Player.Awake (GO vytvoreny v Plugin.Awake koncil v DontDestroyOnLoad limbu bez Update ticku).
- Lokalizace EN + CZ.

---

## v0.1.x (pre-release iterace)

### Pridano postupne
- **Inventory Resize** - konfigurovatelne extra radky/sloupce pro hracuv inventar. Runtime update slideru.
- **Locker Resize** - vetsi vanilla Locker, Wall Locker, Waterproof Locker, Carryall, vehicle storage. Default 6x8 / 4x5.
- **Batohy** (3 tiery Small/Medium/Large) - osazuji se do Chip slotu, pridavaji extra radky inventare. Progressive recept. SlotExtender detekce pro extra Chip sloty.
- **Seamoth Turbo** (3 tiery + efficiency handling) - vehicle modul, sprint + modul = boost rychlosti. Surface falloff, Power Upgrade Module discount.
- **Merged Tanks** (4 tiery) - spojene kyslikove lahve z 2x Plasteel. T4 lightweight bez speed penalty.
- **Battery Rework** - Reinforced / Hyper tiery baterii a power cellu.
- **Inventory Compressor chip** - komprese polozek na 1x1 pres per-instance marker (perzistentni napric save/load a kontejneru). Blacklist pro ryby, vajicka, baterie, lahve.
- **Teleport Beacon** - buildable teleport zarizeni s UI menu (pojmenovani, vyber cile, vzdalenost, cena). 3 efficiency chipy. Model mini-Aurory.

### Detekce modu
- `RamunesCustomizedStorage`, `AdvancedInventory`, `BagEquipment`, `SlotExtender` - feature gating pri konfliktu.

### Lokalizace
- Kompletni EN + CZ preklady (item names, tooltipy, Options menu labely).
