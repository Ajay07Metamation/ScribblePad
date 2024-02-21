using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;


namespace Scribble {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow () {
            InitializeComponent ();
            mPen = new Pen (Brushes.White, 3);
            mEraser = new Pen (Brushes.Black, 5);
        }

        #region PenColour---------------------------------------------------------------------------
        // Tooltip for pencolour
        private void PenColour_MouseEnter (object sender, MouseEventArgs e) =>
            PenColour.ToolTip = "Pen colour";

        /// <summary>Changes the colour of the pen</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PenColour_Click (object sender, RoutedEventArgs e) {
            mPoints = new List<Point> ();
            mErasedPoints = new List<Point> ();
            InvalidateVisual ();
            var colour = new ColorDialog ();
            if (colour.ShowDialog () == System.Windows.Forms.DialogResult.OK)
                mPen.Brush = new SolidColorBrush (Color.FromArgb (colour.Color.A, colour.Color.R,
                                                                  colour.Color.G, colour.Color.B));
        }
        #endregion

        #region Eraser-------------------------------------------------------------------------------
        /// <summary>Choose the eraser tool</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Eraser_Click (object sender, RoutedEventArgs e) =>
            mIsEraser = true;

        // Tooltip for eraser
        private void Eraser_MouseEnter (object sender, MouseEventArgs e) =>
            Eraser.ToolTip = "Eraser";
        #endregion


        #region Save-------------------------------------------------------------------------------
        /// <summary>Save the drawing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click (object sender, RoutedEventArgs e) {
            if (mDrawing.Count == 0) {
                var brush = mPen.Brush.Clone ();
                mDrawing.Add ((brush, mPoints, mErasedPoints));
            }
            SaveFileDialog save = new ();
            save.FileName = "scribble.txt";
            save.Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin";
            if (save.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
                switch (save.FilterIndex) {
                    case 1:
                        SaveText (save.FileName);
                        break;
                    case 2:
                        SaveBinary (save.FileName);
                        break;
                }
            }
        }

        /// <summary>Save the drawing as a bin file</summary>
        /// <param name="path">Path of the bin file</param>
        void SaveBinary (string path) {
            if (mDrawing.Count > 0)
                using (BinaryWriter tw = new BinaryWriter (File.Open (path, FileMode.Create))) {
                    tw.Write (mDrawing.Count);
                    foreach (var (brush, points, eraser) in mDrawing) {
                        if (brush is SolidColorBrush sb) {
                            tw.Write (sb.Color.A);
                            tw.Write (sb.Color.R);
                            tw.Write (sb.Color.G);
                            tw.Write (sb.Color.B);
                        }
                        tw.Write (points.Count);
                        foreach (var point in points) {
                            tw.Write (point.X);
                            tw.Write (point.Y);
                        }
                        tw.Write (eraser.Count);
                        foreach (var point in eraser) {
                            tw.Write (point.X);
                            tw.Write (point.Y);
                        }
                    }
                }
        }

        /// <summary>Save the drawing as a text file</summary>
        /// <param name="path">Path of the text file</param>
        void SaveText (string path) {
            if (mDrawing.Count > 0) {
                using (TextWriter tw = new StreamWriter (path, true)) {
                    tw.WriteLine (mDrawing.Count);
                    foreach (var (brush, points, eraser) in mDrawing) {
                        tw.WriteLine (brush.ToString ());
                        tw.WriteLine (points.Count);
                        foreach (var point in points) {
                            tw.WriteLine (point.X);
                            tw.WriteLine (point.Y);
                        }
                        tw.WriteLine (eraser.Count);
                        foreach (var point in eraser) {
                            tw.WriteLine (point.X);
                            tw.WriteLine (point.Y);
                        }
                    }
                }
            }
        }
        #endregion

        #region Load-------------------------------------------------------------------------------
        /// <summary>Load the drawing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Click (object sender, RoutedEventArgs e) {
            OpenFileDialog load = new ();
            load.Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin";
            if (load.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
                string path = load.FileName;
                switch (load.FilterIndex) {
                    case 1:
                        LoadText (path);
                        break;
                    case 2:
                        LoadBinary (path);
                        break;
                }
            }

        }

        /// <summary>Load the drawing from a bin file</summary>
        /// <param name="filePath">Path of the bin file</param>
        void LoadBinary (string filePath) {
            mDrawing.Clear ();
            using (BinaryReader reader = new BinaryReader (File.Open (filePath, FileMode.Open))) {
                var sCount = reader.ReadInt32 ();
                for (int i = 0; i < sCount; i++) {
                    SolidColorBrush brush = new SolidColorBrush ();
                    List<Point> points = new List<Point> ();
                    List<Point> erased = new List<Point> ();
                    byte a = reader.ReadByte ();
                    byte r = reader.ReadByte ();
                    byte g = reader.ReadByte ();
                    byte b = reader.ReadByte ();
                    brush.Color = Color.FromArgb (a, r, g, b);
                    int pCount = reader.ReadInt32 ();
                    for (int j = 0; j < pCount; j++) {
                        (var x, var y) = (reader.ReadDouble (), reader.ReadDouble ());
                        Point p = new Point (x, y);
                        points.Add (p);
                    }
                    int eCount = reader.ReadInt32 ();
                    for (int k = 0; k < eCount; k++) {
                        (var x, var y) = (reader.ReadDouble (), reader.ReadDouble ());
                        Point p = new Point (x, y);
                        erased.Add (p);
                    }
                    mDrawing.Add ((brush, points, erased));
                }
            }
            InvalidateVisual ();
        }

        /// <summary>Load the drawing from a text file</summary>
        /// <param name="filePath">Path of the text file</param>
        void LoadText (string filePath) {
            mDrawing.Clear ();
            using (StreamReader reader = new StreamReader (filePath)) {
                int.TryParse (reader.ReadLine (), out int dCount);
                for (int i = 0; i < dCount; i++) {
                    List<Point> points = new List<Point> ();
                    List<Point> erased = new List<Point> ();
                    SolidColorBrush brush = new SolidColorBrush ();
                    string line = reader.ReadLine ();
                    brush = new SolidColorBrush ((Color)ColorConverter.ConvertFromString (line));
                    int.TryParse (reader.ReadLine (), out int pCount);
                    for (int j = 0; j < pCount; j++) {
                        double.TryParse (reader.ReadLine (), out double x);
                        double.TryParse (reader.ReadLine (), out double y);
                        Point point = new Point (x, y);
                        points.Add (point);
                    }
                    int.TryParse (reader.ReadLine (), out int eCount);
                    for (int k = 0; k < eCount; k++) {
                        double.TryParse (reader.ReadLine (), out double x);
                        double.TryParse (reader.ReadLine (), out double y);
                        Point point = new Point (x, y);
                        erased.Add (point);
                    }
                    mDrawing.Add ((brush, points, erased));
                }
            }
            InvalidateVisual ();
        }
        #endregion

        #region Drawing-------------------------------------------------------------------------------
        /// <summary>To start the drawing and collect the start point</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown (object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                if (!mIsDrawing && !mIsEraser) {
                    mPoints = new List<Point> ();
                    mErasedPoints = new List<Point> ();
                }
                mIsDrawing = !mIsEraser;
                mStartPoint = e.GetPosition (this);
                if (mIsDrawing) mPoints.Add (mStartPoint);
                else mErasedPoints.Add (mStartPoint);
            }
        }

        /// <summary>Add drawing to the list when mouse is released</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseUp (object sender, MouseButtonEventArgs e) {
            if (e.ButtonState == MouseButtonState.Released) {
                if (mIsDrawing) mIsDrawing = false;
                if (!mIsDrawing && !mIsEraser) {
                    var brush = mPen.Brush.Clone ();
                    mDrawing.Add ((brush, mPoints, mErasedPoints));
                    InvalidateVisual ();
                }
                mIsEraser = false;
            }
        }

        /// <summary>Update state of drawing and collect current point</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseMove (object sender, MouseEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                Point currentPoint = e.GetPosition (this);
                if (mIsDrawing) mPoints.Add (currentPoint);
                else mErasedPoints.Add (currentPoint);
                InvalidateVisual ();
            }
        }

        /// <summary>To render the points on the display</summary>
        /// <param name="drawingContext">Drawing context variable to draw to lines between each point</param>
        protected override void OnRender (DrawingContext drawingContext) {
            if (mDrawing.Count > 0) {
                foreach (var (brush, points, erase) in mDrawing) {
                    for (int i = 1; i < points.Count; i++)
                        drawingContext.DrawLine (new Pen (brush, mPen.Thickness), points[i - 1], points[i]);
                    for (int i = 1; i < erase.Count; i++)
                        drawingContext.DrawLine (mEraser, erase[i - 1], erase[i]);
                }
            }
            for (int i = 1; i < mPoints.Count; i++)
                drawingContext.DrawLine (mPen, mPoints[i - 1], mPoints[i]);
            for (int i = 1; i < mErasedPoints.Count; i++)
                drawingContext.DrawLine (mEraser, mErasedPoints[i - 1], mErasedPoints[i]);
        }
        #endregion

        #region Private Data-------------------------------------------------------------------------------
        Point mStartPoint; // To capture startpoint
        Pen mPen, mEraser; // Pen with brush colour and thickness
        List<(Brush, List<Point>, List<Point>)> mDrawing = new (); // List to store all scribbles
        List<Point> mPoints = new (); // List of scribbled points
        List<Point> mErasedPoints = new (); // List of erased points
        bool mIsDrawing = false, mIsEraser = false; // boolean variable to check if it is drawing or erasing 
        #endregion
    }
}
