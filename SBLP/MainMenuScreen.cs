using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Resources = SBLP.Properties.Resources;

namespace SBLP {
	class MainMenuScreen : IScreen {
		static readonly Bitmap Base = Resources.MainMenuScreen;

		struct MenuEntry {
			public Rectangle DimArea;
			public Action<MainMenuScreen> Select;
		}

		class MenuEntryList : List<MenuEntry> {
			public void Add( Rectangle dimarea, Action<MainMenuScreen> action ) {
				Add( new MenuEntry() { DimArea=dimarea, Select=action } );
			}
		}

		static readonly MenuEntryList Entries = new MenuEntryList
			{ { /* Single Player */ new Rectangle(73,75,53,4), s=>s.StartSinglePlayer() }
			, { /* Multiplayer   */ new Rectangle(77,83,46,4), s=>s.StartMultiplayer()  }
			, { /* Settings      */ new Rectangle(83,91,33,4), s=>s.OpenSettings()      }
			, { /* Quit          */ new Rectangle(92,99,17,4), s=>Application.Exit()    }
			};

		int SelectedIndex = 0;
		bool SelectionChanged = false;
		long SelectionChangeTimestamp = 0;

		public Action StartSinglePlayer, StartMultiplayer, OpenSettings;

		public void Paint( SblpForm form, Graphics fx ) {
			var zoom = Math.Min(form.ClientSize.Width/Base.Width,form.ClientSize.Height/Base.Height);
			if ( zoom<1 ) zoom=1;
			var left = (form.ClientSize.Width -Base.Width *zoom)/2;
			var top  = (form.ClientSize.Height-Base.Height*zoom)/2;

			fx.Clear( Color.FromArgb(unchecked((int)0xFF112233u)) );
			fx.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			fx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			fx.SetClip(new Rectangle(left,top,Base.Width*zoom,Base.Height*zoom));
			fx.TranslateTransform(left,top);
			fx.ScaleTransform(zoom,zoom);

			fx.DrawImage( Base, 0, 0, Base.Width, Base.Height );

			if ( SelectionChanged ) {
				SelectionChangeTimestamp = form.Timer.MillisecondsSinceStart;
				SelectionChanged = false;
			}

			for ( int i=0, n=Entries.Count ; i<n ; ++i ) {
				var f = (i!=SelectedIndex) ? 1 : 1-(Math.Cos(Math.PI*2*(form.Timer.MillisecondsSinceStart-SelectionChangeTimestamp)/500.0)+1)/2;
				using ( var brush = new SolidBrush(Color.FromArgb((int)Math.Round(192*f),Color.Black)) ) {
					fx.FillRectangle(brush,Entries[i].DimArea);
				}
			}
		}

		public void Impulse( Keys key ) {
			switch ( key ) {
			case Keys.Up:
				if ( --SelectedIndex<0 ) SelectedIndex+=Entries.Count;
				SelectionChanged = true;
				break;
			case Keys.Down:
				if ( ++SelectedIndex>=Entries.Count ) SelectedIndex-=Entries.Count;
				SelectionChanged = true;
				break;
			case Keys.Enter:
				Entries[SelectedIndex].Select(this);
				break;
			case Keys.Escape:
				Application.Exit();
				break;
			}
		}
	}
}
