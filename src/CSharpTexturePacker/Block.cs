using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace CSharpTexturePacker
{
    internal class Block
    {
        public Block() { }

        public Block(ITexture texture, string path, bool alphaTrim)
        {
            Texture = texture;
            CalcValid(alphaTrim);
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }

        public Block(Block other)
        {
            Valid   = other.Valid;
            Name    = other.Name;
            Fit     = other.Fit;
            Texture = other.Texture;
            Path    = other.Path;
            Rotated = other.Rotated;
        }

        public Rectangle ValidArea()
        {
		    if(!Rotated) return Valid;

            return new Rectangle
            (
                Valid.Location.X,
                Valid.Location.Y,
                Valid.Height,
                Valid.Width
            );
	    }

        private void CalcValid(bool alphaTrim)
        {
            if (alphaTrim)
            {
                int minx = Int32.MaxValue;
                int miny = Int32.MaxValue;
                int maxx = Int32.MinValue;
                int maxy = Int32.MinValue;

                bool found = false;
                for (int j = 0; j < Texture.Height; j++)
                {
                    for (int i = 0; i < Texture.Width; i++)
                    {
                        if (!IsTransparent(Texture.GetPixel(i, j)))
                        {
                            miny = j;
                            found = true;

                            break;
                        }
                    }
                    if (found)
                        break;
                }

                found = false;
                for (int j = Texture.Height - 1; j >= 0; j--)
                {
                    for (int i = 0; i < Texture.Width; i++)
                    {
                        if (!IsTransparent(Texture.GetPixel(i, j)))
                        {
                            maxy = j;
                            found = true;

                            break;
                        }
                    }
                    if (found)
                        break;
                }


                found = false;
                for (int i = 0; i < Texture.Width; i++)
                {
                    for (int j = miny; j <= maxy; j++)
                    {
                        if (!IsTransparent(Texture.GetPixel(i, j)))
                        {
                            minx = i;
                            found = true;

                            break;
                        }
                    }
                    if (found)
                        break;
                }
                found = false;
                for (int i = Texture.Width - 1; i >= 0; i--)
                {
                    for (int j = miny; j <= maxy; j++)
                    {
                        if (!IsTransparent(Texture.GetPixel(i, j)))
                        {
                            maxx = i;
                            found = true;

                            break;
                        }
                    }
                    if (found)
                        break;
                }

                Valid = new Rectangle(minx, miny, maxx - minx, maxy - miny);
            }
            else
            {
                Valid = new Rectangle(Point.Empty, Texture.Size - new Size(1, 1));
            }
        }

        public void Clear()
        {
            if (Fit != null)
            {
                Fit.Clear();
                Rotated = false;
            }
        }

        public void WriteMeta(StreamWriter writer, ref int indent, int padding)
        {
            indent++;

            Rectangle frame = new Rectangle();
            Point location = Fit.Rectangle.Location;
            frame.Location = location;
            frame.Size = Valid.Size - new Size(1, 1);

            string indentation = new string('\t', indent);

            // TODO: add padding to frame on 1st line
            // TODO: check that Rectangle, Size and Point serialize correctly

            writer.WriteLine($"{indentation}frame : \"{frame}\",");
            writer.WriteLine($"{indentation}offset : \"{Point.Empty}\",");
            writer.WriteLine($"{indentation}rotated : false,");
            writer.WriteLine($"{indentation}sourceColorRect : \"{Valid}\",");
            writer.WriteLine($"{indentation}sourceSize : \"{Texture.Size}\"");

            indent--;
        }

        public Node      Fit     { get; set; }
        public bool      Rotated { get; set; }
        public Rectangle Valid   { get; private set; }
        public string    Name    { get; private set; }
        public ITexture  Texture { get; private set; }

        // TODO: delete?
        public string    Path    { get; private set; }

        private bool IsTransparent(Color color)
        {
            return color.A == 0;
        }
    }
}
