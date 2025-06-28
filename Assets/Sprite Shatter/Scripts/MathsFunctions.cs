namespace SpriteShatter {
    using UnityEngine;

    public static class MathsFunctions {

        //Returns whether a point is within a 2D triangle.
        public static bool pointIn2DTriangle(float x1, float y1, float x2, float y2, float x3, float y3, float xPos, float yPos) {
            float a = ((x1 - xPos) * (y2 - yPos)) - ((x2 - xPos) * (y1 - yPos));
            float b = ((x2 - xPos) * (y3 - yPos)) - ((x3 - xPos) * (y2 - yPos));
            float c = ((x3 - xPos) * (y1 - yPos)) - ((x1 - xPos) * (y3 - yPos));
            return ((a > 0 && b > 0) || (a < 0 && b < 0)) && ((b > 0 && c > 0) || (b < 0 && c < 0));
        }

        //Returns whether an angle is reflex.
        public static bool isAngleReflex(Vector2 vector1, Vector2 vector2, Vector2 origin) {
            vector1 -= origin;
            vector2 -= origin;
            return (vector1.x * vector2.y) - (vector1.y * vector2.x) > 0;
        }

        //Returns whether two 2D lines of finite length intersect
        public static bool _2DLinesIntersect(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
            float d1 = computeDirection(x3, y3, x4, y4, x1, y1);
            float d2 = computeDirection(x3, y3, x4, y4, x2, y2);
            float d3 = computeDirection(x1, y1, x2, y2, x3, y3);
            float d4 = computeDirection(x1, y1, x2, y2, x4, y4);
            return (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) ||
                    (d1 == 0 && isOnSegment(x3, y3, x4, y4, x1, y1)) || (d2 == 0 && isOnSegment(x3, y3, x4, y4, x2, y2)) ||
                    (d3 == 0 && isOnSegment(x1, y1, x2, y2, x3, y3)) || (d4 == 0 && isOnSegment(x1, y1, x2, y2, x4, y4));
        }
        static bool isOnSegment(float xi, float yi, float xj, float yj, float xk, float yk) {
            return (xi <= xk || xj <= xk) && (xk <= xi || xk <= xj) && (yi <= yk || yj <= yk) && (yk <= yi || yk <= yj);
        }
        static int computeDirection(float xi, float yi, float xj, float yj, float xk, float yk) {
            float a = (xk - xi) * (yj - yi);
            float b = (xj - xi) * (yk - yi);
            return a < b ? -1 : (a > b ? 1 : 0);
        }

        //Returns the intersection point of two 2D lines.
        public static Vector2 _2DLineIntersectionPoint(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
            float denominator = ((y4 - y3) * (x2 - x1)) - ((x4 - x3) * (y2 - y1));
            if (denominator == 0)
                return new Vector2(float.MinValue, float.MinValue);
            float numeratorOverDenominator = (((x4 - x3) * (y1 - y3)) - ((y4 - y3) * (x1 - x3))) / denominator;
            return new Vector2(x1 + (numeratorOverDenominator * (x2 - x1)), y1 + (numeratorOverDenominator * (y2 - y1)));
        }

        //Rotates a 2D vector by a given angle around the origin.
        public static Vector2 rotateVector(Vector2 input, float angle) {
            return new Vector2((input.x * Mathf.Cos(angle)) + (input.y * Mathf.Sin(angle)), (input.x * Mathf.Sin(-angle)) + (input.y * Mathf.Cos(-angle)));
        }
    }
}