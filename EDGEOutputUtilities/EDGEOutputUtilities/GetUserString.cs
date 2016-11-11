using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    /* example of using this functionality:
    GetUserString gus = new GetUserString("Enter Name for New View:", "OK", "Cancel");
    if (gus.ShowDialog() == DialogResult.Cancel)
       return false;
    // gus.UserString now contains the user's input
     */

    public partial class GetUserString : Form
    {
        /// <summary>
        /// if true, disallow \/:*?!\ etc.
        /// </summary>
        public bool bCheckForLegalFileName = true;

        /// <summary>
        /// IF FALSE, NO ENTRY (WHEN CLICKING OKAY) GETS AN ERROR
        /// </summary>
        public bool bAllowEmpty = false;

        public GetUserString()
        {
            InitializeComponent();
        }

        /// <summary>
        /// PROMPT THE USER FOR A STRING
        /// </summary>
        /// <param name="strText">THE PROMPT</param>
        /// <param name="strButton1">THE NAME OF A BUTTON, SUCH AS OK</param>
        public GetUserString(string strText, string strButton1)
            : this()
        {
            this.label1.Text = strText;
            button1.Text = strButton1;
            button1.Visible = true;
        }

        /// <summary>
        /// PROMPT THE USER FOR A STRING
        /// </summary>
        /// <param name="strText">THE PROMPT</param>
        /// <param name="strButton1">THE NAME OF A BUTTON, SUCH AS OK</param>
        /// <param name="strButton2">THE NAME OF A SECOND BUTTON, WHICH CAN BE EMPTY OR NULL FOR NO SECOND BUTTON</param>
        public GetUserString(string strText, string strButton1, string strButton2)
            : this(strText, strButton1)
        {
            if (!string.IsNullOrEmpty(strButton2))
            {
                button2.Text = strButton2;
                button2.Visible = true;
            }
        }

        /// <summary>
        /// PROMPT THE USER FOR A STRING
        /// </summary>
        /// <param name="strText">THE PROMPT</param>
        /// <param name="strButton1">THE NAME OF A BUTTON, SUCH AS OK</param>
        /// <param name="strButton2">THE NAME OF A SECOND BUTTON, WHICH CAN BE EMPTY OR NULL FOR NO SECOND BUTTON</param>
        /// <param name="strButton3">THE NAME OF A THIRD BUTTON, WHICH CAN BE EMPTY OR NULL FOR NO THIRD BUTTON</param>
        public GetUserString(string strText, string strButton1, string strButton2, string strButton3)
            : this(strText, strButton1, strButton2)
        {
            if (!string.IsNullOrEmpty(strButton3))
            {
                button3.Text = strButton3;
                button3.Visible = true;
            }
        }

        /// <summary>
        /// PROMPT THE USER FOR A STRING
        /// </summary>
        /// <param name="strText">THE PROMPT</param>
        /// <param name="strButton1">THE NAME OF A BUTTON, SUCH AS OK</param>
        /// <param name="strButton2">THE NAME OF A SECOND BUTTON, WHICH CAN BE EMPTY OR NULL FOR NO SECOND BUTTON</param>
        /// <param name="strButton3">THE NAME OF A THIRD BUTTON, WHICH CAN BE EMPTY OR NULL FOR NO THIRD BUTTON</param>
        /// <param name="strDefault">THE DEFAULT VALUE OF THE RETURN VALUE</param>
        public GetUserString(string strText, string strButton1, string strButton2, string strButton3, string strDefault)
            : this(strText, strButton1, strButton2, strButton3)
        {
            textBox1.Text = strDefault;
        }

        private void GetUserString_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "" && !bAllowEmpty)
            {
                Util.error("You must enter a value.");
                textBox1.Focus();
                return;
            }
            if (bCheckForLegalFileName && textBox1.Text.IndexOfAny(new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '!' }) > -1)
            {
                Util.error("The following characters may not be used: \\ /:*?!\"<>");
                textBox1.Focus();
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public string UserString
        {
            get { return this.textBox1.Text; }
        }

        public string Default
        {
            set { this.textBox1.Text = value; }
        }

        private void GetUserString_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            Program.ShowHelp("Default.htm");
        }
    }
}