using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using InteractiveApp.Models;
using Splat;
using Avalonia.VisualTree;
using Avalonia.Rendering;



#if IOS
using Avalonia.iOS;
using Foundation;
using UIKit;
using CoreGraphics;
#endif

namespace InteractiveApp.Controls;

/// <summary>
/// Interactive Canvas Control for Avalonia.
/// </summary>
public class InteractiveCanvas : Border
{
    //Private Member
#if IOS
    private IOSTouchHandler? _iOSTouchHandler;
#else
    private TouchHandler? _touchHandler;
#endif

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Set background if null. Controls without background 
        // do not trigger pointer events  
        Background = Background ?? Brushes.Transparent;

        #if IOS
        _iOSTouchHandler = new IOSTouchHandler(this, Child);
        #else
        _touchHandler = new TouchHandler(this, Child);
        #endif
    }

    private class TouchHandler
    {
        // Controls
        Control? _parent;
        Control? _child;

        // Constants
        private const double _zoomSpeed = 1.1;
        private const double ZOOM_MAX =  5.0;
        private const double ZOOM_MIN = 1.0/ZOOM_MAX;

        // Private Members
        private Matrix _matrix = Matrix.Identity;
        private TransformOperations.Builder _builder;
        private double _zoomAtSteadyState = 1.0;
        private List<TouchPoint> _touchPoints = new List<TouchPoint>();
        private double _initialDistance = 0.0; // initial distance of two touch points
        private Point _pinchCenter = new Point();
        private bool _penIsDrawing = false;

        // Constructor
        public TouchHandler(Control? parent, Control? child)
        {
            _parent = parent;
            _child = child;

            InitializeMatrixToChild();

            // if the app goes into the background, we want to maintain reasonable state
            Bootstrapper.Globals.WillResignActive += OnAppLostFocus;

            // if the (desktop) app goes into the background, we want to maintain reasonable state
            var applicationLifetime = Application.Current?.ApplicationLifetime;
            if (applicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime &&
                desktopLifetime.MainWindow is Window mainWindow)
            {
                mainWindow.Deactivated += OnAppLostFocus;
            }

            // panning and zooming
            if (_parent != null)
            {
                // Use Cross Platform Avalonia Touch Events
                _parent.PointerPressed += HandlePointerPressed;
                _parent.PointerMoved += HandlePointerMoved;
                _parent.PointerReleased += HandlePointerReleased;

                // Desktop Only
                if (OperatingSystem.IsMacOS() || OperatingSystem.IsWindows() ||
                    OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
                {
                    // zooming
                    _parent.PointerWheelChanged += HandlePointerWheelChanged;
                    Gestures.AddPointerTouchPadGestureMagnifyHandler(_parent,
                        HandlePointerTouchPadGestureMagnify);
                }
            }
        }

        private void InitializeMatrixToChild()
        {
            // copy initial transform matrix of child 
            if (_child != null && _child.RenderTransform != null)
            {
                _matrix = _child.RenderTransform.Value;
            }
        }

        private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var type = e.GetCurrentPoint(_parent).Pointer.Type;
            if (type == PointerType.Pen)
            {
                _penIsDrawing = true;
                _touchPoints.Clear();
                return;
            }
            else if (_touchPoints.Count < 2 && (type == PointerType.Touch | type == PointerType.Mouse))
            {

                _touchPoints.Add(new TouchPoint(e.Pointer.Id,  e.GetPosition(_parent)));
                if (_touchPoints.Count == 2)
                {
                    var point0 = _touchPoints[0].Position;
                    var point1 = _touchPoints[1].Position;
                    _initialDistance = TransformHelper.DistanceBetween(point0, point1);
                    _pinchCenter = new Point((point0.X + point1.X) / 2, (point0.Y + point1.Y) / 2);
                }
            }
        }

        private void HandlePointerMoved(object? sender, PointerEventArgs e)
        { 
            if (_child == null || _parent == null || _penIsDrawing) return;

            // Determine if pointer already exists in touch points list
            // pen input will not be handled normally
            int index = _touchPoints.FindIndex(x => x.Id == e.Pointer.Id);
            if (index != -1)
            {
                if (_touchPoints.Count == 1)
                {
                    // handle one finger panning
                    var previousPoint = _touchPoints[index].Position;
                    var currentPoint  = e.GetPosition(_parent);
                    var delta = new Point(currentPoint.X - previousPoint.X, currentPoint.Y - previousPoint.Y);

                    // translation delta must be scaled according to scale of child
                    delta = TransformHelper.ScalePoint(delta, 1.0/_matrix.M11, 1.0/_matrix.M22);
                    _matrix = TransformHelper.TranslatePrepend(_matrix, delta.X, delta.Y);

                    ApplyTransformation();
                    _touchPoints[index].Position = currentPoint;
                }
                else if (_touchPoints.Count >=2 && (index == 0 || index == 1))
                {
                    // handle pinch scaling
                    _touchPoints[index].Position = e.GetPosition(_parent);
                    var point0 = _touchPoints[0].Position;
                    var point1 = _touchPoints[1].Position;
                    var distance = TransformHelper.DistanceBetween(point0, point1);
                    Point currentPinchCenter = new Point((point0.X + point1.X) / 2, (point0.Y + point1.Y) / 2);
                    Point? scaleOrigin = _parent.TranslatePoint(currentPinchCenter, _child);
                    if (scaleOrigin != null && _initialDistance != 0)
                    {
                        double scaleChange = distance / _initialDistance;
                        ZoomWithScale(scaleChange, scaleOrigin.Value.X, scaleOrigin.Value.Y);
                    }     

                    // handle two finger pan
                    var delta = new Point(currentPinchCenter.X - _pinchCenter.X, currentPinchCenter.Y - _pinchCenter.Y);
                    // translation delta must be scaled according to scale of child
                    delta = TransformHelper.ScalePoint(delta, 1.0/_matrix.M11, 1.0/_matrix.M22);
                    _matrix = TransformHelper.TranslatePrepend(_matrix, delta.X, delta.Y);

                    // apply zoom and/or pan
                    ApplyTransformation();
                    _pinchCenter = currentPinchCenter;
                }
            }
        }

        private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var type = e.GetCurrentPoint(_parent).Pointer.Type;

            if (type == PointerType.Pen )
            {
                _penIsDrawing = false;
                return;
            }
            else if (type == PointerType.Touch | type == PointerType.Mouse)
            {
                var index = _touchPoints.FindIndex(x => x.Id == e.Pointer.Id);
                if (index != -1)
                {
                    _touchPoints.RemoveAt(index);

                    if (index == 0 || index == 1)
                    {
                        // reset zoom steady state when first or second finger is released.
                        _zoomAtSteadyState = _matrix.M11; // should be equal to scale x and y
                    }
                }
            }
        }

        private void HandlePointerTouchPadGestureMagnify(object? sender, PointerDeltaEventArgs e)
        {
            if (_child == null) return;

            var point = e.GetPosition(_child);
            ZoomWithDelta(e.Delta.Y, point.X, point.Y, 2.0);
        }

        private void HandlePointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (_child == null) return;
            var point = e.GetPosition(_child);
            ZoomWithDelta(e.Delta.Y, point.X, point.Y);
        }

        private void OnAppLostFocus(object? sender, EventArgs e) => HandleLostFocus();

        private void HandleLostFocus()
        {
            // When the app window loses focus
            _touchPoints.Clear();
            _zoomAtSteadyState = 1.0;
            _initialDistance = 0.0;
            _pinchCenter = new Point();
            _penIsDrawing = false;
        }

        /// <summary>
        /// Applies transformations to child control
        /// </summary>
        private void ApplyTransformation()
        {
            
            if (_child == null) return;

            _child.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
            _builder = new TransformOperations.Builder(1);
            _builder.AppendMatrix(_matrix);
            _child.RenderTransform = _builder.Build();
            _child.InvalidateVisual();
        }

        /// <summary>
        /// Zoom with the provided delta and provided center point.
        /// </summary>
        /// <param name="delta">The zoom delta.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        public void ZoomWithDelta(double delta, double x, double y, double multiplier = 1.0)
        {
            var scale = Math.Pow(_zoomSpeed * multiplier, delta);
            Matrix scaleMatrix = TransformHelper.ScaleAtPrepend(_matrix, scale, scale, x, y);

            if (ZoomScalingValid(scaleMatrix))
            {
                _matrix = scaleMatrix;
                ApplyTransformation();
            }
        }

        /// <summary>
        /// Zoom with the provided scale and provided center point.
        /// </summary>
        /// <param name="zoom">The zoom scale.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        public void ZoomWithScale(double scale, double x, double y, double multiplier = 1.0)
        {
            var desiredZoom  = scale * _zoomAtSteadyState;
            var existingZoom = _matrix.M11;
            var zoomToApply  = desiredZoom / existingZoom;

            Matrix scaleMatrix = TransformHelper.ScaleAtPrepend(_matrix, zoomToApply, zoomToApply, x, y);
            if (ZoomScalingValid(scaleMatrix))
            {
                _matrix = scaleMatrix;
                ApplyTransformation();
            }
        }

        private bool ZoomScalingValid(Matrix scaleMatrix)
        {
            // work in progress
            return true;
            // // Check Limits
            // bool xScaleWithinLimits = scaleMatrix.M11 < ZOOM_MAX && scaleMatrix.M11 > ZOOM_MIN;
            // bool yScaleWithinLimits = scaleMatrix.M22 < ZOOM_MAX && scaleMatrix.M22 > ZOOM_MIN;

            // return xScaleWithinLimits && yScaleWithinLimits;
        }
    }

#if IOS
    private class IOSTouchHandler
    {
        // Controls
        Control? _parent;
        Control? _child;
        UIView? _uIView;

        // Constants
        private const double ZOOM_MAX =  5.0;
        private const double ZOOM_MIN = 1.0/ZOOM_MAX;
        private const double PALMTHRESHOLD = 60;
        private readonly double POINT_SCALING;

        // Private Members
        private Matrix _matrix = Matrix.Identity;
        private TransformOperations.Builder _builder;
        private double _zoomAtSteadyState = 1.0;
        private List<UITouchPoint> _touches = new List<UITouchPoint>();
        private double _initialDistance = 0.0; // initial distance of two touch points
        private Point _pinchCenter = new Point();
        private bool _penIsDrawing = false;
        private UITouch? _pen;

        private class UITouchPoint
        {
            public readonly UITouch UITouch;
            public readonly Point Origin; // starting location of touch
            public Point Position {get; set;} = new Point();
            public UITouchPoint(UITouch uITouch, Point origin)
            {
                UITouch = uITouch;
                Origin = origin;
                Position = origin;
            }
        }

        private Point ConvertCGPoint(CGPoint point)
        {
            return new Point(point.X / POINT_SCALING, point.Y / POINT_SCALING);
        }

        private double GetPointScaling(Control? control, UIView? uiView)
        {
            double uIWindowHeight = 1;
            var window = uiView?.Window;
            if (window != null)
            {
                uIWindowHeight = window.Frame.Size.Height;
            }

            // if the (desktop) app goes into the background, we want to maintain reasonable state
            var visualRoot = _parent?.GetVisualRoot() as Control;
            double avaloniaHeight = visualRoot?.Height ?? 1.0;

            return avaloniaHeight / uIWindowHeight;
        }

        // Constructor
        public IOSTouchHandler(Control? parent, Control? child)
        {
            _parent = parent;
            _child = child;

            InitializeMatrixToChild();

            var appDelegate = UIApplication.SharedApplication.Delegate as AvaloniaAppDelegate<App>;
            if (appDelegate?.Window != null)
            {
                var view = appDelegate.Window?.RootViewController?.View;
                if (view != null && view.Subviews.Length > 0)
                    _uIView = view.Subviews[0];
                    POINT_SCALING = GetPointScaling(_parent, _uIView);
            }

            // if the app goes into the background, we want to maintain reasonable state
            Bootstrapper.Globals.WillResignActive += OnAppLostFocus;

            // panning and zooming
            if (_parent != null)
            {
                _parent.SizeChanged += (object? sender, SizeChangedEventArgs e) => HandleLostFocus();
                Bootstrapper.Globals.TouchesBegan += HandleTouchesBegan;
                Bootstrapper.Globals.TouchesMoved += HandleTouchesMoved;
                Bootstrapper.Globals.TouchesEnded += HandleTouchesEnded;
                Bootstrapper.Globals.TouchesCancelled += HandleTouchesCancelled;
            }
        }

        private void InitializeMatrixToChild()
        {
            // copy initial transform matrix of child 
            if (_child != null && _child.RenderTransform != null)
            {
                _matrix = _child.RenderTransform.Value;
            }
        }

        private void HandleTouchesBegan(NSSet touches, UIEvent? evt)
        {
            foreach (UITouch touch in touches)
            {
                if (touch.Type == UITouchType.Stylus)
                {
                    _penIsDrawing = true;
                    _pen = touch;
                    _touches.Clear();
                    break;
                }
                else if (_touches.Count < 2 && (touch.Type == UITouchType.Direct))
                {
                    _touches.Add(new UITouchPoint(touch, ConvertCGPoint(touch.LocationInView(_uIView))));
                    if (_touches.Count == 2)
                    {
                        var point0 = _touches[0].Position;
                        var point1 = _touches[1].Position;

                        _initialDistance = TransformHelper.DistanceBetween(point0, point1);
                        _pinchCenter = new Point((point0.X + point1.X) / 2, (point0.Y + point1.Y) / 2);
                    }
                }
            }
        }

        private void HandleTouchesMoved(NSSet touches, UIEvent? evt)
        { 
            if (_child == null || _parent == null || _penIsDrawing) return;

            foreach (UITouch touch in touches)
            {
                int i = _touches.FindIndex(x => x.UITouch == touch);
                if (i != -1)
                {
                    if (_touches.Count == 1)
                    {
                        // handle one finger panning
                        var previousPoint = _touches[i].Position;
                        var currentPoint  = ConvertCGPoint(touch.LocationInView(_uIView));
                        var delta = new Point(currentPoint.X - previousPoint.X,
                            currentPoint.Y - previousPoint.Y);

                        // translation delta must be scaled according to scale of child
                        delta = TransformHelper.ScalePoint(delta, 1.0/_matrix.M11, 1.0/_matrix.M22);
                        _matrix = TransformHelper.TranslatePrepend(_matrix, delta.X, delta.Y);

                        ApplyTransformation();
                        _touches[i].Position = currentPoint;
                    }
                    else if (_touches.Count >=2 && (i == 0 || i == 1))
                    {
                        // handle pinch scaling
                        _touches[i].Position = ConvertCGPoint(touch.LocationInView(_uIView));
                        var point0 = _touches[0].Position;
                        var point1 = _touches[1].Position;
                        var distance = TransformHelper.DistanceBetween(point0, point1);
                        Point currentPinchCenter = new Point((point0.X + point1.X) / 2, (point0.Y + point1.Y) / 2);
                        Point? scaleOrigin = _parent.TranslatePoint(currentPinchCenter, _child);
                        if (scaleOrigin != null && _initialDistance != 0)
                        {
                            double scaleChange = distance / _initialDistance;
                            ZoomWithScale(scaleChange, scaleOrigin.Value.X, scaleOrigin.Value.Y);
                        }     

                        // handle two finger pan
                        var delta = new Point(currentPinchCenter.X - _pinchCenter.X, currentPinchCenter.Y - _pinchCenter.Y);
                        // translation delta must be scaled according to scale of child
                        delta = TransformHelper.ScalePoint(delta, 1.0/_matrix.M11, 1.0/_matrix.M22);
                        _matrix = TransformHelper.TranslatePrepend(_matrix, delta.X, delta.Y);

                        // apply zoom and/or pan
                        ApplyTransformation();
                        _pinchCenter = currentPinchCenter;
                    }
                }
            }
        }

        private void HandleTouchesEnded(NSSet touches, UIEvent? evt)
        {
            foreach (UITouch touch in touches)
            {
                if (touch.Type == UITouchType.Stylus && _pen == touch)
                {
                    _penIsDrawing = false;
                    _pen = null;
                }
                else if (touch.Type == UITouchType.Direct)
                {
                    int i = _touches.FindIndex(x => x.UITouch == touch);
                    if (i != -1)
                    {
                        _touches.RemoveAt(i);
                        if (i == 0 || i == 1)
                        {
                            // reset zoom steady state when first or second finger is released.
                            _zoomAtSteadyState = _matrix.M11; // should be equal to scale x and y
                        }
                    }
                }
            }
        }

        private void HandleTouchesCancelled(NSSet touches, UIEvent? evt)
        {
            foreach (UITouch touch in touches)
            {
                if (touch.Type == UITouchType.Stylus && _pen == touch)
                {
                    _penIsDrawing = false;
                    _pen = null;
                }
                else if (touch.Type == UITouchType.Direct)
                {
                    int i = _touches.FindIndex(x => x.UITouch == touch);
                    if (i != -1)
                    {
                        _touches.RemoveAt(i);
                        if (i == 0 || i == 1)
                        {
                            // reset zoom steady state when first or second finger is released.
                            _zoomAtSteadyState = _matrix.M11; // should be equal to scale x and y
                        }
                    }
                }
            }
        }

        private void OnAppLostFocus(object? sender, EventArgs e) => HandleLostFocus();

        private void HandleLostFocus()
        {
            // When the app window loses focus
            _touches.Clear();
            _zoomAtSteadyState = 1.0;
            _initialDistance = 0.0;
            _pinchCenter = new Point();
            _penIsDrawing = false;
            _pen = null;
        }

        /// <summary>
        /// Applies transformations to child control
        /// </summary>
        private void ApplyTransformation()
        {
            
            if (_child == null) return;

            _child.RenderTransformOrigin = new RelativePoint(new Point(0, 0), RelativeUnit.Relative);
            _builder = new TransformOperations.Builder(1);
            _builder.AppendMatrix(_matrix);
            _child.RenderTransform = _builder.Build();
            _child.InvalidateVisual();
        }

        /// <summary>
        /// Zoom with the provided scale and provided center point.
        /// </summary>
        /// <param name="zoom">The zoom scale.</param>
        /// <param name="x">The center point x axis coordinate.</param>
        /// <param name="y">The center point y axis coordinate.</param>
        public void ZoomWithScale(double scale, double x, double y, double multiplier = 1.0)
        {
            var desiredZoom  = scale * _zoomAtSteadyState;
            var existingZoom = _matrix.M11;
            var zoomToApply  = desiredZoom / existingZoom;

            Matrix scaleMatrix = TransformHelper.ScaleAtPrepend(_matrix, zoomToApply, zoomToApply, x, y);
            if (ZoomScalingValid(scaleMatrix))
            {
                _matrix = scaleMatrix;
                ApplyTransformation();
            }
        }

        private bool ZoomScalingValid(Matrix scaleMatrix)
        {
            // work in progress
            return true;
            // // Check Limits
            // bool xScaleWithinLimits = scaleMatrix.M11 < ZOOM_MAX && scaleMatrix.M11 > ZOOM_MIN;
            // bool yScaleWithinLimits = scaleMatrix.M22 < ZOOM_MAX && scaleMatrix.M22 > ZOOM_MIN;

            // return xScaleWithinLimits && yScaleWithinLimits;
        }
    }
#endif

    // Helper Class
    private static class TransformHelper
    {
        public static Point ScalePoint(Point originalPoint, double scaleX, double scaleY)
        {
            // Multiply both X and Y coordinates by the corresponding scale factors
            double scaledX = originalPoint.X * scaleX;
            double scaledY = originalPoint.Y * scaleY;

            // Return the new scaled point
            return new Point(scaledX, scaledY);
        }

        public static double DistanceBetween(Point p1, Point p2)
        {
            double deltaX = p2.X - p1.X;
            double deltaY = p2.Y - p1.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        /// <summary>
        /// Creates a matrix that is scaling from a specifieds center.
        /// </summary>
        /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
        /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
        /// <param name="centerX">The center X-coordinate of the scaling.</param>
        /// <param name="centerY">The center Y-coordinate of the scaling.</param>
        /// <returns>The created scaling matrix.</returns>
        public static Matrix ScaleAt(double scaleX, double scaleY, double centerX, double centerY)
        {
            return new Matrix(scaleX, 0, 0, scaleY, centerX - (scaleX * centerX), centerY - (scaleY * centerY));
        }

        /// <summary>
        /// Prepends a scale around the center of provided matrix.
        /// </summary>
        /// <param name="matrix">The matrix to prepend scale.</param>
        /// <param name="scaleX">Scaling factor that is applied along the x-axis.</param>
        /// <param name="scaleY">Scaling factor that is applied along the y-axis.</param>
        /// <param name="centerX">The center X-coordinate of the scaling.</param>
        /// <param name="centerY">The center Y-coordinate of the scaling.</param>
        /// <returns>The created scaling matrix.</returns>
        public static Matrix ScaleAtPrepend(Matrix matrix, double scaleX, double scaleY, double centerX, double centerY)
        {
            return  ScaleAt(scaleX, scaleY, centerX, centerY) * matrix;
        }

        /// <summary>
        /// Creates a translation matrix using the specified offsets.
        /// </summary>
        /// <param name="offsetX">X-coordinate offset.</param>
        /// <param name="offsetY">Y-coordinate offset.</param>
        /// <returns>The created translation matrix.</returns>
        public static Matrix Translate(double offsetX, double offsetY)
        {
            return new Matrix(1.0, 0.0, 0.0, 1.0, offsetX, offsetY);
        }

        /// <summary>
        /// Prepends a translation around the center of provided matrix.
        /// </summary>
        /// <param name="matrix">The matrix to prepend translation.</param>
        /// <param name="offsetX">X-coordinate offset.</param>
        /// <param name="offsetY">Y-coordinate offset.</param>
        /// <returns>The created translation matrix.</returns>
        public static Matrix TranslatePrepend(Matrix matrix, double offsetX, double offsetY)
        {
            return Translate(offsetX, offsetY) * matrix;
        }
    }

    // class to maintain information on a single touch point
    private class TouchPoint
    {
        public readonly int Id;
        public readonly Point Origin; // starting location of touch
        public Point Position {get; set;} = new Point();
        public TouchPoint(int id, Point origin)
        {
            Id = id;
            Origin = origin;
            Position = origin;
        }
    }

    // Helper Class
    private static class PrintHelper
    {
        public static string PointToString(Point p)
        {
            return $"({p.X:0.000},{p.Y:0.000})";
        }

        public static string TouchPointsToString(List<TouchPoint> touchPoints)
        {
            string s = "";
            touchPoints.ForEach(x => {s += x.Id + ","; });
            return s;
        }

        public static void DebugPrint(string s)
        {
            try {
                while (Bootstrapper.Resolver.GetService<DataModel>()!.DebugValue.Count > 10)
                {
                    Bootstrapper.Resolver.GetService<DataModel>()!.DebugValue.RemoveAt(0);
                }

                Bootstrapper.Resolver.GetService<DataModel>()!.DebugValue.Add(s);
                Bootstrapper.Resolver.GetService<DataModel>()!.DebugValue = new DataModel.DebugList(Bootstrapper.Resolver.GetService<DataModel>()!.DebugValue);
            }
            catch (Exception)
            {
                // silently fail
            }
        }
    }
}
