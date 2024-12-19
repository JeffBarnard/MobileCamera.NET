using CommunityToolkit.Maui.Alerts;
using System.Collections.ObjectModel;

namespace MobileCamera.NET.Pages
{
    public partial class MainPage : ContentPage
    {
        private bool _isStopped = false;
        
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;         
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!await CheckPermissions())
            {
                await Toast.Make("Not all permissions were accepted. Application will close.").Show();
                Application.Current.Quit();
            }
        }

        private async Task<bool> CheckPermissions()
        {
            PermissionStatus bluetoothStatus = await CheckBluetoothPermissions();
            PermissionStatus cameraStatus = await CheckPermissions<Permissions.Camera>();

            return IsGranted(cameraStatus) && IsGranted(bluetoothStatus);
        }

        private async Task<PermissionStatus> CheckBluetoothPermissions()
        {
            PermissionStatus bluetoothStatus = PermissionStatus.Granted;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                if (DeviceInfo.Version.Major >= 12)
                {
                    bluetoothStatus = await CheckPermissions<BluetoothPermissions>();
                }
                else
                {
                    bluetoothStatus = await CheckPermissions<Permissions.LocationWhenInUse>();
                }
            }

            return bluetoothStatus;
        }

        private async Task<PermissionStatus> CheckPermissions<TPermission>() where TPermission : Permissions.BasePermission, new()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<TPermission>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<TPermission>();
            }

            return status;
        }

        private static bool IsGranted(PermissionStatus status)
        {
            return status == PermissionStatus.Granted || status == PermissionStatus.Limited;
        }

#if ANDROID
        //private bool IsLocationServiceEnabled()
        //{
        //    LocationManager locationManager = (LocationManager)Android.App.Application.Context.GetSystemService(Context.LocationService);
        //    return locationManager.IsProviderEnabled(LocationManager.GpsProvider);
        //}
//#elif IOS || MACCATALYST
//        public bool IsLocationServiceEnabled()
//        {
//            return CLLocationManager.Status == CLAuthorizationStatus.Denied;
//        }
//#elif WINDOWS
//    private bool IsLocationServiceEnabled()
//    {
//        Geolocator locationservice = new Geolocator();
//        return locationservice.LocationStatus == PositionStatus.Disabled;
//    }
#endif

        private void Camera_MediaCaptured(object sender, CommunityToolkit.Maui.Views.MediaCapturedEventArgs e)
        {

        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (!_isStopped)
                Camera.StopCameraPreview();
            else
                Camera.StartCameraPreview(default(CancellationToken));

            _isStopped = !_isStopped;
        }
    }
}