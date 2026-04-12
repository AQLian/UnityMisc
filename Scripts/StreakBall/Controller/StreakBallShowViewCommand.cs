using HappyMahjong.ShopAndBag;

using strange.extensions.command.impl;
using strange.extensions.context.api;
using UnityEngine;
using Device = HappyMahjong.Common.Device;
using DeviceType = HappyMahjong.Common.DeviceType;

#if !compatible_758
using HappyBridge.Util;
using HappyBridge.UI;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallShowViewCommand : EventCommand
    {
        public override void Execute()
        {
            Log.Info("StreakBallShowViewCommand", ModuleType.StreakBall);

            dispatcher.Dispatch(StreakBallEvent.ShowStreakBall, evt.data);
        }
    }
}

