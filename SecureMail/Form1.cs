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

namespace SecureMail
{
    public partial class Form1 : Form
    {
        const string фильтрОткрытогоКлюча = "Open key (*.okey)|*.okey";
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
                RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
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
            button3.Enabled = true;
            button1.Enabled = true;
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
                    //byte[] закодированноеСообщение = rsa.Encrypt(Encoding.ASCII.GetBytes(richTextBox1.Text), true);
                    string msg = "<Сообщение закодировано>\n<MSG>"+Encoding.ASCII.GetString(rsa.Encrypt(Encoding.ASCII.GetBytes(richTextBox1.Text), true)) + "</MSG>";
                    отправитьСообщение(msg);
                    sr.Close();
                }
                catch (Exception exp)
                {
                    показатьСообщениеОбОшибке(exp);
                }
            }
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
    }
}
