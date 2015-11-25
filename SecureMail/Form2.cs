using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Mail;

namespace SecureMail
{
    public partial class Form2 : Form
    {
        Form1 form;
        public Form2(Form1 F)
        {
            form = F;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(textBox2.Text))
                {
                    MailAddress адрес = new MailAddress(textBox1.Text);
                    form.УстановитьДанныеПользователя(
                        адрес,
                        textBox2.Text);
                    this.Close();
                }
                else
                {
                    MessageBox.Show(this, "Введите пароль!", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (FormatException exp)
            {
                MessageBox.Show(this, exp.Message + "\nПовторите ввод.", "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                textBox2.Text = string.Empty;
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            textBox1.Text = string.Empty;
            textBox2.Text = string.Empty;
            //textBox3.Text = string.Empty;
        }
    }
}
