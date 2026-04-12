using SSRItemUpgrade;

using MJWinStreakBallActivity;
using HappyMahjong.Benefit;


#if !compatible_758
using HappyBridge.Util;
#endif

namespace HappyMahjong.StreakBallSpace
{

    public class RspStreakBallServiceCommand : StreakBallEventCommand
    {
        public override void Execute()
        {
            HappyMahjong.Common.Log.Info("RspStreakBallServiceCommand", ModuleType.StreakBall);

            var data = evt.data as GetDetailRes;
            if(data != null)
            {
                model.QueryInfoRsp(data);

                dispatcher.Dispatch(StreakBallEvent.DetailInfoUpdated);
                StartNextUpdateTimer();
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
