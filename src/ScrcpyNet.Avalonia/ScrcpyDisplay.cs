using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

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
    }
}
