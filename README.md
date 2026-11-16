# SPT Damage Report

在**战局结束后的击杀名单页**里，把本局所有被你打伤过的目标合并进同一个列表，并显示：

- `[已击杀]` / `[未击杀]` 标记
- **保留游戏原本的击杀信息**：`Killed / Headshot（武器、受击部位、距离）`
- 实际肉伤（`DidBodyDamage`）
- 被护甲吸收的伤害（`DidArmorDamage`）

没打死的人也会出现在列表里（排在击杀目标后面，按总伤害从高到低）。

## 效果示意

```
#   地图    时间      名字    等级  阵营   状态
1   Woods   12:31:07  Killa   34    BOSS   [已击杀] Killed (M4A1, 头部, 32m) · 总伤害 500（肉 380 / 甲 120）
2   Woods   12:29:44  Scav    12    SCAV   [未击杀] 胸腔 · 总伤害 240（肉 200 / 甲 40）· M4A1 · 55m
3   Woods   12:28:10  PMC     21    USEC   [未击杀] 头部 · 总伤害 95（肉 55 / 甲 40）· AK-74M · 18m
```

## 原理

游戏内部本来就有一条“对敌人造成伤害”的回调链：

```
Player.ApplyDamageInfo
  -> ActiveHealthController.ApplyDamage  (得出实际扣血 DidBodyDamage)
  -> ManageAggressor
  -> 开枪者.StatisticsManager.OnEnemyDamage(...)
```

SPT 单人和 Fika 的主机/客户端都会走这里（Fika 的 `ObservedPlayer.ManageAggressor` 也调用同一个方法），
所以只需在 `LocationStatisticsCollectorAbstractClass.OnEnemyDamage` 上挂一个后置补丁，按
`ProfileId` 聚合即可。结算页的原生击杀名单来自 `Profile.EftStats.Victims`，本 mod 在
`SessionResultKillList.Show` 里把两者合并，再用 `KillListVictim.Show` 后置改文字，
因此界面字体、布局、返回/下一步都是游戏原生的。

## 编译

需要 **.NET SDK 8.0**（或 9.0），不需要 Visual Studio。

安装 SDK（任选其一）：

```powershell
winget install Microsoft.DotNet.SDK.8
```

或者从 <https://dotnet.microsoft.com/download/dotnet/8.0> 下载安装。

工程目标框架是 `net472`。你这台机器上已经装了 .NET Framework 4.7.2 的 targeting pack
（`C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2`），
所以**不需要联网 restore NuGet 包**。如果换到没有 targeting pack 的机器，把
`DamageReport.csproj` 里注释掉的那行 `Microsoft.NETFramework.ReferenceAssemblies` 取消注释即可。

然后在本目录执行（二选一）：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

或者直接双击 `build.cmd`。

如果你的游戏不在默认路径：

```powershell
pwsh -File .\build.ps1 -GameRoot 'D:\Games\SPT\Client.0.16.9.0.40087'
```

或者直接用 dotnet：

```powershell
dotnet build .\DamageReport.csproj -c Release -p:GameRoot="C:\Game\EscapeFromTarkov\Client.0.16.9.0.40087"
```

编译产物会自动输出到：

```
<GameRoot>\BepInEx\plugins\DamageReport\DamageReport.dll
```

如果只想编译、不部署，可以加 `-p:OutputPath=.\bin\`。

## 安装

1. 确认游戏是 SPT 4.0.x / EFT 0.16.9。
2. 编译后确认 `BepInEx\plugins\DamageReport\DamageReport.dll` 存在。
3. 进游戏打一局，打伤几个人（不必打死），撤离或死亡后在“击杀名单”页查看。

> `dist\DamageReport.dll` 是最近一次成功编译的副本（.NET SDK 8 生成，和自动部署到 `BepInEx\plugins\DamageReport\` 的完全一致），
> 可以直接复制去用。`build-csc.ps1` 是不装 SDK 时的备用编译方式（只能 C# 5 语法）。
>
> 注意：`.ps1` 脚本内容故意保持纯英文/ASCII。Windows PowerShell 5.1（`powershell.exe`）会把无 BOM 的 UTF-8
> 当成 ANSI 读取，脚本里如果有中文会变成乱码并导致语法错误；要加中文请把文件另存为“UTF-8 带 BOM”。

## 配置

首次运行会生成 `BepInEx\config\com.tkv.damagereport.cfg`：

- `Show friendly damage`（默认 false）：Fika 组队时是否显示对同队队友造成的伤害。

## 已知限制

- **锁定 EFT 0.16.9 / SPT 4.0.x**。用到的 `GClass2201`、`GClass3910` 等是混淆名，游戏版本更新后可能变化。
- **手雷/爆炸伤害**：Fika **客户端**身份下，爆炸伤害在本地只转发不结算，可能不会出现在列表里；
  单人/主机身份正常。后续可以补爆炸路径。
- 只统计**你作为攻击者**的伤害；环境伤害、自己打自己不计。
- 数值口径：单人/主机路径的肉伤是“实际扣血”（打死最后一枪会被剩余血量截断）；
  Fika 客户端路径是穿透后的伤害原值，可能略高。
- 结算页标题仍然是游戏原来的“击杀名单”，mod 只改行内容和行数。
- 如果这一局没打伤任何人，页面和原版完全一致。

## 排错

BepInEx 控制台（`BepInEx\LogOutput.log`）里搜索 `DamageReport`：

- `DamageReport 1.0.0 loaded.`：插件加载成功
- `raid start, records cleared.`：战局开始重置成功
- `kill list rows = N (killed X, damaged-only Y).`：结算页合并成功
- 如果有 `OnEnemyDamage failed` / `kill list build failed`，把完整异常发出来

## 目录结构

```
SPT-DamageReport/
  DamageReport.csproj
  Plugin.cs                       BepInEx 入口 + 配置
  DamageStore.cs                  按 ProfileId 聚合伤害
  Patches/OnEnemyDamagePatch.cs   伤害采集
  Patches/GameWorldStartedPatch.cs 每局重置
  Patches/KillListPatches.cs      结算页合并 + 行文字
  build.ps1
```
