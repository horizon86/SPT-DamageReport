using System;
using System.Collections.Generic;
using EFT;

namespace DamageReport
{
    /// <summary>
    /// 单个目标身上的累计伤害。
    /// </summary>
    public sealed class TargetDamage
    {
        public string ProfileId;
        public string AccountId = "";
        public string Name = "";
        public EPlayerSide Side;
        public WildSpawnType Role;
        public int Level;
        public int PrestigeLevel;
        public bool IsAI;

        /// <summary>实际扣掉的血量（DidBodyDamage）。</summary>
        public float BodyDamage;

        /// <summary>被护甲吸收的伤害（DidArmorDamage）。</summary>
        public float ArmorDamage;

        public int Hits;
        public string WeaponKey = "";
        public string WeaponName = "";
        public float Distance;
        public EBodyPart LastPart;
        public TimeSpan FirstHitTime;
        public string Location = "";

        public float TotalDamage
        {
            get { return BodyDamage + ArmorDamage; }
        }

        public GClass2201 ToVictim()
        {
            return new GClass2201
            {
                ProfileId = ProfileId,
                AccountId = AccountId,
                Name = Name,
                Side = Side,
                Time = FirstHitTime,
                Level = Level,
                PrestigeLevel = PrestigeLevel,
                BodyPart = LastPart,
                Weapon = WeaponKey,
                Distance = Distance,
                Role = Role,
                Location = Location
            };
        }
    }

    /// <summary>
    /// 本局伤害记录。战局开始时清空，结算页读取。
    /// </summary>
    public static class DamageStore
    {
        private static readonly Dictionary<string, TargetDamage> Records =
            new Dictionary<string, TargetDamage>(StringComparer.Ordinal);

        private static readonly List<TargetDamage> Ordered = new List<TargetDamage>();

        private static DateTime _raidStartUtc = DateTime.UtcNow;
        private static string _location = "";
        private static GameWorld _currentWorld;

        public static IReadOnlyList<TargetDamage> All
        {
            get { return Ordered; }
        }

        public static string Location
        {
            get { return _location; }
        }

        /// <summary>
        /// 兜底：如果 GameWorldStartedPatch 没生效，第一次造成伤害时也会按新战局重置。
        /// </summary>
        public static void EnsureWorld(GameWorld world)
        {
            if (world != null && !ReferenceEquals(world, _currentWorld))
            {
                BeginRaid(world);
            }
        }

        public static void BeginRaid(GameWorld world)
        {
            _currentWorld = world;
            Records.Clear();
            Ordered.Clear();
            _raidStartUtc = DateTime.UtcNow;
            _location = world != null ? world.LocationId : "";

            if (DamageReportPlugin.Log != null)
            {
                DamageReportPlugin.Log.LogInfo("DamageReport: raid start, records cleared.");
            }
        }

        public static TargetDamage Get(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return null;
            }

            TargetDamage record;
            Records.TryGetValue(profileId, out record);
            return record;
        }

        public static void Add(
            string profileId,
            string name,
            string accountId,
            EPlayerSide side,
            WildSpawnType role,
            int level,
            int prestigeLevel,
            bool isAi,
            float bodyDamage,
            float armorDamage,
            string weaponKey,
            string weaponName,
            float distance,
            EBodyPart bodyPart)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            TargetDamage record;
            if (!Records.TryGetValue(profileId, out record))
            {
                record = new TargetDamage
                {
                    ProfileId = profileId,
                    FirstHitTime = DateTime.UtcNow - _raidStartUtc,
                    Location = _location
                };
                Records[profileId] = record;
                Ordered.Add(record);
            }

            if (!string.IsNullOrEmpty(name))
            {
                record.Name = name;
            }

            if (string.IsNullOrEmpty(record.Name))
            {
                record.Name = "未知目标";
            }

            if (!string.IsNullOrEmpty(accountId))
            {
                record.AccountId = accountId;
            }

            record.Side = side;
            record.Role = role;
            record.Level = level;
            record.PrestigeLevel = prestigeLevel;
            record.IsAI = isAi;

            if (bodyDamage > 0f)
            {
                record.BodyDamage += bodyDamage;
            }

            if (armorDamage > 0f)
            {
                record.ArmorDamage += armorDamage;
            }

            record.Hits++;
            record.LastPart = bodyPart;

            if (distance > record.Distance)
            {
                record.Distance = distance;
            }

            if (!string.IsNullOrEmpty(weaponKey))
            {
                record.WeaponKey = weaponKey;
                record.WeaponName = weaponName;
            }
        }
    }
}
