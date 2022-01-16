using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Threading;
using Serilog;
using System;
using AvaloniaPlatform = Avalonia.Platform;

using AVPoint = Avalonia.Point;

namespace ScrcpyNet.Avalonia
{
    public class ScrcpyDisplay : TemplatedControl
    {
        private static readonly ILogger log = Log.ForContext<ScrcpyDisplay>();

        public static readonly DirectProperty<ScrcpyDisplay, Scrcpy?> ScrcpyProperty =
            AvaloniaProperty.RegisterDirect<ScrcpyDisplay, Scrcpy?>(
                nameof(Scrcpy),
                o => o.Scrcpy,
                (o, v) => o.Scrcpy = v);

        public int RenderedFrames { get; private set; }

        private bool isTouching;
        private bool hasNewFrame;
        private Scrcpy? scrcpy;
        private Image? renderTarget;
        private WriteableBitmap? bmp;

        static ScrcpyDisplay()
        {
            FocusableProperty.OverrideDefaultValue(typeof(ScrcpyDisplay), true);
        }

        public ScrcpyDisplay()
        {
            AvaloniaLocator.Current.GetService<IRenderTimer>()!.Tick += RenderTick;
        }

        public Scrcpy? Scrcpy
        {
            get => scrcpy;
            set
            {
                // Unsubscribe on the old scrcpy
                if (scrcpy != null)
                    scrcpy.VideoStreamDecoder.OnFrame -= OnFrame;

                SetAndRaise(ScrcpyProperty, ref scrcpy, value);

                // Subscribe on the new scrcpy
                if (scrcpy != null)
                    scrcpy.VideoStreamDecoder.OnFrame += OnFrame;
            }
        }

        private unsafe void OnFrame(object sender, FrameData frameData)
        {
            if (renderTarget != null)
            {
                if (bmp == null || bmp.Size.Width != frameData.Width || bmp.Size.Height != frameData.Height)
                {
                    bmp = new WriteableBitmap(new PixelSize(frameData.Width, frameData.Height), new Vector(96, 96), AvaloniaPlatform.PixelFormat.Bgra8888, AvaloniaPlatform.AlphaFormat.Opaque);
                    Dispatcher.UIThread.Post(() => renderTarget.Source = bmp);
                }

                using (var l = bmp.Lock())
                {
                    var dest = new Span<byte>(l.Address.ToPointer(), frameData.Data.Length);
                    frameData.Data.CopyTo(dest);
                }

                hasNewFrame = true;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            renderTarget = e.NameScope.Find<Image>("PART_RenderTargetImage");
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.KeyCode = KeycodeHelper.ConvertKey(e.Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers(e.KeyModifiers);
                scrcpy.SendControlCommand(msg);
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP;
                msg.KeyCode = KeycodeHelper.ConvertKey(e.Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers(e.KeyModifiers);
                scrcpy.SendControlCommand(msg);
            }

            base.OnKeyDown(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (scrcpy != null && renderTarget != null)
            {
                var point = e.GetCurrentPoint(renderTarget);

                if (point.Properties.IsRightButtonPressed)
                {
                    e.Handled = true;
                    scrcpy.SendControlCommand(new BackOrScreenOnControlMessage() { Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN });
                    scrcpy.SendControlCommand(new BackOrScreenOnControlMessage() { Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP });
                }
                else if (point.Properties.IsLeftButtonPressed)
                {
                    e.Handled = true;
                    isTouching = true;
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN, point.Position);
                }
            }

            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (scrcpy != null && renderTarget != null)
            {
                var point = e.GetCurrentPoint(renderTarget);

                if (isTouching)
                {
                    e.Handled = true;
                    isTouching = false;
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, point.Position);
                }
            }

            base.OnPointerReleased(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (scrcpy != null && renderTarget != null)
            {
                var point = e.GetCurrentPoint(renderTarget);

                if (isTouching && point.Position.X >= 0 && point.Position.Y >= 0)
                {
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_MOVE, point.Position);
                }
            }

            base.OnPointerMoved(e);
        }

        protected void SendTouchCommand(AndroidMotionEventAction action, AVPoint position)
        {
            if (scrcpy != null && renderTarget != null)
            {
                var msg = new TouchEventControlMessage();
                msg.Action = action;
                msg.Position.Point.X = (int)position.X;
                msg.Position.Point.Y = (int)position.Y;
                msg.Position.ScreenSize.Width = (ushort)renderTarget.Bounds.Width;
                msg.Position.ScreenSize.Height = (ushort)renderTarget.Bounds.Height;
                TouchHelper.ScaleToScreenSize(msg.Position, scrcpy.Width, scrcpy.Height);
                scrcpy.SendControlCommand(msg);

                log.Debug("Sending {Action} for position {PositionX}, {PositionY}", action, msg.Position.Point.X, msg.Position.Point.Y);
            }
        }

        private void RenderTick(TimeSpan obj)
        {
            if (hasNewFrame && renderTarget != null)
            {
                try
                {
                    // This sometimes crashes. Not sure if it's an Avalonia issue?
                    renderTarget.InvalidateVisual();
                    hasNewFrame = false;
                    RenderedFrames++;
                }
                catch
                {
                    Log.Error("Crash!");
                }
            }
        }
    }
}
