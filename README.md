# Avalonia Interactive Canvas

An Avalonia sample app for drawing on a canvas while supporting pan and zoom gestures across desktop, browser, Android, and iOS targets.

The core scenario is a large `DrawingCanvas` hosted inside an `InteractiveCanvas`. Pen input draws strokes with SkiaSharp, while touch and mouse input can pan or pinch/scroll zoom the canvas.

The iOS target contains custom native code because Avalonia's iOS pointer/touch behavior has bugs that affect this interactive canvas use case.