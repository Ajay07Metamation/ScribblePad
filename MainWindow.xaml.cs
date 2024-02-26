using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;


namespace ScribblePad {
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window {
      public MainWindow () {
         InitializeComponent ();
         mPen = new Pen (Brushes.White, 3);
      }

      #region SelectShape-------------------------------------------------------------------------

      // Select the type of shape
      void Pen_Click (object sender, RoutedEventArgs e) => mType = 2;
      void Line_Click (object sender, RoutedEventArgs e) => mType = 3;
      void Rectangle_Click (object sender, RoutedEventArgs e) => mType = 4;
      void Ellipse_Click (object sender, RoutedEventArgs e) => mType = 5;
      #endregion

      #region Undo-Redo-------------------------------------------------------------------------
      /// <summary>Undo a scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Undo_Click (object sender, RoutedEventArgs e) {
         if (mShapes.Count != 0) {
            int last = mShapes.Count - 1;
            mUndoRedo.Push (mShapes[last]);
            mShapes.RemoveAt (last);
            InvalidateVisual ();
         }
      }

      /// <summary>Redo a scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Redo_Click (object sender, RoutedEventArgs e) {
         if (mUndoRedo.Count != 0) {
            var data = mUndoRedo.Pop ();
            mShapes.Add (data);
            InvalidateVisual ();
         }
      }
      #endregion

      #region PenColour---------------------------------------------------------------------------
      /// <summary>Changes the colour of the pen</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void PenColor_Click (object sender, RoutedEventArgs e) {
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
      void Eraser_Click (object sender, RoutedEventArgs e) {
         mType = 1;
         mPen.Brush = Brushes.Black;
      }
      #endregion

      #region Thickness-------------------------------------------------------------------------
      /// <summary>Change thickness of scribble</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void ThicknessSlider_ValueChanged (object sender, RoutedPropertyChangedEventArgs<double> e) {
         if (mPen != null) {
            mPen.Thickness = e.NewValue;
         }
      }
      #endregion

      #region Clear-------------------------------------------------------------------------
      /// <summary>Clear drawing on the screen</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Clear_Click (object sender, RoutedEventArgs e) {
         ClearDrawing ();
         InvalidateVisual ();
      }

      /// <summary>Clear the scribbles list and undo and redo stack</summary>
      void ClearDrawing () {
         if (mShapes.Count != 0) {
            mShapes.Clear ();
            mUndoRedo.Clear ();
         }
      }
      #endregion

      #region Save-------------------------------------------------------------------------------
      /// <summary>Save the drawing</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Save_Click (object sender, RoutedEventArgs e) {
         SaveFileDialog save = new () {
            FileName = "scribble.txt",
            Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin"
         };
         if (save.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            if (save.FilterIndex == 1) SaveText (save.FileName);
            else SaveBinary (save.FileName);
         }
      }

      /// <summary>Save the drawing as a bin file</summary>
      /// <param name="path">Path of the bin file</param>
      void SaveBinary (string path) {
         if (mShapes.Count > 0)
            using (BinaryWriter tw = new (File.Open (path, FileMode.Create))) {
               tw.Write (mShapes.Count); // Total drawing count
               foreach (var shape in mShapes) {
                  tw.Write (shape.Type); // Type of shape
                  if (shape is Scribble scribble) scribble.SaveBinary (tw);
                  else if (shape is Line line) line.SaveBinary (tw);
                  else if (shape is Rectangle rectangle) rectangle.SaveBinary (tw);
                  else if (shape is Ellipse ellipse) ellipse.SaveBinary (tw);
               }
            }
      }

      /// <summary>Save the drawing as a text file</summary>
      /// <param name="path">Path of the text file</param>
      void SaveText (string path) {
         if (mShapes.Count > 0) {
            using (TextWriter tw = new StreamWriter (path, true)) {
               tw.WriteLine (mShapes.Count); // Total drawing count
               foreach (var shape in mShapes) {
                  tw.WriteLine (shape.Type); // Type of shape
                  if (shape is Scribble scribble) scribble.SaveText (tw);
                  else if (shape is Line line) line.SaveText (tw);
                  else if (shape is Rectangle rectangle) rectangle.SaveText (tw);
                  else if (shape is Ellipse ellipse) ellipse.SaveText (tw);
               }
            }
         }
      }
      #endregion

      #region Load-------------------------------------------------------------------------------

      /// <summary>Load the drawing</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Open_Click (object sender, RoutedEventArgs e) {
         OpenFileDialog load = new () {
            FileName = "scribble.txt",
            Filter = "Text files (*.txt)|*.txt|Binary files (*.bin)|*.bin"
         };
         if (load.ShowDialog () == System.Windows.Forms.DialogResult.OK) {
            string path = load.FileName;
            if (path != null) {
               if (load.FilterIndex == 1) LoadText (path);
               else LoadBinary (path);
            }
         }
      }

      /// <summary>Load the drawing from a bin file</summary>
      /// <param name="filePath">Path of the bin file</param>
      void LoadBinary (string filePath) {
         ClearDrawing ();
         using (BinaryReader reader = new (File.Open (filePath, FileMode.Open))) {
            Shapes shape = null;
            var sCount = reader.ReadInt32 (); // Total drawing count
            for (int i = 0; i < sCount; i++) {
               var type = reader.ReadInt32 (); // Type of shape
               switch (type) {
                  case 1:
                     shape = new Scribble (new Pen (Brushes.Black, 1));
                     break;
                  case 2:
                     shape = new Line (new Pen (Brushes.Black, 1));
                     break;
                  case 3:
                     shape = new Rectangle (new Pen (Brushes.Black, 1));
                     break;
                  case 4:
                     shape = new Ellipse (new Pen (Brushes.Black, 1));
                     break;
               }
               mShapes.Add (shape.LoadBinary (reader));
            }
         }
         InvalidateVisual ();
      }

      /// <summary>Load the drawing from a text file</summary>
      /// <param name="filePath">Path of the text file</param>
      void LoadText (string filePath) {
         ClearDrawing ();
         using (StreamReader reader = new (filePath)) {
            Shapes shape = null;
            int.TryParse (reader.ReadLine (), out int dCount); // Total drawing count
            for (int i = 0; i < dCount; i++) {
               int.TryParse (reader.ReadLine (), out int type); // Type of shape
               switch (type) {
                  case 1:
                     shape = new Scribble (new Pen (Brushes.Black, 1));
                     break;
                  case 2:
                     shape = new Line (new Pen (Brushes.Black, 1));
                     break;
                  case 3:
                     shape = new Rectangle (new Pen (Brushes.Black, 1));
                     break;
                  case 4:
                     shape = new Ellipse (new Pen (Brushes.Black, 1));
                     break;
               }
               mShapes.Add (shape.LoadText (reader));
            }
         }
         InvalidateVisual ();
      }
      #endregion

      #region Drawing-------------------------------------------------------------------------------
      /// <summary>To start the drawing and collect the start point</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Drawing_MouseDown (object sender, MouseButtonEventArgs draw) {
         if (draw.LeftButton == MouseButtonState.Pressed) {
            mIsDrawing = true;
            if (mUndoRedo.Count > 0) mUndoRedo.Clear ();
            var startPoint = draw.GetPosition (this);// Start point of the drawing
            switch (mType) {
               case 1 or 2: // Erase or scribble
                  mScribble = new (mPen);
                  mShapes.Add (mScribble);
                  mScribble.AddPoints (startPoint);
                  break;
               case 3: // Line
                  mLine = new (mPen);
                  mShapes.Add (mLine);
                  mLine.StartPoint = startPoint;
                  break;
               case 4: // Rectangle
                  mRectangle = new (mPen);
                  mShapes.Add (mRectangle);
                  mRectangle.StartPoint = startPoint;
                  break;
               case 5: // Ellipse
                  mEllipse = new (mPen);
                  mShapes.Add (mEllipse);
                  mEllipse.StartPoint = startPoint;
                  break;
            }
         }
      }

      /// <summary>Add drawing to the list when mouse is released</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Drawing_MouseUp (object sender, MouseButtonEventArgs draw) {
         if (mIsDrawing && draw.ButtonState == MouseButtonState.Released) {
            Point endPoint = draw.GetPosition (this);
            switch (mType) {
               case 1 or 2: // Erase or scribble
                  mScribble.AddPoints (endPoint);
                  break;
               case 3: // Line
                  mLine.EndPoint = endPoint;
                  break;
               case 4: // Rectangle
                  mRectangle.EndPoint = endPoint;
                  break;
               case 5: // Ellipse
                  mEllipse.EndPoint = endPoint;
                  break;
            }
            InvalidateVisual ();
            mIsDrawing = false;
            if (mType == 1) mPen.Brush = Brushes.White; // Set default colour to white after erasing
         }
      }

      /// <summary>Update state of drawing and collect current point</summary>
      /// <param name="sender"></param>
      /// <param name="e"></param>
      void Drawing_MouseMove (object sender, MouseEventArgs draw) {
         if (mIsDrawing && draw.LeftButton == MouseButtonState.Pressed) {
            Point currentPoint = draw.GetPosition (this);
            switch (mType) {
               case 1 or 2: // Erase or scribble
                  mScribble.AddPoints (currentPoint);
                  break;
               case 3: // Line
                  mLine.EndPoint = currentPoint;
                  break;
               case 4: // Rectangle
                  mRectangle.EndPoint = currentPoint;
                  break;
               case 5: // Ellipse
                  mEllipse.EndPoint = currentPoint;
                  break;
            }
            InvalidateVisual ();
         }
      }

      /// <summary>To render the drawings on the display</summary>
      /// <param name="drawingContext">Drawing context variable to render the drawing</param>
      protected override void OnRender (DrawingContext drawingContext) {
         foreach (var shape in mShapes) {
            shape.Draw (drawingContext);
         }
      }
      #endregion

      #region Private Data-------------------------------------------------------------------------------
      Pen mPen; // Pen with brush colour and thickness
      Stack<Shapes> mUndoRedo = new (); // Stack to undo and redo shapes
      bool mIsDrawing = false; // boolean variable to check if it is drawing
      List<Shapes> mShapes = new (); // List of shapes
      int mType = 2; // Indicate the type of shape (Erase Scribble - 1, Scribble - 2, Line - 3, Rectangle - 4, Ellipse - 5) default - scribble
      Scribble mScribble;
      Line mLine;
      Rectangle mRectangle;
      Ellipse mEllipse;
      #endregion
   }
}
