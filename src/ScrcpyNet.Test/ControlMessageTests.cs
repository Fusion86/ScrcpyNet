using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScrcpyNet.Test
{
    [TestClass]
    public class ControlMessageTests
    {
        [TestMethod]
        public void KeyPressH()
        {
            var msg = new KeycodeControlMessage();
            msg.KeyCode = AndroidKeycode.AKEYCODE_H;
            msg.Metastate = AndroidMetastate.AMETA_NUM_LOCK_ON;

            var expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 };
            var actual = msg.ToBytes();
            CollectionAssert.AreEqual(expected, actual.ToArray());
        }

        [TestMethod]
        public void KeyPressE()
        {
            var msg = new KeycodeControlMessage();
            msg.KeyCode = AndroidKeycode.AKEYCODE_E;
            msg.Metastate = AndroidMetastate.AMETA_NONE;

            var expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x21, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            var actual = msg.ToBytes();
            CollectionAssert.AreEqual(expected, actual.ToArray());
        }

        [TestMethod]
        public void KeyPressL()
        {
            var msg = new KeycodeControlMessage();
            msg.KeyCode = AndroidKeycode.AKEYCODE_L;
            msg.Metastate = AndroidMetastate.AMETA_SHIFT_ON;

            var expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
            var actual = msg.ToBytes();
            CollectionAssert.AreEqual(expected, actual.ToArray());
        }

        [TestMethod]
        public void KeyReleaseO()
        {
            var msg = new KeycodeControlMessage();
            msg.Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP;
            msg.KeyCode = AndroidKeycode.AKEYCODE_O;
            msg.Metastate = AndroidMetastate.AMETA_NUM_LOCK_ON;

            var expected = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x2B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 };
            var actual = msg.ToBytes();
            CollectionAssert.AreEqual(expected, actual.ToArray());
        }

        [TestMethod]
        public void TouchEvent()
        {
            var msg = new TouchEventControlMessage();
            msg.Action = AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN;
            msg.PointerId = 0x1234567887654321;
            msg.Position.Point.X = 100;
            msg.Position.Point.Y = 200;
            msg.Position.ScreenSize.Width = 1080;
            msg.Position.ScreenSize.Height = 1920;

            var expected = new byte[] {
                (byte)ControlMessageType.InjectTouchEvent,
                0x00, // AKEY_EVENT_ACTION_DOWN
                0x12, 0x34, 0x56, 0x78, 0x87, 0x65, 0x43, 0x21, // pointer id
                0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0xc8, // 100 200
                0x04, 0x38, 0x07, 0x80, // 1080 1920
                0xff, 0xff, // pressure
                0x00, 0x00, 0x00, 0x01 // AMOTION_EVENT_BUTTON_PRIMARY
            };
            var actual = msg.ToBytes();
            CollectionAssert.AreEqual(expected, actual.ToArray());
        }
    }
}
