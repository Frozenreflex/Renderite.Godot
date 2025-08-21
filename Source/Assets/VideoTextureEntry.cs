using System;
using Godot;
using Renderite.Godot.Source.Helpers;
using Renderite.Shared;

namespace Renderite.Godot.Source;

public class VideoTextureEntry : TextureEntry
{
    private readonly SubViewport _viewport;
    private Control _videoPlayback;
    private float _fps;
    private int _currentPosition;

    public VideoTextureEntry()
    {
        _viewport = new SubViewport
        {
            Disable3D = true,
            Name = "VideoPlayer",
            Size = new Vector2I
            {
                X = 500,
                Y = 500,
            },
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        Rid = _viewport.GetTexture().GetRid();
        Main.Instance.AddChild(_viewport);
    }

    public VideoTextureEntry(SubViewport viewport)
    {
        _viewport = viewport;
    }

    public void Handle(VideoTextureLoad command)
    {
        if (command.source.Equals(
                "https://assets.resonite.com/6b9a99f06ac972e9b83471027bdb692516a069fc581301ef134ac20a564d0f73"))
            return;
        GD.Print($"Loading video {command.source}");
        _videoPlayback = new Control
        {
            Size = new Vector2I
            {
                X = 500,
                Y = 500,
            },
            PivotOffset = new Vector2I(250, 250),
            Scale = new Vector2I(1, -1)
        };
        _videoPlayback.Set("debug", true);

        // I FUCKING HATE GDSCRIPT
        _videoPlayback.SetScript(GD.Load<GDScript>("res://addons/gde_gozen/video_playback.gd"));
        _videoPlayback.Connect("frame_changed", Callable.From((int frame) => { _currentPosition = frame; }));
        _viewport.AddChild(_videoPlayback);
        _videoPlayback.Call("seek_frame", 0);
        _videoPlayback.Connect("video_loaded", Callable.From(() =>
        {
            GD.Print($"Loaded {command.source}");
            _viewport.Size = _videoPlayback.Call("get_video_resolution").AsVector2I();
            _videoPlayback.Size = _viewport.Size;
            _videoPlayback.PivotOffset = _viewport.Size / 2;
            _fps = _videoPlayback.Call("get_video_framerate").As<float>();
            RendererManager.Instance.BackgroundMessagingManager.SendCommand(new VideoTextureReady
            {
                assetId = command.assetId,
                instanceChanged = !Instantiated,
                length = _videoPlayback.Call("get_video_frame_count").As<int>() / _fps,
                audioTracks = [],
                hasAlpha = false,
                playbackEngine = "gde_gozen",
                size = _videoPlayback.Call("get_video_resolution").AsVector2I().ToRenderite()
            });
            GD.Print($"Sent {command.source}");
            Instantiated = true;
        }));
        _videoPlayback.Call("set_video_path", command.source);
    }

    public void Handle(VideoTextureUpdate command)
    {
        var playing = _videoPlayback.Get("is_playing").AsBool();
        if (playing && !command.play)
        {
            _videoPlayback.Call("pause");
        }
        else if (!playing && command.play)
        {
            _videoPlayback.Call("play");
        }

        var currentFrame = _videoPlayback.Get("current_frame").As<int>();
        var loop = _videoPlayback.Get("loop").AsBool();

        if (loop && !command.loop)
            _videoPlayback.Set("loop", false);
        else if (!loop && command.loop)
            _videoPlayback.Set("loop", true);

        var newPos = (int)Math.Floor(command.position * _fps);
        var diff = newPos - _currentPosition;
        if (diff == 2)
        {
            GD.Print($"Seek to: {newPos} was: {_currentPosition}");
            _videoPlayback.Call("next_frame", false);
        }
        else if (Math.Abs(diff) > 2)
        {
            GD.Print($"Seek to: {newPos} was: {_currentPosition}");
            _videoPlayback.Call("seek_frame", newPos);
        }

        _currentPosition = newPos;
    }

    public void Cleanup()
    {
        //_videoStreamPlayer.Stop();
        _viewport.QueueFree();
    }
}