using System;
using System.Collections;
using System.Collections.Generic;

using HappyBridge.Audio;

using HappyMahjong.Common;
using HappyMahjong.ReturningActivity;
using HappyMahjong.SSRInstituteSpace;

using MJWinStreakBallActivity;

using UnityEngine;
using UnityEngine.UI;

using Util = HappyMahjong.Common.Util;


namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallExchangeHandler : StreakBallPopupHandler
    {
        private GameObject m_exchangeItemPrefab;
        private Transform m_missionTips;
        private Text m_missionTipsText;
        private string m_missionTipsDefaultContent;
        private Coroutine m_missionTipsCoroutine;
        private RectTransform m_exchangeContent;
        private UGUIExtend.UIClippable m_clippable;
        private Transform m_tfLockTipText;

        public void Init(GetDetailRes res)
        {
            m_missionTipsDefaultContent = "请前往对局，获取连胜球获得限时代币";
            //返回按钮
            BindPopupBackBtn(transform.Find("Navigation/btnBack"));
            //设置代币
            var tfBeanInfo = transform.Find("Navigation/Money/moveObject/Diamant");
            StreakBallUtil.SetCoinIcon(tfBeanInfo);
            Util.SetUIText(tfBeanInfo.Find("Label_diamantnumber"), res.ExchangeInfo.CurToken.ToString());
            //兑换锁定
            m_tfLockTipText = transform.Find("LockText");
            //点击跳转任务面板
            UIEventListener.Get(tfBeanInfo.gameObject).onClick = (tfBeanInfoObj) =>
            {
                AudioController.GetInstance().PlayAuto("ui_click", HappyMahjong.Audio.AudioLayers.Oneshot);
            };

            //任务提示
            m_missionTips = transform.Find("Tips");
            if (m_missionTips != null)
            {
                m_missionTipsText = m_missionTips.Find("Text").GetComponent<Text>();
                m_missionTips.gameObject.SetActive(false);
            }

            //可兑换的物品
            m_exchangeContent = transform.Find("PanelContent").GetComponent<RectTransform>();
            if (m_exchangeItemPrefab == null)
            {
                m_exchangeItemPrefab = transform.Find("PanelContent/Scroll View/Viewport/Content/Item")
                    .gameObject;
                m_exchangeItemPrefab.SetActive(false);
                //初始化
                StreakBallListObjectPool<ExchangeItemHandler>.Get(m_exchangeItemPrefab);
                //特效裁剪
                var tfViewPort = transform.Find("PanelContent/Scroll View/Viewport");
                m_clippable = tfViewPort.GetOrAddComponent<UGUIExtend.UIClippable>();
                m_clippable.mask = tfViewPort.GetComponent<RectTransform>();
                m_clippable.IsUseClipRect(true);
            }

            var exchangeLocked = !Util.IsInTimeSpan((uint) res.ExchangeInfo.StartTime, (uint) res.ExchangeInfo.EndTime);
            SetExchangeItemInfo(
            res.ExchangeInfo.ExchangeItems,
            res.ExchangeInfo.CurToken,
            exchangeLocked,
            "兑换锁定中");
            m_tfLockTipText.gameObject.SetActive(exchangeLocked);
            if (exchangeLocked)
            {
                var openStarted = $"{ServerTime.TicketToDate((int) res.ExchangeInfo.StartTime).ToString("MM月dd日")}开启";
                Util.SetUIText(m_tfLockTipText.Find("Text"), $"兑换锁定中，{openStarted}");
            }

            //PC要单独适配
            if (Util.IsPCPlatform())
            {
                var tfGamePanel = transform.Find("GamePanel").GetComponent<RectTransform>();
                tfGamePanel.offsetMax = new Vector2(tfGamePanel.offsetMax.x, -30);
            }
        }

        public  override void RefreshUI(GetDetailRes rspData)
        {
            if (rspData == null)
            {
                return;
            }
            Init(rspData);
        }

        private void SetExchangeItemInfo(List<ExchangeItem> exchangeItemList, int currencyCount, bool isTabExchangeLocked, string tabExchangeLockTips)
        {
            StreakBallListObjectPool<ExchangeItemHandler>.Get().SetItemInfo(exchangeItemList,
                (itemHandler, itemData, itemIndex) =>
                {
                    itemHandler.SetExchangeItem(itemData, currencyCount, isTabExchangeLocked, tabExchangeLockTips, ShowMissionTips);
                });
            //刷新特效裁剪
            if (m_clippable != null)
            {
                m_clippable.ResetData();
            }
        }

        private void ShowMissionTips(string tips)
        {
            if (m_missionTips != null)
            {
                m_missionTips.gameObject.SetActive(false);
                if (m_missionTipsCoroutine != null)
                {
                    StopCoroutine(m_missionTipsCoroutine);
                    m_missionTipsCoroutine = null;
                }
                if (gameObject.activeInHierarchy)
                {
                    m_missionTipsCoroutine = StartCoroutine(ShowMissionTipsCoroutine(tips));
                }
            }
        }


        private IEnumerator ShowMissionTipsCoroutine(string tips)
        {
            if (string.IsNullOrEmpty(tips))
            {
                tips = m_missionTipsDefaultContent;
            }

            if (m_missionTipsText != null)
            {
                m_missionTipsText.text = tips;
            }
            m_missionTips.gameObject.SetActive(true);
            yield return Yielders.GetWaitForSeconds(2f);
            if (m_missionTips != null)
            {
                m_missionTips.gameObject.SetActive(false);
            }
        }
    }
}
