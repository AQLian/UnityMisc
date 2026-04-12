using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HappyMahjong.Common;
using HappyMahjong.ShopAndBag;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public class TalentUpgradeHandler : BubbleBehaviour
    {
        private GameObject m_costItemTemplate;
        private Transform m_costContent;
        private int m_curState;
        private bool m_allowUpgrade;
        private bool m_canUpgrade;
        private int m_itemID;
        private int m_curLevel; // 这个只是上报用

        private UpgradeCoin[] m_upgradeCoins;
        private Dictionary<int, UpgradeCoin> m_coinMap = new();

        private GameObject m_costTips;
        
        private bool m_bBlockClick;
        
        protected override void Awake()
        {
            base.Awake();
            m_costContent = transform.Find("Cost");
            m_costItemTemplate = transform.Find("Cost/Item").gameObject;
            m_costItemTemplate.transform.SetParent(null, false);
            m_costItemTemplate.SetActive(false);
            m_costItemTemplate.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDestroy()
        {
            if (m_costItemTemplate)
            {
                Destroy(m_costItemTemplate);
                m_costItemTemplate = null;
            }
        }

        private void OnClickCostItem(Transform costItem, int itemID, int ownedCount, int costCount, int price)
        {
            if (!m_allowUpgrade || m_costTips)
            {
                return;
            }
            
            GameObject tipsPrefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "CostItemTips");
            GameObject costTips = UIUtil.Instantiate(tipsPrefab, costItem);

            m_costTips = costTips;
            
            costTips.GetOrAddComponent<ItemIconHandlerUgui>().SetItem(itemID, -1, needShowName: true, showItemTips: false);

            Canvas canvas = costTips.GetComponent<Canvas>();
            Canvas parentCanvas = transform.GetComponentInParent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerName = parentCanvas.sortingLayerName;
            
            
            RectTransform rectTrans = (RectTransform) costTips.transform;
            rectTrans.pivot = new Vector2(0.5F, 0F);
            rectTrans.anchoredPosition = new Vector2(0, 48F);

            rectTrans.parent = transform.parent.parent;

            UIEventListener.Get(rectTrans.Find("Block").gameObject).onClick = (go) =>
            {
                Destroy(costTips);
            };

            UIEventListener.Get(rectTrans.Find("BuyButton").gameObject, ClickableTypeDef.ClickSoundType).onClick = (go) =>
            {
                Destroy(costTips);
                OnClickBuyCostItem(itemID);
            };

            Item item = ShopDataHelper.GetInstance().GetItem(itemID);
            if (item != null)
            {
                Transform desc = rectTrans.Find("Desc");
                Util.SetUIText(desc, item.descContent);
            }

            Transform cost = rectTrans.Find("Cost");
            Util.SetUIText(cost, $"{StreakBallUtil.GetFormatedNumber(ownedCount)}/{StreakBallUtil.GetFormatedNumber(costCount)}");

            Transform num = rectTrans.Find("Num");
            Util.SetUIText(num, $"{price}/个");
        }
        
        private void OnClickBuyCostItem(int itemID)
        {
            if (!m_coinMap.ContainsKey(itemID))
            {
                Log.Error($"OnClickBuyCostItem m_coinMap not contains id {itemID}", ModuleType.StreakBall);
                return;
            }
            
            UpgradeCoin upgradeCoin = m_coinMap[itemID];
            // int myCount = ShopDataHelper.GetInstance().getMyItemCount(itemID);

            if (upgradeCoin.buyLimit <= 0)
            {
                // 已达到拥有限制
                StreakBallUtil.ShowToast("已到达购买上限");
                return;
            }
            
            GameObject prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "BuyPanel");
            GameObject buyPanel = UIUtil.Instantiate(prefab);

            var buyHandler = buyPanel.GetOrAddComponent<BuyPanelHandler>();
            buyHandler.SetData(itemID, upgradeCoin);

            PopUpManager.GetInstance().AddPopUp(buyPanel, PopUpType.UGUI);
        }
    }
}

