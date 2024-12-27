using NINA.Astrometry;
using NINA.Plugin.Interfaces;
using NINA.Plugin;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System;
using static NINA.Equipment.SDK.CameraSDKs.PlayerOneSDK.PlayerOneFilterWheelSDK;

namespace BillNash.NINA.GpsdLocationPlugin {
    /// <summary>
    /// This is a GPSD interface plugin for NINA. It gets GPS information from a GPSD server and updates NINA with the location.
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class GpsdLocationPlugin : PluginBase, INotifyPropertyChanged {
        private string gpsdHostname;
        private int gpsdPort = 2947; // Default gpsd port
        private string statusMessage;
        private string locationData;
        private string timeData;
        private string altitudeData;

        private ObserverInfo observerInfo;

        public string GpsdHostname {
            get => gpsdHostname;
            set {
                gpsdHostname = value;
                RaisePropertyChanged();
            }
        }

        public int GpsdPort {
            get => gpsdPort;
            set {
                gpsdPort = value;
                RaisePropertyChanged();
            }
        }

        public string StatusMessage {
            get => statusMessage;
            set {
                statusMessage = value;
                RaisePropertyChanged();
            }
        }

        public string LocationData {
            get => locationData;
            set {
                locationData = value;
                RaisePropertyChanged();
            }
        }

        public string TimeData {
            get => timeData;
            set {
                timeData = value;
                RaisePropertyChanged();
            }
        }

        public string AltitudeData {
            get => altitudeData;
            set {
                altitudeData = value;
                RaisePropertyChanged();
            }
        }

        public ICommand CheckGpsdCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(CheckGpsdConnection);
        public ICommand ApplyGpsdSettingsCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(ApplyGpsdSettings);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void CheckGpsdConnection() {
            try {
                using (var client = new TcpClient(GpsdHostname, GpsdPort))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true }) {
                    // Send a command to GPSD to watch for data
                    await writer.WriteLineAsync("?WATCH={\"enable\":true,\"json\":true,\"nmea\":true,\"raw\":1,\"scaled\":true}");

                    // Read multiple lines from GPSD
                    while (true) {
                        string response = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(response) && response.StartsWith("{")) {
                            // Parse the response to extract location, time, and altitude data
                            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(response);
                            if (jsonResponse.@class == "TPV") {
                                LocationData = $"Latitude: {jsonResponse.lat}, Longitude: {jsonResponse.lon}";
                                TimeData = $"Time: {jsonResponse.time}";
                                AltitudeData = $"Altitude: {jsonResponse.alt}"; // Assuming 'alt' is the key for altitude
                                StatusMessage = $"Connected successfully! {LocationData}, {TimeData}, {AltitudeData}";
                                break;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                StatusMessage = $"Connection failed: {ex.Message}";
            }
        }

        private void ApplyGpsdSettings() {
            try {
                if (observerInfo == null) {
                    StatusMessage = "Observer info is not initialized.";
                    return;
                }

                // Assume LocationData is in the format "Latitude: {lat}, Longitude: {lon}"
                if (string.IsNullOrEmpty(LocationData)) {
                    StatusMessage = "Location data is not available.";
                    return;
                }

                var locationParts = LocationData.Split(',');
                if (locationParts.Length < 2) {
                    StatusMessage = "Location data format is incorrect.";
                    return;
                }

                var latitude = double.Parse(locationParts[0].Split(':')[1].Trim());
                var longitude = double.Parse(locationParts[1].Split(':')[1].Trim());

                // Parse altitude
                var altitude = 0.0;
                if (!string.IsNullOrEmpty(AltitudeData)) {
                    altitude = double.Parse(AltitudeData.Split(':')[1].Trim());
                }

                // Update the observer info
                observerInfo.Latitude = latitude;
                observerInfo.Longitude = longitude;
                observerInfo.Elevation = altitude; // Apply altitude

                StatusMessage = "Location data applied to observer info successfully!";
            } catch (Exception ex) {
                StatusMessage = $"Failed to apply location data: {ex.Message}";
            }

            // Save settings to user settings
            // Properties.Settings.Default.GpsdHostname = GpsdHostname;
            // Properties.Settings.Default.GpsdPort = GpsdPort;
            // Properties.Settings.Default.Save();
        }
    }
}
