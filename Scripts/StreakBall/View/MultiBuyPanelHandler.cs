using System;
using System.Collections;
using System.Collections.Generic;

using HappyMahjong.Common;
using HappyMahjong.SelectionScene;
using HappyMahjong.ShopAndBag;

using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public class MultiBuyPanelHandler : BubbleBehaviour
    {
        private Transform m_priceText;
        private Transform m_buyItemContent;

        private Transform[] m_buyCountTexts;
        private UpgradeCoin[] m_upgradeCoins;
        private BuyCoinVO[] m_buyCoinVOs;
        private bool[] m_selectToBuy;


        private const int m_buyCountLimit = 999;
        
        protected override void Awake()
        {
            base.Awake();
            
            Transform closeBtn = transform.Find("GamePanel/Btn_Close");
            UIEventListener.Get(closeBtn.gameObject, ClickableTypeDef.CloseSoundType).onClick = (go) => OnClickClose();

            Transform buyBtn = transform.Find("GamePanel/Btn_Red");
            UIEventListener.Get(buyBtn.gameObject, ClickableTypeDef.ClickSoundType).onClick = (go) => OnClickBuy();
            
            m_priceText = transform.Find("GamePanel/Price/FinalPrice");
            m_buyItemContent = transform.Find("GamePanel/ScrollView/Viewport/Content");
        }
        
        private void OnClickClose()
        {
            PopUpManager.GetInstance().RemovePopUp(gameObject);
        }

        private int GetTotalPrice()
        {
            int result = 0;
            for (int i = 0; i < m_buyCoinVOs.Length; ++i)
            {
                if (!m_selectToBuy[i])
                {
                    continue;
                }

                UpgradeCoin upgradeCoin = m_upgradeCoins[i];
                result += upgradeCoin.price * m_buyCoinVOs[i].count;
            }

            return result;
        }
        
        private void OnClickBuy()
        {
            int totalPrice = GetTotalPrice();
            if (PlayerDataMgr.GetInstance().PlayerDiamondNum < totalPrice)
            {
                OnClickClose();
                
                Transform trans = transform.parent;
                var callbacks = new DialogCallbacks();
                callbacks.CancelCallback = () => { };
                callbacks.ConfirmCallback = () =>
                {
                    // if (Recharge.RechargeABTestDef.AGroup == Recharge.RechargeController.GetInstance().GetGroup())
                    // {
                    //     BuyBeansController.GetInstance().Show(BuyStyle.Diamond);
                    // }
                    // else
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
            
            for (int i = 0; i < m_buyCoinVOs.Length; ++i)
            {
                if (!m_selectToBuy[i] || m_buyCoinVOs[i].count <= 0)
                {
                    continue;
                }
                
                serviceDO.buyCoinVo.Add(m_buyCoinVOs[i]);
            }

            if (serviceDO.buyCoinVo.Count == 0)
            {
                StreakBallUtil.ShowToast("没有物品可购买");
            }
            else
            {
                OnClickClose();
                bubble.ContextDispatcher(transform, StreakBallEvent.ReqStreakBallDetail, serviceDO);
            }

            
            // CongratulationHandler handler = CongratulationHandler.Instantiate();
            // handler.SetItem((int) SpecialItem.Bean, 1000, (int) SpecialItem.Bean, 1000);
            //
            // handler.delegateClose = () =>
            // {
            //     if (handler != null && handler.gameObject != null)
            //     {
            //         PopUpManager.GetInstance().RemovePopUp(handler.gameObject);
            //     }
            // };
        }

        public void SetData(UpgradeCoin[] upgradeCoins)
        {
            m_upgradeCoins = upgradeCoins;
            m_buyCoinVOs =  new BuyCoinVO[m_upgradeCoins.Length];
            m_selectToBuy = new bool[m_upgradeCoins.Length];
            m_buyCountTexts =  new Transform[m_upgradeCoins.Length];
            
            Array.Fill(m_selectToBuy, true);

            Transform child = m_buyItemContent.GetChild(0);
            GameObject buyItemTemplate = child.gameObject;

            bool first = true;
            for (int i = 0; i < m_upgradeCoins.Length; ++i)
            {
                UpgradeCoin upgradeCoin = m_upgradeCoins[i];

                m_buyCoinVOs[i].itemID = upgradeCoin.itemID;
                
                Item item = ShopDataHelper.GetInstance().GetItem(upgradeCoin.itemID);
                if (item == null)
                {
                    continue;
                }
                
                int myCount = ShopDataHelper.GetInstance().getMyItemCount(upgradeCoin.itemID);
                if (myCount >= upgradeCoin.cost)
                {
                    continue;
                }
                
                GameObject buyItem;
                if (first)
                {
                    first = false;
                    buyItem = buyItemTemplate;
                }
                else
                {
                    buyItem = Instantiate(buyItemTemplate, m_buyItemContent);
                }

                buyItem.SetActive(true);

                m_buyCountTexts[i] = buyItem.transform.Find("NumPanel/PanelInput/Text");
                

                SetCurBuyCount(i, Mathf.Max(0, upgradeCoin.cost - myCount));
                    
                Transform icon = buyItem.transform.Find("Good/Texture");
                ShopDataHelper.GetInstance().GetSpriteName(item.icon, out string atlasName, out string spritename);
                    
                var image = icon.GetComponent<Image>();
                ShopUIHelper.SetItemNetTexture(image, atlasName, spritename, () =>
                {
                    if (image)
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


                Transform label = buyItem.transform.Find("Label");
                Util.SetUIText(label, item.name);

                // Transform desc = buyItem.transform.Find("Good/Desc");
                // Util.SetUIText(desc, item.descContent);

                int buyItemIndex = i;
                Transform numPanel = buyItem.transform.Find("NumPanel");
                UIEventListener.Get(numPanel.Find("ButtonSub10").gameObject).onClick = (go) => OnClickChangeNum(buyItemIndex, -10);
                UIEventListener.Get(numPanel.Find("ButtonSub1").gameObject).onClick = (go) => OnClickChangeNum(buyItemIndex, -1);
                UIEventListener.Get(numPanel.Find("ButtonAdd1").gameObject).onClick = (go) => OnClickChangeNum(buyItemIndex, 1);
                UIEventListener.Get(numPanel.Find("ButtonAdd10").gameObject).onClick = (go) => OnClickChangeNum(buyItemIndex, 10);

                Transform checkTrans = buyItem.transform.Find("Check");
                GameObject checkMark = checkTrans.Find("Checkmark").gameObject;
                UIEventListener.Get(checkTrans.gameObject).onClick = (go) => OnClickSelected(buyItemIndex, checkMark);
            }
        }

        private void OnClickSelected(int index, GameObject checkMark)
        {
            bool selected = m_selectToBuy[index] = !m_selectToBuy[index];
            if (checkMark)
            {
                checkMark.SetActive(selected);
            }
            UpdateTotalPrice();
        }
        
        private void OnClickChangeNum(int index, int delta)
        {
            int count = m_buyCoinVOs[index].count;
            int newValue = count + delta;
            if (newValue < 0)
            {
                StreakBallUtil.ShowToast("最小购买限制");
                return;
            }

            if (count == m_buyCountLimit && delta > 0F)
            {
                StreakBallUtil.ShowToast("最大购买限制");
                return;
            }

            if (newValue > m_buyCountLimit)
            {
                newValue = m_buyCountLimit;
            }

            SetCurBuyCount(index, newValue);
        }
        
        private void SetCurBuyCount(int index, int count)
        {
            m_buyCoinVOs[index].count = count;

            // if (m_buyCountLimit == int.MaxValue)
            // {
            //     Util.SetUIText(m_buyCountTexts[index], $"{count.ToString()}");
            // }
            // else
            {
                Util.SetUIText(m_buyCountTexts[index], $"{count.ToString()}/{m_buyCountLimit}");
            }

            UpdateTotalPrice();
        }

        private void UpdateTotalPrice()
        {
            int totalPrice = GetTotalPrice();
            Util.SetUIText(m_priceText, $"{totalPrice}");
        }
    }
}
