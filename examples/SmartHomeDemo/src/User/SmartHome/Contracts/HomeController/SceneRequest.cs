namespace SmartHome.Contracts;

public partial class SceneRequest
{
    partial void OnSceneNameSetting(ref string value)
    {
        value = value.Trim();
    }

    partial void OnBrightnessPercentSetting(ref int value)
    {
        value = Math.Clamp(value, 0, 100);
    }
}
