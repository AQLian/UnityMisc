using strange.extensions.command.impl;

using System.Collections.Generic;


#if !compatible_758
using HappyBridge.Util;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class ReqStreakBallServiceCommand : EventCommand
    {
        [Inject] public StreakBallService service { get; set; }
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            Log.Info("ReqStreakBallServiceCommand", ModuleType.StreakBall);
            if(evt.data is List<uint> others)
            {
                service.GetDetailReq(others);
            }
            else
            {
                service.GetDetailReq();
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
