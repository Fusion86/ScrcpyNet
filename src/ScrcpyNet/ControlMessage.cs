using System;
using System.Buffers.Binary;

namespace ScrcpyNet
{
    public enum ControlMessageType : byte
    {
        ControlMsgTypeInjectKeycode,
        ControlMsgTypeInjectText,
        ControlMsgTypeInjectTouchEvent,
        ControlMsgTypeInjectScrollEvent,
        ControlMsgTypeBackOrScreenOn,
        ControlMsgTypeExpandNotificationPanel,
        ControlMsgTypeCollapseNotificationPanel,
        ControlMsgTypeGetClipboard,
        ControlMsgTypeSetClipboard,
        ControlMsgTypeSetScreenPowerMode,
        ControlMsgTypeRotateDevice,
    }

    public struct ScreenSize
    {
        public ushort Width;
        public ushort Height;
    }

    public struct Point
    {
        public int X;
        public int Y;
    }

    public struct Position
    {
        public ScreenSize ScreenSize;
        public Point Point;
    }

    public interface IControlMessage
    {
        public ControlMessageType Type { get; }

        Span<byte> ToBytes();
    }

    public class KeycodeControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.ControlMsgTypeInjectKeycode;
        public AndroidKeyeventAction Action;
        public AndroidKeycode KeyCode;
        public uint Repeat;
        public AndroidMetastate Metastate;

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[14];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            BinaryPrimitives.WriteInt32BigEndian(b[2..], (int)KeyCode);
            BinaryPrimitives.WriteInt32BigEndian(b[6..], (int)Repeat);
            BinaryPrimitives.WriteInt32BigEndian(b[10..], (int)Metastate);
            return b;
        }
    }

    public class BackOrScreenOnControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.ControlMsgTypeBackOrScreenOn;

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[1];
            b[0] = (byte)Type;
            return b;
        }
    }

    public class TouchEventControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.ControlMsgTypeInjectTouchEvent;
        public AndroidMotioneventAction Action;
        public AndroidMotioneventButtons Buttons = AndroidMotioneventButtons.AMOTION_EVENT_BUTTON_PRIMARY;
        public ulong PointerId = 0xFFFFFFFFFFFFFFFF;
        public Position Position;
        //public float Pressure { get; set; }

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[28];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            BinaryPrimitives.WriteUInt64BigEndian(b[2..], PointerId);

            // Position
            BinaryPrimitives.WriteInt32BigEndian(b[10..], Position.Point.X);
            BinaryPrimitives.WriteInt32BigEndian(b[14..], Position.Point.Y);
            BinaryPrimitives.WriteUInt16BigEndian(b[18..], Position.ScreenSize.Width);
            BinaryPrimitives.WriteUInt16BigEndian(b[20..], Position.ScreenSize.Height);

            // TODO: Pressure
            b[22] = 0xFF;
            b[23] = 0xFF;

            BinaryPrimitives.WriteInt32BigEndian(b[24..], (int)Buttons);

            return b;
        }
    }
}
