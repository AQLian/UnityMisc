using HappyMahjong.Audio;
using HappyMahjong.ChoiceSex;
using HappyMahjong.Common;
using HappyMahjong.SelectionScene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TalentPavillion;
using System.Collections.Generic;
using HappyMahjong.ShopAndBag;
using System;

namespace HappyMahjong.StreakBallSpace
{
    public class OptionalSelectableItemPanelHandler : MonoBehaviour
    {
        private Transform m_btnClose;
        private Transform m_btnRed;
        private TextMeshProUGUI m_hint;
        private TextMeshProUGUI m_selName;
        private Transform m_itemShelf;
        private Transform m_template;
        private ToggleGroup m_toggleGroup;
        private Transform m_title;
        private Transform m_subTitle;
        private List<SelectableItem> m_items;
        private SelectableItem m_selected;
        private int m_lastSelectedId;
        private bool m_init;
        private bool m_closeBySure;
        
        #region 实例化接口

        public Action<bool, SelectableItem> delegateClose { get; set; }
        public static OptionalSelectableItemPanelHandler Instantiate(List<SelectableItem> items, int lastSelectedId, Action<bool, SelectableItem> callback)
        {
            var prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, "UseOptionalSelectableItemPanel");
            if (prefab != null)
            {
                var ins = UIUtil.Instantiate(prefab);
                if (ins != null)
                {
                    var handler = ins.GetOrAddComponent<OptionalSelectableItemPanelHandler>();
                    if (handler != null)
                    {
                        handler.SetSelectableItem(items, lastSelectedId);
                        handler.delegateClose += callback;
                        PopUpManager.GetInstance().AddPopUp(ins, PopUpType.UGUI);
                        return handler;
                    }
                }
            } 

            return null;
        }
        
        #endregion
        
        public void SetSelectableItem(List<SelectableItem> items, int lastSelected)
        {
            m_items = items;
            m_lastSelectedId = lastSelected;
            RefreshUI();
        }

        private bool m_showSelName;
        
        protected void Awake()
        {
            m_showSelName =DynamicConfig.GetInstance().GetBool(UIDef.ConfigKey, "IsShowSelName", false);
            InitNode();
        }

        public void OnDestroy()
        {
            delegateClose?.Invoke(m_closeBySure, m_selected);
        }

        private void InitNode()
        {
            if (m_init)
            {
                return;
            }

            m_title = transform.Find("GamePanel/Popwindow_bk/Title_Bk/Title");
            m_subTitle = transform.Find("GamePanel/SubTitle");
            
            m_btnClose = transform.Find("GamePanel/Btn_Close");
            UIEventListener.Get(m_btnClose.gameObject, ClickableTypeDef.CloseSoundType).onClick = go =>
            {
                PopUpManager.GetInstance().RemovePopUp(gameObject);
            };
            
            m_btnRed = transform.Find("GamePanel/Popwindow_bottom/Btn_Red");
            m_hint = transform.Find("GamePanel/Popwindow_bottom/Hint").GetComponent<TextMeshProUGUI>();
            m_selName = transform.Find("GamePanel/Popwindow_bottom/SelName").GetComponent<TextMeshProUGUI>();
            m_btnRed.GetComponent<ButtonListener>().onClick.AddListener(OnClickSure);

            m_itemShelf = transform.Find("GamePanel/Scroll/Viewport/ItemShelf");
            m_template = m_itemShelf.Find("template");

            m_toggleGroup = transform.GetComponent<ToggleGroup>();

            m_init = true;
        }

        private void RefreshUI()
        {
            if (!m_init)
            {
                InitNode();
            }
            
            if (m_items==null || m_items.Count ==0)
            {
                Log.Info($"no selectable item info", ModuleType.StreakBall);
                return;
            }

            var isOn = true;
            var showCnt = 0; // 已经展示物品的数量
            for (var  i = 0; i < m_items.Count; i++)
            {
                var elem = m_items[i];
                var itemId = elem.ItemId;

                if (itemId == (int) SpecialItem.Bean || itemId == (int) SpecialItem.Diamond)
                {
                    AddItem(i, elem);
                    continue;      
                }
                
                // 如果找不到对应的物品，跳过
                var item0 = ShopDataHelper.GetInstance().GetItem(itemId);
                if (item0 == null)
                {
                    Log.Info($"item {itemId} not found", ModuleType.StreakBall);
                    continue;
                }

                // 如果道具有性别要求，过滤掉非当前性别的道具
                if (item0.gender != UserGender.UnKnown && item0.gender != ShopDataHelper.GetInstance().getGender())
                {
                    continue;
                }

                AddItem(i, elem, isOn);
                isOn = false;
                showCnt++;
            }
        }

        private void AddItem(int index, SelectableItem selectable, bool bSelect = false)
        {
            var itemId = selectable.ItemId;
            var itemCount = selectable.Count;
            var ins = UIUtil.Instantiate(m_template.gameObject, m_template.parent);
            ins.name = "item" + index;

            var handler = ins.AddComponent<ItemIconHandler>();
            //handler.SetShowLevel(true);
            handler.SetShowLimit(true);
            handler.SetItem(itemId, itemCount);

            var txt = ins.transform.Find("LabelBG/Label").GetComponent<TextMeshProUGUI>();
            var item = ShopDataHelper.GetInstance().GetItem(itemId);
            var itemName = item.itemConfig.name;
            txt.text = $"{itemName}";

            var limit = ins.transform.Find("LimitBg/Text").GetComponent<TextMeshProUGUI>();
            limit.text = ShopDataHelper.GetInstance().GetItemTimeOrCount(item, itemCount);
            limit.transform.parent.gameObject.SetActive(true);
                
            //ShopUIHelper.SetLevel(ins.transform.Find("Level"), item);
            //var tips = ins.GetOrAddComponent<ItemTipsTrigger>();
            //if (tips != null)
            //{
            //    tips.enabled = true;
            //    tips.item = item;
            //}

            // 隐藏选中特效
            var selectLine = ins.transform.Find("SelectedLine");
            selectLine.gameObject.SetActive(bSelect);
            
            // 配置选中按钮
            var tfsToggle = ins.transform.Find("Toggle");
            var toggle = tfsToggle.GetComponent<Toggle>();
            toggle.group = m_toggleGroup;
            toggle.isOn = bSelect;
            if (bSelect)
            {
                m_selected = selectable;
                m_hint.text = !string.IsNullOrEmpty(item.descValidTime) ? item.descValidTime : item.descContent;
                if (m_showSelName)
                {
                    m_selName.text = itemName;
                }
            }
            ins.transform.Find("LastSel").gameObject.SetActive(m_lastSelectedId != 0 && m_lastSelectedId == itemId);
            
            toggle.onValueChanged.AddListener(isOn =>
            {
                selectLine.gameObject.SetActive(isOn);
                if (isOn)
                {
                    m_selected = selectable;
                    m_hint.text = !string.IsNullOrEmpty(item.descValidTime) ? item.descValidTime : item.descContent;
                    if (m_showSelName)
                    {
                        m_selName.text = itemName;
                    }
                    AudioController.Instance.PlayAuto("qiehuan", AudioLayers.Oneshot);
                }
            });
            
            tfsToggle.SetAsLastSibling();
            
            // 产品认为选中框太小了，点击行为改成选中 + 预览
            //var tipsHandler = ins.GetOrAddComponent<ItemTipsTrigger>();
            //if (tipsHandler != null)
            //{
            //    System.Action trick = () =>
            //    {
            //        if (toggle != null)
            //        {
            //            toggle.isOn = true;
            //            toggle.onValueChanged?.Invoke(true);
            //        }
            //    };
                
            //    tipsHandler.onShowExhibition -= trick;
            //    tipsHandler.onShowExhibition += trick;
            //}
            //else
            //{
                UIEventListener.Get(ins, ClickableTypeDef.SwitchSoundType).onClick = (go) =>
                {
                    toggle.isOn = true;
                    toggle.onValueChanged?.Invoke(true);
                };
            //}
           
            ins.SetActive(true);
        }
        
        private void OnClickSure()
        {
            AudioController.Instance.PlayAuto("ui_click", AudioLayers.Oneshot);
            if (m_items == null || m_items.Count == 0 || m_selected == null)
            {
                return;
            }

            m_closeBySure = true;
            PopUpManager.GetInstance().RemovePopUp(gameObject);
        }
    }
}
