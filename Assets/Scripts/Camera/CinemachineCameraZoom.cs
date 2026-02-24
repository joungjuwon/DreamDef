using UnityEngine;
using Unity.Cinemachine; // Unity 6 (Cinemachine 3.x) 네임스페이스
using UnityEngine.InputSystem;

public class CinemachineCameraZoom : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private CinemachineCamera _cinemachineCamera;
    [SerializeField] private float _zoomSpeed = 0.5f;
    [SerializeField] private float _minFOV = 10f;
    [SerializeField] private float _maxFOV = 60f;
    [SerializeField] private float _smoothTime = 0.2f;

    [Header("Input Action Name")]
    [Tooltip("Input System의 Action 이름과 일치해야 합니다 (예: Zoom).")]
    [SerializeField] private string _zoomActionName = "Zoom";

    private float _currentZoomVelocity;
    private float _targetValue;

    private void Awake()
    {
        // 시네머신 카메라 참조가 없으면 찾기 (자식이나 자신의 오브젝트에서)
        if (_cinemachineCamera == null) 
            _cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
            
        if (_cinemachineCamera == null)
        {
            Debug.LogError("[CinemachineCameraZoom] CinemachineCamera 컴포넌트를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        // 초기값 설정 (Perspective는 FOV, Orthographic은 Size)
        if (_cinemachineCamera.Lens.Orthographic)
            _targetValue = _cinemachineCamera.Lens.OrthographicSize;
        else
            _targetValue = _cinemachineCamera.Lens.FieldOfView;
    }

    // PlayerInput의 SendMessages를 통해 호출됨 (Action 이름이 "Zoom"일 경우 OnZoom)
    private void OnZoom(InputValue value)
    {
        // 마우스 휠 스크롤 값 (보통 Vector2.y)
        float scrollInput = value.Get<Vector2>().y;
        
        // 줌 입력이 있을 때만 목표값 변경 (입력 값 정규화가 안되어 있을 수 있으므로 0.01f 등을 곱하거나 zoomSpeed로 조절)
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // 스크롤 방향에 따라 FOV 조절 (휠을 올리면 줌인=FOV감소)
            // Input System의 Scroll 값은 보통 120 단위일 수 있으므로 적절히 보정
            float zoomAmount = scrollInput > 0 ? -1 : 1; 
            
            _targetValue += zoomAmount * _zoomSpeed * 5f; // 감도 보정
            _targetValue = Mathf.Clamp(_targetValue, _minFOV, _maxFOV);
        }
    }

    private void Update()
    {
        if (_cinemachineCamera != null)
        {
            // 부드러운 줌 처리 (카메라 모드에 따라 분기)
            if (_cinemachineCamera.Lens.Orthographic)
            {
                float newSize = Mathf.SmoothDamp(_cinemachineCamera.Lens.OrthographicSize, _targetValue, ref _currentZoomVelocity, _smoothTime);
                _cinemachineCamera.Lens.OrthographicSize = newSize;
            }
            else
            {
                float newFOV = Mathf.SmoothDamp(_cinemachineCamera.Lens.FieldOfView, _targetValue, ref _currentZoomVelocity, _smoothTime);
                _cinemachineCamera.Lens.FieldOfView = newFOV;
            }
        }
    }
}
