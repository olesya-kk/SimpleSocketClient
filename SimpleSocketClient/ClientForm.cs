using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleSocketClient
{
    public partial class ClientForm : Form
    {
        private TcpClient client;
        private NetworkStream clientStream;
        private Thread clientThread;
        private bool isConnected = false;

        public ClientForm()
        {
            InitializeComponent();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            //127.0.0.1
        }


        // Метод для подключения к серверу
        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                client.Connect(textBoxServerIP.Text, 8888);
                clientStream = client.GetStream();
                clientThread = new Thread(new ThreadStart(ReceiveMessages));
                clientThread.Start();
                isConnected = true;

                // Отправка запроса на получение истории сообщений
                SendMessageToServer("GetHistory \n");
            }
            catch (Exception ex)
            {
                Log("Error connecting to server: " + ex.Message);
            }
        }

        // Метод для отправки сообщения серверу
        private void SendMessageToServer(string message)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
            catch (Exception ex)
            {
                Log("Error sending message to server: " + ex.Message);
            }
        }



        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                ConnectToServer();
            }
        }


        private void ReceiveMessages()
        {
            byte[] message = new byte[4096];
            int bytesRead;
            try
            {
                while (isConnected && (bytesRead = clientStream.Read(message, 0, 4096)) > 0)
                {
                    string receivedMessage = Encoding.ASCII.GetString(message, 0, bytesRead);
                    if (receivedMessage.StartsWith("History:"))
                    {
                        // Обновление richTextBoxLog с полученной историей сообщений
                        string history = receivedMessage.Substring(8); // Удаление "History:" из строки
                        UpdateRichTextBox(history);
                    }
                    else
                    {
                        Log(receivedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Error receiving message from server: " + ex.Message);
                Disconnect();
            }
        }

        // Метод для обновления richTextBoxLog с полученной историей сообщений
        private void UpdateRichTextBox(string history)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateRichTextBox), history);
                return;
            }
            richTextBoxLog.AppendText(history + Environment.NewLine);
        }


        private void buttonSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (isConnected)
                {
                    string message = $"{textBoxName.Text}: {textBoxMessage.Text}";
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    Log(message);
                }
                else
                {
                    Log("Not connected to server.");
                }
            }
            catch (Exception ex)
            {
                Log("Error sending message: " + ex.Message);
                Disconnect();
            }
        }

        private void Disconnect()
        {
            if (isConnected)
            {
                isConnected = false;
                clientStream.Close();
                client.Close();
                Log("Disconnected from server.");
            }
        }


        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), message);
                return;
            }
            richTextBoxLog.AppendText(message + Environment.NewLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Disconnect();
        }
    }
}
