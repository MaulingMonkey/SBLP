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

			MainMenuScreen mm = null;

			CurrentScreen = mm = new MainMenuScreen() {};
		}

		public readonly Timer Timer = new Timer();
		readonly Queue<int> Steps = new Queue<int>();
		IScreen CurrentScreen;

		protected override void OnPaint( PaintEventArgs e ) {
			Timer.Update();

			var dt = Timer.dMilliseconds;
			var fx = e.Graphics;
			var w = ClientSize.Width;
			var h = ClientSize.Height;

			Steps.Enqueue(dt);
			while ( Steps.Count>w ) Steps.Dequeue();

			if ( CurrentScreen != null ) {
				foreach ( var key in Tapped ) CurrentScreen.Impulse(key);
				foreach ( var key in Held.Keys.Except(Tapped) ) {
					var a = Timer.MillisecondsSinceStart-Timer.dMilliseconds-Held[key];
					var b = Timer.MillisecondsSinceStart-Held[key];
					a /= RepeatFrequency;
					b /= RepeatFrequency;
					for ( long i=0, n=b-a ; i<n ; ++i ) {
						CurrentScreen.Impulse(key);
					}
				}
				Tapped.Clear();
				CurrentScreen.Paint( this, fx );
			} else {
				fx.Clear( Color.Black );
				fx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				fx.DrawLine( Pens.DarkGray, 0, h/2, w, h/2 );
				fx.DrawLines( Pens.White, Steps.Select((ms,i)=>new Point(i,h/2-ms)).ToArray() );
			}

			base.OnPaint(e);
		}

		readonly HashSet<Keys> Tapped = new HashSet<Keys>();
		readonly Dictionary<Keys,long> Held = new Dictionary<Keys,long>();
		long RepeatFrequency = 200;

		protected override void OnKeyPress( KeyPressEventArgs e ) {
			base.OnKeyPress(e);
		}

		protected override void OnKeyDown( KeyEventArgs e ) {
			if (!Held.ContainsKey(e.KeyCode)) {
				Held.Add(e.KeyCode,Timer.MillisecondsSinceStart);
				Tapped.Add(e.KeyCode);
			}
			base.OnKeyDown(e);
		}

		protected override void OnKeyUp( KeyEventArgs e ) {
			Held.Remove(e.KeyCode);
			base.OnKeyUp(e);
		}

		[STAThread] static void Main() {
			using ( var form = new SblpForm() ) {
				Application.Idle += (s,args) => form.Invalidate();
				Application.Run(form);
			}
		}
	}
}
