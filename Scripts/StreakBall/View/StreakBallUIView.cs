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
using HappyMahjong.SSRInstituteSpace;
using HappyMahjong.Tutorial;

using SSRItemUpgrade;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;

using TalentPavillion;

using TMPro;

using UnityEditor;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

using AvatarPreview = RankedBattlePassSpace.AvatarPreview;
using Console = System.Console;
using DeviceType = HappyMahjong.Common.DeviceType;
using ItemPreview = RankedBattlePassSpace.ItemPreview;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallUIView : EventView, IScreenChanged
    {
        [Inject]
        public StreakBallModel model { get; set; }
        public IShopModel shopModel { get; set; }

        private BubbleEventHelper m_bubble;

        public BubbleEventHelper bubble
        {
            get
            {
                if (m_bubble == null)
                    m_bubble = new BubbleEventHelper();

                return m_bubble;
            }
        }

        private Transform m_navigationUI;
        private GameObject m_block;

        #region 升阶
        #endregion

        #region 底部Tips
        #endregion

        #region 右上角
        #endregion

        #region UI State
        public object showArgument { get; set; }
        private bool m_isDestroying;
        #endregion

        #region 左上角按钮
        private MoneyBarHandler m_moneyBarHandler;
        private Transform m_coinContainer;
        private Transform m_diamondObj;
        #endregion

        #region StreakBall
        public static GameObject BgObject;
        private static Sprite m_bgDefaultSprite;

        private SlotInfo m_selctedSlot;
        private OrbInfo m_selectedOrb;
        private GameObject m_talentSkillDetail;
        private GameObject m_talentBtn;
        private TextMeshProUGUI m_talentSkillTitle;
        private GameObject m_talentSkillLevelTag;
        private TextMeshProUGUI m_skilDesc;
        private TextMeshProUGUI m_talentBtnText;
        private TextMeshProUGUI m_talentBtnText_Gray;
        private TextMeshProUGUI m_talentBtnCooldownText;
        private GameObject m_operations;
        private GameObject m_talentEquipTip;
        Transform bg;
        #endregion

        #region LifeCircle
        protected override void Awake()
        {
            base.Awake();

            StreakBallUtil.SetIsInStreakBall(true);

            // normal
            m_block = transform.Find("Block").gameObject;
            //StreakBallUIView.InitBgObject();

            m_navigationUI = transform.Find("Navigation");
            Transform exitTrans = transform.Find("Navigation/btnBack/click_area");
            UIEventListener.Get(transform.Find("Navigation/Label/Btn_help").gameObject, ClickableTypeDef.ClickSoundType).onClick = (go) => OnClickHelp();
            UIEventListener.Get(exitTrans.gameObject, ClickableTypeDef.CloseSoundType).onClick = OnClickExit;
            m_moneyBarHandler = transform.Find("Navigation/Money").GetComponent<MoneyBarHandler>();
            m_coinContainer = transform.Find("Navigation/Money/moveObject");
            m_diamondObj = m_coinContainer.Find("Diamant");

            // talent
            m_talentSkillDetail = transform.Find("TalentSkillDetail").gameObject;
            m_talentSkillTitle = transform.Find("TalentSkillDetail/Title").GetComponent<TextMeshProUGUI>();
            m_talentSkillLevelTag = transform.Find("TalentSkillDetail/Title/LevelTag").gameObject;
            m_talentBtn = transform.Find("TalentSkillDetail/Btn").gameObject;
            m_talentBtnText = transform.Find("TalentSkillDetail/Btn/Text").GetComponent<TextMeshProUGUI>();
            m_talentBtnText_Gray = transform.Find("TalentSkillDetail/Btn/Text_Gray").GetComponent<TextMeshProUGUI>();
            m_talentBtnCooldownText = transform.Find("TalentSkillDetail/Btn/CooldownText").GetComponent<TextMeshProUGUI>();
            m_skilDesc = transform.Find("TalentSkillDetail/DescBg/Desc").GetComponent<TextMeshProUGUI>();
            m_operations = transform.Find("Operations").gameObject;
            m_talentEquipTip = transform.Find("TalentEquipTip").gameObject;


            if (Util.IsPCPlatform())
            {
                RectTransform rectTrans = (RectTransform) m_navigationUI;
                rectTrans.offsetMax = new Vector2(0F, -20F);

                rectTrans = (RectTransform) transform.Find("TopRightBtns");
                rectTrans.offsetMax = new Vector2(0F, -20F);
            }
        }

        public static void InitBgObject()
        {
            if (BgObject == null)
            {
                var bgPrefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "StreakBallRoot");
                BgObject = UIUtil.Instantiate(bgPrefab);
                BgObject.transform.localPosition = new Vector3(0F, 10F, 0F);
                var bg = BgObject.transform.Find("BG/Background");
                if (bg)
                {
                    if (bg.TryGetComponent(out Image img))
                    {
                        m_bgDefaultSprite = img.sprite;
                    }
                }
            }
        }

        private void ResetMainSceneOnDestroy()
        {
        }

        protected override void Start()
        {
            base.Start();
            ScreenChangeHandler.GetInstance().RegisterPost(transform, this);
            AdaptScreen();

            UIEventListener.Get(m_operations.transform.Find("BtnUnequip").gameObject).onClick = OnExchangeClick;
            UIEventListener.Get(m_operations.transform.Find("BtnDetail").gameObject).onClick = OnDetailClick;

            // 直接展示详情
            TryShowingDetailView();
            //刷新
            bubble.ContextDispatcher(transform, StreakBallEvent.ReqStreakBallDetail);
            PlayerStatistics.GetInstance().RecordMessage((int) SNSType.GeneralEvent, (int) ReportEvent.StreakBallMainUIExposed);
        }


        private void OnItemCenteredHandler(SlotInfo slot, OrbInfo orb)
        {
            m_selctedSlot = slot;
            m_selectedOrb = orb;
            OnUpdateBottomUI();
            TryShowNewPlayerGuide();
        }

        private void OnCenterInfoUpdatedHandler(SlotInfo slot, OrbInfo orb)
        {
            m_selctedSlot = slot;
            m_selectedOrb = orb;
        }

        private void AdaptScreen()
        {
        }

        protected override void OnDestroy()
        {
            ScreenChangeHandler.GetInstance().UnRegister(transform);
            m_isDestroying = true;

            try
            {
                StreakBallInGameController.Cleanup();
            }
            catch (Exception e)
            {
                Log.Error($"Clean SSR Ingame exception: {e}", ModuleType.StreakBall);
            }

            if (BgObject)
                Destroy(BgObject);

            ResetMainSceneOnDestroy();
            StreakBallUtil.SetIsInStreakBall(false);
            base.OnDestroy();
            bubble.ContextDispatcher(transform, CommonProtocolKey.StreakBallOnDestroyView);
            StreakBallListObjectPoolCache.ClearAll();
            ResourcesLoader.GetInstance().GarbageCollection();
            Resources.UnloadUnusedAssets();
        }

        #endregion

        #region UI
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

        public void TryShowingDetailView()
        {
            if (showArgument != null && showArgument is ShowStreakBallDetailVO)
            {
                bubble.ContextDispatcher(transform, StreakBallEvent.TryRegisterChildView, showArgument);
            }
            else
            {
                // try show guide
                TryShowNewPlayerGuide();
            }
        }
        #endregion

        #region 主要逻辑
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

        private void OnClickExit(GameObject go)
        {
            PopUpManager.GetInstance().RemovePopUp(gameObject);
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

        public void OnMyItemLoaded()
        {
            UpdateLeftBarCoins();
        }

        public void OnUpdateBeanDiamand(BeanDiamandInfo beanDiamandInfo)
        {
            if (m_moneyBarHandler)
            {
                m_moneyBarHandler.setDiamont(beanDiamandInfo.diamondNumber, false, false);
            }
        }

        #endregion

        #region 背景图片
        #endregion

        #region 左上角按钮
        private void ClearLeftBarCoins()
        {
            foreach (Transform child in m_coinContainer)
            {
                if (child != m_diamondObj && child.gameObject.activeSelf)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void UpdateLeftBarCoins()
        {
            ClearLeftBarCoins();
        }

        #endregion

        #region 新手引导
        public void TryShowNewPlayerGuide()
        {
            if (m_selctedSlot.IsEmpty())
            {
                Tutorial.TutorialController.GetInstance().ShowNewPlayerGuide((int) FlagIndexNew.ShowStreakBallToEquipView);
            }
            else if (m_selectedOrb.CanSelectableItem() == 0)
            {
                Tutorial.TutorialController.GetInstance().ShowNewPlayerGuide((int) FlagIndexNew.ShowTalentSkiilCanSelectItem);
            }
        }
        #endregion

        #region StreakBall
        internal void UIUpdated()
        {
            OnUpdateBottomUI();
        }

        internal void OnUpdateBottomUI()
        {
        }

        private void HandleSelectionGift(OrbSkillState skillState, long endTime)
        {
        }

        private void OnDetailClick(GameObject go)
        {
            ShowCurrentDetail();
            PlayerStatistics.GetInstance().RecordMessage((int) SNSType.ButtonClick, (int) ReportButton.StreakBallMainUIDetailClick);
        }

        private void OnExchangeClick(GameObject go)
        {
            bubble.ContextDispatcher(transform, StreakBallEvent.OpenExchange);
            PlayerStatistics.GetInstance().RecordMessage((int) SNSType.ButtonClick, (int) ReportButton.StreakBallMainUIExchangeClick);
        }

        public void ShowCurrentDetail()
        {
            if (m_selctedSlot !=null)
            {
                showArgument = new ShowStreakBallDetailVO
                {
                    ItemId = m_selectedOrb.ItemId,
                    SlotId = m_selctedSlot.SlotId,
                    TryShowGuide = false,
                    ExitCloseAll = false
                };
                TryShowingDetailView();
            }
        }

        internal void OnGenderChanged()
        {
        }

        internal void GotoLinkEvent(object data)
        {
        }

        internal void ClosePanel()
        {
            PopUpManager.GetInstance().RemovePopUp(gameObject);
        }
        #endregion
    }
}// 自动生成于：8/12/2025 3:37:51 PM
