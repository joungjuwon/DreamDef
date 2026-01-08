using UnityEngine;
using UnityEngine.UI;

public class HealthBarController : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // 위치 미세 조정을 위한 오프셋
    private Transform _cameraTransform;

    private void Start()
    {
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (_cameraTransform != null)
        {
            // 1. 회전: 카메라의 회전값을 그대로 복사합니다.
            // LookAt보다 이 방식이 쿼터뷰/탑뷰에서 UI가 찌그러지지 않고 2D처럼 깔끔하게 보입니다.
            transform.rotation = _cameraTransform.rotation;

            // 2. 위치 보정 (선택 사항): 부모 오브젝트(유닛)의 피벗 위치가 바닥이라 체력바가 발에 붙는 경우 등을 위해 오프셋 적용
            if (transform.parent != null)
            {
                // 부모의 위치에 오프셋을 더한 위치로 설정 (부모가 회전해도 체력바 위치가 흔들리지 않게 하려면 이 방식이 유리할 수 있음)
                // 단, 단순히 부모를 따라다니는 것이라면 transform.localPosition을 Inspector에서 조정하는 것으로 충분할 수 있습니다.
                // 여기서는 transform.localPosition을 유지하면서 회전만 고정합니다.
            }
        }
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (slider != null)
        {
            slider.value = currentHealth / maxHealth;
        }
    }
}
