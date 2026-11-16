# Development notes

Implementation details for **SPT Damage Report**. Player-facing documentation lives in [README.md](README.md).

## Overview

The mod is a single client-side BepInEx plugin (`DamageReport.dll`, .NET Framework 4.7.2). It does not ship a server component and does not reference Fika.

Data flow:

```
Player.ApplyDamageInfo (victim)
  -> ActiveHealthController.ApplyDamage  => damageInfo.DidBodyDamage (actual HP loss)
  -> ManageAggressor
  -> aggressor.StatisticsManager.OnEnemyDamage(victim info, damageInfo, distance, ...)
```

`OnEnemyDamage` is the game's own "damage dealt to an enemy" sink. It is reached for shots, melee and (in single-player / host) explosions. The statistics manager that receives it belongs to the player who dealt the damage, so the mod only needs to filter `damage.Player.IsYourPlayer` and aggregate by victim `ProfileId`.

## Hook points

| Patch | Target | Purpose |
|---|---|---|
| `OnEnemyDamagePatch` | `LocationStatisticsCollectorAbstractClass.OnEnemyDamage` (postfix) | Collect per-target damage |
| `GameWorldStartedPatch` | `GameWorld.OnGameStarted` (postfix) | Clear records at raid start |
| `KillListShowPatch` | `SessionResultKillList.Show(GClass3910)` (prefix) | Merge kills + damaged-only targets |
| `KillListVictimShowPatch` | `KillListVictim.Show(GClass2201, bool, int)` (prefix + postfix) | Name rule + row text |

`DamageStore.EnsureWorld` also resets when the `GameWorld` instance changes, as a fallback if `OnGameStarted` is not patched successfully.

### Collection

`OnEnemyDamage` parameters used:

- `damage.DidBodyDamage` — HP actually removed
- `damage.DidArmorDamage` — damage absorbed by armor
- `damage.Weapon` — `Item.ShortName` (localization key) and `GClass2348.LocalizedShortName(item)`
- `playerProfileId`, `playerSide`, `role`, `groupId`, `distance`
- `EBodyPart bodyPart` — last hit body part

Victim `Player` is resolved through `GameWorld.GetEverExistedPlayerByID(profileId)` to capture `Profile.Nickname`, `AccountId`, `Info.Level`, `PrestigeLevel`, `IsAI`.

Teammate damage (non-empty `groupId` equal to `MainPlayer.GroupId`) is skipped unless `Show friendly damage` is enabled.

### Kill list merge

`Profile.EftStats.Victims` is the source of the vanilla kill list and only contains targets credited to you. `KillListShowPatch` builds a combined list:

1. every existing `GClass2201` from `controller.Victims` (killed by you);
2. every `TargetDamage` whose `ProfileId` is not in that set (damaged but not killed by you), sorted by total damage.

It then calls the two-argument `SessionResultKillList.Show(GClass1630<GClass2201>, DogtagComponent[])` directly, which reuses the game's own row binder, layout and buttons. The original one-argument `Show` only delegates to this overload, so skipping it is safe.

### Row text

`KillListVictim.Show(victim, knownName, number)` renders name, level, faction and status. The plugin:

- **Prefix**: for victims not in the killed set, forces `knownName = false`, i.e. "no dogtag". The vanilla method then applies its own rule `knownName = knownName || Side == Savage`, so names/levels stay identical to a killed target without a dogtag.
- **Postfix**: rewrites only `_status` through `Traverse` (`_status` is a `CustomTextMeshProUGUI`).

Status formats:

- killed: `[已击杀] <native Killed/Headshot text> · 总伤害 X（肉 A / 甲 B）`
- not killed: `[未击杀] <body part> · 总伤害 X（肉 A / 甲 B）· weapon · distance`

`GClass945.GetCorrectedNickname(profileData)` is what transliterates Cyrillic bot nicknames (`Тагилла` -> `Tagilla`); the plugin does not call it directly, it just lets the vanilla renderer do the work.

## Fika notes

- Shots: on a client, hits on observed players go through `ObservedPlayer.ApplyShot` / `ApplyClientShot`, both of which apply damage locally and call `ManageAggressor`, so `OnEnemyDamage` fires on the shooter's client.
- Host: `FikaBot.ApplyShot` calls the base `Player.ApplyDamageInfo`, which also reaches `OnEnemyDamage`.
- Explosions: `GClass2085` fragments go through the normal ballistics path and are recorded. Blast damage uses `BodyPartCollider.ApplyEnvironmentalDamage` -> `IPlayerBridge.ApplyDamageInfo`. On a Fika **client** that bridge (`ObservedClientBridge`) only records the damage without calling `ManageAggressor`, so client-side grenade blast damage can be missing.
- Teammate kills do not enter your `EftStats.Victims` (`FikaPlayer.HandleTeammateKill` only checks quest kill conditions), so they show up as `[未击杀]`, consistent with "not killed by you".

## Version-specific game types

These obfuscated names are compiled against **EFT 0.16.9.0.40087** and will change with game updates:

- `GClass2201` — victim entry (`Name`, `Side`, `Role`, `Level`, `BodyPart`, `Weapon`, `Distance`, `Time`, `Location`)
- `SessionResultKillList.GClass3910` — kill list controller (`Victims`, `Tags`)
- `GClass1630<T>` — victim collection passed to `Show`
- `GClass2348` — `LocalizedShort(EBodyPart, EStringCase)`, `LocalizedShortName(Item)`
- `GClass945.GetCorrectedNickname` — vanilla nickname correction (used indirectly)

To re-verify after an EFT update, `Mono.Cecil.dll` ships with SPT (`SPT/Mono.Cecil.dll`) and can be used to dump method signatures and IL without a decompiler.

## Build

`DamageReport.csproj`:

- `net472`, `LangVersion 9.0`
- `GameRoot` property defaults to `C:\Game\EscapeFromTarkov\Client.0.16.9.0.40087`
- output goes to `$(GameRoot)\BepInEx\plugins\DamageReport`
- references `Assembly-CSharp`, `Comfort`, Unity modules, `Unity.TextMeshPro`, `BepInEx`, `0Harmony`, `spt-reflection`
- additionally references `netstandard`, `Sirenix.Serialization`, `DissonanceVoip`, `ItemComponent.Types`, `ItemTemplate.Types` because `Assembly-CSharp` exposes types from them

`build.ps1` / `build.cmd` wrap:

```powershell
dotnet build .\DamageReport.csproj -c Release -p:GameRoot="..."
```

`build-csc.ps1` is a no-SDK fallback that compiles the same sources with the legacy compiler shipped with Windows (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`). That compiler only supports C# 5, so the sources intentionally avoid newer syntax; keep it that way if the fallback must keep working.

The build needs the .NET Framework 4.7.2 targeting pack (installed with Visual Studio or the Windows SDK). If it is missing, uncomment the `Microsoft.NETFramework.ReferenceAssemblies` `PackageReference` in the csproj.

## Packaging a release

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\package.ps1 -Version 1.0.0
```

It builds the plugin (unless `-SkipBuild` is passed), stages the DLL, and writes `dist/DamageReport-v1.0.0.zip` with the game-root layout:

```
DamageReport-v1.0.0.zip
  BepInEx/
    plugins/
      DamageReport/
        DamageReport.dll
```

Players extract the zip into the SPT game root. `package.cmd` is a double-click wrapper.

The README expects the screenshot at `docs/screenshot.png`.

## Debugging

Set the BepInEx console/log level to Info and search `BepInEx/LogOutput.log` for `DamageReport`:

```
DamageReport 1.0.0 loaded.
DamageReport: raid start, records cleared.
DamageReport: kill list rows = N (killed X, damaged-only Y).
```

Errors are logged as `DamageReport: OnEnemyDamage failed`, `kill list build failed`, `decorate row failed`.

If a patch target cannot be resolved, the plugin logs `failed to enable <patch>` and keeps running with the remaining patches.

## Known issues / TODO

- Grenade blast damage on Fika clients (see above).
- In-game labels are Chinese only; add localization keys or a config-driven language.
- The body part shown for a damaged-only target is the **last hit** part, not the most damaged one. Could be changed to a per-part breakdown.
- `dist/` and `obj/` are build artifacts and are git-ignored.
