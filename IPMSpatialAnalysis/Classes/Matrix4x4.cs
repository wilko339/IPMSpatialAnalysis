using System;
namespace IPMSpatialAnalysis.Classes
{
    public class Matrix4x4
    {
        public double[,] M { get; private set; }

        public Matrix4x4()
        {
            M = new double[4, 4];
            for (int i = 0; i < 4; i++)
                M[i, i] = 1;
        }

        public Matrix4x4(
            double m00, double m01, double m02, double m03,
            double m10, double m11, double m12, double m13,
            double m20, double m21, double m22, double m23,
            double m30, double m31, double m32, double m33)
        {
            M = new double[4, 4];
            M[0, 0] = m00;
            M[0, 1] = m01;
            M[0, 2] = m02;
            M[0, 3] = m03;
            M[1, 0] = m10;
            M[1, 1] = m11;
            M[1, 2] = m12;
            M[1, 3] = m13;
            M[2, 0] = m20;
            M[2, 1] = m21;
            M[2, 2] = m22;
            M[2, 3] = m23;
            M[3, 0] = m30;
            M[3, 1] = m31;
            M[3, 2] = m32;
            M[3, 3] = m33;

        }

        public static Matrix4x4 Translation(double tx, double ty, double tz)
        {
            var result = new Matrix4x4();
            result.M[0, 3] = tx;
            result.M[1, 3] = ty;
            result.M[2, 3] = tz;
            return result;
        }

        public static Matrix4x4 RotationX(double angle)
        {
            var result = new Matrix4x4();
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            result.M[1, 1] = cos;
            result.M[1, 2] = -sin;
            result.M[2, 1] = sin;
            result.M[2, 2] = cos;
            return result;
        }

        public static Matrix4x4 RotationY(double angle)
        {
            var result = new Matrix4x4();
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            result.M[0, 0] = cos;
            result.M[0, 2] = sin;
            result.M[2, 0] = -sin;
            result.M[2, 2] = cos;
            return result;
        }

        public static Matrix4x4 RotationZ(double angle)
        {
            var result = new Matrix4x4();
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            result.M[0, 0] = cos;
            result.M[0, 1] = -sin;
            result.M[1, 0] = sin;
            result.M[1, 1] = cos;
            return result;
        }

        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            var result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result.M[i, j] = 0;
                    for (int k = 0; k < 4; k++)
                    {
                        result.M[i, j] += a.M[i, k] * b.M[k, j];
                    }
                }
            }
            return result;
        }

        public (double x, double y, double z) Transform(double x, double y, double z)
        {
            //double[] vector = new double[4] { x, y, z, 1 };
            //double[] result = new double[4];
            //for (int i = 0; i < 4; i++)
            //{
            //    result[i] = 0;
            //    for (int j = 0; j < 4; j++)
            //    {
            //        result[i] += M[i, j] * vector[j];
            //    }
            //}
            //return (result[0], result[1], result[2]);

            double rx = M[0, 0] * x + M[0, 1] * y + M[0, 2] * z + M[0, 3];
            double ry = M[1, 0] * x + M[1, 1] * y + M[1, 2] * z + M[1, 3];
            double rz = M[2, 0] * x + M[2, 1] * y + M[2, 2] * z + M[2, 3];
            // Optionally handle homogeneous coordinate (M[3, *]) if needed
            return (rx, ry, rz);
        }
    }
}
