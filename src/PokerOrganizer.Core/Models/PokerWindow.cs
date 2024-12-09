using System;

namespace PokerOrganizer.Core.Models
{
    public class PokerWindow
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public PokerRoom Room { get; set; }

        public PokerWindow(IntPtr handle, string title, int x, int y, int width, int height, PokerRoom room)
        {
            Handle = handle;
            Title = title;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Room = room;
        }
    }
} 