using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MJWinStreakBallActivity;
using System;
using HappyMahjong.Common;
using HappyBridge.Audio;
using HappyMahjong.ShopAndBag;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public class ExchangeItemHandler : MonoBehaviour
    {
        public void SetExchangeItem(ExchangeItem item, 
            int currencyCount,
            bool isTabExchangeLocked, 
            string tabExchangeLockTips, 
            Action<string> onGeryClick)
        {
            HappyMahjong.ShopAndBag.Item itemInfo = null;
            string itemShowName = string.Empty;
            Reward showReward = null;
            string itemLevel = string.Empty;
            //物品图标
            var itemHandler = transform.Find("ItemIcon").GetOrAddComponent<ItemIconHandlerUgui>();
            var reaward = item.Reward;
            if (itemHandler.SetItem(reaward.ItemId, reaward.ItemNum))
            {
                showReward = reaward;
                itemInfo = ShopDataHelper.GetInstance().GetItem(reaward.ItemId);
            }

            //物品名称
            if (itemInfo != null)
            {
                var label = transform.Find("ItemIcon/Label").GetComponent<Text>();
                var opType = (OPType) itemInfo.opType;
                bool isTime = opType == OPType.Time || opType == OPType.TimeOnceDay;
                var middle = isTime ? "·" : "x";
                itemShowName = $"{itemInfo.name}{middle}{label.text}";
                Util.SetUIText(transform.Find("NameBG/Text"), itemShowName);
                //获取提示
                var tfTipToggle = transform.Find("TipToggle").GetComponent<Toggle>();
                bool showTip = false;
                tfTipToggle.gameObject.SetActive(showTip);
                //物品等级
                itemLevel = itemInfo.level;
            }

            //展示物品等级
            var tfLevel = transform.Find("Level");
            for (int i = 0; i < tfLevel.childCount; i++)
            {
                var tfChild = tfLevel.GetChild(i);
                if (tfChild != null)
                {
                    bool showLevel = tfChild.name.Equals(itemLevel);
                    tfChild.gameObject.SetActive(showLevel);
                }
            }

            //物品花费
            var strCost = item.TokenRequire.ToString();
            var btnYellow = transform.Find("Btn/Btn_Yellow");
            var btnGery = transform.Find("Btn/Btn_Grey");
            Util.SetUIText(btnYellow.Find("TextNum"), strCost);
            Util.SetUIText(btnGery.Find("TextNum"), strCost);

            //状态
            //1次数满了,就是换过了。2已经有这个东西的永久版本了
            var btnGeryText = btnGery.Find("Text");
            var btnGeryNumText = btnGery.Find("TextNum");
            int status = item.Status;
            bool isGet = status > 2;
            if ( status == (int) ExchangeStatus.ExchangeStatusOwned)
            {
                var strStatus =  "已拥有";
                Util.SetUIText(btnGeryText, strStatus);
            }
            else if (status == (int) ExchangeStatus.ExchangeStatusCanExchange)
            {
                var strStatus =  "可兑换";
                Util.SetUIText(btnGeryText, strStatus);
            }
            else if(status == (int) ExchangeStatus.ExchangeStatusNotEnoughToExchange || status == (int) ExchangeStatus.ExchangeStatusNone)
            {
                var strStatus = "不可兑换";
                Util.SetUIText(btnGeryText, strStatus);
            }
            btnGeryText.gameObject.SetActive(isGet);
            btnGeryNumText.gameObject.SetActive(!isGet);

            bool canExchange = false;
            //不可兑换
            if (!isGet)
            {
                canExchange = currencyCount >= item.TokenRequire && !isTabExchangeLocked;
            }

            //按钮状态
            btnYellow.gameObject.SetActive(canExchange);
            btnGery.gameObject.SetActive(!canExchange);

            if (canExchange)
            {
                UIEventListener.Get(btnYellow.gameObject).onClick = (btnYellowObj) =>
                {
                    AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);
                    //确认兑换面板
                    var confirmHandler = StreakBallPopupHandler.CreatePopupHandler<StreakBallExchangeConfirmHandler>("StreakBallExchangeConfirmPanel");
                    if (confirmHandler != null)
                    {
                        confirmHandler.Init(item, currencyCount, itemInfo, showReward, itemShowName);
                    }
                };
            }
            else
            {
                UIEventListener.Get(btnGery.gameObject).onClick = (btnGeryObj) =>
                {
                    if (!isGet)
                    {
                        AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);
                        if (onGeryClick != null)
                        {
                            string tips = string.Empty;
                            if (isTabExchangeLocked)
                            {
                                tips = tabExchangeLockTips;
                            }
                            Log.Info($"ExchangeItemHandler tips:{tips} isTabExchangeLocked:{isTabExchangeLocked}", ModuleType.StreakBall);
                            onGeryClick(tips);
                        }
                    }
                };
            }
        }
    }
}
