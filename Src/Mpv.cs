// 2022 @ cpasjuste

using System;
using System.Runtime.InteropServices;
using System.Text;
using PlatformID = Noesis.PlatformID;

namespace NoesisApp
{
    public static class Mpv
    {
        public static string LibPath = string.Empty;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string strLib);

        [DllImport("kernel32.dll")]
        private static extern int FreeLibrary(IntPtr iModule);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        private static extern IntPtr GetProcAddress(IntPtr iModule, string strProcName);

        [DllImport("libdl")]
        private static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl")]
        private static extern int dlclose(IntPtr handle);

        static Mpv()
        {
            // windows: https://sourceforge.net/projects/mpv-player-windows/files/libmpv/
            // linux: install libmpv...
            var path = string.IsNullOrEmpty(LibPath) ? Noesis.Platform.ID == PlatformID.Windows ? "mpv-2.dll" : "libmpv.so.1" : LibPath;
            var handle = LoadLib(path);

            Create = (MpvCreateFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_create"), typeof(MpvCreateFn));
            CreateClient = (MpvCreateClientFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_create_client"), typeof(MpvCreateClientFn));
            Initialize = (MpvInitializeFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_initialize"), typeof(MpvInitializeFn));
            Destroy = (MpvDestroyFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_destroy"), typeof(MpvDestroyFn));
            MpvCommand = (MpvCommandFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_command"), typeof(MpvCommandFn));
            MpvCommandString = (MpvCommandStringFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_command_string"), typeof(MpvCommandStringFn));
            ErrorString = (MpvErrorStringFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_error_string"), typeof(MpvErrorStringFn));
            MpvGetProperty = (MpvGetPropertyFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_get_property"), typeof(MpvGetPropertyFn));
            MpvGetPropertyDouble = (MpvGetPropertyDoubleFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_get_property"), typeof(MpvGetPropertyDoubleFn));
            MpvSetProperty = (MpvSetPropertyFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_set_property"), typeof(MpvSetPropertyFn));
            MpvSetPropertyLong = (MpvSetPropertyLongFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_set_property"), typeof(MpvSetPropertyLongFn));
            MpvSetPropertyDouble = (MpvSetPropertyDoubleFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_set_property"), typeof(MpvSetPropertyDoubleFn));
            Free = (MpvFreeFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_free"), typeof(MpvFreeFn));
            WaitEvent = (MpvWaitEventFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_wait_event"), typeof(MpvWaitEventFn));
            RenderContextCreate = (MpvRenderContextCreateFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_render_context_create"), typeof(MpvRenderContextCreateFn));
            RenderContextUpdate = (MpvRenderContextUpdateFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_render_context_update"), typeof(MpvRenderContextUpdateFn));
            RenderContextRender = (MpvRenderContextRenderFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_render_context_render"), typeof(MpvRenderContextRenderFn));
            RenderContextFree = (MpvRenderContextFreeFn)Marshal.GetDelegateForFunctionPointer(GetProc(handle, "mpv_render_context_free"), typeof(MpvRenderContextFreeFn));
        }

        private static IntPtr LoadLib(string name)
        {
            var handle = Noesis.Platform.ID == PlatformID.Windows ? LoadLibrary(name) : dlopen(name, 2);
            if (handle == IntPtr.Zero)
                throw new Exception($"mpv: failed to load dll \"{name}\"");

            return handle;
        }

        private static IntPtr GetProc(IntPtr iModule, string strProcName)
        {
            var ptr = Noesis.Platform.ID == PlatformID.Windows ? GetProcAddress(iModule, strProcName) : dlsym(iModule, strProcName);
            if (ptr == IntPtr.Zero)
                throw new Exception($"mpv: failed to load function \"{strProcName}\"");

            return ptr;
        }

        #region imports

        public delegate IntPtr MpvCreateFn();
        public static MpvCreateFn Create;

        public delegate IntPtr MpvCreateClientFn(IntPtr mpvHandle, [MarshalAs(UnmanagedType.AnsiBStr)] string command);
        public static MpvCreateClientFn CreateClient;

        public delegate MpvError MpvInitializeFn(IntPtr mpvHandle);
        public static MpvInitializeFn Initialize;

        public delegate void MpvDestroyFn(IntPtr mpvHandle);
        public static MpvDestroyFn Destroy;

        public delegate MpvError MpvCommandFn(IntPtr mpvHandle, IntPtr strings);
        private static readonly MpvCommandFn MpvCommand;

        public delegate MpvError MpvCommandStringFn(IntPtr mpvHandle, [MarshalAs(UnmanagedType.AnsiBStr)] string command);
        private static readonly MpvCommandStringFn MpvCommandString;

        public delegate IntPtr MpvErrorStringFn(MpvError error);
        public static MpvErrorStringFn ErrorString;

        public delegate MpvError MpvGetPropertyFn(IntPtr mpvHandle, byte[] name, MpvFormat format, out IntPtr data);
        private static readonly MpvGetPropertyFn MpvGetProperty;

        public delegate MpvError MpvGetPropertyDoubleFn(IntPtr mpvHandle, byte[] name, MpvFormat format, out double data);
        private static readonly MpvGetPropertyDoubleFn MpvGetPropertyDouble;

        public delegate MpvError MpvSetPropertyFn(IntPtr mpvHandle, byte[] name, MpvFormat format, ref byte[] data);
        private static readonly MpvSetPropertyFn MpvSetProperty;

        public delegate MpvError MpvSetPropertyLongFn(IntPtr mpvHandle, byte[] name, MpvFormat format, ref long data);
        private static readonly MpvSetPropertyLongFn MpvSetPropertyLong;

        public delegate MpvError MpvSetPropertyDoubleFn(IntPtr mpvHandle, byte[] name, MpvFormat format, ref double data);
        private static readonly MpvSetPropertyDoubleFn MpvSetPropertyDouble;

        public delegate void MpvFreeFn(IntPtr data);
        public static MpvFreeFn Free;

        public delegate IntPtr MpvWaitEventFn(IntPtr mpvHandle, double timeout);
        public static MpvWaitEventFn WaitEvent;

        public delegate MpvError MpvRenderContextCreateFn(out IntPtr renderContextHandle, IntPtr mpvHandle, MpvRenderParam[] p);
        public static MpvRenderContextCreateFn RenderContextCreate;

        public delegate MpvRenderContextFlag MpvRenderContextUpdateFn(IntPtr renderContextHandle);
        public static MpvRenderContextUpdateFn RenderContextUpdate;

        public delegate MpvError MpvRenderContextRenderFn(IntPtr renderContextHandle, MpvRenderParam[] p);
        public static MpvRenderContextRenderFn RenderContextRender;

        public delegate void MpvRenderContextFreeFn(IntPtr renderContextHandle);
        public static MpvRenderContextFreeFn RenderContextFree;

        public enum MpvError
        {
            MpvErrorSuccess = 0,
            MpvErrorEventQueueFull = -1,
            MpvErrorNoMem = -2,
            MpvErrorUninitialized = -3,
            MpvErrorInvalidParameter = -4,
            MpvErrorOptionNotFound = -5,
            MpvErrorOptionFormat = -6,
            MpvErrorOptionError = -7,
            MpvErrorPropertyNotFound = -8,
            MpvErrorPropertyFormat = -9,
            MpvErrorPropertyUnavailable = -10,
            MpvErrorPropertyError = -11,
            MpvErrorCommand = -12,
            MpvErrorLoadingFailed = -13,
            MpvErrorAoInitFailed = -14,
            MpvErrorVoInitFailed = -15,
            MpvErrorNothingToPlay = -16,
            MpvErrorUnknownFormat = -17,
            MpvErrorUnsupported = -18,
            MpvErrorNotImplemented = -19,
            MpvErrorGeneric = -20
        }

        public enum MpvEventId
        {
            MpvEventNone = 0,
            MpvEventShutdown = 1,
            MpvEventLogMessage = 2,
            MpvEventGetPropertyReply = 3,
            MpvEventSetPropertyReply = 4,
            MpvEventCommandReply = 5,
            MpvEventStartFile = 6,
            MpvEventEndFile = 7,
            MpvEventFileLoaded = 8,
            MpvEventIdle = 11, //deprecated
            MpvEventTick = 14, //deprecated
            MpvEventScriptInputDispatch = 15,
            MpvEventClientMessage = 16,
            MpvEventVideoReConfig = 17,
            MpvEventAudioReConfig = 18,
            MpvEventSeek = 20,
            MpvEventPlaybackRestart = 21,
            MpvEventPropertyChange = 22,
            MpvEventQueueOverflow = 24,
            MpvEventHook = 25
        }

        public enum MpvFormat
        {
            MpvFormatNone = 0,
            MpvFormatString = 1,
            MpvFormatOsdString = 2,
            MpvFormatFlag = 3,
            MpvFormatInt64 = 4,
            MpvFormatDouble = 5,
            MpvFormatNode = 6,
            MpvFormatNodeArray = 7,
            MpvFormatNodeMap = 8,
            MpvFormatByteArray = 9
        }

        public enum MpvLogLevel
        {
            MpvLogLevelNone = 0,
            MpvLogLevelFatal = 10,
            MpvLogLevelError = 20,
            MpvLogLevelWarn = 30,
            MpvLogLevelInfo = 40,
            MpvLogLevelV = 50,
            MpvLogLevelDebug = 60,
            MpvLogLevelTrace = 70,
        }

        public enum MpvEndFileReason
        {
            MpvEndFileReasonEof = 0,
            MpvEndFileReasonStop = 2,
            MpvEndFileReasonQuit = 3,
            MpvEndFileReasonError = 4,
            MpvEndFileReasonRedirect = 5
        }

        public enum MpvRenderParamType
        {
            MpvRenderParamInvalid = 0,
            MpvRenderParamApiType = 1,
            MpvRenderParamOpenGlInitParams = 2,
            MpvRenderParamOpenGlFbo = 3,
            MpvRenderParamFlipY = 4,
            MpvRenderParamDepth = 5,
            MpvRenderParamIccProfile = 6,
            MpvRenderParamAmbientLight = 7,
            MpvRenderParamX11Display = 8,
            MpvRenderParamWlDisplay = 9,
            MpvRenderParamAdvancedControl = 10,
            MpvRenderParamNextFrameInfo = 11,
            MpvRenderParamBlockForTargetTime = 12,
            MpvRenderParamSkipRendering = 13,
            MpvRenderParamDrmDisplay = 14,
            MpvRenderParamDrmDrawSurfaceSize = 15,
            MpvRenderParamDrmDisplayV2 = 16,
            MpvRenderParamSwSize = 17,
            MpvRenderParamSwFormat = 18,
            MpvRenderParamSwStride = 19,
            MpvRenderParamSwPointer = 20,
        }

        [Flags]
        public enum MpvRenderContextFlag
        {
            MpvRenderUpdateFrame = 1 << 0,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEventLogMessage
        {
            public IntPtr prefix;
            public IntPtr level;
            public IntPtr text;
            public MpvLogLevel log_level;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEvent
        {
            public MpvEventId event_id;
            public int error;
            public ulong reply_userdata;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEventClientMessage
        {
            public int num_args;
            public IntPtr args;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEventProperty
        {
            public string name;
            public MpvFormat format;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvEventEndFile
        {
            public int reason;
            public int error;
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        public struct MpvNode
        {
            [FieldOffset(0)] public IntPtr str;
            [FieldOffset(0)] public int flag;
            [FieldOffset(0)] public long int64;
            [FieldOffset(0)] public double dbl;
            [FieldOffset(0)] public IntPtr list;
            [FieldOffset(0)] public IntPtr ba;
            [FieldOffset(8)] public MpvFormat format;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MpvRenderParam
        {
            public MpvRenderParam(MpvRenderParamType t, IntPtr d)
            {
                type = t;
                data = d;
            }

            public MpvRenderParamType type;
            public IntPtr data;
        }

        #endregion

        #region functions

        public static void Command(IntPtr handle, string command)
        {
            var err = MpvCommandString(handle, command);
            if (err < 0)
                HandleError(err, "error executing command: " + command);
        }

        public static void CommandV(IntPtr handle, params string[] args)
        {
            var count = args.Length + 1;
            var pointers = new IntPtr[count];
            var rootPtr = Marshal.AllocHGlobal(IntPtr.Size * count);

            for (var index = 0; index < args.Length; index++)
            {
                var bytes = GetUtf8Bytes(args[index]);
                var ptr = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                pointers[index] = ptr;
            }

            Marshal.Copy(pointers, 0, rootPtr, count);
            var err = MpvCommand(handle, rootPtr);

            foreach (var ptr in pointers)
                Marshal.FreeHGlobal(ptr);

            Marshal.FreeHGlobal(rootPtr);
            if (err < 0)
                HandleError(err, "error executing command: " + string.Join("\n", args));
        }

        public static string ConvertFromUtf8(IntPtr nativeUtf8)
        {
            var len = 0;

            while (Marshal.ReadByte(nativeUtf8, len) != 0)
                ++len;

            var buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        public static string GetError(MpvError err) => ConvertFromUtf8(ErrorString(err));

        public static byte[] GetUtf8Bytes(string s) => Encoding.UTF8.GetBytes(s + "\0");

        public static bool GetPropertyBool(IntPtr handle, string name)
        {
            var err = MpvGetProperty(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatFlag, out var lpBuffer);
            if (err < 0)
                HandleError(err, "error getting property: " + name);

            return lpBuffer.ToInt32() != 0;
        }

        public static void SetPropertyBool(IntPtr handle, string name, bool value)
        {
            long val = value ? 1 : 0;
            var err = MpvSetPropertyLong(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatFlag, ref val);
            if (err < 0)
                HandleError(err, $"error setting property: {name} = {value}");
        }

        public static int GetPropertyInt(IntPtr handle, string name)
        {
            var err = MpvGetProperty(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatInt64, out var lpBuffer);
            if (err < 0)
                HandleError(err, "error getting property: " + name);

            return lpBuffer.ToInt32();
        }

        public static void SetPropertyInt(IntPtr handle, string name, int value)
        {
            long val = value;
            var err = MpvSetPropertyLong(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatInt64, ref val);
            if (err < 0)
                HandleError(err, $"error setting property: {name} = {value}");
        }

        public static void SetPropertyLong(IntPtr handle, string name, long value)
        {
            var err = MpvSetPropertyLong(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatInt64, ref value);
            if (err < 0)
                HandleError(err, $"error setting property: {name} = {value}");
        }

        public static long GetPropertyLong(IntPtr handle, string name)
        {
            var err = MpvGetProperty(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatInt64, out var lpBuffer);
            if (err < 0)
                HandleError(err, "error getting property: " + name);

            return lpBuffer.ToInt64();
        }

        public static double GetPropertyDouble(IntPtr handle, string name, bool handleError = true)
        {
            var err = MpvGetPropertyDouble(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatDouble, out var value);
            if (err < 0)
                HandleError(err, "error getting property: " + name);

            return value;
        }

        public static void SetPropertyDouble(IntPtr handle, string name, double value)
        {
            var val = value;
            var err = MpvSetPropertyDouble(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatDouble, ref val);
            if (err < 0)
                HandleError(err, $"error setting property: {name} = {value}");
        }

        public static string GetPropertyString(IntPtr handle, string name)
        {
            var err = MpvGetProperty(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatString, out var lpBuffer);

            if (err == 0)
            {
                var ret = ConvertFromUtf8(lpBuffer);
                Free(lpBuffer);
                return ret;
            }

            if (err < 0)
                HandleError(err, "error getting property: " + name);

            return "";
        }

        public static void SetPropertyString(IntPtr handle, string name, string value)
        {
            var bytes = GetUtf8Bytes(value);
            var err = MpvSetProperty(handle, GetUtf8Bytes(name), MpvFormat.MpvFormatString, ref bytes);
            if (err < 0)
                HandleError(err, $"error setting property: {name} = {value}");
        }

        public static void HandleError(MpvError err, string msg)
        {
            Console.WriteLine(msg);
            Console.WriteLine(GetError(err));
        }

        #endregion
    }
}