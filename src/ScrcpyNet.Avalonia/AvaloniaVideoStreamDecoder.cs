using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Threading;
using System;

using AvaloniaPlatform = Avalonia.Platform;

namespace ScrcpyNet.Avalonia
{
    public class AvaloniaVideoStreamDecoder : VideoStreamDecoder
    {
        public int RenderedFrames { get; private set; }
        public int DroppedFrames => FrameCount - RenderedFrames;

        private bool hasNewFrame = false;
        private Image targetControl;
        private WriteableBitmap? bmp;

        public AvaloniaVideoStreamDecoder(Image image)
        {
            targetControl = image;
            AvaloniaLocator.Current.GetService<IRenderTimer>().Tick += RenderTick;
        }

        protected unsafe override void OnFrame(FrameData frameData)
        {
            base.OnFrame(frameData);

            if (bmp == null || bmp.Size.Width != frameData.Width || bmp.Size.Height != frameData.Height)
            {
                bmp = new WriteableBitmap(new PixelSize(frameData.Width, frameData.Height), new Vector(96, 96), AvaloniaPlatform.PixelFormat.Bgra8888, AvaloniaPlatform.AlphaFormat.Opaque);
                Dispatcher.UIThread.Post(() => targetControl.Source = bmp);
            }

            using (var l = bmp.Lock())
            {
                var dest = new Span<byte>(l.Address.ToPointer(), frameData.Data.Length);
                frameData.Data.CopyTo(dest);
            }

            hasNewFrame = true;
        }

        private void RenderTick(TimeSpan obj)
        {
            if (hasNewFrame)
            {
                targetControl.InvalidateVisual();
                hasNewFrame = false;
                RenderedFrames++;
            }
        }
    }
}
