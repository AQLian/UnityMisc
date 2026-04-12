using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HappyMahjong.StreakBallSpace
{
    public class OnDestroyHelper : MonoBehaviour
    {
        private Action m_onDestroy;
        public event Action onDestroy
        {
            add
            {
                if (m_onDestroy == null)
                {
                    m_onDestroy = delegate { };
                }
                
                m_onDestroy += value;
            }

            remove
            {
                if (m_onDestroy == null)
                    return;
                
                m_onDestroy -= value;
            }
        }
        
        private Action m_onDisable;
        public event Action onDisable
        {
            add
            {
                if (m_onDisable == null)
                {
                    m_onDisable = delegate { };
                }
                
                m_onDisable += value;
            }

            remove
            {
                if (m_onDisable == null)
                    return;
                
                m_onDisable -= value;
            }
        }

        private void OnDestroy()
        {
            m_onDestroy?.Invoke();
        }

        private void OnDisable()
        {
            m_onDisable?.Invoke();
        }
    }

}
