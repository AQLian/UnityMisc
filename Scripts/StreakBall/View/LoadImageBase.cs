using System;

using HappyMahjong.Common;
using HappyMahjong.Puffer;

using UGUIExtend;

using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

using DeviceType = HappyMahjong.Common.DeviceType;

namespace HappyMahjong.StreakBallSpace
{
    public class LoadImageBase : MonoBehaviour
    {
        public int loadKey;
        public GameObject loadingIndicator;
        protected UIClippable m_clip;
        private BubbleEventHelper m_bubble;
        public BubbleEventHelper bubble
        {
            get
            {
                if (m_bubble == null)
                {
                    m_bubble = new BubbleEventHelper();
                }

                return m_bubble;
            }
        }

        protected string TimerKey => $"{GetInstanceID()}_Timer";

        protected void CancelTimer()
        {
            HappyMahjong.Common.VPTimer.CancelAll(TimerKey);
        }

        protected void StartTimer(float delay, float interval, VPTimer.Callback callback)
        {
            CancelTimer();
            HappyMahjong.Common.VPTimer.In(delay, callback, -1, interval, methodName: TimerKey);
        }

        protected virtual void Awake()
        {
            m_clip = gameObject.GetOrAddComponent<UIClippable>();
        }

        protected virtual void OnDestroy()
        {
            CancelTimer();
        }

        protected virtual void Start()
        {
            if (m_clip)
            {
                var mask = GetComponentInParent<RectMask2D>();
                if (mask)
                {
                    m_clip.mask = mask.rectTransform;
                    m_clip.IsUseClipRect(true);
                }
            }
        }

        protected void SetLoadingIndicator(bool loading)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(loading);
        }

        protected void LoadImage(Image image, string atlasName, string spritename)
        {
            if (!string.IsNullOrEmpty(atlasName) && !string.IsNullOrEmpty(spritename))
            {
                SetImgState(image, false);
                SetLoadingIndicator(true);
                HappyMahjong.ResourcesLoader.GetInstance().LoadAssetBundle(atlasName + "_UGUI", (object data, Message message, AssetBundle assetBundle) =>
                {
                    if (assetBundle == null)
                    {
                        Log.Info($"load sprite ab {atlasName} is null", ModuleType.StreakBall);
                        return;
                    }

                    var spriteAtlas = assetBundle.LoadAsset<SpriteAtlas>(atlasName + "_UGUI");
                    Sprite sprite = null;
                    if (spriteAtlas != null)
                    {
                        sprite = spriteAtlas.GetSprite(spritename);
                    }
                    else
                    {
                        sprite = assetBundle.LoadAsset<Sprite>(spritename);
                    }

                    if (this && (int)data == loadKey)
                    {
                        if (sprite == null)
                        {
                            Log.Info($"UpdateItem load sprite is null {atlasName}|{spritename}", ModuleType.StreakBall);
                            return;
                        }

                        SetLoadingIndicator(false);
                        image.sprite = sprite;
                        SetImgState(image, true);
                    }
                }, loadKey, EnmPriority.High, EnmResTag.Shop);
            }
            else
            {
                SetImgState(image, false);
                SetLoadingIndicator(true);
            }
        }

        protected void SetImgState(Image img, bool v)
        {
            var col = img.color;
            col.a = v ? 1 : 0;
            img.color = col;
        }

        protected void LoadImageFx(Image image, string effectName)
        {
            if (!string.IsNullOrEmpty(effectName) && Device.GetInstance().OrigType > DeviceType.Low && Device.GetInstance().type > DeviceType.Low)
            {
                SetImgState(image, false);
                SetLoadingIndicator(true);
                StreakBallUtil.SetAllChildActive(image.transform, false);
                HappyMahjong.ResourcesLoader.GetInstance().LoadAssetBundle(effectName + "_effect", (object data, Message message, AssetBundle assetBundle) =>
                {
                    if (this && (int) data == loadKey)
                    {
                        if (image)
                        {
                            SetIconEffect(message, assetBundle, image.transform, effectName);
                        }
                    }
                }, loadKey, EnmPriority.High, EnmResTag.Shop);
            }
            else
            {
                StreakBallUtil.SetAllChildActive(image.transform, false);
            }
        }

        protected void SetIconEffect(Message message, AssetBundle assetBundle, Transform cloth, string effectName)
        {
            if (message == Message.success && assetBundle != null && cloth != null && effectName != string.Empty)
            {
                SetLoadingIndicator(false);
                if (cloth.childCount != 0)
                {
                    Util.DestroyAllChildren(cloth);
                }

                var resources = assetBundle.LoadAsset<GameObject>(effectName);
                var effect = GameObject.Instantiate(resources);
                effect.transform.SetParent(cloth);
                effect.transform.localPosition = Vector3.zero;
                //effect.transform.localScale = new Vector3(0.426F, 0.426F, 1F);

                if (m_clip != null && m_clip.mask!=null)
                {
                    var images = effect.GetComponentsInChildren<Image>();
                    foreach (var image in images)
                    {
                        image.material = UIUtil.GetMaterial(UIUtil.CommonUIPath, "Hotload/ScrollUIClips");
                        image.material = Instantiate(image.material);
                    }
                    m_clip.ResetData();
                }
            }
        }
    }
}

