using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShogiCore.Drawing {
    /// <summary>
    /// BlunderGraphicsを用いた表示を行うコントロール
    /// </summary>
    public partial class BlunderViewControl : UserControl {
        BlunderGraphics graphics = new BlunderGraphics();
        Bitmap bitmap;

        public BlunderViewControl() {
            InitializeComponent();
            Disposed += new EventHandler(BlunderViewControl_Disposed);

            ClientSize = graphics.Size;
            bitmap = new Bitmap(graphics.Width, graphics.Height);
            using (Graphics g = Graphics.FromImage(bitmap)) {
                g.FillRectangle(Brushes.Black, 0, 0, graphics.Width, graphics.Height);
            }
        }

        /// <summary>
        /// 後始末
        /// </summary>
        void BlunderViewControl_Disposed(object sender, EventArgs e) {
            bitmap.Dispose();
            graphics.Dispose();
        }

        /// <summary>
        /// 描画
        /// </summary>
        public void Draw(Board board) {
            lock (bitmap) {
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    graphics.Draw(g, board);
                }
            }
            Invalidate();
            Update();
        }

        /// <summary>
        /// 背景の描画
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e) {
            // 何も処理させない。
        }

        /// <summary>
        /// 描画
        /// </summary>
        private void BlunderViewControl_Paint(object sender, PaintEventArgs e) {
            lock (bitmap) {
                e.Graphics.DrawImage(bitmap,
                    e.ClipRectangle.X, e.ClipRectangle.Y,
                    e.ClipRectangle, GraphicsUnit.Pixel);
            }
        }
    }
}
