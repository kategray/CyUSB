namespace BulkLoop
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;


        /* Summary
         The function is used to dispose or clean up all the resources allocated, after the use.
         <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        */
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.StartBtn = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cboINEndpoint = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cboOutEndPoint = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.BytesOutLabel = new System.Windows.Forms.Label();
            this.BytesInLabel = new System.Windows.Forms.Label();
            this.ConstByteBtn = new System.Windows.Forms.RadioButton();
            this.RandomByteBtn = new System.Windows.Forms.RadioButton();
            this.IncrByteBtn = new System.Windows.Forms.RadioButton();
            this.IncrWordBtn = new System.Windows.Forms.RadioButton();
            this.StartValBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cboDeviceConnected = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // StartBtn
            // 
            this.StartBtn.BackColor = System.Drawing.Color.Aquamarine;
            this.StartBtn.Location = new System.Drawing.Point(148, 379);
            this.StartBtn.Name = "StartBtn";
            this.StartBtn.Size = new System.Drawing.Size(85, 32);
            this.StartBtn.TabIndex = 0;
            this.StartBtn.Text = "Start";
            this.StartBtn.UseVisualStyleBackColor = false;
            this.StartBtn.Click += new System.EventHandler(this.StartBtn_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cboINEndpoint);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.cboOutEndPoint);
            this.groupBox1.Location = new System.Drawing.Point(12, 58);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(354, 109);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Endpoint Pair (Out / In) ";
            // 
            // cboINEndpoint
            // 
            this.cboINEndpoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboINEndpoint.FormattingEnabled = true;
            this.cboINEndpoint.Location = new System.Drawing.Point(97, 27);
            this.cboINEndpoint.Name = "cboINEndpoint";
            this.cboINEndpoint.Size = new System.Drawing.Size(247, 21);
            this.cboINEndpoint.TabIndex = 6;
            this.cboINEndpoint.SelectionChangeCommitted += new System.EventHandler(this.cboINEndpoint_SelectionChangeCommitted);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(14, 31);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "IN Endpoints";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 66);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(74, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Out Endpoints";
            // 
            // cboOutEndPoint
            // 
            this.cboOutEndPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboOutEndPoint.FormattingEnabled = true;
            this.cboOutEndPoint.Location = new System.Drawing.Point(97, 64);
            this.cboOutEndPoint.Name = "cboOutEndPoint";
            this.cboOutEndPoint.Size = new System.Drawing.Size(247, 21);
            this.cboOutEndPoint.TabIndex = 2;
            this.cboOutEndPoint.SelectionChangeCommitted += new System.EventHandler(this.cboOutEndPoint_SelectionChangeCommitted);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(42, 327);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(142, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Bytes transferred OUT .........";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(42, 355);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(145, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Bytes transferred IN ..............";
            // 
            // BytesOutLabel
            // 
            this.BytesOutLabel.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.BytesOutLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.BytesOutLabel.Location = new System.Drawing.Point(213, 324);
            this.BytesOutLabel.Name = "BytesOutLabel";
            this.BytesOutLabel.Size = new System.Drawing.Size(120, 19);
            this.BytesOutLabel.TabIndex = 5;
            this.BytesOutLabel.Text = "0";
            this.BytesOutLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // BytesInLabel
            // 
            this.BytesInLabel.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.BytesInLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.BytesInLabel.Location = new System.Drawing.Point(213, 352);
            this.BytesInLabel.Name = "BytesInLabel";
            this.BytesInLabel.Size = new System.Drawing.Size(120, 19);
            this.BytesInLabel.TabIndex = 6;
            this.BytesInLabel.Text = "0";
            this.BytesInLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ConstByteBtn
            // 
            this.ConstByteBtn.AutoSize = true;
            this.ConstByteBtn.Checked = true;
            this.ConstByteBtn.Location = new System.Drawing.Point(207, 14);
            this.ConstByteBtn.Name = "ConstByteBtn";
            this.ConstByteBtn.Size = new System.Drawing.Size(131, 17);
            this.ConstByteBtn.TabIndex = 0;
            this.ConstByteBtn.TabStop = true;
            this.ConstByteBtn.Text = "Output Constant Bytes";
            this.ConstByteBtn.UseVisualStyleBackColor = true;
            // 
            // RandomByteBtn
            // 
            this.RandomByteBtn.AutoSize = true;
            this.RandomByteBtn.Location = new System.Drawing.Point(22, 46);
            this.RandomByteBtn.Name = "RandomByteBtn";
            this.RandomByteBtn.Size = new System.Drawing.Size(88, 17);
            this.RandomByteBtn.TabIndex = 1;
            this.RandomByteBtn.Text = "Random byte";
            this.RandomByteBtn.UseVisualStyleBackColor = true;
            // 
            // IncrByteBtn
            // 
            this.IncrByteBtn.AutoSize = true;
            this.IncrByteBtn.Location = new System.Drawing.Point(207, 45);
            this.IncrByteBtn.Name = "IncrByteBtn";
            this.IncrByteBtn.Size = new System.Drawing.Size(110, 17);
            this.IncrByteBtn.TabIndex = 2;
            this.IncrByteBtn.Text = "Incrementing Byte";
            this.IncrByteBtn.UseVisualStyleBackColor = true;
            // 
            // IncrWordBtn
            // 
            this.IncrWordBtn.AutoSize = true;
            this.IncrWordBtn.Location = new System.Drawing.Point(207, 76);
            this.IncrWordBtn.Name = "IncrWordBtn";
            this.IncrWordBtn.Size = new System.Drawing.Size(113, 17);
            this.IncrWordBtn.TabIndex = 3;
            this.IncrWordBtn.Text = "Incrementing Int32";
            this.IncrWordBtn.UseVisualStyleBackColor = true;
            // 
            // StartValBox
            // 
            this.StartValBox.Location = new System.Drawing.Point(207, 108);
            this.StartValBox.Name = "StartValBox";
            this.StartValBox.Size = new System.Drawing.Size(100, 20);
            this.StartValBox.TabIndex = 4;
            this.StartValBox.Text = "10";
            this.StartValBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.StartValBox.TextChanged += new System.EventHandler(this.StartValBox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 112);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(146, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Constant Value or Start Value";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.StartValBox);
            this.groupBox2.Controls.Add(this.IncrWordBtn);
            this.groupBox2.Controls.Add(this.IncrByteBtn);
            this.groupBox2.Controls.Add(this.RandomByteBtn);
            this.groupBox2.Controls.Add(this.ConstByteBtn);
            this.groupBox2.Location = new System.Drawing.Point(12, 176);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(354, 139);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = " Data Pattern ";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 24);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Connected Devices";
            // 
            // cboDeviceConnected
            // 
            this.cboDeviceConnected.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDeviceConnected.FormattingEnabled = true;
            this.cboDeviceConnected.Location = new System.Drawing.Point(115, 21);
            this.cboDeviceConnected.Name = "cboDeviceConnected";
            this.cboDeviceConnected.Size = new System.Drawing.Size(251, 21);
            this.cboDeviceConnected.TabIndex = 8;
            this.cboDeviceConnected.SelectionChangeCommitted += new System.EventHandler(this.cboDeviceConnected_SelectionChangeCommitted);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(380, 417);
            this.Controls.Add(this.cboDeviceConnected);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.BytesInLabel);
            this.Controls.Add(this.BytesOutLabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.StartBtn);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "C# BulkLoop";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button StartBtn;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label BytesOutLabel;
        private System.Windows.Forms.Label BytesInLabel;
        private System.Windows.Forms.ComboBox cboOutEndPoint;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboINEndpoint;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.RadioButton ConstByteBtn;
        private System.Windows.Forms.RadioButton RandomByteBtn;
        private System.Windows.Forms.RadioButton IncrByteBtn;
        private System.Windows.Forms.RadioButton IncrWordBtn;
        private System.Windows.Forms.TextBox StartValBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboDeviceConnected;
    }
}

