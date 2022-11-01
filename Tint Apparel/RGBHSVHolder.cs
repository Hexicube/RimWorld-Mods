using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TintApparel
{
    public class RGBHSVHolder
    {
        private Color _lastCol = Color.white;

        private Color Col
        {
            get;
            set;
        } = Color.white;

        public void SetCol(Color c)
        {
            if (c.Equals(_lastCol)) return;
            Col = c;
            _lastCol = c;
            RGBHSVBars = MakeBars((int)_barSize.x, (int)_barSize.y);
        }

        private Vector2 _barSize = new Vector2(1, 1);

        public Vector2 BarSize
        {
            get => _barSize;
            set
            {
                if (_barSize.Equals(value)) return;
                _barSize = value;
                RGBHSVBars = MakeBars((int) _barSize.x, (int) _barSize.y);
            }
        }

        private static readonly string[] _labels = {
            "R", "G", "B", "H", "S", "V"
        };

        public Color? RenderAndCheck(float margin, float x, float y, List<Color> options)
        {
            Widgets.Label(new Rect(x, y, 50f, _barSize.y), "Col".Translate());
            
            // slightly off because of discrepancy vs texture draw
            Widgets.DrawBoxSolidWithOutline(new Rect(x + 51f, y, _barSize.x-1, _barSize.y), Col, Color.black);
            
            float[] RGB = GetRGB();
            float[] HSV = GetHSV();
            float[] inputs = {RGB[0], RGB[1], RGB[2], HSV[0], HSV[1], HSV[2]};
            float[] outputs = new float[6];
            for (int a = 0; a < 6; a++)
            {
                float yPos = y + (_barSize.y + margin) * (a + 1);
                Widgets.Label(new Rect(x, yPos, 50f, _barSize.y), _labels[a].Translate());
                var rect = new Rect(x + 50f, yPos, _barSize.x, _barSize.y);
                GUI.DrawTexture(rect, RGBHSVBars[a]);
                rect.y += 4;
                rect.height -= 8;
                outputs[a] = Widgets.HorizontalSlider(rect, inputs[a], 0, 1);
            }

            bool changed = false;
            if (inputs[0] != outputs[0] || inputs[1] != outputs[1] || inputs[2] != outputs[2])
            {
                SetRGB(outputs[0], outputs[1], outputs[2]);
                changed = true;
            }
            
            if (inputs[3] != outputs[3] || inputs[4] != outputs[4] || inputs[5] != outputs[5])
            {
                SetHSV(outputs[3], outputs[4], outputs[5]);
                changed = true;
            }

            Widgets.Label(new Rect(x, y + (_barSize.y + margin) * 7 - 2, _barSize.x, 26), "Exists".Translate());
            var maxPerRow = (int) _barSize.x / (int) (_barSize.y + 30);
            for (int a = 0; a < options.Count; a++)
            {
                var xPos = x + (a % maxPerRow) * _barSize.y;
                var yPos = y + (_barSize.y + margin) * 7 + (a / maxPerRow) * _barSize.y + 20;
                
                Widgets.DrawBoxSolidWithOutline(new Rect(xPos, yPos, _barSize.y, _barSize.y), options[a], Color.black);
                if (Widgets.ButtonInvisible(new Rect(xPos, yPos, _barSize.y, _barSize.y)))
                {
                    Col = options[a];
                    changed = true;
                }
            }
            
            if (changed) return Col;
            return null;
        }

        private float[] GetRGB()
        {
            return new[] {Col.r, Col.g, Col.b};
        }

        private float[] GetHSV() {
            var resp = new float[3];
            Color.RGBToHSV(Col, out resp[0], out resp[1], out resp[2]);
            return resp;
        }

        private void SetRGB(float R, float G, float B)
        {
            Col = new Color(R, G, B);
            if (Col.Equals(_lastCol)) return;
            _lastCol = new Color(R, G, B);
            RGBHSVBars = MakeBars((int)_barSize.x, (int)_barSize.y);
        }

        private void SetHSV(float H, float S, float V)
        {
            Col = Color.HSVToRGB(H, S, V);
            if (Col.Equals(_lastCol)) return;
            _lastCol = new Color(Col.r, Col.g, Col.b);
            RGBHSVBars = MakeBars((int)_barSize.x, (int)_barSize.y);
        }

        private Texture2D[] RGBHSVBars
        {
            get;
            set;
        }
        private const int _barFudgeFactor = 5;
        private Texture2D[] MakeBars(int W, int H)
        {
            var bars = new[]
            {
                new Texture2D(W, H), new Texture2D(W, H), new Texture2D(W, H),
                new Texture2D(W, H), new Texture2D(W, H), new Texture2D(W, H)
            };
            Color c = new Color();
            var RGB = GetRGB();
            var HSV = GetHSV();
            for (int x = 0; x < W; x++)
            {
                for (int a = 0; a < 6; a++)
                {
                    bars[a].SetPixel(x, 0, Color.black);
                    bars[a].SetPixel(x, H-1, Color.black);
                    if (x != 0 && x != W - 1) continue;
                    for (var y = 1; y < H-1; y++) bars[a].SetPixel(x, y, Color.black);
                }
                if (x == 0 || x == W - 1) continue;
                
                var part = (float) (x - _barFudgeFactor) / (W - 1 - _barFudgeFactor - _barFudgeFactor);
                // red
                c.r = part;
                c.g = RGB[1];
                c.b = RGB[2];
                for (var y = 1; y < H-1; y++) bars[0].SetPixel(x, y, c);
                // green
                c.r = RGB[0];
                c.g = part;
                for (var y = 1; y < H-1; y++) bars[1].SetPixel(x, y, c);
                // blue
                c.g = RGB[1];
                c.b = part;
                for (var y = 1; y < H-1; y++) bars[2].SetPixel(x, y, c);
                // hue
                c = Color.HSVToRGB(part, HSV[1], HSV[2]);
                for (var y = 1; y < H-1; y++) bars[3].SetPixel(x, y, c);
                // sat
                c = Color.HSVToRGB(HSV[0], part, HSV[2]);
                for (var y = 1; y < H-1; y++) bars[4].SetPixel(x, y, c);
                // val
                c = Color.HSVToRGB(HSV[0], HSV[1], part);
                for (var y = 1; y < H-1; y++) bars[5].SetPixel(x, y, c);
            }
            for (int a = 0; a < 6; a++) bars[a].Apply();
            return bars;
        }
    }
}