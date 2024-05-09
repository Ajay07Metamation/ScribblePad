using DesignLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;

namespace DesignCraft;

public class DocManager {

   #region Implementation ------------------------------------------------------------------------
   public void Save () {
      if (!IsSaved) {
         SaveFileDialog save = new () { FileName = "Untitled", Filter = "Binary files (*.bin)|*.bin" };
         if (save.ShowDialog () == DialogResult.OK) Save (save.FileName);
         (IsSaved, SavedFileName) = (true, save.FileName);
      } else Save (SavedFileName);
   }
   void Save (string path) {
      if (Drawing.Count > 0)
         using (BinaryWriter bw = new (File.Open (path, FileMode.Create))) {
            bw.Write (Drawing.Count);
            foreach (var pLine in Drawing.Plines) {
               var points = pLine.GetPoints ();
               bw.Write (points.Count ());
               foreach (var point in points) {
                  bw.Write (point.X);
                  bw.Write (point.Y);
               }
            }
         }
   }

   /// <summary>Load the drawing</summary>
   public void Load () {
      OpenFileDialog load = new () { FileName = "Untitled.bin", Filter = "Binary files (*.bin)|*.bin" };
      if (load.ShowDialog () == DialogResult.OK) {
         string path = load.FileName;
         if (path != null) {
            Load (path);
            (IsSaved, SavedFileName) = (true, path);
         }
      }
   }
   public void Clear () {
      if (mDrawing.Count != 0) mDrawing.Clear ();
   }
   void Load (string filePath) {
      Clear ();
      using (BinaryReader reader = new (File.Open (filePath, FileMode.Open))) {
         var pLineCount = reader.ReadInt32 (); // Total drawing count
         for (int i = 0; i < pLineCount; i++) {
            var pCount = reader.ReadInt32 ();
            List<Point> points = new ();
            for (int j = 0; j < pCount; j++) {
               var (x, y) = (reader.ReadDouble (), reader.ReadDouble ());
               points.Add (PointConverter.ToCustomPoint (x, y));
            }
            Drawing.AddPline (new Pline (points));
         }
         Drawing.RedrawReq ();
      }
   }
   #endregion

   #region Properties ----------------------------------------------------------------------------
   public string SavedFileName { get => mSavedFileName; set => mSavedFileName = value; }
   public bool IsSaved { get => mIsSaved; set => mIsSaved = value; }
   public Drawing Drawing { get => mDrawing; set => mDrawing = value; }
   #endregion

   #region Private field -------------------------------------------------------------------------
   Drawing mDrawing = new ();
   bool mIsSaved = false;
   string mSavedFileName = string.Empty;
   #endregion
}