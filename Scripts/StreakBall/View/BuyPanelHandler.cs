using System;
using System.Collections;
using System.Collections.Generic;
using HappyMahjong.BuyBeans;
using HappyMahjong.Common;
using HappyMahjong.SelectionScene;
using HappyMahjong.ShopAndBag;
using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public class BuyPanelHandler : BubbleBehaviour 
    {
        private Transform m_buyCountText;
        private Transform m_priceText;
        private int m_curBuyCount;
        private const int m_buyCountLimit = 999;

        private int m_curItemID;
        private UpgradeCoin m_coinInfo;
        
        private void Awake()
        {
            Transform closeBtn = transform.Find("GamePanel/Btn_Close");
            UIEventListener.Get(closeBtn.gameObject, ClickableTypeDef.CloseSoundType).onClick = (go) => OnClickClose();

            Transform buyBtn = transform.Find("GamePanel/Btn_Red");
            UIEventListener.Get(buyBtn.gameObject, ClickableTypeDef.ClickSoundType).onClick = (go) => OnClickBuy();

            Transform numPanel = transform.Find("GamePanel/Bg/Good/NumPanel");

            m_buyCountText = numPanel.Find("PanelInput/Text");
            m_priceText = transform.Find("GamePanel/Price/FinalPrice");
            UIEventListener.Get(numPanel.Find("ButtonSub10").gameObject).onClick = (go) => OnClickChangeNum(-10);
            UIEventListener.Get(numPanel.Find("ButtonSub1").gameObject).onClick = (go) => OnClickChangeNum(-1);
            UIEventListener.Get(numPanel.Find("ButtonAdd1").gameObject).onClick = (go) => OnClickChangeNum(1);
            UIEventListener.Get(numPanel.Find("ButtonAdd10").gameObject).onClick = (go) => OnClickChangeNum(10);

            SetCurBuyCount(10);
        }

        private void SetCurBuyCount(int count)
        {
            m_curBuyCount = count;

            {
                Util.SetUIText(m_buyCountText, $"{m_curBuyCount.ToString()}/{m_buyCountLimit}");
            }

            Util.SetUIText(m_priceText, $"{m_coinInfo.price * count}");
        }

        private void OnClickChangeNum(int delta)
        {
            int newValue = m_curBuyCount + delta;
            if (newValue <= 0)
            {
                StreakBallUtil.ShowToast("最小购买限制");
                return;
            }

            if (m_curBuyCount == m_buyCountLimit && delta > 0F)
            {
                StreakBallUtil.ShowToast("最大购买限制");
                return;
            }

            if (newValue > m_buyCountLimit)
            {
                newValue = m_buyCountLimit;
            }

            SetCurBuyCount(newValue);
        }
        
        private void OnClickClose()
        {
            PopUpManager.GetInstance().RemovePopUp(gameObject);
        }

        private void OnClickBuy()
        {
            OnClickClose();
            
            if (PlayerDataMgr.GetInstance().PlayerDiamondNum < m_curBuyCount * m_coinInfo.price)
            {
                Transform trans = transform.parent;
                var callbacks = new DialogCallbacks();
                callbacks.CancelCallback = () => { };
                callbacks.ConfirmCallback = () =>
                {
                    {
                        bubble.ContextDispatcher(trans, SelectionViewEvent.ShowRecharge);
                    }
                };
            
                DialogManager.GetInstance().ShowDialogUgui(2, callbacks, "钻石不足，是否前往购买？", LanguageKey.SURE, LanguageKey.CANCLE);
                return;
            }
            
            ReqStreakBallServiceDO serviceDO = default;
            serviceDO.cmd = ReqSSRCMD_ID.CMD_Buy;
            serviceDO.buyCoinVo = new();
            serviceDO.buyCoinVo.Add(new BuyCoinVO{ itemID = m_curItemID, count =  m_curBuyCount });
            
            bubble.ContextDispatcher(transform, StreakBallEvent.ReqStreakBallDetail, serviceDO);
        }

        public void SetData(int itemID, UpgradeCoin upgradeCoin, int initialBuyCount = 0)
        {
            m_curItemID = itemID;
            m_coinInfo = upgradeCoin;
            
            Item item = ShopDataHelper.GetInstance().GetItem(itemID);
            if (item != null)
            {
                if (initialBuyCount <= 0)
                {
                    int myCount = ShopDataHelper.GetInstance().getMyItemCount(itemID);
                    SetCurBuyCount(Mathf.Max(1, upgradeCoin.cost - myCount));
                }
                else
                {
                    SetCurBuyCount(initialBuyCount);
                }

                
                Transform icon = transform.Find("GamePanel/Bg/Good/Texture");
                ShopDataHelper.GetInstance().GetSpriteName(item.icon, out string atlasName, out string spritename);
                
                var image = icon.GetComponent<Image>();
                ShopUIHelper.SetItemNetTexture(image, atlasName, spritename, () =>
                {
                    if (image && m_curItemID == itemID)
                    {
                        Sprite sprite = image.sprite;
                        if (sprite == null)
                        {
                            return;
                        }
                        
                        image.color = Color.white;
                            
                        float width = sprite.rect.width;
                        float height = sprite.rect.height;

                        Vector2 sizeDalta = ((RectTransform) image.transform).sizeDelta;
                        float size = Mathf.Min(sizeDalta.x, sizeDalta.y);
                        
                        if (height <= width)
                        {
                            Vector2 sizeDeatil = new Vector2(size, size);
                            float ratio = (float)size / height;
                            sizeDeatil.x = (int)(width * ratio);
                            ((RectTransform) image.transform).sizeDelta = sizeDeatil;
                        }
                        else
                        {
                            Vector2 sizeDeatil = new Vector2(size, size);
                            float ratio = (float) size / width;
                            sizeDeatil.y = (int)(height * ratio);
                            ((RectTransform) image.transform).sizeDelta = sizeDeatil;
                        }
                    }
                });


                Transform label = transform.Find("GamePanel/Bg/Good/Label");
                Util.SetUIText(label, item.name);

                Transform desc = transform.Find("GamePanel/Bg/Good/Desc");
                Util.SetUIText(desc, item.descContent);
            }
        }
    }
}

