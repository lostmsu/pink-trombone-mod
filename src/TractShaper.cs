namespace Vocal {
    using System;
    internal class TractShaper {
        const float gridOffset = 1.7f;

        readonly Tract tract;

        const float movementSpeed = 15;
        public float VelumOpenTarget { get; set; } = 0.4f;
        public float VelumClosedTarget { get; set; } = 0.01f;

        readonly double[] targetDiameter;
        public float VelumTarget { get; set; }
        public double TongueIndex { get; set; } = 12.9;
        public float TongueDiameter { get; set; } = 2.43f;

        int lastObstruction = -1;

        public TractShaper(Tract tract) {
            this.tract = tract ?? throw new ArgumentNullException(nameof(tract));

            this.targetDiameter = new double[Tract.n];
            this.ShapeNoise(true);
            tract.CalculateNoseReflections(); // (nose reflections are calculated only once, but with open velum)
            this.ShapeNoise(false);
            this.ShapeMainTract();
        }

        void ShapeMainTract() {
            for (int i = 0; i < Tract.n; i++) {
                double d = this.GetRestDiameter(i);
                this.tract.diameter[i] = d;
                this.targetDiameter[i] = d;
            }
        }

        public double GetRestDiameter(int i) {
            if (i < 7) return 0.6;
            if (i < Tract.BladeStart) return 1.1;
            if (i >= Tract.LipStart) return 1.5;

            double t = 1.1 * Math.PI * (this.TongueIndex - i) / (Tract.TipStart - Tract.BladeStart);
            double fixedTongueDiameter = 2 + (this.TongueDiameter - 2) / 1.5;
            double curve = (1.5 - fixedTongueDiameter + gridOffset) * Math.Cos(t);
            if (i == Tract.BladeStart - 2 || i == Tract.LipStart - 1) {
                curve *= 0.8;
            }
            if (i == Tract.BladeStart || i == Tract.LipStart - 2) {
                curve *= 0.94;
            }
            return 1.5 - curve;
        }

        // Adjusts the shape of the tract towards the target values.
        public void AdjustTractShape(float deltaTime) {
            double amount = deltaTime * movementSpeed;
            int newLastObstruction = -1;
            for (int i = 0; i < Tract.n; i++) {
                double diameter = this.tract.diameter[i];
                double targetDiameter = this.targetDiameter[i];
                if (diameter <= 0) {
                    newLastObstruction = i;
                }
                double slowReturn;
                if (i < Tract.NoseStart)
                    slowReturn = 0.6;
                else if (i >= Tract.TipStart)
                    slowReturn = 1;
                else
                    slowReturn = 0.6 + 0.4 * (i - Tract.NoseStart) / (Tract.TipStart - Tract.NoseStart);

                this.tract.diameter[i] = MathX.MoveTowards(diameter, targetDiameter, slowReturn * amount, 2 * amount);
            }
            if (this.lastObstruction >= 0 && newLastObstruction < 0 && this.tract.noseDiameter[0] < 0.223) {
                this.AddTransient(this.lastObstruction);
            }
            this.lastObstruction = newLastObstruction;
            this.tract.noseDiameter[0] = MathX.MoveTowards(this.tract.noseDiameter[0], this.VelumTarget, amount * 0.25, amount * 0.1);
        }

        void AddTransient(int position) {
            this.tract.transients.Add(new Transient {
                Position = position,
                StartTime = this.tract.time,
                LifeTime = 0.2,
                Strength = 0.3,
                Exponent = 200,
            });
        }

        void ShapeNoise(bool velumOpen) {
            this.VelumTarget = velumOpen ? this.VelumOpenTarget : this.VelumClosedTarget;
            for (int i = 0; i < Tract.NoseLength; i++) {
                double d = i * 2f / Tract.NoseLength;
                double diameter;
                if (i == 0)
                    diameter = this.VelumTarget;
                else if (d < 1)
                    diameter = 0.4 + 1.6 * d;
                else
                    diameter = 0.5 + 1.5 * (2 - d);

                diameter = Math.Min(diameter, 1.9);
                this.tract.noseDiameter[i] = diameter;
            }
        }
    }
}