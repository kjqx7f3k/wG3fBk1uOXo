using UnityEngine;

[RequireComponent(typeof(Light))]
public class SunLightController : MonoBehaviour
{
    [Header("太陽運動設定")]
    [SerializeField] private float dayDuration = 86400f; // 一天的長度（秒）
    [SerializeField] private float sunTilt = 23.5f; // 太陽傾斜角（度）
    [SerializeField] private float startAngle = 0f; // 起始角度（度，0=日出）
    [SerializeField] private float timeMultiplier = 1f; // 時間倍率
    [SerializeField] private bool autoStart = true; // 自動開始
    
    [Header("光照設定")]
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 強度曲線
    [SerializeField] private Gradient sunColor = new Gradient(); // 太陽顏色漸變
    [SerializeField] private float maxIntensity = 2f; // 最大光照強度
    
    [Header("調試")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool isPaused = false;
    
    private Light sunLight;
    private float currentTime = 0f; // 當前時間（0-1）
    private Vector3 originalRotation;
    
    void Start()
    {
        sunLight = GetComponent<Light>();
        originalRotation = transform.eulerAngles;
        
        if (sunColor.colorKeys.Length == 0)
        {
            SetupDefaultGradient();
        }
        
        currentTime = startAngle / 360f;
        
        if (autoStart)
        {
            UpdateSunPosition();
        }
    }
    
    void Update()
    {
        if (!isPaused)
        {
            currentTime += (Time.deltaTime * timeMultiplier) / dayDuration;
            currentTime %= 1f; // 保持在0-1範圍內
            
            UpdateSunPosition();
        }
        
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }
    
    void UpdateSunPosition()
    {
        // 計算太陽的水平角度（0-360度）
        float horizontalAngle = currentTime * 360f;
        
        // 計算太陽的垂直角度（考慮傾斜）
        float verticalAngle = Mathf.Sin(currentTime * 2f * Mathf.PI) * sunTilt;
        
        // 應用旋轉
        transform.rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        
        // 更新光照強度和顏色
        UpdateLightProperties();
    }
    
    void UpdateLightProperties()
    {
        // 計算太陽高度（-1到1，負值表示地平線下）
        float sunHeight = Mathf.Sin((currentTime - 0.5f) * 2f * Mathf.PI);
        
        // 計算強度（只在太陽在地平線上時有光）
        float intensity = 0f;
        if (sunHeight > 0f)
        {
            float normalizedHeight = Mathf.Clamp01(sunHeight);
            intensity = intensityCurve.Evaluate(normalizedHeight) * maxIntensity;
        }
        
        sunLight.intensity = intensity;
        
        // 設定顏色
        sunLight.color = sunColor.Evaluate(currentTime);
    }
    
    void SetupDefaultGradient()
    {
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        
        // 夜晚 -> 日出 -> 中午 -> 日落 -> 夜晚
        colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 0f);    // 夜晚藍
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f);   // 日出橙
        colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f);      // 中午白
        colorKeys[3] = new GradientColorKey(new Color(1f, 0.4f, 0.2f), 0.75f);   // 日落紅
        colorKeys[4] = new GradientColorKey(new Color(0.1f, 0.1f, 0.3f), 1f);    // 夜晚藍
        
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);
        
        sunColor.SetKeys(colorKeys, alphaKeys);
    }
    
    void ShowDebugInfo()
    {
        float hours = currentTime * 24f;
        int hour = Mathf.FloorToInt(hours);
        int minute = Mathf.FloorToInt((hours - hour) * 60f);
        
        Debug.Log($"太陽時間: {hour:00}:{minute:00} | 強度: {sunLight.intensity:F2} | 高度角: {transform.eulerAngles.x:F1}°");
    }
    
    // 公開方法
    public void PauseSun()
    {
        isPaused = true;
    }
    
    public void ResumeSun()
    {
        isPaused = false;
    }
    
    public void SetTime(float normalizedTime)
    {
        currentTime = Mathf.Clamp01(normalizedTime);
        UpdateSunPosition();
    }
    
    public void SetTimeOfDay(int hour, int minute)
    {
        float timeOfDay = (hour + minute / 60f) / 24f;
        SetTime(timeOfDay);
    }
    
    public float GetCurrentTime()
    {
        return currentTime;
    }
    
    public string GetTimeString()
    {
        float hours = currentTime * 24f;
        int hour = Mathf.FloorToInt(hours);
        int minute = Mathf.FloorToInt((hours - hour) * 60f);
        return $"{hour:00}:{minute:00}";
    }
}