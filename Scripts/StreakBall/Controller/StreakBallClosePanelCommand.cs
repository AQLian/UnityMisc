using strange.extensions.command.impl;
using strange.extensions.context.api;
using strange.extensions.dispatcher.eventdispatcher.api;

using HappyBridge.FillUpBeanBridge;
using HappyBridge.Util;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallClosePanelCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            Log.Info("StreakBallClosePanelCommand  start", ModuleType.StreakBall);

            string json = (string) evt.data;

            Log.Info(string.Format("StreakBallClosePanelCommand data : {0}", json), ModuleType.StreakBall);

            //FillUpBeanData.PanelHookData data = LitJson.JsonMapper.ToObject<FillUpBeanData.PanelHookData>(json);

            FillUpBeanData.PanelHookData data = StreakBallUtil.ParsePanelHookDataFromBaseData(json);

            Log.Info("StreakBallClosePanelCommand parse end", ModuleType.StreakBall);

            if (data.PanelName != StreakBallConfig.ActivityPanelName)
            {
                Log.Info("StreakBallClosePanelCommand PanelName is wrong!", ModuleType.StreakBall);

                return;
            }

            dispatcher.Dispatch(StreakBallEvent.DoClosePanel);
        }
    }
}// 自动生成于：2023/6/15 17:02:15
