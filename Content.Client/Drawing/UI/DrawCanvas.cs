// BF14

using System.Numerics;
using Content.Shared.Input;
using Content.Shared.Paper;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.Drawing.UI;

public sealed partial class DrawCanvas : Control
{
    private readonly List<Vector2> _currentPoints = new();
    private bool _drawing;
    private bool _panning;
    private Vector2 _lastPanPosition;

    // BF14: For map surfaces the strokes are stored in the map's natural pixel space and scaled
    // to the current display size, so they stay anchored to the map as the window resizes.
    private bool _scalesWithContent;
    private Vector2 _contentSize = Vector2.One;

    public Color DrawColor { get; set; } = Color.Black;

    public List<DrawStroke> Strokes { get; set; } = new();

    public bool DrawingEnabled
    {
        get => MouseFilter != MouseFilterMode.Ignore && _inputEnabled;
        set
        {
            _inputEnabled = value;
            MouseFilter = _inputEnabled ? MouseFilterMode.Stop : MouseFilterMode.Ignore;
        }
    }

    private bool _inputEnabled;

    public event Action<DrawStroke>? StrokeCompleted;

    public DrawCanvas()
    {
        RectClipContent = true;
        MinSize = new Vector2(64, 64);
        HorizontalExpand = true;
        VerticalExpand = true;
        DrawingEnabled = false;
    }

    /// <summary>
    ///     BF14: Sets the natural pixel size of the map content. When set, strokes are stored in
    ///     this canonical space and scaled to the current display size as the window resizes.
    /// </summary>
    public void SetContentSize(Vector2 size)
    {
        _contentSize = size;
        _scalesWithContent = true;
        MinSize = size;
    }

    private Vector2 ContentScale
    {
        get
        {
            if (!_scalesWithContent)
                return Vector2.One;

            var sx = Size.X > 0 ? Size.X / _contentSize.X : 1f;
            var sy = Size.Y > 0 ? Size.Y / _contentSize.Y : 1f;
            return new Vector2(sx, sy);
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function == ContentKeyFunctions.MouseMiddle)
        {
            _panning = true;
            _lastPanPosition = args.RelativePixelPosition;
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        _drawing = true;
        _currentPoints.Clear();
        AddPoint(args.RelativePixelPosition);
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (_panning)
        {
            var delta = args.RelativePixelPosition - _lastPanPosition;
            _lastPanPosition = args.RelativePixelPosition;
            PanBy(delta);
            args.Handle();
            return;
        }

        if (!_drawing)
            return;

        AddPoint(args.RelativePixelPosition);
        args.Handle();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == ContentKeyFunctions.MouseMiddle)
        {
            _panning = false;
            args.Handle();
            return;
        }

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        if (_drawing && _currentPoints.Count >= 2)
        {
            var stroke = new DrawStroke
            {
                Color = DrawColor,
                Points = new List<Vector2>(_currentPoints)
            };

            Strokes.Add(stroke);
            StrokeCompleted?.Invoke(stroke);
        }

        _drawing = false;
        _currentPoints.Clear();
        args.Handle();
    }

    private void PanBy(Vector2 delta)
    {
        for (var parent = Parent; parent != null; parent = parent.Parent)
        {
            if (parent is not ScrollContainer scroll)
                continue;

            var value = scroll.GetScrollValue();
            scroll.SetScrollValue(value - delta);
            return;
        }
    }

    private void AddPoint(Vector2 pixelPos)
    {
        var scale = ContentScale;
        var pos = new Vector2(pixelPos.X / scale.X, pixelPos.Y / scale.Y);

        if (_currentPoints.Count > 0 && Vector2.Distance(_currentPoints[^1], pos) < 0.5f)
            return;

        _currentPoints.Add(pos);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var scale = ContentScale;
        foreach (var stroke in Strokes)
        {
            DrawStrokePolyline(handle, stroke.Points, stroke.Color, scale);
        }

        if (_drawing && _currentPoints.Count >= 2)
        {
            DrawStrokePolyline(handle, _currentPoints, DrawColor, scale);
        }
    }

    private void DrawStrokePolyline(DrawingHandleScreen handle, IReadOnlyList<Vector2> points, Color color, Vector2 scale)
    {
        if (points.Count == 0)
            return;

        var baseRadius = Math.Clamp(Size.Y * 0.008f, 1.0f, 4f);

        for (var i = 0; i < points.Count - 1; i++)
        {
            var a = points[i] * scale;
            var b = points[i + 1] * scale;
            var segLen = Vector2.Distance(a, b);
            if (segLen <= 0f)
                continue;

            var step = Math.Max(baseRadius * 0.4f, 0.5f);
            var count = (int)Math.Ceiling(segLen / step);
            if (count > 512)
                count = 512;

            for (var j = 0; j <= count; j++)
            {
                var t = count == 0 ? 0f : (float)j / count;
                var p = Vector2.Lerp(a, b, t);
                handle.DrawCircle(p, baseRadius, color);
            }
        }

        handle.DrawCircle(points[^1] * scale, baseRadius, color);
    }
}
