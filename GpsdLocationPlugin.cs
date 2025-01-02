using NINA.Astrometry;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BillNash.NINA.GpsdLocationPlugin {
    [Export(typeof(IPluginManifest))]
    public class GpsdLocationPlugin : PluginBase, INotifyPropertyChanged {
        private string gpsdHostname;
        private int gpsdPort = 2947; // Default gpsd port
        private string statusMessage;
        private string locationData;
        private string timeData;
        private string altitudeData;

        private IProfileService profileService; // Declare the profileService field

        [ImportingConstructor]
        public GpsdLocationPlugin(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string GpsdHostname {
            get => gpsdHostname;
            set {
                gpsdHostname = value;
                RaisePropertyChanged();
                SaveSettings();
            }
        }

        public int GpsdPort {
            get => gpsdPort;
            set {
                gpsdPort = value;
                RaisePropertyChanged();
                SaveSettings();
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

        private void SaveSettings() {
            try {
                // Assuming profileService.ActiveProfile.PluginSettings is the correct place to save these settings
                var pluginSettings = profileService.ActiveProfile.PluginSettings as PluginSettings;
                if (pluginSettings != null) {
                    pluginSettings.GpsdHostname = gpsdHostname;
                    pluginSettings.GpsdPort = gpsdPort;
                    profileService.ActiveProfile.Save();
                    StatusMessage = "Settings saved successfully!";
                } else {
                    StatusMessage = "Failed to save settings.";
                }
            } catch (Exception ex) {
                StatusMessage = $"Failed to save settings: {ex.Message}";
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
           
            // Ensure LocationData is available
            if (string.IsNullOrEmpty(LocationData)) {
                StatusMessage = "Location data is not available.";
                return;
             }

            // Parse the location data
            var locationParts = LocationData.Split(',');
            if (locationParts.Length < 2) {
                    StatusMessage = "Location data format is incorrect.";
                    return;
             }


            // Split up location data into two pieces: latitude and longitude
            var latitude = double.Parse(locationParts[0].Split(':')[1].Trim());
            var longitude = double.Parse(locationParts[1].Split(':')[1].Trim());

            // Parse altitude if available
            var altitude = 0.0;
            if (!string.IsNullOrEmpty(AltitudeData)) {
                altitude = double.Parse(AltitudeData.Split(':')[1].Trim());
            }
         
            try {
                // Update the active profile's astrometry settings
                var astroSettings = profileService.ActiveProfile.AstrometrySettings as AstrometrySettings;
                if (astroSettings != null) {
                    profileService.ChangeLatitude(latitude);
                    profileService.ChangeLongitude(longitude);
                    profileService.ChangeElevation(altitude);

                    // Save the profile to ensure changes are persisted
                    profileService.ActiveProfile.Save();


                    StatusMessage = "Location data applied to astrometry settings successfully!";
                } else {
                    StatusMessage = "Failed to apply location data to astrometry settings.";
                }
            } catch (Exception ex) {
                StatusMessage = $"Failed to apply location data: {ex.Message}";
            }
        }
    }
}
