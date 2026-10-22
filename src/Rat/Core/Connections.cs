using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace Rat;

public sealed record Connection(int Pid, string ProcessName, string? ProcessPath, IPAddress Remote, ushort RemotePort, IPAddress Local, ushort LocalPort, string State);

public sealed class ProcessCache
{
    readonly Dictionary<int, (string Name, string? Path)> _cache = new();

    public (string Name, string? Path) Get(int pid)
    {
        if (_cache.TryGetValue(pid, out var v)) return v;
        string name = pid <= 4 ? "System" : $"pid {pid}";
        string? path = null;
        try
        {
            using var p = Process.GetProcessById(pid);
            name = p.ProcessName;
            path = p.MainModule?.FileName;
        }
        catch { }
        _cache[pid] = (name, path);
        return (name, path);
    }
}

public static class Connections
{
    const uint AF_INET = 2;
    const uint TCP_TABLE_OWNER_MODULE_ALL = 4;
    const ushort TCP_STATE_SYN_SENT = 3;
    const ushort TCP_STATE_ESTABLISHED = 5;
    const ushort TCP_STATE_DELETE_TCB = 12;

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCPROW_OWNER_PID
    {
        public uint State, LocalAddr, LocalPort, RemoteAddr, RemotePort, OwningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MIB_TCPROW
    {
        public uint State, LocalAddr, LocalPort, RemoteAddr, RemotePort;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref uint dwOutBufLen, bool sort, uint ipVersion, uint tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    static extern uint SetTcpEntry(ref MIB_TCPROW row);

    public static List<Connection> Snapshot(ProcessCache cache)
    {
        var result = new List<Connection>();
        uint size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, true, AF_INET, TCP_TABLE_OWNER_MODULE_ALL, 0);
        if (size == 0) return result;
        var buf = Marshal.AllocHGlobal((IntPtr)size);
        try
        {
            if (GetExtendedTcpTable(buf, ref size, true, AF_INET, TCP_TABLE_OWNER_MODULE_ALL, 0) != 0) return result;
            int count = Marshal.ReadInt32(buf);
            int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
            for (int i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>((IntPtr)(buf.ToInt64() + 4 + (long)i * rowSize));
                if (row.State is not (TCP_STATE_ESTABLISHED or TCP_STATE_SYN_SENT)) continue;
                var remote = new IPAddress(row.RemoteAddr);
                var local = new IPAddress(row.LocalAddr);
                if (IPAddress.IsLoopback(remote) || IPAddress.Any.Equals(remote)) continue;
                var info = cache.Get((int)row.OwningPid);
                result.Add(new Connection((int)row.OwningPid, info.Name, info.Path, remote, ReadPort(row.RemotePort), local, ReadPort(row.LocalPort),
                    row.State == TCP_STATE_ESTABLISHED ? "Established" : "Connecting"));
            }
        }
        finally { Marshal.FreeHGlobal(buf); }
        return result;
    }

    /// <summary>Cuts an established IPv4 connection (DELETE_TCB). Needs admin; fails silently otherwise.</summary>
    public static void Close(Connection c)
    {
        var row = new MIB_TCPROW
        {
            State = TCP_STATE_DELETE_TCB,
            LocalAddr = ToRaw(c.Local),
            LocalPort = ToRawPort(c.LocalPort),
            RemoteAddr = ToRaw(c.Remote),
            RemotePort = ToRawPort(c.RemotePort),
        };
        SetTcpEntry(ref row);
    }

    static ushort ReadPort(uint rawPort) => (ushort)(((rawPort & 0xff) << 8) | ((rawPort >> 8) & 0xff));

    static uint ToRawPort(ushort port) => (uint)(((port & 0xff) << 8) | (port >> 8));

    static uint ToRaw(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b[0] | ((uint)b[1] << 8) | ((uint)b[2] << 16) | ((uint)b[3] << 24);
    }
}
