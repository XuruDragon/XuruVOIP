using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Controls;

namespace XuruVoipClient.Views;

/// <summary>
/// Fullscreen transparent overlay for selecting the OCR scan region.
/// Opens on the selected monitor and lets the user draw a green rectangle.
/// </summary>
public partial class OcrRegionPicker : Window
{
    public System.Windows.Rect SelectedRegion { get; private set; }

    private System.Windows.Point _startPoint;
    private bool _isDragging;

    public OcrRegionPicker(int monitorIndex, System.Windows.Rect initialRegion)
    {
        InitializeComponent();

        // Position window to cover the target monitor exactly
        var screens = Screen.AllScreens;
        var screen = screens[Math.Min(monitorIndex, screens.Length - 1)];
        Left   = screen.Bounds.X;
        Top    = screen.Bounds.Y;
        Width  = screen.Bounds.Width;
        Height = screen.Bounds.Height;

        // Set banner width
        InstructionBanner.Width = screen.Bounds.Width;

        // Show initial region if any
        if (initialRegion.Width > 0 && initialRegion.Height > 0)
        {
            ShowSelection(initialRegion.X, initialRegion.Y,
                          initialRegion.Width, initialRegion.Height);
        }

        MainCanvas.MouseLeftButtonDown += Canvas_MouseDown;
        MainCanvas.MouseMove           += Canvas_MouseMove;
        MainCanvas.MouseLeftButtonUp   += Canvas_MouseUp;
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { DialogResult = false; Close(); } };
    }

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(MainCanvas);
        _isDragging = true;
        MainCanvas.CaptureMouse();

        SelectionRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;

        HideHandles();
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(MainCanvas);
        double x = Math.Min(_startPoint.X, current.X);
        double y = Math.Min(_startPoint.Y, current.Y);
        double w = Math.Abs(current.X - _startPoint.X);
        double h = Math.Abs(current.Y - _startPoint.Y);

        ShowSelection(x, y, w, h);
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        MainCanvas.ReleaseMouseCapture();

        double x = Canvas.GetLeft(SelectionRect);
        double y = Canvas.GetTop(SelectionRect);
        double w = SelectionRect.Width;
        double h = SelectionRect.Height;

        if (w < 10 || h < 4) return; // Too small — ignore

        SelectedRegion = new System.Windows.Rect(x, y, w, h);
        DialogResult = true;
        Close();
    }

    private void ShowSelection(double x, double y, double w, double h)
    {
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width  = Math.Max(0, w);
        SelectionRect.Height = Math.Max(0, h);
        SelectionRect.Visibility = Visibility.Visible;

        // Update coordinate label
        CoordText.Text = $"X={x:F0}  Y={y:F0}  {w:F0}×{h:F0}";
        Canvas.SetLeft(CoordLabel, x);
        Canvas.SetTop(CoordLabel, Math.Max(0, y - 24));
        CoordLabel.Visibility = Visibility.Visible;

        // Corner handles
        PositionHandle(HandleTL, x - 5,     y - 5);
        PositionHandle(HandleTR, x + w - 5, y - 5);
        PositionHandle(HandleBL, x - 5,     y + h - 5);
        PositionHandle(HandleBR, x + w - 5, y + h - 5);

        HandleTL.Visibility = HandleTR.Visibility =
        HandleBL.Visibility = HandleBR.Visibility = Visibility.Visible;
    }

    private static void PositionHandle(System.Windows.Shapes.Rectangle handle, double x, double y)
    {
        Canvas.SetLeft(handle, x);
        Canvas.SetTop(handle, y);
    }

    private void HideHandles()
    {
        HandleTL.Visibility = HandleTR.Visibility =
        HandleBL.Visibility = HandleBR.Visibility = Visibility.Collapsed;
        CoordLabel.Visibility = Visibility.Collapsed;
    }
}
