using GTA;

namespace CruiseControl {
	partial class CruiseControl {
		public struct CrazyWorld {
			public enum State {
				CREATE,
				DESTROY
			};

			public bool enabled;
			public Vehicle[] vehicles;
			public Ped[] peds;
			public GTA.Object[] objects;

			public CrazyWorld(bool enabled) {
				this.enabled = enabled;
				vehicles = null;
				peds = null;
				objects = null;
			}
		}
	}
}
