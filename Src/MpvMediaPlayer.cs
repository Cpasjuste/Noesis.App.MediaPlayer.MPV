// 2022 @ cpasjuste

using Noesis;
using System;

namespace NoesisApp
{
    public class MpvMediaPlayer : MediaPlayer
    {
        private TextureData _texture;
        private IntPtr _mpvHandle;
        private IntPtr _mpvRenderContext;

        private MpvMediaPlayer(MediaElement owner, Uri uri)
        {
            // first check for file existence
            if (!System.IO.File.Exists(uri.LocalPath))
            {
                RaiseMediaFailed(new System.IO.IOException("MpvMediaPlayer: file not found (" + uri + ")"));
                return;
            }

            // mpv player create
            _mpvHandle = Mpv.Create();
            if (_mpvHandle == IntPtr.Zero)
            {
                throw new Exception("mpv_create");
            }

            // mpv properties/parameters
#if DEBUG
            Mpv.SetPropertyString(_mpvHandle, "terminal", "yes");
            Mpv.SetPropertyString(_mpvHandle, "msg-level", "all=info");
#endif
            Mpv.SetPropertyString(_mpvHandle, "idle", "yes");
            Mpv.SetPropertyString(_mpvHandle, "keep-open", "yes");
            Mpv.SetPropertyString(_mpvHandle, "profile", "sw-fast");

            // mpv init
            var err = Mpv.Initialize(_mpvHandle);
            if (err < 0)
            {
                throw new Exception("mpv_initialize error: " + Mpv.GetError(err));
            }

            var api = Marshal.StringToHGlobalAnsi("sw");
            Mpv.MpvRenderParam[] renderParams =
            {
                new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamApiType, api),
                new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamInvalid, IntPtr.Zero)
            };
            err = Mpv.RenderContextCreate(out _mpvRenderContext, _mpvHandle, renderParams);
            if (err < 0)
            {
                throw new Exception("mpv_render_context_create error: " + Mpv.GetError(err));
            }

            Marshal.FreeHGlobal(api);

            // handle mpv events
            owner.View.Rendering += OnRendering;

            // load file
            Mpv.CommandV(_mpvHandle, "loadfile", uri.ToString(), "replace");
        }

        ~MpvMediaPlayer()
        {
            //Console.WriteLine("MPVMediaPlayer: ~MPVMediaPlayer");
            Close();
        }

        public override void Close()
        {
            //Console.WriteLine("MPVMediaPlayer: Close");
            if (_mpvRenderContext != IntPtr.Zero)
            {
                Mpv.RenderContextFree(_mpvRenderContext);
                _mpvRenderContext = IntPtr.Zero;
            }

            if (_mpvHandle == IntPtr.Zero) return;
            Mpv.Destroy(_mpvHandle);
            _mpvHandle = IntPtr.Zero;
        }

        private void OnRendering(object sender, Noesis.EventArgs e)
        {
            // as per mpv documentation, it is safe to check for event in main/rendering thread if mpv_wait_event timeout is 0
            // https://github.com/mpv-player/mpv/blob/ead8469454afa63e6e1fdd9e978af765f89379ce/libmpv/client.h#L1679
            if (_mpvHandle == IntPtr.Zero || _mpvRenderContext == IntPtr.Zero) return;

            try
            {
                // first check for eof-reached ("keep-open" MPV_EVENT_FILE_LOADED not firing fix)
                var eof = IsLoaded() && Mpv.GetPropertyBool(_mpvHandle, "eof-reached");
                if (eof)
                {
                    RaiseMediaEnded();
                    return;
                }

                var ptr = Mpv.WaitEvent(_mpvHandle, 0);
                var evt = Marshal.PtrToStructure<Mpv.MpvEvent>(ptr);
                switch (evt.event_id)
                {
                    case Mpv.MpvEventId.MpvEventFileLoaded:
                        Console.WriteLine("MPVMediaPlayer: OnMediaOpened");
                        _texture = new TextureData(Width, Height,
                            new DynamicTextureSource(Width, Height, TextureRender, this));
                        RaiseMediaOpened();
                        break;
                    case Mpv.MpvEventId.MpvEventEndFile:
                        var data = Marshal.PtrToStructure<Mpv.MpvEventEndFile>(evt.data);
                        var reason = (Mpv.MpvEndFileReason)data.reason;
                        if (reason != Mpv.MpvEndFileReason.MpvEndFileReasonError)
                        {
                            Console.WriteLine("MPVMediaPlayer: OnMediaEnded");
                            RaiseMediaEnded();
                        }
                        else
                        {
                            Console.WriteLine("MPVMediaPlayer: OnMediaFailed");
                            RaiseMediaFailed(new Exception("MpvMediaPlayer: " + Mpv.GetError((Mpv.MpvError)data.error)));
                        }

                        break;
                    case Mpv.MpvEventId.MpvEventNone:
                    case Mpv.MpvEventId.MpvEventShutdown:
                    case Mpv.MpvEventId.MpvEventLogMessage:
                    case Mpv.MpvEventId.MpvEventGetPropertyReply:
                    case Mpv.MpvEventId.MpvEventSetPropertyReply:
                    case Mpv.MpvEventId.MpvEventCommandReply:
                    case Mpv.MpvEventId.MpvEventStartFile:
                    case Mpv.MpvEventId.MpvEventIdle:
                    case Mpv.MpvEventId.MpvEventTick:
                    case Mpv.MpvEventId.MpvEventScriptInputDispatch:
                    case Mpv.MpvEventId.MpvEventClientMessage:
                    case Mpv.MpvEventId.MpvEventVideoReConfig:
                    case Mpv.MpvEventId.MpvEventAudioReConfig:
                    case Mpv.MpvEventId.MpvEventSeek:
                    case Mpv.MpvEventId.MpvEventPlaybackRestart:
                    case Mpv.MpvEventId.MpvEventPropertyChange:
                    case Mpv.MpvEventId.MpvEventQueueOverflow:
                    case Mpv.MpvEventId.MpvEventHook:
                    default:
                        //Console.WriteLine("event: " + evt.event_id);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override uint Width => IsLoaded() ? (uint)Mpv.GetPropertyInt(_mpvHandle, "width") : 0;

        public override uint Height => IsLoaded() ? (uint)Mpv.GetPropertyInt(_mpvHandle, "height") : 0;

        public override bool CanPause => true;

        public override bool HasAudio => true;

        public override bool HasVideo => true;

        public override float BufferingProgress => 1;

        public override float DownloadProgress => 1;

        public override double Duration => IsLoaded() ? Mpv.GetPropertyDouble(_mpvHandle, "duration") : 0;

        public override double Position {
            get => IsLoaded() ? Mpv.GetPropertyDouble(_mpvHandle, "playback-time") : 0;
            set {
                if (IsLoaded()) Mpv.Command(_mpvHandle, "no-osd seek " + value + " absolute");
            }
        }

        public override float SpeedRatio {
            get => IsLoaded() ? (float)Mpv.GetPropertyDouble(_mpvHandle, "speed") : 0;
            set {
                if (IsLoaded()) Mpv.Command(_mpvHandle, "no-osd set speed " + value);
            }
        }

        public override float Volume {
            get => IsLoaded() ? (float)Mpv.GetPropertyInt(_mpvHandle, "volume") / 100 : 0.5f;
            set {
                if (IsLoaded()) Mpv.Command(_mpvHandle, "no-osd set volume " + (value * 100));
            }
        }

        public override float Balance => 0.5f;

        public override bool IsMuted => false;

        public override bool ScrubbingEnabled => false;

        public override void Play()
        {
            Console.WriteLine("MPVMediaPlayer: Play");
            if (IsLoaded()) Mpv.Command(_mpvHandle, "set pause no");
        }

        public override void Pause()
        {
            Console.WriteLine("MPVMediaPlayer: Pause");
            if (IsLoaded()) Mpv.Command(_mpvHandle, "set pause yes");
        }

        public override void Stop()
        {
            Console.WriteLine("MPVMediaPlayer: Stop");
            if (!IsLoaded()) return;
            Position = 0;
            Mpv.Command(_mpvHandle, "set pause yes");
        }

        public bool IsPaused()
        {
            return IsLoaded() && Mpv.GetPropertyBool(_mpvHandle, "pause");
        }

        public bool IsLoaded()
        {
            return _mpvRenderContext != IntPtr.Zero && !Mpv.GetPropertyBool(_mpvHandle, "playback-abort");
        }

        public override ImageSource TextureSource => _texture.TextureSource;

        public static MediaPlayer Create(MediaElement owner, Uri uri, object user)
        {
            return new MpvMediaPlayer(owner, uri);
        }

        private static Texture TextureRender(RenderDevice device, object user)
        {
            var mediaPlayer = user as MpvMediaPlayer;
            return mediaPlayer?.GetTexture(device);
        }

        private unsafe Texture GetTexture(RenderDevice device)
        {
            if (!IsLoaded() || Width <= 0 || Height <= 0) return null;

            if (_texture.Texture == null || _texture.Width != Width || _texture.Height != Height)
            {
                Console.WriteLine("MpvMediaPlayer: resizing pixels (" + Width + " x " + Height + ")");
                _texture.Resize(Width, Height);
                _texture.Texture = device.CreateTexture(
                    "mpv", _texture.Width, _texture.Height, 1, TextureFormat.RGBA8, IntPtr.Zero);
            }

            var flags = Mpv.RenderContextUpdate(_mpvRenderContext);
            if (!flags.HasFlag(Mpv.MpvRenderContextFlag.MpvRenderUpdateFrame)) return _texture.Texture;
            lock (_texture)
            {
                fixed (byte* data = _texture.Data)
                {
                    var p = _texture.Pitch;
                    var pitch = (IntPtr)(&p);
                    int[] s = { (int)_texture.Width, (int)_texture.Height };
                    fixed (int* size = s)
                    {
                        Mpv.MpvRenderParam[] renderParams =
                        {
                            new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamSwSize, new IntPtr(size)),
                            new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamSwFormat, _texture.FormatPtr),
                            new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamSwStride, pitch),
                            new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamSwPointer, (IntPtr)data),
                            new Mpv.MpvRenderParam(Mpv.MpvRenderParamType.MpvRenderParamInvalid, IntPtr.Zero)
                        };

                        var err = Mpv.RenderContextRender(_mpvRenderContext, renderParams);
                        if (err < 0)
                        {
                            throw new Exception("mpv_render_context_render error: " + Mpv.GetError(err));
                        }
                    }

                    device.UpdateTexture(_texture.Texture, 0, 0, 0, _texture.Width, _texture.Height, (IntPtr)data);
                }
            }

            return _texture.Texture;
        }

        private class TextureData
        {
            public TextureData(uint w, uint h, DynamicTextureSource tex)
            {
                FormatPtr = Marshal.StringToHGlobalAnsi("rgba");
                TextureSource = tex;
                Resize(w, h);
            }

            ~TextureData()
            {
                Marshal.FreeHGlobal(FormatPtr);
            }

            public void Resize(uint w, uint h)
            {
                Pitch = w * 4;
                Data = new byte[w * h * 4];
                TextureSource.Resize(w, h);
            }

            public uint Width => (uint)TextureSource.PixelWidth;
            public uint Height => (uint)TextureSource.PixelHeight;
            public long Pitch;
            public byte[] Data;
            public readonly IntPtr FormatPtr;
            public readonly DynamicTextureSource TextureSource;
            public Texture Texture;
        }
    }
}
