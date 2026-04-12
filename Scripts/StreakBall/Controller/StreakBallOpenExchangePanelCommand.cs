using strange.extensions.command.impl;

#if !compatible_758
using HappyBridge.Util;
#endif
namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallOpenExchangePanelCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }

        public override void Execute()
        {
            Log.Info("StreakBallOpenExchangePanelCommand", ModuleType.StreakBall);
            var exchangeHandler = StreakBallPopupHandler.CreatePopupHandler<StreakBallExchangeHandler>("StreakBallExchangePanel");
            if (exchangeHandler != null)
            {
                exchangeHandler.Init(model.Info);
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
