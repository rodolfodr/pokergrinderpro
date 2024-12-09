using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using PokerOrganizer.Core.Interfaces;
using PokerOrganizer.Core.Models;

namespace PokerOrganizer.Core.Services
{
    public class LayoutManager : ILayoutManager
    {
        private readonly string _layoutsPath;

        public LayoutManager()
        {
            _layoutsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PokerOrganizer",
                "layouts.json"
            );

            var directory = Path.GetDirectoryName(_layoutsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Cria o arquivo se não existir
            if (!File.Exists(_layoutsPath))
            {
                SaveLayouts(new List<Layout>());
            }
        }

        public List<Layout> GetLayouts()
        {
            try
            {
                var json = File.ReadAllText(_layoutsPath);
                return JsonSerializer.Deserialize<List<Layout>>(json) ?? new List<Layout>();
            }
            catch
            {
                return new List<Layout>();
            }
        }

        public Layout? GetLayout(string name)
        {
            var layouts = GetLayouts();
            return layouts.Find(l => l.Name == name);
        }

        public void SaveLayout(Layout layout)
        {
            var layouts = GetLayouts();
            var existingLayout = layouts.FindIndex(l => l.Name == layout.Name);

            if (existingLayout >= 0)
            {
                layouts[existingLayout] = layout;
            }
            else
            {
                layouts.Add(layout);
            }

            SaveLayouts(layouts);
        }

        public void DeleteLayout(string name)
        {
            var layouts = GetLayouts();
            layouts.RemoveAll(l => l.Name == name);
            SaveLayouts(layouts);
        }

        public List<WindowPosition> CalculatePositions(int windowCount, Layout? layout)
        {
            if (layout?.Positions == null || layout.Positions.Count == 0)
            {
                return CalculateDefaultPositions(windowCount);
            }

            var positions = new List<WindowPosition>();

            // Se temos menos janelas que posições no layout, usa as primeiras posições
            if (windowCount <= layout.Positions.Count)
            {
                for (int i = 0; i < windowCount; i++)
                {
                    positions.Add(new WindowPosition(
                        layout.Positions[i].X,
                        layout.Positions[i].Y,
                        layout.Positions[i].Width,
                        layout.Positions[i].Height
                    ));
                }
            }
            // Se temos mais janelas que posições no layout, cria posições extras
            else
            {
                // Primeiro adiciona todas as posições do layout
                positions.AddRange(layout.Positions);

                // Depois cria posições extras em cascata
                var lastPos = layout.Positions[layout.Positions.Count - 1];
                for (int i = layout.Positions.Count; i < windowCount; i++)
                {
                    positions.Add(new WindowPosition(
                        lastPos.X + 25 * (i - layout.Positions.Count + 1),
                        lastPos.Y + 25 * (i - layout.Positions.Count + 1),
                        lastPos.Width,
                        lastPos.Height
                    ));
                }
            }

            return positions;
        }

        private List<WindowPosition> CalculateDefaultPositions(int windowCount)
        {
            var positions = new List<WindowPosition>();
            var screenWidth = 1920; // Largura padrão
            var screenHeight = 1080; // Altura padrão

            try
            {
                // Tenta obter as dimensões reais da tela principal
                var screen = Screen.PrimaryScreen;
                if (screen != null)
                {
                    screenWidth = screen.Bounds.Width;
                    screenHeight = screen.Bounds.Height;
                }
            }
            catch { }

            var windowWidth = 560;
            var windowHeight = 436;

            switch (windowCount)
            {
                case 1:
                    positions.Add(new WindowPosition(
                        (screenWidth - windowWidth) / 2,
                        (screenHeight - windowHeight) / 2,
                        windowWidth,
                        windowHeight
                    ));
                    break;

                case 2:
                    positions.Add(new WindowPosition(
                        screenWidth / 4 - windowWidth / 2,
                        (screenHeight - windowHeight) / 2,
                        windowWidth,
                        windowHeight
                    ));
                    positions.Add(new WindowPosition(
                        3 * screenWidth / 4 - windowWidth / 2,
                        (screenHeight - windowHeight) / 2,
                        windowWidth,
                        windowHeight
                    ));
                    break;

                case 3:
                    positions.Add(new WindowPosition(
                        screenWidth / 2 - windowWidth / 2,
                        screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    positions.Add(new WindowPosition(
                        screenWidth / 4 - windowWidth / 2,
                        3 * screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    positions.Add(new WindowPosition(
                        3 * screenWidth / 4 - windowWidth / 2,
                        3 * screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    break;

                case 4:
                    positions.Add(new WindowPosition(
                        screenWidth / 4 - windowWidth / 2,
                        screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    positions.Add(new WindowPosition(
                        3 * screenWidth / 4 - windowWidth / 2,
                        screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    positions.Add(new WindowPosition(
                        screenWidth / 4 - windowWidth / 2,
                        3 * screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    positions.Add(new WindowPosition(
                        3 * screenWidth / 4 - windowWidth / 2,
                        3 * screenHeight / 4 - windowHeight / 2,
                        windowWidth,
                        windowHeight
                    ));
                    break;

                default:
                    // Para 5 ou mais janelas, cria um layout em grade
                    var cols = (int)Math.Ceiling(Math.Sqrt(windowCount));
                    var rows = (int)Math.Ceiling(windowCount / (double)cols);

                    for (int i = 0; i < windowCount; i++)
                    {
                        var row = i / cols;
                        var col = i % cols;

                        positions.Add(new WindowPosition(
                            col * (screenWidth / cols) + (screenWidth / cols - windowWidth) / 2,
                            row * (screenHeight / rows) + (screenHeight / rows - windowHeight) / 2,
                            windowWidth,
                            windowHeight
                        ));
                    }
                    break;
            }

            return positions;
        }

        private void SaveLayouts(List<Layout> layouts)
        {
            var json = JsonSerializer.Serialize(layouts, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_layoutsPath, json);
        }
    }
} 