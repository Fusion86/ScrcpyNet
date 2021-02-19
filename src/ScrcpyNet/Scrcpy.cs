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

        private volatile bool shouldStop = false;
        private Thread? scrcpyThread;

        private readonly AdbClient adb;
        private readonly DeviceData device;
        private readonly StreamDecoder? decoder;

        public Scrcpy(DeviceData device, StreamDecoder? decoder = null)
        {
            adb = new AdbClient();
            this.device = device;
            this.decoder = decoder;

            if (decoder != null)
                decoder.ScrcpyContext = this;
        }

        public void Start(long bitrate = 1_000_000 * 32)
        {
            Setup();

            scrcpyThread = new Thread(ScrcpyListener);
            scrcpyThread.Start();

            StartScrcpy(bitrate);
        }

        public void Stop()
        {
            if (scrcpyThread != null)
            {
                shouldStop = true;
                scrcpyThread.Join();
            }
        }

        private void ScrcpyListener()
        {
            var pool = ArrayPool<byte>.Shared;

            var listener = new TcpListener(IPAddress.Loopback, 27183);
            listener.Start();

            Log.Information("scrcpy listener started.");
            Log.Information("Waiting for mobile device to connect...");

            while (!shouldStop)
            {
                if (!listener.Pending())
                {
                    Thread.Sleep(10);
                }

                var client = listener.AcceptTcpClient();
                Log.Information("Mobile device connected.");

                var clientStream = client.GetStream();
                clientStream.ReadTimeout = 2000;

                // Read 68-byte header.
                var deviceInfoBuf = pool.Rent(68);
                int bytesRead = clientStream.Read(deviceInfoBuf, 0, 68);

                if (bytesRead == 0)
                    throw new Exception("bytesRead == 0");

                // Decode device name from header.
                var deviceInfoSpan = deviceInfoBuf.AsSpan();
                DeviceName = Encoding.UTF8.GetString(deviceInfoSpan.Slice(0, 64)).TrimEnd(new[] { '\0' });
                Log.Information("Device name: " + DeviceName);

                Width = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[64..]);
                Height = BinaryPrimitives.ReadInt16BigEndian(deviceInfoSpan[66..]);
                Log.Information($"{Width}x{Height}");

                pool.Return(deviceInfoBuf);

                // Read and forward all frames.
                var metaBuf = pool.Rent(12);

                // Loop to read all frames.
                while (!shouldStop)
                {
                    // Read metadata
                    try
                    {
                        bytesRead = clientStream.Read(metaBuf, 0, 12);
                    }
                    catch (IOException ex)
                    {
                        // Timeout, probably?
                        Log.Warning(ex.Message);
                        continue;
                    }

                    if (bytesRead != 12)
                        throw new Exception("bytesRead != 12");

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
                        bytesRead = clientStream.Read(packetBuf, pos, bytesToRead);

                        if (bytesRead == 0)
                            throw new Exception("bytesRead == 0");

                        pos += bytesRead;
                        bytesToRead -= bytesRead;
                    }

                    Log.Verbose($"Presentation Time: {presentationTimeUs} us, PacketSize: {packetSize} bytes");
                    decoder?.Decode(packetBuf, presentationTimeUs);

                    pool.Return(packetBuf);
                }

                Log.Information("Mobile device disconnected.");
            }

            Log.Information("scrcpy listener stopped.");
        }

        private void Setup()
        {
            // Remove any existing network stuff.
            adb.RemoveAllForwards(device);
            adb.RemoveAllReverseForwards(device);

            // Push scrcpy-server.jar
            PushServer();

            // Create port reverse rule
            adb.CreateReverseForward(device, "localabstract:scrcpy", "tcp:27183", true);
        }

        private void StartScrcpy(long bitrate)
        {
            Log.Information("Starting scrcpy...");

            var cts = new CancellationTokenSource();
            var receiver = new SerilogOutputReceiver();

            _ = adb.ExecuteRemoteCommandAsync($"CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.genymobile.scrcpy.Server 1.17 debug 0 {bitrate} 0 0 false - true true 0 false false - -", device, receiver, cts.Token);
        }

        private void PushServer()
        {
            using SyncService service = new SyncService(new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort)), device);
            using Stream stream = File.OpenRead(@"L:\Repos\LupoCV\LupoCV.Core.Test\bin\Debug\netcoreapp3.1\scrcpy-server.jar");
            service.Push(stream, "/data/local/tmp/scrcpy-server.jar", 444, DateTime.Now, null, CancellationToken.None);
        }
    }
}
