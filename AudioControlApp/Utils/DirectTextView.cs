using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace AudioControlApp.Utils
{
    public class DirectTextView
    {
        bool debugMode = false; // when on, will draw rectangles around text areas

        private static WebBrowser web = new WebBrowser();
        private StringFormat _format;
        private Font _textFont;
        private string _fontName;
        public string FontName
        {
            get { return _fontName; }
            set
            {
                if (_fontName != value)
                {
                    _fontName = value;
                    _textFont.Dispose();
                    _textFont = new Font(_fontName, _size);
                    _dirty = true;
                }
            }
        }
        private int _size;
        public int FontSize
        {
            get { return _size; }
            set
            {
                if (_size != value)
                {
                    _size = value;
                    _textFont.Dispose();
                    _textFont = new Font(_fontName, _size);
                    _dirty = true;
                }
            }
        }
        private SolidBrush _brush = new SolidBrush(Color.Blue);
        private Rectangle _rect;
        private SizeF _textSize;
        private System.Windows.Forms.HorizontalAlignment _alignment;

        public FontStyle _fontStyle = FontStyle.Regular;

        public System.Windows.Forms.HorizontalAlignment Alignment
        {
            get { return _alignment; }
            set { _alignment = value; _dirty = true; }
        }

        private Rectangle _realPos;
        public Bitmap _textBitmap;


        private int _width;
        public int Width
        {
            get { return _width; }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    _rect.Width = _width;
                    _doResize = true;
                    _dirty = true;
                }
            }
        }
        private int _height;
        public int Height
        {
            get { return _height; }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    _rect.Height = _height;
                    _doResize = true;
                    _dirty = true;
                }
            }
        }

        private bool _dirty = false;
        private bool _doResize = false;

        string text;
        public string Text
        {
            get { return text; }
            set { text = value; _dirty = true; }
        }
        public void getDim(out int x, out int y)
        {
            x = (int)_textSize.Width;
            y = (int)_textSize.Height;
        }
        public void getPos(out int x, out int y)
        {
            x = (int)_realPos.X;
            y = (int)_realPos.Y;
        }

        private int _TextureId = -1;

        private float texCoordX0, texCoordX1, texCoordY0, texCoordY1;
        private float quadLeft, quadRight, quadTop, quadBottom;
        private float quadDepth = -50.0f;

        // ****************************************************************************************

        // ****************************************************************************************

        public DirectTextView(string fontName, int size, int width, int height)
        {
            _fontName = fontName;
            _size = size;
            _textFont = new Font(fontName, size);
            _textBitmap = new Bitmap(width, height);
 
            Width = width;
            Height = height;

            _rect = new Rectangle(0, 0, width, height);

            _format = new StringFormat();
            _format.Alignment = StringAlignment.Near;
            
        }

        public void setColor(float r, float g, float b, float a)
        {
            _brush.Color = Color.FromArgb((int)(a * 255), (int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        public void resize()
        {
            _textBitmap.Dispose();
            _textBitmap = new Bitmap(_width, _height);


            _doResize = false;
        }

        public void update()
        {
            if (!_dirty)
                return;
            if (_doResize)
            {
                resize();
            }

            using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(_textBitmap))
            {
                gfx.Clear(Color.FromArgb(0, 255, 255, 255));

                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;


                gfx.DrawString(text, new System.Drawing.Font(_fontName, (float)_size, _fontStyle), _brush, 0, 0);
            }

            _realPos = rectAroundText(_textBitmap);
            _textSize.Width = _realPos.Width;
            _textSize.Height = _realPos.Height;

            // ************************************************************************************************************************************
            // ****************************************************************** debug code (turn on visibility of text area)
            bool doDrawRectangleAroundTextArea = debugMode;

            if (doDrawRectangleAroundTextArea)
            {
                using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(_textBitmap))
                {
                    gfx.DrawRectangle(Pens.Black, _realPos);
                    gfx.DrawRectangle(Pens.Red, _rect);
                }
            }
            // ************************************************************************************************************************************
            // ************************************************************************************************************************************
            
        }

        /// <summary>
        /// wrap text in provided rect area:
        /// </summary>
        /// <param name="whichRect"></param>
        public void update(RectangleF whichRect, bool doCenter)
        {
            if (!_dirty)
                return;
            if (_doResize)
            {
                resize();
            }

            using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(_textBitmap))
            {
                gfx.Clear(Color.FromArgb(0, 255, 255, 255));

                gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                StringFormat myFormat = new StringFormat();
                myFormat.LineAlignment = StringAlignment.Near;
                myFormat.Alignment = StringAlignment.Near;

                if (doCenter)
                {
                    myFormat.Alignment = StringAlignment.Center;
                    myFormat.LineAlignment = StringAlignment.Center;
                }

                gfx.DrawString(text, new System.Drawing.Font(_fontName, (float)_size, _fontStyle), _brush, whichRect, myFormat);
            }

            _realPos = rectAroundText(_textBitmap);
            _textSize.Width = _realPos.Width;
            _textSize.Height = _realPos.Height;


            // ************************************************************************************************************************************
            // ****************************************************************** debug code (turn on visibility of text area)
            bool doDrawRectangleAroundTextArea = debugMode;

            if (doDrawRectangleAroundTextArea)
            {
                using (System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(_textBitmap))
                {
                    gfx.DrawRectangle(Pens.Black, _realPos);
                    gfx.DrawRectangle(Pens.Red, _rect);
                }
            }
            // ************************************************************************************************************************************
            // ************************************************************************************************************************************
            
            
        }

        private void updateQuadSize()
        {
            quadLeft = 0.0f - (float)Width / 2.0f;
            quadRight = (float)Width / 2.0f;

            quadBottom = 0.0f - (float)Height / 2.0f;
            quadTop = (float)Height / 2.0f;

            texCoordX0 = 0.0f;
            texCoordX1 = 1.0f;

            texCoordY0 = 0.0f;
            texCoordY1 = 1.0f;
        }

        #region measure text area

        private Rectangle rectAroundText(Bitmap source)
        {
            Rectangle srcRect = default(Rectangle);
            BitmapData data = null;
            try
            {
                data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[data.Height * data.Stride];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                int xMin = int.MaxValue,
                    xMax = int.MinValue,
                    yMin = int.MaxValue,
                    yMax = int.MinValue;

                bool foundPixel = false;

                // Find xMin
                for (int x = 0; x < data.Width; x++)
                {
                    bool stop = false;
                    for (int y = 0; y < data.Height; y++)
                    {
                        byte alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            xMin = x;
                            stop = true;
                            foundPixel = true;
                            break;
                        }
                    }
                    if (stop)
                        break;
                }

                // Image is empty...
                if (!foundPixel)
                    return srcRect;

                // Find yMin
                for (int y = 0; y < data.Height; y++)
                {
                    bool stop = false;
                    for (int x = xMin; x < data.Width; x++)
                    {
                        byte alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            yMin = y;
                            stop = true;
                            break;
                        }
                    }
                    if (stop)
                        break;
                }

                // Find xMax
                for (int x = data.Width - 1; x >= xMin; x--)
                {
                    bool stop = false;
                    for (int y = yMin; y < data.Height; y++)
                    {
                        byte alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            xMax = x;
                            stop = true;
                            break;
                        }
                    }
                    if (stop)
                        break;
                }

                // Find yMax
                for (int y = data.Height - 1; y >= yMin; y--)
                {
                    bool stop = false;
                    for (int x = xMin; x <= xMax; x++)
                    {
                        byte alpha = buffer[y * data.Stride + 4 * x + 3];
                        if (alpha != 0)
                        {
                            yMax = y;
                            stop = true;
                            break;
                        }
                    }
                    if (stop)
                        break;
                }

                srcRect = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
            }
            finally
            {
                if (data != null)
                    source.UnlockBits(data);
            }

            return srcRect;
        }

        #endregion measure text area

    }


}
