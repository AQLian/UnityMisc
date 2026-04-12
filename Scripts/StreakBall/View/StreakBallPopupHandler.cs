using System.Collections;
using System.Collections.Generic;

using HappyBridge.Audio;

using HappyMahjong.Common;
using HappyMahjong.Setting;

using MJWinStreakBallActivity;
using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{

    public class StreakBallPopupHandler : BubbleBehaviour
    {
        protected virtual void BindPopupBackBtn(Transform backBtn)
        {
            //返回按钮
            if (backBtn != null)
            {
                UIEventListener.Get(backBtn.gameObject).onClick = (backBtnObj) =>
                {
                    AudioController.GetInstance().PlayAuto("guanbi", HappyMahjong.Audio.AudioLayers.Oneshot);
                    ClosePopupHandler();
                };
            }

            //绑定ESC键
            var backComponent = gameObject.GetOrAddComponent<BackComponent>();
            if (backComponent != null)
            {
                backComponent.backLogicDelegate = ClosePopupHandler;
            }

            //弹窗
            PopUpManager.GetInstance().AddPopUp(gameObject, PopUpType.UGUI);
        }

        public virtual void ClosePopupHandler()
        {
            if (gameObject != null)
            {
                bubble.ContextDispatcher(transform, StreakBallEvent.TryRemoveChildView, this);
                PopUpManager.GetInstance().RemovePopUp(gameObject);
            }
        }

        public static TPopup CreatePopupHandler<TPopup>(string prefabName) where TPopup : StreakBallPopupHandler
        {
            var prefab = UIUtil.LoadPrefab(UIDef.StreakBallABPath, prefabName);
            if (prefab != null)
            {
                var instance = GameObject.Instantiate(prefab);
                PopUpManager.GetInstance().AddPopUp(instance, PopUpType.UGUI);
                Util.SetEffectSortingLayer(instance.transform);
                var tPopup = instance.GetOrAddComponent<TPopup>();
                //增加刷新UI的逻辑
                tPopup.bubble.ContextDispatcher(tPopup.transform, StreakBallEvent.TryRegisterChildView, tPopup);
                return tPopup;
            }

            return null;
        }

        public virtual void RefreshUI(GetDetailRes res)
        {
        }
    }
}
