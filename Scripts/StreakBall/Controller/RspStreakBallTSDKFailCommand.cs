using strange.extensions.command.impl;
#if !compatible_758
using HappyBridge.Util;
using HappyBridge.UI;

#endif

namespace HappyMahjong.StreakBallSpace
{
    public class RspStreakBallTSDKFailCommand : EventCommand
    {
        public override void Execute()
        {
            Log.Info("RspStreakBallTSDKFailCommand", ModuleType.StreakBall);

            // 走到这里 一般是后台代码core down了
            // var rspStreakBallTsdkdo = (RspStreakBallTSDKDO) evt.data;
            
            //StreakBallUtil.ShowToast(LanguageKey.NETWORK_INSTABILITY_TRY_LATER);
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
