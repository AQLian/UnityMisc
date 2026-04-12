using strange.extensions.command.impl;

#if !compatible_758
using HappyBridge.Util;
#endif
namespace HappyMahjong.StreakBallSpace
{
    public class HideStreakBallCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            Log.Info("HideStreakBallCommand", ModuleType.StreakBall);

            model.ViewCreated = false;
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
