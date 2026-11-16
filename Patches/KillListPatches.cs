using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.SessionEnd;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace DamageReport.Patches
{
    /// <summary>
    /// 结算页“击杀名单”的入口：把本局所有受伤目标合并进同一个列表，
    /// 击杀过的用原生数据，受伤未死的追加在后面。
    /// </summary>
    public sealed class KillListShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(SessionResultKillList).GetMethod(
                "Show",
                new[] { typeof(SessionResultKillList.GClass3910) });
        }

        [PatchPrefix]
        private static bool Prefix(SessionResultKillList __instance, SessionResultKillList.GClass3910 controller)
        {
            try
            {
                KillListPage.BuildAndShow(__instance, controller);
                return false;
            }
            catch (Exception ex)
            {
                if (DamageReportPlugin.Log != null)
                {
                    DamageReportPlugin.Log.LogError("DamageReport: kill list build failed, fallback to vanilla: " + ex);
                }

                return true;
            }
        }
    }

    /// <summary>
    /// 每一行的文字后处理：加“已击杀 / 未击杀”标记和伤害数字。
    /// </summary>
    public sealed class KillListVictimShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(KillListVictim).GetMethod(
                "Show",
                new[] { typeof(GClass2201), typeof(bool), typeof(int) });
        }

        [PatchPostfix]
        private static void Postfix(KillListVictim __instance, GClass2201 victim)
        {
            try
            {
                KillListPage.Decorate(__instance, victim);
            }
            catch (Exception ex)
            {
                if (DamageReportPlugin.Log != null)
                {
                    DamageReportPlugin.Log.LogError("DamageReport: decorate row failed: " + ex);
                }
            }
        }
    }

    internal static class KillListPage
    {
        private static readonly HashSet<string> KilledProfileIds =
            new HashSet<string>(StringComparer.Ordinal);

        public static void BuildAndShow(SessionResultKillList screen, SessionResultKillList.GClass3910 controller)
        {
            var victims = new List<GClass2201>();
            KilledProfileIds.Clear();

            if (controller != null && controller.Victims != null)
            {
                foreach (var victim in controller.Victims)
                {
                    if (victim == null)
                    {
                        continue;
                    }

                    victims.Add(victim);
                    if (!string.IsNullOrEmpty(victim.ProfileId))
                    {
                        KilledProfileIds.Add(victim.ProfileId);
                    }
                }
            }

            var damagedOnly = DamageStore.All
                .Where(r => r != null
                            && !string.IsNullOrEmpty(r.ProfileId)
                            && r.TotalDamage >= 1f
                            && !KilledProfileIds.Contains(r.ProfileId))
                .OrderByDescending(r => r.TotalDamage)
                .ToList();

            for (var i = 0; i < damagedOnly.Count; i++)
            {
                victims.Add(damagedOnly[i].ToVictim());
            }

            var tags = controller != null ? controller.Tags : new DogtagComponent[0];
            screen.Show(new GClass1630<GClass2201>(victims), tags);

            if (DamageReportPlugin.Log != null)
            {
                DamageReportPlugin.Log.LogInfo(
                    "DamageReport: kill list rows = " + victims.Count
                    + " (killed " + KilledProfileIds.Count
                    + ", damaged-only " + damagedOnly.Count + ").");
            }
        }

        public static void Decorate(KillListVictim view, GClass2201 victim)
        {
            if (view == null || victim == null)
            {
                return;
            }

            var killed = !string.IsNullOrEmpty(victim.ProfileId)
                         && KilledProfileIds.Contains(victim.ProfileId);

            var record = DamageStore.Get(victim.ProfileId);

            var marker = killed
                ? "<color=#8CD17D>[已击杀]</color>"
                : "<color=#F1CE63>[未击杀]</color>";

            // 击杀名单里原本就有的目标（没有伤害记录，比如手雷/异常情况）：只在原文字前加标记。
            if (record == null)
            {
                if (killed)
                {
                    PrependStatus(view, marker + " ");
                }

                return;
            }

            var damageText = BuildDamageText(record);

            if (killed)
            {
                // 保留游戏原本的 “Killed / Headshot（武器、部位、距离）”，只在前面加标记、后面补伤害。
                var native = GetStatus(view);
                var status = marker;

                if (!string.IsNullOrEmpty(native) && native != "???")
                {
                    status += " " + native;
                }

                status += " <size=80%>· " + damageText + "</size>";
                SetText(view, "_status", status);
                return;
            }

            // 未击杀：原生只会显示 ???，这里用记录补上名字、等级、部位、伤害、武器、距离。
            SetText(view, "_name", record.Name);
            SetText(view, "_level", record.Level > 0 ? record.Level.ToString() : "--");

            var statusText = marker + " " + BodyPartText(record.LastPart) + " · " + damageText;

            if (!string.IsNullOrEmpty(record.WeaponName))
            {
                statusText += " · " + record.WeaponName;
            }

            if (record.Distance >= 1f)
            {
                statusText += " · " + Mathf.RoundToInt(record.Distance) + "m";
            }

            SetText(view, "_status", statusText);
        }

        private static string BuildDamageText(TargetDamage record)
        {
            var total = Mathf.RoundToInt(record.BodyDamage + record.ArmorDamage);
            var text = "总伤害 <b>" + total + "</b>";

            if (record.ArmorDamage >= 1f)
            {
                text += " <size=80%>（肉 " + Mathf.RoundToInt(record.BodyDamage)
                        + " / 甲 " + Mathf.RoundToInt(record.ArmorDamage) + "）</size>";
            }

            return text;
        }

        private static string BodyPartText(EBodyPart bodyPart)
        {
            try
            {
                return GClass2348.LocalizedShort(bodyPart, EStringCase.Lower);
            }
            catch
            {
                return bodyPart.ToString();
            }
        }

        private static string GetStatus(KillListVictim view)
        {
            var text = GetText(view, "_status");
            return text != null ? text.text : string.Empty;
        }

        private static void PrependStatus(KillListVictim view, string prefix)
        {
            var text = GetText(view, "_status");
            if (text != null)
            {
                text.text = prefix + text.text;
            }
        }

        private static void SetText(KillListVictim view, string fieldName, string value)
        {
            var text = GetText(view, fieldName);
            if (text != null)
            {
                text.text = value;
            }
        }

        private static TMP_Text GetText(KillListVictim view, string fieldName)
        {
            return Traverse.Create(view).Field(fieldName).GetValue<TMP_Text>();
        }
    }
}
