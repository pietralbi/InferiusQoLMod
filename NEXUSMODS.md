# Inferius Quality of Life

A modular Quality-of-Life package for Subnautica. One mod, many improvements - all configurable, all can be toggled individually.

---

## English description (for Nexus Mods Description tab)

### What it does

**Inferius Quality of Life** bundles the most-requested QoL tweaks into a single mod. Every feature is optional and configurable in-game (Options → Mods → Inferius Quality of Life). All item names, tooltips and labels are localized (English + Czech).

### Features

**Inventory**
- **Enlarge player inventory** - add configurable rows/columns. Runtime updates when you move the slider.
- **Bigger lockers** - resize vanilla Locker and Wall Locker to custom dimensions (default 6×8 / 4×5). Works both on newly-built and existing lockers.
- **Inventory Compressor chip** *(Experimental, temporarily hidden from crafting since v0.3.1)* - equip in Chip slot to shrink every non-blacklisted item to 1×1. Persistent across save/load, base storage, vehicles. User-editable blacklist (fish, eggs, batteries, tanks). **Hidden from crafting by default** - toggle `Craftable (Experimental)` in Options or `spawn InferiusCompressor` in console. **Warning**: once equipped, compressed items get persistent markers in `compressed-items.json`. Uninstalling the mod without running `qol_compressor_decompress_all` first may cause unpredictable behavior / item loss. If you never equip the chip, no persistent data is created - safe to uninstall.

**Backpacks** (3 tiers)
- Equip in Chip slot for extra inventory rows. Progressive recipe chain: Small → Medium → Large. Each tier consumes the previous + new materials.

**Seamoth Turbo** (3 tiers + efficiency handling)
- Vehicle module. Hold Sprint while piloting + module installed = speed boost with higher drain.
- Surface falloff: boost smoothly fades as you approach the surface (no more "jumping out of water" glitches).
- Respects vanilla Vehicle Power Upgrade Module discount.
- Each tier has its own speed/drain multiplier in config.

**Merged Oxygen Tanks** (4 tiers)
- Craft in Modification Station. T1 combines 2× Plasteel Tank; T2/T3/T4 upgrade the previous merged tank.
- T1/T2/T3: inherit vanilla Plasteel speed penalty (weight).
- **T4 Lightweight**: same capacity as T3 but no speed penalty.

**Battery Rework**
- **Reinforced Battery / Power Cell** (default 250/500) - mid-game tier, craftable in Fabricator → Resources → Electronics alongside vanilla.
- **Hyper Battery / Power Cell** (default 1500/3000) - endgame tier in Modification Station, consumes Ion Battery/Cell in recipe.

**Teleport Beacon**
- Buildable interior piece in Habitat Builder. Uses the Aurora mini-model (Starship Souvenir mesh).
- Click the beacon to open a menu: rename it, see all other beacons with distance + energy cost, teleport to any of them.
- Energy cost is distance-based (base cost + per-100m factor).
- **3 Efficiency Chips** (MK1/MK2/MK3) - craft them and install directly in the beacon UI. Each tier reduces teleport cost (default 75% / 50% / 25% of base).
- Teleport drops you exactly 2m in front of the target beacon, facing it - consistent landing.

**Locker Mover** *(since v0.2.0)*
- Relocate full lockers without manually unloading every item. Point at a full Locker / Wall Locker / Waterproof Locker / Carryall, press the keybind (default `G`) - the contents move to an in-memory clipboard and the locker becomes empty (ready to deconstruct).
- Point at a new empty locker of the same type and press the key again - contents drop back in.
- Fully compatible with the **Inventory Compressor**: compressed items stay 1×1 throughout the move.
- Single-slot clipboard. Current limitation: contents are lost on save/quit while the clipboard is non-empty (v1 does not persist).

**AutoCraft** *(since v0.3.0, port of EasyCraft by Wintik)*
- Fabricator/Workbench/Modification Station/Habitat Builder/Mobile Vehicle Bay pull ingredients from **nearby storage** (default: Range 100m, configurable 50-500m; or Inside base/pod, or Off).
- **Recursive auto-craft** of missing sub-ingredients (depth 5). If you have Titanium + Lithium but miss Plasteel, it crafts the Plasteel for you.
- **Batch craft** - hold **Shift** (default x5) or **Ctrl** (default x10) while clicking a recipe. Multipliers configurable. If you don't have enough for the full batch, scales down to the maximum you can afford.
- **Craft speed** slider (50-500%, default 100% = vanilla). Faster crafting drains proportionally more energy.
- **Better ingredient tooltips** - shows current count vs required.
- **Return surplus** - leftover crafted items go to Inventory or nearest Autosorter locker (configurable).

**Oxygen Auto-Refill** *(since v0.3.0)*
- Faster oxygen tank refill when you surface above water or enter a moonpool/habitat. Vanilla rate is 30 units/sec; default here is 120/sec (configurable up to 300).
- Toggle: refill **all oxygen tanks in your inventory**, not just the equipped one.
- No free oxygen underwater - refill only triggers where vanilla already breathes you (surface + pressurised interiors).

**Inventory Viewer** *(since v0.4.1)*
- Hotkey-toggleable overlay (default `I`) showing **all items aggregated** across your Inventory + nearby StorageContainers (lockers, carryalls including ones held in inventory).
- Search filter, item counts per TechType, container counts.
- Range configurable (0 = no limit).

**Mobile Resource Scanner**
- Craft and equip a Chip that lets the HUD resource tracker scan around your current position while you hold a powered Scanner.
- Draw the Scanner and open the resource menu with the configurable mod input (default **left mouse button**), then choose the resource to track.
- Active tracking drains the held Scanner battery on each refresh; energy use is configurable from 0-200%.
- Range, scan interval, energy use, modifier, scanned-only filter and full-TechType listing are configurable.

### Compatibility

The mod detects and respects these other mods:
- **AdvancedInventory** - our (planned) scrollable inventory stays off
- **SlotExtender** - detected, extra Chip slots help with multi-chip setups

### Configuration

- **Options menu** (Options → Mods) - each feature has its own section with toggles and sliders. Changes are saved to `BepInEx/config/InferiusQoL/config.json` and applied at runtime where possible.
- **Blacklist file** at `BepInEx/plugins/InferiusQoL/Data/CompressorBlacklist.json` - edit which TechTypes the Compressor cannot shrink (fish, eggs, batteries, tanks by default).
- **Console commands** (`~` key): `qol_status` for overview, `qol_log_level` to change verbosity, and more.

### Uninstall procedure (important)

The **Inventory Compressor** uses per-instance markers stored in a JSON file. If you uninstall the mod while compressed items are still in your inventory or lockers, vanilla Subnautica will try to place them at their original (large) sizes - items that don't fit can be lost.

Before uninstalling:
1. Open the console (`~`) and run `qol_compressor_decompress_all` to clear the markers.
2. Free up space in your inventory and lockers - expect items to grow back to vanilla size.
3. Save and reload. Items take their vanilla size. Any that don't fit are lost.

### Requirements

- Subnautica (not Below Zero)
- BepInEx 5.x
- Nautilus (the modern replacement for SMLHelper)

### Credits

Built on top of BepInEx, Nautilus and Harmony. Localization contributors welcome - drop a new JSON file in `LanguageFiles/`.

---

## Český popis (pro Nexus Mods Description)

### Co mod dělá

**Inferius Quality of Life** je modulární Quality-of-Life balíček pro Subnauticu v jednom modu. Každá featura je volitelná a konfigurovatelná přímo ve hře (Options → Mods → Inferius Quality of Life). Všechny názvy položek, tooltipy a popisky jsou v angličtině i češtině.

### Featury

**Inventář**
- **Zvětšený inventář hráče** - přidání konfigurovatelných řádků/sloupců. Změna sliderů v Options se projeví ihned.
- **Větší skříně** - mění vanilla Locker a Wall Locker na vlastní rozměry (default 6×8 / 4×5). Funguje na nově postavené i existující skříně.
- **Inventory Compressor chip** *(Experimentální, od v0.3.1 dočasně skryt z craftingu)* - osaď do Chip slotu a všechny položky mimo blacklist se zmenší na 1×1. Perzistentní napříč save/load, skříněmi i vozidly. Uživatelsky editovatelný blacklist (ryby, vajíčka, baterie, lahve). **Dočasně skryt z craftingu** - lze zapnout v Options (`Craftable (Experimentální)` toggle) nebo spawnout `spawn InferiusCompressor` v konzoli. **Pozor**: po osazení a kompresi položek se vytvoří persistentní markery (`compressed-items.json`). Při odinstalaci modu bez předchozího `qol_compressor_decompress_all` může dojít k nepředvídatelnému chování / ztrátě položek. Pokud chip nikdy neosadíš, žádná persistentní data se nevytvoří - bezpečné k odinstalaci.

**Batohy** (3 tiery)
- Osazují se do Chip slotu, každý tier přidá extra řádky. Progresivní recept: Malý → Střední → Velký. Vyšší tier spotřebuje nižší + další materiály.

**Seamoth Turbo** (3 tiery + detekce efektivity)
- Vehicle modul. Sprint + modul při pilotáži = boost rychlosti s vyšší spotřebou.
- Surface falloff: boost plynule klesá jak se blížíš k hladině (žádné skoky nad vodu).
- Respektuje vanilla Vehicle Power Upgrade Module slevu na spotřebě.
- Každý tier má vlastní multiplikátor rychlosti a spotřeby v Options.

**Spojené kyslíkové lahve** (4 tiery)
- Craft ve Vylepšovací stanici. T1 spojí 2× Plasteel Tank; T2/T3/T4 vylepšují předchozí spojenou lahev.
- T1/T2/T3: dědí vanilla Plasteel penalty na rychlost plavání (váha).
- **T4 Odlehčená**: stejná kapacita jako T3 bez jakéhokoliv penalty.

**Rework baterií**
- **Reinforced Baterie / Power Cell** (default 250/500) - mid-game tier, craft ve Fabricatoru → Resources → Electronics vedle vanilla.
- **Hyper Baterie / Power Cell** (default 1500/3000) - endgame tier ve Vylepšovací stanici, recept spotřebuje Ion Baterii/Cell.

**Teleportační maják**
- Buildable interior piece v Habitat Builderu. Model mini-Aurory (mesh z Starship Souvenir).
- Klikni na maják → menu: pojmenuj ho, ukáže všechny ostatní majáky s vzdáleností a cenou, teleport na libovolný.
- Energetická cena je úměrná vzdálenosti (základ + per-100m faktor).
- **3 Efficiency čipy** (MK1/MK2/MK3) - vyrob je, osaď v UI majáku. Každý tier snižuje cenu (default 75% / 50% / 25% z plné).
- Teleport tě přesně na 2 m před cílový maják, čelem k němu - konzistentní místo dopadu.

**Stěhování skříní (Locker Mover)** *(od v0.2.0)*
- Přesuň plnou skříň bez ručního vyndávání každé položky. Zaměř plný Locker / Wall Locker / Waterproof Locker / Carryall a stiskni klávesu (default `G`) - obsah se přesune do in-memory clipboardu, skříň zůstane prázdná (připravená k demontáži).
- Zaměř novou prázdnou skříň stejného typu a stiskni klávesu znovu - obsah se přesype zpět.
- Plně kompatibilní s **Inventory Compressorem**: slisované položky zůstávají 1×1 i během přesunu.
- Single-slot clipboard. Aktuální omezení: obsah clipboardu nepřežije save/quit (v1 neperzistuje).

**AutoCraft** *(od v0.3.0, port mod EasyCraft od Wintik)*
- Fabricator/Workbench/Modification Station/Habitat Builder/Mobile Vehicle Bay čerpají suroviny z **okolních skříní** (default: Range 100m, konfigurovatelné 50-500m; nebo Inside base/pod, nebo Off).
- **Rekurzivní auto-craft** chybějících sub-ingrediencí (hloubka 5). Když máš Titanium + Lithium ale chybí Plasteel, mod ti Plasteel sám vytvoří.
- **Batch craft** - drž **Shift** (default x5) nebo **Ctrl** (default x10) při kliknutí na recept. Násobiče konfigurovatelné. Pokud nemáš dost na plný batch, scale-down na maximum které si můžeš dovolit.
- **Craft speed** slider (50-500%, default 100% = vanilla). Zrychlení = poměrně vyšší spotřeba energie.
- **Lepší tooltipy** s aktuálním počtem surovin vs. potřeba.
- **Return surplus** - přebytky z auto-craftu jdou do Inventáře nebo nejbližší Autosorter skříně (konfigurovatelné).

**Auto-doplnění kyslíku** *(od v0.3.0)*
- Rychlejší doplnění O2 tanku po vynoru nad hladinu nebo v moonpoolu/habitatu. Vanilla = 30 units/sec, default zde 120/sec (konfigurovatelné až 300).
- Toggle: doplnit **všechny lahve v inventáři**, ne jen equipnutou.
- Žádný kyslík zdarma pod vodou - refill se spouští jen tam, kde tě vanilla i tak dýchá (hladina + pressurizované interiéry).

**Přehled inventáře (Inventory Viewer)** *(od v0.4.1)*
- Hotkey toggleable overlay (default `I`) - **agregovaný seznam všech položek** napříč Inventářem + okolními StorageContainery (lockery, carryally včetně držených v inventáři).
- Search filter, počty per TechType, počty kontejnerů.
- Konfigurovatelný dosah (0 = bez limitu).

**Mobile Resource Scanner**
- Vyrob a osaď Chip, který dovolí HUD resource trackeru skenovat okolo aktuální pozice hráče, pokud držíš nabitý Scanner.
- Vezmi Scanner do ruky a menu otevři přes konfigurovatelný mod input (default **levé tlačítko myši**), potom vybereš sledovanou surovinu.
- Aktivní tracking spotřebovává baterii drženého Scanneru při každém refreshi; spotřeba je konfigurovatelná od 0 do 200%.
- Dosah, interval skenu, spotřeba, modifier, filtr jen naskenovaných položek a zobrazení všech TechTypes jsou konfigurovatelné.

### Kompatibilita

Mod detekuje a respektuje tyto další mody:
- **RamunesCustomizedStorage** - naše locker resize se nezapne, pokud ho máš
- **AdvancedInventory** - naše (plánovaná) scrollable inventář se nezapne
- **BagEquipment** - naše batohy se nezapnou
- **SlotExtender** - detekován, extra Chip sloty pomáhají s multi-chip setupem

### Konfigurace

- **Options menu** (Options → Mods) - každá featura má vlastní sekci s toggly a slidery. Změny se uloží do `BepInEx/config/InferiusQoL/config.json` a aplikují runtime kde je to možné.
- **Blacklist soubor** v `BepInEx/plugins/InferiusQoL/Data/CompressorBlacklist.json` - edituj který TechType Compressor nezmenšuje.
- **Konzolové příkazy** (`~` klávesa): `qol_status` pro přehled, `qol_log_level` pro změnu verbosity, další.

### Postup před odinstalací (důležité)

**Inventory Compressor** používá per-instance markery uložené v JSON souboru. Pokud mod odinstaluješ zatímco máš komprimované položky v inventáři nebo skříních, vanilla Subnautica se je pokusí vrátit na původní (velkou) velikost - položky, které se nevejdou, se mohou ztratit.

Před odinstalací:
1. Otevři konzoli (`~`) a spusť `qol_compressor_decompress_all` pro vymazání markerů.
2. Uvolni místo v inventáři a skříních - počítej s tím, že se věci zvětší.
3. Save a reload. Položky se rozbalí na vanilla velikost. Co se nevejde, ztratí se.

### Požadavky

- Subnautica (ne Below Zero)
- BepInEx 5.x
- Nautilus (moderní náhrada SMLHelperu)

### Poděkování

Postaveno na BepInEx, Nautilus a Harmony. Překlady do dalších jazyků vítány - stačí JSON soubor do `LanguageFiles/`.
