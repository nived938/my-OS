using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusOS.Core.Windowing;

namespace NexusOS.Tests;

[TestClass]
public class WindowManagerTests
{
    [TestMethod]
    public void RegisterWindow_AddsToOpenWindows_AndActivates()
    {
        var wm = new WindowManager();
        var win = new WindowInstance("test.app", "Test Window");

        wm.RegisterWindow(win);

        Assert.AreEqual(1, wm.OpenWindows.Count);
        Assert.AreEqual(win.Id, wm.ActiveWindow?.Id);
        Assert.IsTrue(win.IsActive);
    }

    [TestMethod]
    public void MaximizeAndRestore_PreservesNormalBounds()
    {
        var wm = new WindowManager();
        var win = new WindowInstance("test.app", "Test Window", width: 600, height: 400)
        {
            Left = 50,
            Top = 60
        };

        wm.RegisterWindow(win);

        // Maximize
        wm.MaximizeWindow(win.Id);
        Assert.AreEqual(WindowDisplayState.Maximized, win.State);

        // Restore
        wm.RestoreWindow(win.Id);
        Assert.AreEqual(WindowDisplayState.Normal, win.State);
        Assert.AreEqual(50, win.Left);
        Assert.AreEqual(60, win.Top);
        Assert.AreEqual(600, win.Width);
        Assert.AreEqual(400, win.Height);
    }

    [TestMethod]
    public void SnapWindow_PositionsCorrectly()
    {
        var wm = new WindowManager();
        var win = new WindowInstance("test.app", "Test Window");
        wm.RegisterWindow(win);

        // Snap Left Half on 1920x1080 canvas
        wm.SnapWindow(win.Id, SnapPosition.LeftHalf, 1920, 1080);

        Assert.AreEqual(WindowDisplayState.SnappedLeft, win.State);
        Assert.AreEqual(0, win.Left);
        Assert.AreEqual(0, win.Top);
        Assert.AreEqual(960, win.Width);
        Assert.AreEqual(1080, win.Height);

        // Snap Right Half
        wm.SnapWindow(win.Id, SnapPosition.RightHalf, 1920, 1080);
        Assert.AreEqual(WindowDisplayState.SnappedRight, win.State);
        Assert.AreEqual(960, win.Left);
        Assert.AreEqual(0, win.Top);
        Assert.AreEqual(960, win.Width);
        Assert.AreEqual(1080, win.Height);
    }

    [TestMethod]
    public void AltTabNext_CyclesToPreviousActiveWindow()
    {
        var wm = new WindowManager();
        var win1 = new WindowInstance("app1", "Window 1");
        var win2 = new WindowInstance("app2", "Window 2");

        wm.RegisterWindow(win1);
        wm.RegisterWindow(win2);

        // Window 2 is active
        Assert.AreEqual(win2.Id, wm.ActiveWindow?.Id);

        // Alt-Tab cycles to Window 1
        wm.AltTabNext();
        Assert.AreEqual(win1.Id, wm.ActiveWindow?.Id);
    }

    [TestMethod]
    public void MinimizeAll_MinimizesAllWindows()
    {
        var wm = new WindowManager();
        var win1 = new WindowInstance("app1", "Window 1");
        var win2 = new WindowInstance("app2", "Window 2");

        wm.RegisterWindow(win1);
        wm.RegisterWindow(win2);

        wm.MinimizeAll();

        Assert.IsTrue(wm.OpenWindows.All(w => w.State == WindowDisplayState.Minimized));
        Assert.IsNull(wm.ActiveWindow);
    }
}
