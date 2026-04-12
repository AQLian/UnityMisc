using System.Collections;
using System.Collections.Generic;

using HappyMahjong.Common;

using UnityEngine;
using UnityEngine.UI;

namespace HappyMahjong.StreakBallSpace
{
    public class TalentInGameBG : MonoBehaviour, IScreenChanged
    {
        private void Awake()
        {
            HappyMahjong.ScreenChangeHandler.GetInstance().Register(transform, this);
        }

        private void OnDestroy()
        {
            HappyMahjong.ScreenChangeHandler.GetInstance().UnRegister(transform);
        }

        private void Start()
        {
            OnScreenChanged();
        }

        public void OnScreenChanged()
        {
            RawImage rawImage = GetComponent<RawImage>();
            if (rawImage)
            {
                Texture texture = rawImage.texture;
                if (texture)
                {
                    RectTransform trans = (RectTransform) transform;
                    trans.sizeDelta = new Vector2(texture.width, texture.height);
                    AdapterBackGround(transform, texture.width, texture.height);
                }
            }
            
            Image image = GetComponent<Image>();
            if (image)
            {
                Sprite sprite = image.sprite;
                if (sprite)
                {
                    Texture texture = sprite.texture;
                    if (texture)
                    {
                        RectTransform trans = (RectTransform) transform;
                        trans.sizeDelta = new Vector2(texture.width, texture.height);
                        AdapterBackGround(transform, texture.width, texture.height);
                    }
                }
            }
        }
        
        static void AdapterBackGround(Transform background, float width, float height, bool bIgnoreUIRoot = false)
        {
            RectTransform rootRectTransform = null;
            Canvas canvas = background.GetComponentInParent<Canvas>();
            if (canvas)
            {
                rootRectTransform = canvas.rootCanvas.transform as RectTransform;
            }

            if (rootRectTransform == null)
            {
                return;
            }
            
            Vector2 screenSize = new Vector2(rootRectTransform.rect.width, rootRectTransform.rect.height);

            var scale = Mathf.Max(screenSize.x / width, screenSize.y / height);
            background.transform.localScale = Vector3.one * (scale + 0.02f); // 这里的+0.02是因为和相机的fov不匹配，稍微放大些防止边缘有黑边
        }
    }
}

