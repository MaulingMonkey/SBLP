using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SlimDX;
using Spawn = SBLP.LevelScript.Spawn;

namespace SBLP {
	class Enemy {
		public Vector2 Position, Velocity;
		public FireControlScriptRunner FireControl;

		public RectangleF AABB { get {
			return new RectangleF(Position.X-5,Position.Y-5,10,10);
		}}
	}

	struct Bullet {
		public BulletType BulletType;
		public Vector2 Position, Velocity;
	}

	class Game {
		public LevelDescription Level;
		public readonly List<Bullet> Bullets = new List<Bullet>();
		public readonly List<Enemy> Enemies = new List<Enemy>();
	}

	class LevelScript : List<LevelScript.Event> {
		public class Event {
			public long When;
		}
		public class Spawn : Event {
			public Vector2 Center;
			public Vector2 InitialVelocity;
			public FireControlScript FireControlScript;
		}

		public void Add( int ms, Event e ) {
			Debug.Assert(e.When==0);
			e.When = ms;
			Add(e);
		}
	}

	class EnemyDescription {
	}

	class LevelDescription {
		public Rectangle   Dimensions;
		public LevelScript Script;

		public static readonly LevelDescription Level1 = new LevelDescription()
			{ Dimensions = Rectangle.FromLTRB(-366,-100,-100,+100)
			, Script = new LevelScript()
				{ { 2000, new Spawn() { Center = new Vector2(0,  0), InitialVelocity = new Vector2(-100,0), FireControlScript = FireControlScript.Shotgunner    } }
				, { 4000, new Spawn() { Center = new Vector2(0,+75), InitialVelocity = new Vector2(-100,0), FireControlScript = FireControlScript.ScannerUp     } }
				, { 6000, new Spawn() { Center = new Vector2(0,-75), InitialVelocity = new Vector2(-100,0), FireControlScript = FireControlScript.ScannerDown   } }
				, { 8000, new Spawn() { Center = new Vector2(0,  0), InitialVelocity = new Vector2(- 50,0), FireControlScript = FireControlScript.DoubleSprayer } }
				}
			};
	}

	class GameScreen : IScreen {
		Game Game = new Game()
			{ Level = LevelDescription.Level1
			};
		long Time;

		public Action OnGameOver;

		void Update( Enemy e, long dt ) {
			if ( dt==0 ) return;
			if ( e.FireControl != null ) e.FireControl.Update( dt, Game, e );
			e.Position += dt * e.Velocity / 1000.0f;
		}

		void Update( ref Bullet b, long dt ) {
			if ( dt==0 ) return;
			b.Position += dt * b.Velocity / 1000.0f;
		}

		void Update( SblpForm form ) {
			var dt = form.Timer.dMilliseconds;
			var prev = Time;
			var now  = Time += dt;

			int enemy_spawn_watermark  = Game.Enemies.Count;
			int bullet_spawn_watermark = Game.Bullets.Count;

			var new_events = Game.Level.Script.Where(e=>prev<e.When&&e.When<=now);

			foreach ( var e in new_events ) {
				var spawn = e as Spawn;
				if ( spawn != null ) {
					var enemy = new Enemy() { Position=spawn.Center, Velocity=spawn.InitialVelocity, FireControl = new FireControlScriptRunner(spawn.FireControlScript) };
					Game.Enemies.Add( enemy );
					Update(enemy,now-e.When);
				} else {
					throw new NotImplementedException();
				}
			}

			for ( int i=0 ; i<enemy_spawn_watermark ; ++i ) {
				Update(Game.Enemies[i],dt);
			}

			for ( int i=0 ; i<bullet_spawn_watermark ; ++i ) {
				var b = Game.Bullets[i];
				Update(ref b,dt);
				Game.Bullets[i] = b;
			}
		}

		public void Paint( SblpForm form, Graphics fx ) {
			Update(form);

			var dims = Game.Level.Dimensions;
			var zoom = Math.Min(form.ClientSize.Width/dims.Width,form.ClientSize.Height/dims.Height);
			if ( zoom<1 ) zoom=1;
			var left = (form.ClientSize.Width -dims.Width *zoom)/2;
			var top  = (form.ClientSize.Height-dims.Height*zoom)/2;

			fx.Clear( Color.FromArgb(unchecked((int)0xFF112233u)) );
			fx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			fx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			fx.SetClip(new Rectangle(left,top,dims.Width*zoom,dims.Height*zoom));
			fx.TranslateTransform(left,top);
			fx.ScaleTransform(zoom,zoom);
			fx.TranslateTransform(-dims.Left,-dims.Top);

			foreach ( var enemy in Game.Enemies ) {
				var tl = enemy.AABB.Location;
				var br = enemy.AABB.Location;
				br.X += enemy.AABB.Width;
				br.Y += enemy.AABB.Height;

				fx.FillRectangle( Brushes.White, tl.X, tl.Y, br.X-tl.X, br.Y-tl.Y );
			}

			foreach ( var bullet in Game.Bullets ) {
				fx.FillEllipse( Brushes.Red, bullet.Position.X-2, bullet.Position.Y-2, 4, 4 );
			}
		}

		public void Impulse( Keys key ) {
			switch ( key ) {
			case Keys.Escape:
				OnGameOver();
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

			MainMenuScreen mm = null;

			CurrentScreen = mm = new MainMenuScreen()
				{ StartSinglePlayer = () => CurrentScreen = new GameScreen()
					{ OnGameOver = () => CurrentScreen = mm
					}
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
