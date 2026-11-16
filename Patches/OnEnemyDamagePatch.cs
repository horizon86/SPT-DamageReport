using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using SPT.Reflection.Patching;

namespace DamageReport.Patches
{
    /// <summary>
    /// 游戏自己的“对敌人造成伤害”回调：
    /// Player.ApplyDamageInfo -> ManageAggressor -> 开枪者.StatisticsManager.OnEnemyDamage(...)
    /// 这是 SPT 与 Fika 客户端/主机都会走的统一汇聚点，参数里带目标 ProfileId / 阵营 / 角色 /
    /// 距离 / 满血，且 DamageInfoStruct 里已有实际扣血（DidBodyDamage）和护甲吸收（DidArmorDamage）。
    /// </summary>
    public sealed class OnEnemyDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(LocationStatisticsCollectorAbstractClass).GetMethod(
                "OnEnemyDamage",
                BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void Postfix(
            LocationStatisticsCollectorAbstractClass __instance,
            DamageInfoStruct damage,
            EBodyPart bodyPart,
            string playerProfileId,
            EPlayerSide playerSide,
            WildSpawnType role,
            string groupId,
            float fullHealth,
            bool isHeavyDamage,
            float distance,
            int hour,
            List<string> targetEquipment,
            HealthEffects enemyEffects,
            List<string> zoneIds)
        {
            try
            {
                if (damage.Player == null || damage.Player.iPlayer == null)
                {
                    return;
                }

                if (!damage.Player.iPlayer.IsYourPlayer)
                {
                    return;
                }

                if (!Singleton<GameWorld>.Instantiated)
                {
                    return;
                }

                var world = Singleton<GameWorld>.Instance;
                if (world == null)
                {
                    return;
                }

                DamageStore.EnsureWorld(world);

                if (world.LocationId == "hideout")
                {
                    return;
                }

                var mainPlayer = world.MainPlayer;
                if (mainPlayer == null || playerProfileId == mainPlayer.ProfileId)
                {
                    return;
                }

                if (!DamageReportPlugin.ShowFriendlyDamage.Value
                    && !string.IsNullOrEmpty(groupId)
                    && groupId == mainPlayer.GroupId)
                {
                    return;
                }

                Player victim = null;
                try
                {
                    victim = world.GetEverExistedPlayerByID(playerProfileId);
                }
                catch
                {
                    // 目标可能已经销毁，忽略，用回调参数兜底。
                }

                var name = victim != null ? victim.Profile.Nickname : "未知目标";
                var accountId = victim != null ? victim.Profile.AccountId : "";
                var side = victim != null ? victim.Profile.Info.Side : playerSide;
                var victimRole = victim != null ? victim.Profile.Info.Settings.Role : role;
                var level = victim != null ? victim.Profile.Info.Level : 0;
                var prestigeLevel = victim != null ? victim.Profile.PrestigeLevel : 0;
                var isAi = victim != null && victim.IsAI;

                var weaponKey = damage.Weapon != null ? damage.Weapon.ShortName : "";
                var weaponName = damage.Weapon != null ? GClass2348.LocalizedShortName(damage.Weapon) : "";

                DamageStore.Add(
                    playerProfileId,
                    name,
                    accountId,
                    side,
                    victimRole,
                    level,
                    prestigeLevel,
                    isAi,
                    damage.DidBodyDamage,
                    damage.DidArmorDamage,
                    weaponKey,
                    weaponName,
                    distance,
                    bodyPart);
            }
            catch (Exception ex)
            {
                if (DamageReportPlugin.Log != null)
                {
                    DamageReportPlugin.Log.LogError("DamageReport: OnEnemyDamage failed: " + ex);
                }
            }
        }
    }
}
