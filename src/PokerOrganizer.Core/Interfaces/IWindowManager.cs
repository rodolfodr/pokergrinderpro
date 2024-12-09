using System.Collections.Generic;
using PokerOrganizer.Core.Models;

namespace PokerOrganizer.Core.Interfaces
{
    public interface IWindowManager
    {
        List<PokerWindow> FindPokerWindows();
        bool MoveWindow(PokerWindow window, WindowPosition position);
        WindowPosition? GetWindowPosition(IntPtr handle);
    }
} 