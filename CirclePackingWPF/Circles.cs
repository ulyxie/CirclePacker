using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclePackingWPF {
  internal class Circle {
    public SKPoint Position { get; set; } = new SKPoint();
    public float Radius { get; set; }

    public bool Overlaps(Circle other) {
      var dx = Position.X - other.Position.X;
      var dy = Position.Y - other.Position.Y;
      var distanceSq = dx * dx + dy * dy;
      var radiusSum = Radius + other.Radius;
      return distanceSq < radiusSum * radiusSum;
    }
  }

  internal static class Extensions {
    public static SKPoint ToSKPoint(this System.Windows.Point p) {
      return new SKPoint((float)p.X, (float)p.Y);
    }
  }

  internal static class EnclosingCircleHelper {
    private static Func<SKPoint, SKPoint, float> Distance = (SKPoint a, SKPoint b) => MathF.Sqrt(MathF.Pow(a.X - b.X, 2) + MathF.Pow(a.Y - b.Y, 2));

    private static Func<Circle, SKPoint, bool> IsPointInside = (Circle c, SKPoint p) => Distance(c.Position, p) <= c.Radius + 1e-3f;

    public static Circle GetTightEnclosingCircle(List<Circle> circles) {
      if (circles == null || circles.Count == 0)
        return new Circle { Position = new SKPoint(0, 0), Radius = 0 };

      var points = circles.Select(c => c.Position).ToList();

      var mec = ComputeMinimalEnclosingCircle(points);

      float maxDistance = circles.Max(c => Distance(mec.Position, c.Position) + c.Radius);

      return new Circle { Position = mec.Position, Radius = maxDistance };
    }

    private static Circle ComputeMinimalEnclosingCircle(List<SKPoint> points) {
      var shuffled = points.ToList();
      (new Random()).Shuffle(shuffled.ToArray());

      Circle? circle = null;
      for (int i = 0; i < shuffled.Count; i++) {
        var p = shuffled[i];
        if (circle == null || !IsPointInside(circle, p)) {
          circle = MakeCircleOnePoint(shuffled.Take(i + 1).ToList(), p);
        }
      }
      return circle is not null ? circle : new Circle();
    }

    private static Circle MakeCircleOnePoint(List<SKPoint> points, SKPoint p) {
      Circle circle = new Circle { Position = p, Radius = 0 };
      for (int i = 0; i < points.Count; i++) {
        var q = points[i];
        if (!IsPointInside(circle, q)) {
          circle = MakeCircleTwoPoints(points.Take(i + 1).ToList(), p, q);
        }
      }
      return circle;
    }

    private static Circle MakeCircleTwoPoints(List<SKPoint> points, SKPoint p, SKPoint q) {
      var center = new SKPoint((p.X + q.X) / 2, (p.Y + q.Y) / 2);
      float radius = Distance(p, q) / 2f;
      var circle = new Circle { Position = center, Radius = radius };

      foreach (var r in points) {
        if (!IsPointInside(circle, r)) {
          circle = MakeCircleThreePoints(p, q, r);
        }
      }
      return circle;
    }

    private static Circle MakeCircleThreePoints(SKPoint a, SKPoint b, SKPoint c) {
      (float dX, float dY) dAB = (b.X - a.X, b.Y - a.Y);
      (float dX, float dY) dAC = (c.X - a.X, c.Y - a.Y);

      float detAB = dAB.dX * (a.X + b.X) + dAB.dY * (a.Y + b.Y);
      float detAC = dAC.dX * (a.X + c.X) + dAC.dY * (a.Y + c.Y);
      float norm = 2 * (dAB.dX * (c.Y - b.Y) - dAB.dY * (c.X - b.X));

      if (Math.Abs(norm) < 1e-6) {
        (float Min, float Max) X = (Math.Min(a.X, Math.Min(b.X, c.X)), Math.Max(a.X, Math.Max(b.X, c.X)));
        (float Min, float Max) Y = (Math.Min(a.Y, Math.Min(b.Y, c.Y)), Math.Max(a.Y, Math.Max(b.Y, c.Y)));
        var center = new SKPoint((X.Min + X.Max) / 2, (Y.Min + Y.Max) / 2);
        float radius = Math.Max(Distance(center, a), Math.Max(Distance(center, b), Distance(center, c)));
        return new Circle { Position = center, Radius = radius };
      }

      float cx = (dAC.dY * detAB - dAB.dY * detAC) / norm;
      float cy = (dAB.dX * detAC - dAC.dX * detAB) / norm;
      var centerPoint = new SKPoint(cx, cy);
      float radiusFinal = Distance(centerPoint, a);
      return new Circle { Position = centerPoint, Radius = radiusFinal };
    }

    public static Circle GetEnclosingCircle(List<Circle> circles) {
      if (circles == null || circles.Count == 0)
        return new Circle { Position = new SKPoint(0, 0), Radius = 0 };

      (float Min, float Max) X = (float.MaxValue, float.MinValue);
      (float Min, float Max) Y = (float.MaxValue, float.MinValue);

      foreach (var c in circles) {
        (float Left, float Right, float Up, float Down) Rect = (c.Position.X - c.Radius, c.Position.X + c.Radius, c.Position.Y - c.Radius, c.Position.Y + c.Radius);

        X = (MathF.Min(X.Min, Rect.Left), MathF.Max(X.Max, Rect.Right));
        Y = (MathF.Min(Y.Min, Rect.Up), MathF.Max(Y.Max, Rect.Down));
      }

      var center = new SKPoint((X.Min + X.Max) / 2, (Y.Min + Y.Max) / 2);

      float radius = circles.Max(c => Distance(center, c.Position) + c.Radius);

      return new Circle { Position = center, Radius = radius };
    }
  }
}
