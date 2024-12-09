using System.Collections.Generic;
using PokerOrganizer.Core.Models;

namespace PokerOrganizer.Core.Interfaces
{
    public interface ILayoutManager
    {
        List<Layout> GetLayouts();
        Layout? GetLayout(string name);
        void SaveLayout(Layout layout);
        void DeleteLayout(string name);
        List<WindowPosition> CalculatePositions(int windowCount, Layout? layout);
    }
} 