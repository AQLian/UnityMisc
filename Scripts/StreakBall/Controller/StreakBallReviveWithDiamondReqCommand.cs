using strange.extensions.command.impl;

using HappyMahjong.Loading;

using HappyMahjong.BuyBeans;
using HappyMahjong.ReturningActivity;


#if !compatible_758
using HappyBridge.Util;
#endif

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallReviveWithDiamondReqCommand : EventCommand
    {
        [Inject] public StreakBallModel model { get; set; }
        [Inject] public StreakBallService service { get; set; }

        public override void Execute()
        {
            var vo = evt.data as StreakBallReviveWithDiamondVO;
            if (vo == null)
            {
                return;
            }
            var data = vo.Data;
            var diamond_price = vo.Diamond;
            var package_id = vo.PackageId;

            var diamondNum = GetCurDiamond();
            if (diamondNum >= diamond_price)
            {
               //todo! buy request
            }
            //否则提示充值钻石
            else
            {
                string moneyPrice;
                string payID;
                int modifiedDiamondsToBuy;
                int extra;
                bool isFirst;
                if (BuyBeansController.GetInstance()
                    .GetLuckyCardSupplyDiamondDataWithCurDiamond(diamond_price, diamondNum, out moneyPrice, out payID,
                        out modifiedDiamondsToBuy,
                        out extra,
                        out isFirst))
                {
                    var sceneID = StreakBallUtil.IsUseSceneIDSourceType(data.PopupSourceType) ? data.SceneID : data.GameType;
                    var pfExt = HappyBridge.Pay.DirectBuy.GetInstance().GetDirectBuyPfExt("winstreakballactivity", data.BuyBeansFrom, sceneID, HappyBridge.Util.ServerTime.GetInstance().GetServerTime().ToString());
                    m_nCurBuyCount = 1;
                    m_totalPrice = diamond_price;
                    m_packageId = package_id;
                    BuyBeansController.GetInstance().ConfirmBuyDiamond(payID, modifiedDiamondsToBuy, pfExt, BuyDiamondResult);
                    Log.Info("InitLuckyDrawBuyDiamondUIHandler", ModuleType.StreakBall);
                }
            }
        }


        private int m_nCurBuyCount;
        private int m_totalPrice;
        private int m_packageId;
        /// <summary>
        /// 购买回调
        /// </summary>
        private void BuyDiamondResult(BuyDiamondResultParam param)
        {
        }

        private void OnDiamondSupplyCallback(int package_id)
        {
        }

        private long GetCurDiamond()
        {
            long diamondNum = 0;
            if (SceneSwitch.CurrentScene() == SceneSwitch.Scene.Main)
            {
                diamondNum = PlayerDataMgr.GetInstance().PlayerDiamondNum;
            }
            else
            {
                var selfInfo = PlayerDataMgr.GetInstance().GetSelfInfo();
                if (selfInfo != null)
                {
                    diamondNum = selfInfo.diamondNum;
                }
            }
            return diamondNum;
        }
    }

}// 自动生成于：8/12/2025 3:37:51 PM
