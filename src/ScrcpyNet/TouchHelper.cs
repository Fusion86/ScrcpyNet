namespace ScrcpyNet
{
    public static class TouchHelper
    {
        public static void ScaleToScreenSize(Position position, int width, int height)
        {
            // Lets assume that the scale on the X/Y is the same.
            double scale = (double)width / position.ScreenSize.Width;

            position.ScreenSize.Width = (ushort)width;
            position.ScreenSize.Height = (ushort)height;
            position.Point.X = (ushort)(scale * position.Point.X);
            position.Point.Y = (ushort)(scale * position.Point.Y);
        }
    }
}
