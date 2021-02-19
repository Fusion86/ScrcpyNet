using FFmpeg.AutoGen;
using Serilog;
using System;
using System.Buffers;
using System.Diagnostics;

namespace ScrcpyNet
{
    // TODO: Make FrameData Data format configurable.

    public ref struct FrameData
    {
        /// <summary>
        /// Byte array with the frame data in BGRA32 format.
        /// </summary>
        public ReadOnlySpan<byte> Data { get; }

        public int Width { get; }
        public int Height { get; }
        public int FrameNumber { get; }

        /// <summary>
        /// Frametime in milliseconds.
        /// </summary>
        public long FrameTime { get; }

        public FrameData(ReadOnlySpan<byte> data, int width, int height, int frameNumber, long frameTime)
        {
            Data = data;
            Width = width;
            Height = height;
            FrameNumber = frameNumber;
            FrameTime = frameTime;
        }
    }

    public unsafe class StreamDecoder : IDisposable
    {
        public int FrameNumber { get; private set; }

        private bool disposed;
        private SwsContext* swsContext = null;

        private readonly AVCodec* codec;
        private readonly AVCodecParserContext* parser;
        private readonly AVCodecContext* ctx;
        private readonly AVFrame* frame;
        private readonly AVPacket* packet;
        private readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public StreamDecoder()
        {
            codec = ffmpeg.avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
            if (codec == null) throw new Exception("Couldn't find AVCodec for AV_CODEC_ID_H264.");

            parser = ffmpeg.av_parser_init((int)codec->id);
            if (parser == null) throw new Exception("Couldn't initialize AVCodecParserContext");

            ctx = ffmpeg.avcodec_alloc_context3(codec);
            if (ctx == null) throw new Exception("Couldn't allocate AVCodecContext");

            int ret = ffmpeg.avcodec_open2(ctx, codec, null);
            if (ret < 0) throw new Exception("Couldn't open AVCodecContext.");

            frame = ffmpeg.av_frame_alloc();
            if (frame == null) throw new Exception("Couldn't allocate AVFrame.");

            packet = ffmpeg.av_packet_alloc();
            if (frame == null) throw new Exception("Couldn't allocate AVPacket.");
        }

        ~StreamDecoder()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Decode(byte[] data)
        {
            fixed (byte* dataPtr = data)
            {
                byte* ptr = dataPtr;
                int dataSize = data.Length;

                while (dataSize > 0)
                {
                    int ret = ffmpeg.av_parser_parse2(parser, ctx, &packet->data, &packet->size, ptr, dataSize, ffmpeg.AV_NOPTS_VALUE, ffmpeg.AV_NOPTS_VALUE, 0);

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
                    Log.Error("Error sending a packet for decoding.");
                    return;
                }

                while (ret >= 0)
                {
                    var sw = Stopwatch.StartNew();
                    ret = ffmpeg.avcodec_receive_frame(ctx, frame);

                    if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
                        return;

                    FrameNumber++;

                    int destSize = 4 * frame->width * frame->height;
                    int[] destStride = new int[] { 4 * frame->width };

                    // In my tests the code crashed when we use a C# byte-array (new byte[])
                    byte* destBufferPtr = (byte*)ffmpeg.av_malloc((ulong)destSize);
                    byte*[] dest = { destBufferPtr };

                    swsContext = ffmpeg.sws_getCachedContext(swsContext, frame->width, frame->height, ctx->pix_fmt, frame->width, frame->height, AVPixelFormat.AV_PIX_FMT_BGRA, ffmpeg.SWS_BICUBIC, null, null, null);

                    if (swsContext == null) throw new Exception("Couldn't allocate SwsContext.");

                    int outputSliceHeight = ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, frame->height, dest, destStride);

                    if (outputSliceHeight > 0)
                    {
                        //byte[] managedBuffer = pool.Rent(destSize);
                        //Marshal.Copy((IntPtr)destBufferPtr, managedBuffer, 0, destSize);
                        var managedBuffer = new ReadOnlySpan<byte>(destBufferPtr, destSize);
                        OnFrame(new FrameData(managedBuffer, frame->width, frame->height, ctx->frame_number, sw.ElapsedMilliseconds));
                        //pool.Return(managedBuffer);
                    }
                    else
                    {
                        Log.Warning("outputSliceHeight == 0, not sure if this is bad?");
                    }

                    ffmpeg.av_free(destBufferPtr);
                }
            }
        }

        protected virtual void OnFrame(FrameData frameData)
        {
            Log.Debug("Frame: " + frameData.FrameNumber);
            Log.Debug($"Frametime: {frameData.FrameTime}ms");
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
