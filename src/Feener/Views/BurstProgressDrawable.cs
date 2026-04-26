using Microsoft.Maui.Graphics;

namespace Feener.Views;

/// <summary>
/// Custom drawable for the Burst Mode progress bar.
/// Renders a segmented linear progress bar showing sent messages vs daily limit,
/// with markers for individual sessions.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class BurstProgressDrawable : IDrawable
{
    public float Progress { get; set; }
    public int TotalSessions { get; set; } = 1;

    public bool IsDarkTheme { get; set; }

    private static readonly Color TrackLight = Color.FromArgb("#E5E7EB");
    private static readonly Color TrackDark = Color.FromArgb("#2E3036");
    private static readonly Color FillColor = Color.FromArgb("#8B5CF6"); // BurstAccent
    private static readonly Color MarkerColorLight = Color.FromArgb("#FFFFFF");
    private static readonly Color MarkerColorDark = Color.FromArgb("#1E1F23"); // App background

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cornerRadius = dirtyRect.Height / 2f;

        // Draw track
        canvas.FillColor = IsDarkTheme ? TrackDark : TrackLight;
        canvas.FillRoundedRectangle(dirtyRect, cornerRadius);

        // Draw fill
        if (Progress > 0)
        {
            var fillWidth = Math.Max(cornerRadius * 2, dirtyRect.Width * Progress);
            var fillRect = new RectF(dirtyRect.X, dirtyRect.Y, fillWidth, dirtyRect.Height);
            
            canvas.SetShadow(new SizeF(0, 2), 6, FillColor.WithAlpha(0.5f));
            
            var gradientPaint = new LinearGradientPaint
            {
                StartColor = Color.FromArgb("#A78BFA"),
                EndColor = FillColor,
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5)
            };
            canvas.SetFillPaint(gradientPaint, fillRect);
            
            canvas.FillRoundedRectangle(fillRect, cornerRadius);
            
            canvas.SetShadow(SizeF.Zero, 0, Colors.Transparent);
        }

        // Draw session markers
        if (TotalSessions > 1)
        {
            canvas.StrokeColor = IsDarkTheme ? MarkerColorDark : MarkerColorLight;
            canvas.StrokeSize = 2;

            for (int i = 1; i < TotalSessions; i++)
            {
                float x = dirtyRect.Width * ((float)i / TotalSessions);
                // Don't draw markers too close to the edges
                if (x > cornerRadius && x < dirtyRect.Width - cornerRadius)
                {
                    canvas.DrawLine(x, dirtyRect.Y, x, dirtyRect.Bottom);
                }
            }
        }
    }
}
