using Serilog;
using SharpAdbClient;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ScrcpyNet
{
    public class Scrcpy
    {
        public string DeviceName { get; private set; } = "";
        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool Connected { get; set; }

        private Thread? videoThread;
        private Thread? controlThread;
        private TcpClient? videoClient;
        private TcpClient? controlClient;
        private TcpListener? listener;
        private CancellationTokenSource? cts;
        private VideoStreamDecoder? videoStreamDecoder;

        private readonly AdbClient adb;
        private readonly DeviceData device;
        private static readonly ArrayPool<byte> pool = ArrayPool<byte>.Shared;

        public Scrcpy(DeviceData device, VideoStreamDecoder? videoStreamDecoder = null)
        {
            adb = new AdbClient();
            this.device = device;
            this.videoStreamDecoder = videoStreamDecoder ?? new VideoStreamDecoder();
        }

        public void SetDecoder(VideoStreamDecoder videoStreamDecoder)
        {
            this.videoStreamDecoder = videoStreamDecoder;
        }

        public void Start(long timeoutMs = 5000)
        {
            if (Connected)
                throw new Exception("Already connected.");

            MobileServerSetup();

            listener = new TcpListener(IPAddress.Loopback, 27183);
            listener.Start();

            MobileServerStart();

            int waitTimeMs = 0;
            while (!listener.Pending())
            {
                Thread.Sleep(10);
                waitTimeMs += 10;

                if (waitTimeMs > timeoutMs)
                    throw new Exception("Timeout while waiting for server to connect.");
            }

            videoClient = listener.AcceptTcpClient();
            Log.Information("Video socket connected.");

            if (!listener.Pending())
                throw new Exception("Server is not sending a second connection request. Is 'control' disabled?");

            controlClient = listener.AcceptTcpClient();
            Log.Information("Control socket connected.");

            ReadDeviceInfo();

            cts = new CancellationTokenSource();

            videoThread = new Thread(VideoMain);
            controlThread = new Thread(ControllerMain);

            videoThread.Start();
            controlThread.Start();

            Connected = true;

            // ADB forward/reverse is not needed anymore.
            MobileServerCleanup();
        }

        public void Stop()
        {
            if (!Connected)
                throw new Exception("Not connected.");

            cts?.Cancel();
            videoThread?.Join();
            controlThread?.Join();
            listener?.Stop();
        }

        // TODO: Implement this
        public void SendCommandTmp()
        {
            if (controlClient == null)
                throw new Exception();

            var stream = controlClient.GetStream();

            // Keypress
            var msg = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 };
            stream.Write(msg);

            // Key release
            msg = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 };
            stream.Write(msg);
        }

        private void ReadDeviceInfo()
        {
            if (videoClient == null)
                throw new Exception("Can't read device info when videoClient is null.");

            var infoStream = videoClient.GetStream();
            infoStream.ReadTimeout = 2000;

            // Read 68-byte header.
            var deviceInfoBuf = pool.Rent(68);
            int bytesRead = infoStream.Read(deviceInfoBuf, 0, 68);

            if (bytesRead != 68)
                throw new Exception($"Expected to read exactly 68 bytes, but got {bytesRead} bytes.");

            // Decode device name from header.
            var deviceInfoSpan = deviceInfoBuf.AsSpan();
            DeviceName = Encoding.UTF8.GetString(deviceInfoSpan.Slice(0, 64)).TrimEnd(new[] { '\0' });
            Log.Information("Device name: " + DeviceName);

            Width = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[64..]);
            Height = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[66..]);
            Log.Information($"{Width}x{Height}");

            pool.Return(deviceInfoBuf);
        }

        private void VideoMain()
        {
            // Both of these should never happen.
            if (videoClient == null) throw new Exception("videoClient is null.");
            if (cts == null) throw new Exception("cts is null.");

            var videoStream = videoClient.GetStream();
            videoStream.ReadTimeout = 2000;

            int bytesRead = 0;
            var metaBuf = pool.Rent(12);

            while (!cts.Token.IsCancellationRequested)
            {
                // Read metadata (each packet starts with some metadata)
                try
                {
                    bytesRead = videoStream.Read(metaBuf, 0, 12);
                }
                catch (IOException ex)
                {
                    // Ignore timeout errors.
                    if (ex.InnerException is SocketException x && x.SocketErrorCode == SocketError.TimedOut)
                        continue;
                    throw ex;
                }

                if (bytesRead != 12)
                    throw new Exception($"Expected to read exactly 12 bytes, but got {bytesRead} bytes.");

                // Decode metadata
                var metaSpan = metaBuf.AsSpan();
                var presentationTimeUs = BinaryPrimitives.ReadInt64BigEndian(metaSpan);
                var packetSize = BinaryPrimitives.ReadInt32BigEndian(metaSpan[8..]);

                // Read the whole frame, this might require more than one .Read() call.
                var packetBuf = pool.Rent(packetSize);
                var pos = 0;
                var bytesToRead = packetSize;

                while (bytesToRead != 0)
                {
                    bytesRead = videoStream.Read(packetBuf, pos, bytesToRead);

                    if (bytesRead == 0)
                        throw new Exception("Unable to read any bytes.");

                    pos += bytesRead;
                    bytesToRead -= bytesRead;
                }

                Log.Verbose($"Presentation Time: {presentationTimeUs}us, PacketSize: {packetSize} bytes");
                videoStreamDecoder?.Decode(packetBuf, presentationTimeUs);

                pool.Return(packetBuf);
            }
        }

        private void ControllerMain()
        {
            // Both of these should never happen.
            if (controlClient == null) throw new Exception("controlClient is null.");
            if (cts == null) throw new Exception("cts is null.");

            while (!cts.Token.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        }

        private void MobileServerSetup()
        {
            MobileServerCleanup();

            // Push scrcpy-server.jar
            UploadMobileServer();

            // Create port reverse rule
            adb.CreateReverseForward(device, "localabstract:scrcpy", "tcp:27183", true);
        }

        /// <summary>
        /// Remove ADB forwards/reverses.
        /// </summary>
        private void MobileServerCleanup()
        {
            // Remove any existing network stuff.
            adb.RemoveAllForwards(device);
            adb.RemoveAllReverseForwards(device);
        }

        /// <summary>
        /// Start the scrcpy server on the android device.
        /// </summary>
        /// <param name="bitrate"></param>
        private void MobileServerStart()
        {
            Log.Information("Starting scrcpy server...");

            var cts = new CancellationTokenSource();
            var receiver = new SerilogOutputReceiver();

            string version = "1.17";
            long bitrate = 8000000;
            int maxFramerate = 0;
            int orientation = -1; // -1 means allow rotate
            string control = "true";
            string showTouches = "false";
            string stayAwake = "false";
            string command = $"CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.genymobile.scrcpy.Server {version} debug 0 {bitrate} {maxFramerate} {orientation} false - true {control} 0 {showTouches} {stayAwake} - -";

            Log.Information("Start command: " + command);
            _ = adb.ExecuteRemoteCommandAsync(command, device, receiver, cts.Token);
        }

        private void UploadMobileServer()
        {
            using SyncService service = new SyncService(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), device);
            using Stream stream = File.OpenRead(@"L:\Repos\LupoCV\LupoCV.Core.Test\bin\Debug\netcoreapp3.1\scrcpy-server.jar");
            service.Push(stream, "/data/local/tmp/scrcpy-server.jar", 444, DateTime.Now, null, CancellationToken.None);
        }
    }
}
