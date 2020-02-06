using System.Drawing;

namespace CSharpTexturePacker
{
    internal class Node
    {
        public Node() { }

        public Node(int x, int y, int w, int h)
        {
            Rectangle = new Rectangle(x, y, w, h);
        }

        public Node(Node other)
        {
            Rectangle = other.Rectangle;

            Used = other.Used;
            Down = other.Down;
            Right = other.Right;
        }

        public void Clear()
        {
            Down?.Clear();
            Down = null;

            Right?.Clear();
            Right = null;
        }

        public Rectangle Rectangle { get; private set; }

        public bool Used  { get; set; }
        public Node Down  { get; set; }
        public Node Right { get; set; }
    }
}
