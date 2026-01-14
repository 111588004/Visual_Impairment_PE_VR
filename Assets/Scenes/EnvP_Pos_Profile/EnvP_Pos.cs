using UnityEngine;
using UnityEngine.UI;

public class EvnP_Pos : MonoBehaviour
{
    [Header("控制對象 (Cube)")]
    public Transform target; // 要控制位置的物件

    [Header("Slider 控制軸向")]
    public Slider sliderX;
    public Slider sliderY;
    public Slider sliderZ;

    [Header("範圍設定")]
    public float minValue = -5f;
    public float maxValue = 5f;

    private void Start()
    {
        // 初始化 Slider 範圍與當前值
        SetupSlider(sliderX, target.position.x);
        SetupSlider(sliderY, target.position.y);
        SetupSlider(sliderZ, target.position.z);

        // 綁定事件
        sliderX.onValueChanged.AddListener(UpdatePosition);
        sliderY.onValueChanged.AddListener(UpdatePosition);
        sliderZ.onValueChanged.AddListener(UpdatePosition);
    }

    void SetupSlider(Slider slider, float initialValue)
    {
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = initialValue;
    }

    void UpdatePosition(float _)
    {
        if (target == null) return;

        Vector3 newPos = new Vector3(
            sliderX.value,
            sliderY.value,
            sliderZ.value
        );
        target.position = newPos;
    }
}
