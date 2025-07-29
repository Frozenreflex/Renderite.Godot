using Godot;

namespace Renderite.Godot.Source.Helpers;

public static class KeyHelper
{
    public static Shared.Key ToRenderite(this Key key, KeyLocation location = KeyLocation.Unspecified) =>
        key switch
        {
            Key.A => Shared.Key.A,
            Key.B => Shared.Key.B,
            Key.C => Shared.Key.C,
            Key.D => Shared.Key.D,
            Key.E => Shared.Key.E,
            Key.F => Shared.Key.F,
            Key.G => Shared.Key.G,
            Key.H => Shared.Key.H,
            Key.I => Shared.Key.I,
            Key.J => Shared.Key.J,
            Key.K => Shared.Key.K,
            Key.L => Shared.Key.L,
            Key.M => Shared.Key.M,
            Key.N => Shared.Key.N,
            Key.O => Shared.Key.O,
            Key.P => Shared.Key.P,
            Key.Q => Shared.Key.Q,
            Key.R => Shared.Key.R,
            Key.S => Shared.Key.S,
            Key.T => Shared.Key.T,
            Key.U => Shared.Key.U,
            Key.V => Shared.Key.V,
            Key.W => Shared.Key.W,
            Key.X => Shared.Key.X,
            Key.Y => Shared.Key.Y,
            Key.Z => Shared.Key.Z,

            Key.Key0 => Shared.Key.Alpha0,
            Key.Key1 => Shared.Key.Alpha1,
            Key.Key2 => Shared.Key.Alpha2,
            Key.Key3 => Shared.Key.Alpha3,
            Key.Key4 => Shared.Key.Alpha4,
            Key.Key5 => Shared.Key.Alpha5,
            Key.Key6 => Shared.Key.Alpha6,
            Key.Key7 => Shared.Key.Alpha7,
            Key.Key8 => Shared.Key.Alpha8,
            Key.Key9 => Shared.Key.Alpha9,

            Key.Escape => Shared.Key.Escape,
            Key.Tab => Shared.Key.Tab,
            Key.Backspace => Shared.Key.Backspace,
            Key.Enter => Shared.Key.Return,
            Key.Space => Shared.Key.Space,
            Key.Delete => Shared.Key.Delete,
            Key.Insert => Shared.Key.Insert,
            Key.Home => Shared.Key.Home,
            Key.End => Shared.Key.End,
            Key.Pageup => Shared.Key.PageUp,
            Key.Pagedown => Shared.Key.PageDown,

            Key.Up => Shared.Key.UpArrow,
            Key.Down => Shared.Key.DownArrow,
            Key.Left => Shared.Key.LeftArrow,
            Key.Right => Shared.Key.RightArrow,

            Key.Shift => location switch
            {
                KeyLocation.Left => Shared.Key.LeftShift,
                KeyLocation.Right => Shared.Key.RightShift,
                _ => Shared.Key.Shift
            },
            Key.Ctrl => location switch
            {
                KeyLocation.Left => Shared.Key.LeftControl,
                KeyLocation.Right => Shared.Key.RightControl,
                _ => Shared.Key.Control
            },
            Key.Alt => location switch
            {
                KeyLocation.Left => Shared.Key.LeftAlt,
                KeyLocation.Right => Shared.Key.RightAlt,
                _ => Shared.Key.Alt
            },
            Key.Meta => location switch
            {
                KeyLocation.Left => Shared.Key.LeftWindows,
                KeyLocation.Right => Shared.Key.RightWindows,
                _ => Shared.Key.Windows
            },

            Key.F1 => Shared.Key.F1,
            Key.F2 => Shared.Key.F2,
            Key.F3 => Shared.Key.F3,
            Key.F4 => Shared.Key.F4,
            Key.F5 => Shared.Key.F5,
            Key.F6 => Shared.Key.F6,
            Key.F7 => Shared.Key.F7,
            Key.F8 => Shared.Key.F8,
            Key.F9 => Shared.Key.F9,
            Key.F10 => Shared.Key.F10,
            Key.F11 => Shared.Key.F11,
            Key.F12 => Shared.Key.F12,

            Key.Comma => Shared.Key.Comma,
            Key.Period => Shared.Key.Period,
            Key.Slash => Shared.Key.Slash,
            Key.Semicolon => Shared.Key.Semicolon,
            Key.Apostrophe => Shared.Key.Quote,
            Key.Bracketleft => Shared.Key.LeftBracket,
            Key.Braceright => Shared.Key.RightBracket,
            Key.Backslash => Shared.Key.Backslash,
            Key.Minus => Shared.Key.Minus,
            Key.Equal => Shared.Key.Equals,
            Key.Quoteleft => Shared.Key.BackQuote,

            _ => Shared.Key.None,
        };
}
