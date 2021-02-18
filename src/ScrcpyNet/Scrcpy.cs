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
        private volatile bool shouldStop = false;
        private Thread? scrcpyThread;

        private readonly AdbClient adb;
        private readonly DeviceData device;

        public Scrcpy(DeviceData device)
        {
            adb = new AdbClient();
            this.device = device;
        }

        public void Start()
        {
            Setup();

            scrcpyThread = new Thread(ScrcpyListener);
            scrcpyThread.Start();

            StartScrcpy(1_000_000 * 32);
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
                string deviceName = Encoding.UTF8.GetString(deviceInfoBuf.AsSpan().Slice(0, 64)).TrimEnd(new[] { '\0' });
                Log.Information("Device name: " + deviceName);

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

                    var span = metaBuf.AsSpan();

                    // Decode metadata
                    var presentationTimeUs = BinaryPrimitives.ReadInt64BigEndian(span);
                    var packetSize = BinaryPrimitives.ReadInt32BigEndian(span[8..]);

                    // Read the whole frame, this might require more than one .Read() call.
                    var frameBuf = pool.Rent(packetSize + 12);
                    var pos = 0;
                    var bytesToRead = packetSize;

                    while (bytesToRead != 0)
                    {
                        bytesRead = clientStream.Read(frameBuf, pos, bytesToRead);

                        if (bytesRead == 0)
                            throw new Exception("bytesRead == 0");

                        pos += bytesRead;
                        bytesToRead -= bytesRead;
                    }

                    Log.Verbose($"Presentation Time: {presentationTimeUs} us, PacketSize: {packetSize} bytes");

                    pool.Return(frameBuf);
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

            // TODO: Create own ConsoleOutputReceiver
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
