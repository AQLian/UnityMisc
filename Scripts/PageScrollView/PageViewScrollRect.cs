using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class PageViewScrollRect : ScrollRect
{
    [Header("Page Settings")]
    [SerializeField] private int _pageCount = 3;
    [SerializeField] private bool _loopPages = true;

    [Header("Auto Scroll Settings")]
    [SerializeField] private bool _enableAutoScroll = true;
    [SerializeField] private float _firstWaitTime = 3f;         // wait how long to start first time auto paging 
    [SerializeField] private float _pageInterval = 3f;          // wait how long to each paging wait to next 

    [Header("Simple Drag Settings")]
    [SerializeField] private float _dragThreshold = 30f;        // Min drag distance to change page
    [SerializeField] private float _minScrollDuration = 0.15f;  // For tiny adjustments
    [SerializeField] private float _maxScrollDuration = 0.4f;   // For full page transitions

    [Header("Page Indicator")]
    [SerializeField] private GameObject _dotPrefab;
    [SerializeField] private Transform _dotsContainer;
    [SerializeField] private Sprite _activeDotSprite;
    [SerializeField] private Sprite _inactiveDotSprite;

    private HorizontalLayoutGroup _layoutGroup;
    private List<Image> _pageDots = new List<Image>();
    private float _pageWidth;
    private int _currentPage = 0;
    private bool _isDragging = false;
    private bool _isScrolling = false;
    private Coroutine _autoScrollCoroutine;

    // Simple drag tracking
    private Vector2 _dragStartPosition;

    public int CurrentPage => _currentPage;
    public System.Action<int> OnPageChanged;

    private Coroutine m_ScrollToPageCoroutine;

    protected override void Start()
    {
        base.Start();
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
        {
            return;
        }
#endif

        InitializePageView();
    }

    private void InitializePageView()
    {
        if(content == null) { return; }

        _layoutGroup = content.GetComponent<HorizontalLayoutGroup>();
        if (_layoutGroup == null)
            _layoutGroup = content.gameObject.AddComponent<HorizontalLayoutGroup>();

        _layoutGroup.childControlWidth = true;
        _layoutGroup.childControlHeight = true;
        _layoutGroup.childForceExpandWidth = false;
        _layoutGroup.childForceExpandHeight = false;
        _layoutGroup.spacing = 0;

        _pageWidth = GetComponent<RectTransform>().rect.width;
        SetupContentSize();
        CreatePageIndicators();

        if (_enableAutoScroll)
            StartAutoScroll();
    }

    private void SetupContentSize()
    {
        float contentWidth = _pageWidth * _pageCount;
        content.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentWidth);

        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i).GetComponent<RectTransform>();
            if (child != null)
                child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _pageWidth);
        }
    }

    private void CreatePageIndicators()
    {
        if (_dotsContainer == null) return;

        foreach (var dot in _pageDots)
            if (dot != null) Destroy(dot.gameObject);
        _pageDots.Clear();

        for (int i = 0; i < _pageCount; i++)
        {
            GameObject dotObj = Instantiate(_dotPrefab, _dotsContainer);
            _pageDots.Add(dotObj.GetComponent<Image>());
        }

        UpdatePageIndicator();
    }

    private void UpdatePageIndicator()
    {
        for (int i = 0; i < _pageDots.Count; i++)
        {
            if (_pageDots[i] != null)
                _pageDots[i].sprite = i == _currentPage ? _activeDotSprite : _inactiveDotSprite;
        }
    }

    public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        _isDragging = true;
        _dragStartPosition = eventData.position;
        StopScrollToPageCoroutine();
    }

    public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        _isDragging = false;

        Vector2 dragDistance = eventData.position - _dragStartPosition;
        int targetPage = CalculateTargetPageFromDistance(dragDistance.x);

        float scrollDistance = CalculateScrollDistance(targetPage);
        float duration = CalculateDynamicDuration(scrollDistance);

        ScrollToPage(targetPage, duration);
    }

    private int CalculateTargetPageFromDistance(float dragDistanceX)
    {
        int currentPage = _currentPage;

        if (Mathf.Abs(dragDistanceX) < _dragThreshold )
            return currentPage;

        bool dragRight = dragDistanceX < 0;

        if (dragRight)
        {
            if (currentPage < _pageCount - 1)
                return currentPage + 1;
        }
        else
        {
            if (currentPage > 0)
                return currentPage - 1;
        }
        return currentPage;
    }

    private float CalculateScrollDistance(int targetPage)
    {
        float currentPos = Mathf.Abs(content.anchoredPosition.x);
        float targetPos = targetPage * _pageWidth;
        return Mathf.Abs(targetPos - currentPos);
    }

    private float CalculateDynamicDuration(float distance)
    {
        float normalizedDistance = distance / _pageWidth;
        return Mathf.Lerp(_minScrollDuration, _maxScrollDuration, normalizedDistance);
    }


    private void ScrollToPage(int pageIndex, float duration)
    {
        pageIndex = Mathf.Clamp(pageIndex, 0, _pageCount - 1);

        StopScrollToPageCoroutine();
        m_ScrollToPageCoroutine = StartCoroutine(ScrollToPageCoroutine(pageIndex, duration));
    }

    private IEnumerator ScrollToPageCoroutine(int targetPage, float duration)
    {
        StopAutoScroll();

        _isScrolling = true;
        _currentPage = targetPage;

        Vector2 startPos = content.anchoredPosition;
        Vector2 targetPos = new Vector2(-targetPage * _pageWidth, startPos.y);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = 1f - Mathf.Pow(1f - t, 3);
            content.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        content.anchoredPosition = targetPos;
        _isScrolling = false;

        UpdatePageIndicator();
        OnPageChanged?.Invoke(_currentPage);

        if (_enableAutoScroll)
        {
            StartAutoScroll();
        }
    }

    private IEnumerator AutoScrollRoutine()
    {
        yield return new WaitForSeconds(_firstWaitTime);

        while (true)
        {
            yield return new WaitForSeconds(_pageInterval);

            if (!_isDragging && !_isScrolling)
            {
                int nextPage = _currentPage + 1;

                if (nextPage >= _pageCount)
                {
                    if (_loopPages)
                    {
                        nextPage = 0;
                    }
                    else
                    {
                        nextPage = _pageCount - 1;
                    }
                }

                float distance = CalculateScrollDistance(nextPage);
                float duration = CalculateDynamicDuration(distance);
                ScrollToPage(nextPage, duration);
            }
        }
    }

    private void StartAutoScroll()
    {
        StopAutoScroll();
        _autoScrollCoroutine = StartCoroutine(AutoScrollRoutine());
    }

    private void StopAutoScroll()
    {
        if (_autoScrollCoroutine != null)
        {
            StopCoroutine(_autoScrollCoroutine);
            _autoScrollCoroutine = null;
        }
    }

    private void StopScrollToPageCoroutine()
    {
        if (m_ScrollToPageCoroutine!=null)
        {
            StopCoroutine(m_ScrollToPageCoroutine);
            m_ScrollToPageCoroutine = null;
        }
    }

    public void SetPageCount(int count)
    {
        _pageCount = Mathf.Max(1, count);
        SetupContentSize();
        CreatePageIndicators();
        ScrollToPage(0, _minScrollDuration);
    }

    public void SetAutoScroll(bool enable)
    {
        _enableAutoScroll = enable;
        if (enable)
            StartAutoScroll();
        else
            StopAutoScroll();
    }

    public void SetDragThreshold(float threshold)
    {
        _dragThreshold = Mathf.Max(10f, threshold);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopScrollToPageCoroutine();
        StopAutoScroll();
    }
}
