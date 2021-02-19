using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Threading;
using System;
using System.Diagnostics;

using AvaloniaPlatform = Avalonia.Platform;

namespace ScrcpyNet.Avalonia.Sample
{
    public class AvaloniaScrcpy : StreamDecoder
    {
        private int renderedFrames = 0;
        private bool hasNewFrame = false;
        private WriteableBitmap? bmp;
        private readonly Image targetControl;
        private readonly TextBlock textBlock;

        public AvaloniaScrcpy(Image image, TextBlock txt)
        {
            targetControl = image;
            textBlock = txt;

            AvaloniaLocator.Current.GetService<IRenderTimer>().Tick += RenderTick;
        }

        protected unsafe override void OnFrame(FrameData frameData)
        {
            base.OnFrame(frameData);

            var sw = Stopwatch.StartNew();
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

            //Needed because frameData is a ref struct.
            int frameNumber = frameData.FrameNumber;
            Dispatcher.UIThread.Post(() =>
            {
                // Rendering is moved to RenderTick()
                //targetControl.Source = bmp;
                //targetControl.InvalidateVisual();

                int droppedFrames = frameNumber - renderedFrames;
                textBlock.Text = $"Received frames: {frameNumber}\nRendered frames: {renderedFrames}\nDropped frames: {droppedFrames}";
            });
        }

        private void RenderTick(TimeSpan obj)
        {
            if (hasNewFrame)
            {
                targetControl.InvalidateVisual();
                hasNewFrame = false;
                renderedFrames++;
            }
        }
    }
}
