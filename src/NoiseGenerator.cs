/*
 * A speed-improved perlin and simplex noise algorithms for 2D.
 *
 * Based on example code by Stefan Gustavson (stegu@itn.liu.se).
 * Optimisations by Peter Eastman (peastman@drizzle.stanford.edu).
 * Better rank ordering method by Stefan Gustavson in 2012.
 * Converted to Javascript by Joseph Gentle.
 *
 * Version 2012-03-09
 *
 * This code was placed in the public domain by its original author,
 * Stefan Gustavson. You may use it as you see fit, but
 * attribution is appreciated.
 *
 */
using System;

namespace Vocal {
    internal class NoiseGenerator {
        struct Grad {
            public readonly float x, y, z;

            public Grad(float x, float y, float z) {
                this.x = x; this.y = y; this.z = z;
            }

            public float Dot2(float x, float y) => this.x * x + this.y * y;
            public float Dot3(float x, float y, float z) => this.x * x + this.y * y + this.z * z;
        }

        static readonly Grad[] grad3 = {
            new Grad(1, 1, 0), new Grad(-1, 1, 0), new Grad(1, -1, 0), new Grad(-1, -1, 0),
            new Grad(1, 0, 1), new Grad(-1, 0, 1), new Grad(1, 0, -1), new Grad(-1, 0, -1),
            new Grad(0, 1, 1), new Grad(0, -1, 1), new Grad(0, 1, -1), new Grad(0, -1, -1),
        };

        static readonly byte[] p = {
             151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36,
             103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0,
             26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56,
             87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77,
             146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245,
             40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89,
             18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
             52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206,
             59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44,
             154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108,
             110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193,
             238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192,
             214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138,
             236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180,
        };

        readonly Grad[] gradP = new Grad[512];
        readonly int[] perm = new int[512];

        public void SetSeed(ushort seed) {
            if (seed < 256) {
                seed |= (ushort)(seed << 8);
            }

            for (int i = 0; i < 256; i++) {
                int v = (i & 1) == 1
                    ? p[i] ^ (seed & 255)
                    : p[i] ^ ((seed >> 8) & 255);
                this.perm[i] = this.perm[i + 256] = v;
                this.gradP[i] = this.gradP[i + 256] = grad3[v % 12];
            }
        }

        public NoiseGenerator(ushort seed) {
            this.SetSeed(seed);
        }

        public NoiseGenerator() : this((ushort)(DateTime.Now.Ticks / 134127)) { }

        // Skewing and unskewing factors for 2, 3, and 4 dimensions
        static readonly float f2 = 0.5f * (MathF.Sqrt(3) - 1);
        static readonly float g2 = (3 - MathF.Sqrt(3)) / 6;

        public float Simplex(float xin, float yin) {
            float n0, n1, n2; // Noise contributions from the three corners
                              // Skew the input space to determine which simplex cell we're in
            float s = (xin + yin) * f2; // Hairy factor for 2D
            int i = (int)MathF.Floor(xin + s);
            int j = (int)MathF.Floor(yin + s);
            float t = (i + j) * g2;
            float x0 = xin - i + t; // The x,y distances from the cell origin, unskewed.
            float y0 = yin - j + t;
            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if (x0 > y0) { // lower triangle, XY order: (0,0)->(1,0)->(1,1)
                i1 = 1;
                j1 = 0;
            } else {    // upper triangle, YX order: (0,0)->(0,1)->(1,1)
                i1 = 0;
                j1 = 1;
            }
            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3 - sqrt(3)) / 6
            float x1 = x0 - i1 + g2; // Offsets for middle corner in (x,y) unskewed coords
            float y1 = y0 - j1 + g2;
            float x2 = x0 - 1 + 2 * g2; // Offsets for last corner in (x,y) unskewed coords
            float y2 = y0 - 1 + 2 * g2;
            // Work out the hashed gradient indices of the three simplex corners
            i &= 255;                                                // tslint:disable-line:no-bitwise
            j &= 255;                                                // tslint:disable-line:no-bitwise
            var gi0 = this.gradP[i + this.perm[j]];
            var gi1 = this.gradP[i + i1 + this.perm[j + j1]];
            var gi2 = this.gradP[i + 1 + this.perm[j + 1]];
            // Calculate the contribution from the three corners
            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 < 0) {
                n0 = 0;
            } else {
                t0 *= t0;
                n0 = t0 * t0 * gi0.Dot2(x0, y0);  // (x,y) of grad3 used for 2D gradient
            }
            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 < 0) {
                n1 = 0;
            } else {
                t1 *= t1;
                n1 = t1 * t1 * gi1.Dot2(x1, y1);
            }
            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 < 0) {
                n2 = 0;
            } else {
                t2 *= t2;
                n2 = t2 * t2 * gi2.Dot2(x2, y2);
            }
            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return 70 * (n0 + n1 + n2);
        }

        public float Simplex(float x) => this.Simplex(x * 1.2f, -x * 0.7f);
    }
}