using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace InteractiveApp.Controls;

/// <summary>
/// Drawing Control for Avalonia.
/// </summary>
public class DrawingCanvas : ContentControl
{
    private readonly Vector DPI = new Vector(96,96);
    private WriteableBitmap _wrBitmap;
    private SKBitmap _skBitmap;
    private SKCanvas _skCanvas;
    private SKPaint _skPaint;
    private bool _isDrawing;
    private int _penId;
    private Point? _previousPoint;
    private List<Point> _points = new List<Point>();
    private Stroke _strokes = new Stroke();

    public class Stroke :  List<List<Point>>
    {
        public new void Add(List<Point> points)
        {
            if (base.Count == 100) base.RemoveAt(0);
            base.Add(points);
        }
    }

    public DrawingCanvas()
    {
        ClipToBounds = false;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;

        // Initialize the WritableBitmap
        _wrBitmap = new WriteableBitmap(new PixelSize(1200, 1800), DPI, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        _skBitmap = new SKBitmap(1200, 1800);

        _skCanvas = new SKCanvas(_skBitmap);
        _skCanvas.Clear(SKColors.White);

        _skPaint = new SKPaint();
        _skPaint.Color = SKColors.Black; // Set the drawing color
        _skPaint.StrokeWidth = 2; // Set the width of the line
        _skPaint.Style = SKPaintStyle.Stroke; // Stroke for lines
        _skPaint.IsAntialias = true; // Enable anti-aliasing

        // Handle pointer events
        PointerPressed += HandlePointerPressed;
        PointerMoved += HandlePointerMoved;
        PointerReleased += HandlePointerReleased;

        Width = _skBitmap.Width;
        Height = _skBitmap.Height;
    }

    public DrawingCanvas(Bitmap map)
    {
        ClipToBounds = false;
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        ZIndex = 1;

        Content = new Image() { Source = map };

        // Initialize the WritableBitmap
        // Check if the source is a Bitmap

        _wrBitmap = new WriteableBitmap(new PixelSize(map.PixelSize.Width, map.PixelSize.Height),
            DPI, PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        _skBitmap = new SKBitmap(map.PixelSize.Width, map.PixelSize.Height);

        _skCanvas = new SKCanvas(_skBitmap);
        _skCanvas.Clear(SKColors.Transparent);

        _skPaint = new SKPaint();
        _skPaint.Color = SKColors.Black; // Set the drawing color
        _skPaint.StrokeWidth = 2; // Set the width of the line
        _skPaint.Style = SKPaintStyle.Stroke; // Stroke for lines
        _skPaint.IsAntialias = true; // Enable anti-aliasing

        // Handle pointer events
        PointerPressed += HandlePointerPressed;
        PointerMoved += HandlePointerMoved;
        PointerReleased += HandlePointerReleased;

        Width = _skBitmap.Width;
        Height = _skBitmap.Height;
    }

    private SKPoint ConvertToSKPoint(Avalonia.Point point)
    {
        return new SKPoint((float)point.X, (float)point.Y);
    }

    protected void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var type = e.GetCurrentPoint(this).Pointer.Type;
        if (type == PointerType.Pen)
        {
            _isDrawing = true;
            _penId = e.GetCurrentPoint(this).Pointer.Id;
            _points.Add(e.GetPosition(this));
            _previousPoint = e.GetPosition(this);
        }
    }

    protected void HandlePointerMoved(object? sender, PointerEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this).Pointer;
        if (pointer.Type == PointerType.Pen && pointer.Id == _penId
            && _isDrawing && _previousPoint != null)
        {
            Point currentPoint = e.GetPosition(this);

            SKPoint skPointPrev = ConvertToSKPoint(_previousPoint.Value);
            SKPoint skPointCurr = ConvertToSKPoint(currentPoint);
            _skCanvas.DrawLine(skPointPrev, skPointCurr, _skPaint);

            InvalidateVisual(); // Request a redraw

            _points.Add(currentPoint);
            _previousPoint = currentPoint;
        }
    }
    protected void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this).Pointer;
        if (pointer.Type == PointerType.Pen && pointer.Id == _penId)
        {
            _previousPoint = null;
            _isDrawing = false;
            _strokes.Add(new List<Point>(_points));
            _points.Clear();
        }
    }

    public override void Render(DrawingContext context)
    { 
        base.Render(context);

        CopySKBitmapToWriteableBitmap();

        var sourceRect = new Rect(0, 0, _wrBitmap.PixelSize.Width, _wrBitmap.PixelSize.Height);
        var destRect   = new Rect(0, 0, _wrBitmap.PixelSize.Width, _wrBitmap.PixelSize.Height);

        context.DrawImage(_wrBitmap, sourceRect, destRect);
    }

    private void CopySKBitmapToWriteableBitmap()
    {
        // Create a byte array to hold the pixel data
        var pixelData = new byte[_skBitmap.Width * _skBitmap.Height * 4]; // BGRA format

        // Get the pointer to the pixel data and copy it into the byte array
        IntPtr skBitmapPtr = _skBitmap.GetPixels();
        System.Runtime.InteropServices.Marshal.Copy(skBitmapPtr, pixelData, 0, pixelData.Length);

        // Lock the WriteableBitmap for writing
        using (var lockedBitmap = _wrBitmap.Lock())
        {
            // Copy the pixel data into the WriteableBitmap
            System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, lockedBitmap.Address, pixelData.Length);
        }
    }
}