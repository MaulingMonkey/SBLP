using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows.Forms;

namespace SBLP {
	public class Timer {
		[DllImport("winmm.dll")] static extern uint timeEndPeriod( uint ms );
		[DllImport("winmm.dll")] static extern uint timeBeginPeriod( uint ms );
		[DllImport("winmm.dll")] static extern uint timeGetTime();

		static readonly BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static Timer() {
			timeBeginPeriod(1);
			Application.ApplicationExit += (s,args) => timeEndPeriod(1);
		}

		private uint previous;
		public  int  dMilliseconds          { get; private set; }
		public  long MillisecondsSinceStart { get; private set; }

		public Timer() {
			previous = timeGetTime();
		}

		public Timer( Timer toclone ) {
			foreach ( var field in typeof(Timer).GetFields(All) ) field.SetValue( this, field.GetValue(toclone) );
		}

		public void Update() {
			uint now = timeGetTime();
			MillisecondsSinceStart += dMilliseconds = unchecked((int)(now-previous));
			previous = now;
		}
	}
}
