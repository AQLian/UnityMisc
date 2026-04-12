using strange.extensions.command.impl;

using HappyMahjong.GDTAdsController;

using MJWinStreakBallActivity;


#if !compatible_758
using HappyBridge.Util;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallAdsCallBackReqCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }


        public override void Execute()
        {
            Log.Info("StreakBallAdsCallBackReqCommand", ModuleType.StreakBall);

            if (model.StreakInfo.ReviveInfo.ReviveWithAds == 1)
            {
                ADSConfigManager.PlayAds(ADSConfigManager.Key.WinStreakBall, OnWatchAdsFinish);
            }
            else
            {
                Log.Info($"streak ball callback but revive with ads is 0", ModuleType.StreakBall);
            }
        }

        private void OnWatchAdsFinish()
        {
            service.AdsCallBackReq();
        }
    }

}// 自动生成于：8/12/2025 3:37:51 PM
