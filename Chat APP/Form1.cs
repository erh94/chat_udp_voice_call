using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Voice;
using System.Threading;


namespace Chat_APP
{
    public partial class Form1 : Form
    {
        private Socket r;
        private Thread t;
        private bool connected = false;

        Socket mySocket;
        EndPoint epLocal, epRemote;
        byte[] buffer;
              
        
        public Form1()
        {
            InitializeComponent();
            

        }

        private void Form1_Load(object sender, EventArgs e)
        {
           // Process.Start(myStart);
           

            //get user IP
            txtLocalIP.Text = GetLocalIP();
            //txtRemoteIP.Text = GetLocalIP();
            txtRemoteIP.Text = "192.168.137.57";
            txtLocalPort.Text = "70";
            txtRemotePort.Text = "70";

        }
        private string GetLocalIP()
        {
            IPHostEntry myHost;
            myHost = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in myHost.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return "127.0.0.1";
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {

            //set up socket
            mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);


            //binding sockets
            epLocal = new IPEndPoint(IPAddress.Parse(txtLocalIP.Text), Convert.ToInt32(txtLocalPort.Text));
            mySocket.Bind(epLocal);

            //Connecting To Remote IP
            epRemote = new IPEndPoint(IPAddress.Parse(txtRemoteIP.Text), Convert.ToInt32(txtRemotePort.Text));
            mySocket.Connect(epRemote);
             
            //Listening TO specific Port
            buffer = new byte[1500];
            if (mySocket.Connected)
            {

                mySocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
            }

            //Changing btnCOnnect text
            BtnConnect.Text = "Connected";
            BtnConnect.Enabled = false;
            BtnDiscnt.Enabled = true;

        }
        private void MessageCallBack(IAsyncResult aResult)
        {
            try
            {
                byte[] RecivedData = new byte[1500];
                RecivedData = (byte[])aResult.AsyncState;

                //converting byte[] into string
                ASCIIEncoding aEncoding = new ASCIIEncoding();
                string RecivedMessage = aEncoding.GetString(RecivedData);

                //Adding this message to listbox
                ListMessages.Items.Add("Friend : " + RecivedMessage);

                buffer = new byte[1500];
                if (mySocket.Connected)
                {

                    mySocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref epRemote, new AsyncCallback(MessageCallBack), buffer);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (mySocket.Connected)
            {



                //converting string messages to byte
                ASCIIEncoding aEncoding = new ASCIIEncoding();
                byte[] SendingMessage = new byte[1500];
                SendingMessage = aEncoding.GetBytes(txtMessage.Text);

                //sending Encoding message
                mySocket.Send(SendingMessage);

                //adding to listbox
                ListMessages.Items.Add("ME : " + txtMessage.Text);
                txtMessage.Text = null;

            }


        }

        

        private void BtnDiscnt_Click(object sender, EventArgs e)
        {

           // mySocket.Disconnect(true);
            mySocket.Close();
            BtnDiscnt.Enabled = false;
            BtnConnect.Enabled = true;
            BtnConnect.Text = "Connect";


        }
          
               
        
        private WaveOutPlayer m_Player;
        private WaveInRecorder m_Recorder;
        private FifoStream m_Fifo = new FifoStream();

        private byte[] m_PlayBuffer;
        private byte[] m_RecBuffer;

        private void Voice_In()
        {
            byte[] br;
            r.Bind(new IPEndPoint(IPAddress.Parse(txtLocalIP.Text), 6000)); // textbox2 receiving port

            br = new byte[16384];

            while (btnEndCall.Enabled==true)
            {
                
                r.Receive(br);
                m_Fifo.Write(br, 0, br.Length);
            }
        }

        private void Voice_Out(IntPtr data, int size)
        {
            try
            {
                //for Recorder
                if (m_RecBuffer == null || m_RecBuffer.Length < size)
                    m_RecBuffer = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(data, m_RecBuffer, 0, size);
                //Microphone ==> data ==> m_RecBuffer ==> m_Fifo
                r.SendTo(m_RecBuffer, new IPEndPoint(IPAddress.Parse(txtRemoteIP.Text), 5000)); // textbox1 receiver ip, textbox3 sending port

            }
            catch (Exception e)
            {
                Stop();
                MessageBox.Show(e.ToString());
                throw;
            }
        }




        private void Start()
        {
            Stop();
            try
            {
                WaveFormat fmt = new WaveFormat(44100, 16, 1);
                m_Player = new WaveOutPlayer(-1, fmt, 16384, 6, new BufferFillEventHandler(Filler));
                m_Recorder = new WaveInRecorder(-1, fmt, 16384, 6, new BufferDoneEventHandler(Voice_Out));
            }
            catch
            {
                Stop();
                throw;
            }
        }

        private void Stop()
        {
            if (m_Player != null)
                try
                {
                    m_Player.Dispose();
                }
                finally
                {
                    m_Player = null;
                }
            if (m_Recorder != null)
                try
                {
                    m_Recorder.Dispose();
                }
                finally
                {
                    m_Recorder = null;
                }
            m_Fifo.Flush(); // clear all pending data
        }

        

        private void btnCall_Click(object sender, EventArgs e)
        {
            t = new Thread(new ThreadStart(Voice_In));

            r = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            

            if (connected == false)
            {
                               
                    t.Start();
                               
                connected = true;
            }

          //  Start();
            btnEndCall.Enabled = true;
            btnCall.Enabled = false;

        }

        private void btnEndCall_Click(object sender, EventArgs e)
        {
            btnEndCall.Enabled = false;
            btnCall.Enabled = true;
            Stop();
            r.Shutdown(SocketShutdown.Both);
            r.Close();
            t.Abort();
            t.Join(5);
           
            //t.Suspend();
            connected = false;
            

            

        }

        private void Filler(IntPtr data, int size)
        {
            if (m_PlayBuffer == null || m_PlayBuffer.Length < size)
                m_PlayBuffer = new byte[size];
            if (m_Fifo.Length >= size)
                m_Fifo.Read(m_PlayBuffer, 0, size);
            else
                for (int i = 0; i < m_PlayBuffer.Length; i++)
                    m_PlayBuffer[i] = 0;
            System.Runtime.InteropServices.Marshal.Copy(m_PlayBuffer, 0, data, size);
            // m_Fifo ==> m_PlayBuffer==> data ==> Speakers
        }

    }
}
