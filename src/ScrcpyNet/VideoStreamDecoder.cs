using FFmpeg.AutoGen;
using Serilog;
using System;
using System.Text;

namespace ScrcpyNet
{
    public unsafe class FrameData : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Byte array with the frame data in BGRA32 format.
        /// WARNING: This field is only valid inside the OnFrame event. After this is finished the Data will be freed.
        /// If you need the Data field outside the OnFrame event then make sure to copy it somewhere.
        /// </summary>
        public ReadOnlySpan<byte> Data
        {
            get
            {
                // This line might not be needed?
                if (disposed) throw new ObjectDisposedException(nameof(FrameData));
                return new ReadOnlySpan<byte>(data, length);
            }
        }

        public int Width { get; }
        public int Height { get; }
        public int FrameNumber { get; }
        public AVPixelFormat PixelFormat { get; }

        private readonly byte* data;
        private readonly int length;

        public FrameData(byte* data, int length, int width, int height, int frameNumber, AVPixelFormat pixelFormat)
        {
            this.data = data;
            this.length = length;
            Width = width;
            Height = height;
            FrameNumber = frameNumber;
            PixelFormat = pixelFormat;
        }

        ~FrameData()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                ffmpeg.av_free(data);

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public unsafe class VideoStreamDecoder : IDisposable
    {
        public Scrcpy? Scrcpy { get; set; }

        /// <summary>
        /// Number of the last decoded frame.
        /// </summary>
        public int FrameCount { get; private set; }

        public event EventHandler<FrameData>? OnFrame;

        private bool disposed;
        private int lastFrameRefCount;
        private readonly object lastFrameLock = new();
        private SwsContext* swsContext = null;
        private FrameData? lastFrame;

        private readonly AVCodec* codec;
        private readonly AVCodecParserContext* parser;
        private readonly AVCodecContext* ctx;
        private readonly AVFrame* frame;
        private readonly AVPacket* packet;

        private static readonly ILogger log = Log.ForContext<VideoStreamDecoder>();

        public VideoStreamDecoder()
        {
            codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec == null) throw new Exception("Couldn't find AVCodec for AV_CODEC_ID_H264.");

            parser = ffmpeg.av_parser_init((int)codec->id);
            if (parser == null) throw new Exception("Couldn't initialize AVCodecParserContext.");

            ctx = ffmpeg.avcodec_alloc_context3(codec);
            if (ctx == null) throw new Exception("Couldn't allocate AVCodecContext.");

            int ret = ffmpeg.avcodec_open2(ctx, codec, null);
            if (ret < 0) throw new Exception("Couldn't open AVCodecContext.");

            frame = ffmpeg.av_frame_alloc();
            if (frame == null) throw new Exception("Couldn't allocate AVFrame.");

            packet = ffmpeg.av_packet_alloc();
            if (frame == null) throw new Exception("Couldn't allocate AVPacket.");
        }

        ~VideoStreamDecoder()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public FrameData? GetLastFrame()
        {
            lock (lastFrameLock)
            {
                lastFrameRefCount++;
                return lastFrame;
            }
        }

        public void Decode(byte[] data, long pts = -1)
        {
            fixed (byte* dataPtr = data)
            {
                byte* ptr = dataPtr;
                int dataSize = data.Length;

                while (dataSize > 0)
                {
                    int ret = ffmpeg.av_parser_parse2(parser, ctx, &packet->data, &packet->size, ptr, dataSize, pts != -1 ? pts : ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);

                    if (ret < 0)
                        throw new Exception("Error while parsing.");

                    ptr += ret;
                    dataSize -= ret;

                    if (packet->size != 0)
                    {
                        DecodePacket();
                    }
                }
            }
        }

        private void DecodePacket()
        {
            int ret = ffmpeg.avcodec_send_packet(ctx, packet);

            if (ret != ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                if (ret < 0)
                {
                    byte[] errorMessageBytes = new byte[512];
                    fixed (byte* ptr = errorMessageBytes)
                    {
                        ffmpeg.av_strerror(ret, ptr, (ulong)errorMessageBytes.Length);
                        string errorMessage = new((sbyte*)ptr, 0, errorMessageBytes.Length - 1, Encoding.ASCII);
                        log.Error("Error sending a packet for decoding. {@ErrorMessage}", errorMessage);
                    }

                    return;
                }

                while (ret >= 0)
                {
                    ret = ffmpeg.avcodec_receive_frame(ctx, frame);

                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                        return;

                    FrameCount++;

                    if (Scrcpy != null)
                    {
                        Scrcpy.Width = frame->width;
                        Scrcpy.Height = frame->height;
                    }

                    int destSize = 4 * frame->width * frame->height;
                    int[] destStride = new int[] { 4 * frame->width };

                    // In my tests the code crashed when we use a C# byte-array (new byte[])
                    byte* destBufferPtr = (byte*)ffmpeg.av_malloc((ulong)destSize);
                    byte*[] dest = { destBufferPtr };

                    // This `free`s the old context if needed, so there is no leak here.
                    swsContext = ffmpeg.sws_getCachedContext(swsContext, frame->width, frame->height, ctx->pix_fmt, frame->width, frame->height, AVPixelFormat.AV_PIX_FMT_BGRA, ffmpeg.SWS_BICUBIC, null, null, null);

                    if (swsContext == null) throw new Exception("Couldn't allocate SwsContext.");

                    int outputSliceHeight = ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, frame->height, dest, destStride);

                    if (outputSliceHeight > 0)
                    {
                        // Poor man's reference counting.
                        lock (lastFrameLock)
                        {
                            //if (lastFrame != null && Interlocked.Read(ref lastFrameRefCount) == 0)
                            if (lastFrame != null && lastFrameRefCount == 0)
                            {
                                // We don't have to dispose it, but then the GC will remove all old frames after 'some time'.
                                // On my 32GB RAM computer the GC allowed the app to use up to 8GB before cleaning it up.
                                lastFrame.Dispose();
                            }
                            //Interlocked.Exchange(ref lastFrameRefCount, 0);
                            lastFrameRefCount = 0;
                        }

                        // FrameData takes ownership of the destBufferPtr and will free it when disposed!
                        lastFrame = new FrameData(destBufferPtr, destSize, frame->width, frame->height, ctx->frame_number, AVPixelFormat.AV_PIX_FMT_BGRA);
                        OnFrame?.Invoke(this, lastFrame);
                    }
                    else
                    {
                        log.Warning("outputSliceHeight == 0, not sure if this is bad?");

                        // Manually free the destBufferPtr when we don't create a FrameData object.
                        ffmpeg.av_free(destBufferPtr);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                ffmpeg.av_parser_close(parser);
                ffmpeg.sws_freeContext(swsContext);

                fixed (AVCodecContext** ptr = &ctx)
                    ffmpeg.avcodec_free_context(ptr);

                fixed (AVFrame** ptr = &frame)
                    ffmpeg.av_frame_free(ptr);

                fixed (AVPacket** ptr = &packet)
                    ffmpeg.av_packet_free(ptr);

                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
