using strange.extensions.command.impl;

#if !compatible_758
using HappyBridge.Util;
#endif
namespace HappyMahjong.StreakBallSpace
{
    public class ShopMyItemLoadCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService svr { get; set; }


        public override void Execute()
        {
            Log.Info("ShopMyItemLoadCommand", ModuleType.StreakBall);

            var changed = (bool)evt.data;
            if(! model.ViewCreated && changed)
            {
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
