﻿using AdasVetelServer.messages;
using SimpleTcp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
namespace AdasVetelClient.tcp
{
    public class TcpClientWrapper
    {
        public static TcpClientWrapper Instance {get;} = new TcpClientWrapper();
        public List<ClientView> views { get; } = new List<ClientView>();
        private SimpleTcpClient client;
        private JsonSerializerOptions sOption = new JsonSerializerOptions { WriteIndented = false };
        private int currentMessageSize = 0;
        private MemoryStream currentMessageStream = new MemoryStream();
        private BinaryReader br;
        long readPos = 0;      
        private TcpClientWrapper() {
            string ipPort = File.ReadAllText(Application.StartupPath + "\\serverConfig\\serverIp.conf");
            client = new SimpleTcpClient(ipPort);

            br = new BinaryReader(currentMessageStream);
          
            client.Events.Connected += EventServerConnected;
            client.Events.Disconnected += EventServerDisconnected;
            client.Events.DataReceived += EventDataReceived;
        }
        public void send<T>(MessageBase message) where T : MessageBase
        {
            using (var outputStream = new MemoryStream())
            {
                using (var bw = new BinaryWriter(outputStream))
                {
                    byte[] data = JsonSerializer.SerializeToUtf8Bytes((T)message, sOption);
                    outputStream.Capacity = data.Length + 4;                
                    bw.Write(data.Length);                   
                    bw.Write(data);                   
                    outputStream.Position = 0;
                    client.Send(outputStream.Length, outputStream);     
                    Console.WriteLine($"{outputStream.Length} bytes sent to server.");
                }
            }
        }
        public void connectToServer() {
            if (!client.IsConnected)
            {
                try
                {
                    client.Connect();

                }
                catch (Exception ex)
                {
                    client.Dispose();                   
                }
            } 
        }   
        private void EventDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"{e.Data.Length} bytes received from server");
            receiveData(e.Data);
        }
        private void receiveData(byte[] data)
        {
            currentMessageStream.Capacity = currentMessageStream.Capacity + data.Length;
            currentMessageStream.Write(data, 0, data.Length);
            currentMessageStream.Position = readPos;
            if (currentMessageSize == 0)
            {  
                currentMessageSize = br.ReadInt32();   
                if (currentMessageStream.Length -4< currentMessageSize)
                {
                    readPos = currentMessageStream.Position;
                    currentMessageStream.Seek(0, SeekOrigin.End);
                    return;
                }
            }
            byte[] dataToHandle = br.ReadBytes(currentMessageSize);
            foreach (ClientView view in views)
                handleData(dataToHandle, view);            
            if (currentMessageStream.Position < currentMessageStream.Length)
            {
                byte[] remaining = br.ReadBytes((int)(currentMessageStream.Length - currentMessageStream.Position));
                resetStream();
                receiveData(remaining);
            }
            else if (currentMessageStream.Position == currentMessageStream.Length)
            {
                resetStream();
            }
        }
        
        private void resetStream() {
            currentMessageStream.Close();
            br.Close();
            currentMessageStream = new MemoryStream();
            br = new BinaryReader(currentMessageStream);
            readPos = 0;         
            currentMessageSize = 0;
        }
        private void handleData(byte[] data, ClientView view) {
            try
            {
                MessageBase msg = JsonSerializer.Deserialize<MessageBase>(new ReadOnlySpan<byte>(data));

                if (msg.Type == "ErrorMessage")
                {
                    ErrorMessage ermsg = JsonSerializer.Deserialize<ErrorMessage>(new ReadOnlySpan<byte>(data));
                    view.handleMessage(ermsg, msg.Type);
                }
                if (msg.Type == "LoadContract")
                {
                    LoadMessage lmsg = JsonSerializer.Deserialize<LoadMessage>(new ReadOnlySpan<byte>(data));
                    view.handleMessage(lmsg, msg.Type);
                }
                if (msg.Type == "ProgressInfo")
                {
                    ProgressInfoMessage pimsg = JsonSerializer.Deserialize<ProgressInfoMessage>(new ReadOnlySpan<byte>(data));
                    view.handleMessage(pimsg, msg.Type);
                }
                if (msg.Type == "RecordRequest")
                {
                    GetRecordMessage grmsg = JsonSerializer.Deserialize<GetRecordMessage>(new ReadOnlySpan<byte>(data));
                    view.handleMessage(grmsg, msg.Type);
                }
                if (msg.Type == "DataChanged")
                {
                    DatabaseChangedMessage dbcmsg = JsonSerializer.Deserialize<DatabaseChangedMessage>(new ReadOnlySpan<byte>(data));
                    view.handleMessage(dbcmsg, msg.Type);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);                
                ErrorMessage ermsg = new ErrorMessage(ex.StackTrace);
                view.handleMessage(ermsg, "ErrorMessage");
            }
        }

        private void EventServerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {           
            foreach(ClientView view in views)
                 view.disconnected();         
        }

        private void EventServerConnected(object sender, ClientConnectedEventArgs e)
        {
            foreach (ClientView view in views)
                view.connected(); 
            
        }
        public bool isConnected() {
            return client.IsConnected;
        }

    }
}
