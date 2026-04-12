using Configuration;
using Configuration;

using strange.extensions.command.impl;

using UnityEngine;
using UnityEngine.UI;

using HappyMahjong.Audio;

using HappyMahjong.SelectionScene;

using Sirenix.OdinInspector.Editor.Validation;

using System.Collections.Generic;



#if !compatible_758
using MainUIState = HappyMahjong.SelectionScene.MainUIState;
using MainUIStateCenter = HappyMahjong.SelectionScene.MainUIStateCenter;
using LeftBarIconBtnType = HappyMahjong.SelectionScene.LeftBarIconBtnType;
using Message = HappyMahjong.Message;
using SelectionAnmiateSytle = HappyMahjong.SelectionScene.SelectionAnmiateSytle;

using HappyBridge.Util;
using HappyBridge.UI;
using MJWinStreakBallActivity;

using PopUpType = HappyMahjong.Common.PopUpType;
#endif


namespace HappyMahjong.StreakBallSpace
{
    public class ShowStreakBallTopRightEntranceCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            Log.Info("ShowStreakBallEntranceCommand", ModuleType.StreakBall);
            var data = evt.data as GetDetailRes;

            //CheckIsValid(data);
            if (Util.IsInTimeSpan((uint) data.StartTime , (uint)data.EndTime))
            {
                var bResReady = HappyMahjong.ResHotUpdate.ActivityResHelper.GetInstance().IsResourceReady(UIDef.ConfigKey);
                var showEntrance = DynamicConfig.GetInstance().GetBool(UIDef.ConfigKey, "ShowEntrance", true);

                if (!bResReady)
                {
                    return;
                }

                if (!showEntrance)
                {
                    return;
                }

                if (model.iconTopRight == null)
                {
                    string iconName = DynamicConfig.GetInstance().GetString(UIDef.ConfigKey, "StreakBallTopRightIcon", "StreakBallTopRightIcon");
                    Log.Info($"AddTopRightMiddleIcon StreakBall: {iconName}", ModuleType.StreakBall);
                    LocalAddTopRightMiddleIcon(UIUtil.LoadPrefab(UIDef.StreakBallABPath, iconName));
                    StreakBallUtil.RecordEvent((int) ReportEvent.StreakBallTopRightBtnExposed);
                }
            }
        }

        private void LocalAddTopRightMiddleIcon(GameObject iconPrefab)
        {
            if (iconPrefab == null)
            {
                Log.Info("AddTopRightMiddleIcon load icon fail!", ModuleType.StreakBall);
                return;
            }

            // 注册icon
            GameObject iconGO = UIUtil.Instantiate(iconPrefab);

            UIEventListener.Get(iconGO).onClick = (go) =>
            {
                //点击上报
                AudioController.Instance.PlayAuto("ui_click", AudioLayers.Oneshot);
                PlayerStatistics.GetInstance().RecordMessage((int) SNSType.ButtonClick, (int) ReportButtonType.RankBattlePassIconClick, 1, MainUIStateCenter.GetInstance().currentState);
                dispatcher.Dispatch(StreakBallEvent.ShowStreakBall);

                var bubbleObj = iconGO.transform.Find("Bubble");
                if (bubbleObj != null)
                {
                    bubbleObj.gameObject.SetActive(false);
                }
                RankedBattlePassSpace.RankedBattlePassUtil.RecordRankedBPBubbleShow();
            };

            model.iconTopRight = iconGO;

            if (!TopRightMiddleIconHandler.GetInstance().AddIcon(iconGO, "SteakBall"))
            {
                iconGO.SetActive(false);
                Object.Destroy(iconGO);
                model.iconTopRight = null;
            }
            else
            {
                //补充上报
                PlayerStatistics.GetInstance().RecordMessage((int) SNSType.GeneralEvent, (int) ReportButtonType.RankBattlePassIconClick, 1, MainUIStateCenter.GetInstance().currentState);
            }
        }
    }
}
