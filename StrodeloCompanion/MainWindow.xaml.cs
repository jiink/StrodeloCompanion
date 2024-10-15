using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Windows;
using System.IO;

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
                await Task.Delay(5000); //5s

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
        public void SendFile(string filePath, string host, int port)
        {
            var client = new TcpClient(host, port);
            var stream = client.GetStream();

            using (var fileStream = File.OpenRead(filePath))
            {
                fileStream.CopyTo(stream);
            }

            stream.Close();
            client.Close();
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "Open File...";
            openFileDialog.Filter = "3D Files (*.obj;*.glb;*.gltf;*.fbx)|*.obj;*.glb;*.gltf;*.fbx";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                Debug.WriteLine("Selected file: " + filePath);

                // Send the file to pairedDeviceAddress
                SendFile(filePath, pairedDeviceAddress.ToString(), 8111); // Put this port number (8111) in a const variable somewhere

            }
        }
    }
}