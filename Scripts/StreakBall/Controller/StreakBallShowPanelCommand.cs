using System.Collections.Generic;
using System.IO;
using System.Text;

using HappyBridge.UI;
using HappyBridge.Util;

using HappyMahjong.Common;
using HappyMahjong.FillUpBeanSpace;
using HappyMahjong.ReturningActivity;
using HappyMahjong.StreakBallSpace;

using ProtoBuf;

using strange.extensions.command.impl;
using strange.extensions.context.api;
using strange.extensions.dispatcher.eventdispatcher.api;

using UnityEngine;

using Log = HappyBridge.Util.Log;
using PopUpManager = HappyBridge.UI.PopUpManager;
using RspFillUpBeanHotloadProtoData = HappyBridge.FillUpBeanBridge.FillUpBeanData.RspFillUpBeanHotloadProtoData;
using SceneSwitch = HappyMahjong.Loading.SceneSwitch;
using UIUtil = HappyBridge.UI.UIUtil;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallShowPanelCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }

        public override void Execute()
        {
            if (!model.IsOpen())
            {
                return;
            }
            if (!model.CheckTempOpen())
            {
                return;
            }

            RspFillUpBeanHotloadProtoData protoData = null;

            if (evt.data != null)
            {
                protoData = evt.data as RspFillUpBeanHotloadProtoData;
            }

            // 主域的ShowPanel触发的，主域给了触发数据（分礼包版本）
            if (protoData != null)
            {
                MainArenaServiceDataJson data = StreakBallUtil.ParseMainArenaServiceDataJson(protoData.rspFillUpBeanServiceDataJson);

                Log.Info(string.Format("StreakBallShowPanelCommand data by MainArena: {0} PanelName: {1}", protoData.rspFillUpBeanServiceDataJson, data.PanelName), ModuleType.StreakBall);

                if (data.PanelName != StreakBallConfig.ActivityPanelName)
                {
                    return;
                }

                if (!StreakBallUtil.CheckCanReactShowPanel(data.SceneID))
                {
                    DispatchOpenPanelFailHook(data);

                    return;
                }


                // 加几个不能弹窗的判断
                var popupSourceType = data.PopupSourceType;
                if (popupSourceType > 0)
                {
                    if (StreakBallUtil.IsInGameSourceType(popupSourceType))
                    {
                        // 局内弹窗，需要在游戏场景才触发
                        if (SceneSwitch.CurrentScene() != SceneSwitch.Scene.Game && (int) SceneSwitch.CurrentScene() != StreakBallContext.TSGameSceneEnumValue)
                        {
                            Log.Info($"StreakBallShowPanelCommand source:{popupSourceType} 不在局内收到了弹窗protoData消息", ModuleType.StreakBall);

                            return;
                        }
                    }
                    else
                    {
                        // 局外弹窗，需要在主场景才触发
                        if (SceneSwitch.CurrentScene() != SceneSwitch.Scene.Main)
                        {
                            Log.Info($"StreakBallShowPanelCommand source:{popupSourceType} 不在局外收到了弹窗protoData消息", ModuleType.StreakBall);

                            return;
                        }
                    }
                }

                dispatcher.Dispatch(StreakBallEvent.ShowStreakBall);
                DispatchOpenPanelRetHook(data);
                Log.Info($"StreakBallShowPanelCommand by MainArena model", ModuleType.StreakBall);
            }
        }

        private void DispatchOpenPanelRetHook(MainArenaServiceDataJson data)
        {
            if (data != null)
            {
                PanelHookData hookData = new PanelHookData();
                hookData.PanelType = (int) HLMJBankruptControl.ActivityUpdateType.HOT_UPDATE;
                hookData.PanelName = data.PanelName;
                hookData.PopupSourceType = data.PopupSourceType;
                hookData.ResultCode = 0;
                var hookJson = LitJson.JsonMapper.ToJson(hookData);
                dispatcher.Dispatch(HotLoadEvent.OpenPanelRetHook, hookJson);
                Log.Info("EnterRoomCardShelvesHandler dispatch OpenPanelRetHook，data: " + hookJson, ModuleType.StreakBall);
            }
        }

        private void DispatchOpenPanelFailHook(MainArenaServiceDataJson data)
        {
            PanelHookData hookData = new PanelHookData();
            hookData.PanelType = (int) HLMJBankruptControl.ActivityUpdateType.HOT_UPDATE;
            hookData.PanelName = data.PanelName;
            hookData.PopupSourceType = data.PopupSourceType;
            hookData.ResultCode = -1;

            string hookJson = LitJson.JsonMapper.ToJson(hookData);

            dispatcher.Dispatch(HotLoadEvent.OpenPanelRetHook, hookJson);

            Log.Info("ShowEnterRoomCardShelvesCommand dispatch OpenPanelRetHook，data: " + hookJson, ModuleType.StreakBall);
        }
    }
}// 自动生成于：2023/6/15 17:02:15
