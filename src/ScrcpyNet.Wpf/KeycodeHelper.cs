using Serilog;
using System.Collections.Generic;
using System.Windows.Input;

namespace ScrcpyNet.Wpf
{
    public static class KeycodeHelper
    {
        private static readonly Dictionary<Key, AndroidKeycode> keycodeDict = new Dictionary<Key, AndroidKeycode>
        {
            { Key.Space, AndroidKeycode.AKEYCODE_SPACE },
            { Key.Back, AndroidKeycode.AKEYCODE_DEL },
            { Key.Left, AndroidKeycode.AKEYCODE_DPAD_LEFT },
            { Key.Up, AndroidKeycode.AKEYCODE_DPAD_UP },
            { Key.Right, AndroidKeycode.AKEYCODE_DPAD_RIGHT },
            { Key.Down, AndroidKeycode.AKEYCODE_DPAD_DOWN },
            { Key.Delete, AndroidKeycode.AKEYCODE_FORWARD_DEL },
            { Key.Tab, AndroidKeycode.AKEYCODE_TAB },
            { Key.Enter, AndroidKeycode.AKEYCODE_ENTER },
        };

        public static AndroidKeycode ConvertKey(Key key)
        {
            // A - Z
            if (key >= Key.A && key <= Key.Z)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_A - (int)Key.A;
                return (AndroidKeycode)((int)key + offset);
            }
            // Digits 0-9
            else if (key >= Key.D0 && key <= Key.D9)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_0 - (int)Key.D0;
                return (AndroidKeycode)((int)key + offset);
            }
            else if (keycodeDict.TryGetValue(key, out var androidKey))
            {
                return androidKey;
            }

            Log.Warning("Unimplemented key: {@Key}", key);

            return AndroidKeycode.AKEYCODE_UNKNOWN;
        }

        public static AndroidMetastate ConvertModifiers(ModifierKeys keyModifiers)
        {
            AndroidMetastate metastate = AndroidMetastate.AMETA_NONE;

            if (keyModifiers.HasFlag(ModifierKeys.Shift))
                metastate |= AndroidMetastate.AMETA_SHIFT_ON;

            if (keyModifiers.HasFlag(ModifierKeys.Control))
                metastate |= AndroidMetastate.AMETA_CTRL_ON;

            if (keyModifiers.HasFlag(ModifierKeys.Alt))
                metastate |= AndroidMetastate.AMETA_ALT_ON;

            return metastate;
        }
    }
}
