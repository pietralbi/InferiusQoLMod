[size=6][b]Inferius Quality of Life[/b][/size]

A modular Quality-of-Life package for Subnautica. One mod, many improvements - all configurable, all can be toggled individually.

[line]

[size=5][b]English description (for Nexus Mods Description tab)[/b][/size]

[size=4][b]What it does[/b][/size]

[b]Inferius Quality of Life[/b] bundles the most-requested QoL tweaks into a single mod. Every feature is optional and configurable in-game (Options -> Mods -> Inferius Quality of Life). All item names, tooltips and labels are localized (English + Czech).

[size=4][b]Features[/b][/size]

[b]Inventory[/b]
[list]
[*][b]Enlarge player inventory[/b] - add configurable rows/columns. Runtime updates when you move the slider.
[*][b]Bigger lockers[/b] - resize vanilla Locker and Wall Locker to custom dimensions (default 6x8 / 4x5). Works both on newly-built and existing lockers.
[*][b]Inventory Compressor chip[/b] [i](Experimental, temporarily hidden from crafting since v0.3.1)[/i] - equip in Chip slot to shrink every non-blacklisted item to 1x1. Persistent across save/load, base storage, vehicles. User-editable blacklist (fish, eggs, batteries, tanks). [b]Hidden from crafting by default[/b] - toggle [code]Craftable (Experimental)[/code] in Options or [code]spawn InferiusCompressor[/code] in console. [b]Warning[/b]: once equipped, compressed items get persistent markers. Uninstalling without running [code]qol_compressor_decompress_all[/code] first may cause item loss. If you never equip the chip, no persistent data is created - safe to uninstall.
[/list]

[b]Backpacks[/b] (3 tiers)
[list]
[*]Equip in Chip slot for extra inventory rows. Progressive recipe chain: Small -> Medium -> Large. Each tier consumes the previous + new materials.
[/list]

[b]Seamoth Turbo[/b] (3 tiers + efficiency handling)
[list]
[*]Vehicle module. Hold Sprint while piloting + module installed = speed boost with higher drain.
[*]Surface falloff: boost smoothly fades as you approach the surface (no more "jumping out of water" glitches).
[*]Respects vanilla Vehicle Power Upgrade Module discount.
[*]Each tier has its own speed/drain multiplier in config.
[/list]

[b]Merged Oxygen Tanks[/b] (4 tiers)
[list]
[*]Craft in Modification Station. T1 combines 2x Plasteel Tank; T2/T3/T4 upgrade the previous merged tank.
[*]T1/T2/T3: inherit vanilla Plasteel speed penalty (weight).
[*][b]T4 Lightweight[/b]: same capacity as T3 but no speed penalty.
[/list]

[b]Battery Rework[/b]
[list]
[*][b]Reinforced Battery / Power Cell[/b] (default 250/500) - mid-game tier, craftable in Fabricator -> Resources -> Electronics alongside vanilla.
[*][b]Hyper Battery / Power Cell[/b] (default 1500/3000) - endgame tier in Modification Station, consumes Ion Battery/Cell in recipe.
[/list]

[b]Teleport Beacon[/b]
[list]
[*]Buildable interior piece in Habitat Builder. Uses the Aurora mini-model (Starship Souvenir mesh).
[*]Click the beacon to open a menu: rename it, see all other beacons with distance + energy cost, teleport to any of them.
[*]Energy cost is distance-based (base cost + per-100m factor).
[*][b]3 Efficiency Chips[/b] (MK1/MK2/MK3) - craft them and install directly in the beacon UI. Each tier reduces teleport cost (default 75% / 50% / 25% of base).
[*]Teleport drops you exactly 2m in front of the target beacon, facing it - consistent landing.
[/list]

[b]Locker Mover[/b] [i](since v0.2.0)[/i]
[list]
[*]Relocate full lockers without manually unloading every item. Point at a full Locker / Wall Locker / Waterproof Locker / Carryall, press the keybind (default [code]G[/code]) - the contents move to an in-memory clipboard and the locker becomes empty (ready to deconstruct).
[*]Point at a new empty locker of the same type and press the key again - contents drop back in.
[*]Fully compatible with the [b]Inventory Compressor[/b]: compressed items stay 1x1 throughout the move.
[*]Single-slot clipboard. Current limitation: contents are lost on save/quit while the clipboard is non-empty (v1 does not persist).
[/list]

[b]AutoCraft[/b] [i](since v0.3.0, port of EasyCraft by Wintik)[/i]
[list]
[*]Fabricator/Workbench/Modification Station/Habitat Builder/Mobile Vehicle Bay pull ingredients from [b]nearby storage[/b] (default: Range 100m, configurable 50-500m; or Inside base/pod, or Off).
[*][b]Recursive auto-craft[/b] of missing sub-ingredients (depth 5). If you have Titanium + Lithium but miss Plasteel, it crafts the Plasteel for you.
[*][b]Batch craft[/b] - hold [b]Shift[/b] (default x5) or [b]Ctrl[/b] (default x10) while clicking a recipe. Multipliers configurable. If you don't have enough for the full batch, scales down to the maximum you can afford.
[*][b]Craft speed[/b] slider (50-500%, default 100% = vanilla). Faster crafting drains proportionally more energy.
[*][b]Better ingredient tooltips[/b] - shows current count vs required.
[*][b]Return surplus[/b] - leftover crafted items go to Inventory or nearest Autosorter locker (configurable).
[/list]

[b]Oxygen Auto-Refill[/b] [i](since v0.3.0)[/i]
[list]
[*]Faster oxygen tank refill when you surface above water or enter a moonpool/habitat. Vanilla rate is 30 units/sec; default here is 120/sec (configurable up to 300).
[*]Toggle: refill [b]all oxygen tanks in your inventory[/b], not just the equipped one.
[*]No free oxygen underwater - refill only triggers where vanilla already breathes you (surface + pressurised interiors).
[/list]

[b]Inventory Viewer[/b] [i](since v0.4.1)[/i]
[list]
[*]Hotkey-toggleable overlay (default [code]I[/code]) showing [b]all items aggregated[/b] across your Inventory + nearby StorageContainers (lockers, carryalls including ones held in inventory).
[*]Search filter, item counts per TechType, container counts.
[*]Range configurable (0 = no limit).
[/list]

[size=4][b]Compatibility[/b][/size]

The mod detects and respects these other mods:
[list]
[*][b]AdvancedInventory[/b] - our (planned) scrollable inventory stays off
[*][b]SlotExtender[/b] - detected, extra Chip slots help with multi-chip setups
[/list]

[size=4][b]Configuration[/b][/size]

[list]
[*][b]Options menu[/b] (Options -> Mods) - each feature has its own section with toggles and sliders. Changes are saved to [code]BepInEx/config/InferiusQoL/config.json[/code] and applied at runtime where possible.
[*][b]Blacklist file[/b] at [code]BepInEx/plugins/InferiusQoL/Data/CompressorBlacklist.json[/code] - edit which TechTypes the Compressor cannot shrink (fish, eggs, batteries, tanks by default).
[*][b]Console commands[/b] ([code]~[/code] key): [code]qol_status[/code] for overview, [code]qol_log_level[/code] to change verbosity, and more.
[/list]

[size=4][b]Uninstall procedure (important)[/b][/size]

The [b]Inventory Compressor[/b] uses per-instance markers stored in a JSON file. If you uninstall the mod while compressed items are still in your inventory or lockers, vanilla Subnautica will try to place them at their original (large) sizes - items that don't fit can be lost.

Before uninstalling:
[list]
[*]Open the console ([code]~[/code]) and run [code]qol_compressor_decompress_all[/code] to clear the markers.
[*]Free up space in your inventory and lockers - expect items to grow back to vanilla size.
[*]Save and reload. Items take their vanilla size. Any that don't fit are lost.
[/list]

[size=4][b]Requirements[/b][/size]

[list]
[*]Subnautica (not Below Zero)
[*]BepInEx 5.x
[*]Nautilus (the modern replacement for SMLHelper)
[/list]

[size=4][b]Credits[/b][/size]

Built on top of BepInEx, Nautilus and Harmony. Localization contributors welcome - drop a new JSON file in [code]LanguageFiles/[/code].

[line]

[size=5][b]Cesky popis (pro Nexus Mods Description)[/b][/size]

[size=4][b]Co mod dela[/b][/size]

[b]Inferius Quality of Life[/b] je modularni Quality-of-Life balicek pro Subnauticu v jednom modu. Kazda featura je volitelna a konfigurovatelna primo ve hre (Options -> Mods -> Inferius Quality of Life). Vsechny nazvy polozek, tooltipy a popisky jsou v anglictine i cestine.

[size=4][b]Featury[/b][/size]

[b]Inventar[/b]
[list]
[*][b]Zvetseny inventar hrace[/b] - pridani konfigurovatelnych radku/sloupcu. Zmena slideru v Options se projevi ihned.
[*][b]Vetsi skrine[/b] - meni vanilla Locker a Wall Locker na vlastni rozmery (default 6x8 / 4x5). Funguje na nove postavene i existujici skrine.
[*][b]Inventory Compressor chip[/b] [i](Experimentalni, od v0.3.1 docasne skryt z craftingu)[/i] - osad do Chip slotu a vsechny polozky mimo blacklist se zmensi na 1x1. Perzistentni napric save/load, skrinemi i vozidly. Uzivatelsky editovatelny blacklist (ryby, vajicka, baterie, lahve). [b]Docasne skryt z craftingu[/b] - lze zapnout v Options ([code]Craftable (Experimentalni)[/code] toggle) nebo spawnout [code]spawn InferiusCompressor[/code] v konzoli. [b]Pozor[/b]: po osazeni se vytvori persistentni markery. Pri odinstalaci bez predchoziho [code]qol_compressor_decompress_all[/code] muze dojit ke ztrate polozek. Pokud chip nikdy neosadis, zadna persistentni data se nevytvori - bezpecne k odinstalaci.
[/list]

[b]Batohy[/b] (3 tiery)
[list]
[*]Osazuji se do Chip slotu, kazdy tier prida extra radky. Progresivni recept: Maly -> Stredni -> Velky. Vyssi tier spotrebuje nizsi + dalsi materialy.
[/list]

[b]Seamoth Turbo[/b] (3 tiery + detekce efektivity)
[list]
[*]Vehicle modul. Sprint + modul pri pilotazi = boost rychlosti s vyssi spotrebou.
[*]Surface falloff: boost plynule klesa jak se blizis k hladine (zadne skoky nad vodu).
[*]Respektuje vanilla Vehicle Power Upgrade Module slevu na spotrebe.
[*]Kazdy tier ma vlastni multiplikator rychlosti a spotreby v Options.
[/list]

[b]Spojene kyslikove lahve[/b] (4 tiery)
[list]
[*]Craft ve Vylepsovaci stanici. T1 spoji 2x Plasteel Tank; T2/T3/T4 vylepsuji predchozi spojenou lahev.
[*]T1/T2/T3: dedi vanilla Plasteel penalty na rychlost plavani (vaha).
[*][b]T4 Odlehcena[/b]: stejna kapacita jako T3 bez jakehokoliv penalty.
[/list]

[b]Rework baterii[/b]
[list]
[*][b]Reinforced Baterie / Power Cell[/b] (default 250/500) - mid-game tier, craft ve Fabricatoru -> Resources -> Electronics vedle vanilla.
[*][b]Hyper Baterie / Power Cell[/b] (default 1500/3000) - endgame tier ve Vylepsovaci stanici, recept spotrebuje Ion Baterii/Cell.
[/list]

[b]Teleportacni majak[/b]
[list]
[*]Buildable interior piece v Habitat Builderu. Model mini-Aurory (mesh z Starship Souvenir).
[*]Klikni na majak -> menu: pojmenuj ho, ukaze vsechny ostatni majaky s vzdalenosti a cenou, teleport na libovolny.
[*]Energeticka cena je umerna vzdalenosti (zaklad + per-100m faktor).
[*][b]3 Efficiency cipy[/b] (MK1/MK2/MK3) - vyrob je, osad v UI majaku. Kazdy tier snizuje cenu (default 75% / 50% / 25% z plne).
[*]Teleport te presne na 2 m pred cilovy majak, celem k nemu - konzistentni misto dopadu.
[/list]

[b]Stehovani skrini (Locker Mover)[/b] [i](od v0.2.0)[/i]
[list]
[*]Presun plnou skrin bez rucniho vyndavani kazde polozky. Zamer plny Locker / Wall Locker / Waterproof Locker / Carryall a stiskni klavesu (default [code]G[/code]) - obsah se presune do in-memory clipboardu, skrin zustane prazdna (pripravena k demontazi).
[*]Zamer novou prazdnou skrin stejneho typu a stiskni klavesu znovu - obsah se presype zpet.
[*]Plne kompatibilni s [b]Inventory Compressorem[/b]: slisovane polozky zustavaji 1x1 i behem presunu.
[*]Single-slot clipboard. Aktualni omezeni: obsah clipboardu neprezije save/quit (v1 neperzistuje).
[/list]

[b]AutoCraft[/b] [i](od v0.3.0, port modu EasyCraft od Wintik)[/i]
[list]
[*]Fabricator/Workbench/Modification Station/Habitat Builder/Mobile Vehicle Bay cerpaji suroviny z [b]okolnich skrini[/b] (default: Range 100m, konfigurovatelne 50-500m; nebo Inside base/pod, nebo Off).
[*][b]Rekurzivni auto-craft[/b] chybejicich sub-ingredienci (hloubka 5). Kdyz mas Titanium + Lithium ale chybi Plasteel, mod ti Plasteel sam vytvori.
[*][b]Batch craft[/b] - drz [b]Shift[/b] (default x5) nebo [b]Ctrl[/b] (default x10) pri kliknuti na recept. Nasobice konfigurovatelne. Pokud nemas dost na plny batch, scale-down na maximum ktere si muzes dovolit.
[*][b]Craft speed[/b] slider (50-500%, default 100% = vanilla). Zrychleni = pomerne vyssi spotreba energie.
[*][b]Lepsi tooltipy[/b] s aktualnim poctem surovin vs. potreba.
[*][b]Return surplus[/b] - prebytky z auto-craftu jdou do Inventare nebo nejblizsi Autosorter skrine (konfigurovatelne).
[/list]

[b]Auto-doplneni kysliku[/b] [i](od v0.3.0)[/i]
[list]
[*]Rychlejsi doplneni O2 tanku po vynoru nad hladinu nebo v moonpoolu/habitatu. Vanilla = 30 units/sec, default zde 120/sec (konfigurovatelne az 300).
[*]Toggle: doplnit [b]vsechny lahve v inventari[/b], ne jen equipnutou.
[*]Zadny kyslik zdarma pod vodou - refill se spousti jen tam, kde te vanilla i tak dycha (hladina + pressurizovane interiery).
[/list]

[b]Prehled inventare (Inventory Viewer)[/b] [i](od v0.4.1)[/i]
[list]
[*]Hotkey toggleable overlay (default [code]I[/code]) - [b]agregovany seznam vsech polozek[/b] napric Inventarem + okolnimi StorageContainery (lockery, carryally vc. drzenych v inventari).
[*]Search filter, pocty per TechType, pocty kontejneru.
[*]Konfigurovatelny dosah (0 = bez limitu).
[/list]

[size=4][b]Kompatibilita[/b][/size]

Mod detekuje a respektuje tyto dalsi mody:
[list]
[*][b]RamunesCustomizedStorage[/b] - nase locker resize se nezapne, pokud ho mas
[*][b]AdvancedInventory[/b] - nase (planovana) scrollable inventar se nezapne
[*][b]BagEquipment[/b] - nase batohy se nezapnou
[*][b]SlotExtender[/b] - detekovan, extra Chip sloty pomahaji s multi-chip setupem
[/list]

[size=4][b]Konfigurace[/b][/size]

[list]
[*][b]Options menu[/b] (Options -> Mods) - kazda featura ma vlastni sekci s toggly a slidery. Zmeny se ulozi do [code]BepInEx/config/InferiusQoL/config.json[/code] a aplikuji runtime kde je to mozne.
[*][b]Blacklist soubor[/b] v [code]BepInEx/plugins/InferiusQoL/Data/CompressorBlacklist.json[/code] - edituj ktery TechType Compressor nezmensuje.
[*][b]Konzolove prikazy[/b] ([code]~[/code] klavesa): [code]qol_status[/code] pro prehled, [code]qol_log_level[/code] pro zmenu verbosity, dalsi.
[/list]

[size=4][b]Postup pred odinstalaci (dulezite)[/b][/size]

[b]Inventory Compressor[/b] pouziva per-instance markery ulozene v JSON souboru. Pokud mod odinstalujes zatimco mas komprimovane polozky v inventari nebo skrinich, vanilla Subnautica se je pokusi vratit na puvodni (velkou) velikost - polozky ktere se nevejdou se mohou ztratit.

Pred odinstalaci:
[list]
[*]Otevri konzoli ([code]~[/code]) a spust [code]qol_compressor_decompress_all[/code] pro vymazani markeru.
[*]Uvolni misto v inventari a skrinich - pocitej s tim ze se veci zvetsi.
[*]Save a reload. Polozky se rozbali na vanilla velikost. Co se nevejde ztrati se.
[/list]

[size=4][b]Pozadavky[/b][/size]

[list]
[*]Subnautica (ne Below Zero)
[*]BepInEx 5.x
[*]Nautilus (moderni nahrada SMLHelperu)
[/list]

[size=4][b]Podekovani[/b][/size]

Postaveno na BepInEx, Nautilus a Harmony. Preklady do dalsich jazyku vitany - staci JSON soubor do [code]LanguageFiles/[/code].
