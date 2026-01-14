// EnvP_CS.cs — Unity 6 / URP：用 Slider(0~255) 調整 BaseMap 顏色深淺（左亮→右暗），不改透明度
using UnityEngine;
using UnityEngine.UI;

public class EnvP_CS : MonoBehaviour
{
    public MeshRenderer cube;
    public Slider alphaSlider; // 仍沿用此名稱，但作為亮度/深淺(0~255)

    private Material _mat;
    private Color _baseColor0; // 記住初始 BaseColor，作為壓暗基準

    void Start()
    {
        _mat = cube.material;

        // 讀取 URP/Lit 的 BaseColor（含 alpha）
        _baseColor0 = _mat.HasProperty("_BaseColor") ? _mat.GetColor("_BaseColor") : _mat.color;

        // 確保為 Opaque（不透明），避免之前被設成 Transparent 造成誤判
        if (_mat.HasProperty("_Surface")) _mat.SetFloat("_Surface", 0f); // 0: Opaque
        if (_mat.HasProperty("_AlphaClip")) _mat.SetFloat("_AlphaClip", 0f);
        _mat.renderQueue = -1; // 使用 shader 預設佇列

        if (alphaSlider != null)
        {
            alphaSlider.wholeNumbers = true; // 0~255 的整數段
            alphaSlider.minValue = 0f;
            alphaSlider.maxValue = 255f;
            alphaSlider.onValueChanged.AddListener(OnEdit);
            OnEdit(alphaSlider.value); // 進場同步一次
        }
    }

    // 用 0~255 的值把顏色壓暗；alpha 固定 1（不透明）
    void OnEdit(float value)
    {
        // 0~255 → 0~1；並「反轉」：左(0)=最亮、右(255)=最暗
        float t = 1f - Mathf.Clamp01(Mathf.Round(value) / 255f);

        // 以初始 BaseColor 為基準壓暗，alpha 固定 1
        Color c = _baseColor0;
        c = new Color(c.r * t, c.g * t, c.b * t, 1f);

        if (_mat.HasProperty("_BaseColor"))
            _mat.SetColor("_BaseColor", c);
        else
            _mat.color = c;
    }

    void OnDestroy()
    {
        if (alphaSlider != null)
            alphaSlider.onValueChanged.RemoveListener(OnEdit);
    }
}
