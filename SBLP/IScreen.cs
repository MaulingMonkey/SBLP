using System.Drawing;
using System.Windows.Forms;

namespace SBLP {
	interface IScreen {
		void Paint( SblpForm form, Graphics fx );
		void Impulse( Keys key );
	}
}
