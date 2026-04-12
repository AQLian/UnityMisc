namespace HappyMahjong.StreakBallSpace
{
    public enum StreakBallEvent
    {
        // 逻辑事件，对应dispatcher|bubble.ContextDispatcher
        ShowStreakBall,
        HideStreakBall,

        ReqStreakBallDetail,


        // 请求响应
        RspGetDetail,
        RspClaimWinStreakBall,
        RspExchange,
        RspAdsCallBack,
        GameEndNotify,

        RspStreakBallFail,

        // UI事件, 对应view.dispatcher|bubble.BubbleDispatch
        ShowView,
        // UI整体刷新
        DetailInfoUpdated,

        ClaimWinStreakBallReq,
        ExchangeReq,
        OpenExchange,
        UseAdsReq,
        UseDiamondReq,

        GotoLinkEvent,
        TryRegisterChildView,
        TryRemoveChildView,
        SelectableGetSuccess,

        PassRedDot,
        LogoutClear,
        
        DoClosePanel,
        ShowCongratulation,
        StreakBallBuyDiamondResult,
    }
}// 自动生成于：8/12/2025 3:37:51 PM
