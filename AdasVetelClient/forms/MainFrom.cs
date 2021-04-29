

using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using SimpleTcp;
using AdasVetelServer.messages;
using AdasVetelClient.tcp;
using System.IO;

namespace AdasVetelClient
{
    public partial class AdasVetel : MaterialSkin.Controls.MaterialForm , ClientView
    {   
        private LoadingForm loadingForm;         

        public AdasVetel()
        {
            InitializeComponent();       
            
      
            docLoadButton.BackgroundImage = new Bitmap(docLoadButton.BackgroundImage, new Size(docLoadButton.Width, docLoadButton.Height));
            txtLoadButton.BackgroundImage = new Bitmap(txtLoadButton.BackgroundImage, new Size(txtLoadButton.Width, txtLoadButton.Height));
            pdfLoadButton.BackgroundImage = new Bitmap(pdfLoadButton.BackgroundImage, new Size(pdfLoadButton.Width, pdfLoadButton.Height));
            imageLoadButton.BackgroundImage = new Bitmap(imageLoadButton.BackgroundImage, new Size(imageLoadButton.Width, imageLoadButton.Height));
            connectButton.BackgroundImage = new Bitmap(connectButton.BackgroundImage, new Size(connectButton.Width, connectButton.Height));
        }
       
        private void readContract_Click(object sender, EventArgs e)
        {
            if (TcpClientWrapper.Instance.isConnected())
            {
                if (fileFlowLayout.Controls.Count == 0)
                {
                    MessageBox.Show("Nem történt beolvasás", " Eredmény", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    int count = 10;
                    Cursor.Current = Cursors.WaitCursor;
                    loadingForm = new LoadingForm(fileFlowLayout.Controls.Count>count ? count : fileFlowLayout.Controls.Count);
                    loadingForm.Show();

                    foreach (FileIconControl item in fileFlowLayout.Controls)
                    {
                        if (count == 0) {
                            timer1.Enabled = true;
                            limitLabel.Visible = true;
                            break;
                        }
                        item.loadingForm = loadingForm;
                        LoadMessage msg = new LoadMessage(item.getText(), item.FileName);
                        if (msg.Data != "") {                           
                            TcpClientWrapper.Instance.send<LoadMessage>(msg);                          
                        }
                        else
                        {
                            loadingForm.setProgress(item.SafeFileName + " kihagyva.");
                        }
                        count--;
                    }
                   
                }
            }
        }

        private void docLoadButton_Click(object sender, EventArgs e)
        {
            loadFileNames("Word Document|*.docx|" +
                "Word 97-2003|*.doc");
        }

        private void txtLoadButton_Click(object sender, EventArgs e)
        {
            loadFileNames("Text File|*.txt");
        }

        private void pdfLoadButton_Click(object sender, EventArgs e)
        {
            loadFileNames("PDF|*.pdf");
        }

        private void imageLoadButton_Click(object sender, EventArgs e)
        {
            loadFileNames("JPG|*.jpg|" +
                 "JPEG|*.jpeg|" +
                 "PNG|*.png|" +
                 "PDF|*.pdf");
        }
        private void loadFileNames(string filter)
        {
            string pathFile = Application.StartupPath + "\\prevPath\\docPrevPath.loc";           
            string dir = File.ReadAllText(pathFile);       
            if (!Directory.Exists(dir))
                dir = "c:\\";
            using (OpenFileDialog ofd = new OpenFileDialog()
            {
                InitialDirectory = dir,
                ValidateNames = true,
                Multiselect = true,
                Filter = filter
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var filename in ofd.FileNames)
                    {
                        fileFlowLayout.Controls.Add(new FileIconControl(filename,
                            Path.GetFileName(filename), fileFlowLayout.Controls));
                    }
                    File.WriteAllText(pathFile, Path.GetDirectoryName(ofd.FileName));
                }
            }
        }
        private void viewContracts_Click(object sender, EventArgs e)
        {
            DisplayForm displayForm = new DisplayForm(null);
            displayForm.Show();
        }     


        private void AdasVetel_Load(object sender, EventArgs e)
        {
            TcpClientWrapper.Instance.views.Add(this);
            new ConnectForm().ShowDialog();
        }
    

        private void clearListBtn_Click(object sender, EventArgs e)
        {
            fileFlowLayout.Controls.Clear();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {            
             new ConnectForm().ShowDialog();            
        }

        public void connected()
        {
            
            Console.WriteLine("Server connected.");
            connectionSignal.BackColor = Color.Green;
          
        }     

        public void disconnected()
        {
            Invoke((MethodInvoker)delegate
            {
                connectionSignal.BackColor = Color.Red;
                Console.WriteLine("Server disconnected.");
                new ConnectForm().ShowDialog();
            });

        }

        public void handleMessage(MessageBase message, string type)
        {            
            Invoke((MethodInvoker)delegate
            {
                if (type == "LoadContract")
                {
                    LoadMessage lmsg = (LoadMessage)message;
                    for (int i = 0; i < fileFlowLayout.Controls.Count; i++)
                    {
                        FileIconControl fic = (FileIconControl)fileFlowLayout.Controls[i];
                        if (fic.FileName == lmsg.FileName)
                        {
                            fileFlowLayout.Controls.RemoveAt(i);
                            i--;
                        }
                    }
                    loadingForm.setProgress(Path.GetFileName(lmsg.FileName) + " beolvasva.");
                }
                else if (type == "ProgressInfo")
                {
                    ProgressInfoMessage pimsg = (ProgressInfoMessage)message;
                    loadingForm.setLabelText(Path.GetFileName(pimsg.Info));

                }
                else if (type == "ErrorMessage") {
                    ErrorMessage ermsg = (ErrorMessage)message;
                    MessageBox.Show(ermsg.Message, "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            limitLabel.Visible = false;
        }
    }
}
