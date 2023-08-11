using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PassManager
{
    public partial class SelecScreen : Form
    {
        private string login_user;
        public SelecScreen(string login_user)
        {
            InitializeComponent();
            this.login_user = login_user;
        }

        private void button1_Click(object sender, EventArgs e)
        {            
            PassGenerator passGenerator = new PassGenerator(login_user);
            passGenerator.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PassShow passShow = new PassShow(login_user);
            passShow.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DelUser delUser = new DelUser(login_user);
            delUser.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ChangePass changePass = new ChangePass(login_user);
            changePass.Show();
            this.Hide();
        }
    }
}
