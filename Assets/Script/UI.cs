using UnityEngine;
using UnityEngine.UI;
 
public class AspectRatioUI : MonoBehaviour
{
    private void Start()
    {
        // 为Canvas添加Aspect Ratio Fitter组件
        AspectRatioFitter aspectFitter = gameObject.AddComponent<AspectRatioFitter>();
        
        // 设置根据屏幕比例自动调整
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        
        // 或者固定特定比例
        // aspectFitter.aspectRatio = 16f / 9f; // 固定16:9
        // aspectFitter.aspectRatio = 4f / 3f;  // 固定4:3
    }
    
    private void Update()
    {
        // 动态根据当前屏幕比例调整
        float currentAspect = (float)Screen.width / Screen.height;
        GetComponent<AspectRatioFitter>().aspectRatio = currentAspect;
    }
}
