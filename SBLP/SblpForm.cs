using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SBLP {
	[System.ComponentModel.DesignerCategory("")]
	class SblpForm : Form {
		SblpForm() {
			DoubleBuffered = true;
			Text = "SBLP";
			FormBorderStyle = FormBorderStyle.None;
			Bounds = Screen.PrimaryScreen.Bounds;
			StartPosition = FormStartPosition.Manual;

			Steps.Enqueue(0);
			Steps.Enqueue(0);
		}

		static readonly Timer Timer = new Timer();

		readonly Queue<int> Steps = new Queue<int>();

		protected override void OnPaint( PaintEventArgs e ) {
			Timer.Update();

			var dt = Timer.dMilliseconds;
			var fx = e.Graphics;
			var w = ClientSize.Width;
			var h = ClientSize.Height;

			Steps.Enqueue(dt);
			while ( Steps.Count>w ) Steps.Dequeue();

			fx.Clear( Color.Black );
			fx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			fx.DrawLine( Pens.DarkGray, 0, h/2, w, h/2 );
			fx.DrawLines( Pens.White, Steps.Select((ms,i)=>new Point(i,h/2-ms)).ToArray() );

			base.OnPaint(e);
		}

		[STAThread] static void Main() {
			using ( var form = new SblpForm() ) {
				Application.Idle += (s,args) => form.Invalidate();
				Application.Run(form);
			}
		}
	}
}
