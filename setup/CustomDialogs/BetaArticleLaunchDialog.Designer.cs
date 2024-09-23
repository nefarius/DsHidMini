namespace Nefarius.DsHidMini.Setup.CustomDialogs;

partial class BetaArticleLaunchDialog
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
            this.banner = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.next = new System.Windows.Forms.Button();
            this.linkLabelBetaArticle = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.banner)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // banner
            // 
            this.banner.BackColor = System.Drawing.Color.White;
            this.banner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.banner.Location = new System.Drawing.Point(0, 0);
            this.banner.Name = "banner";
            this.banner.Size = new System.Drawing.Size(501, 59);
            this.banner.TabIndex = 0;
            this.banner.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.Control;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.banner);
            this.panel2.Location = new System.Drawing.Point(-5, -5);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(503, 61);
            this.panel2.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(30, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(322, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "A web article should have opened in your default Browser by now";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.White;
            this.label1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(144, 14);
            this.label1.TabIndex = 1;
            this.label1.Text = "Online Documentation";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.next);
            this.panel1.Location = new System.Drawing.Point(-3, 308);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(503, 57);
            this.panel1.TabIndex = 9;
            // 
            // next
            // 
            this.next.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.next.Location = new System.Drawing.Point(308, 12);
            this.next.Name = "next";
            this.next.Size = new System.Drawing.Size(75, 23);
            this.next.TabIndex = 0;
            this.next.Text = "[WixUINext]";
            this.next.UseVisualStyleBackColor = true;
            this.next.Click += new System.EventHandler(this.next_Click);
            // 
            // linkLabelBetaArticle
            // 
            this.linkLabelBetaArticle.AutoSize = true;
            this.linkLabelBetaArticle.Location = new System.Drawing.Point(26, 112);
            this.linkLabelBetaArticle.Name = "linkLabelBetaArticle";
            this.linkLabelBetaArticle.Size = new System.Drawing.Size(91, 13);
            this.linkLabelBetaArticle.TabIndex = 11;
            this.linkLabelBetaArticle.TabStop = true;
            this.linkLabelBetaArticle.Text = "Open Beta Article";
            this.linkLabelBetaArticle.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelBetaArticle_LinkClicked);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(29, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(453, 35);
            this.label3.TabIndex = 12;
            this.label3.Text = "The next steps are outlined in the online documentation. If the web site didn\'t o" +
    "pen for you, you can try again by clicking the link below:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(26, 168);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(349, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Contact support if this didn\'t work for some reason. Click Next to finish.";
            // 
            // BetaArticleLaunchDialog
            // 
            this.ClientSize = new System.Drawing.Size(494, 361);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.linkLabelBetaArticle);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "BetaArticleLaunchDialog";
            this.Text = "[UserNameDlg_Title]";
            this.Load += new System.EventHandler(this.dialog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.banner)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox banner;
    private System.Windows.Forms.Panel panel2;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Button next;
    private System.Windows.Forms.LinkLabel linkLabelBetaArticle;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label4;
}