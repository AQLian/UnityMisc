using strange.extensions.command.impl;

#if !compatible_758
using HappyBridge.Util;
#endif
using MJWinStreakBallActivity;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallExchangeReqCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }


        public override void Execute()
        {
            Log.Info("StreakBallExchangeReqCommand", ModuleType.StreakBall);

            if(evt.data is ExchangeItem vo)
            {
                service.ExchangeReq(vo.Id);
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
