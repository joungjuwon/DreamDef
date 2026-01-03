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

    private void UpdateResourceUI()
    {
        if (resourceText != null)
        {
            resourceText.text = "자원: " + _currentResources;
        }
    }
}