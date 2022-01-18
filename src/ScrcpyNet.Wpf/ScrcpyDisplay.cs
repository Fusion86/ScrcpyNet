using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScrcpyNet.Wpf
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ScrcpyNet.Wpf"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ScrcpyNet.Wpf;assembly=ScrcpyNet.Wpf"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ScrcpyDisplay/>
    ///
    /// </summary>
    public class ScrcpyDisplay : Control
    {
        private static readonly ILogger log = Log.ForContext<ScrcpyDisplay>();

        public static readonly DependencyProperty ScrcpyProperty = DependencyProperty.Register(
            nameof(Scrcpy),
            typeof(Scrcpy),
            typeof(ScrcpyDisplay),
            new PropertyMetadata(OnScrcpyChanged));

        private Image? renderTarget;
        private WriteableBitmap? bmp;

        static ScrcpyDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrcpyDisplay), new FrameworkPropertyMetadata(typeof(ScrcpyDisplay)));
        }

        public Scrcpy? Scrcpy
        {
            get => (Scrcpy)GetValue(ScrcpyProperty);
            set => SetValue(ScrcpyProperty, value);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_RenderTargetImage") is Image img)
                renderTarget = img;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            // For some reason WPF doesn't focus the control on click??
            Focus();

            if (Scrcpy != null)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    Scrcpy.SendControlCommand(new BackOrScreenOnControlMessage() { Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN });
                    Scrcpy.SendControlCommand(new BackOrScreenOnControlMessage() { Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP });
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN, e);
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (Scrcpy != null)
            {
                e.Handled = true;
                SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, e);
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Scrcpy != null && renderTarget != null)
            {
                var point = e.GetPosition(renderTarget);

                if (e.LeftButton == MouseButtonState.Pressed && point.X >= 0 && point.Y >= 0)
                {
                    // Do we need to set e.Handled?
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_MOVE, e);
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.KeyCode = KeycodeHelper.ConvertKey(e.Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers(e.KeyboardDevice.Modifiers);
                Scrcpy.SendControlCommand(msg);
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (Scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP;
                msg.KeyCode = KeycodeHelper.ConvertKey(e.Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers(e.KeyboardDevice.Modifiers);
                Scrcpy.SendControlCommand(msg);
            }

            base.OnKeyUp(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var pos = GetScrcpyMousePosition(e);
            if (Scrcpy != null && pos != null)
            {
                e.Handled = true;

                var msg = new ScrollEventControlMessage();
                msg.Position = pos;
                msg.VerticalScroll = e.Delta / 120; // Random guess
                msg.HorizontalScroll = 0; // TODO: Can we implement this?
                Scrcpy.SendControlCommand(msg);
            }

            base.OnMouseWheel(e);
        }

        protected void SendTouchCommand(AndroidMotionEventAction action, MouseEventArgs e)
        {
            var pos = GetScrcpyMousePosition(e);
            if (Scrcpy != null && pos != null)
            {
                var msg = new TouchEventControlMessage();
                msg.Action = action;
                msg.Position = pos;
                Scrcpy.SendControlCommand(msg);

                log.Debug("Sending {Action} for position {PositionX}, {PositionY}", action, msg.Position.Point.X, msg.Position.Point.Y);
            }
        }

        private Position? GetScrcpyMousePosition(MouseEventArgs e)
        {
            if (Scrcpy == null || renderTarget == null) return null;

            var point = e.GetPosition(renderTarget);

            var pos = new Position();
            pos.Point = new Point { X = (int)point.X, Y = (int)point.Y };
            pos.ScreenSize.Width = (ushort)renderTarget.ActualWidth;
            pos.ScreenSize.Height = (ushort)renderTarget.ActualHeight;
            TouchHelper.ScaleToScreenSize(pos, Scrcpy.Width, Scrcpy.Height);

            return pos;
        }

        private unsafe void OnFrame(object? sender, FrameData frameData)
        {
            if (renderTarget != null)
            {
                // This probably isn't the best way to do this.
                try
                {
                    // The timeout is required. Otherwise this will block forever when the application is about to exit but the videoThread sends a last frame.
                    // The DispatcherPriority has been randomly selected, so it might not be the optimal value.
                    Dispatcher.Invoke(() =>
                    {
                        if (bmp == null || bmp.Width != frameData.Width || bmp.Height != frameData.Height)
                        {
                            bmp = new WriteableBitmap(frameData.Width, frameData.Height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                            renderTarget.Source = bmp;
                        }

                        try
                        {
                            bmp.Lock();
                            var dest = new Span<byte>(bmp.BackBuffer.ToPointer(), frameData.Data.Length);
                            frameData.Data.CopyTo(dest);
                            bmp.AddDirtyRect(new Int32Rect(0, 0, frameData.Width, frameData.Height));
                        }
                        finally
                        {
                            bmp.Unlock();
                        }
                    }, DispatcherPriority.Send, default, TimeSpan.FromMilliseconds(200));
                }
                catch (TimeoutException)
                {
                    log.Debug("Ignoring TimeoutException inside OnFrame.");
                }
                catch (TaskCanceledException)
                {
                    log.Debug("Ignoring TaskCanceledException inside OnFrame.");
                }
            }
        }

        private static void OnScrcpyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ScrcpyDisplay display)
            {
                // Unsubscribe on the old scrcpy
                if (e.OldValue is Scrcpy old && old != null)
                    old.VideoStreamDecoder.OnFrame -= display.OnFrame;

                // Subscribe on the new scrcpy
                if (e.NewValue is Scrcpy value && value != null)
                    value.VideoStreamDecoder.OnFrame += display.OnFrame;
            }
        }
    }
}
