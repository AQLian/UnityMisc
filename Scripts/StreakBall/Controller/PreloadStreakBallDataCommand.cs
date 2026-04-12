using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Configuration;
using Hall.ShopAndBag;

using HappyMahjong.Common;
using HappyMahjong.ShopAndBag;
using strange.extensions.command.impl;
using UnityEngine;



namespace HappyMahjong.StreakBallSpace
{
    public class PreloadStreakBallDataCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        [Inject] public StreakBallService service { get; set; }


        public override void Execute()
        {
            Log.Info("PreloadStreakBallDataCommand", ModuleType.StreakBall);
            service.GetDetailReq();

            // 这里拉取所有StreakBall配置
            var skillonfigs = ProtoConfigLoader<TalentSkillItemConfig>.getInstance().getAllCachedConfig();
            var hasAnyItem = skillonfigs.Count > 0;
            Log.Info($"StreakBall has any items: {hasAnyItem.ToString()}", ModuleType.StreakBall);
            StreakBallUtil.SetHasAnyItems(hasAnyItem);

            // preload assets
            AppRoot.GetInstance().StartCoroutine(LoadAdditionItems());
        }

        private IEnumerator LoadAdditionItems()
        {
            yield return new WaitForEndOfFrame();
        }
    }
}
