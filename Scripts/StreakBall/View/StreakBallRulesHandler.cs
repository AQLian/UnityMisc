using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HappyBridge.Audio;
using HappyBridge.UI;
using ItemIconHandlerUgui = HappyMahjong.Common.ItemIconHandlerUgui;
using BubbleBehaviour = HappyMahjong.Common.BubbleBehaviour;
using BackComponent = HappyMahjong.Setting.BackComponent;
using PopUpType = HappyMahjong.Common.PopUpType;
using HappyBridge.Util;


namespace StreakBallSpace
{
    public class StreakBallRulesHandler : StreakBallPopupHandler
    {
        public void Init()
        {
            //返回按钮
            BindPopupBackBtn(transform.Find("GamePanel/Btn_Close"));

            //设置规则
            SetRules();
        }

        private void SetRules()
        {
            var label = transform.Find("GamePanel/Scrollview/Content/Label");
            if (label != null)
            {
                var rules = DynamicConfig.GetInstance().GetString(UIDef.ConfigKey, "Rule");
                Util.SetUIText(label , rules);
            }
        }
    }
}
