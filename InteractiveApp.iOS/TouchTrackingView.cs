using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CoreGraphics;
using Foundation;
using InteractiveApp.Models;
using Splat;
using UIKit;

namespace InteractiveApp.iOS;

public class TouchTrackingView : UIView
{
    private UIView _superView;
    public TouchTrackingView(UIView superView)
    {
        _superView = superView;
        AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
        UserInteractionEnabled = true;
        MultipleTouchEnabled = true;
        //BackgroundColor = UIColor.Blue.ColorWithAlpha((nfloat)0.3);
        ContentMode = UIViewContentMode.ScaleToFill;
    }
    public override void TouchesBegan(NSSet touches, UIEvent? evt)
    {
        Bootstrapper.Globals.InvokeTouchesBegan(touches, evt);
        _superView.TouchesBegan(touches, evt);
    }

    public override void TouchesMoved(NSSet touches, UIEvent? evt)
    {
        Bootstrapper.Globals.InvokeTouchesMoved(touches, evt);
        _superView.TouchesMoved(touches, evt);
    }

    public override void TouchesEnded(NSSet touches, UIEvent? evt)
    {
        Bootstrapper.Globals.InvokeTouchesEnded(touches, evt);
        _superView.TouchesEnded(touches, evt);
    }

    public override void TouchesCancelled(NSSet touches, UIEvent? evt)
    {
        Bootstrapper.Globals.InvokeTouchesCancelled(touches, evt);
        _superView.TouchesCancelled(touches, evt);
    }
}