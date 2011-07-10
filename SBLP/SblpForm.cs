using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SlimDX;
using Resources = SBLP.Properties.Resources;

namespace SBLP {
	class Script<A> : List<Script<A>.Entry> {
		public struct Entry {
			public long      When;
			public Action<A> Action;
		}

		public new void Add( Entry entry ) {
			Debug.Assert( Count==0 || entry.When >= this.Last().When );
			base.Add(entry);
		}

		public void Add( long when, Action<A> action ) {
			Add( new Script<A>.Entry()
				{ When   = when
				, Action = action
				});
		}
	}

	class ScriptRunner<A> {
		readonly Script<A> Script;
		int Next = 0;

		public ScriptRunner( Script<A> script ) {
			Script = script;
		}

		public void UpdateTo( long time, A arg ) {
			while ( Next < Script.Count && Script[Next].When<time ) {
				Script[Next].Action(arg);
				++Next;
			}
		}

		public bool IsDone { get { return Next >= Script.Count; }}
	}

	struct Enemy {
		public Bitmap  Bitmap;
		public Vector2 Position, Velocity;
		public int     HP;
	}

	struct Bullet {
		public Bitmap Bitmap;
		public Vector2 Position, Velocity;
		public bool    Dead;
	}

	class Game {
		public readonly List<Bullet> Bullets = new List<Bullet>();
		public readonly List<Enemy>  Enemies = new List<Enemy>();
		public readonly List<ScriptRunner<Game>> Scripts = new List<ScriptRunner<Game>>();

		const long LogicalTimestep = 1000/60;
		long RealTime, LogicalTime;
		public float GraphicalTween { get { return (RealTime-LogicalTime)/1000f; }}
		public void Update( long dTime ) {
			RealTime += dTime;

			while ( LogicalTime<RealTime-LogicalTimestep ) {
				LogicalTime += LogicalTimestep;

				var sstep = LogicalTimestep/1000f;

				for ( int i=0, n=Bullets.Count ; i<n ; ++i ) {
					var b = Bullets[i];
					b.Position += sstep * b.Velocity;
					Bullets[i] = b;
				}

				for ( int i=0, n=Enemies.Count ; i<n ; ++i ) {
					var e = Enemies[i];
					e.Position += sstep * e.Velocity;
					Enemies[i] = e;
				}

				foreach ( var script in Scripts ) {
					script.UpdateTo(LogicalTime,this);
				}

				Bullets.RemoveAll( bullet => bullet.Dead );
				Enemies.RemoveAll( enemy => enemy.HP <= 0 );
				Scripts.RemoveAll( script => script.IsDone );
			}
		}
	}

	static class Level1 {
		static readonly Bitmap Enemy1 = Resources.Enemy1;

		static Action<Game> SpawnV( int _n, int _y ) {
			Debug.Assert(_n>0);

			return g => {
				int n = _n;
				int y = _y;

				int x = 220;
				int dy = 0;

				g.Enemies.Add( new Enemy() { Bitmap=Enemy1, HP=1, Position=new Vector2(x,y), Velocity=new Vector2(-100,0) } );
				--n;

				while (n>0) {
					dy += 20;
					x += 20;
					if (n-->0) g.Enemies.Add( new Enemy() { Bitmap=Enemy1, HP=1, Position=new Vector2(x,y+dy), Velocity=new Vector2(-100,0) } );
					if (n-->0) g.Enemies.Add( new Enemy() { Bitmap=Enemy1, HP=1, Position=new Vector2(x,y-dy), Velocity=new Vector2(-100,0) } );
				}
			};
		}

		public static readonly Script<Game> Script = new Script<Game>()
			{ { 1000, SpawnV(3, 75) }
			, { 2000, SpawnV(3, 25) }
			, { 3000, SpawnV(3,125) }
			, { 4000, SpawnV(3, 75) }
			};
	}

	class GameScreen : IScreen {
		static readonly Bitmap FauxOverlay = Resources.FauxGUI;

		Game Game = new Game()
			{ Scripts = { new ScriptRunner<Game>(Level1.Script) }
			};

		public void Paint( SblpForm form, Graphics fx ) {
			var zoom = Math.Min(form.ClientSize.Width/FauxOverlay.Width,form.ClientSize.Height/FauxOverlay.Height);
			if ( zoom<1 ) zoom=1;
			var left = (form.ClientSize.Width -FauxOverlay.Width *zoom)/2;
			var top  = (form.ClientSize.Height-FauxOverlay.Height*zoom)/2;

			fx.Clear( Color.FromArgb(unchecked((int)0xFF112233u)) );
			fx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			fx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			fx.SetClip(new Rectangle(left,top,FauxOverlay.Width*zoom,FauxOverlay.Height*zoom));
			fx.TranslateTransform(left,top);
			fx.ScaleTransform(zoom,zoom);

			Game.Update(form.Timer.dMilliseconds);

			float dt = Game.GraphicalTween;

			foreach ( var bullet in Game.Bullets ) {
				var img = bullet.Bitmap;
				var xy = bullet.Position + dt*bullet.Velocity - new Vector2(img.Width,img.Height)/2;
				fx.DrawImage( img, xy.X, xy.Y, img.Width, img.Height );
			}

			foreach ( var enemy in Game.Enemies ) {
				var img = enemy.Bitmap;
				var xy = enemy.Position + dt*enemy.Velocity - new Vector2(img.Width,img.Height)/2;
				fx.DrawImage( img, xy.X, xy.Y, img.Width, img.Height );
			}

			fx.DrawImage( FauxOverlay, 0, 0, FauxOverlay.Width, FauxOverlay.Height );
		}

		public void Impulse( Keys key ) {
			if ( key == Keys.Escape ) Quit();
		}

		public Action Quit;
	}

	[System.ComponentModel.DesignerCategory("")]
	class SblpForm : Form {
		SblpForm() {
			DoubleBuffered = true;
			Text = "SBLP";
			FormBorderStyle = FormBorderStyle.None;
			//Bounds = Screen.PrimaryScreen.Bounds;
			//StartPosition = FormStartPosition.Manual;
			ClientSize = new Size(800,600);
			StartPosition = FormStartPosition.CenterScreen;

			Steps.Enqueue(0);
			Steps.Enqueue(0);

			MainMenuScreen mm = null;

			CurrentScreen = mm = new MainMenuScreen()
				{ StartSinglePlayer = () => CurrentScreen = new GameScreen() { Quit = () => CurrentScreen = mm }
				};
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
			}
			fx.ResetTransform();
			fx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			fx.DrawLine( Pens.DarkGray, 0, h/2, w, h/2 );
			fx.DrawLines( Pens.White, Steps.Select((ms,i)=>new Point(i,h/2-ms)).ToArray() );

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
