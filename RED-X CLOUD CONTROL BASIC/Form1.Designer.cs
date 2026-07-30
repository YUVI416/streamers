namespace RED_X_CLOUD_CONTROL_BASIC
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
            this.button1 = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBoxHead = new System.Windows.Forms.CheckBox();
            this.checkScopeSniper = new System.Windows.Forms.CheckBox();
            this.checkSwitchSniper = new System.Windows.Forms.CheckBox();
            this.sta = new System.Windows.Forms.Label();
            this.bindBtn = new System.Windows.Forms.Button();
            this.bindBtnHead = new System.Windows.Forms.Button();
            this.bindBtnScope = new System.Windows.Forms.Button();
            this.bindBtnSwitch = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(50, 25);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(178, 52);
            this.button1.TabIndex = 0;
            this.button1.Text = "Aimbot Load";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(92, 83);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(94, 17);
            this.checkBox1.TabIndex = 1;
            this.checkBox1.Text = "Aimbot On/Off";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // sta
            // 
            this.sta.AutoSize = true;
            this.sta.Location = new System.Drawing.Point(12, 275);
            this.sta.Name = "sta";
            this.sta.Size = new System.Drawing.Size(68, 13);
            this.sta.TabIndex = 2;
            this.sta.Text = "Connected !!";
            // 
            // bindBtn
            // 
            this.bindBtn.Location = new System.Drawing.Point(92, 106);
            this.bindBtn.Name = "bindBtn";
            this.bindBtn.Size = new System.Drawing.Size(94, 33);
            this.bindBtn.TabIndex = 3;
            this.bindBtn.Text = "None";
            this.bindBtn.UseVisualStyleBackColor = true;
            this.bindBtn.Click += new System.EventHandler(this.bindBtn_Click);
            // 
            // checkBoxHead (hidden - Head aimbot toggle)
            // 
            this.checkBoxHead.AutoSize = true;
            this.checkBoxHead.Location = new System.Drawing.Point(-500, -500);
            this.checkBoxHead.Name = "checkBoxHead";
            this.checkBoxHead.Size = new System.Drawing.Size(94, 17);
            this.checkBoxHead.TabIndex = 10;
            this.checkBoxHead.Text = "Head On/Off";
            this.checkBoxHead.Visible = false;
            this.checkBoxHead.UseVisualStyleBackColor = true;
            this.checkBoxHead.CheckedChanged += new System.EventHandler(this.checkBoxHead_CheckedChanged);
            // 
            // bindBtnHead (hidden - Head aimbot bind)
            // 
            this.bindBtnHead.Location = new System.Drawing.Point(-500, -500);
            this.bindBtnHead.Name = "bindBtnHead";
            this.bindBtnHead.Size = new System.Drawing.Size(94, 33);
            this.bindBtnHead.TabIndex = 11;
            this.bindBtnHead.Text = "None";
            this.bindBtnHead.Visible = false;
            this.bindBtnHead.UseVisualStyleBackColor = true;
            this.bindBtnHead.Click += new System.EventHandler(this.bindBtnHead_Click);
            // 
            // checkScopeSniper (hidden)
            // 
            this.checkScopeSniper.AutoSize = true;
            this.checkScopeSniper.Location = new System.Drawing.Point(-600, -600);
            this.checkScopeSniper.Name = "checkScopeSniper";
            this.checkScopeSniper.Size = new System.Drawing.Size(94, 17);
            this.checkScopeSniper.TabIndex = 20;
            this.checkScopeSniper.Visible = false;
            this.checkScopeSniper.UseVisualStyleBackColor = true;
            this.checkScopeSniper.CheckedChanged += new System.EventHandler(this.checkScopeSniper_CheckedChanged);
            // 
            // checkSwitchSniper (hidden)
            // 
            this.checkSwitchSniper.AutoSize = true;
            this.checkSwitchSniper.Location = new System.Drawing.Point(-600, -620);
            this.checkSwitchSniper.Name = "checkSwitchSniper";
            this.checkSwitchSniper.Size = new System.Drawing.Size(94, 17);
            this.checkSwitchSniper.TabIndex = 21;
            this.checkSwitchSniper.Visible = false;
            this.checkSwitchSniper.UseVisualStyleBackColor = true;
            this.checkSwitchSniper.CheckedChanged += new System.EventHandler(this.checkSwitchSniper_CheckedChanged);
            // 
            // bindBtnScope (hidden)
            // 
            this.bindBtnScope.Location = new System.Drawing.Point(-600, -640);
            this.bindBtnScope.Name = "bindBtnScope";
            this.bindBtnScope.Size = new System.Drawing.Size(94, 33);
            this.bindBtnScope.TabIndex = 22;
            this.bindBtnScope.Text = "None";
            this.bindBtnScope.Visible = false;
            this.bindBtnScope.UseVisualStyleBackColor = true;
            this.bindBtnScope.Click += new System.EventHandler(this.bindBtnScope_Click);
            // 
            // bindBtnSwitch (hidden)
            // 
            this.bindBtnSwitch.Location = new System.Drawing.Point(-600, -660);
            this.bindBtnSwitch.Name = "bindBtnSwitch";
            this.bindBtnSwitch.Size = new System.Drawing.Size(94, 33);
            this.bindBtnSwitch.TabIndex = 23;
            this.bindBtnSwitch.Text = "None";
            this.bindBtnSwitch.Visible = false;
            this.bindBtnSwitch.UseVisualStyleBackColor = true;
            this.bindBtnSwitch.Click += new System.EventHandler(this.bindBtnSwitch_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(50, 211);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(178, 42);
            this.button2.TabIndex = 4;
            this.button2.Text = "Exit";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(50, 161);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(178, 44);
            this.button3.TabIndex = 5;
            this.button3.Text = "CHAMS MENU";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(278, 297);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.bindBtn);
            this.Controls.Add(this.bindBtnHead);
            this.Controls.Add(this.bindBtnScope);
            this.Controls.Add(this.bindBtnSwitch);
            this.Controls.Add(this.sta);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.checkBoxHead);
            this.Controls.Add(this.checkScopeSniper);
            this.Controls.Add(this.checkSwitchSniper);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBoxHead;
        private System.Windows.Forms.CheckBox checkScopeSniper;
        private System.Windows.Forms.CheckBox checkSwitchSniper;
        private System.Windows.Forms.Label sta;
        private System.Windows.Forms.Button bindBtn;
        private System.Windows.Forms.Button bindBtnHead;
        private System.Windows.Forms.Button bindBtnScope;
        private System.Windows.Forms.Button bindBtnSwitch;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
    }
}

