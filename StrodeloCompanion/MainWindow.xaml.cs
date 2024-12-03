using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using System.Configuration;


namespace StrodeloCompanion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IPAddress pairedDeviceAddress;
        Configuration config;
        KeyValueConfigurationCollection configSection;

        Brush fileSubmissionAreaOgBackground; // just used to restore normal brush when drag and drop is done
        Brush fileSubmissionAreaOgBorder;

        public MainWindow()
        {
            InitializeComponent();
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configSection = config.AppSettings.Settings;
            IpAddressBox.Text = configSection["EnteredIpAddress"].Value;
            DeviceStatusTextBlock.Text = "Pair a device to begin 🥽";
            DeviceStatusTextBlock.Foreground = Brushes.Yellow;
            fileSubmissionAreaOgBackground = FileSubmissionArea.Background;
            fileSubmissionAreaOgBorder = FileSubmissionArea.BorderBrush;
            ProgressIndicator.Visibility = Visibility.Collapsed;
            FileStatus.Visibility = Visibility.Collapsed;
        }

        private void SaveConfig()
        {
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }

        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pairing button clicked");

            string ipAddressString = IpAddressBox.Text;
            configSection["EnteredIpAddress"].Value = ipAddressString;
            SaveConfig();
            PairButton.IsEnabled = false;

            // Check for valid IP address format
            if (!IPAddress.TryParse(ipAddressString, out pairedDeviceAddress))
            {
                // Show error message for invalid IP address format
                DeviceStatusTextBlock.Visibility = Visibility.Visible;
                DeviceStatusTextBlock.Text = "Invalid IP address ❌";
                DeviceStatusTextBlock.Foreground = Brushes.Red;
                PairButton.IsEnabled = true;

                FileSubmissionArea.IsEnabled = false;
            }
            else
            {
                // Show status text and loading indicator
                DeviceStatusTextBlock.Text = "Checking for device... ℹ";
                DeviceStatusTextBlock.Foreground = Brushes.White;
                // Run CheckDeviceExists on a separate task to avoid blocking the UI
                bool deviceExists = await Task.Run(() => CheckDeviceExists(pairedDeviceAddress));
                // Optional delay to let the user read the message
                await Task.Delay(100); //5s
                // Re-enable the pair button
                PairButton.IsEnabled = true;
                // Check the device existence and update the status
                if (deviceExists)
                {
                    // Show success message
                    DeviceStatusTextBlock.Text = "Device paired ✅";
                    DeviceStatusTextBlock.Foreground = Brushes.Lime;
                    FileSubmissionArea.IsEnabled = true;
                }
                else
                {
                    DeviceStatusTextBlock.Visibility = Visibility.Visible; // Show error message for no device found
                    DeviceStatusTextBlock.Text = "Not found ❌";
                    DeviceStatusTextBlock.Foreground = Brushes.Red;
                    FileSubmissionArea.IsEnabled = false;
                }
            }
        }


        // Does an actual device exist at the given IP address? ruturn => result to the above operation
        private bool CheckDeviceExists(IPAddress ipAddress)
        {
            Ping ping = new Ping();
            PingReply reply;
            try
            {
                reply = ping.Send(ipAddress, 500);
                return reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                return false;
            }
        }

        // Given a file path, load the file and send its contents to the given host (e.g. 192.168.50.122:8111)
        public async void SendFile(string filePath, string host, int port)
        {
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;

            ProgressIndicator.Visibility = Visibility.Visible;

            FileStatus.Visibility = Visibility.Collapsed;

            FileSubmissionArea.IsEnabled = false;

            try
            {
                using (var client = new TcpClient(host, port))
                using (var stream = client.GetStream())
                using (var fileStream = File.OpenRead(filePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName);
                    byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
                    await stream.WriteAsync(fileNameLength, 0, fileNameLength.Length);
                    await stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

                    byte[] buffer = new byte[81920]; // 80KB buffer size
                    int bytesRead;
                    long totalBytesSent = 0;
                    long fileLength = fileStream.Length;

                    
                    ProgressBar.Value = 0; // Reset progress bar
                    ProgressPercentageText.Content = "0%"; // Reset percentage display

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesSent += bytesRead;

                        // Calculate the percentage and update UI
                        double progress = (double)totalBytesSent / fileLength * 100;
                        ProgressBar.Value = progress;
                        ProgressPercentageText.Content = $"{progress:F2}%";

                        Debug.WriteLine($"Bytes sent: {totalBytesSent} / {fileLength} ({progress:F2}%)");   
                    }
                }
            }
            catch (Exception ex)
            {
                FileStatus.Content = "File transfer failed";
                FileStatus.Foreground = Brushes.Red;
                MessageBox.Show($"File transfer failed: {ex.Message}");
            }
            finally
            {
                ProgressIndicator.Visibility = Visibility.Collapsed;
                FileStatus.Content = "File transfer successful";
                FileStatus.Foreground = Brushes.Lime;
                FileStatus.Visibility = Visibility.Visible;
                FileSubmissionArea.IsEnabled = true;
            }
            //stream.Close();
            //client.Close();
        }

        // Button click event for selecting and sending a file
        private void FileSubmissionArea_Click(object sender, RoutedEventArgs e)
        {
            if (pairedDeviceAddress is null || string.IsNullOrEmpty(pairedDeviceAddress.ToString()) || !CheckDeviceExists(pairedDeviceAddress))
            {
                FileStatus.Content = "Pair a device first 😡";
                return;
            }
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Open File...";
            openFileDialog.Filter = "3D Files (*.obj;*.glb;*.gltf;*.fbx;*.stl;*.ply;*.3mf;*.dae;*.png;*.jpg;*.hdr)|*.obj;*.glb;*.gltf;*.fbx;*.stl;*.ply;*.3mf;*.dae;*.png;*.jpg;*.hdr";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Debug.WriteLine("Selected file: " + filePath);

                // Verify file integrity before sending
                if (VerifyFileIntegrity(filePath))
                {
                    // Send the file to pairedDeviceAddress
                    SendFile(filePath, pairedDeviceAddress.ToString(), 8111); // Put this port number in a const variable somewhere
                }
                else
                {
                    MessageBox.Show("File integrity check failed. Please try again with a valid file.");
                }
            }
        }

        // Drag and Drop testing...
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                FileSubmissionArea.Background = new SolidColorBrush(Colors.LightGreen);  // Change background to green
                FileSubmissionArea.BorderBrush = new SolidColorBrush(Colors.Green);      // Change border to green
                DropText.Text = "Release to Drop";                                    // Change the text
                DropText.Foreground = new SolidColorBrush(Colors.White);              // Change text color for better visibility
                e.Effects = DragDropEffects.Copy;                                     // Set the effect to 'copy' (indicating a valid drop)
            }
            else
            {
                e.Effects = DragDropEffects.None; // Set to 'none' if not valid file type
            }
        }

        // Event handler for when a file leaves the drag area (without dropping)
        private void OnDragLeave(object sender, DragEventArgs e)
        {
            ResetDragDropArea();  // Reset the visual state
        }

        // Event handler for when a file is dropped in the drop zone
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    string filePath = files[0];  // Get the first file (assuming single file drag-and-drop)
                    Debug.WriteLine("Dropped file: " + filePath);

                    // Start sending the file
                    SendFile(filePath, pairedDeviceAddress.ToString(), 8111);

                    // Reset the visual indication after file is dropped
                    ResetDragDropArea();
                }
            }
        }

        // Helper method to reset the drag-and-drop area to its default state
        private void ResetDragDropArea()
        {
            FileSubmissionArea.Background = fileSubmissionAreaOgBackground; // Reset background
            FileSubmissionArea.BorderBrush = fileSubmissionAreaOgBorder;      // Reset border
            DropText.Text = "Drag and Drop File Here";                          // Reset text
            DropText.Foreground = new SolidColorBrush(Colors.Black);            // Reset text color
        }

        // Verify the integrity of the file (hash check, file size check, etc.)
        private bool VerifyFileIntegrity(string filePath)
        {
            // Simple size check (ensure file is not too small or too large)
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0 || fileInfo.Length > 1_000_000_000) // e.g., reject files larger than 1GB
            {
                return false;
            }

            // Optional: Add hash check or file extension validation here
            return true;
        }
    }
}
