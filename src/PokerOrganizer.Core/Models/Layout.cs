using System.Collections.Generic;

namespace PokerOrganizer.Core.Models
{
    public class Layout
    {
        public string Name { get; set; } = string.Empty;
        public List<WindowPosition> Positions { get; set; }

        public Layout()
        {
            Positions = new List<WindowPosition>();
        }

        public Layout(string name) : this()
        {
            Name = name;
        }
    }

    public class WindowPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public WindowPosition() { }

        public WindowPosition(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
} 