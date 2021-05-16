namespace Vocal {
    using System;
    public sealed class TurbulencePoint {
        float diameter;
        float position;

        /// <summary>
        /// 2..44 <see cref="Tract.n"/>
        /// </summary>
        public float Position {
            get => this.position;
            set {
                if (this.position < 2 || this.position > Tract.n)
                    throw new ArgumentOutOfRangeException(nameof(this.Position));
                this.position = value;
            }
        }
        /// <summary>
        /// 0..
        /// </summary>
        public float Diameter {
            get => this.diameter;
            set {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(this.Diameter));
                this.diameter = value;
            }
        }
        public float StartTime { get; set; }
        public float EndTime { get; set; } = float.NaN;
    }
}