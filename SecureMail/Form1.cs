using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Xml;
using System.IO;
using OpenPop.Pop3;

namespace SecureMail
{
    public partial class Form1 : Form
    {
        const int blokSize = 80;
        const string фильтрОткрытогоКлюча = "Open key (*.okey)|*.okey";
        const string фильтрЗакрытогоКлюча = "Private key (*.pkey)|*.pkey";
        struct ДанныеПользователя
        {
            private MailAddress адрес;
            private string пароль;
            public string Пароль
            {
                get
                {
                    return пароль;
                }

                set
                {
                    пароль = value;
                }
            }

            public MailAddress Адрес
            {
                get
                {
                    return адрес;
                }

                set
                {
                    адрес = value;
                }
            }
        }
        MailMessage полученноеСообщение;
        ДанныеПользователя пользователь;
        public Form1()
        {
            InitializeComponent();
        }
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.MyDocuments;
            fbd.Description = "Выберите место для сохранения ключей";
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider(1024);
                StreamWriter sw = new StreamWriter(fbd.SelectedPath+"\\OpenKey.okey");
                sw.WriteLine(csp.ToXmlString(false));
                sw.Close();
                sw = new StreamWriter(fbd.SelectedPath + "\\PrivateKey.pkey");
                sw.WriteLine(csp.ToXmlString(true));
                sw.Close();
                MessageBox.Show(this, "Ключи сохранены по адресу:\n" + fbd.SelectedPath.ToString(), "Еведомление.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            Form2 form = new Form2(this);
            form.ShowDialog(this);
            form.Focus();
        }
        public void УстановитьДанныеПользователя(MailAddress адрес, string пароль)
        {
            пользователь.Адрес = адрес;
            пользователь.Пароль = пароль;
            toolStripStatusLabel1.Text = "Данные пользователя введены.";
            if (groupBox1.Enabled == true)
            {
                button3.Enabled = true;
                button1.Enabled = true;
            }
            if (groupBox5.Enabled==true)
            {
                button2.Enabled = true;
            }
        }
        private void отправитьСообщениеКакЕсть(object sender, EventArgs e)
        {
            отправитьСообщение(richTextBox1.Text);
        }
        void СообщениеДоставлено(object sender, AsyncCompletedEventArgs e)
        {
            MailMessage mail = (MailMessage)e.UserState;
            string subject = mail.Subject;

            if (e.Cancelled)
            {
                string cancelled = string.Format("[{0}] Отправка отменена.", subject);
                MessageBox.Show(cancelled);
            }
            if (e.Error != null)
            {
                string error = String.Format("[{0}] {1}", subject, e.Error.ToString());
                MessageBox.Show(error,"Ошибка!",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("Сообщение успешно отправлено!","Уведомление.",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
        }
        private void отправитьСообщениеЗашифрованным(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = фильтрОткрытогоКлюча;
            ofd.FilterIndex = 1;
            ofd.Title = "Выберите файл открытого ключа.";
            ofd.Multiselect = false;
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog()==DialogResult.OK)
            {
                try
                {
                    StreamReader sr = new StreamReader(ofd.FileName);
                    string ключ = sr.ReadToEnd();
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(ключ);
                    byte[] buf = Encoding.UTF32.GetBytes(richTextBox1.Text);
                    string tmpSTR = string.Empty;
                    //byte[] buff;
                    if (buf.Length > blokSize)
                    {
                        int count = 0;
                        int countBloks = 0;
                        bool flag = false;
                        byte[] tmp = new byte[blokSize];
                        for (int i = 0; i < buf.Length; i++)
                        {
                            byte item = buf[i];
                            if (count < blokSize && i != buf.Length)
                            {
                                tmp[count] = item;
                                count++;
                            }
                            else
                            {
                                byte[] tt = rsa.Encrypt(tmp, true);
                                //buff = tt;
                                tmpSTR += ByteToHEXString(tt);//Encoding.UTF32.GetString(tt);
                                int ll = tmpSTR.Length;
                                count = 0;
                                countBloks++;
                                i--;
                                if (buf.Length > blokSize * countBloks + blokSize)
                                    tmp = new byte[blokSize];
                                else
                                {
                                    tmp = new byte[buf.Length - blokSize * countBloks];
                                    flag = true;
                                }
                            }
                        }
                        if (flag == true)
                        {
                            byte[] tt = rsa.Encrypt(tmp, true);
                            tmpSTR += ByteToHEXString(rsa.Encrypt(tmp, true));
                        }
                    }
                    else
                    {
                        byte[] tt = rsa.Encrypt(buf, true);
                        tmpSTR += ByteToHEXString(tt);
                    }
                    int len = tmpSTR.Length;
                    string msg = "<!MSG_ENCRYPT!>\n<MSG>" + tmpSTR +"</MSG>";
                    отправитьСообщение(msg);
                    sr.Close();
                }
                catch (Exception exp)
                {
                    показатьСообщениеОбОшибке(exp);
                }
            }
        }
        private string ByteToHEXString(byte[] b)
        {
            string s = string.Empty;
            foreach (var item in b)
            {
                s += item.ToString("X2");
            }
            return s;
        }
        private static byte[] HEXStringToByte(string s)
        {
            byte[] b = new byte[s.Length / 2];
            int iter = 0;
            for (int i = 0; i < s.Length; i += 2)
            {
                b[iter] = Convert.ToByte(s[i].ToString() + s[i + 1].ToString(), 16);
                iter++;
            }
            return b;
        }
        private void отправитьСообщение(string строка)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                try
                {
                    MailAddress получатель = new MailAddress(textBox1.Text);

                    MailMessage сообщение = new MailMessage(пользователь.Адрес, получатель);
                    сообщение.Subject = textBox2.Text;
                    сообщение.Body = строка;

                    SmtpClient client = new SmtpClient("smtp." + пользователь.Адрес.Host, 25);
                    client.Credentials = new NetworkCredential(пользователь.Адрес.Address, пользователь.Пароль);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.EnableSsl = true;
                    object obj = сообщение;
                    client.SendCompleted += СообщениеДоставлено;
                    client.SendAsync(сообщение, obj);
                }
                catch (Exception exp)
                {
                    показатьСообщениеОбОшибке(exp);
                }
            }
        }
        private void показатьСообщениеОбОшибке(Exception exp)
        {
            MessageBox.Show(this, exp.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void показатьЭкранПолученияСообщений(object sender, EventArgs e)
        {
            groupBox1.Enabled = false;
            groupBox4.Enabled = false;
            groupBox1.Visible = false;
            groupBox4.Visible = false;
            groupBox5.Enabled = true;
            groupBox5.Visible = true;
            button2.Enabled = false;
            button4.Enabled = false;
            пользователь.Адрес = null;
            пользователь.Пароль = string.Empty;
            statusStrip1.Text = "Данные пользователя не введены.";
        }
        private void ПоказатьЭкранОтправкиПисьма(object sender, EventArgs e)
        {
            groupBox1.Enabled = true;
            groupBox4.Enabled = true;
            groupBox1.Visible = true;
            groupBox4.Visible = true;
            groupBox5.Enabled = false;
            groupBox5.Visible = false;
            пользователь.Адрес = null;
            пользователь.Пароль = string.Empty;
            statusStrip1.Text = "Данные пользователя не введены.";
        }
        private void получитьСообщение(object sender, EventArgs e)
        {
            Pop3Client client = new Pop3Client();
            client.Connect("pop." + пользователь.Адрес.Host, 995, true);
            client.Authenticate(пользователь.Адрес.Address, пользователь.Пароль);
            if (client.Connected)
            {
                полученноеСообщение = client.GetMessage(client.GetMessageCount()).ToMailMessage();
                richTextBox2.AppendText("От: " + полученноеСообщение.From.Address);
                richTextBox2.AppendText("\nТема: " + полученноеСообщение.Subject);
                richTextBox2.AppendText("\nСообщение: " + полученноеСообщение.Body);
                client.Disconnect();
                if (полученноеСообщение.Body.IndexOf("<!MSG_ENCRYPT!>") != -1)
                    button4.Enabled = true;
            }
        }
        private void расшифроватьСообщение(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = фильтрЗакрытогоКлюча;
            ofd.FilterIndex = 1;
            ofd.Title = "Выберите файл закрытого ключа.";
            ofd.Multiselect = false;
            ofd.RestoreDirectory = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StreamReader sr = new StreamReader(ofd.FileName);
                    string ключ = sr.ReadToEnd();
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(ключ);
                    int startMsg = полученноеСообщение.Body.IndexOf("<MSG>");
                    int endMsg = полученноеСообщение.Body.IndexOf("</MSG>");
                    string msg = полученноеСообщение.Body.Substring(startMsg + 5, endMsg - startMsg - 5);
                    byte[] buf =  HEXStringToByte(msg);
                    string tmpSTR = string.Empty;
                    int countBlok = buf.Length / 128;
                    for (int i = 0; i < countBlok; i++)
                    {
                        byte[] b = buf.Skip(128 * i).ToArray();
                        byte[] tmp = b.Take(128).ToArray();
                        tmpSTR += ByteToHEXString(rsa.Decrypt(tmp, true));
                    }
                    msg = Encoding.UTF32.GetString(HEXStringToByte(tmpSTR));
                    richTextBox2.Clear();
                    richTextBox2.AppendText("От: " + полученноеСообщение.From.Address);
                    richTextBox2.AppendText("\nТема: " + полученноеСообщение.Subject);
                    richTextBox2.AppendText("\nСообщение: " +msg+полученноеСообщение.Body.Substring(endMsg+6));
                    sr.Close();
                    rsa.Dispose();
                }
                catch (Exception exp)
                {
                    показатьСообщениеОбОшибке(exp);
                }
            }
        }
    }
}
