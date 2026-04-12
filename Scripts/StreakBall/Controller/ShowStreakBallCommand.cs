using HappyMahjong.SelectionScene;
using HappyMahjong.ShopAndBag;

using strange.extensions.command.impl;
using strange.extensions.context.api;

using UnityEngine;

using Device = HappyMahjong.Common.Device;
using DeviceType = HappyMahjong.Common.DeviceType;
using MainUIStateCenter = HappyBridge.UI.MainUIStateCenter;

#if !compatible_758
using HappyBridge.Util;
using HappyBridge.UI;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class ShowStreakBallCommand : EventCommand
    {
        [Inject(ContextKeys.CONTEXT_VIEW)] public GameObject contextView { get; set; }

        [Inject] public StreakBallModel model { get; set; }
        
        [Inject] public IShopModel shopModel { get; set; }

        private static StreakBallUIView UIView { get; set; }

        public override void Execute()
        {
            Log.Info("ShowStreakBallCommand", ModuleType.StreakBall);
            if (!model.IsOpen())
            {
                return;
            }
            if (!model.CheckTempOpen())
            {
                return;
            }

            if (MainUIStateCenter.GetInstance().currentState == (int) MainUIState.Shop)
            {
                dispatcher.Dispatch(ShopUIEvent.CheckItemNeedSave);
            }

            dispatcher.Dispatch(CommonProtocolKey.StreakBallOnShowView);
            StreakBallUtil.SetShopModel(shopModel);
            if(UIView == null)
            {
                var prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "StreakBall");
                var go = UIUtil.Instantiate(prefab);
                go.name = "StreakBallUIView";
                var instance = PopUpManager.GetInstance().AddPopUp(go, HappyMahjong.Common.PopUpType.UGUI);
                var adapter = instance.GetComponent<HappyMahjong.Common.iPhoneXAdapter>();
                if (adapter != null)
                {
                    adapter.UpdateAdapter();
                }
                var view = go.GetOrAddComponent<StreakBallUIView>();
                view.shopModel = shopModel;
                view.showArgument = evt.data;
                model.ViewCreated = true;
                UIView = view;
            }
            else
            {
                UIView.showArgument = evt.data;
                UIView.TryShowingDetailView();
            }

            // update red dot
            if (!Tutorial.TutorialController.GetInstance().IsTutorialDisplayed(HappyMahjong.Tutorial.FlagIndexNew.ShowStreakBallFirstOnline))
            {
                HappyMahjong.Tutorial.TutorialController.GetInstance().AddTutorialFlag(HappyMahjong.Tutorial.FlagIndexNew.ShowStreakBallFirstOnline - 1);
            }

            model.UpdateRedDot();
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
