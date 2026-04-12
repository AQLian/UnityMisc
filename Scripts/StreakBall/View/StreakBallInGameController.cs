using System;
using System.Collections;
using System.Collections.Generic;

using HappyMahjong.Common;
using HappyMahjong.Loading;

using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public abstract class StreakBallInGameController : MonoBehaviour
    {
        public static StreakBallInGameController Instance { get; private set; }
        public static bool IsInStreakBall { get; private set; }

        public static event Action onCreated = delegate { };
        
        public static void Create()
        {
            if (!HappyMahjong.Loading.NewSceneManager.GetInstance().UseSameScene() || Device.GetInstance().OrigType <= HappyMahjong.Common.DeviceType.SuperLow)
            {
                GameObject nullController = new GameObject("NullStreakBallInGameController",typeof(NullStreakBallInGameController));
            }
            else
            {
                AppRoot.GetInstance().InGameBridgeBase.GameReplayTutorialControllerFunc(5);
            }
        }

        public static void Cleanup()
        {
            if (Instance)
            {
                Instance.Destroy();
            }
        }

        public virtual void OnCreated()
        {
            onCreated?.Invoke();
        }
        
        protected virtual void Awake()
        {
            Instance = this;
            IsInStreakBall = true;
        }
        
        // Start is called before the first frame update
        protected virtual void Start()
        {
            StartCoroutine(DelayStart());
        }
        
        private IEnumerator DelayStart()
        {
            yield return new WaitForEndOfFrame();
            GameObject systemIcons = GameObject.Find("GameUIRoot/UI Root/System_icons");
            if (systemIcons)
            {
                Canvas canvas = systemIcons.GetComponent<Canvas>();
                canvas.enabled = false;
                
                GraphicRaycaster raycaster = systemIcons.GetComponent<GraphicRaycaster>();
                raycaster.enabled = false;
            }
        }

        protected virtual void OnDestroy()
        {
            SceneSwitch.AddSceneRecord(SceneSwitch.Scene.Main);
            
            if (Instance == this)
            {
                Instance = null;
            }

            IsInStreakBall = false;
        }

        public virtual void Destroy()
        {
            DestroyImmediate(gameObject);
        }

        public abstract void Show();
        public abstract void Hide();

        public abstract void ShowMagicFace(int id);

        public abstract void ClearMagicFace();

        public abstract void SetUserUIPosition(RectTransform refTrans0, RectTransform refTrans1, RectTransform refTrans2 = null, RectTransform refTrans3 = null);

        public abstract void ShowBigRSlay();

        public abstract void ClearBigRSlay();

        public abstract void ShowEntryBoardCast(int itemID);

        public abstract void ClearEntryBoardCast();

        public abstract void OnHideOrShowUI(bool isHiding);
    }

    public sealed class NullStreakBallInGameController : StreakBallInGameController
    {
        protected override void Start()
        {
            base.Start();
            OnCreated();
        }
        
        public override void Show()
        {
        }
        public override void Hide()
        {
        }
        public override void ShowMagicFace(int id)
        {
        }
        public override void ClearMagicFace()
        {
        }
        public override void SetUserUIPosition(RectTransform refTrans0, RectTransform refTrans1, RectTransform refTrans2 = null, RectTransform refTrans3 = null)
        {
        }
        public override void ShowBigRSlay()
        {
        }
        public override void ClearBigRSlay()
        {
        }
        public override void ShowEntryBoardCast(int itemID)
        {
        }
        public override void ClearEntryBoardCast()
        {
        }

        public override void OnHideOrShowUI(bool isHiding)
        {
        }
    }
}

