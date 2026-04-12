using strange.extensions.command.impl;
using HappyMahjong.SelectionScene;
using HappyBridge.UI;
using HappyMahjong.ShopAndBag;
using UnityEngine;
using MainUIStateCenter = HappyBridge.UI.MainUIStateCenter;
#if !compatible_758
using HappyBridge.Util;
#endif
namespace HappyMahjong.StreakBallSpace
{
    public class ShowDetailViewCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }
        [Inject] public IShopModel shopModel { get; set; }

        private static StreakBallUIView UIView { get; set; }

        public override void Execute()
        {
            Log.Info("ShowDetailViewCommand", ModuleType.StreakBall);
            if(evt.data != null && evt.data is int orbId)
            {

                if (UIView != null)
                {
                    Log.Info($"trying to open talent hall view , but already exist!", ModuleType.StreakBall);
                    return;
                }

                if (MainUIStateCenter.GetInstance().currentState == (int) MainUIState.Shop)
                {
                    dispatcher.Dispatch(ShopUIEvent.CheckItemNeedSave);
                }

                dispatcher.Dispatch(CommonProtocolKey.StreakBallOnShowView);
                StreakBallUtil.SetShopModel(shopModel);
                {
                    var prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "StreakBall");
                    var go = UIUtil.Instantiate(prefab);
                    go.name = "StreakBallUIView";
                    var sceneRoot = GameObject.Find("SelectionSceneRoot");
                    if (sceneRoot)
                    {
                        go.transform.SetParent(sceneRoot.transform, true);
                    }

                    var view = go.GetOrAddComponent<StreakBallUIView>();
                    view.shopModel = shopModel;
                    view.showArgument = evt.data;
                    model.ViewCreated = true;
                    UIView = view;
                }
            }
            else
            {
                Log.Info($"ShowDetailViewCommand invalid param: {evt.data}", ModuleType.StreakBall);
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
