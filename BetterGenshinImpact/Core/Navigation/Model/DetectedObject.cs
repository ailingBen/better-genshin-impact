using OpenCvSharp;

namespace BetterGenshinImpact.Core.Navigation.Model;

public class DetectedObject
{
    public Rect BBox { get; set; }
    public string Label { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public float Depth { get; set; }
}
