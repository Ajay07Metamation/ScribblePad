using DesignCraft.Lib;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace DesignCraft;
public class DrawingEditor {

   #region Implementation---------------------------------------------------------------------------------
   public void Undo () {
      if (mDrawing.Count != 0) {
         int last = mDrawing.Count - 1;
         mUndoRedo.Push (Drawing.Plines[last]);
         mDrawing.Plines.RemoveAt (last);
         EntityCount = mDrawing.Count;
      }
      mDrawing.RedrawReq ();
   }

   public void Redo () {
      if (mUndoRedo.Count != 0) {
         var pLine = mUndoRedo.Pop ();
         mDrawing.AddPline (pLine);
         EntityCount = mDrawing.Count;
      }
      mDrawing.RedrawReq ();
   }

   public void DeleteSelecetedEntity () {
      var deletedPLines = mDrawing.Plines.Where (x => x.IsSelected).ToList ();
      foreach (var pLine in deletedPLines)
         mUndoRedo.Push (pLine);
      mDrawing.Plines.RemoveAll (x => x.IsSelected);
      mDrawing.RedrawReq ();
   }

   // Determine whether undo or redo can be executed
   public void CanRedo (object sender, CanExecuteRoutedEventArgs e) { e.CanExecute = mUndoRedo.Any () && EntityCount == mDrawing.Count; if (!e.CanExecute) mUndoRedo.Clear (); }
   public void CanUndo (object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = mDrawing.Plines.Any ();

   /// <summary>Clear the display</summary>
   public void Clear () {
      if (mDrawing.Count != 0) { mDrawing.Clear (); mUndoRedo.Clear (); mDrawing.RedrawReq (); }
   }


   #endregion

   #region Properties-------------------------------------------------------------------------------------
   public int EntityCount { get => mCount; set => mCount = value; }
   public Drawing Drawing { get => mDrawing; set => mDrawing = value; }
   #endregion

   #region Private Field----------------------------------------------------------------------------------
   Stack<Pline> mUndoRedo = new (); // Stack for redo and undo operation
   Drawing mDrawing = new ();
   int mCount = 0;
   #endregion
}