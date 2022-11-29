# Noesis.App.MediaPlayer.MPV

**Usage**
- Allow unsafe block in your project
  - `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`
- Set MediaElement callback
  - `MediaElement.SetCreateMediaPlayerCallback(MpvMediaPlayer.Create, null);`

**Notes**
- Works on Windows (x64) with embedded [mpv-2.dll](https://sourceforge.net/projects/mpv-player-windows/files/libmpv/)
- Works on Linux with libmpv ("libmpv.so.1"), to be installed on target
- Can't load/play embedded resources
- Use software rendering
