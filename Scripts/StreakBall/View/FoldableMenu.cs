using System;
using System.Collections;
using System.Collections.Generic;

using HappyMahjong.Common;

using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public class FoldableMenu : MonoBehaviour
    {
        public bool expanded;
        public RectTransform content;

        public event Action onClick = delegate { };
        
        public bool isUserClick { get; private set; }

        private void Start()
        {
            UIEventListener.Get(gameObject, ClickableTypeDef.ClickSoundType).onClick = (go) =>
            {
                isUserClick = true;
                OnClickSelf();
                isUserClick = false;
            };
            
            UpdateState();
        }

        public void OnClickSelf()
        {
            Toggle();
            onClick?.Invoke();
        }

        public void Expand()
        {
            expanded = true;

            if (content.childCount > 0)
            {
                content.gameObject.SetActive(true);
            }
        }

        public void Collapse()
        {
            expanded = false;
            content.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            expanded = !expanded;
            UpdateState();
        }

        private void UpdateState()
        {
            if (expanded)
            {
                Expand();
            }
            else
            {
                Collapse();
            }
        }
    }
}

