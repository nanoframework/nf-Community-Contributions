using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace BufferedBleSppXamarin
{
    internal class StartupPage : ContentPage
    {
        const string BLE_DEVICE_NAME = "BufferedBleSppServer";


        IBluetoothLE ble;
        IAdapter adapter;
        List<IDevice> deviceList = new List<IDevice>();

        BufferedBleSpp spp;

        ProgressBar progressBar;
        Label messageLabel;
        public StartupPage()
        {
            progressBar = new ProgressBar();

            messageLabel = new Label();
            var connectButton = new Button { Text = "Connect" };
            connectButton.Clicked += async (s, e) => {
                try
                {
                    IDevice device = deviceList.FirstOrDefault(o => o.Name == BLE_DEVICE_NAME);
                    if (device == null)
                    {
                        messageLabel.Text = $"Device {device.Name} has not been detected during scan";
                        return;
                    }
                    spp = new BufferedBleSpp(adapter, device);
                    await spp.Connect();
                    messageLabel.Text = $"Device [{device.Name}] connected";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    messageLabel.Text = "Problem";
                }
            };


            var sendButton = new Button { Text = "Send message" };
            sendButton.Clicked += async (s, e) => {
                try
                {
                    messageLabel.Text = "";
                    var message = "";
                    for (int i = 0; i < 10000; i++) message += " " + i.ToString();
                    var messageArray = Encoding.UTF8.GetBytes(message);
                    progressBar.Progress = 0.0f;
                    var returnedData = await spp.SendMessage(messageArray, new CancellationTokenSource(30000).Token, (progress) => { 
                        progressBar.Progress = progress;
                    });
                    messageLabel.Text = "Receive success";
                    Debug.WriteLine(Encoding.UTF8.GetString(returnedData));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    messageLabel.Text = "Problem";
                }
            };


            Content = new StackLayout { Children = { progressBar, messageLabel, connectButton, sendButton } };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            #region *// Permissions
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Denied)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status == PermissionStatus.Denied)
                {
                    throw new ApplicationException("Location permission denied");
                }
            }

            status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
            if (status == PermissionStatus.Denied)
            {
                status = await Permissions.RequestAsync<Permissions.StorageRead>();
                if (status == PermissionStatus.Denied)
                {
                    throw new ApplicationException("Storage read permission denied");
                }
            }

            status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (status == PermissionStatus.Denied)
            {
                status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                if (status == PermissionStatus.Denied)
                {
                    throw new ApplicationException("Storage write permission denied");
                }
            }
            #endregion

            #region *// BLE initialisation
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += async (s, a) =>
            {
                deviceList.Add(a.Device);
                if (a.Device.Name == null) return;
                if (a.Device.Name.Contains(BLE_DEVICE_NAME))
                {
                    await adapter.StopScanningForDevicesAsync();
                }
            };
            adapter.ScanTimeout = 20000;
            #endregion

            #region *// Scanning for devices
            messageLabel.Text = "Discovering...";
            await adapter.StartScanningForDevicesAsync();

            if (deviceList.Count == 0)
                messageLabel.Text = "No devices discovered";
            else
                messageLabel.Text = $"Device [{BLE_DEVICE_NAME}] discovered"; 
            #endregion

        }
    }
}
