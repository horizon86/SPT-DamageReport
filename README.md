# SPT Damage Report

![Damage Report screenshot](docs/screenshot.png)

**English** | [中文](#中文)

Shows every enemy you damaged in the post-raid kill list — including the ones you did **not** kill — together with the damage you dealt.

## Features

- Damaged-but-not-killed targets are added to the end-of-raid kill list.
- Each target shows the total damage you dealt (body damage + armor damage).
- Names follow the game's default rules: SCAV / bosses are shown, USEC / BEAR stay `???` without a dogtag.

## Compatibility

SPT 4.0.x · EFT 0.16.9.0.40087 · BepInEx 5.4.x · Fika 2.x

## Installation

1. Download `DamageReport.zip` from [Releases](../../releases/latest).
2. Extract it into your SPT game root (the folder containing `EscapeFromTarkov.exe`).
3. The plugin ends up at `BepInEx/plugins/DamageReport/DamageReport.dll`.

## Build

Requires [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet build .\DamageReport.csproj -c Release -p:GameRoot="C:\path\to\your\SPT\client"
```

Output: `<GameRoot>\BepInEx\plugins\DamageReport\DamageReport.dll`

---

## 中文

把**没打死的敌人**也显示在战局结算的击杀名单里，并显示你对他造成的伤害。

### 功能

- 结算名单中会追加所有被你打伤、但没打死的目标。
- 每个目标显示你造成的总伤害（肉伤 + 护甲吸收）。
- 名字显示规则和游戏默认一致：SCAV / Boss 显示名字，USEC / BEAR 没狗牌显示 `???`。

### 适用版本

SPT 4.0.x · EFT 0.16.9.0.40087 · BepInEx 5.4.x · Fika 2.x

### 安装

1. 从 [Releases](../../releases/latest) 下载 `DamageReport.zip`。
2. 解压到游戏根目录（包含 `EscapeFromTarkov.exe` 的那一层）。
3. 插件会位于 `BepInEx/plugins/DamageReport/DamageReport.dll`。

### 编译

需要 [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)。

```powershell
dotnet build .\DamageReport.csproj -c Release -p:GameRoot="C:\你的\SPT\客户端\路径"
```

输出：`<GameRoot>\BepInEx\plugins\DamageReport\DamageReport.dll`
