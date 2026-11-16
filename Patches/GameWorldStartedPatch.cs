using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace DamageReport.Patches
{
    /// <summary>
    /// 战局开始时清空上一局的伤害记录。
    /// </summary>
    public sealed class GameWorldStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(
                "OnGameStarted",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void Postfix(GameWorld __instance)
        {
            try
            {
                DamageStore.BeginRaid(__instance);
            }
            catch
            {
                // 重置失败不应影响战局。
            }
        }
    }
}
