using HappyMahjong.ShopAndBag;

using strange.extensions.command.impl;
using strange.extensions.context.api;

using UnityEngine;

using Device = HappyMahjong.Common.Device;
using DeviceType = HappyMahjong.Common.DeviceType;

using MJWinStreakBallActivity;
using UnityEngine.Pool;
using System;

#if !compatible_758
using HappyBridge.Util;
using HappyBridge.UI;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class ClaimWinStreakBallResCommand : StreakBallEventCommand
    {
        public override void Execute()
        {
            Log.Info("ClaimWinStreakBallResCommand", ModuleType.StreakBall);

            var rsp = evt.data as ClaimWinStreakBallRes;
            if (rsp != null)
            {
                model.UpdateStreakInfo(rsp);

                dispatcher.Dispatch(StreakBallEvent.DetailInfoUpdated);
                StartNextUpdateTimer();
            }
        }
    }
}

