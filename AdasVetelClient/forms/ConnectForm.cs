using AdasVetelServer.messages;
using AdasVetelClient.tcp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdasVetelClient
{
    public partial class ConnectForm : MaterialSkin.Controls.MaterialForm
    {

        bool stopped = false;
        public ConnectForm()
        {
            InitializeComponent();
            CenterToScreen();
            new Thread(doConnect).Start();
        
           
        }
        public void doConnect() {
            while (!TcpClientWrapper.Instance.isConnected() &&! stopped)
            TcpClientWrapper.Instance.connectToServer();
            if (stopped)
                return;
            Invoke((MethodInvoker)delegate
            {
                stopped = true;
                Close();
            });
        }
       
        private void closeBtn_Click(object sender, EventArgs e)
        {
            stopped = true;
            Close();
        }       
        private void ConnectForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopped = true;
            DialogResult = DialogResult.OK;
        }
    }
}
