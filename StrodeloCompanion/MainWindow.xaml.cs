using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Media;

namespace StrodeloCompanion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IPAddress pairedDeviceAddress;

        public MainWindow()
        {
            InitializeComponent();
        }



        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pairing button clicked");

            string ipAddressString = IpAddressBox.Text;

            // Hide previous messages initially
            DeviceStatusTextBlock.Visibility = Visibility.Collapsed;
            DeviceStatusTextBlock1.Visibility = Visibility.Collapsed;

            StatusTextBlock.Visibility = Visibility.Collapsed; // Hide status text initially
            CheckProgressBar.Visibility = Visibility.Collapsed; // Hide loading indicator
            PairButton.IsEnabled = false; // Disable the pair button

            // Check for valid IP address format
            if (!IPAddress.TryParse(ipAddressString, out pairedDeviceAddress))
            {
                // Show error message for invalid IP address format
                DeviceStatusTextBlock.Visibility = Visibility.Visible; // Show the error message
                DeviceStatusTextBlock.Text = "Status: Invalid IP address!";
                PairButton.IsEnabled = true;
            }
            else
            {
                // Show status text and loading indicator
                StatusTextBlock.Visibility = Visibility.Visible; // Show status text
                StatusTextBlock.Text = "Checking for device..."; // Set initial status message
                CheckProgressBar.Visibility = Visibility.Visible; // Show loading indicator


                // Run CheckDeviceExists on a separate task to avoid blocking the UI
                bool deviceExists = await Task.Run(() => CheckDeviceExists(pairedDeviceAddress));


                // Optional delay to let the user read the message
                await Task.Delay(100); //5s

                StatusTextBlock.Visibility = Visibility.Collapsed; // Hide status message
                CheckProgressBar.Visibility = Visibility.Collapsed; // Hide loading indicator

                // Re-enable the pair button
                PairButton.IsEnabled = true;


                // Check the device existence and update the status
                if (deviceExists)
                {
                    DeviceStatusTextBlock1.Visibility = Visibility.Visible; // Show success message
                    DeviceStatusTextBlock1.Text = "Status: Device successfully paired!";
                }
                else
                {
                    DeviceStatusTextBlock.Visibility = Visibility.Visible; // Show error message for no device found
                    DeviceStatusTextBlock.Text = "Status: No device found at this IP address.";
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
                reply = ping.Send(ipAddress, 1000);
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

            ProgressPercentageTextBlock.Visibility = Visibility.Visible;

            SendFileButton.IsEnabled = false;

            try
            {
                using (var client = new TcpClient(host, port))
                using (var stream = client.GetStream())
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] buffer = new byte[81920]; // 80KB buffer size
                    int bytesRead;
                    long totalBytesSent = 0;
                    long fileLength = fileStream.Length;

                    ProgressBar.Value = 0; // Reset progress bar
                    ProgressPercentageTextBlock.Text = "0%"; // Reset percentage display

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesSent += bytesRead;

                        // Calculate the percentage and update UI
                        double progress = (double)totalBytesSent / fileLength * 100;
                        ProgressBar.Value = progress;
                        ProgressPercentageTextBlock.Text = $"{progress:F2}%";

                        Debug.WriteLine($"Bytes sent: {totalBytesSent} / {fileLength} ({progress:F2}%)");
                    }

                    Task.Run(() =>
                    {
                        MessageBox.Show("File transfer completed!");
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File transfer failed: {ex.Message}");
            }
            finally
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressPercentageTextBlock.Visibility = Visibility.Collapsed;

                SendFileButton.IsEnabled = true;
            }
        

        //stream.Close();
        //client.Close();

        }

        // Button click event for selecting and sending a file
        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Open File...";
            openFileDialog.Filter = "3D Files (*.obj;*.glb;*.gltf;*.fbx)|*.obj;*.glb;*.gltf;*.fbx";

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
                FileDropBorder.Background = new SolidColorBrush(Colors.LightGreen);  // Change background to green
                FileDropBorder.BorderBrush = new SolidColorBrush(Colors.Green);      // Change border to green
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
            FileDropBorder.Background = new SolidColorBrush(Colors.LightGray); // Reset background
            FileDropBorder.BorderBrush = new SolidColorBrush(Colors.Gray);      // Reset border
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