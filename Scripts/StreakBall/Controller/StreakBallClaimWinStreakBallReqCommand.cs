using strange.extensions.command.impl;

using TalentPavillion;


#if !compatible_758
using HappyBridge.Util;
#endif
namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallClaimWinStreakBallReqCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }


        public override void Execute()
        {
            Log.Info("StreakBallClaimWinStreakBallReqCommand", ModuleType.StreakBall);
            service.ClaimWinStreakBallReq();
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
