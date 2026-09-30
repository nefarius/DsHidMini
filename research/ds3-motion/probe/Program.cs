// DS3 / SIXAXIS motion research probe over WinUSB (Zadig-bound 054C:0268).
// See ../README.md for the WinUSB binding procedure and the measurement protocols,
// and docs/MOTION.md for what the numbers mean.
//
//   Ds3MotionProbe --name <label> [--out <dir>] [--seconds N] [--interactive] [--calbyte] [--no-stream] [--wait]
//
// --out defaults to a "captures" folder beside the executable.
// --wait polls for up to two minutes until a WinUSB-bound pad appears AND answers on EP0, so the
// dump starts the instant the pad is plugged in (for pads that go dead on the bus shortly after -
// original SIXAXIS units do this under WinUSB).
//
// - dumps Feature 0x01/0xF2/0xF5/0xF7/0xF8 and 0xEF (plain + every 0x10 page via SET 0xEF page select)
// - enables streaming (0xF4 42 0C 00 00), reads EP1 IN, logs raw/sixaxis.sys-style/calibrated sensor values
// - --interactive: six-orientation accelerometer table plus a 5 s yaw window
// - --calbyte: waits for the gyro zero to settle, then cycles the cal byte in the output report
//   (+2/+4/0/-2/-4 steps) and measures the raw shift per step; the byte actually on the wire is
//   written to the CSV's calByte column, so the plateaus can be told apart from yaw turns offline
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Ds3MotionProbe;
using Microsoft.Win32;
string name = "controller";
// Keep packaged runs self-contained. Developer invocations can still override this with --out.
string outDir = Path.Combine(AppContext.BaseDirectory, "captures");
int seconds = 15;
bool interactive = false, calByteTest = false, stream = true, waitForDevice = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--name": name = args[++i]; break;
        case "--out": outDir = args[++i]; break;
        case "--seconds": seconds = int.Parse(args[++i]); break;
        case "--interactive": interactive = true; break;
        case "--calbyte": calByteTest = true; break;
        case "--no-stream": stream = false; break;
        case "--wait": waitForDevice = true; break;
        default: Console.Error.WriteLine($"unknown arg {args[i]}"); return 1;
    }
}

outDir = Path.GetFullPath(outDir);
Directory.CreateDirectory(outDir);
string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
string logPath = Path.Combine(outDir, $"{name}_{stamp}.txt");
string csvPath = Path.Combine(outDir, $"{name}_{stamp}_stream.csv");
using var log = new StreamWriter(logPath, false, new UTF8Encoding(false)) { AutoFlush = true };

void Log(string s)
{
    Console.WriteLine(s);
    log.WriteLine(s);
}

static string Hex(ReadOnlySpan<byte> b)
{
    var sb = new StringBuilder(b.Length * 3);
    for (int i = 0; i < b.Length; i++)
    {
        if (i > 0 && i % 16 == 0) sb.Append("\n    ");
        sb.Append(b[i].ToString("x2")).Append(' ');
    }

    return sb.ToString().TrimEnd();
}

static string HexRedacted(ReadOnlySpan<byte> b, params int[] redactedOffsets)
{
    var redacted = redactedOffsets.ToHashSet();
    var sb = new StringBuilder(b.Length * 3);
    for (int i = 0; i < b.Length; i++)
    {
        if (i > 0 && i % 16 == 0) sb.Append("\n    ");
        sb.Append(redacted.Contains(i) ? "xx" : b[i].ToString("x2")).Append(' ');
    }

    return sb.ToString().TrimEnd();
}

// --- locate WinUSB-bound DS3 -------------------------------------------------------------
WinUsbRaw? dev = null;
WinUsbRaw? unverified = null;
string? unverifiedPath = null;
var waitClock = Stopwatch.StartNew();
bool announcedWait = false;
var seenMessages = new HashSet<string>();
void LogOnce(string s) { if (seenMessages.Add(s)) Log(s); }

while (true)
{
    var candidates = new List<(string instance, Guid guid)>();
    using (RegistryKey? root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_054C&PID_0268"))
    {
        foreach (string inst in root?.GetSubKeyNames() ?? [])
        {
            using RegistryKey? k = root!.OpenSubKey(inst);
            if (!string.Equals(k?.GetValue("Service") as string, "WinUSB", StringComparison.OrdinalIgnoreCase)) continue;
            using RegistryKey? dp = k!.OpenSubKey("Device Parameters");
            if (dp?.GetValue("DeviceInterfaceGUIDs") is string[] guids)
            {
                foreach (string g in guids) candidates.Add((inst, Guid.Parse(g)));
            }
        }
    }

    if (candidates.Count == 0 && !waitForDevice)
    {
        Log("No 054C:0268 bound to WinUSB found (check Zadig).");
        return 2;
    }

    foreach ((string inst, Guid guid) in candidates)
    {
        // device interface symbolic link as created by the WinUSB driver for this instance
        string path = $@"\\?\usb#vid_054c&pid_0268#{inst}#{{{guid}}}";
        WinUsbRaw? c = null;
        try
        {
            c = new WinUsbRaw(path);
        }
        catch (Exception ex)
        {
            LogOnce($"# skip {path}: {ex.Message}");
            continue;
        }

        // a stale interface from an earlier Zadig install still opens and serves cached
        // descriptors, but every real transfer fails - prove EP0 works before committing
        try
        {
            var probe = new byte[18];
            c.ControlIn(0x80, 0x06, 0x0100, 0, probe);
            dev = c;
            Log($"# device: instance {inst}, guid {guid}, path {path} (found after {waitClock.Elapsed.TotalSeconds:F1} s)");
            break;
        }
        catch (Exception ex)
        {
            LogOnce($"# interface {guid} opens but EP0 fails: {ex.Message}");
            if (!waitForDevice && unverified is null) { unverified = c; unverifiedPath = path; } else { c.Dispose(); }
        }
    }

    if (dev is not null || !waitForDevice || waitClock.Elapsed.TotalSeconds > 120) break;

    if (!announcedWait)
    {
        Log("# --wait: polling for a pad that answers on EP0 - (re)plug it now ...");
        announcedWait = true;
    }

    Thread.Sleep(100);
}

if (dev is null && waitForDevice)
{
    Log("# --wait timed out after 120 s without a pad that answers on EP0.");
    return 3;
}

if (dev is null && unverified is not null)
{
    Log($"# no interface passed the EP0 check; continuing with {unverifiedPath} anyway.");
    Log("# If every request below fails with error 31, the pad's control endpoint is wedged:");
    Log("#   unplug it, plug it back in (ideally into a different port) and re-run.");
    dev = unverified;
}
else
{
    unverified?.Dispose();
}

if (dev is null)
{
    Log("WinUSB device interface not present (device unplugged?).");
    return 3;
}

using WinUsbRaw usb = dev;
Log($"# {name}  {DateTime.Now:O}");
Log($"# descriptor: VID {usb.Descriptor.idVendor:X4} PID {usb.Descriptor.idProduct:X4} bcdDevice {usb.Descriptor.bcdDevice:X4} bcdUSB {usb.Descriptor.bcdUSB:X4} " +
    $"strings: iManufacturer {usb.Descriptor.iManufacturer} '{usb.TryGetString(usb.Descriptor.iManufacturer) ?? "-"}' iProduct {usb.Descriptor.iProduct} '{usb.TryGetString(usb.Descriptor.iProduct) ?? "-"}' iSerial {usb.Descriptor.iSerialNumber}");
Log($"# interface 0: class 0x{usb.InterfaceClass:X2}");
foreach (WinUsbRaw.WINUSB_PIPE_INFORMATION p in usb.Pipes) Log($"#   pipe 0x{p.PipeId:X2} type {p.PipeType} {((p.PipeId & 0x80) != 0 ? "IN" : "OUT")} mps {p.MaximumPacketSize} interval {p.Interval}");

// HID class requests on interface 0
const byte ReqTypeGetClassItf = 0xA1, ReqTypeSetClassItf = 0x21;
const byte GetReport = 0x01, SetReport = 0x09;

byte[] GetFeature(byte id, int len = 64)
{
    var buf = new byte[len];
    int n = usb.ControlIn(ReqTypeGetClassItf, GetReport, (ushort)(0x0300 | id), 0, buf);
    return buf.AsSpan(0, n).ToArray();
}

void SetFeature(byte id, ReadOnlySpan<byte> data) => usb.ControlOut(ReqTypeSetClassItf, SetReport, (ushort)(0x0300 | id), 0, data);

void SetOutput(ReadOnlySpan<byte> data48) => usb.ControlOut(ReqTypeSetClassItf, SetReport, 0x0201, 0, data48);

byte[] TryGetFeature(byte id, string label, params int[] redactedOffsets)
{
    try
    {
        byte[] r = GetFeature(id);
        string rendered = redactedOffsets.Length == 0 ? Hex(r) : HexRedacted(r, redactedOffsets);
        Log($"GET Feature 0x{id:X2} ({label}) len={r.Length}\n    {rendered}");
        return r;
    }
    catch (Exception ex)
    {
        Log($"GET Feature 0x{id:X2} ({label}) FAILED: {ex.Message}");
        return [];
    }
}

// --- control path sanity + PS3-style SET_IDLE ------------------------------------------------
try
{
    var dd = new byte[18];
    int n = usb.ControlIn(0x80, 0x06, 0x0100, 0, dd);
    Log($"# EP0 sanity GET_DESCRIPTOR(device) ok, {n} bytes: {Hex(dd.AsSpan(0, n))}");
}
catch (Exception ex) { Log($"# EP0 sanity GET_DESCRIPTOR(device) FAILED: {ex.Message}"); }

bool Ep0Works()
{
    try
    {
        var dd = new byte[18];
        usb.ControlIn(0x80, 0x06, 0x0100, 0, dd);
        return true;
    }
    catch { return false; }
}

if (!Ep0Works())
{
    // Some pads (observed on an original SIXAXIS) come up with EP0 halted under WinUSB
    // and answer nothing until the endpoints are re-armed and the IN pipe has been read.
    Log("# EP0 is not responding - attempting recovery");
    foreach (byte pid in new byte[] { 0x00 }.Concat(usb.Pipes.Select(p => p.PipeId)))
    {
        Log($"#   ResetPipe 0x{pid:X2}: {usb.TryResetPipe(pid) ?? "ok"}");
    }

    Log($"#   SetCurrentAlternateSetting(0): {usb.TrySelectAltSetting0() ?? "ok"}");
    Log($"#   EP0 after reset: {(Ep0Works() ? "ok" : "still failing")}");

    if (!Ep0Works())
    {
        var inPipeInfo = usb.Pipes.FirstOrDefault(p => (p.PipeId & 0x80) != 0);
        if ((inPipeInfo.PipeId & 0x80) == 0)
        {
            Log("# no interrupt-IN pipe is available");
            return 3;
        }

        byte inp = inPipeInfo.PipeId;
        usb.SetPipeTimeout(inp, 500);
        var warm = new byte[64];
        for (int i = 0; i < 3 && !Ep0Works(); i++)
        {
            try
            {
                int n = usb.ReadPipe(inp, warm);
                Log($"#   interrupt IN read {n} bytes: {Hex(warm.AsSpan(0, Math.Min(n, 16)))}");
            }
            catch (Exception ex) { Log($"#   interrupt IN read failed: {ex.Message}"); break; }
        }

        Log($"#   EP0 after reading input reports: {(Ep0Works() ? "ok" : "still failing")}");
    }
}

try
{
    usb.ControlOut(ReqTypeSetClassItf, 0x0A, 0x0000, 0, []);
    Log("# SET_IDLE accepted");
}
catch (Exception ex) { Log($"# SET_IDLE rejected (genuine DS3 STALLs this too): {ex.Message}"); }

// --- feature dumps -------------------------------------------------------------------------
Log("\n## Feature report dumps");
byte[] f01 = TryGetFeature(0x01, "identification");
// Keep the vendor prefix from the controller address, but redact its unique suffix.
TryGetFeature(0xF2, "BT address / firmware", 7, 8, 9);
// The paired host address is not needed for diagnosis.
TryGetFeature(0xF5, "host address", 2, 3, 4, 5, 6, 7);
byte[] efPlain = TryGetFeature(0xEF, "plain, no page select");
TryGetFeature(0xF8, "F8 plain");
TryGetFeature(0xF7, "F7 status");

var efPages = new Dictionary<byte, byte[]>();
for (int page = 0x00; page <= 0xF0; page += 0x10)
{
    var sel = new byte[48];
    sel[4] = 0x03;
    sel[5] = 0x01;
    sel[6] = (byte)page;
    try
    {
        SetFeature(0xEF, sel);
        byte[] r = GetFeature(0xEF);
        efPages[(byte)page] = r;
        Log($"SET 0xEF page 0x{page:X2} -> GET 0xEF len={r.Length}\n    {Hex(r)}");
    }
    catch (Exception ex)
    {
        Log($"SET/GET 0xEF page 0x{page:X2} FAILED: {ex.Message}");
    }
}

// re-select 0xA0 like the PS3 leaves it and read F8 afterwards
byte[] efA0 = [];
try
{
    var sel = new byte[48];
    sel[4] = 0x03;
    sel[5] = 0x01;
    sel[6] = 0xA0;
    SetFeature(0xEF, sel);
    efA0 = GetFeature(0xEF);
    TryGetFeature(0xF8, "F8 after 0xEF page 0xA0");
}
catch (Exception ex)
{
    Log($"re-select 0xA0 FAILED: {ex.Message}");
}

// --- decode identification + calibration ---------------------------------------------------
bool isSixaxisClass = false;
bool hasField7 = false;
if (f01.Length >= 0x2A)
{
    isSixaxisClass = f01[8] == 0x17;
    int numFields = f01[0x25];
    ReadOnlySpan<byte> fields = f01.AsSpan(0x26, Math.Min(numFields, f01.Length - 0x26));
    hasField7 = fields.IndexOf((byte)0x07) >= 0;
    Log($"\n## Identification: type bytes 8..11 = {Hex(f01.AsSpan(8, 4))} => {(isSixaxisClass ? "SIXAXIS-class (0x17)" : f01[8] == 0x18 ? "DualShock 3-class (0x18)" : "unknown")}; " +
        $"calibration fields ({numFields}) = {Hex(fields)}; field 0x07 (gyro cal byte in output) = {hasField7}");
}

// prefer the re-selected read, but fall back to the page captured during the sweep
if (!(efA0.Length >= 0x21 && efA0[1] == 0xEF) && efPages.TryGetValue(0xA0, out byte[]? sweptA0) && sweptA0.Length >= 0x21 && sweptA0[1] == 0xEF)
{
    Log("## using page 0xA0 as read during the sweep (the re-select failed)");
    efA0 = sweptA0;
}

(ushort v1, ushort v2)[] cal = new (ushort, ushort)[4];
bool haveCal = efA0.Length >= 0x21 && efA0[1] == 0xEF && efA0[7] == 0xA0;
if (haveCal)
{
    for (int i = 0; i < 4; i++)
    {
        cal[i] = (BinaryPrimitives.ReadUInt16BigEndian(efA0.AsSpan(0x11 + i * 4)), BinaryPrimitives.ReadUInt16BigEndian(efA0.AsSpan(0x13 + i * 4)));
    }

    Log($"## 0xEF page 0xA0 calibration (BE u16 at 0x11): X {cal[0].v1}/{cal[0].v2} (1g span {cal[0].v1 - cal[0].v2})  Y {cal[1].v1}/{cal[1].v2} ({cal[1].v1 - cal[1].v2})  Z {cal[2].v1}/{cal[2].v2} ({cal[2].v1 - cal[2].v2})  G {cal[3].v1}/{cal[3].v2}");
}
else
{
    Log("## 0xEF page 0xA0 not available/valid - page read failed or was not 0xA0; factory calibration not confirmed");
}

if (!stream)
{
    return 0;
}

// --- streaming -----------------------------------------------------------------------------
var streamInPipe = usb.Pipes.FirstOrDefault(p => (p.PipeId & 0x80) != 0);
if ((streamInPipe.PipeId & 0x80) == 0)
{
    Log("# no interrupt-IN pipe is available");
    return 3;
}

byte inPipe = streamInPipe.PipeId;
usb.SetPipeTimeout(inPipe, 500);

Log("\n## Enable streaming: SET Feature 0xF4 42 0C 00 00");
SetFeature(0xF4, [0x42, 0x0C, 0x00, 0x00]);

var gyroCal = new GyroCal();
byte calByte = 0;
if (haveCal)
{
    calByte = gyroCal.Initial(cal[3].v2, cal[3].v1);
    Log($"## InitialGyroCal(eepromCal={cal[3].v2}, eepromZero={cal[3].v1}) -> calByte 0x{calByte:X2}, zeroRef {gyroCal.ZeroRef}");
}

byte[] outReport = new byte[48];
// the cal byte currently on the wire (-1 = none sent); this, not the tracker's idea of it,
// is what the CSV records so the --calbyte plateaus are identifiable offline
int activeCalByte = -1;
void SendOutput(byte cb, bool withCal)
{
    Array.Clear(outReport);
    outReport[9] = 0x02; // LED 1
    outReport[25] = 0xFF; outReport[27] = 0x01; outReport[29] = 0x01;
    if (withCal)
    {
        // sixaxis.sys: if (!PLAIN_ZERO) out[3..4]; if (HW_CAL) out[5..6]  (see docs/MOTION.md)
        if (isSixaxisClass) { outReport[3] = 0xFF; outReport[4] = cb; }
        else { outReport[5] = 0xFF; outReport[6] = cb; }
    }

    activeCalByte = withCal ? cb : -1;
    SetOutput(outReport);
}

SendOutput(calByte, haveCal && (hasField7 || isSixaxisClass));
Log($"## Output report sent (cal bytes {(haveCal && (hasField7 || isSixaxisClass) ? $"0xFF 0x{calByte:X2} at [{(isSixaxisClass ? "3,4" : "5,6")}]" : "none")})");

using var csv = new StreamWriter(csvPath, false, new UTF8Encoding(false));
// calByte = byte currently in the output report (-1 if none), zeroRef = tracker state
csv.WriteLine("t_ms,rawX,rawY,rawZ,rawG,sixX,sixY,sixZ,sixG,calX,calY,calZ,calG,calByte,zeroRef");

int CalAccel(int raw, (ushort v1, ushort v2) c) => c.v1 == c.v2 ? raw : 113 * ((raw - c.v1) * 1024 / (c.v1 - c.v2)) / 1024 + 512;

var buf = new byte[64];
var sw = Stopwatch.StartNew();
List<(long t, int g)>? yawTrace = null;
double yawRest = 512;

(double x, double y, double z, double g, int n) Sample(double durationSec, bool print, bool applyRuntimeCal)
{
    double sx = 0, sy = 0, sz = 0, sg = 0;
    int n = 0;
    long lastPrint = 0;
    var start = sw.ElapsedMilliseconds;
    while (sw.ElapsedMilliseconds - start < durationSec * 1000)
    {
        int len;
        try { len = usb.ReadPipe(inPipe, buf); }
        catch (Exception ex) { Log($"read failed: {ex.Message}"); break; }
        if (len < 49 || buf[0] != 0x01) continue;

        int rx = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(41));
        int ry = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(43));
        int rz = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(45));
        int rg = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(47));

        // what DsHidMini's SIXAXIS.SYS mode exposes today
        int sxv = 0x3FF - rx, syv = ry, szv = rz, sgv = rg;

        int cx = rx, cy = ry, cz = rz, cg = rg;
        if (haveCal)
        {
            cx = CalAccel(rx, cal[0]);
            cy = CalAccel(ry, cal[1]);
            cz = CalAccel(rz, cal[2]);
            if (applyRuntimeCal)
            {
                cg = gyroCal.Runtime(rg, out byte newCal, out bool changed);
                if (changed && (hasField7 || isSixaxisClass))
                {
                    calByte = newCal;
                    SendOutput(calByte, true);
                    Log($"  [{sw.ElapsedMilliseconds,7} ms] RuntimeGyroCal changed cal byte -> 0x{calByte:X2} (zeroRef {gyroCal.ZeroRef})");
                }
            }
            else
            {
                cg = Math.Clamp(512 + cal[3].v1 - rg, 0, 1023);
            }
        }

        csv.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{sw.ElapsedMilliseconds},{rx},{ry},{rz},{rg},{sxv},{syv},{szv},{sgv},{cx},{cy},{cz},{cg},{activeCalByte},{gyroCal.ZeroRef}"));
        sx += rx; sy += ry; sz += rz; sg += rg; n++;
        yawTrace?.Add((sw.ElapsedMilliseconds, rg));

        if (print && sw.ElapsedMilliseconds - lastPrint >= 250)
        {
            lastPrint = sw.ElapsedMilliseconds;
            Console.WriteLine($"  raw X {rx,4} Y {ry,4} Z {rz,4} G {rg,4} | sixaxis.sys X {sxv,4} Y {syv,4} Z {szv,4} G {sgv,4} | cal X {cx,4} Y {cy,4} Z {cz,4} G {cg,4} | cal 0x{calByte:X2}");
        }
    }

    return n == 0 ? (0, 0, 0, 0, 0) : (sx / n, sy / n, sz / n, sg / n, n);
}

if (interactive)
{
    string[] orientations =
    [
        "flat on table, buttons up (Z = -1g expected)",
        "upside down, buttons facing table",
        "standing on its left grip (L side down)",
        "standing on its right grip (R side down)",
        "front edge down (triggers pointing to the table)",
        "back edge down (USB port up, triggers pointing at the ceiling)",
    ];
    Log("\n## Six orientations (2 s each)");
    foreach (string o in orientations)
    {
        Console.Write($"Place controller: {o}. Press Enter... ");
        Console.ReadLine();
        Sample(0.5, false, false); // flush
        var s = Sample(2.0, false, false);
        yawRest = s.g; // last orientation's resting gyro average
        Log($"  {o}: n={s.n} raw avg X {s.x:F1} Y {s.y:F1} Z {s.z:F1} G {s.g:F1}" +
            (haveCal && s.n > 0 ? $" | cal X {CalAccel((int)Math.Round(s.x), cal[0])} Y {CalAccel((int)Math.Round(s.y), cal[1])} Z {CalAccel((int)Math.Round(s.z), cal[2])}" : haveCal ? " | cal unavailable" : ""));
    }

    Console.Write("Now rotate the pad slowly around its vertical (yaw) axis for 5 s. Press Enter... ");
    Console.ReadLine();
    Log("## Yaw rotation sample (printing live)");
    yawTrace = [];
    var y = Sample(5.0, true, true);
    if (yawTrace.Count > 1)
    {
        // integrate (raw - rest) over time; positive/negative lobes tell the direction, the sum the total "counts*s"
        double rest = yawRest;
        double integral = 0, posPeak = 0, negPeak = 0;
        for (int i = 1; i < yawTrace.Count; i++)
        {
            double dt = (yawTrace[i].t - yawTrace[i - 1].t) / 1000.0;
            double v = yawTrace[i].g - rest;
            integral += v * dt;
            if (v > posPeak) posPeak = v;
            if (v < negPeak) negPeak = v;
        }

        Log($"  yaw: n={y.n} rest {rest:F1} raw min {yawTrace.Min(p => p.g)} max {yawTrace.Max(p => p.g)} peak {posPeak:+0;-0} / {negPeak:+0;-0} counts, integral {integral:F1} counts*s (divide by degrees turned for counts per deg/s)");
    }

    yawTrace = null;
}
else
{
    Log($"\n## Passive stream for {seconds} s (runtime gyro cal active)");
    var s = Sample(seconds, true, true);
    Log($"## resting avg over {s.n} reports: raw X {s.x:F1} Y {s.y:F1} Z {s.z:F1} G {s.g:F1}" +
        (haveCal && s.n > 0 ? $" | cal X {CalAccel((int)Math.Round(s.x), cal[0])} Y {CalAccel((int)Math.Round(s.y), cal[1])} Z {CalAccel((int)Math.Round(s.z), cal[2])} G(out) {gyroCal.Output} calByte 0x{gyroCal.CalByte:X2} zeroRef {gyroCal.ZeroRef}" : haveCal ? " | cal unavailable" : ""));
}

if (calByteTest && (hasField7 || isSixaxisClass))
{
    Log("\n## Cal-byte experiment: measure raw gyro shift per cal byte step (keep the pad still)");

    // Warm-up: the gyro zero of some pads drifts by 10-20 counts over the first ~20 s after
    // power-up (DS3-A1b did, see docs/MOTION.md), which corrupts a baseline taken too early.
    // Require three consecutive 1 s block averages within 1.0 count of each other (~3 s settled).
    SendOutput(calByte, true);
    Log("  waiting for the gyro zero to settle (3 consecutive 1 s blocks within 1.0 count, 60 s max) ...");
    var recent = new Queue<double>();
    var warm = Stopwatch.StartNew();
    bool settled = false;
    while (warm.Elapsed.TotalSeconds < 60)
    {
        var b = Sample(1.0, false, false);
        if (b.n == 0) break;
        recent.Enqueue(b.g);
        while (recent.Count > 3) recent.Dequeue();
        if (recent.Count == 3 && recent.Max() - recent.Min() <= 1.0) { settled = true; break; }
    }

    Log(settled
        ? $"  settled after {warm.Elapsed.TotalSeconds:F1} s, raw G {recent.Average():F1} (block spread {recent.Max() - recent.Min():F2})"
        : $"  NOT settled after {warm.Elapsed.TotalSeconds:F1} s (last blocks {string.Join(", ", recent.Select(v => v.ToString("F1")))}) - results below may be drift-contaminated");

    // Each non-zero step is measured against the most recent zero-delta reading, so a slow
    // residual drift shows up in the "0" rows instead of silently skewing the counts/step.
    int[] deltas = [0, +2, +4, 0, -2, -4, 0];
    double baseline = double.NaN, first = double.NaN;
    foreach (int d in deltas)
    {
        byte cb = (byte)(calByte + d);
        SendOutput(cb, true);
        Sample(0.5, false, false); // settle
        var s = Sample(1.5, false, false);
        if (d == 0)
        {
            if (double.IsNaN(first)) first = s.g;
            baseline = s.g;
            Log($"  calByte 0x{cb:X2} (0): raw G avg {s.g:F1} (n={s.n}) baseline (drift since first baseline {s.g - first:+0.0;-0.0})");
        }
        else
        {
            Log($"  calByte 0x{cb:X2} ({d:+0;-0}): raw G avg {s.g:F1} (n={s.n}) shift {s.g - baseline:+0.0;-0.0} => {(s.g - baseline) / d:F2} counts/step (sixaxis.sys/ds3cal: 26.4)");
        }
    }

    SendOutput(calByte, true);
}
else if (calByteTest)
{
    Log("\n## Cal-byte experiment skipped: unit reports no gyro cal field");
}

// --- shutdown mirroring the PS3 -------------------------------------------------------------
try
{
    Array.Clear(outReport);
    SetOutput(outReport);
    SetFeature(0xF4, [0x42, 0x0B, 0x00, 0x00]);
    Log("\n## Sent zero output report and SET Feature 0xF4 42 0B 00 00");
}
catch (Exception ex)
{
    Log($"shutdown failed: {ex.Message}");
}

Log($"# log: {logPath}\n# csv: {csvPath}");
return 0;
