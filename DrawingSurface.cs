﻿using DesignCraft.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using Point = DesignCraft.Lib.Point;

namespace DesignCraft;
public class DrawingSurface : Canvas {

   #region OnRender override --------------------------------------------------------------------------------
   protected override void OnRender (DrawingContext dc) {
      base.OnRender (dc);
      var dwgCom = DrawingCommands.GetInstance;
      (dwgCom.DC, dwgCom.Xfm, dwgCom.Brush) = (dc, Xfm, Brushes.Red);
      EntityBuilder?.Draw (dwgCom);
      dwgCom.Brush = Brushes.Black;
      Drawing?.Draw (dwgCom);
   }
   #endregion

   #region Field --------------------------------------------------------------------------------------------
   public Drawing Drawing;
   public Widget EntityBuilder;
   public Matrix Xfm, mProjXfm = Matrix.Identity, mInvProjXfm = Matrix.Identity;
   #endregion
}
public class DrawingCommands {

   #region Constructors -------------------------------------------------------------------------------------
   private DrawingCommands () { }
   #endregion

   #region Implementation -----------------------------------------------------------------------------------
   public void DrawLines (IEnumerable<Point> dwgPts) {
      var itr = dwgPts.GetEnumerator ();
      if (!itr.MoveNext ()) return;
      var prevPt = itr.Current;
      while (itr.MoveNext ()) {
         DrawLine (prevPt, itr.Current, Brush);
         prevPt = itr.Current;
      }
   }
   public void DrawLine (Point startPt, Point endPt, Brush brush) {
      var pen = new Pen (brush, 1);
      mDc.DrawLine (pen, mXfm.Transform (PointOperation.ToSystemPoint (startPt)), mXfm.Transform (PointOperation.ToSystemPoint (endPt)));
   }
   #endregion

   #region Properties ---------------------------------------------------------------------------------------
   public static DrawingCommands GetInstance { get { mDrawingCommands ??= new DrawingCommands (); return mDrawingCommands; } }
   public DrawingContext DC { set => mDc = value; }
   public Matrix Xfm { set => mXfm = value; }
   public Brush Brush { get; set; }
   #endregion

   #region Private Field ------------------------------------------------------------------------------------
   Matrix mXfm;
   DrawingContext mDc;
   static DrawingCommands mDrawingCommands;
   #endregion
}

public interface IDrawable {
   public Action RedrawReq { get; set; }
   public abstract void Draw (DrawingCommands drawingCommands);
}

public class Drawing : IDrawable {

   #region Implementation ----------------------------------------------------------------------------------
   public void AddPline (Pline pline) {
      mPlines.Add (pline);
      Bound = new Bound (mPlines.Select (pline => pline.Bound));
      RedrawReq ();
   }

   public virtual void Draw (DrawingCommands drawingCommands) {
      foreach (var pline in mPlines)
         drawingCommands.DrawLines (pline.GetPoints ());
   }
   public void Clear () => mPlines.Clear ();
   #endregion

   #region Properties --------------------------------------------------------------------------------------
   public Bound Bound { get; private set; }
   public Action RedrawReq { get; set; }
   public int Count => mPlines.Count;
   public List<Pline> Plines => mPlines;
   #endregion

   #region Private Field -----------------------------------------------------------------------------------
   readonly List<Pline> mPlines = new ();
   #endregion
}