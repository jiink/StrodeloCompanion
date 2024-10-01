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

        private void PairButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pairing button clicked");

            string ipAddressString = IpAddressBox.Text;

            if (IPAddress.TryParse(ipAddressString, out pairedDeviceAddress))
            {
                bool deviceExists = CheckDeviceExists(pairedDeviceAddress);

                if (deviceExists)
                {
                    Debug.WriteLine("Device found at IP address: " + ipAddressString);
                }
                else
                {
                    Debug.WriteLine("No device found at IP address: " + ipAddressString);
                }
            }
            else
            {
                Debug.WriteLine("Invalid IP address format: " + ipAddressString);
            }
        }

        // Does an actual device exist at the given IP address? Or is it just bogus?
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