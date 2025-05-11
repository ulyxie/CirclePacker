using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CirclePackingWPF {
  internal class Processing {

    // Aggressive placement
    public static Circle? PlaceNextCircleAggressively(Circle toPlace, List<Circle> packed_circles) {
      var bestPosition = FindBestPositionForCircle(toPlace, packed_circles);
      if (bestPosition != null) {
        return bestPosition;
      }

      float angle = 0;
      float step = 0.1f;
      float radius = toPlace.Radius;
      for (int i = 0; i < 10000; i++) {
        float x = radius * MathF.Cos(angle);
        float y = radius * MathF.Sin(angle);
        var candidate = new Circle { Position = new SKPoint(x, y), Radius = toPlace.Radius };
        if (!packed_circles.Any(c => c.Overlaps(candidate)))
          return candidate;
        angle += step;
        radius += 0.1f;
      }

      return null;
    }

    // Best way: tangent positions
    private static Circle? FindBestPositionForCircle(Circle toPlace, List<Circle> packed_circles) {
      Circle? bestCircle = null;
      float bestDistance = float.MaxValue;

      foreach (var a in packed_circles) {
        foreach (var b in packed_circles) {
          if (a == b) continue;

          var positions = TangentPositions(a, b, toPlace.Radius);
          foreach (var pos in positions) {
            var candidate = new Circle { Position = pos, Radius = toPlace.Radius };
            if (!packed_circles.Any(c => c.Overlaps(candidate))) {
              float dist = pos.Length;
              if (dist < bestDistance) {
                bestDistance = dist;
                bestCircle = candidate;
              }
            }
          }
        }
      }

      return bestCircle;
    }

    private static IEnumerable<SKPoint> TangentPositions(Circle c1, Circle c2, float newRadius) {
      float R1 = c1.Radius + newRadius;
      float R2 = c2.Radius + newRadius;

      var dx = c2.Position.X - c1.Position.X;
      var dy = c2.Position.Y - c1.Position.Y;
      var d = MathF.Sqrt(dx * dx + dy * dy);

      if (d < 1e-5f) yield break;

      float angle = MathF.Atan2(dy, dx);
      float cos = (R1 * R1 + d * d - R2 * R2) / (2 * R1 * d);
      if (cos < -1 || cos > 1) yield break;

      float theta = MathF.Acos(cos);
      foreach (var a in new[] { angle + theta, angle - theta }) {
        yield return new SKPoint(
            c1.Position.X + R1 * MathF.Cos(a),
            c1.Position.Y + R1 * MathF.Sin(a));
      }
    }
  }
}
