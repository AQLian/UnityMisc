using System;
using System.Collections.Generic;
using System.Linq;

using HappyMahjong.Common;
using HappyMahjong.SelectionScene;
using HappyMahjong.ShopAndBag;

using SSRItemUpgrade;

using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.mediation.impl;

using UnityEngine;
using UnityEngine.Pool;
using MJWinStreakBallActivity;

namespace HappyMahjong.StreakBallSpace
{
    public class StreakBallMediator : EventMediator
    {
        [Inject] public StreakBallUIView view { get; set; }

        private HashSet<StreakBallPopupHandler> m_childViews = new();

        [Inject] public StreakBallModel model { get; set; }

        [Inject] public StreakBallService service { get; set; }

        public override void OnRegister()
        {
            UpdateListeners(true);
        }

        public override void OnRemove()
        {
            UpdateListeners(false);
            model.ViewCreated = false;
        }

        private void UpdateListeners(bool value)
        {
            dispatcher.UpdateListener(value, TSDKMessageEvent.RecvChangeGenderRsp, OnGenderChanged);
            
            dispatcher.UpdateListener(value, StreakBallEvent.RspGetDetail, OnRspStreakBallService);
            dispatcher.UpdateListener(value, StreakBallEvent.RspStreakBallFail, OnRspStreakBallFail);
            
            dispatcher.UpdateListener(value, SelectionViewEvent.UpdateBeanDiamand, OnUpdateBeanDiamand);
            dispatcher.UpdateListener(value, ShopEvent.MyItemLoaded, OnMyItemLoaded);
            view.dispatcher.UpdateListener(value, SelectionViewEvent.FittonItemList, (evt) => dispatcher.Dispatch(SelectionViewEvent.FittonItemList, evt.data));

            dispatcher.UpdateListener(value, StreakBallEvent.DetailInfoUpdated, OnDetailInfoUpdated);

            dispatcher.UpdateListener(value, StreakBallEvent.GotoLinkEvent, OnGotoLinkEvent);
            dispatcher.UpdateListener(value, StreakBallEvent.TryRegisterChildView, OnTryRegisterChildView);
            dispatcher.UpdateListener(value, StreakBallEvent.TryRemoveChildView, OnTryRemoveChildView);
            dispatcher.UpdateListener(value, StreakBallEvent.ShowCongratulation, OnShowCongratulation);
            dispatcher.UpdateListener(value, StreakBallEvent.SelectableGetSuccess, OnSelectableGetSuccess);

            dispatcher.UpdateListener(value, StreakBallEvent.DoClosePanel, OnDoClosePanel);
        }

        private void OnShowCongratulation(IEvent payload)
        {
            if (payload.data is ExchangeInfo info)
            {
                var rewards = info.ExchangeItems.Select(item => item.Reward).ToList();
                if (rewards != null && rewards.Count > 0)
                {
                    var awardList = new List<CongratulationHandler.Item64>();
                    var hasBean = false;
                    var hasDiamond = false;
                    var hasItem = false;
                    for (int i = 0; i < rewards.Count; i++)
                    {
                        int id = rewards[i].ItemId;
                        int num = rewards[i].ItemNum;
                        if (id > 0)
                        {
                            awardList.Add(new CongratulationHandler.Item64() { nID = id, nCount = num });
                        }

                        if (id == (int) HappyMahjong.ShopAndBag.SpecialItem.Diamond)
                        {
                            hasDiamond = true;
                        }
                        else if (id == (int) HappyMahjong.ShopAndBag.SpecialItem.Bean)
                        {
                            hasBean = true;
                        }
                        else
                        {
                            hasItem = true;
                        }
                    }

                    if (awardList.Count > 0)
                    {
                        CongratulationHandler handler = CongratulationHandler.Instantiate();

                        if (handler != null)
                        {
                            handler.delegateClose = () =>
                            {
                                if (hasDiamond)
                                {
                                    //查询
                                    PersonalInfoQueryByFlag.queryBeansAndDiamond();
                                }
                                else if (hasBean)
                                {
                                    //查询
                                    PersonalInfoQueryByFlag.queryBeans();
                                }

                                if (hasItem)
                                {
                                    HappyBridge.Shop.FetchMyItem.FetchItem();
                                }
                            };

                            handler.SetItem(awardList, true, true, string.Empty, true, false);
                        }
                    }
                }
            }
        }

        private void OnDoClosePanel(IEvent payload)
        {
            if (view != null)
            {
                view.ClosePanel();
            }
        }

        private void OnSelectableGetSuccess(IEvent payload)
        {
        }

        private void OnTryRegisterChildView(IEvent payload)
        {
            var showArgument = payload.data;
            if (showArgument is StreakBallPopupHandler vo)
            {
                m_childViews.Add(vo);
                PlayerStatistics.GetInstance().RecordMessage((int) SNSType.GeneralEvent, (int) ReportEvent.StreakBallDetailUIExposed);
            }
        }
        private void OnTryRemoveChildView(IEvent payload)
        {
            var showArgument = payload.data;
            if (showArgument is StreakBallPopupHandler vo)
            {
                m_childViews.Remove(vo);
            }
        }

        private void OnMainViewResumeShow()
        {
            if (view != null)
            {
                view.TryShowNewPlayerGuide();
            }
        }

        private void OnGotoLinkEvent(IEvent payload)
        {
            if (view != null)
            {
                view.GotoLinkEvent(payload.data);
            }

            CloseAll();
        }

        private void CloseAll()
        {
            var toArray = m_childViews.ToArray();
            m_childViews.Clear();
            foreach (var k in toArray)
            {
                if (k != null)
                    PopUpManager.GetInstance().RemovePopUp(k.gameObject);
            }

            if (view)
            {
                PopUpManager.GetInstance().RemovePopUp(view.gameObject);
            }
        }

        private void OnDetailInfoUpdated(IEvent payload)
        {
            // refresh main ui
            if (view != null)
            {
                view.UIUpdated();
            }

            // refresh child ui
            foreach(var k in m_childViews)
            {
                if(k!=null)
                {
                    k.RefreshUI(model.Info);
                }
            }
        }

        private void OnGenderChanged(IEvent evt)
        {
            if (view)
            {
                view.OnGenderChanged();
            }
        }

        private void OnRspStreakBallService(IEvent evt)
        {
        }

        private void OnRspStreakBallFail(IEvent evt)
        {
        }

        private void OnUpdateBeanDiamand(IEvent evt)
        {
            var beanDiamandInfo = (BeanDiamandInfo) evt.data;
            if (view)
            {
                view.OnUpdateBeanDiamand(beanDiamandInfo);
            }
        }

        private void OnMyItemLoaded(IEvent evt)
        {
            if (view)
            {
                view.OnMyItemLoaded();
            }
        }
    }
}// 自动生成于：8/12/2025 3:37:51 PM
