namespace ScrcpyNet
{
    // Values taken from Genymobile/scrcpy -> app/src/options.h
    public enum ScrcpyLockVideoOrientation
    {
        Unlocked = -1,
        /// <summary>
        /// Lock the current orientation when scrcpy starts.
        /// </summary>
        Initial = -2,
        /// <summary>
        /// Natural orientation.
        /// </summary>
        Orientation0 = 0,
        /// <summary>
        /// 90° counterclockwise.
        /// </summary>
        Orientation1,
        /// <summary>
        /// 180°, aka upside-down.
        /// </summary>
        Orientation2,
        /// <summary>
        /// 90° clockwise.
        /// </summary>
        Orientation3,
    };
}
