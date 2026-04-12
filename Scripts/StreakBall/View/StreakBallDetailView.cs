using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Configuration;

using Happy.Blueprint.System;

using HappyMahjong.Audio;
using HappyMahjong.ChoiceSex;
using HappyMahjong.Common;
using HappyMahjong.SelectionScene;
using HappyMahjong.ShopAndBag;
using HappyMahjong.Tutorial;

using SSRItemUpgrade;

using strange.extensions.mediation.impl;

using UnityEditor;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

using ItemPreview = RankedBattlePassSpace.ItemPreview;
using AvatarPreview = RankedBattlePassSpace.AvatarPreview;
using Console = System.Console;
using DeviceType = HappyMahjong.Common.DeviceType;

using TalentPavillion;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallDetailView : BubbleBehaviour,
        IScrollSourceProvider, IScrollTabSelectEvent, IRefreshUIable, IScreenChanged
    {
        public StreakBallModel model { get; set; }
        public ShowStreakBallDetailVO vo { get; set; }

        private Transform m_navigationUI;
        private GameObject m_block;

        #region 升阶
        #endregion

        #region 左上角按钮
        private MoneyBarHandler m_moneyBarHandler;
        private Transform m_coinContainer;
        #endregion

        #region 底部Tips
        #endregion

        #region 右上角
        #endregion

        #region UI State
        private bool m_isDestroying;
        #endregion

        #region StreakBall
        private SlotInfo m_selctedSlot;
        private OrbInfo m_selectedOrb;
        private GameObject m_talentSkillDetail;
        private GameObject m_operations;
        private GameObject m_talentEquipTip;

        public SkillCenterItemHandler centerHandler;
        public SkillRightItemDetailHandler rightHandler;
        private LoopVerticalScrollRect m_loopSroll;
        private OrbInfo m_currentSelectedInfo;

        private List<OrbInfo> m_orbInfos = new();
        public object DataSource => m_orbInfos;
        Transform bg;
        #endregion

        #region LifeCircle

        protected override void Awake()
        {
            // normal
            m_block = transform.Find("Block").gameObject;
            m_navigationUI = transform.Find("Navigation");

            Transform exitTrans = transform.Find("Navigation/btnBack/click_area");
            UIEventListener.Get(transform.Find("Navigation/Label/Btn_help").gameObject, ClickableTypeDef.ClickSoundType).onClick = (go) => OnClickHelp();
            UIEventListener.Get(exitTrans.gameObject, ClickableTypeDef.CloseSoundType).onClick = OnClickExit;
            m_moneyBarHandler = transform.Find("Navigation/Money").GetComponent<MoneyBarHandler>();
            m_coinContainer = transform.Find("Navigation/Money/moveObject");

            if (Util.IsPCPlatform())
            {
                RectTransform rectTrans = (RectTransform) m_navigationUI;
                rectTrans.offsetMax = new Vector2(0F, -20F);

                rectTrans = (RectTransform) transform.Find("TopRightBtns");
                rectTrans.offsetMax = new Vector2(0F, -20F);
            }
        }

        private void ResetMainSceneOnDestroy()
        {
        }
        
        private void OnInGameTableCreated()
        {
        }


        void Start()
        {
            ScreenChangeHandler.GetInstance().RegisterPost(transform, this);
            PlayerStatistics.GetInstance().RecordMessage((int) SNSType.GeneralEvent, (int)ReportEvent.StreakBallDetailUIExposed);
        }

        public Action<MonoBehaviour> OnDestroyCallback { get; set; }

        void OnDestroy()
        {
            ScreenChangeHandler.GetInstance().UnRegister(transform);
            m_isDestroying = true;
            OnDestroyCallback?.Invoke(this);
            StreakBallInGameController.onCreated -= OnInGameTableCreated;

            try
            {
                StreakBallInGameController.Cleanup();
            }
            catch (Exception e)
            {
                Log.Error($"Clean SSR Ingame exception: {e}", ModuleType.StreakBall);
            }

            ResetMainSceneOnDestroy();
            
            StreakBallUtil.SetIsInStreakBall(false);
            
            PersonalInfoQueryByFlag.queryBeans();

            bubble.ContextDispatcher(transform, CommonProtocolKey.StreakBallOnDestroyView);

            ResourcesLoader.GetInstance().GarbageCollection();
            Resources.UnloadUnusedAssets();
        }
        
        #endregion
        
        #region UI
        private EventSystem m_eventSystem;
        private void SetGraphicRaycaster(bool enable)
        {
            GetComponent<GraphicRaycaster>().enabled = enable;
            m_navigationUI.GetComponent<GraphicRaycaster>().enabled = enable;
        }
        #endregion

        #region 主要逻辑

        private Action m_action;
        public void OverrideBackAction(Action act)
        {
            m_action = act;
        }

        private void OnClickExit(GameObject go)
        {
            if (m_action != null)
            {
                m_action();
            }
            else
            {
                PopUpManager.GetInstance().RemovePopUp(gameObject);
            }
        }

        private void OnClickHelp()
        {
            GameObject prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "StreakBallHelpPanel");
            GameObject go = UIUtil.Instantiate(prefab);
            UIEventListener.Get(go.transform.Find("GamePanel/Btn_Close").gameObject, ClickableTypeDef.CloseSoundType).onClick = (a) =>
            {
                // Destroy(go);
                PopUpManager.GetInstance().RemovePopUp(go);
            };
            var label = go.transform.Find("GamePanel/Scrollview/Content/Label");
            string descText = DynamicConfig.GetInstance().GetString(UIDef.ConfigKey, "RuleTip");
            Util.SetUIText(label, descText);
            PopUpManager.GetInstance().AddPopUp(go, PopUpType.UGUI);
        }

        #endregion

        #region 协议返回

        [Sirenix.OdinInspector.Button]
        public void OnBoughtCoinSuccess(IList<CoinInfo> coinInfos)
        {
            FetchMyItem.fetMyitem();
            PersonalInfoQueryByFlag.queryBeansAndDiamond();
            
            foreach (var coinInfo in coinInfos)
            {
                model.AddBoughtCoin(coinInfo.item_id, coinInfo.item_num);
            }

            List<CongratulationHandler.Item64> itemList = ListPool<CongratulationHandler.Item64>.Get();
            try
            {
                foreach (var coinInfo in coinInfos)
                {
                    CongratulationHandler.Item64 item = new CongratulationHandler.Item64();
                    item.nID = coinInfo.item_id;
                    item.nCount = coinInfo.item_num;
                    item.isFitonItem = false;
                    itemList.Add(item);
                }
                
                CongratulationHandler handler = CongratulationHandler.Instantiate();
                handler.SetItem(itemList, false);
            }
            finally
            {
                ListPool<CongratulationHandler.Item64>.Release(itemList);
            }
        }

        public void OnUpdateBeanDiamand(BeanDiamandInfo beanDiamandInfo)
        {
        }

        #endregion

        #region 背景图片
        #endregion

        #region 新手引导
        #endregion

        #region Talent

        public void OnScreenChanged()
        {
            if (bg == null)
            {
                bg = transform.Find("Bg");
            }
            if (bg)
            {
                var rect = bg.GetComponent<RectTransform>();
                var vector2 = new Vector2(rect.rect.width, rect.rect.height);
                var popParent = PopUpManager.GetInstance().GetParent(PopUpType.UGUI);
                if (popParent)
                {
                    Canvas.ForceUpdateCanvases();
                    var m_rootRectTransform = popParent.transform as RectTransform;
                    float screenAspectW = 1.0f * m_rootRectTransform.rect.width / vector2.x;
                    float screenAspectH = 1.0f * m_rootRectTransform.rect.height / vector2.y;
                    var screenAspect = Math.Max(screenAspectW, screenAspectH);
                    bg.localScale = screenAspect * Vector3.one;
                }
            }
        }

        internal void InitWithVO(ShowStreakBallDetailVO vo)
        {
            this.vo = vo;
            m_orbInfos.Clear();
            m_orbInfos.Sort((a,b)=>b.Level.CompareTo(a.Level));
            m_orbInfos.Sort((a, b) => b.IsOwned.CompareTo(a.IsOwned));

            // center
            centerHandler = transform.Find("SkillCenter").GetComponent<SkillCenterItemHandler>();
            // right
            rightHandler = transform.Find("RightItemDetail").GetComponent<SkillRightItemDetailHandler>();
            // left
            m_loopSroll = transform.Find("SkillScrollView").GetComponent<LoopVerticalScrollRect>();

            m_loopSroll.totalCount = m_orbInfos.Count;
            m_loopSroll.RefillCells();

            TalentSkillItemTab.FirstSelect = 0;
            if (vo.ItemId != 0)
            {
                var idx = m_orbInfos.FindIndex(o => o.ItemId == vo.ItemId);
                if(idx > -1)
                {
                    TalentSkillItemTab.FirstSelect = idx;
                    m_loopSroll.SrollToCell(idx, 100);
                }
            }
            else
            {
                // rule1:玩家已获取天赋技能，优先跳转至玩家已获取但未装备的技能
                var firstIndex = m_orbInfos.FindIndex(o => o.IsOwned == 1 && o.IsEquipped == 0);
                if (firstIndex == -1)
                {
                    // rule2:玩家未获取天赋技能，直接跳转在线可获取的技能
                    firstIndex = m_orbInfos.FindIndex(o => o.TryGetTalentSkillItemConfig(out var clientCfg) && StreakBallUtil.IsActResourceReady(clientCfg.linkActName));
                }
                if (firstIndex > -1)
                {
                    TalentSkillItemTab.FirstSelect = firstIndex;
                    m_loopSroll.SrollToCell(firstIndex, 100);
                }
            }
        }

        public void OnScrollTabSelect(OrbInfo info)
        {
            m_currentSelectedInfo = info;
            if (centerHandler)
            {
                centerHandler.OrbInfoUpdate(info, vo.SlotId, model);
                if (vo.TryShowGuide)
                {
                    centerHandler.TryShowGuide();
                }
            }
            if (rightHandler)
            {
                rightHandler.OrbInfoUpdate(info);
            }
        }

        public void RefreshUI()
        {
        }
        #endregion
    }
}// 自动生成于：8/12/2025 3:37:51 PM
