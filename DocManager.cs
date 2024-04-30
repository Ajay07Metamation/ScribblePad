using DesignLib;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Line = DesignLib.Line;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using Rectangle = DesignLib.Rectangle;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using Shape = DesignLib.Shape;

namespace DesignCraft;
class DocManager {
   #region Constructor --------------------------------------------------------------------------------------
   public DocManager (List<Shape> entityList) { mEntityCollection = entityList; mDrawing = new (entityList); }
   #endregion

   #region Implementation------------------------------------------------------------------------------------
   #region Save & Load----------------------------------------------------------------------------------------
   /// <summary>Save the drawing</summary>
   public void Save () {
      if (!IsSaved) {
         SaveFileDialog save = new () {
            FileName = "Untitled.bin",
            Filter = "Binary files (*.bin)|*.bin"
         };
         if (save.ShowDialog () == DialogResult.OK) Save (save.FileName);
         (IsSaved, mCurrentFilePath) = (true, save.FileName);
      } else Save (mCurrentFilePath);
   }
   void Save (string path) {
      if (mEntityCollection.Count > 0)
         using (BinaryWriter bw = new (File.Open (path, FileMode.Create))) {
            bw.Write (mEntityCollection.Count);
            foreach (var shape in mEntityCollection) {
               bw.Write (shape.Type);
               bw.Write (shape.Brush.Item1); bw.Write (shape.Brush.Item2); bw.Write (shape.Brush.Item3); bw.Write (shape.Brush.Item4);
               shape.Save (bw);
            }
         }
   }

   /// <summary>Load the drawing</summary>
   public void Load () {
      OpenFileDialog load = new () {
         FileName = "Untitled.bin",
         Filter = "Binary files (*.bin)|*.bin"
      };
      if (load.ShowDialog () == DialogResult.OK) {
         string path = load.FileName;
         if (path != null) {
            Load (path);
            (IsSaved, mCurrentFilePath) = (true, path);
         }
      }
   }
   void Clear () {
      if (mEntityCollection.Count != 0) mEntityCollection.Clear ();
   }
   void Load (string filePath) {
      Clear ();
      using (BinaryReader reader = new (File.Open (filePath, FileMode.Open))) {
         Shape shape = null;
         var sCount = reader.ReadInt32 (); // Total drawing count
         for (int i = 0; i < sCount; i++) {
            var type = reader.ReadInt32 ();
            switch (type) {
               case 1: shape = new CLine (); break;
               case 2: shape = new Line (); break;
               case 3: shape = new Rectangle (); break;
               case 4: shape = new Circle (); break;
            }
            mEntityCollection.Add (shape.Load (reader));
         }
      }
   }

   // Save and clear the display
   void SaveAndClear () { Save (); Clear (); }
   #endregion

   /// <summary>Keyboard shortcuts for commands</summary>
   /// <param name="sender"></param>
   /// <param name="e"></param>
   public void Shortcuts (object sender, System.Windows.Input.KeyEventArgs e) {
      if (Keyboard.Modifiers == ModifierKeys.Control)
         switch (e.Key) {
            case Key.N: CheckAndPrompt (); mCurrentFilePath = ""; break;
            case Key.S: Save (); break;
            case Key.O: Load (); break;
         } else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.F4) {
         Window window = new ();
         window.Close ();
      }
   }

   /// <summary>Provides user prompt</summary>
   /// Provides user prompt to save changes made to the drawing
   /// <returns></returns>
   public bool CheckAndPrompt () {
      bool isCanceled = false;
      MessageBoxResult messageBox = System.Windows.MessageBox.Show ("Do you want to save your changes?", "Scribble Pad",
                                                     MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      switch (messageBox) {
         case MessageBoxResult.Yes: SaveAndClear (); break;
         case MessageBoxResult.No: Clear (); break;
         case MessageBoxResult.Cancel: isCanceled = true; break;
      }
      return isCanceled;
   }
   #endregion

   #region Properties----------------------------------------------------------------------------------------
   public bool IsSaved { get; set; } // Indicate if the drawing is saved
   public string TitleName => mCurrentFilePath;
   #endregion

   #region Private Fields------------------------------------------------------------------------------------
   DrawingEditor mDrawing;
   string mCurrentFilePath = "Untitled.bin"; // Current file path where the drawing is saved
   List<Shape> mEntityCollection;
   #endregion
}