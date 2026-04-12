using HappyMahjong.ShopAndBag;

using strange.extensions.command.impl;
using strange.extensions.context.api;

using UnityEngine;

using Device = HappyMahjong.Common.Device;
using DeviceType = HappyMahjong.Common.DeviceType;
using MJWinStreakBallActivity;
using System.Collections.Generic;
using HappyBridge.SelectionScene;
using Unity.Collections.LowLevel.Unsafe;
using Util = HappyMahjong.Common.Util;
using HappyMahjong.FillUpBeanSpace;
using UnityEngine.Pool;

#if !compatible_758
using HappyBridge.Util;
using HappyBridge.UI;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class RspAdsCallBackCommand : StreakBallEventCommand
    {
        public override void Execute()
        {
            Log.Info("RspAdsCallBackCommand", ModuleType.StreakBall);
            var rsp = evt.data as AdsCallBackRes;
            if (rsp!=null)
            {
                var callbacks=new Common.DialogCallbacks
                {
                };
                DialogManager.GetInstance().ShowDialog(1, callbacks, LangKeys.reviveSuccess, LanguageKey.SURE);
                service.GetDetailReq();
            }
        }
    }
}

