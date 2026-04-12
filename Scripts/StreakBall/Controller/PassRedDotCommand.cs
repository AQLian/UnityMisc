using HappyMahjong.Common;

using strange.extensions.command.impl;

using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public class PassRedDotCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            Log.Info("PassRedDotCommand", ModuleType.StreakBall);
            if (evt.data is GameObject go)
            {
                model.EntryIcon = go;
            }
            else
            {
                Log.Info("PassRedDotCommand evt.data is not go", ModuleType.StreakBall);
            }
        }
    }
}
