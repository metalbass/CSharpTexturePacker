using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTexturePacker
{
    internal class Packer
    {
        private Packer()
        {
            m_padding = 1;
            m_allowRotate = true;
            m_powerOfTwo = true;
            m_alphaTrim = true;

            m_images = new LinkedList<Tuple<string, Block>>();
        }

        private Block GetBlock(string name)
        {
            foreach (Tuple<string, Block> tuple in m_images)
            {
                Block blk = tuple.Item2;
                if (tuple.Item1 == name)
                    return blk;
            }

            return null;
        }

        private bool AddTexture(string name, ITexture img)
        {
		    if(GetBlock(name) != null)
			    return false;

            m_images.AddLast(Tuple.Create(name, new Block(img, name, m_alphaTrim)));
		    m_totalArea += m_images.Last.Value.Item2.Valid.ComputeArea();

		    return true;
	    }

        private bool Remove(string name)
        {
		    if(GetBlock(name) == null)
			    return false;

            LinkedListNode<Tuple<string, Block>> current = m_images.First;

            while (current != null)
            {
                if (current.Value.Item1 == name)
                {
                    Block blk = current.Value.Item2;
                    m_totalArea -= blk.Valid.ComputeArea();

                    m_images.Remove(current);
                    break;
                }

                current = current.Next;
            }

		    return true;
	    }

        private void Clear()
        {
            Tidy();

            m_images.Clear();

            m_totalArea = 0;
        }

        private void Tidy()
        {
            foreach (Tuple<string, Block> tuple in m_images)
            {
                Block blk = tuple.Item2;
                blk.Clear();
            }

            m_rootNode?.Clear();
        }

        private string GetIndent()
        {
            return new string('\t', m_indent);
        }

        private void WriteMeta(StreamWriter s, LinkedList<Tuple<string, Block>> p, ITexture t, string n)
        {
            s.WriteLine("{");
		    {
			    m_indent++;
                s.WriteLine(GetIndent() + "frames: {");
			    {
				    m_indent++;
				    int i = 0;
                    foreach (Tuple<string, Block> tuple in m_images)
                    {
					    Block blk = tuple.Item2;
                        s.WriteLine(GetIndent() + blk.Name + " : {");
                        blk.WriteMeta(s, ref m_indent, m_padding);
                        if (i == p.Count - 1)
                            s.WriteLine(GetIndent() + "}");
                        else
                            s.WriteLine(GetIndent() + "},");
					    i++;
				    }
				    m_indent--;
			    }
                s.WriteLine(GetIndent() + "},");
			    m_indent--;
		    }
		    {
			    m_indent++;
			    s.WriteLine(GetIndent() + "metadata : {");
			    {
				    m_indent++;
				    s.WriteLine(GetIndent() + "textureName : " + n + ",");
				    s.WriteLine(GetIndent() + "size : " + "\"" + t.Size + "\"");
				    m_indent--;
			    }
			    s.WriteLine(GetIndent() + "}");
			    m_indent--;
		    }
		    s.WriteLine("}");
	    }

        private void Pack(ITexture result, LinkedList<Tuple<string, Block>> packed, LinkedList<Tuple<string, Block>> failed)
        {
		    // Step 1. Tidy.
		    Tidy();

            // Step 2. Sort.
            m_images = new LinkedList<Tuple<string, Block>>(m_images.OrderBy(x => x.Item2.Valid));

		    // Step 3. Fit.
		    if (m_outputTextureSize.IsEmpty)
            {
			    if (true) //sqrt_area
                {
                    int s = (int)(Math.Sqrt(m_totalArea) + 0.5f);
				    m_rootNode = new Node(0, 0, s, s);
			    }
                else
                {
				    int w = m_images.Count > 0 ? (m_images.First.Value.Item2.Valid.Width + (m_padding * 2)): 0;
				    int h = m_images.Count > 0 ? (m_images.First.Value.Item2.Valid.Height + (m_padding * 2)) : 0;
				    m_rootNode = new Node(0, 0, w, h);
			    }
		    }
            else
            {
			    m_rootNode = new Node(0, 0, m_outputTextureSize.X, m_outputTextureSize.Y);
		    }

            Node nd = null;
            foreach (Tuple<string, Block> tuple in m_images)
            {
			    Block blk = tuple.Item2;
                nd = Find(m_rootNode, blk.Valid.Width + (m_padding * 2), blk.Valid.Height + (m_padding * 2));

                if (nd != null)
				    blk.Fit = Split(nd, blk.Valid.Width + (m_padding * 2), blk.Valid.Height + (m_padding * 2));
			    else
				    blk.Fit = Grow(blk.Valid.Width + (m_padding * 2), blk.Valid.Height + (m_padding * 2));

			    if(blk.Fit == null && m_allowRotate)
                {
				    blk.Rotated = true;
                    nd = Find(m_rootNode, blk.ValidArea().Width + (m_padding * 2), blk.ValidArea().Height + (m_padding * 2));

                    if (nd != null)
					    blk.Fit = Split(nd, blk.ValidArea().Width + (m_padding * 2), blk.ValidArea().Height + (m_padding * 2));
				    else
					    blk.Fit = Grow(blk.ValidArea().Width + (m_padding * 2), blk.ValidArea().Height + (m_padding * 2));
			    }

			    if(blk.Fit != null)
				    packed.AddLast(Tuple.Create(tuple.Item1, new Block(blk)));
			    else
				    failed.AddLast(Tuple.Create(tuple.Item1, new Block(blk)));
		    }

		    // Step 4. Draw.
		    Point os = new Point(m_rootNode.Rectangle.Width, m_rootNode.Rectangle.Height);
		    if(m_powerOfTwo) {
			    os.X = (int)Math.Pow(2, Math.Ceiling(Math.Log(os.X) / Math.Log(2)));
			    os.Y = (int)Math.Pow(2, Math.Ceiling(Math.Log(os.Y) / Math.Log(2)));
		    }
		    result.Resize(new Size(os.X, os.Y));

            foreach (Tuple<string, Block> tuple in m_images)
            {
			    Block blk = tuple.Item2;
			    if (blk.Rotated)
                {
				    for (int j = 0; j < blk.Valid.Height; j++)
                    {
					    for (int i = 0; i < blk.Valid.Width; i++)
                        {
						    int x = i + blk.Fit.Rectangle.X + m_padding;
						    int y = j + blk.Fit.Rectangle.Y + m_padding;
						    result.SetPixel(y, x, blk.Texture.GetPixel(i + blk.Valid.X, j + blk.Valid.Y));
					    }
				    }
			    }
                else
                {
				    if (false) // per_pixel
                    {
					    for (int j = 0; j < blk.Valid.Height; j++)
                        {
						    for(int i = 0; i < blk.Valid.Width; i++) {
							    int x = i + blk.Fit.Rectangle.X + m_padding;
							    int y = j + blk.Fit.Rectangle.Y + m_padding;
							    result.SetPixel(x, y, blk.Texture.GetPixel(i + blk.Valid.X, j + blk.Valid.Y));
						    }
					    }
				    } else {
					    result.CopyFrom(
						    blk.Texture,
						    blk.Valid.X, blk.Texture.Height - blk.Valid.Y - blk.Valid.Height,
						    blk.Valid.Width, blk.Valid.Height,
						    blk.Fit.Rectangle.X + m_padding, blk.Fit.Rectangle.Y + m_padding
					    );
				    }
			    }
		    }
	    }

        private Node Find(Node root, int w, int h)
        {
            if (root.Used)
            {
                Node r = Find(root.Right, w, h);
                if (r == null) r = Find(root.Down, w, h);

                return r;
            }
            else if (w <= root.Rectangle.Width && h <= root.Rectangle.Height)
            {
                return root;
            }
            else
            {
                return null;
            }
        }

        private Node Split(Node nd, int w, int h)
        {
            nd.Used = true;
            nd.Down = new Node(nd.Rectangle.X, nd.Rectangle.Y + h, nd.Rectangle.Width, nd.Rectangle.Height - h);
            nd.Right = new Node(nd.Rectangle.X + w, nd.Rectangle.Y, nd.Rectangle.Width - w, h);

            return nd;
        }

        private Node Grow(int w, int h)
        {
            bool can_grow_down = w <= m_rootNode.Rectangle.Width;
            bool can_grow_right = h <= m_rootNode.Rectangle.Height;

            bool should_grow_right = can_grow_right && (m_rootNode.Rectangle.Height >= (m_rootNode.Rectangle.Width + w));
            bool should_grow_down = can_grow_down && (m_rootNode.Rectangle.Width >= (m_rootNode.Rectangle.Height + h));

            if (should_grow_right)
                return GrowRight(w, h);
            else if (should_grow_down)
                return GrowDown(w, h);
            else if (can_grow_right)
                return GrowRight(w, h);
            else if (can_grow_down)
                return GrowDown(w, h);
            else
                return null;
        }

        private Node GrowRight(int w, int h)
        {
            Node r = new Node(0, 0, m_rootNode.Rectangle.Width + w, m_rootNode.Rectangle.Height);
            r.Used = true;
            r.Down = m_rootNode;
            r.Right = new Node(m_rootNode.Rectangle.Width, 0, w, m_rootNode.Rectangle.Height);
            m_rootNode = new Node(r);

            Node node = Find(m_rootNode, w, h);
            if (node != null)
                return Split(node, w, h);
            else
                return null;
        }

        private Node GrowDown(int w, int h)
        {
            Node r = new Node(0, 0, m_rootNode.Rectangle.Width, m_rootNode.Rectangle.Height + h);
            r.Used = true;
            r.Down = new Node(0, m_rootNode.Rectangle.Height, m_rootNode.Rectangle.Width, h);
            r.Right = m_rootNode;
            m_rootNode = new Node(r);

            Node node = Find(m_rootNode, w, h);
            if (node != null)
                return Split(node, w, h);
            else
                return null;
        }

        private int m_padding;
        private bool m_allowRotate;
        private bool m_powerOfTwo;
        private bool m_alphaTrim;
        private Point m_outputTextureSize;

	    private LinkedList<Tuple<string, Block>> m_images;
        private Node m_rootNode;
        private int m_totalArea;
        
        private int m_indent;
    }
}
