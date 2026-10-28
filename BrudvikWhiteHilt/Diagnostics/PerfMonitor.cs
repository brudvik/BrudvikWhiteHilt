using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BrudvikWhiteHilt.Diagnostics;

/// <summary>
/// The whitehilt_perf console command: measures, while it runs, how much time the mod's own code takes each second and
/// how many bytes this machine sends and receives, per message.
/// </summary>
/// <remarks>
/// Nothing is measured until the command starts it. Then a second Harmony instance wraps every Harmony patch method
/// and every <c>Update</c>, <c>LateUpdate</c> and <c>FixedUpdate</c> of the mod with a timer, and patches the game's
/// connection and routed messages to count their sizes. <c>stop</c> removes all of it again. Times include the methods
/// a method calls; the JIT may inline a very small method into its caller, and then it does not show on its own.
/// <para>
/// The timer also counts the memory each method allocates, which the garbage collector must later clear and which
/// shows as stutter. The game's runtime counts the bytes each thread allocates where it can; where it reports nothing,
/// the growth of the whole heap is used, which only moves when a new block of memory is taken, so small allocations
/// show late and in lumps, and a collection during the method makes it shrink (that call then counts as nothing).
/// </para>
/// </remarks>
public static class PerfMonitor
{
    private const string HarmonyId = BrudvikWhiteHilt.PluginGUID + ".perf";
    private const int DefaultRows = 15;

    // Routed message header: message id, sender, target, ZDOID (long + uint), method hash and the package length.
    private const int RoutedHeaderBytes = 8 + 8 + 8 + 12 + 4 + 4;

    private static readonly string[] connectionNames =
    {
        "ZDOData", "RoutedRPC", "PeerInfo", "ServerHandshake", "ClientHandshake", "Disconnect", "Error", "Kicked",
        "SavePlayerProfile", "NetTime", "PlayerList", "RefPos", "CharacterID", "AdminList", "RemotePrint"
    };

    private static readonly Dictionary<MethodBase, Timing> timings = new();
    private static readonly Dictionary<string, Traffic> traffic = new();
    private static readonly Dictionary<int, string> names = new();

    // The runtime's count of bytes allocated on this thread, if it has one that works; else the heap's size.
    private static Func<long> allocated;
    private static bool exactAllocations;

    private static Harmony harmony;
    private static float startedAt;
    private static int collectionsAtStart;
    private static long allocatedAtStart;
    private static int patchedMethods;
    private static bool logging;
    private static float nextLog;

    /// <summary>Whether a measurement is running.</summary>
    public static bool Running => harmony != null;

    /// <summary>
    /// Registers the console command. Call from the plugin's Awake.
    /// </summary>
    public static void Initialize()
    {
        CommandManager.Instance.AddConsoleCommand(new PerfCommand());
    }

    /// <summary>
    /// Starts measuring, or starts again from zero.
    /// </summary>
    /// <returns>What happened, for the console.</returns>
    public static string Start()
    {
        Stop();
        timings.Clear();
        traffic.Clear();
        SeedNames();
        ChooseAllocationCounter();
        harmony = new Harmony(HarmonyId);
        patchedMethods = 0;
        HarmonyMethod begin = new(AccessTools.Method(typeof(PerfMonitor), nameof(Begin)));
        HarmonyMethod end = new(AccessTools.Method(typeof(PerfMonitor), nameof(End)));
        foreach (MethodInfo method in OwnMethods())
        {
            try
            {
                harmony.Patch(method, prefix: begin, postfix: end);
                patchedMethods++;
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Perf: could not time {Describe(method)}: {ex.Message}");
            }
        }

        PatchNetwork();
        ResetWindow(Time.realtimeSinceStartup);
        return $"Measuring {patchedMethods} methods and the network. 'whitehilt_perf report' shows the numbers, 'whitehilt_perf stop' ends it.";
    }

    /// <summary>
    /// Writes a report to the log every <c>[Diagnostics] PerfLogMinutes</c> minutes while in a world, measuring from
    /// zero after each; a dedicated server has no console for the command. Call every frame.
    /// </summary>
    public static void Tick()
    {
        int minutes = PerformanceSettings.PerfLogMinutes?.Value ?? 0;
        if (minutes <= 0 || ZNet.instance == null)
        {
            if (logging && minutes <= 0)
            {
                logging = false;
                Stop();
            }

            return;
        }

        float now = Time.realtimeSinceStartup;
        if (!logging || !Running)
        {
            logging = true;
            if (!Running)
            {
                Jotunn.Logger.LogInfo(Start());
            }

            nextLog = now + minutes * 60f;
            return;
        }

        if (now < nextLog)
        {
            return;
        }

        Jotunn.Logger.LogInfo(Report(DefaultRows));
        timings.Clear();
        traffic.Clear();
        ResetWindow(now);
        nextLog = now + minutes * 60f;
    }

    /// <summary>
    /// Stops measuring and removes every patch it added.
    /// </summary>
    public static void Stop()
    {
        if (harmony == null)
        {
            return;
        }

        harmony.UnpatchSelf();
        harmony = null;
    }

    /// <summary>
    /// The numbers so far: the mod's slowest methods, then the messages by bytes.
    /// </summary>
    /// <param name="rows">Most rows in each list.</param>
    /// <returns>The report, one line per row.</returns>
    public static string Report(int rows)
    {
        float seconds = Mathf.Max(0.001f, Time.realtimeSinceStartup - startedAt);
        StringBuilder text = new();
        double totalMs = timings.Values.Sum(timing => timing.Ticks) * 1000.0 / Stopwatch.Frequency;
        text.AppendLine(Invariant($"White Hilt over {seconds:0} s: {totalMs / seconds:0.00} ms of the mod's code per second ({patchedMethods} methods timed, times include calls)"));
        text.AppendLine(MemoryLine(seconds));
        text.AppendLine("  ms/s     calls/s   avg us   max us   alloc KB/s   method");
        foreach (KeyValuePair<MethodBase, Timing> row in timings.OrderByDescending(pair => pair.Value.Ticks).Take(rows))
        {
            Timing timing = row.Value;
            double ms = timing.Ticks * 1000.0 / Stopwatch.Frequency;
            double average = timing.Calls > 0 ? ms * 1000.0 / timing.Calls : 0.0;
            double max = timing.MaxTicks * 1000000.0 / Stopwatch.Frequency;
            double alloc = timing.Bytes / 1024.0 / seconds;
            text.AppendLine(Invariant($"  {ms / seconds,7:0.000}  {timing.Calls / seconds,8:0.0}  {average,7:0.0}  {max,7:0}  {alloc,10:0.0}   {Describe(row.Key)}"));
        }

        long sent = traffic.Values.Sum(item => item.SentBytes);
        long received = traffic.Values.Sum(item => item.ReceivedBytes);
        text.AppendLine(Invariant($"Network: {sent / seconds / 1024f:0.0} KB/s out, {received / seconds / 1024f:0.0} KB/s in (connection messages; routed ones are also counted on their own)"));
        text.AppendLine("  out B/s   out/s    in B/s    in/s    message");
        foreach (KeyValuePair<string, Traffic> row in traffic.OrderByDescending(pair => pair.Value.SentBytes + pair.Value.ReceivedBytes).Take(rows))
        {
            Traffic item = row.Value;
            text.AppendLine(Invariant($"  {item.SentBytes / seconds,8:0}  {item.SentCount / seconds,6:0.0}  {item.ReceivedBytes / seconds,8:0}  {item.ReceivedCount / seconds,6:0.0}   {row.Key}"));
        }

        return text.ToString();
    }

    // Garbage collections since the start, and with an exact counter, all this thread allocated: the whole game's
    // main thread, to set the mod's share against.
    private static string MemoryLine(float seconds)
    {
        int collections = GC.CollectionCount(0) - collectionsAtStart;
        string line = Invariant($"Memory: {collections} garbage collections ({collections * 60f / seconds:0.0} a minute)");
        if (exactAllocations)
        {
            double all = (allocated() - allocatedAtStart) / 1024.0 / seconds;
            return line + Invariant($", {all:0.0} KB/s allocated by the whole main thread; alloc is the bytes each method allocated, calls included");
        }

        return line + "; alloc is the heap's growth during each method, rough: it moves in blocks and leaves out calls a collection ran in";
    }

    private static void ResetWindow(float now)
    {
        startedAt = now;
        collectionsAtStart = GC.CollectionCount(0);
        allocatedAtStart = allocated?.Invoke() ?? 0L;
    }

    // Uses the runtime's per-thread count of allocated bytes when it has one and it moves when memory is allocated; the
    // game's runtime may have the method and still always answer nothing.
    private static void ChooseAllocationCounter()
    {
        if (allocated != null)
        {
            return;
        }

        MethodInfo method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, Type.EmptyTypes, null);
        if (method != null && method.ReturnType == typeof(long))
        {
            try
            {
                Func<long> perThread = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
                long before = perThread();
                byte[] probe = new byte[64 * 1024];
                long after = perThread();
                GC.KeepAlive(probe);
                if (after - before >= probe.Length)
                {
                    allocated = perThread;
                    exactAllocations = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogDebug($"Perf: the per-thread allocation count does not work here: {ex.Message}");
            }
        }

        allocated = () => GC.GetTotalMemory(false);
        exactAllocations = false;
    }

    // The mod's Harmony patch methods and Unity update methods: where its per-frame time goes.
    private static IEnumerable<MethodInfo> OwnMethods()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(PerfMonitor).Assembly))
        {
            if (type == typeof(PerfMonitor) || type.IsGenericTypeDefinition || type.ContainsGenericParameters)
            {
                continue;
            }

            bool behaviour = typeof(MonoBehaviour).IsAssignableFrom(type);
            bool patchClass = type.GetCustomAttributes(typeof(HarmonyPatch), false).Length > 0;
            if (!behaviour && !patchClass)
            {
                continue;
            }

            foreach (MethodInfo method in type.GetMethods(all))
            {
                if (method.IsAbstract || method.IsGenericMethodDefinition || method.GetMethodBody() == null)
                {
                    continue;
                }

                if ((behaviour && !method.IsStatic && method.Name is "Update" or "LateUpdate" or "FixedUpdate" && method.GetParameters().Length == 0)
                    || (patchClass && method.IsStatic && IsRuntimePatch(method)))
                {
                    yield return method;
                }
            }
        }
    }

    // Prefixes, postfixes and finalizers run while the game plays; transpilers only once, when patching.
    private static bool IsRuntimePatch(MethodInfo method)
    {
        return method.GetCustomAttributes(typeof(HarmonyPrefix), false).Length > 0
            || method.GetCustomAttributes(typeof(HarmonyPostfix), false).Length > 0
            || method.GetCustomAttributes(typeof(HarmonyFinalizer), false).Length > 0
            || method.Name is "Prefix" or "Postfix" or "Finalizer";
    }

    private static void PatchNetwork()
    {
        Type rpc = typeof(ZRpc);
        Type routed = typeof(ZRoutedRpc);
        TryPatch(AccessTools.Method(rpc, nameof(ZRpc.Invoke), new[] { typeof(string), typeof(object[]) }),
            nameof(ConnectionSendPrefix), nameof(ConnectionSendPostfix));
        TryPatch(AccessTools.Method(rpc, "HandlePackage"), nameof(ConnectionReceivePrefix), null);
        TryPatch(AccessTools.Method(routed, nameof(ZRoutedRpc.InvokeRoutedRPC), new[] { typeof(long), typeof(ZDOID), typeof(string), typeof(object[]) }),
            nameof(RoutedNamePrefix), null);
        TryPatch(AccessTools.Method(routed, "RouteRPC"), nameof(RoutedSendPrefix), null);
        TryPatch(AccessTools.Method(routed, "HandleRoutedRPC"), nameof(RoutedReceivePrefix), null);
    }

    private static void TryPatch(MethodInfo target, string prefix, string postfix)
    {
        if (target == null)
        {
            Jotunn.Logger.LogWarning($"Perf: a network method to count ({prefix}) was not found; that part is left out.");
            return;
        }

        harmony.Patch(target,
            prefix: new HarmonyMethod(AccessTools.Method(typeof(PerfMonitor), prefix)),
            postfix: postfix != null ? new HarmonyMethod(AccessTools.Method(typeof(PerfMonitor), postfix)) : null);
    }

    // Names come from the game's connection messages and every string constant in the mod (its RPC names among them);
    // other names are learned as this machine sends them.
    private static void SeedNames()
    {
        foreach (string name in connectionNames)
        {
            names[name.GetStableHashCode()] = name;
        }

        foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(PerfMonitor).Assembly))
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (field.IsLiteral && field.FieldType == typeof(string) && field.GetRawConstantValue() is string value && value.Length > 0)
                {
                    names[value.GetStableHashCode()] = value;
                }
            }
        }
    }

    private static string NameOf(int hash, string kind)
    {
        return names.TryGetValue(hash, out string name) ? $"{kind} {name}" : $"{kind} #{hash}";
    }

    private static Traffic Count(string key)
    {
        if (!traffic.TryGetValue(key, out Traffic item))
        {
            item = new Traffic();
            traffic[key] = item;
        }

        return item;
    }

    // The allocation count is read before the clock starts and after it stops, so its own cost is not timed.
    private static void Begin(out Mark __state)
    {
        __state.Bytes = allocated();
        __state.Ticks = Stopwatch.GetTimestamp();
    }

    private static void End(Mark __state, MethodBase __originalMethod)
    {
        long ticks = Stopwatch.GetTimestamp() - __state.Ticks;
        long bytes = allocated() - __state.Bytes;
        if (!timings.TryGetValue(__originalMethod, out Timing timing))
        {
            timing = new Timing();
            timings[__originalMethod] = timing;
        }

        timing.Calls++;
        timing.Ticks += ticks;
        timing.MaxTicks = Math.Max(timing.MaxTicks, ticks);

        // Less than before only when a collection ran during the call.
        if (bytes > 0)
        {
            timing.Bytes += bytes;
        }
    }

    private static void ConnectionSendPrefix(ZRpc __instance, string method, out bool __state)
    {
        __state = __instance.IsConnected();
        if (__state)
        {
            names[method.GetStableHashCode()] = method;
        }
    }

    private static void ConnectionSendPostfix(ZRpc __instance, string method, bool __state)
    {
        if (__state)
        {
            Traffic item = Count("net " + method);
            item.SentCount++;
            item.SentBytes += __instance.m_pkg.Size();
        }
    }

    private static void ConnectionReceivePrefix(ZPackage package)
    {
        int position = package.GetPos();
        int hash = package.ReadInt();
        package.SetPos(position);
        Traffic item = Count(NameOf(hash, "net"));
        item.ReceivedCount++;
        item.ReceivedBytes += package.Size();
    }

    private static void RoutedNamePrefix(string methodName)
    {
        names[methodName.GetStableHashCode()] = methodName;
    }

    // Counts what leaves this machine for each routed message: the server passes one to every other peer, a client
    // only to the server.
    private static void RoutedSendPrefix(ZRoutedRpc __instance, ZRoutedRpc.RoutedRPCData rpcData)
    {
        int copies = 0;
        foreach (ZNetPeer peer in __instance.m_peers)
        {
            if (peer.IsReady() && (!__instance.m_server || rpcData.m_targetPeerID == 0L
                ? peer.m_uid != rpcData.m_senderPeerID
                : peer.m_uid == rpcData.m_targetPeerID))
            {
                copies++;
            }
        }

        Traffic item = Count(NameOf(rpcData.m_methodHash, "rpc"));
        item.SentCount += copies;
        item.SentBytes += (long)copies * (rpcData.m_parameters.Size() + RoutedHeaderBytes);
    }

    private static void RoutedReceivePrefix(ZRoutedRpc.RoutedRPCData data)
    {
        Traffic item = Count(NameOf(data.m_methodHash, "rpc"));
        item.ReceivedCount++;
        item.ReceivedBytes += data.m_parameters.Size() + RoutedHeaderBytes;
    }

    private static string Describe(MethodBase method)
    {
        return $"{method.DeclaringType?.Name}.{method.Name}";
    }

    private static string Invariant(FormattableString text)
    {
        return text.ToString(CultureInfo.InvariantCulture);
    }

    // What Begin noted for End.
    private struct Mark
    {
        public long Ticks;
        public long Bytes;
    }

    private sealed class Timing
    {
        public long Calls;
        public long Ticks;
        public long MaxTicks;
        public long Bytes;
    }

    private sealed class Traffic
    {
        public long SentCount;
        public long SentBytes;
        public long ReceivedCount;
        public long ReceivedBytes;
    }

    private sealed class PerfCommand : ConsoleCommand
    {
        public override string Name => "whitehilt_perf";

        public override string Help => "[start|report [rows]|stop] Measures the mod's time per frame and the network bytes per message.";

        /// <summary>
        /// Starts, reports or stops a measurement; a console command.
        /// </summary>
        public override void Run(string[] args)
        {
            string action = args.Length > 0 ? args[0].ToLowerInvariant() : Running ? "report" : "start";
            int rows = args.Length > 1 && int.TryParse(args[1], out int asked) ? Mathf.Clamp(asked, 1, 100) : DefaultRows;
            switch (action)
            {
                case "start":
                    Print(Start());
                    break;
                case "report" when Running:
                    Print(Report(rows));
                    break;
                case "stop" when Running:
                    string report = Report(rows);
                    Stop();
                    Print(report + "Stopped.");
                    break;
                default:
                    Print("Not measuring. 'whitehilt_perf start' begins.");
                    break;
            }
        }

        // The console shows the lines; the log keeps the whole report to read later.
        private static void Print(string text)
        {
            Jotunn.Logger.LogInfo(text);
            foreach (string line in text.Split('\n'))
            {
                global::Console.instance?.Print(line.TrimEnd('\r'));
            }
        }
    }
}
