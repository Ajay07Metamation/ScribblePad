using DesignLib;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Line = DesignLib.Line;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Rectangle = DesignLib.Rectangle;
using Shape = DesignLib.Shape;
using TextBox = System.Windows.Controls.TextBox;

namespace DesignCraft;
public abstract class Widget : INotifyPropertyChanged {
   public Widget () { }
   public Widget (Paint eventSource) {
      mEventSource = eventSource;
      Initialize ();
   }

   #region Mouse Events --------------------------------------------------------------------------------------
   public void Attach () {
      mEventSource.MouseDown += MEventSource_MouseDown;
      mEventSource.MouseMove += MEventSource_MouseMove;
   }
   public void Detach () {
      mEventSource.MouseDown -= MEventSource_MouseDown;
      mEventSource.MouseMove -= MEventSource_MouseMove;
   }
   public void MEventSource_MouseDown (object sender, MouseButtonEventArgs e) {
      var point = e.GetPosition (mEventSource);
      var newPoint = new DesignLib.Point (point.X, point.Y);
      if (!mIsStartPointSet) {
         mEventSource.CurrentEntity = mCurrentEntity;
         mCurrentEntity.StartPoint = newPoint;
         (X, Y) = (point.X, point.Y);
         mIsStartPointSet = true;
      } else if (mIsStartPointSet && mCurrentEntity is not CLine) {
         mCurrentEntity.EndPoint = newPoint;
         UpdateInputs ();
         Reset ();
      }
      if (mCurrentEntity is CLine) {
         mCurrentEntity.EndPoint = newPoint;
         mCurrentEntity.AddPoint (newPoint);
         UpdateInputs ();
         mCurrentEntity.StartPoint = newPoint;
         (X, Y) = (point.X, point.Y);
         mEventSource.InvalidateVisual ();
      }
      if (mPrompts != null && mPIndex < mPrompts.Length) Prompt = mPrompts[mPIndex++];
      IsModified = true;
   }
   public void MEventSource_MouseMove (object sender, MouseEventArgs e) {
      if (mIsStartPointSet) {
         var point = e.GetPosition (mEventSource);
         mCurrentEntity.EndPoint = new (point.X, point.Y);
         mEventSource.InvalidateVisual ();
      }
   }
   public virtual void Reset () {
      mIsStartPointSet = false;
      AddEnitity ();
      mEventSource.CurrentEntity = null;
      mEventSource.InvalidateVisual ();
      mPIndex = 0;
   }
   void Initialize () {
      GetElements ();
      UpdateInputBar ();
      mEventSource.CurrentEntity = null;
      mEventSource.InvalidateVisual ();
   }
   protected abstract void UpdateInputs ();

   #endregion

   #region Entity --------------------------------------------------------------------------------------------
   public void EndEntity () { (mCurrentEntity.StartPoint, mCurrentEntity.EndPoint) = (new (0, 0), new (0, 0)); Reset (); }

   void AddEnitity () {
      mCurrentEntity.Brush = (255, 0, 0, 0);
      mEventSource.AddEntity (mCurrentEntity);
   }
   #endregion
   void GetElements () => InputBar = ((MainWindow)((DockPanel)((Border)mEventSource.Parent).Parent).Parent).mInputBar;
   private void UpdateInputBar () {
      InputBar.Children.Clear ();
      var tblock = new TextBlock () {
         Margin = new Thickness (10, 5, 10, 0),
         FontWeight = FontWeights.Bold,
      };
      tblock.SetBinding (TextBlock.TextProperty, new Binding (nameof (Prompt)) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
      InputBar.Children.Add (tblock);
      for (int i = 0, len = InputBox.Length; i < len; i++) {
         var str = InputBox[i];
         var tBlock = new TextBlock () { Text = str + ":", Margin = new Thickness (5, 5, 5, 0) };
         var tBox = new TextBox () {
            Name = str + "TextBox",
            Width = 50,
            Height = 20,
         };
         tBox.PreviewKeyDown += Tb_PreviewKeyDown;
         var binding = new Binding (str) { Source = this, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
         tBox.SetBinding (TextBox.TextProperty, binding);
         InputBar.Children.Add (tBlock);
         InputBar.Children.Add (tBox);
      }
   }
   public virtual void CreateEntity () {
      (mCurrentEntity.StartPoint, mCurrentEntity.EndPoint) = (new (X, Y), new (X + DX, Y + DY));
      Reset ();
      mEventSource.InvalidateVisual ();
   }
   private void Tb_PreviewKeyDown (object sender, KeyEventArgs e) {
      var key = e.Key;
      e.Handled = !((key is >= Key.D0 and <= Key.D9) ||
                    (key is >= Key.NumPad0 and <= Key.NumPad9) ||
                    (key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Tab));
      if (key is Key.Enter) CreateEntity ();
   }

   #region PropertyChangedEventHandler---------------------------------------------------
   public event PropertyChangedEventHandler PropertyChanged;
   protected virtual void OnPropertyChanged (string propertyName) {
      PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
   }
   #endregion

   #region Properties ------------------------------------------------------------
   public string Prompt { get => mPrompt; set { mPrompt = value; OnPropertyChanged (nameof (Prompt)); } }
   protected StackPanel InputBar { get; set; }
   protected virtual string[] InputBox { get; set; }
   public bool IsModified { get; set; }
   public double X { get => Math.Round (mX, 3); set { mX = value; OnPropertyChanged (nameof (X)); } }
   public double Y { get => Math.Round (mY, 3); set { mY = value; OnPropertyChanged (nameof (Y)); } }
   public double DX { get => Math.Round (mDX, 3); set { mDX = value; OnPropertyChanged (nameof (DX)); } }
   public double DY { get => Math.Round (mDY, 3); set { mDY = value; OnPropertyChanged (nameof (DY)); } }
   public bool IsStartPointSet { get => mIsStartPointSet; set => mIsStartPointSet = value; }
   #endregion

   #region Private Data -----------------------------------------------------------------------------
   double mDX;
   double mDY;
   double mX;
   double mY;
   bool mIsStartPointSet = false;
   protected Shape mCurrentEntity;
   protected Paint mEventSource;
   protected string mPrompt;
   protected string[] mPrompts;
   int mPIndex = 0;
   #endregion
}
#region CLine Widget
public class DrawCLine : Widget {
   public DrawCLine (Paint eventSource) : base (eventSource) {
      mCurrentEntity = new CLine () { Brush = (255, 255, 0, 0) };
      mPrompts = new string[] { " Connected Line: Pick start point", " Connected Line: Pick end point" };
      Prompt = mPrompts[0];
   }
   public override void Reset () {
      base.Reset ();
      mCurrentEntity = new CLine () { Brush = (255, 255, 0, 0) };
   }
   protected override void UpdateInputs () {
      var (start, end) = (mCurrentEntity.StartPoint, mCurrentEntity.EndPoint);
      DX = end.X - X;
      DY = end.Y - Y;
      Length = Math.Sqrt (Math.Pow (end.X - start.X, 2) + Math.Pow (end.Y - start.Y, 2));
   }
   public double Length { get => Math.Round (mLength, 3); set { mLength = value; OnPropertyChanged (nameof (Length)); } }

   double mLength;
   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (DX), nameof (DY), nameof (Length) };
}
#endregion

#region Circle Widget
public class DrawCircle : Widget {
   public DrawCircle (Paint eventSource) : base (eventSource) {
      mCurrentEntity = new Circle () { Brush = (255, 255, 0, 0) };
      mPrompts = new string[] { " Circle: Pick centre point", " Circle: Pick point on circle" };
      Prompt = mPrompts[0];

   }
   public override void Reset () {
      base.Reset ();
      mCurrentEntity = new Circle () { Brush = (255, 255, 0, 0) };
   }
   protected override void UpdateInputs () {
      var (start, end) = (mCurrentEntity.StartPoint, mCurrentEntity.EndPoint);
      DX = end.X - X;
      DY = end.Y - Y;
      Radius = Math.Sqrt (Math.Pow (end.X - start.X, 2) + Math.Pow (end.Y - start.Y, 2));
   }
   public double Radius { get => Math.Round (mRadius, 3); set { mRadius = value; OnPropertyChanged (nameof (Radius)); } }
   double mRadius;

   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (Radius) };
}
#endregion

#region Line Widget
public class DrawLine : Widget {
   public DrawLine (Paint eventSource) : base (eventSource) {
      mCurrentEntity = new Line () { Brush = (255, 255, 0, 0) };
      mPrompts = new string[] { " Line: Pick start point", " Line: Pick end point" };
      Prompt = mPrompts[0];

   }
   public override void Reset () {
      base.Reset ();
      mCurrentEntity = new Line () { Brush = (255, 255, 0, 0) };
   }
   protected override void UpdateInputs () {
      var (start, end) = (mCurrentEntity.StartPoint, mCurrentEntity.EndPoint);
      DX = end.X - X;
      DY = end.Y - Y;
      Length = Math.Sqrt (Math.Pow (end.X - start.X, 2) + Math.Pow (end.Y - start.Y, 2));
   }
   public double Length { get => Math.Round (mLength, 3); set { mLength = value; OnPropertyChanged (nameof (Length)); } }
   double mLength;
   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (DX), nameof (DY), nameof (Length) };
}
#endregion

#region Rectangle Widget
public class DrawRectangle : Widget {
   public DrawRectangle (Paint eventSource) : base (eventSource) {
      mCurrentEntity = new Rectangle () { Brush = (255, 255, 0, 0) };
      mPrompts = new string[] { " Rectangle: Pick first corner of rectangle", " Rectangle: Pick opposite corner of rectangle" };
      Prompt = mPrompts[0];

   }
   public override void Reset () {
      base.Reset ();
      mCurrentEntity = new Rectangle () { Brush = (255, 255, 0, 0) };
   }
   protected override void UpdateInputs () {
      var (start, end) = (mCurrentEntity.StartPoint, mCurrentEntity.EndPoint);
      DX = end.X - X;
      DY = end.Y - Y;
      Length = end.X - start.X;
      Breadth = end.Y - start.Y;
   }
   public double Length { get => Math.Round (mLength, 3); set { mLength = value; OnPropertyChanged (nameof (Length)); } }
   public double Breadth { get => Math.Round (mBreadth, 3); set { mBreadth = value; OnPropertyChanged (nameof (Breadth)); } }

   double mLength;
   double mBreadth;
   protected override string[] InputBox => new string[] { nameof (X), nameof (Y), nameof (DX), nameof (DY), nameof (Length), nameof (Breadth) };
}
#endregion