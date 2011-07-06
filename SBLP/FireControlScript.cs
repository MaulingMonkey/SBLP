using System;
using System.Collections.Generic;
using SlimDX;
using System.Diagnostics;

namespace SBLP {
	class BulletType {
		public readonly static BulletType
			Pointy   = new BulletType(),
			Fireball = new BulletType();
	}

	interface IFireControlEvent {
		int When { get; set; }
	}

	class FireEvent : List<FireEvent.Entry>, IFireControlEvent {
		public struct Entry {
			public BulletType BulletType;
			public Vector2    RelativePosition;
			public Vector2    RelativeVelocity;
		}

		public int When { get; set; }

		public void Add( BulletType bt, double dx, double dy ) {
			Add( new Entry()
				{ BulletType = bt
				, RelativePosition = new Vector2(0,0)
				, RelativeVelocity = new Vector2((float)dx,(float)dy)
				});
		}
	}

	class FireControlScript : List<IFireControlEvent> {
		public int TotalDuration { get; private set; }
		public bool Looping { get; set; }

		public void Add( int delay ) {
			TotalDuration += delay;
		}

		public new void Add( IFireControlEvent e ) {
			e.When = TotalDuration;
			base.Add(e);
		}

		public void Add( params Action<FireControlScript>[] modifiers ) {
			foreach ( var m in modifiers ) m(this);
		}

		static FireEvent Fire( BulletType bt, double dx, double dy ) {
			return new FireEvent() { { bt, dx, dy } };
		}

		static Action<FireControlScript> Loop = fcs => fcs.Looping = true;

		public static readonly FireControlScript
			Sprayer = new FireControlScript()
				{ 100, Fire( BulletType.Pointy, - 70, -100 )
				, 100, Fire( BulletType.Pointy, - 90, - 50 )
				, 100, Fire( BulletType.Pointy, -100,    0 )
				, 100, Fire( BulletType.Pointy, - 90, + 50 )
				, 100, Fire( BulletType.Pointy, - 70, +100 )
				, 100, Fire( BulletType.Pointy, - 90, + 50 )
				, 100, Fire( BulletType.Pointy, -100,    0 )
				, 100, Fire( BulletType.Pointy, - 90, - 50 )
				, Loop
				},
			ScannerDown = new FireControlScript()
				{ 100, Fire( BulletType.Pointy, - 70, -100 )
				, 100, Fire( BulletType.Pointy, - 90, - 50 )
				, 100, Fire( BulletType.Pointy, -100,    0 )
				, 100, Fire( BulletType.Pointy, - 90, + 50 )
				, 100, Fire( BulletType.Pointy, - 70, +100 )
				, Loop
				},
			ScannerUp = new FireControlScript()
				{ 100, Fire( BulletType.Pointy, - 70, +100 )
				, 100, Fire( BulletType.Pointy, - 90, + 50 )
				, 100, Fire( BulletType.Pointy, -100,    0 )
				, 100, Fire( BulletType.Pointy, - 90, - 50 )
				, 100, Fire( BulletType.Pointy, - 70, -100 )
				, Loop
				},
			DoubleSprayer = new FireControlScript()
				{ 150, new FireEvent()
					{ { BulletType.Pointy, - 70, -100 }
					, { BulletType.Pointy, - 90, - 50 }
					}
				, 150, new FireEvent()
					{ { BulletType.Pointy, - 90, - 50 }
					, { BulletType.Pointy, -100,    0 }
					}
				, 150, new FireEvent()
					{ { BulletType.Pointy, -100,    0 }
					, { BulletType.Pointy, - 90, + 50 }
					}
				, 150, new FireEvent()
					{ { BulletType.Pointy, - 90, + 50 }
					, { BulletType.Pointy, - 70, +100 }
					}
				, 150, new FireEvent()
					{ { BulletType.Pointy, -100,    0 }
					, { BulletType.Pointy, - 90, + 50 }
					}
				, 150, new FireEvent()
					{ { BulletType.Pointy, - 90, - 50 }
					, { BulletType.Pointy, -100,    0 }
					}
				, Loop
				},
			Shotgunner = new FireControlScript()
				{ 1000, new FireEvent()
					{ { BulletType.Pointy, - 70, -100 }
					, { BulletType.Pointy, - 90, - 50 }
					, { BulletType.Pointy, -100,    0 }
					, { BulletType.Pointy, - 90, + 50 }
					, { BulletType.Pointy, - 70, +100 }
					}
				, Loop
				},
			PulseShotgunner = new FireControlScript()
				{ 500, new FireEvent()
					{ { BulletType.Pointy, - 70, -100 }
					, { BulletType.Pointy, - 90, - 50 }
					, { BulletType.Pointy, -100,    0 }
					, { BulletType.Pointy, - 90, + 50 }
					, { BulletType.Pointy, - 70, +100 }
					}
				, 500, new FireEvent()
					{ { BulletType.Pointy, - 70, -100 }
					, { BulletType.Pointy, - 90, - 50 }
					, { BulletType.Pointy, -100,    0 }
					, { BulletType.Pointy, - 90, + 50 }
					, { BulletType.Pointy, - 70, +100 }
					}
				, 500, new FireEvent()
					{ { BulletType.Pointy, - 70, -100 }
					, { BulletType.Pointy, - 90, - 50 }
					, { BulletType.Pointy, -100,    0 }
					, { BulletType.Pointy, - 90, + 50 }
					, { BulletType.Pointy, - 70, +100 }
					}
				, 1500
				, Loop
				};
	}

	class FireControlScriptRunner {
		readonly FireControlScript Script;
		long Time = 0;

		public FireControlScriptRunner( FireControlScript fcs ) {
			Script = fcs;
			Debug.Assert(Script.TotalDuration>0);
		}

		public void Update( long dt, Game game, Enemy spawner ) {
			var prev = Time;
			var now  = Time + dt;

			do {
				Time = now;

				foreach ( var scriptEntry in Script )
				if ( prev < scriptEntry.When && scriptEntry.When <= now )
				{
					var dt_prespawn  = (scriptEntry.When-prev)/1000.0f;
					var dt_postspawn = (now-scriptEntry.When)/1000.0f;

					var fireEvent = scriptEntry as FireEvent;
					if ( fireEvent != null ) {
						foreach ( var bulletSpawn in fireEvent ) {
							var bullet = new Bullet()
								{ BulletType = bulletSpawn.BulletType
								};
							bullet.Velocity = spawner.Velocity + bulletSpawn.RelativeVelocity;
							bullet.Position = spawner.Position + dt_prespawn * spawner.Velocity + dt_postspawn * bullet.Velocity;
							// FIXME: Integrate with movement scripts somehow when those are introduced.
							// Probably by querying e.g. spawner.PositionAtTime(scriptEntry.When + LoopCount*TotalDuration) or equivalent

							game.Bullets.Add(bullet);
						}
					} else {
						throw new NotImplementedException();
					}
				}

				prev = 0;
				now -= Script.TotalDuration;
			} while ( now>0 );
		}
	}
}
