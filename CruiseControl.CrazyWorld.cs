using GTA;

using static CruiseControl.CruiseControl;

namespace CruiseControl {
	public partial class CruiseControl {
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

	public static partial class CrazyWorldStuff {
		public static void DoCrazyWorld(CrazyWorld crazyWorld, Vector3 velocity, Player player) {
			if (!crazyWorld.enabled)
				return;

			if (crazyWorld.vehicles != null && crazyWorld.peds != null && crazyWorld.objects != null) {
				foreach (Vehicle vehicle in crazyWorld.vehicles) {
					if (Game.Exists(vehicle)) {
						vehicle.Velocity = Vector3.Clamp((vehicle.Velocity + Vector3.Normalize(velocity)), Vector3.Zero, velocity);
						vehicle.GetPedOnSeat(VehicleSeat.Driver);
					}
				}
				foreach (Ped pedestrian in crazyWorld.peds) {
					if (Game.Exists(pedestrian)) {
						if (pedestrian != player.Character)
							pedestrian.Velocity += Vector3.Lerp(pedestrian.Velocity, velocity, 1f);
					}
				}

				foreach (GTA.Object gObject in crazyWorld.objects) {
					if (Game.Exists(gObject)) {
						Vehicle tmpVehicle = World.GetClosestVehicle(gObject.Position, 20F);
						if (Game.Exists(tmpVehicle))
							gObject.Velocity += Vector3.Cross(velocity, tmpVehicle.Velocity);
						else
							gObject.Velocity += Vector3.Cross(velocity, Vector3.RandomXYZ());
					}
				}
			}
		}

		public static void CrazyWorld_State(CrazyWorld crazyWorld, CrazyWorld.State state, Player player) {
			if (!crazyWorld.enabled)
				return;

			if (state == CrazyWorld.State.CREATE) {
				crazyWorld.vehicles = World.GetAllVehicles();
				foreach (Vehicle vehicle in crazyWorld.vehicles) {
					if (Game.Exists(vehicle)) {
						if (vehicle != player.Character.CurrentVehicle) {
							vehicle.EveryoneLeaveVehicle();
						}
					}
				}
				crazyWorld.peds = World.GetAllPeds();
				crazyWorld.objects = World.GetAllObjects();
			} else if (state == CrazyWorld.State.DESTROY) {
				crazyWorld.vehicles = null;
				crazyWorld.peds = null;
				crazyWorld.objects = null;
			}
		}
	}
}
