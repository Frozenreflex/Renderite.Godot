using System;
using Godot;
using Renderite.Shared;

namespace Renderite.Godot.Source.Helpers;

public static class EnumHelpers
{
    public static RenderingServer.ShadowCastingSetting ToGodot(this ShadowCastMode mode)
    {
        return mode switch
        {
            ShadowCastMode.Off => RenderingServer.ShadowCastingSetting.Off,
            ShadowCastMode.On => RenderingServer.ShadowCastingSetting.On,
            ShadowCastMode.ShadowOnly => RenderingServer.ShadowCastingSetting.ShadowsOnly,
            ShadowCastMode.DoubleSided => RenderingServer.ShadowCastingSetting.DoubleSided,
            _ => RenderingServer.ShadowCastingSetting.Off,
        };
    }
}
