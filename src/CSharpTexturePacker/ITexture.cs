using System.Drawing;

namespace CSharpTexturePacker
{
    internal interface ITexture
    {
        int Width { get; }
        int Height { get; }

        Size Size { get; }

        Color GetPixel(int x, int y);
        void SetPixel(int x, int y, Color color);

        void CopyFrom(ITexture other, int left, int top, int width, int height, int x, int y);

        void Resize(Size newSize);
    }
}
