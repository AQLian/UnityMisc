using Configuration;

using strange.extensions.command.impl;

using UnityEngine;
using UnityEngine.UI;

using HappyMahjong.Audio;

using HappyMahjong.SelectionScene;

using Sirenix.OdinInspector.Editor.Validation;

using System.Collections.Generic;

using static HappyBridge.FillUpBeanBridge.FillUpBeanData;


#if !compatible_758
using MainUIState = HappyMahjong.SelectionScene.MainUIState;
using MainUIStateCenter = HappyMahjong.SelectionScene.MainUIStateCenter;
using LeftBarIconBtnType = HappyMahjong.SelectionScene.LeftBarIconBtnType;
using Message = HappyMahjong.Message;
using SelectionAnmiateSytle = HappyMahjong.SelectionScene.SelectionAnmiateSytle;

using HappyBridge.Util;
using HappyBridge.UI;
using PopupSourceType = HappyMahjong.FillUpBeanSpace.PopupSourceType;

using PopUpType = HappyMahjong.Common.PopUpType;
#endif


namespace HappyMahjong.StreakBallSpace
{
    public class ShowStreakBallNormalEntranceCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            if (!model.IsOpen())
            {
                return;
            }

            KeyValuePair<GameObject, RspFillUpBeanHotloadProtoData> data = (KeyValuePair<GameObject, RspFillUpBeanHotloadProtoData>) evt.data;

            if (data.Key == null || data.Value == null || string.IsNullOrEmpty(data.Value.rspFillUpBeanServiceDataJson))
            {
                return;
            }

            var protoData = data.Value;
            var serverData = StreakBallUtil.ParseMainArenaServiceDataJson(protoData.rspFillUpBeanServiceDataJson);

            Log.Info(string.Format(" - ShowStreakBallNormalEntranceCommand PanelName : {0}", serverData.PanelName), ModuleType.StreakBall);
            if (serverData.PanelName != StreakBallConfig.ActivityIconName)
            {
                return;
            }

            string prefabName = "StreakBallEntrance";
            var prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, prefabName);
            if (prefab != null)
            {
                var btn = UIUtil.Instantiate(prefab);
                btn.name = serverData.PanelName;

                btn.transform.SetParent(data.Key.transform, false);
                UIEventListener.Get(btn).onClick = (goArg) =>
                {
                    dispatcher.Dispatch(StreakBallEvent.ShowStreakBall);
                };

                // UpdateRedDotState(btn.transform);
                Log.Info(string.Format("ShowStreakBallNormalEntranceCommand data : {0}", data.Value.rspFillUpBeanServiceDataJson), ModuleType.StreakBall);

                // icon曝光
                StreakBallUtil.RecordEventWithReportStr((int) ReportEvent.StreakBallNormalBtnExposed, new List<string>
                {
                    serverData.SceneID > 0 ? serverData.SceneID.ToString() : serverData.GameType.ToString(),
                });
            }
        }
    }
}
