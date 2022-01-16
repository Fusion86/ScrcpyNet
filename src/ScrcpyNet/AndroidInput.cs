// Based on https://android.googlesource.com/platform/frameworks/native/+/master/include/android/input.h

namespace ScrcpyNet
{
    public enum AndroidKeyEventAction : byte
    {
        /** The key has been pressed down. */
        AKEY_EVENT_ACTION_DOWN = 0,

        /** The key has been released. */
        AKEY_EVENT_ACTION_UP = 1,

        /**
         * Multiple duplicate key events have occurred in a row, or a
         * complex string is being delivered.  The repeat_count property
         * of the key event contains the number of times the given key
         * code should be executed.
         */
        AKEY_EVENT_ACTION_MULTIPLE = 2
    }

    public enum AndroidMetastate : int
    {
        /** No meta keys are pressed. */
        AMETA_NONE = 0,

        /** This mask is used to check whether one of the ALT meta keys is pressed. */
        AMETA_ALT_ON = 0x02,

        /** This mask is used to check whether the left ALT meta key is pressed. */
        AMETA_ALT_LEFT_ON = 0x10,

        /** This mask is used to check whether the right ALT meta key is pressed. */
        AMETA_ALT_RIGHT_ON = 0x20,

        /** This mask is used to check whether one of the SHIFT meta keys is pressed. */
        AMETA_SHIFT_ON = 0x01,

        /** This mask is used to check whether the left SHIFT meta key is pressed. */
        AMETA_SHIFT_LEFT_ON = 0x40,

        /** This mask is used to check whether the right SHIFT meta key is pressed. */
        AMETA_SHIFT_RIGHT_ON = 0x80,

        /** This mask is used to check whether the SYM meta key is pressed. */
        AMETA_SYM_ON = 0x04,

        /** This mask is used to check whether the FUNCTION meta key is pressed. */
        AMETA_FUNCTION_ON = 0x08,

        /** This mask is used to check whether one of the CTRL meta keys is pressed. */
        AMETA_CTRL_ON = 0x1000,

        /** This mask is used to check whether the left CTRL meta key is pressed. */
        AMETA_CTRL_LEFT_ON = 0x2000,

        /** This mask is used to check whether the right CTRL meta key is pressed. */
        AMETA_CTRL_RIGHT_ON = 0x4000,

        /** This mask is used to check whether one of the META meta keys is pressed. */
        AMETA_META_ON = 0x10000,

        /** This mask is used to check whether the left META meta key is pressed. */
        AMETA_META_LEFT_ON = 0x20000,

        /** This mask is used to check whether the right META meta key is pressed. */
        AMETA_META_RIGHT_ON = 0x40000,

        /** This mask is used to check whether the CAPS LOCK meta key is on. */
        AMETA_CAPS_LOCK_ON = 0x100000,

        /** This mask is used to check whether the NUM LOCK meta key is on. */
        AMETA_NUM_LOCK_ON = 0x200000,

        /** This mask is used to check whether the SCROLL LOCK meta key is on. */
        AMETA_SCROLL_LOCK_ON = 0x400000,
    }

    public enum AndroidMotionEventAction
    {
        /** Bit mask of the parts of the action code that are the action itself. */
        AMOTION_EVENT_ACTION_MASK = 0xff,

        /**
         * Bits in the action code that represent a pointer index, used with
         * AMOTION_EVENT_ACTION_POINTER_DOWN and AMOTION_EVENT_ACTION_POINTER_UP.  Shifting
         * down by AMOTION_EVENT_ACTION_POINTER_INDEX_SHIFT provides the actual pointer
         * index where the data for the pointer going up or down can be found.
         */
        AMOTION_EVENT_ACTION_POINTER_INDEX_MASK = 0xff00,

        /** A pressed gesture has started, the motion contains the initial starting location. */
        AMOTION_EVENT_ACTION_DOWN = 0,

        /**
         * A pressed gesture has finished, the motion contains the final release location
         * as well as any intermediate points since the last down or move event.
         */
        AMOTION_EVENT_ACTION_UP = 1,

        /**
         * A change has happened during a press gesture (between AMOTION_EVENT_ACTION_DOWN and
         * AMOTION_EVENT_ACTION_UP).  The motion contains the most recent point, as well as
         * any intermediate points since the last down or move event.
         */
        AMOTION_EVENT_ACTION_MOVE = 2,

        /**
         * The current gesture has been aborted.
         * You will not receive any more points in it.  You should treat this as
         * an up event, but not perform any action that you normally would.
         */
        AMOTION_EVENT_ACTION_CANCEL = 3,

        /**
         * A movement has happened outside of the normal bounds of the UI element.
         * This does not provide a full gesture, but only the initial location of the movement/touch.
         */
        AMOTION_EVENT_ACTION_OUTSIDE = 4,

        /**
         * A non-primary pointer has gone down.
         * The bits in AMOTION_EVENT_ACTION_POINTER_INDEX_MASK indicate which pointer changed.
         */
        AMOTION_EVENT_ACTION_POINTER_DOWN = 5,

        /**
         * A non-primary pointer has gone up.
         * The bits in AMOTION_EVENT_ACTION_POINTER_INDEX_MASK indicate which pointer changed.
         */
        AMOTION_EVENT_ACTION_POINTER_UP = 6,

        /**
         * A change happened but the pointer is not down (unlike AMOTION_EVENT_ACTION_MOVE).
         * The motion contains the most recent point, as well as any intermediate points since
         * the last hover move event.
         */
        AMOTION_EVENT_ACTION_HOVER_MOVE = 7,

        /**
         * The motion event contains relative vertical and/or horizontal scroll offsets.
         * Use getAxisValue to retrieve the information from AMOTION_EVENT_AXIS_VSCROLL
         * and AMOTION_EVENT_AXIS_HSCROLL.
         * The pointer may or may not be down when this event is dispatched.
         * This action is always delivered to the winder under the pointer, which
         * may not be the window currently touched.
         */
        AMOTION_EVENT_ACTION_SCROLL = 8,

        /** The pointer is not down but has entered the boundaries of a window or view. */
        AMOTION_EVENT_ACTION_HOVER_ENTER = 9,

        /** The pointer is not down but has exited the boundaries of a window or view. */
        AMOTION_EVENT_ACTION_HOVER_EXIT = 10,

        /* One or more buttons have been pressed. */
        AMOTION_EVENT_ACTION_BUTTON_PRESS = 11,

        /* One or more buttons have been released. */
        AMOTION_EVENT_ACTION_BUTTON_RELEASE = 12,
    };

    public enum AndroidMotionEventButtons : int
    {
        /** primary */
        AMOTION_EVENT_BUTTON_PRIMARY = 1 << 0,
        /** secondary */
        AMOTION_EVENT_BUTTON_SECONDARY = 1 << 1,
        /** tertiary */
        AMOTION_EVENT_BUTTON_TERTIARY = 1 << 2,
        /** back */
        AMOTION_EVENT_BUTTON_BACK = 1 << 3,
        /** forward */
        AMOTION_EVENT_BUTTON_FORWARD = 1 << 4,
        AMOTION_EVENT_BUTTON_STYLUS_PRIMARY = 1 << 5,
        AMOTION_EVENT_BUTTON_STYLUS_SECONDARY = 1 << 6,
    };
}
