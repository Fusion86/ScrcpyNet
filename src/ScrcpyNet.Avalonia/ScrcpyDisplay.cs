using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Serilog;

using AVPoint = Avalonia.Point;

namespace ScrcpyNet.Avalonia
{
    public class ScrcpyDisplay : TemplatedControl
    {
        public static readonly DirectProperty<ScrcpyDisplay, Scrcpy?> ScrcpyProperty =
            AvaloniaProperty.RegisterDirect<ScrcpyDisplay, Scrcpy?>(
                nameof(Scrcpy),
                o => o.Scrcpy,
                (o, v) => o.Scrcpy = v);

        private Scrcpy? scrcpy;
        private Image? renderTarget;
        private bool isTouching;

        static ScrcpyDisplay()
        {
            FocusableProperty.OverrideDefaultValue(typeof(ScrcpyDisplay), true);
        }

        public Scrcpy? Scrcpy
        {
            get => scrcpy;
            set
            {
                SetAndRaise(ScrcpyProperty, ref scrcpy, value);
                UpdateRenderTarget();
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            renderTarget = e.NameScope.Find<Image>("PART_RenderTargetImage");
            UpdateRenderTarget();
        }

        protected void UpdateRenderTarget()
        {
            if (renderTarget != null && scrcpy != null)
                scrcpy.SetDecoder(new AvaloniaVideoStreamDecoder(renderTarget));
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
                msg.Action = AndroidKeyeventAction.AKEY_EVENT_ACTION_UP;
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
                    var msg = new BackOrScreenOnControlMessage();
                    scrcpy.SendControlCommand(msg);
                }
                else if (point.Properties.IsLeftButtonPressed)
                {
                    e.Handled = true;
                    isTouching = true;
                    SendTouchCommand(AndroidMotioneventAction.AMOTION_EVENT_ACTION_DOWN, point.Position);
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
                    SendTouchCommand(AndroidMotioneventAction.AMOTION_EVENT_ACTION_UP, point.Position);
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
                    SendTouchCommand(AndroidMotioneventAction.AMOTION_EVENT_ACTION_MOVE, point.Position);
                }
            }

            base.OnPointerMoved(e);
        }

        protected void SendTouchCommand(AndroidMotioneventAction action, AVPoint position)
        {
            if (scrcpy != null && renderTarget != null)
            {
                var msg = new TouchEventControlMessage();
                msg.Action = action;
                msg.Position.Point.X = (int)position.X;
                msg.Position.Point.Y = (int)position.Y;
                msg.Position.ScreenSize.Width = (ushort)renderTarget.Bounds.Width;
                msg.Position.ScreenSize.Height = (ushort)renderTarget.Bounds.Height;
                TouchHelper.ScaleToScreenSize(ref msg.Position, scrcpy.Width, scrcpy.Height);
                scrcpy.SendControlCommand(msg);
            }
        }
    }
}
