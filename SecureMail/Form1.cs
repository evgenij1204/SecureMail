﻿using System;
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
        const int размерБлокаШифрования = 80;
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
        ДанныеПользователя аккаунт;
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
            FolderBrowserDialog обозревательПапок = new FolderBrowserDialog();
            обозревательПапок.RootFolder = Environment.SpecialFolder.MyDocuments;
            обозревательПапок.Description = "Выберите место для сохранения ключей";
            if (обозревательПапок.ShowDialog() == DialogResult.OK)
            {
                RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider(1024);
                StreamWriter потокПишущийВФайл = new StreamWriter(обозревательПапок.SelectedPath+"\\OpenKey.okey");
                потокПишущийВФайл.WriteLine(RSACSP.ToXmlString(false));
                потокПишущийВФайл.Close();
                потокПишущийВФайл = new StreamWriter(обозревательПапок.SelectedPath + "\\PrivateKey.pkey");
                потокПишущийВФайл.WriteLine(RSACSP.ToXmlString(true));
                потокПишущийВФайл.Close();
                MessageBox.Show(this, "Ключи сохранены по адресу:\n" + обозревательПапок.SelectedPath.ToString(), "Еведомление.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            Form2 формаВводаУчетныхДанных = new Form2(this);
            формаВводаУчетныхДанных.ShowDialog(this);
            формаВводаУчетныхДанных.Focus();
        }
        public void УстановитьДанныеПользователя(MailAddress адрес, string пароль)
        {
            аккаунт.Адрес = адрес;
            аккаунт.Пароль = пароль;
            меткаВСтрокеСтатуса.Text = "Данные пользователя введены.";
            if (groupBox1.Enabled == true)
            {
                кнопкаОТправитьСообщениеКакЕсть.Enabled = true;
                кнопкаОтправитьСообщениеЗашифрованным.Enabled = true;
            }
            if (groupBox5.Enabled==true)
            {
                кнопкаПолучитьСообщение.Enabled = true;
            }
        }
        private void отправитьСообщениеКакЕсть(object sender, EventArgs e)
        {
            отправитьСообщение(richTextBox1.Text);
        }
        void СообщениеДоставлено(object sender, AsyncCompletedEventArgs e)
        {
            MailMessage сообщение = (MailMessage)e.UserState;
            string субъект = сообщение.Subject;
            if (e.Cancelled)
            {
                string cancelled = string.Format("[{0}] Отправка отменена.", субъект);
                MessageBox.Show(cancelled);
            }
            if (e.Error != null)
            {
                string error = String.Format("[{0}] {1}", субъект, e.Error.ToString());
                MessageBox.Show(error,"Ошибка!",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("Сообщение успешно отправлено!","Уведомление.",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        private void отправитьСообщениеЗашифрованным(object sender, EventArgs e)
        {
            OpenFileDialog диалоговоеОкноОткрытияФайла = new OpenFileDialog();
            диалоговоеОкноОткрытияФайла.Filter = фильтрОткрытогоКлюча;
            диалоговоеОкноОткрытияФайла.FilterIndex = 1;
            диалоговоеОкноОткрытияФайла.Title = "Выберите файл открытого ключа.";
            диалоговоеОкноОткрытияФайла.Multiselect = false;
            диалоговоеОкноОткрытияФайла.RestoreDirectory = true;
            if (диалоговоеОкноОткрытияФайла.ShowDialog()==DialogResult.OK)
            {
                try
                {
                    StreamReader потокЧитающийИзФайла = new StreamReader(диалоговоеОкноОткрытияФайла.FileName);
                    string ключ = потокЧитающийИзФайла.ReadToEnd();
                    RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider();
                    RSACSP.FromXmlString(ключ);
                    byte[] массивБайтов = Encoding.UTF32.GetBytes(richTextBox1.Text);
                    string выходнаяСтрока = string.Empty;
                    if (массивБайтов.Length > размерБлокаШифрования)
                    {
                        int итератор = 0;
                        int количествоБлоков = 0;
                        bool флаг = false;
                        byte[] блок = new byte[размерБлокаШифрования];
                        for (int i = 0; i < массивБайтов.Length; i++)
                        {
                            if (итератор < размерБлокаШифрования && i != массивБайтов.Length)
                            {
                                блок[итератор] = массивБайтов[i];
                                итератор++;
                            }
                            else
                            {
                                выходнаяСтрока += ByteToHEXString(RSACSP.Encrypt(блок, true));
                                итератор = 0;
                                количествоБлоков++;
                                i--;
                                if (массивБайтов.Length > размерБлокаШифрования * количествоБлоков + размерБлокаШифрования)
                                    блок = new byte[размерБлокаШифрования];
                                else
                                {
                                    блок = new byte[массивБайтов.Length - размерБлокаШифрования * количествоБлоков];
                                    флаг = true;
                                }
                            }
                        }
                        if (флаг == true)
                            выходнаяСтрока += ByteToHEXString(RSACSP.Encrypt(блок, true));
                    }
                    else
                        выходнаяСтрока += ByteToHEXString(RSACSP.Encrypt(массивБайтов, true));
                    отправитьСообщение("<!MSG_ENCRYPT!>\n<MSG>" + выходнаяСтрока + "</MSG>");
                    потокЧитающийИзФайла.Close();
                }
                catch (Exception exp)
                {
                    показатьСообщениеОбОшибке(exp);
                }
            }
        }
        private string ByteToHEXString(byte[] массив)
        {
            string выходнаяСтрока = string.Empty;
            foreach (var элемент in массив)
            {
                выходнаяСтрока += элемент.ToString("X2");
            }
            return выходнаяСтрока;
        }
        private static byte[] HEXStringToByte(string строка)
        {
            byte[] массивБайтов = new byte[строка.Length / 2];
            int итератор = 0;
            for (int i = 0; i < строка.Length; i += 2)
            {
                массивБайтов[итератор] = Convert.ToByte(строка[i].ToString() + строка[i + 1].ToString(), 16);
                итератор++;
            }
            return массивБайтов;
        }
        private void отправитьСообщение(string строка)
        {
            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                try
                {
                    MailAddress получатель = new MailAddress(textBox1.Text);

                    MailMessage сообщение = new MailMessage(аккаунт.Адрес, получатель);
                    сообщение.Subject = textBox2.Text;
                    сообщение.Body = строка;

                    SmtpClient клиентДляОтправкиСообщения = new SmtpClient("smtp." + аккаунт.Адрес.Host, 25);
                    клиентДляОтправкиСообщения.Credentials = new NetworkCredential(аккаунт.Адрес.Address, аккаунт.Пароль);
                    клиентДляОтправкиСообщения.DeliveryMethod = SmtpDeliveryMethod.Network;
                    клиентДляОтправкиСообщения.EnableSsl = true;
                    object obj = сообщение;
                    клиентДляОтправкиСообщения.SendCompleted += СообщениеДоставлено;
                    клиентДляОтправкиСообщения.SendAsync(сообщение, obj);
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
            кнопкаПолучитьСообщение.Enabled = false;
            button4.Enabled = false;
            аккаунт.Адрес = null;
            аккаунт.Пароль = string.Empty;
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
            аккаунт.Адрес = null;
            аккаунт.Пароль = string.Empty;
            statusStrip1.Text = "Данные пользователя не введены.";
        }
        private void получитьСообщение(object sender, EventArgs e)
        {
            Pop3Client клиентДляПолученияСообщения = new Pop3Client();
            клиентДляПолученияСообщения.Connect("pop." + аккаунт.Адрес.Host, 995, true);
            клиентДляПолученияСообщения.Authenticate(аккаунт.Адрес.Address, аккаунт.Пароль);
            if (клиентДляПолученияСообщения.Connected)
            {
                полученноеСообщение = клиентДляПолученияСообщения.GetMessage(клиентДляПолученияСообщения.GetMessageCount()).ToMailMessage();
                полеОтображенияСообщения.AppendText("От: " + полученноеСообщение.From.Address);
                полеОтображенияСообщения.AppendText("\nТема: " + полученноеСообщение.Subject);
                полеОтображенияСообщения.AppendText("\nСообщение: " + полученноеСообщение.Body);
                клиентДляПолученияСообщения.Disconnect();
                if (полученноеСообщение.Body.IndexOf("<!MSG_ENCRYPT!>") != -1)
                    button4.Enabled = true;
            }
        }
        private void расшифроватьСообщение(object sender, EventArgs e)
        {
            OpenFileDialog диалоговоеОкноОткрытияФайла = new OpenFileDialog();
            диалоговоеОкноОткрытияФайла.Filter = фильтрЗакрытогоКлюча;
            диалоговоеОкноОткрытияФайла.FilterIndex = 1;
            диалоговоеОкноОткрытияФайла.Title = "Выберите файл закрытого ключа.";
            диалоговоеОкноОткрытияФайла.Multiselect = false;
            диалоговоеОкноОткрытияФайла.RestoreDirectory = true;
            if (диалоговоеОкноОткрытияФайла.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StreamReader потокЧитающийИзФайла = new StreamReader(диалоговоеОкноОткрытияФайла.FileName);
                    string ключ = потокЧитающийИзФайла.ReadToEnd();
                    RSACryptoServiceProvider RSACSP = new RSACryptoServiceProvider();
                    RSACSP.FromXmlString(ключ);
                    int началоСообщения = полученноеСообщение.Body.IndexOf("<MSG>") + 5;
                    int конецСообщения = полученноеСообщение.Body.IndexOf("</MSG>") - 5;
                    string телоСообщения = полученноеСообщение.Body.Substring(началоСообщения, конецСообщения - началоСообщения);
                    byte[] массивБайтов =  HEXStringToByte(телоСообщения);
                    string временнаяСтрока = string.Empty;
                    int количествоБлоков = массивБайтов.Length / 128;
                    for (int i = 0; i < количествоБлоков; i++)
                    {
                        byte[] веременныйМассив = массивБайтов.Skip(128 * i).ToArray().Take(128).ToArray();
                        временнаяСтрока += ByteToHEXString(RSACSP.Decrypt(веременныйМассив, true));
                    }
                    телоСообщения = Encoding.UTF32.GetString(HEXStringToByte(временнаяСтрока));
                    полеОтображенияСообщения.Clear();
                    полеОтображенияСообщения.AppendText("От: " + полученноеСообщение.From.Address);
                    полеОтображенияСообщения.AppendText("\nТема: " + полученноеСообщение.Subject);
                    полеОтображенияСообщения.AppendText("\nСообщение: " +телоСообщения+полученноеСообщение.Body.Substring(конецСообщения+6));
                    потокЧитающийИзФайла.Close();
                    RSACSP.Dispose();
                }
                catch (Exception exp)
                {
                    показатьСообщениеОбОшибке(exp);
                }
            }
        }
    }
}
