﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;

using MaterialDesignThemes.Wpf;
namespace PWPlanner
{
    public partial class MainWindow : Window
    {
        public static TileData TileDB = new TileData(80, 60);
        public static Line[] vertLines;
        public static Line[] horiLines;

        public static string[] blacklist = new string[] { "Bedrock", "BedrockFlat", "BedrockLava" };

        //Disable/Enable Grid
        private void Grid_Click(object sender, RoutedEventArgs e)
        {
            if (gridButton.IsChecked)
            {
                RemoveGrid();
                gridButton.Icon = PackIconKind.GridOff;
            }
            else
            {
                DrawGrid(TileDB.Height, TileDB.Width);
            }
        }

        //Painter
        public void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
			
			int X = (int)(e.GetPosition(MainCanvas).X) / 32;
			int Y = (int)(e.GetPosition(MainCanvas).Y) / 32;
            int realY = 60 - Y - 1;

            PosLabel.Content = $"X = {X} | Y = {realY}";

            //Last pixel crashes the entire thing. Why? No idea.
            if (Y == 60 || X == 80)
            {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //Check if a tile has been selected before doing anything first.
                if (FirstSelected)
                {
                    //Check if the tile selected can be placed at a position
                    if (!SameTypeAt(_selectedTile, X, Y) && !AlreadyHasBothTypes(X, Y))
                    {
                        //So we can make sure to put a confirm box if the user makes a new world or opens one
                        if (!firstPlaced) firstPlaced = true;

                        PlaceAt(X, Y, _selectedTile);

                    }
                }
            }

            //Delete Tiles
            if (e.RightButton == MouseButtonState.Pressed)
            {
                DeleteAt(X, Y, _selectedTile);
            }

            //Pan with mouse wheel click
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                scrollMousePoint = e.GetPosition(sv);
                hOff = sv.HorizontalOffset;
                vOff = sv.VerticalOffset;
                sv.CaptureMouse();
            }
        }

        //Pass Click to Move + Tile Selector
        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
				int X = (int)(e.GetPosition(MainCanvas).X) / 32;
				int Y = (int)(e.GetPosition(MainCanvas).Y) / 32;

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (GetBackgroundAt(X, Y, out BackgroundName bg))
                    {
                        backgroundMap.TryGetValue(bg, out BitmapImage src);
                        Image image = new Image()
                        {
                            Source = src
                        };
                        _selectedTile = new Tile(TileType.Background, image);
                        _selectedTile.bgName = bg.ToString();
                        SelectTile(TileType.Background, bg.ToString());
                        LabelImg.Source = src;
                        TileHover.Content = bg;

                    }
                } 
                else if (e.RightButton == MouseButtonState.Pressed)
                {
                    GetForegroundAt(X, Y, out BlockName bl);
                    if (GetForegroundAt(X, Y, out bl))
                    {
                        blockMap.TryGetValue(bl, out BitmapImage src);
                        Image image = new Image()
                        {
                            Source = src
                        };

                        _selectedTile = new Tile(TileType.Foreground, image);
                        _selectedTile.blName = bl.ToString();
                        SelectTile(TileType.Foreground, bl.ToString());
                        LabelImg.Source = src;
                        TileHover.Content = bl;
                    }
                }
 
            }
            else
            {
                MainCanvas_MouseMove(sender, e);
            }
        }

        //Zoom
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isRendered)
            {
                ScaleTransform scale;
                if (e.NewValue > e.OldValue)
                {
                    scale = new ScaleTransform(MainCanvas.LayoutTransform.Value.M11 + 0.13, MainCanvas.LayoutTransform.Value.M22 + 0.13);
                }
                else
                {
                    scale = new ScaleTransform(MainCanvas.LayoutTransform.Value.M11 - 0.13, MainCanvas.LayoutTransform.Value.M22 - 0.13);
                }
                MainCanvas.LayoutTransform = scale;
                MainCanvas.InvalidateVisual();
            }
        }

        private void DrawGrid(int height, int width)
        {
            MainCanvas.Height = height * 32;
            MainCanvas.Width = width * 32;

            vertLines = new Line[width];
            for (int i = 32; i <= MainCanvas.Width; i += 32)
            {
                int count = i / 32 - 1;
                vertLines[count] = new Line()
                {
                    Stroke = Brushes.Gray,
                    X1 = i,
                    X2 = i,
                    Y1 = 0,
                    Y2 = MainCanvas.Height,
                    StrokeThickness = 1
                };
                MainCanvas.Children.Add(vertLines[count]);
            }

            horiLines = new Line[height];
            for (int i = 32; i <= MainCanvas.Height; i += 32)
            {
                int count = i / 32 - 1;
                horiLines[count] = new Line()
                {
                    Stroke = Brushes.Gray,
                    X1 = 0,
                    X2 = MainCanvas.Width,
                    Y1 = i,
                    Y2 = i,
                    StrokeThickness = 1
                };
                MainCanvas.Children.Add(horiLines[count]);
            }

            gridButton.IsChecked = true;
        }

        private void RemoveGrid()
        {
            //Temporarily get rid of cache, so the lines doesn't stay until visual update
            CacheMode cm = MainCanvas.CacheMode;
            MainCanvas.CacheMode = null;

            for (int i = 0; i < horiLines.Length - 1; i++)
            {
                MainCanvas.Children.Remove(horiLines[i]);
            }
            for (int i = 0; i < vertLines.Length -1; i++)
            {
                MainCanvas.Children.Remove(vertLines[i]);
            }
            MainCanvas.CacheMode = cm;

            gridButton.IsChecked = false;
        }

        private void DrawBedrock()
        {
            if (TileDB.Width != 80 || TileDB.Height != 60)
            {
                return;
            }
            int location = 2;
            for (int y = TileDB.Height - 1; y > TileDB.Height - 4; y--)
            {
                for (int x = TileDB.Width - 1; x >= 0 ; x--)
                {
                    BlockName bn;
                    if (y == 59) bn = BlockName.BedrockLava;
                    else if (y == 58) bn = BlockName.Bedrock;
                    else bn = BlockName.BedrockFlat;

                    Image image = new Image();
                    blockMap.TryGetValue(bn, out BitmapImage src);
                    image.Source = src;
                    Canvas.SetTop(image, y * 32);
                    Canvas.SetLeft(image, x * 32);
                    image.SetValue(Canvas.ZIndexProperty, 20);
                    MainCanvas.Children.Add(image);
                    
                    TilePosition Position = new TilePosition(TileType.Foreground, x, y);

                    TileDB.Tiles[x, y] = new Tile(TileType.Foreground, bn.ToString(), image, Position);
                }
                location--;
            }
        }

        private void PlaceAt(int X, int Y, Tile tile)
        {
            Image image = new Image()
            {
                Source = selectableTiles[index].source
            };
            Canvas.SetTop(image, Y * 32);
            Canvas.SetLeft(image, X * 32);
            MainCanvas.Children.Add(image);

            switch (tile.Type)
            {
                case TileType.Background:
                    image.SetValue(Canvas.ZIndexProperty, 10);

                    //Check if theres already a bg there
                    if (HasForegroundAt(X, Y))
                    {
                        TileDB.Tiles[X, Y].Type = TileType.Both;
                        TileDB.Tiles[X, Y].Background = image;
                        TileDB.Tiles[X, Y].bgName = selectableTiles[index].bgName;
                        if (TileDB.Tiles[X, Y].Positions == null)
                        {
                            TileDB.Tiles[X, Y].Positions = new TilePosition(tile.Type, X, Y);
                        }
                        else
                        {
                            TileDB.Tiles[X, Y].Positions.SetBackgroundPositions(X, Y);
                        }

                    }
                    else
                    {
                        TilePosition Position = new TilePosition(tile.Type, X, Y);
                        TileDB.Tiles[X, Y] = new Tile(TileType.Background, selectableTiles[index].bgName, image, Position);

                        //Add to previous tiles selected
                        AddToPreviousTilesSelected(_selectedTile, TileType.Background);

                    }
                    break;
                case TileType.Foreground:
                    image.SetValue(Canvas.ZIndexProperty, 20);
                    if (HasBackgroundAt(X, Y))
                    {
                        TileDB.Tiles[X, Y].Type = TileType.Both;
                        TileDB.Tiles[X, Y].Foreground = image;
                        TileDB.Tiles[X, Y].blName = selectableTiles[index].blName;

                        if (TileDB.Tiles[X, Y].Positions == null)
                        {
                            TileDB.Tiles[X, Y].Positions = new TilePosition(tile.Type, X, Y);
                        }
                        else
                        {
                            TileDB.Tiles[X, Y].Positions.SetForegroundPositions(X, Y);
                        }
                    }
                    else
                    {
                        TilePosition Position = new TilePosition(tile.Type, X, Y);
                        TileDB.Tiles[X, Y] = new Tile(TileType.Foreground, selectableTiles[index].blName, image, Position);


                        //Add to previous tiles selected
                        AddToPreviousTilesSelected(_selectedTile, TileType.Foreground);
                    }
                    break;
            }
        }

        public void DeleteAt(int X, int Y, Tile tile)
        {
            if (TileDB.Tiles[X, Y] != null)
            {
                //If there are both types on 1 tile, and the current selected is a background, leave the foreground only
                if ((TileDB.Tiles[X, Y].Type == TileType.Both) && (tile.Type == TileType.Background))
                {
                    MainCanvas.Children.Remove(TileDB.Tiles[X, Y].Background);
                    TileDB.Tiles[X, Y].Type = TileType.Foreground;
                    TileDB.Tiles[X, Y].Background = null;
                    TileDB.Tiles[X, Y].Positions.DeleteBackgroundPositions();
                }
                //If there are both types on 1 tile, and the current selected is a foreground, leave the background only
                else if ((TileDB.Tiles[X, Y].Type == TileType.Both) && (tile.Type == TileType.Foreground))
                {
                    MainCanvas.Children.Remove(TileDB.Tiles[X, Y].Foreground);
                    TileDB.Tiles[X, Y].Type = TileType.Background;
                    TileDB.Tiles[X, Y].Foreground = null;
                    TileDB.Tiles[X, Y].Positions.DeleteForegroundPositions();
                }
                //If there is only one type, just erase the tile.
                else if (TileDB.Tiles[X, Y].Type == tile.Type)
                {
                    switch (tile.Type)
                    {
                        case TileType.Background:
                            MainCanvas.Children.Remove(TileDB.Tiles[X, Y].Background);
                            TileDB.Tiles[X, Y].Positions.DeleteBackgroundPositions();
                            break;
                        case TileType.Foreground:
                            MainCanvas.Children.Remove(TileDB.Tiles[X, Y].Foreground);
                            TileDB.Tiles[X, Y].Positions.DeleteForegroundPositions();
                            break;
                    }
                    TileDB.Tiles[X, Y] = null;
                }
            }
        }

        public bool GetBackgroundAt(int X, int Y, out BackgroundName Background)
        {
            Background = BackgroundName.NONE;
            if (HasBackgroundAt(X, Y))
            {
                Background = SelectableTile.GetBackgroundNameByString(TileDB.Tiles[X, Y].bgName);
                return true;
            }
            return false;
        }

        public bool GetForegroundAt(int X, int Y, out BlockName Foreground)
        {
            Foreground = BlockName.NONE;
            if (HasForegroundAt(X, Y))
            {
                Foreground = SelectableTile.GetBlockNameByString(TileDB.Tiles[X, Y].blName);
                return true;
            }
            return false;
        }

        public bool TileExists(int X, int Y)
        {
            if (TileDB.Tiles[X, Y] != null)
            {
                return true;
            }
            return false;
        }

        public bool HasBackgroundAt(int X, int Y)
        {
            if (TileExists(X, Y))
            {
                if (TileDB.Tiles[X, Y].Background != null)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool HasForegroundAt(int X, int Y)
        {
            if (TileExists(X, Y))
            {
                if (TileDB.Tiles[X, Y].Foreground != null)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool AlreadyHasBothTypes(int X, int Y)
        {
            if (TileExists(X, Y))
            {
                if (TileDB.Tiles[X, Y].Type == TileType.Both)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool SameTypeAt(Tile tile, int X, int Y)
        {
            if (TileDB.Tiles[X, Y] == null)
            {
                return false;
            }
            if (tile.Type == TileDB.Tiles[X, Y].Type)
            {
                return true;
            }
            return false;
        }
    
    }
}
