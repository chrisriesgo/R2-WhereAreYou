using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Locations;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.OS;
using RadiusNetworks.IBeaconAndroid;
using Region = RadiusNetworks.IBeaconAndroid.Region;

namespace R2_WhereAreYou
{
    [Activity(Label = "R2 - Where Are You", MainLauncher = true, Icon = "@drawable/r2_icon", ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class FindR2Activity : Activity, IBeaconConsumer
    {
        private readonly IBeaconManager iBeaconManager;
        private readonly MonitorNotifier monitorNotifier;
        private readonly RangeNotifier rangeNotifier;
        private readonly Region monitoringRegion;
        private readonly Region rangingRegion;
        private LinearLayout dashboard;
        private Button trackButton;
        private TextView trackingTextView;
        private ImageView progressImageView;
        private int previousProximity;

        private const string UUID = "e2c56db5dffb48d2b060d0f5a71096e0";

        public FindR2Activity()
        {
            iBeaconManager = IBeaconManager.GetInstanceForApplication(this);
            monitorNotifier = new MonitorNotifier();
            rangeNotifier = new RangeNotifier();
            monitoringRegion = new Region("r2MonitoringUniqueId", UUID, null, null);
            rangingRegion = new Region("r2RangingUniqueId", UUID, null, null);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Dashboard);

            dashboard = FindViewById<LinearLayout>(Resource.Id.dashboardLayout);
            trackButton = FindViewById<Button>(Resource.Id.track);
            trackingTextView = FindViewById<TextView>(Resource.Id.tracking_text);
            progressImageView = FindViewById<ImageView>(Resource.Id.progressImage);

            trackButton.Enabled = false;

            iBeaconManager.Bind(this);
        }

        protected override void OnResume()
        {
            base.OnResume();
            trackButton.Click += ChangeTracking;

            monitorNotifier.EnterRegionComplete += EnteredRegion;
            monitorNotifier.ExitRegionComplete += ExitedRegion;

            rangeNotifier.DidRangeBeaconsInRegionComplete += RangingBeaconsInRegion;
        }

        protected override void OnPause()
        {
            base.OnPause();
            trackButton.Click -= ChangeTracking;

            monitorNotifier.EnterRegionComplete -= EnteredRegion;
            monitorNotifier.ExitRegionComplete -= ExitedRegion;

            rangeNotifier.DidRangeBeaconsInRegionComplete -= RangingBeaconsInRegion;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            iBeaconManager.UnBind(this);
        }

        private void ChangeTracking(object sender, EventArgs e)
        {
            if (trackButton.Text == GetString(Resource.String.StartTracking))
            {
                StartTracking();
            }
            else
            {
                StopTracking();
            }
        }

        private void EnteredRegion(object sender, MonitorEventArgs e)
        {
            LogToDisplay("Tracking R2");
        }

        private void ExitedRegion(object sender, MonitorEventArgs e)
        {
            LogToDisplay("R2 is out of range.");
            RunOnUiThread(() => progressImageView.SetImageDrawable(Resources.GetDrawable(Resource.Drawable.sand_people_sq)));
        }

        private void RangingBeaconsInRegion(object sender, RangeEventArgs e)
        {
            try
            {
                if (e.Beacons == null || e.Beacons.Count <= 0) return;
                var beacon = e.Beacons.FirstOrDefault();
                if (beacon == null) return;

                switch ((ProximityType)beacon.Proximity)
                {
                    case ProximityType.Immediate:
                        dashboard.Background = new ColorDrawable(Color.Green);
                        break;
                    case ProximityType.Near:
                        dashboard.Background = new ColorDrawable(Color.Blue);
                        break;
                    case ProximityType.Far:
                        dashboard.Background = new ColorDrawable(Color.Red);
                        break;
                    case ProximityType.Unknown:
                        dashboard.Background = new ColorDrawable(Color.Black);
                        break;
                }

                LogToDisplay("R2 is about " + Convert.ToDecimal(beacon.Accuracy).ToString("#.##") + " meters away.");
                ChangeProgressImage(beacon.Proximity);
                previousProximity = beacon.Proximity;
            }
            catch (Exception ex)
            {
                Log.Error("R2-WhereAreYou.FindR2Activity", ex.Message);
                throw;
            }
        }

        private void StartTracking()
        {
            try
            {
                iBeaconManager.StartMonitoringBeaconsInRegion(monitoringRegion);
                iBeaconManager.StartRangingBeaconsInRegion(rangingRegion);
                trackButton.Text = GetString(Resource.String.StopTracking);
                LogToDisplay("Searching for R2 . . .");
            }
            catch (Exception ex)
            {
                Log.Error("FindR2Activity.StartTracking", ex.Message);
                throw;
            }
        }

        private void StopTracking()
        {
            try
            {
                iBeaconManager.StopMonitoringBeaconsInRegion(monitoringRegion);
                iBeaconManager.StopRangingBeaconsInRegion(rangingRegion);
                trackButton.Text = GetString(Resource.String.StartTracking);
                LogToDisplay("No longer searching for R2");
                dashboard.Background = new ColorDrawable(Color.Black);
            }
            catch (Exception ex)
            {
                Log.Error("FindR2Activity.StopTracking", ex.Message);
                throw;
            }
        }


        public void OnIBeaconServiceConnect()
        {
            iBeaconManager.SetMonitorNotifier(monitorNotifier);
            iBeaconManager.SetRangeNotifier(rangeNotifier);

            trackButton.Enabled = true;
        }

        private void ChangeProgressImage(int proximity)
        {
            Drawable image = null;
            switch ((ProximityType)proximity)
            {
                case ProximityType.Immediate:
                    image = Resources.GetDrawable(Resource.Drawable.r2_sq);
                    break;
                default:
                    image = Resources.GetDrawable(Resource.Drawable.sand_sq);
                    break;
            }

            if (image == null) return;
            RunOnUiThread(() => progressImageView.SetImageDrawable(image));
        }

        private void LogToDisplay(string text)
        {
            RunOnUiThread(() => trackingTextView.Text = text);
        }
    }
}

