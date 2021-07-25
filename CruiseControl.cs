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
		readonly SettingsFile settings;


#if DEBUG
		GTA.Font screenFont, pixelFont;
		Color screenBoxColor = Color.FromArgb(127, 0, 0, 255);
		Color pixelBoxColor = Color.FromArgb(127, 0, 255, 0);
#endif
		private CrazyWorld crazyWorld;
		private RectangleF cruiseIndicator;
		readonly GTA.Font speedFont = new GTA.Font(20, FontScaling.Pixel);

		float magnitude = 0.0f;
		// Bad things detection
		int vehicleTimeOffWheel = 0;
		int vehicleTimeCrashing = 0;
		//int vehicleTimeSkidding = 0;
		readonly int max_vehicleTimeOffWheel;
		readonly int max_vehicleTimeCrashing;

		readonly bool showSpeed = true;

		public CruiseControl() {
			settings = Settings;
			BindKey(Keys.C, true, true, false, new KeyPressDelegate(ToggleCruiseControl));
			BindConsoleCommand("reloadCruiseControlConf", new ConsoleCommandDelegate(ReloadSettings));
			BindConsoleCommand("reloadCCConf", new ConsoleCommandDelegate(ReloadSettings));
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
			max_vehicleTimeOffWheel = 50;
			max_vehicleTimeCrashing = 75;
			if (!Exists(settings)) {
				if (!File.Exists("CruiseControl.ini")) {
					StreamWriter settingsFile = File.CreateText("CruiseControl.ini");
					settingsFile.WriteLine("[Extras]");
					settingsFile.WriteLine("crazyWorld=false");
					settingsFile.WriteLine("showSpeed=true");
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
				showSpeed = settings.GetValueBool("showSpeed", "Extras", true);
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
			if (showSpeed) {
				e.Graphics.Scaling = FontScaling.ScreenUnits;
				e.Graphics.DrawText(Math.Truncate(cruiseSpeed) + "", 0.85F, 0.85F, Color.White, speedFont);
			}

		}

		public void ToggleCruiseControl() {
			cruiseOn = !cruiseOn;
			if (cruiseOn) {
				cruiseSpeed = Player.Character.CurrentVehicle.Speed;
				magnitude = Player.Character.CurrentVehicle.Velocity.Length();
				vehicleTimeCrashing = 0;
				vehicleTimeOffWheel = 0;
				//vehicleTimeSkidding = 0;
				CrazyWorldStuff.CrazyWorld_State(crazyWorld, CrazyWorld.State.CREATE, Player);
			} else {
				cruiseSpeed = 0.0f;
				magnitude = 0.0f;
				CrazyWorldStuff.CrazyWorld_State(crazyWorld, CrazyWorld.State.DESTROY, Player);
			}
		}

		public void HandleCruise(object sender, EventArgs e) {
			if (!cruiseOn) {
				return;
			} else {
				if (Player.Character.isInVehicle() && Player.Character.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == Player.Character) {
					if (isKeyPressed(Keys.Space))
						cruiseOn = false;

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
						CrazyWorldStuff.DoCrazyWorld(crazyWorld, velocity, Player);
				} else {
					cruiseOn = false;
					cruiseSpeed = 0.0f;
				}
			}
		}

		public void ReloadSettings(ParameterCollection param) {
			settings.Load();
			Game.Console.Print("Reloaded CruiseControl settings!");
		}

		public bool IsCarInWater(Vehicle vehicle) {
			return GTA.Native.Function.Call<bool>("IS_CAR_IN_WATER", vehicle);
		}

		
	}
}
