namespace WindowsFormsApplication2
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.processMvsMD = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.btnDownload = new System.Windows.Forms.Button();
            this.btnGetAllClaims = new System.Windows.Forms.Button();
            this.tbxFindText = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.btnGetRIEligibleClaims = new System.Windows.Forms.Button();
            this.btnTemp = new System.Windows.Forms.Button();
            this.btnXML_fromCARA_SDC = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // processMvsMD
            // 
            this.processMvsMD.Location = new System.Drawing.Point(22, 12);
            this.processMvsMD.Name = "processMvsMD";
            this.processMvsMD.Size = new System.Drawing.Size(106, 23);
            this.processMvsMD.TabIndex = 0;
            this.processMvsMD.Text = "Process M vs. MD";
            this.processMvsMD.UseVisualStyleBackColor = true;
            this.processMvsMD.Click += new System.EventHandler(this.btnProcessMvsMD_Click);
            this.processMvsMD.MouseHover += new System.EventHandler(this.button1_MouseHover);
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(22, 41);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(811, 488);
            this.textBox1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(344, 12);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(94, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Beautify XML file";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            this.button2.MouseHover += new System.EventHandler(this.button2_MouseHover);
            // 
            // btnDownload
            // 
            this.btnDownload.Location = new System.Drawing.Point(134, 12);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(204, 23);
            this.btnDownload.TabIndex = 3;
            this.btnDownload.Text = "Download Everything from AWS outbox";
            this.btnDownload.UseVisualStyleBackColor = true;
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            this.btnDownload.MouseHover += new System.EventHandler(this.btnDownload_MouseHover);
            // 
            // btnGetAllClaims
            // 
            this.btnGetAllClaims.Location = new System.Drawing.Point(444, 12);
            this.btnGetAllClaims.Name = "btnGetAllClaims";
            this.btnGetAllClaims.Size = new System.Drawing.Size(121, 23);
            this.btnGetAllClaims.TabIndex = 4;
            this.btnGetAllClaims.Text = "Get Claims matching:";
            this.btnGetAllClaims.UseVisualStyleBackColor = true;
            this.btnGetAllClaims.Click += new System.EventHandler(this.btnGetAllClaims_Click);
            // 
            // tbxFindText
            // 
            this.tbxFindText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbxFindText.Location = new System.Drawing.Point(571, 12);
            this.tbxFindText.Multiline = true;
            this.tbxFindText.Name = "tbxFindText";
            this.tbxFindText.Size = new System.Drawing.Size(262, 23);
            this.tbxFindText.TabIndex = 5;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(859, 12);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(210, 23);
            this.button3.TabIndex = 6;
            this.button3.Text = "format RIDE claims for audit";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // btnGetRIEligibleClaims
            // 
            this.btnGetRIEligibleClaims.Location = new System.Drawing.Point(859, 41);
            this.btnGetRIEligibleClaims.Name = "btnGetRIEligibleClaims";
            this.btnGetRIEligibleClaims.Size = new System.Drawing.Size(210, 23);
            this.btnGetRIEligibleClaims.TabIndex = 7;
            this.btnGetRIEligibleClaims.Text = "get RI-eligible claims from M XML";
            this.btnGetRIEligibleClaims.UseVisualStyleBackColor = true;
            this.btnGetRIEligibleClaims.Click += new System.EventHandler(this.btnGetRIEligibleClaims_Click);
            // 
            // btnTemp
            // 
            this.btnTemp.Location = new System.Drawing.Point(859, 70);
            this.btnTemp.Name = "btnTemp";
            this.btnTemp.Size = new System.Drawing.Size(210, 23);
            this.btnTemp.TabIndex = 8;
            this.btnTemp.Text = "temp";
            this.btnTemp.UseVisualStyleBackColor = true;
            this.btnTemp.Click += new System.EventHandler(this.btnTemp_Click);
            // 
            // btnXML_fromCARA_SDC
            // 
            this.btnXML_fromCARA_SDC.Location = new System.Drawing.Point(859, 99);
            this.btnXML_fromCARA_SDC.Name = "btnXML_fromCARA_SDC";
            this.btnXML_fromCARA_SDC.Size = new System.Drawing.Size(210, 23);
            this.btnXML_fromCARA_SDC.TabIndex = 9;
            this.btnXML_fromCARA_SDC.Text = "Delimit CARA Txt File";
            this.btnXML_fromCARA_SDC.UseVisualStyleBackColor = true;
            this.btnXML_fromCARA_SDC.Click += new System.EventHandler(this.btnDelimit_CARA_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1081, 541);
            this.Controls.Add(this.btnXML_fromCARA_SDC);
            this.Controls.Add(this.btnTemp);
            this.Controls.Add(this.btnGetRIEligibleClaims);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.tbxFindText);
            this.Controls.Add(this.btnGetAllClaims);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.processMvsMD);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button processMvsMD;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnGetAllClaims;
        private System.Windows.Forms.TextBox tbxFindText;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button btnGetRIEligibleClaims;
        private System.Windows.Forms.Button btnTemp;
        private System.Windows.Forms.Button btnXML_fromCARA_SDC;
    }
}

