using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MJWinStreakBallActivity;
using HappyMahjong.Common;

using Util = HappyBridge.Util.Util;

using HappyBridge.Audio;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallExchangeConfirmHandler : StreakBallPopupHandler
    {
        public void Init(ExchangeItem exchangeItem, int currencyCount, HappyMahjong.ShopAndBag.Item itemInfo, Reward reward, string itemShowName)
        {
            //返回按钮
            BindPopupBackBtn(transform.Find("GamePanel/Btn_Close"));

            //设置基础信息
            Util.SetUIText(transform.Find("GamePanel/ContentPanel/Bottom/GlodTips/Num"), currencyCount.ToString());
            Util.SetUIText(transform.Find("GamePanel/ContentPanel/Bottom/BtnExchange/Text"), exchangeItem.TokenRequire.ToString());

            //设置礼包信息
            var iconHandler = transform.Find("GamePanel/ContentPanel/Gift/ItemIcon").GetOrAddComponent<ItemIconHandlerUgui>();
            iconHandler.SetItem(reward.ItemId, reward.ItemNum);

            //礼包名称
            Util.SetUIText(transform.Find("GamePanel/ContentPanel/Gift/Title"), itemShowName);
            // Util.SetUIText(transform.Find("GamePanel/ContentPanel/Gift/NumText"), "1");
            //兑换数量
            bool showExchangeNum = exchangeItem.MaxExchange > 0;
            if (showExchangeNum)
            {
                var exchangeNum = $"{exchangeItem.CurExchange}/{exchangeItem.MaxExchange}";
                Util.SetUIText(transform.Find("GamePanel/ContentPanel/Gift/ExchangeTitle/ExchangeText"), exchangeNum);
            }
            transform.Find("GamePanel/ContentPanel/Gift/ExchangeTitle").gameObject.SetActive(showExchangeNum);
            transform.Find("GamePanel/ContentPanel/Gift/ExchangeTextTips").gameObject.SetActive(!showExchangeNum);

            //点击兑换按钮
            var btnExchange = transform.Find("GamePanel/ContentPanel/Bottom/BtnExchange");
            UIEventListener.Get(btnExchange.gameObject).onClick = (btnExchangeObj) =>
            {
                AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);
                //兑换
                bubble.ContextDispatcher(transform, StreakBallEvent.ExchangeReq, exchangeItem);
                //关闭弹窗
                ClosePopupHandler();
            };
        }
    }
}
