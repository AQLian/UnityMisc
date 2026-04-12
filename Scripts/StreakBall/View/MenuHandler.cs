using System;
using System.Collections;
using System.Collections.Generic;
using HappyMahjong.Common;

using TMPro;

using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public interface IMenuHandler
    {
        public void SetSelect(bool select);
    }
    
    public class LevelMenuHandler : MonoBehaviour, IMenuHandler
    {
        private Transform m_level;
        private Transform m_name;
        private Transform m_lock;
        private Transform m_normal;
        private GameObject m_selected;
        private bool m_isLocked;
        
        private void Awake()
        {
            m_selected = transform.Find("Selected").gameObject;
            
            m_level = transform.Find("Offset/Labels/Level");
            m_name = transform.Find("Offset/Labels/Name");
            
            m_lock = transform.Find("Offset/OffsetLock");
            m_normal = transform.Find("Offset/Normal");
            
            UnSelect();
        }

        public void SetLevelText(string text)
        {
            Util.SetUIText(m_level, text);
        }

        public void SetNameText(string text)
        {
            Util.SetUIText(m_name, text);
        }

        public void SetLock(bool state)
        {
            m_isLocked = state;
            m_lock.gameObject.SetActive(state);
            m_normal.gameObject.SetActive(!state);
            
            UpdateTextColor();
        }
        
        public void Select()
        {
            SetSelect(true);
        }

        public void UnSelect()
        {
            SetSelect(false);
        }

        public void SetSelect(bool select)
        {
            m_selected.SetActive(select);
            
            if (m_lock.gameObject.activeSelf)
            {
                m_lock.Find("Lock/Selected").gameObject.SetActive(select);
            }
            
            if (m_normal.gameObject.activeSelf)
            {
                m_normal.Find("Selected").gameObject.SetActive(select);
            }

            UpdateTextColor();
        }

        private void UpdateTextColor()
        {
            Color textColor;
            if (m_selected.activeSelf)
            {
                textColor = new Color(0.54117647F, 0.26666666666666666F, 0.17254901960784313F);
            }
            else
            {
                if (m_isLocked)
                {
                    textColor = Color.white;
                }
                else
                {
                    textColor = new Color(1F, 0.7529411764705882F, 0.2235294117647059F);
                }
            }

            m_level.GetComponent<TMP_Text>().color = textColor;
            m_name.GetComponent<TMP_Text>().color = textColor;
        }
    }
    
    
    public class MenuHandler : MonoBehaviour, IMenuHandler
    {
        public Transform textTrans;
        private GameObject m_selected;
        private GameObject m_dotSelected;

        private void Awake()
        {
            m_selected = transform.Find("Selected").gameObject;
            m_dotSelected = transform.Find("Dot/Selected").gameObject;
        }

        public void SetDisplayName(string name)
        {
            Util.SetUIText(textTrans, name);
        }

        public void SetSelect(bool select)
        {
            m_selected.SetActive(select);
            m_dotSelected.SetActive(select);
            
            
            Color textColor;
            if (select)
            {
                textColor = new Color(0.5411764705882353F, 0.26666666666666666F, 0.17254901960784313F);
            }
            else
            {
                textColor = Color.white;
            }

            textTrans.GetComponent<TMP_Text>().color = textColor;
        }
    }
    
    public class LevelAbilityMenuHandler : MonoBehaviour, IMenuHandler
    {
        private GameObject m_selected;
        private Transform m_textTrans;
        
        private void Awake()
        {
            m_selected = transform.Find("Selected").gameObject;
            m_textTrans = transform.Find("Name");
        }
        
        public void SetSelect(bool select)
        {
            m_selected.SetActive(select);
            
            Color textColor;
            if (select)
            {
                textColor = new Color(0.5411764705882353F, 0.26666666666666666F, 0.17254901960784313F);
            }
            else
            {
                textColor = Color.white;
            }

            m_textTrans.GetComponent<TMP_Text>().color = textColor;
            
            StreakBallUtil.ClearTips();
        }
    }

    public class MenuGroup : MonoBehaviour
    {
        private IMenuHandler m_curMenuHandler;

        private List<IMenuHandler> m_menuHandlers = new List<IMenuHandler>();

        // 这个是可选，支持 Click First
        public void AddMenuHandler(IMenuHandler menuHandler)
        {
            m_menuHandlers.Add(menuHandler);
        }

        public void SelectFirst()
        {
            if (m_menuHandlers.Count > 0)
            {
                Select(m_menuHandlers[0]);
            }
        }
        
        public bool Select(IMenuHandler menu)
        {
            if ((UnityEngine.Object) m_curMenuHandler == (UnityEngine.Object) menu)
            {
                return false;
            }

            if ((UnityEngine.Object)m_curMenuHandler != null)
            {
                m_curMenuHandler.SetSelect(false);
            }
            
            m_curMenuHandler = menu;
            m_curMenuHandler.SetSelect(true);

            return true;
        }

        public void UnSelect()
        {
            if ((UnityEngine.Object) m_curMenuHandler != null)
            {
                m_curMenuHandler.SetSelect(false);
                m_curMenuHandler = null;
            }
        }
    }
}

