using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    // 싱글턴 인스턴스
    public static ResourceManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI resourceText; // 자원 양을 표시할 UI

    private int _currentResources = 0;

    private void Awake()
    {
        // 싱글턴 패턴 구현
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // 게임 시작 시 초기 자원 설정 (예: 100)
        _currentResources = 100;
        UpdateResourceUI();
    }

    public void AddResources(int amount)
    {
        _currentResources += amount;
        UpdateResourceUI();
    }

    // 자원 사용 시도 (성공 시 true, 실패 시 false 반환)
    public bool TrySpendResources(int amount)
    {
        if (_currentResources >= amount)
        {
            _currentResources -= amount;
            UpdateResourceUI();
            return true;
        }
        return false;
    }

    private void UpdateResourceUI()
    {
        if (resourceText != null)
        {
            resourceText.text = "" + _currentResources;
        }
    }
}
