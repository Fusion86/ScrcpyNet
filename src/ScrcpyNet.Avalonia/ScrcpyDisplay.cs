using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Serilog;

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
            Log.Verbose("OnKeyDown: {@Key}", e.Key);

            if (scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.KeyCode = KeycodeConverter.ConvertKey(e.Key);
                msg.Metastate = KeycodeConverter.ConvertModifiers(e.KeyModifiers);
                scrcpy.SendControlCommand(msg);
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            Log.Verbose("OnKeyUp: {@Key}", e.Key);

            if (scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.Action = AndroidKeyeventAction.AKEY_EVENT_ACTION_UP;
                msg.KeyCode = KeycodeConverter.ConvertKey(e.Key);
                msg.Metastate = KeycodeConverter.ConvertModifiers(e.KeyModifiers);
                scrcpy.SendControlCommand(msg);
            }

            base.OnKeyDown(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (scrcpy != null && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                e.Handled = true;

                var msg = new BackOrScreenOnControlMessage();
                scrcpy.SendControlCommand(msg);
            }

            base.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (scrcpy != null && renderTarget != null)
            {
                var point = e.GetCurrentPoint(renderTarget);
                AndroidMotioneventAction? action = null;

                if (point.Properties.IsLeftButtonPressed)
                {
                    if (isTouching)
                    {
                        action = AndroidMotioneventAction.AMOTION_EVENT_ACTION_MOVE;
                    }
                    else
                    {
                        action = AndroidMotioneventAction.AMOTION_EVENT_ACTION_DOWN;
                        isTouching = true;
                    }
                }
                else if (isTouching)
                {
                    // Stop existing event when mouse button is released.
                    action = AndroidMotioneventAction.AMOTION_EVENT_ACTION_UP;
                    isTouching = false;
                }

                if (action != null && point.Position.X >= 0 && point.Position.Y >= 0)
                {
                    // TODO: This doesn't seem to work.
                    var msg = new TouchEventControlMessage();
                    msg.Action = action.Value;
                    msg.Position.Point.X = (int)point.Position.X;
                    msg.Position.Point.Y = (int)point.Position.Y;
                    msg.Position.ScreenSize.Width = (ushort)renderTarget.Bounds.Width;
                    msg.Position.ScreenSize.Height = (ushort)renderTarget.Bounds.Height;
                    scrcpy.SendControlCommand(msg);
                    Log.Verbose("TouchEvent: {@Action}, {@X}, {@Y}", action.Value, point.Position.X, point.Position.Y);
                }
            }

            base.OnPointerMoved(e);
        }
    }
}
