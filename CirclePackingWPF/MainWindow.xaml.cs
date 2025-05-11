using OpenTK.Graphics.OpenGL;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CirclePackingWPF {
  public partial class MainWindow : Window {
    private List<Circle> unpacked_circles = new();
    private List<Circle> packed_circles = new();
    private Random rng = new();
    private DispatcherTimer timer;
    private int currentIndex = 0;
    private DateTime lastPlacement;

    #region interaction fields
    private SKPoint drag_origin;
    private bool dragging;
    private SKPoint pan = new SKPoint(0, 0);
    private float zoom = 1.0f;
    #endregion

    public MainWindow() {
      timer = new DispatcherTimer {
        Interval = TimeSpan.FromMilliseconds(50)
      };
      InitializeComponent();
      StartPacking();
    }

    private List<Circle> GenerateCircleList(int count) {
      var list = new List<Circle>();
      for (int i = 0; i < count; i++) {
        float r = (new Random()).Next(10, 30);
        list.Add(new Circle { Radius = r });
      }
      return list;
    }

    private void StartPacking() {
      packed_circles.Clear();
      unpacked_circles = GenerateCircleList(100);
      currentIndex = 0;

      Circle first = unpacked_circles[0];
      first.Position = new SKPoint(0, 0);
      packed_circles.Add(first);
      currentIndex = 1;
      lastPlacement = DateTime.Now;

      timer.Tick += Timer_Tick;
      timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs? e) {
      if (currentIndex >= unpacked_circles.Count) {
        timer.Stop();
        return;
      }

      Circle? next = Processing.PlaceNextCircleAggressively(unpacked_circles[currentIndex], packed_circles);
      if (next != null) {
        packed_circles.Add(next);
        unpacked_circles[currentIndex] = next;
        TimeSpan delay = DateTime.Now - lastPlacement;
        lastPlacement = DateTime.Now;
        TimerText.Text = $"Circle {currentIndex + 1} / {unpacked_circles.Count} placed after {delay.TotalMilliseconds:F0} ms";
      }

      Progress.Value = (double)currentIndex / (unpacked_circles.Count - 1) * 100.0;
      currentIndex++;
      SkiaCanvas.InvalidateVisual();
    }

    private void OnPaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e) {
      var canvas = e.Surface.Canvas;
      canvas.Clear(new SKColor(30, 30, 46)); // mocha base

      using var strokePaint = new SKPaint {
        Style = SKPaintStyle.Stroke,
        Color = new SKColor(245, 194, 231), // mocha pink
        StrokeWidth = 2,
        IsAntialias = true
      };

      using var fillPaint = new SKPaint {
        Style = SKPaintStyle.Fill,
        Color = new SKColor(205, 214, 244, 100), // mocha overlay
        IsAntialias = true
      };

      canvas.Translate(e.Info.Width / 2 + pan.X, e.Info.Height / 2 + pan.Y);
      canvas.Scale(zoom);

      foreach (var circle in packed_circles) {
        canvas.DrawCircle(circle.Position, circle.Radius, fillPaint);
        canvas.DrawCircle(circle.Position, circle.Radius, strokePaint);
      }

      if (packed_circles.Count > 0) {
        var enclosingCircle = EnclosingCircleHelper.GetTightEnclosingCircle(packed_circles); //CALCULATE THE BOUNDING CIRCLE

        using var borderPaint = new SKPaint {
          Style = SKPaintStyle.Stroke,
          Color = new SKColor(166, 227, 161), // green
          StrokeWidth = 3,
          IsAntialias = true
        };

        canvas.DrawCircle(enclosingCircle.Position, enclosingCircle.Radius, borderPaint); //DRAW THE BOUNDING CIRCLE
      }

    }

    private void Restart_Click(object sender, RoutedEventArgs e) {
      timer?.Stop();
      StartPacking();
    }

    private void SkiaCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
      zoom *= e.Delta > 0 ? 1.1f : 0.9f;
      SkiaCanvas.InvalidateVisual();
    }

    private void SkiaCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      dragging = true;
      drag_origin = e.GetPosition(SkiaCanvas).ToSKPoint();
      Mouse.Capture(SkiaCanvas);
    }

    private void SkiaCanvas_MouseMove(object sender, MouseEventArgs e) {
      if (dragging) {
        var current = e.GetPosition(SkiaCanvas).ToSKPoint();
        var delta = current - drag_origin;
        pan.X += delta.X;
        pan.Y += delta.Y;
        drag_origin = current;
        SkiaCanvas.InvalidateVisual();
      }
    }

    private void SkiaCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      dragging = false;
      Mouse.Capture(null);
    }
  }
}
