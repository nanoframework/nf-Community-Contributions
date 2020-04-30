using System;

namespace WS2812
{
    public class Color
    {
        #region Fields

        const int HUE_DEGREE = 360;

        public byte B;
        public byte G;
        public byte R;
        public byte W = 0;

        #endregion Fields

        #region Methods

        public static Color FromHSV(int H, int S, int V)
        {
            HsvToRgb(H, S, V, out byte R, out byte G, out byte B);
            return new Color() { R = R, G = G, B = B };
        }

        public Color SetHSV(int H, double S, double V)
        {
            HsvToRgb(H, S, V, out byte r, out byte g, out byte b);
            R = r;
            G = g;
            B = b;
            W = 0;

            return this;
        }

        public static void HsvToRgb(double h, double S, double V, out byte r, out byte g, out byte b)
        {
            double H = h;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            double R, G, B;
            if (V <= 0)
            { R = G = B = 0; }
            else if (S <= 0)
            {
                R = G = B = V;
            }
            else
            {
                double hf = H / 60.0;
                int i = (int)hf; //(int)Math.Floor(hf);
                double f = hf - i;
                double pv = V * (1 - S);
                double qv = V * (1 - S * f);
                double tv = V * (1 - S * (1 - f));
                switch (i)
                {

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;


                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;


                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;


                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    default:
                        R = G = B = V;
                        break;
                }
            }
            r = CheckLimitValues((int)(R * 255.0));
            g = CheckLimitValues((int)(G * 255.0));
            b = CheckLimitValues((int)(B * 255.0));
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        private static byte CheckLimitValues(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return (byte)i;
        }

        #endregion Methods
    }
}
