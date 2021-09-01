using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing.Drawing2D;

namespace GroupNoticeBlocker
{
    public partial class Form2 : Form
    {
        Size FinalSize = new Size(985, 440);
        public Form2()
        {
            InitializeComponent();
        }

        private void OK2_Click(object sender, EventArgs e)
        {
            timer2.Enabled = true;
            timer2.Start();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
        /*    GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse();//pictureBox1.ClientRectangle
            Region region = new Region(gp);
            pictureBox1.Region = region;
            gp.Dispose();
            region.Dispose();
            */
      //      this.Opacity = 0;
           // this.Size = new Size((int)(FinalSize.Width * 3 / 4), (int)(FinalSize.Height * 3 / 4));
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
            double delta = 0.11;
            if (this.Opacity + delta <= 1)
            {
                this.Opacity += delta;
            }
            else
            {
                this.Opacity = 1;
                timer1.Stop();
            }
            
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            timer2.Enabled = true;
            timer2.Start();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            double delta = 0.15;
            if (this.Opacity - delta >= 0)
            {
                this.Opacity -= delta;
            }
            else
            {
                this.Opacity = 0;
                timer2.Stop();
                this.Dispose();
            }
        }
    }
}
