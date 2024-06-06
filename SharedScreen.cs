using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

namespace Client
{
    public partial class SharedScreen : Form
    {
        private readonly int port;
        private TcpClient client;
        private TcpListener server;
        private NetworkStream mainStream;
        private Thread Listening;
        private Thread GetImage;
        private volatile bool listeningThreadRunning;
        private volatile bool getImageThreadRunning;

        public SharedScreen(string ip, int Port)
        {
            this.port = Port;
            this.client = new TcpClient();
            this.Listening = new Thread(StartListening);
            this.GetImage = new Thread(ReceiveImage);

            InitializeComponent();
        }

        private void ReceiveImage()
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            while (client.Connected && getImageThreadRunning)
            {
                try
                {
                    mainStream = client.GetStream();
                    if (mainStream != null && mainStream.DataAvailable)
                    {
                        pictureBox1.Invoke((MethodInvoker)delegate
                        {
                            pictureBox1.Image = (Image)binaryFormatter.Deserialize(mainStream);
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error receiving image: " + ex.Message);
                    getImageThreadRunning = false;
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            server = new TcpListener(IPAddress.Any, port);
            listeningThreadRunning = true;
            Listening.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            StopListening();
        }

        private void StartListening()
        {
            try
            {
                server.Start();
                while (listeningThreadRunning)
                {
                    if (server.Pending())
                    {
                        client = server.AcceptTcpClient();
                        if (client.Connected)
                        {
                            getImageThreadRunning = true;
                            GetImage.Start();
                            break;
                        }
                    }
                    Thread.Sleep(100); // Sleep briefly to avoid busy-waiting
                }
            }
            catch (Exception ex)
            {
                if (listeningThreadRunning) // Only show error if we weren't intentionally stopping
                {
                    MessageBox.Show("Error starting listener: " + ex.Message);
                }
                listeningThreadRunning = false;
            }
        }

        private void StopListening()
        {
            listeningThreadRunning = false;
            getImageThreadRunning = false;

            try
            {
                server?.Stop();
                client?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error stopping listener: " + ex.Message);
            }

            if (Listening != null && Listening.IsAlive)
            {
                Listening.Join();
            }

            if (GetImage != null && GetImage.IsAlive)
            {
                GetImage.Join();
            }
        }

        private void SharedScreen_Load(object sender, EventArgs e)
        {
            // Initialize if needed
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Handle picture box click if needed
        }
    }
}
