using HappyMahjong.FillUpBeanSpace;
using HappyMahjong.SelectionScene;

using strange.extensions.command.api;
using strange.extensions.context.impl;
using strange.extensions.dispatcher.api;
using strange.extensions.injector.api;
using strange.extensions.mediation.api;
using strange.framework.api;

#if !compatible_758
using SelectionContext = HappyMahjong.SelectionScene.SelectionContext;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallContext : IStrangeContext {

        public void InjectionBinder(ICrossContextInjectionBinder injectionBinder, MVCSContext context)
        {
            if (context is SelectionContext)
            {
                injectionBinder.Bind<StreakBallService>().To<StreakBallService>().ToSingleton().CrossContext();
                injectionBinder.Bind<StreakBallModel>().To<StreakBallModel>().ToSingleton().CrossContext();
            }
        }

        public void CommandBinder(ICommandBinder commandBinder, MVCSContext context)
        {
            if (context is SelectionContext)
            {
                commandBinder.Bind(StreakBallEvent.ReqStreakBallDetail).To<ReqStreakBallServiceCommand>();
                // 消息响应
                commandBinder.Bind(StreakBallEvent.RspGetDetail).To<RspStreakBallServiceCommand>();
                commandBinder.Bind(StreakBallEvent.RspClaimWinStreakBall).To<ClaimWinStreakBallResCommand>();
                commandBinder.Bind(StreakBallEvent.RspExchange).To<RspExchangeCommand>();
                commandBinder.Bind(StreakBallEvent.RspAdsCallBack).To<RspAdsCallBackCommand>();

                commandBinder.Bind(StreakBallEvent.ShowStreakBall).To<ShowStreakBallCommand>();
                commandBinder.Bind(StreakBallEvent.HideStreakBall).To<HideStreakBallCommand>();
                //commandBinder.Bind(StreakBallEvent.ShowDetailView).To<ShowDetailViewCommand>();

                // popup
                commandBinder.Bind(StreakBallCommonProtocolKey.JumpToStreakBall).To<ShowStreakBallViewCommand>();
                commandBinder.Bind(StreakBallCommonProtocolKey.PopupStreakBall).To<ShowPopUpViewStreakBallCmd>();

                // 请求错误
                commandBinder.Bind(StreakBallEvent.RspStreakBallFail).To<RspStreakBallTSDKFailCommand>();
                commandBinder.Bind(StreakBallEvent.PassRedDot).To<PassRedDotCommand>();
                commandBinder.Bind(ShopAndBag.ShopEvent.MyItemLoaded).To<ShopMyItemLoadCommand>();
                commandBinder.Bind(CommonProtocolKey.StreakBallShowView).To<StreakBallShowViewCommand>();
                commandBinder.Bind(CommonProtocolKey.StreakBallShowSkill).To<StreakBallShowViewCommand>();

                // 这里是起始触发点
                commandBinder.AppendBind(CommonProtocolKey.PreRequestData).To<PreloadStreakBallDataCommand>();
                commandBinder.AppendBind(HotLoadEvent.ShowIcon).To<ShowStreakBallNormalEntranceCommand>();
                commandBinder.AppendBind(HotLoadEvent.ShowPanel).To<StreakBallShowPanelCommand>();
                commandBinder.AppendBind(HotLoadEvent.ClosePanel).To<StreakBallClosePanelCommand>();

                commandBinder.Bind(StreakBallEvent.ClaimWinStreakBallReq).To<StreakBallClaimWinStreakBallReqCommand>();
                commandBinder.Bind(StreakBallEvent.ExchangeReq).To<StreakBallExchangeReqCommand>();
                commandBinder.Bind(StreakBallEvent.UseAdsReq).To<StreakBallAdsCallBackReqCommand>();
                commandBinder.Bind(StreakBallEvent.UseDiamondReq).To<StreakBallReviveWithDiamondReqCommand>();
                commandBinder.Bind(StreakBallEvent.OpenExchange).To<StreakBallOpenExchangePanelCommand>();
            }
        }

        public void MediationBinder(IMediationBinder mediationBinder, MVCSContext context)
        {
            if (context is SelectionContext)
            {
                mediationBinder.Bind<StreakBallUIView>().To<StreakBallMediator>();
            }
        }

        public void CrossContextBridgeBind(IBinder crossContextBridge,  MVCSContext context)
        {
        }

        public void UnBindCrossContextBinder(IBinder crossContextBridge, ICrossContextInjectionBinder injectionBinder)
        {
            injectionBinder.Unbind<StreakBallService>();
            injectionBinder.Unbind<StreakBallModel>();
        }

        public readonly static int TSGameSceneEnumValue = 8;
    }
}// 自动生成于：8/12/2025 3:37:51 PM
