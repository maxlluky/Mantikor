using SharpPcap;
using SharpPcap.LibPcap;

/// <summary>
/// Automatically records every packet seen on the configured adapter to a
/// timestamped .pcap file while an attack is running, so the intercepted
/// traffic can be inspected afterwards (e.g. in Wireshark). Started by
/// <see cref="Attack_Class.StartAttack"/> and stopped by
/// <see cref="Attack_Class.ForceStop"/> - there is nothing for the user to
/// configure.
/// </summary>
class PacketCapture_Class
{
    private const string CaptureDirectory = "captures";

    private LibPcapLiveDevice? device;
    private CaptureFileWriterDevice? writer;
    private readonly object writeLock = new();
    private volatile bool isCapturing = false;

    /// <summary>Full path of the .pcap file written by the most recent capture.</summary>
    public string? CaptureFilePath { get; private set; }

    /// <summary>
    /// Opens a new .pcap file and starts mirroring every packet the device
    /// sees into it. Safe to call more than once - a capture already in
    /// progress is left untouched.
    /// </summary>
    public void Start(LibPcapLiveDevice pDevice)
    {
        if (isCapturing)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(CaptureDirectory);
            string fileName = $"mantikor_{DateTime.Now:yyyyMMdd_HHmmss}.pcap";
            CaptureFilePath = Path.Combine(CaptureDirectory, fileName);

            writer = new CaptureFileWriterDevice(CaptureFilePath);
            writer.Open();

            device = pDevice;
            if (!device.Opened)
            {
                device.Open();
            }

            device.OnPacketArrival += OnPacketArrival;

            // StartCapture() throws if a capture loop is already running
            // (e.g. one left over from the scanner) - that just means packets
            // are already flowing in, so ignore it.
            try
            {
                device.StartCapture();
            }
            catch (InvalidOperationException) { }

            isCapturing = true;
            Console.WriteLine("#> Recording traffic to \"{0}\".", CaptureFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("#> Could not start packet capture: {0}", ex.Message);
            Stop();
        }
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        // CaptureFileWriterDevice is not guaranteed thread-safe against
        // concurrent writes, and OnPacketArrival can fire on a background
        // libpcap thread while Stop() is tearing things down from the UI
        // thread.
        lock (writeLock)
        {
            try
            {
                writer?.Write(e.GetPacket());
            }
            catch (Exception)
            {
                // Never let a logging failure take down the attack.
            }
        }
    }

    /// <summary>Stops the capture and flushes/closes the .pcap file.</summary>
    public void Stop()
    {
        if (device != null)
        {
            device.OnPacketArrival -= OnPacketArrival;

            if (isCapturing)
            {
                try
                {
                    device.StopCapture();
                }
                catch (Exception)
                {
                    // Device may already be stopped/closed - ignore.
                }
            }
        }

        lock (writeLock)
        {
            writer?.Close();
            writer = null;
        }

        device = null;

        if (isCapturing && CaptureFilePath != null)
        {
            Console.WriteLine("#> Capture saved to \"{0}\".", CaptureFilePath);
        }

        isCapturing = false;
    }
}
