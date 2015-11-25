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
using System.Net.Security;
using System.Net.Mail;
using System.Xml;
using System.IO;

namespace SecureMail
{
    public partial class Form1 : Form
    {
        const string filter = "rsa files (*.rsa)|*.rsa";
        RSACryptoServiceProvider csp;
        RSAParameters key;
        SmtpClient client;
        MailAddress адресОтправителя;
        MailMessage сообщение;
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
        {;
            InitializeComponent();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Выберите место для сохранения ключей";
            fbd.ShowDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                if (csp==null)
                {
                    csp = new RSACryptoServiceProvider();
                }
                StreamWriter sw = new StreamWriter(fbd.SelectedPath+"\\OpenKey.rsa");
                sw.WriteLine(csp.ToXmlString(false));
                sw.Close();
                sw = new StreamWriter(fbd.SelectedPath + "\\PrivateKey.rsa");
                sw.WriteLine(csp.ToXmlString(true));
                sw.Close();
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
        }
    }
}
