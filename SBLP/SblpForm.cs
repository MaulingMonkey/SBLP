using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SlimDX;
using Resources = SBLP.Properties.Resources;

namespace SBLP {
	interface IScreen {
		void Paint( SblpForm form, Graphics fx );
		void Impulse( Keys key );
	}

	class LocalPlayer {
		public Vector2 Position;
	}

	class Game {
		Timer Timer = new Timer();
		public readonly List<LocalPlayer> LocalPlayers = new List<LocalPlayer>();
	}

	class GameScreen : IScreen {
		public void Paint( SblpForm form, Graphics fx ) {
		}
		public void Impulse( Keys key ) {
		}
	}

	class MainMenuScreen : IScreen {
		static readonly Bitmap Base = Resources.MainMenuScreen;

		struct MenuEntry {
			public Rectangle DimArea;
			public Action    Select;
		}

		class MenuEntryList : List<MenuEntry> {
			public void Add( Rectangle dimarea, Action action ) {
				Add( new MenuEntry() { DimArea=dimarea, Select=action } );
			}
		}

		static readonly MenuEntryList Entries = new MenuEntryList
			{ { new Rectangle(75,72,38,4), ()=>{} }
			, { new Rectangle(72,92,41,4), ()=>Application.Exit() }
			};

		int SelectedIndex = 0;

		public void Paint( SblpForm form, Graphics fx ) {
			var zoom = Math.Min(form.ClientSize.Width/Base.Width,form.ClientSize.Height/Base.Height);
			if ( zoom<0 ) zoom=1;
			var left = (form.ClientSize.Width -Base.Width *zoom)/2;
			var top  = (form.ClientSize.Height-Base.Height*zoom)/2;

			fx.Clear( Color.FromArgb(unchecked((int)0xFF112233u)) );
			fx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			fx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			fx.SetClip(new Rectangle(left,top,Base.Width*zoom,Base.Height*zoom));
			fx.TranslateTransform(left,top);
			fx.ScaleTransform(zoom,zoom);

			fx.DrawImage( Base, 0, 0, Base.Width, Base.Height );

			for ( int i=0, n=Entries.Count ; i<n ; ++i ) {
				var f = (i!=SelectedIndex) ? 1 : (Math.Cos(Math.PI*2*form.Timer.MillisecondsSinceStart/1000.0)+1)/2;
				using ( var brush = new SolidBrush(Color.FromArgb((int)Math.Round(128*f),Color.Black)) ) {
					fx.FillRectangle(brush,Entries[i].DimArea);
				}
			}
		}

		public void Impulse( Keys key ) {
			switch ( key ) {
			case Keys.Up:
				if ( --SelectedIndex<0 ) SelectedIndex+=Entries.Count;
				break;
			case Keys.Down:
				if ( ++SelectedIndex>=Entries.Count ) SelectedIndex-=Entries.Count;
				break;
			case Keys.Enter:
				Entries[SelectedIndex].Select();
				break;
			case Keys.Escape:
				Application.Exit();
				break;
			}
		}
	}

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

		public readonly Timer Timer = new Timer();
		readonly Queue<int> Steps = new Queue<int>();
		IScreen CurrentScreen = new MainMenuScreen();

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
					var a = Timer.MillisecondsSinceStart-Held[key];
					var b = Timer.MillisecondsSinceStart-Timer.dMilliseconds-Held[key];
					a /= RepeatFrequency;
					b /= RepeatFrequency;
					for ( long i=0, n=b-a ; i<n ; ++i ) CurrentScreen.Impulse(key);
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
		long RepeatFrequency = 500;

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
