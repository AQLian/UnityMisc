using HappyMahjong.ShopAndBag;

using strange.extensions.command.impl;
using strange.extensions.context.api;

using UnityEngine;

using Device = HappyMahjong.Common.Device;
using DeviceType = HappyMahjong.Common.DeviceType;

using TalentPavillion;

using System.Security.Cryptography;



#if !compatible_758
using HappyBridge.Util;
using HappyBridge.UI;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallShowCustomSelectableCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        [Inject] public StreakBallService service { get; set; }

        public override void Execute()
        {
            Log.Info("StreakBallShowCustomSelectableCommand", ModuleType.StreakBall);
        }
    }
}

