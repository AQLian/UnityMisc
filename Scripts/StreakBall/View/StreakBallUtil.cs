using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using HappyBridge.FillUpBeanBridge;
using HappyBridge.Util;

using HappyMahjong.Common;
using HappyMahjong.ResHotUpdate;
using HappyMahjong.ReturningActivity;
using HappyMahjong.SelectionScene;
using HappyMahjong.ShopAndBag;
using HappyMahjong.SSRInstituteSpace;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Color = UnityEngine.Color;
using DynamicConfig = HappyMahjong.Common.DynamicConfig;
using Image = UnityEngine.UI.Image;
using Log = HappyMahjong.Common.Log;
using Object = UnityEngine.Object;
using PopupSourceType = HappyMahjong.FillUpBeanSpace.PopupSourceType;
using Util = HappyMahjong.Common.Util;

namespace HappyMahjong.StreakBallSpace
{
    public static class StreakBallUtil
    {
        private static ToastHandlerUgui s_toast;
        public static RawImage gameUIRawImage { get; set; }
        public static RawImage tableUIRawImage { get; set; }
        
        public static IShopModel shopModel { get; set; }
        
        public static bool isInStreakBall { get; private set; }
        private static Transform s_canvasTransform;
        internal static void SetCanvasTransform(Transform canvasTran)
        {
            s_canvasTransform = canvasTran;
        }


        internal static Action s_adaptScreenForInGameCallback;
        
        public static void AdaptScreenForInGame()
        {
            s_adaptScreenForInGameCallback?.Invoke();
        }

        internal static void SetShopModel(IShopModel aShopModel)
        {
            shopModel = aShopModel;
        }

        internal static void SetIsInStreakBall(bool state)
        {
            isInStreakBall = state;
        }

        private static bool s_hasAnyItems = false;
        public static bool HasAnyItems => s_hasAnyItems;
        internal static void SetHasAnyItems(bool state)
        {
            s_hasAnyItems = state;
        }
        
        public static void ShowToast(string msg, float time = 1.5F)
        {
            ToastHandlerUgui handler;
            
            if (s_toast)
            {
                handler = s_toast;
            }
            else
            {
                GameObject template = (GameObject) Resources.Load("Toast_ab/Toastu");
                GameObject obj = Object.Instantiate(template);
                handler = s_toast = obj.GetComponent<ToastHandlerUgui>();
            }
            
            handler.setText(msg, -1);
            handler.show(time, false, 0);

            handler.transform.SetParent(s_canvasTransform, false);
            handler.transform.localPosition = (Vector2) handler.transform.localPosition;

            Canvas canvas = handler.GetComponent<Canvas>();
            canvas.sortingLayerName = "Exhibition";
            canvas.sortingOrder = 10;
        }

        // 获取当前已装备的道具,包括背包中临时穿戴的（未上报服务器）
        public static List<int> GetCurrentFitonItemId(bool bFilterShop = true)
        {
            var result = new List<int>();
            if (MainUIStateCenter.GetInstance().currentState != (int) MainUIState.Shop)
            {
                return result;
            }

            var shopMainUI = GameObject.Find("SelectionSceneRoot/UI Root/MainUI/ShopUIView/ShopMainUI");
            if (shopMainUI == null)
            {
                Log.Info("shopMainUI is null, can not init table pos", ModuleType.StreakBall);
                return result;
            }

            var shopPanel = shopMainUI.transform.Find("Panel");
            if (shopPanel != null && shopPanel.gameObject.activeInHierarchy)
            {
                var shopPanelHandler = shopPanel.GetComponent<ShopAndBag.ShopPanelHandler>();

                // 默认只有背包临时穿戴的需要返回
                if (!shopPanelHandler.IsShop() || !bFilterShop)
                {
                    var shopMainUIHandler = shopPanelHandler.GetComponentInParent<ShopMainUIHandler>();
                    var fitonManager = shopMainUIHandler.FitonManager;
                    return fitonManager.GetFitonItemId();
                }
            }
            
            return result;
        }

        public static bool IsItemFitoned(int itemID)
        {
            if (MainUIStateCenter.GetInstance().currentState == (int) MainUIState.Shop)
            {
                var currentFitonItems = GetCurrentFitonItemId();
                return currentFitonItems.Contains(itemID);
            }
            
            
            var fitonItems = ShopDataHelper.GetInstance().GetFitonItemId();
            return fitonItems.Contains(itemID);
        }

        public static void FitonItem(Item item)
        {
            if (MainUIStateCenter.GetInstance().currentState == (int) MainUIState.Shop)
            {
                var shopMainUI = GameObject.Find("SelectionSceneRoot/UI Root/MainUI/ShopUIView/ShopMainUI");
                if (shopMainUI == null)
                {
                    Log.Info("shopMainUI is null, can not init table pos", ModuleType.StreakBall);
                    return;
                }

                var shopPanel = shopMainUI.transform.Find("Panel");
                if (shopPanel != null && shopPanel.gameObject.activeInHierarchy)
                {
                    var shopPanelHandler = shopPanel.GetComponent<ShopAndBag.ShopPanelHandler>();

                    // bag
                    if (!shopPanelHandler.IsShop())
                    {
                        // var shopMainUIHandler = shopPanelHandler.GetComponentInParent<ShopMainUIHandler>();
                        // var fitonManager = shopMainUIHandler.FitonManager;
                        // fitonManager.FitonItem(item);
                        
                        new BubbleEventHelper().BubbleDispatch(shopPanelHandler.transform, ShopUIEvent.OnSpecialItemSelected, item.ID);
                        
                        ShopUIupdateMsg msgData = new ShopUIupdateMsg();
                        msgData.msg = ShopUpdateUI.UpdateUI;
                        msgData.dataQuery = shopModel.GetDataQuery();
                        new BubbleEventHelper().ContextDispatcher(shopPanelHandler.transform, ShopEvent.UpdateUI, msgData);
                        return;
                    }
                }
            }
            
            List<int> fitonItems = ShopDataHelper.GetInstance().GetFitonItemId();
            if (!fitonItems.Contains(item.ID))
            {
                // dispatch
                new BubbleEventHelper().ContextDispatcher(null, SelectionViewEvent.FittonItemList, new List<int>{ item.ID });
            }
        }

        public static void UnFitonItem(Item item)
        {
            if (MainUIStateCenter.GetInstance().currentState == (int) MainUIState.Shop)
            {
                var shopMainUI = GameObject.Find("SelectionSceneRoot/UI Root/MainUI/ShopUIView/ShopMainUI");
                if (shopMainUI == null)
                {
                    Log.Info("shopMainUI is null, can not init table pos", ModuleType.StreakBall);
                    return;
                }

                var shopPanel = shopMainUI.transform.Find("Panel");
                if (shopPanel != null && shopPanel.gameObject.activeInHierarchy)
                {
                    var shopPanelHandler = shopPanel.GetComponent<ShopAndBag.ShopPanelHandler>();

                    // bag
                    if (!shopPanelHandler.IsShop())
                    {
                        // var shopMainUIHandler = shopPanelHandler.GetComponentInParent<ShopMainUIHandler>();
                        // var fitonManager = shopMainUIHandler.FitonManager;
                        // fitonManager.UnselectItem(item.ID);
                        
                        new BubbleEventHelper().BubbleDispatch(shopPanelHandler.transform, ShopUIEvent.OnSpecialItemUnSelected, item.ID);
                        
                        ShopUIupdateMsg msgData = new ShopUIupdateMsg();
                        msgData.msg = ShopUpdateUI.UpdateUI;
                        msgData.dataQuery = shopModel.GetDataQuery();
                        new BubbleEventHelper().ContextDispatcher(shopPanelHandler.transform, ShopEvent.UpdateUI, msgData);
                        return;
                    }
                }
            }
            
            List<int> fitonItems = ShopDataHelper.GetInstance().GetFitonItemId();
            if (fitonItems.Contains(item.ID))
            {
                // dispatch
                new BubbleEventHelper().ContextDispatcher(null, SelectionViewEvent.UnFittonItemList, new List<int>{ item.ID });
            }
        }
        
        private static GameObject s_activeTips;

        public static void ShowTips(GameObject tipsObj, Transform trans, Transform parent, LayoutDirection layoutDirection = LayoutDirection.Top, Vector2 extraOffset = new Vector2())
        {
            if (s_activeTips && s_activeTips != tipsObj)
            {
                Object.Destroy(s_activeTips);
            }
            s_activeTips = tipsObj;
            
            Canvas canvas = tipsObj.GetComponent<Canvas>();
            Canvas parentCanvas = trans.GetComponentInParent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerName = parentCanvas.sortingLayerName;
            
            
            RectTransform rectTrans = (RectTransform) tipsObj.transform;

            float offset = 48F;
            if (layoutDirection == LayoutDirection.Top)
            {
                rectTrans.pivot = new Vector2(0.5F, 0F);
                rectTrans.anchoredPosition = new Vector2(0, offset);
            }
            else if (layoutDirection == LayoutDirection.Bottom)
            {
                rectTrans.pivot = new Vector2(0.5F, 1F);
                rectTrans.anchoredPosition = new Vector2(0, -offset);
            }
            else if (layoutDirection == LayoutDirection.Left)
            {
                rectTrans.pivot = new Vector2(1F, 0.5F);
                rectTrans.anchoredPosition = new Vector2(-offset, 0);
            }
            else if (layoutDirection == LayoutDirection.Right)
            {
                rectTrans.pivot = new Vector2(0F, 0.5F);
                rectTrans.anchoredPosition = new Vector2(offset, 0);
            }
            else if (layoutDirection == LayoutDirection.Center)
            {
                rectTrans.pivot = new Vector2(0.5F, 0.5F);
                rectTrans.anchoredPosition = Vector2.zero;
            }

            rectTrans.anchoredPosition += extraOffset;


            rectTrans.parent = parent;

            RectTransform container = rectTrans.Find("Container") as RectTransform;
            if (container)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            }
            else
            {
                container = (RectTransform) tipsObj.transform;
            }
        }
        
        public static void ShowTips(string tips, Transform trans, Transform parent, LayoutDirection layoutDirection = LayoutDirection.Top, Vector2 extraOffset = new Vector2(), string prefabName = null)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                prefabName = "CommonTips";
            }

            GameObject prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, prefabName);
            GameObject tipsObj = UIUtil.Instantiate(prefab, trans);

            RectTransform rectTrans = (RectTransform) tipsObj.transform;
            var block = rectTrans.Find("Block");
            if (block)
            {
                UIEventListener.Get(block.gameObject).onClick = (go) =>
                {
                    Object.Destroy(tipsObj);
                };
            }

            var name = rectTrans.Find("Container/Name") as RectTransform;
            if (name)
            {
                Util.SetUIText(name, tips);

                float preferWidth = LayoutUtility.GetPreferredWidth(name);
                Vector2 oldSizeDelta = name.sizeDelta;
                if (preferWidth < oldSizeDelta.x)
                {
                    name.sizeDelta = new Vector2(preferWidth, oldSizeDelta.y);
                }
            }
            
            ShowTips(tipsObj, trans, parent, layoutDirection, extraOffset);
        }

        public static void ClearTips()
        {
            if (s_activeTips)
            {
                Object.Destroy(s_activeTips);
                s_activeTips = null;
            }
        }

        public static string ItemTypeToDisplayName(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Pendant:
                case ItemType.OutPendant:
                    return "背饰";
                case ItemType.Thumb:
                case ItemType.LittleFinger:
                case ItemType.MiddleFinger:
                case ItemType.Ring:
                    return "戒指";
                case ItemType.Tattoo:
                    return "纹身";
                case ItemType.Bracelets:
                    return "手环";
            }
            return string.Empty;
        }
        
        public static string GetFormatedNumber(int num)
        {
            if (num < 10000)
                return num.ToString();

            float w = num / 10000f;
            return $"{w:F1}万";
        }

        public static string GetConfigString(string key, string defaultValue)
        {
            return DynamicConfig.GetInstance().GetString(UIDef.ConfigKey, key, defaultValue);
        }

        public static int GetConfigInt(string key, int defaultValue)
        {
            return DynamicConfig.GetInstance().GetInt(UIDef.ConfigKey, key, defaultValue);
        }

        public static Transform FindClosestIndex(List<Transform> transforms, Vector3 targetLocalPos, out int closestIndex)
        {
            closestIndex = -1;

            if (transforms == null || transforms.Count == 0)
                return null;

            float minSqrDistance = float.MaxValue;
            Transform closest = null;

            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                if (t == null)
                    continue; 

                float sqrDistance = (t.localPosition - targetLocalPos).sqrMagnitude;
                if (sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    closestIndex = i;
                    closest = t;
                }
            }

            return closest;
        }



        /// <summary>
        /// 技能冷却剩余时间展示
        /// </summary>
        /// <param name="remainingSeconds"></param>
        /// <returns></returns>
        public static string FormatCountdown(int remainingSeconds)
        {
            if(remainingSeconds < 0)
            {
                remainingSeconds = 0;
            }

            var ts = TimeSpan.FromSeconds(remainingSeconds);
            var totalHours = ts.TotalHours;

            // 长周期：剩余 > 24小时
            if (totalHours > 24)
            {
                return string.Format(LangKeys.skillCooldownRemainLong, ts.Days, ts.Hours);
            }
            // 中周期：1小时 < 剩余 ≤ 24小时
            else if (totalHours > 1)
            {
                return string.Format(LangKeys.skillCooldownRemainMiddle, ts.Hours, ts.Minutes);
            }
            // 短周期：剩余 ≤ 1小时
            else
            {
                return string.Format(LangKeys.skillCooldownRemainShort, ts.Minutes, ts.Seconds);
            }
        }


        // ugui utility
        public static Vector2 ConvertScreenDeltaToLocalDelta(PointerEventData eventData, RectTransform rect, out bool success)
        {
            if (eventData == null || rect == null)
            {
                success = false;
                return Vector2.zero;
            }
            var currentScreenPos = eventData.position;
            var previousScreenPos = currentScreenPos - eventData.delta;
            var convertedCurrent = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                currentScreenPos,
                eventData.pressEventCamera, // Auto-null for Overlay canvases
                out var currentLocalPos);
            var convertedPrevious = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                previousScreenPos,
                eventData.pressEventCamera,
                out var previousLocalPos);
            success = convertedCurrent && convertedPrevious;
            return success ? currentLocalPos - previousLocalPos : Vector2.zero;
        }

        // convert PointerEventData.delta -> localSpace delta
        // this will make like ondrag event control RectTranform position(.anchoredPosition+=delta)
        // exact match local space coordinate
        public static Vector2 ConvertScreenDeltaToLocalDelta(PointerEventData eventData, RectTransform rect)
        {
            return ConvertScreenDeltaToLocalDelta(eventData, rect, out _);
        }

        public static string AddNewLineAfterEachChar(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                result.Append(input[i]);
                if (i != input.Length - 1)
                {
                    result.Append('\n');
                }
            }
            return result.ToString();
        }

        public static void SetAllChildActive(Transform t, bool state)
        {
            if (t)
            {
                foreach(Transform child in t)
                {
                    child.gameObject.SetActive(state);
                }
            }
        }

        public static string GetFileSizeString(long fileSize)
        {
            if (fileSize < 1024)
                return $"{fileSize}B";
            if (fileSize < 1024 * 1024)
                return $"{fileSize / 1024f:0.#}K";
            if (fileSize < 1024L * 1024 * 1024)
                return $"{fileSize / 1024f / 1024f:0.#}M";
            if (fileSize < 1024L * 1024 * 1024 * 1024)
                return $"{fileSize / 1024f / 1024f / 1024f:0.#}G";
            return $"{fileSize / 1024f / 1024f / 1024f / 1024f:0.#}T";
        }

        public static bool IsActResourceReady(string bindAct)
        {
            if (string.IsNullOrEmpty(bindAct))
            {
                return false;
            }

            return ActivityResHelper.GetInstance().IsResourceReady(bindAct);
        }

        public static void SetBtnGray(GameObject btn, bool value)
        {
            if (btn)
            {
                var img = btn.GetComponent<UnityEngine.UI.Image>();
                if (img)
                {
                    Sprite sp;
                    if (value)
                    {
                        sp = UIUtil.GetSprite(UIUtil.CommonUIPath, "Btn_Grey_Small");
                    }
                    else
                    {
                        sp = UIUtil.GetSprite(UIUtil.CommonUIPath, "Btn_Yellow_Small");
                    }

                    if (sp)
                    {
                        img.sprite = sp;
                    }
                }
            }
        }


        /// <summary>
        /// 解析从主域传递过来的数据
        /// </summary>
        /// <returns></returns>
        public static MainArenaServiceDataJson ParseMainArenaServiceDataJson(string json)
        {
            MainArenaServiceDataJson result = new MainArenaServiceDataJson();
            try
            {
                Dictionary<string, object> jsonDic = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;

                if (jsonDic.ContainsKey("ResultID"))
                {
                    result.ResultID = System.Convert.ToInt32(jsonDic["ResultID"]);
                }

                if (jsonDic.ContainsKey("SceneID"))
                {
                    result.SceneID = System.Convert.ToInt32(jsonDic["SceneID"]);
                }

                if (jsonDic.ContainsKey("PopupSourceType"))
                {
                    result.PopupSourceType = System.Convert.ToInt32(jsonDic["PopupSourceType"]);
                }

                if (jsonDic.ContainsKey("PanelType"))
                {
                    result.PanelType = System.Convert.ToInt32(jsonDic["PanelType"]);
                }

                if (jsonDic.ContainsKey("PanelName"))
                {
                    result.PanelName = (string) jsonDic["PanelName"];
                }

                if (jsonDic.ContainsKey("rule_id"))
                {
                    result.rule_id = System.Convert.ToInt32(jsonDic["rule_id"]);
                }

                if (jsonDic.ContainsKey("report_type"))
                {
                    result.report_type = System.Convert.ToInt32(jsonDic["report_type"]);
                }

                if (jsonDic.ContainsKey("MyDiamend"))
                {
                    result.MyDiamend = System.Convert.ToInt32(jsonDic["MyDiamend"]);
                }

                if (jsonDic.ContainsKey("BuyBeansFrom"))
                {
                    result.BuyBeansFrom = System.Convert.ToInt32(jsonDic["BuyBeansFrom"]);
                }

                if (jsonDic.ContainsKey("GameType"))
                {
                    result.GameType = System.Convert.ToInt32(jsonDic["GameType"]);
                }
            }
            catch (Exception ex)
            {
                Log.Info("ParseMainArenaServiceDataJson exception: " + ex.ToString(), ModuleType.StreakBall);
            }

            return result;
        }

        // 使用SceneID上报SceneID，否则使用GameType
        public static bool IsUseSceneIDSourceType(int sourceType)
        {
            if (sourceType == (int) PopupSourceType.InGameBankrupt ||
                sourceType == (int) PopupSourceType.InBalanceNormal ||
                sourceType == (int) PopupSourceType.InGameNextGame ||
                sourceType == (int) PopupSourceType.InGameIconClick ||
                sourceType == (int) PopupSourceType.EnterRoom ||
                sourceType == (int) PopupSourceType.InBalanceIconClick)
            {
                return true;
            }


            if (sourceType == (int) PopupSourceType.InSelectRoomAfterExitGameScene)
            {
                return true;
            }

            return false;
        }

        // 局内的弹窗类型
        public static bool IsInGameSourceType(int sourceType)
        {
            if (sourceType == (int) PopupSourceType.InGameBankrupt ||
                sourceType == (int) PopupSourceType.InBalanceNormal ||
                sourceType == (int) PopupSourceType.InGameNextGame ||
                sourceType == (int) PopupSourceType.InGameIconClick ||
                sourceType == (int) PopupSourceType.InBalanceIconClick)
            {
                return true;
            }

            return false;
        }

        // 正常进场或者点击下一局都算， 目前礼包购买只有这两个路径需要判断触发
        public static bool IsEnterGameSourceType(int sourceType)
        {
            return sourceType == (int) PopupSourceType.EnterRoom || sourceType == (int) PopupSourceType.InGameNextGame;
        }

        public static bool IsSelectionIconSourceType(int sourceType)
        {
            return sourceType == (int) PopupSourceType.InSelectionRoomIconClick;
        }

        public static FillUpBeanData.PanelHookData ParsePanelHookDataFromBaseData(string json)
        {
            FillUpBeanData.PanelHookData result = new FillUpBeanData.PanelHookData();

            try
            {
                Dictionary<string, object> jsonDic = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;

                if (jsonDic.ContainsKey("PanelType"))
                {
                    result.PanelType = System.Convert.ToInt32(jsonDic["PanelType"]);
                }

                if (jsonDic.ContainsKey("PanelName"))
                {
                    result.PanelName = (string) jsonDic["PanelName"];
                }

                if (jsonDic.ContainsKey("PopupSourceType"))
                {
                    result.PopupSourceType = System.Convert.ToInt32(jsonDic["PopupSourceType"]);
                }

                if (jsonDic.ContainsKey("ResultCode"))
                {
                    result.ResultCode = System.Convert.ToInt32(jsonDic["ResultCode"]);
                }
            }
            catch (Exception ex)
            {
                Log.Info("ParsePanelHookDataFromBaseData exception: " + ex.ToString(), ModuleType.StreakBall);
            }


            return result;
        }


        #region 活动数据上报
        public static void RecordEvent(int snsSubType, int count = 1)
        {
            string openId = PlayerLoginDataMgr.GetInstance().GetOpenId().TrimEnd('\0');
            uint playerUin = PlayerDataMgr.GetInstance().GetSelfInfo().playerUin;

#if UNITY_EDITOR
            playerUin = 333366666;
            openId = "F7AC586AB9370FA9F999FAB9A8448B55";
#endif

            PlayerStatistics.GetInstance().RecordMessage((int) HappyMahjong.SNSType.GeneralEvent, snsSubType, count);
            Log.Info(string.Format("SNSType:{0} snsSubType:{1} count:{2} uin:{3} openid:{4}",
                (int) HappyMahjong.SNSType.GeneralEvent,
                snsSubType, count, playerUin, openId), ModuleType.StreakBall);
        }


        public static void RecordEventWithReportStr(int snsSubType, List<string> reportStr)
        {
            string openId = PlayerLoginDataMgr.GetInstance().GetOpenId().TrimEnd('\0');
            uint playerUin = PlayerDataMgr.GetInstance().GetSelfInfo().playerUin;

#if UNITY_EDITOR
            playerUin = 333366666;
            openId = "F7AC586AB9370FA9F999FAB9A8448B55";
#endif

            HappyMahjong.PlayerStatistics.GetInstance().RecordMessage((int) HappyMahjong.SNSType.GeneralEvent, snsSubType, 1, 0, reportStr);
            Log.Info(string.Format("SNSType:{0} snsSubType:{1} count:{2} uin:{3} openid:{4} reportStr:{5}",
                (int) HappyMahjong.SNSType.GeneralEvent, snsSubType, 1, playerUin, openId, string.Join(",", reportStr)), ModuleType.StreakBall);
        }

        public static void RecordBtnEvent(int snsSubType)
        {
            PlayerStatistics.GetInstance().RecordButtonClickCount(snsSubType);
            Log.Info(string.Format("SNSType:{0} snsSubType:{1}", (int) HappyMahjong.SNSType.ButtonClick,
                snsSubType), ModuleType.StreakBall);
        }

        #endregion

        internal static bool CheckCanReactShowPanel(int sceneID)
        {
            return true;
        }

        internal static void SetCoinIcon(Transform coinObj)
        {
            Item item = ShopDataHelper.GetInstance().GetItem(StreakBallConfig.CoinId);
            if (item == null)
            {
                return;
            }

            ShopDataHelper.GetInstance().GetSpriteName(item.icon, out string atlasName, out string spritename);
            ShopUIHelper.LoadIcon(atlasName, spritename, (sprite, oData) =>
            {
                if (coinObj && sprite)
                {
                    var image = coinObj.transform.Find("btn_add").GetComponent<Image>();
                    image.sprite = sprite;
                    image.color = Color.white;
                    float width = sprite.rect.width;
                    float height = sprite.rect.height;
                    float ratio = width / height;
                    if (ratio > 1F)
                    {
                        image.transform.localScale = new Vector3(1F, 1F / ratio, 1F);
                    }
                    else
                    {
                        image.transform.localScale = new Vector3(ratio, 1F, 1F);
                    }
                    if (image.transform.childCount > 0)
                    {
                        Vector3 scale = image.transform.localScale;
                        image.transform.GetChild(0).localScale = new Vector3(1F / scale.x, 1F / scale.y, 1F);
                    }
                }
            });
        }

        public static void LogPlatform(string str)
        {
            if (Util.IsEditorPlatform())
            {
                Debug.Log($"{GetPrintTime()} {ModuleType.GardenParty}-->{str}");
            }
            else
            {
                Log.Info(str, ModuleType.GardenParty);
            }
        }

        public static string GetPrintTime()
        {
            var dtTime = ServerTime.GetDateTimeFromUnixTime(ServerTime.GetInstance().GetServerTime());

            string strMin = dtTime.Minute >= 10 ? $"{dtTime.Minute}" : $"0{dtTime.Minute}";

            return ($"{dtTime.Hour}:{strMin}");
        }
    }

    public class StreakBallListObjectPool<T> : StreakBallListObjectPoolCache where T : MonoBehaviour
    {
        private GameObject m_itemPrefab;
        private List<T> m_itemList;

        private static StreakBallListObjectPool<T> s_instance;
        public static StreakBallListObjectPool<T> Get()
        {
            if (s_instance == null)
            {
                s_instance = new StreakBallListObjectPool<T>();
            }
            return s_instance;
        }

        public static StreakBallListObjectPool<T> Get(GameObject itemPrefab)
        {
            if (s_instance == null)
            {
                s_instance = new StreakBallListObjectPool<T>();
            }
            s_instance.Init(itemPrefab);
            return s_instance;
        }

        public void SetItemInfo<TData>(List<TData> itemDataList, Action<T, TData, int> setDataCallback)
        {
            int itemIndex = 0;
            if (itemDataList != null)
            {
                for (int i = 0; i < itemDataList.Count; i++)
                {
                    var itemData = itemDataList[i];
                    var itemHandler = GetItem(itemIndex);
                    // itemHandler.SetExchangeItem(item,currencyCount);
                    if (setDataCallback != null)
                    {
                        setDataCallback(itemHandler, itemData, i);
                    }
                    itemHandler.transform.SetSiblingIndex(itemIndex);
                    itemHandler.gameObject.SetActive(true);
                    itemIndex++;
                }
            }

            var itemList = GetItemList();
            if (itemList != null)
            {
                for (int i = itemIndex; i < itemList.Count; i++)
                {
                    if (itemList[i] != null && itemList[i].gameObject != null)
                    {
                        itemList[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public StreakBallListObjectPool<T> Init(GameObject itemPrefab)
        {
            m_itemPrefab = itemPrefab;
            m_itemList = new List<T>();
            return this;
        }

        public T GetItem(int itemIndex)
        {
            if (m_itemPrefab == null)
            {
                return default(T);
            }

            T itemHandler = null;
            if (itemIndex < m_itemList.Count)
            {
                itemHandler = m_itemList[itemIndex];
            }
            else
            {
                itemHandler = GameObject.Instantiate(m_itemPrefab, m_itemPrefab.transform.parent)
                    .GetOrAddComponent<T>();
                m_itemList.Add(itemHandler);
            }

            return itemHandler;
        }


        public List<T> GetItemList()
        {
            return m_itemList;
        }

        public override void ReleaseListObjectPool()
        {
            if (m_itemList != null)
            {
                foreach (var item in m_itemList)
                {
                    if (item != null && item.gameObject != null)
                    {
                        GameObject.DestroyImmediate(item.gameObject);
                    }
                }
                m_itemList.Clear();
                m_itemList = null;
            }

            if (m_itemPrefab != null)
            {
                GameObject.DestroyImmediate(m_itemPrefab);
                m_itemPrefab = null;
            }

            s_instance = null;
        }
    }

    public class StreakBallListObjectPoolCache
    {
        private static List<StreakBallListObjectPoolCache> s_cacheList;
        public StreakBallListObjectPoolCache()
        {
            if (s_cacheList == null)
            {
                s_cacheList = new List<StreakBallListObjectPoolCache>();
            }
            s_cacheList.Add(this);
        }

        public static void ClearAll()
        {
            if (s_cacheList != null)
            {
                foreach (var item in s_cacheList)
                {
                    if (item != null)
                    {
                        item.ReleaseListObjectPool();
                    }
                }
                s_cacheList.Clear();
                s_cacheList = null;
            }
        }

        public virtual void ReleaseListObjectPool()
        {
        }
    }


    public enum LayoutDirection
    {
        Top,
        Left,
        Right,
        Bottom,
        Center
    }
}

