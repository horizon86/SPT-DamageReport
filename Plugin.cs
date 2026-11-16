using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DamageReport.Patches;

namespace DamageReport
{
    [BepInPlugin("com.tkv.damagereport", "Damage Report", "1.0.0")]
    public class DamageReportPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        /// <summary>
        /// 是否在结算页显示对队友造成的伤害（Fika 组队时）。默认关闭。
        /// </summary>
        internal static ConfigEntry<bool> ShowFriendlyDamage;

        private void Awake()
        {
            Log = Logger;

            ShowFriendlyDamage = Config.Bind(
                "General",
                "Show friendly damage",
                false,
                "在结算页显示对同队队友造成的伤害（仅 Fika 组队时有意义）。");

            // 某个补丁目标找不到时不要让整个插件挂掉。
            TryEnable(new OnEnemyDamagePatch());
            TryEnable(new KillListShowPatch());
            TryEnable(new KillListVictimShowPatch());
            TryEnable(new GameWorldStartedPatch());

            Log.LogInfo("DamageReport 1.0.0 loaded.");
        }

        private static void TryEnable(SPT.Reflection.Patching.ModulePatch patch)
        {
            try
            {
                patch.Enable();
            }
            catch (System.Exception ex)
            {
                Log.LogError("DamageReport: failed to enable " + patch.GetType().Name + ": " + ex);
            }
        }
    }
}
