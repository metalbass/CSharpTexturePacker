using System.Drawing;

namespace CSharpTexturePacker
{
    public static class RectangleExtensions
    {
        public static int ComputeArea(this Rectangle rectangle)
        {
            return rectangle.Width * rectangle.Height;
        }

    }
}
