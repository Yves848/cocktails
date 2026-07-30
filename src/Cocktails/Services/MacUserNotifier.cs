using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Cocktails.Services;

/// <summary>
/// Notifications système natives via <c>UNUserNotificationCenter</c> (interop
/// Objective-C). Attribue la notification à l'app (nom + icône Cocktails), contrairement
/// à osascript. <b>Ne fonctionne que dans un bundle .app</b> ayant un bundle id — sinon
/// <c>currentNotificationCenter</c> lève une exception ObjC ; on ne l'instancie donc que
/// dans ce contexte (cf. <see cref="PlatformNotifier"/>).
/// </summary>
public sealed class MacUserNotifier : INotifier
{
    private const string Objc = "/usr/lib/libobjc.A.dylib";

    [DllImport(Objc, EntryPoint = "objc_getClass")] private static extern IntPtr GetClass(string name);
    [DllImport(Objc, EntryPoint = "sel_registerName")] private static extern IntPtr Sel(string name);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr sel);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern IntPtr Send(IntPtr receiver, IntPtr sel, IntPtr a);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern IntPtr SendStr(IntPtr receiver, IntPtr sel, [MarshalAs(UnmanagedType.LPUTF8Str)] string a);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern void SendAuth(IntPtr receiver, IntPtr sel, UIntPtr options, IntPtr block);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern IntPtr Send3(IntPtr receiver, IntPtr sel, IntPtr a, IntPtr b, IntPtr c);
    [DllImport(Objc, EntryPoint = "objc_msgSend")] private static extern void Send2(IntPtr receiver, IntPtr sel, IntPtr a, IntPtr b);

    // Bloc de complétion (BOOL granted, NSError* error) — requis par requestAuthorization.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AuthCallback(IntPtr block, byte granted, IntPtr error);
    private static readonly AuthCallback KeepAlive = static (_, _, _) => { };

    private static readonly object Gate = new();
    private static IntPtr _authBlock;
    private static bool _blockReady;

    private static IntPtr NSString(string s)
        => SendStr(GetClass("NSString"), Sel("stringWithUTF8String:"), s);

    public Task NotifyAsync(string title, string message)
    {
        try
        {
            Post(title, message);
        }
        catch (Exception)
        {
            // Une notification qui échoue ne doit jamais casser le monitoring.
        }

        return Task.CompletedTask;
    }

    private static void Post(string title, string message)
    {
        var centerClass = GetClass("UNUserNotificationCenter");
        if (centerClass == IntPtr.Zero)
        {
            return;
        }

        var center = Send(centerClass, Sel("currentNotificationCenter"));
        if (center == IntPtr.Zero)
        {
            return;
        }

        // Autorisation (Badge=1 | Sound=2 | Alert=4 → 7) avec un bloc de complétion valide.
        SendAuth(center, Sel("requestAuthorizationWithOptions:completionHandler:"), (UIntPtr)7, AuthBlock());

        var content = Send(Send(GetClass("UNMutableNotificationContent"), Sel("alloc")), Sel("init"));
        Send(content, Sel("setTitle:"), NSString(title));
        Send(content, Sel("setBody:"), NSString(message));

        var identifier = NSString(Guid.NewGuid().ToString("N"));
        var request = Send3(
            GetClass("UNNotificationRequest"),
            Sel("requestWithIdentifier:content:trigger:"),
            identifier, content, IntPtr.Zero);

        // Handler nil autorisé pour l'ajout.
        Send2(center, Sel("addNotificationRequest:withCompletionHandler:"), request, IntPtr.Zero);
    }

    /// <summary>Construit (une fois) un bloc global Objective-C encapsulant le callback.</summary>
    private static IntPtr AuthBlock()
    {
        lock (Gate)
        {
            if (_blockReady)
            {
                return _authBlock;
            }

            var libSystem = NativeLibrary.Load("/usr/lib/libSystem.dylib");
            var isa = NativeLibrary.GetExport(libSystem, "_NSConcreteGlobalBlock");
            var invoke = Marshal.GetFunctionPointerForDelegate(KeepAlive);

            // Block_descriptor { unsigned long reserved; unsigned long size; }
            var descriptor = Marshal.AllocHGlobal(16);
            Marshal.WriteInt64(descriptor, 0, 0);
            Marshal.WriteInt64(descriptor, 8, 32);   // taille du Block_literal

            // Block_literal { void* isa; int flags; int reserved; void* invoke; void* descriptor; }
            var block = Marshal.AllocHGlobal(32);
            Marshal.WriteIntPtr(block, 0, isa);
            Marshal.WriteInt32(block, 8, 1 << 28);    // BLOCK_IS_GLOBAL
            Marshal.WriteInt32(block, 12, 0);
            Marshal.WriteIntPtr(block, 16, invoke);
            Marshal.WriteIntPtr(block, 24, descriptor);

            _authBlock = block;
            _blockReady = true;
            return _authBlock;
        }
    }
}
