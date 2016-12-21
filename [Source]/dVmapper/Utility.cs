using UnityEngine;


namespace SigmadVmapperPlugin
{
    public static class HSBcolors
    {
        public static Color FromColor(Color color)
        {
            Color ret = new Color(0f, 0f, 0f, color.a);

            float r = color.r;
            float g = color.g;
            float b = color.b;

            float max = Mathf.Max(r, Mathf.Max(g, b));

            if (max <= 0)
            {
                return ret;
            }

            float min = Mathf.Min(r, Mathf.Min(g, b));
            float dif = max - min;

            if (max > min)
            {
                if (g == max)
                {
                    ret.r = (b - r) / dif * 60f + 120f;
                }
                else if (b == max)
                {
                    ret.r = (r - g) / dif * 60f + 240f;
                }
                else if (b > g)
                {
                    ret.r = (g - b) / dif * 60f + 360f;
                }
                else
                {
                    ret.r = (g - b) / dif * 60f;
                }
                if (ret.r < 0)
                {
                    ret.r = ret.r + 360f;
                }
            }
            else
            {
                ret.r = 0;
            }

            ret.r *= 1f / 360f;
            ret.g = (dif / max) * 1f;
            ret.b = max;

            return ret;
        }

        public static Color ToColor(Color hsbColor)
        {
            float r = hsbColor.b;
            float g = hsbColor.b;
            float b = hsbColor.b;
            if (hsbColor.g != 0)
            {
                float max = hsbColor.b;
                float dif = hsbColor.b * hsbColor.g;
                float min = hsbColor.b - dif;

                float h = hsbColor.r * 360f;

                if (h < 60f)
                {
                    r = max;
                    g = h * dif / 60f + min;
                    b = min;
                }
                else if (h < 120f)
                {
                    r = -(h - 120f) * dif / 60f + min;
                    g = max;
                    b = min;
                }
                else if (h < 180f)
                {
                    r = min;
                    g = max;
                    b = (h - 120f) * dif / 60f + min;
                }
                else if (h < 240f)
                {
                    r = min;
                    g = -(h - 240f) * dif / 60f + min;
                    b = max;
                }
                else if (h < 300f)
                {
                    r = (h - 240f) * dif / 60f + min;
                    g = min;
                    b = max;
                }
                else if (h <= 360f)
                {
                    r = max;
                    g = min;
                    b = -(h - 360f) * dif / 60 + min;
                }
                else
                {
                    r = 0;
                    g = 0;
                    b = 0;
                }
            }

            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), hsbColor.a);
        }
    }
}
