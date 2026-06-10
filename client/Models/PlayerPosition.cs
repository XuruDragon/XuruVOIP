namespace XuruVoipClient.Models;

/// <summary>
/// Parsed position from Star Citizen OCR output.
/// Format: "Zone: Hangar XLTop Area18 854875740883 Pos: -4.98m -10.00m -114.03m"
/// </summary>
public class PlayerPosition
{
    public string Zone { get; set; } = "";
    public string ContainerID { get; set; } = "";
    public string ContainerName { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double TsCapture { get; set; }

    public bool IsEmpty => string.IsNullOrEmpty(Zone);

    public override string ToString() =>
        IsEmpty ? "No position" : $"{Zone} | {X:F2}m {Y:F2}m {Z:F2}m";
}
