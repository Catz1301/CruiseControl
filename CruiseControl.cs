using System;
using System.Windows.Forms;
using GTA;
using System.Drawing;
using GTA.Native;
using System.IO;

namespace CruiseControl {
	partial class CruiseControl : Script {
#if DEBUG

#endif
		bool cruiseOn = false;
		float cruiseSpeed = 0.0f;
		Vector3 velocity;
		Vector3 direction;
		SettingsFile settings;
		
		
#if DEBUG
		GTA.Font screenFont, pixelFont;
		Color screenBoxColor = Color.FromArgb(127, 0, 0, 255);
		Color pixelBoxColor = Color.FromArgb(127, 0, 255, 0);
#endif
		CrazyWorld crazyWorld;
		private RectangleF cruiseIndicator;
		float magnitude = 0.0f;

		// Bad things detection
		int vehicleTimeOffWheel = 0;
		int vehicleTimeCrashing = 0;
		//int vehicleTimeSkidding = 0;
		int max_vehicleTimeOffWheel = 50;
		int max_vehicleTimeCrashing = 75;

		GTA.Font font = new GTA.Font(20, FontScaling.Pixel);
		public CruiseControl() {
			settings = Settings;
			BindKey(Keys.C, true, true, false, new KeyPressDelegate(toggleCruiseControl));
			BindConsoleCommand("reloadCruiseControlConf", new ConsoleCommandDelegate(reloadSettings));
			BindConsoleCommand("reloadCCConf", new ConsoleCommandDelegate(reloadSettings));
			this.Tick += new System.EventHandler(HandleCruise);
#if DEBUG
			screenFont = new GTA.Font(0.02F, FontScaling.ScreenUnits);
			screenFont.Color = Color.Red;

			pixelFont = new GTA.Font(15.0F, FontScaling.Pixel);
			pixelFont.Color = Color.Red; 
#endif
			direction = new Vector3();
			velocity = new Vector3();
			this.PerFrameDrawing += new GraphicsEventHandler(this.DrawingExample_PerFrameDrawing);
			bool doCrazyWorld = false;
			if (!Exists(settings)) {
				if (!File.Exists("CruiseControl.ini")) {
					StreamWriter settingsFile = File.CreateText("CruiseControl.ini");
					settingsFile.WriteLine("[Extras]");
					settingsFile.WriteLine("crazyWorld=false");
					settingsFile.WriteLine("[SafteyDetection]");
					settingsFile.WriteLine("maxVehicleTimeOffWheel=50");
					settingsFile.WriteLine("maxVehicleTimeCrashing=75");
					//settingsFile.WriteLine("crazyWorld=false");
					settingsFile.Close();
				}
			} else {
				doCrazyWorld = settings.GetValueBool("crazyWorld", "Extras", false);
				max_vehicleTimeOffWheel = settings.GetValueInteger("maxVehicleTimeOffWheel", "SafetyDetection", 50);
				max_vehicleTimeCrashing = settings.GetValueInteger("maxVehicleTimeCrashing", "SafetyDetection", 75);
			}
			crazyWorld = new CrazyWorld(doCrazyWorld);
			string txt;
			if (doCrazyWorld)
				txt = "CrazyWorld is On";
			else
				txt = "CrazyWorld is Off";
			Game.DisplayText(txt);
			cruiseIndicator = new RectangleF(8, 8, 16, 16);
			
		}

		private void DrawingExample_PerFrameDrawing(object sender, GraphicsEventArgs e) {
#if DEBUG
			if (isKeyPressed(Keys.NumPad0)) {
				e.Graphics.Scaling = FontScaling.Pixel; // fixed amount of pixels, size on screen will differ for each resolution
				RectangleF rect = new RectangleF(64, 64, 512, 512);
				e.Graphics.DrawRectangle(rect, pixelBoxColor);
				if (Player.Character.isInVehicle()) {
					e.Graphics.DrawText("X: " + vehicleTimeOffWheel, rect, TextAlignment.Center | TextAlignment.VerticalCenter, pixelFont);
					if (Player.Character.CurrentVehicle.isUpright) {
						e.Graphics.Scaling = FontScaling.ScreenUnits; // size on screen will always be the same, regardless of resolution
						rect = new RectangleF(0.65F, 0.65F, 0.3F, 0.3F);
						e.Graphics.DrawRectangle(rect, screenBoxColor);
						e.Graphics.DrawText("G" + Player.Character.CurrentVehicle.Velocity.ToGround().ToString() + " V" + velocity.ToString(), rect, TextAlignment.Center | TextAlignment.VerticalCenter, screenFont);
					}
				}
			} 
#endif
			e.Graphics.Scaling = FontScaling.Pixel;
			if (cruiseOn)
				e.Graphics.DrawRectangle(cruiseIndicator, Color.Yellow);
		}

		public void toggleCruiseControl() {
			cruiseOn = !cruiseOn;
			if (cruiseOn) {
				cruiseSpeed = Player.Character.CurrentVehicle.Speed;
				magnitude = Player.Character.CurrentVehicle.Velocity.Length();
				vehicleTimeCrashing = 0;
				vehicleTimeOffWheel = 0;
				//vehicleTimeSkidding = 0;
				CrazyWorld_State(crazyWorld, CrazyWorld.State.CREATE);
			} else {
				cruiseSpeed = 0.0f;
				magnitude = 0.0f;
				CrazyWorld_State(crazyWorld, CrazyWorld.State.DESTROY);
			}
		}

		public void HandleCruise(object sender, EventArgs e) {
			if (!cruiseOn) {
				return;
			} else {
				if (Player.Character.isInVehicle() && Player.Character.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == Player.Character) {
					float orginalZ = velocity.Z;
					direction = Player.Character.CurrentVehicle.Velocity;
					velocity = Vector3.Normalize(direction) * magnitude;
#if DEBUG
					if (isCarInWater(Player.Character.CurrentVehicle))
						Game.DisplayText("You're in the water!"); 
#endif
					//Player.Character.CurrentVehicle.Velocity = velocity;
					Player.Character.CurrentVehicle.Velocity = new Vector3(velocity.X, velocity.Y, orginalZ);
					if (Math.Abs(cruiseSpeed - Player.Character.CurrentVehicle.Speed) > 10)
						vehicleTimeCrashing++;
					else
						vehicleTimeCrashing -= 2;
					if (vehicleTimeCrashing < 0)
						vehicleTimeCrashing = 0;

					if (!Player.Character.CurrentVehicle.isOnAllWheels)
						vehicleTimeOffWheel++;
					else
						vehicleTimeOffWheel -= 2;
					if (vehicleTimeOffWheel < 0)
						vehicleTimeOffWheel = 0;

					/*if (Math.Abs(Player.Character.CurrentVehicle.Velocity.ToHeading() - Player.Character.CurrentVehicle.Heading) >= 5)
						vehicleTimeSkidding++;
					else
						vehicleTimeSkidding -= 2;
					if (vehicleTimeSkidding < 0)
						vehicleTimeSkidding = 0;*/

					if (vehicleTimeOffWheel >= max_vehicleTimeOffWheel)
						cruiseOn = false;
					if (vehicleTimeCrashing >= max_vehicleTimeCrashing)
						cruiseOn = false;
					/*if (vehicleTimeSkidding >= 50)
						cruiseOn = false;*/

					if (crazyWorld.enabled)
						doCrazyWorld(crazyWorld);
				} else {
					cruiseOn = false;
					cruiseSpeed = 0.0f;
				}
			}
		}

		public void reloadSettings(ParameterCollection param) {
			settings.Load();
			Game.Console.Print("Reloaded CruiseControl settings!");
		}

		public bool isCarInWater(Vehicle vehicle) {
			return GTA.Native.Function.Call<bool>("IS_CAR_IN_WATER", vehicle);
		}

		private void doCrazyWorld(CrazyWorld crazyWorld) {
			if (!crazyWorld.enabled)
				return;

			if (crazyWorld.vehicles != null && crazyWorld.peds != null && crazyWorld.objects != null) {
				foreach (Vehicle vehicle in crazyWorld.vehicles) {
					if (Exists(vehicle)) {
						vehicle.Velocity = Vector3.Clamp((vehicle.Velocity + Vector3.Normalize(velocity)), Vector3.Zero, velocity);
						vehicle.GetPedOnSeat(VehicleSeat.Driver);
					}
				}
				foreach (Ped pedestrian in crazyWorld.peds) {
					if (Exists(pedestrian)) {
						if (pedestrian != Player.Character)
							pedestrian.Velocity += Vector3.Lerp(pedestrian.Velocity, velocity, 1f);
					}
				}

				foreach (GTA.Object gObject in crazyWorld.objects) {
					if (Exists(gObject)) {
						Vehicle tmpVehicle = World.GetClosestVehicle(gObject.Position, 20F);
						if (Exists(tmpVehicle))
							gObject.Velocity += Vector3.Cross(velocity, tmpVehicle.Velocity);
						else
							gObject.Velocity += Vector3.Cross(velocity, Vector3.RandomXYZ());
					}
				}
			}
		}

		private void CrazyWorld_State(CrazyWorld crazyWorld, CrazyWorld.State state) {
			if (!crazyWorld.enabled)
				return;
			
			if (state == CrazyWorld.State.CREATE) {
				crazyWorld.vehicles = World.GetAllVehicles();
				foreach (Vehicle vehicle in crazyWorld.vehicles) {
					if (Exists(vehicle)) {
						if (vehicle != Player.Character.CurrentVehicle) {
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
