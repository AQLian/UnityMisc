using System;
using System.Collections;
using System.Collections.Generic;

using com.tencent.pandora;

using Configuration;

using HappyMahjong.Common;

using TalentPavillion;

using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public static class StreakBallExtensions
    {
        /// <summary>
        /// 同时检查当前灵珠状态和技能状态
        /// </summary>
        /// <param name="info">灵珠信息</param>
        /// <param name="state">装备状态</param>
        /// <param name="skillState">技能状态</param>
        /// <param name="remain">技能状态的附加字段</param>
        public static void GetOrbEquipState(this OrbInfo info, out OrbEquipState state, out OrbSkillState skillState, out long totalLimitOrEndTime)
        {
            totalLimitOrEndTime = 0;
            state = OrbEquipState.Equiped;
            if (info.IsOwned == 0)
            {
                state = OrbEquipState.NotOwned;
            }
            else if (info.IsEquipped == 0)
            {
                state = OrbEquipState.NotEquiped;
            }

            skillState = OrbSkillState.CanUse;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (info.ExpireTime != 0 && now > info.ExpireTime)
            {
                totalLimitOrEndTime = info.ExpireTime;
                skillState = OrbSkillState.Expired;
            }
            else if (info.TotalUseLimit != 0 && info.UsedCount > info.TotalUseLimit)
            {
                totalLimitOrEndTime = info.TotalUseLimit;
                skillState = OrbSkillState.ExceedLimit;
            }
            if (info.CooldownEndTime == 0)
            {
                skillState = OrbSkillState.CooldownPaused;
            }
            else if (info.CooldownEndTime > 0 && info.CooldownEndTime > now)
            {
                totalLimitOrEndTime = info.CooldownEndTime;
                skillState = OrbSkillState.Cooldown;
            }
        }


        public static int CanSelectableItem(this OrbInfo info) 
        {
            if (info.SkillType == (int) SkillType.SkillTypeEntryCardGiftChoose)
            {
                info.GetOrbEquipState(out var equipState, out var skillState, out var remain);
                if(info.PendingItems != null && info.PendingItems.Count > 0)
                {
                    if (skillState == OrbSkillState.CanUse)
                    {
                        return 0;
                    }
                    else if(skillState == OrbSkillState.Cooldown)
                    {
                        return (int)remain;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 天赋技能当前不可用状态
        /// </summary>
        /// <param name="info">灵珠信息</param>
        /// <param name="stateStr">当前技能状态文本</param>
        /// <param name="hint">点击后提示文本</param>
        /// <returns></returns>
        public static bool IsSkillInvalid(this OrbInfo info, out string stateStr, out string hint)
        {
            stateStr = "";
            hint = "";
            return true;
        }

        /// <summary>
        /// 获取天赋技能客户端配置
        /// </summary>
        /// <param name="info"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool TryGetTalentSkillItemConfig(this OrbInfo info, out TalentSkillItemConfig config)
        {
            if(info == null)
            {
                config = null;
                return false;
            }

            config = ProtoConfigLoader<TalentSkillItemConfig>.getInstance().getConfigByKey(info.ItemId);
            return config != null;
        }

        /// <summary>
        /// 是否空槽位
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool IsEmpty(this SlotInfo info) => (info ==null) ||  (info != null && info.Status == (int) SlotStatus.SlotStatusEmpty);
    }
}
