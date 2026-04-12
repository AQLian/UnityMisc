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
    public class RspExchangeCommand : StreakBallEventCommand
    {
        public override void Execute()
        {
            Log.Info("RspExchangeCommand", ModuleType.StreakBall);
            var rsp = evt.data as ExchangeRes;
            if (rsp != null)
            {
                model.UpdateExchangeInfo(rsp);

                dispatcher.Dispatch(StreakBallEvent.ShowCongratulation, model.ExchangeInfo);
                dispatcher.Dispatch(StreakBallEvent.DetailInfoUpdated);
                StartNextUpdateTimer();
            }
        }
    }
}

