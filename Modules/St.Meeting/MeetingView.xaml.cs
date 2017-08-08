using System.Windows;
using System;
using System.Drawing;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;

namespace St.Meeting
{
    /// <summary>
    /// MeetingView.xaml 的交互逻辑
    /// </summary>
    public partial class MeetingView
    {
        public MeetingView(Action<bool, string> startMeetingCallback, Action<bool, string> exitMeetingCallback)
        {
            InitializeComponent();
            InitPictureBoxsAndLabels();

            MeetingViewModel mvm = new MeetingViewModel(this, startMeetingCallback, exitMeetingCallback);
            LayoutMenu.SubmenuOpened += mvm.RefreshLayoutMenu;
            DataContext = mvm;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window win = sender as Window;
            win.DragMove();
        }

        public void InitPictureBoxsAndLabels()
        {
            PictureBox1 = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill
            };
            PictureBox2 = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill
            };
            PictureBox3 = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill
            };
            PictureBox4 = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill
            };
            PictureBox5 = new PictureBox()
            {
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill
            };

            Label1 = new Label()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Visible = false
            };
            Label2 = new Label()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Visible = false
            };
            Label3 = new Label()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Visible = false
            };
            Label4 = new Label()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Visible = false
            };
            Label5 = new Label()
            {
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Visible = false
            };

            Panel1.Controls.Add(Label1);
            Panel2.Controls.Add(Label2);
            Panel3.Controls.Add(Label3);
            Panel4.Controls.Add(Label4);
            Panel5.Controls.Add(Label5);

            Panel1.Controls.Add(PictureBox1);
            Panel2.Controls.Add(PictureBox2);
            Panel3.Controls.Add(PictureBox3);
            Panel4.Controls.Add(PictureBox4);
            Panel5.Controls.Add(PictureBox5);
        }

        public Label Label1;
        public Label Label2;
        public Label Label3;
        public Label Label4;
        public Label Label5;

        public PictureBox PictureBox1;
        public PictureBox PictureBox2;
        public PictureBox PictureBox3;
        public PictureBox PictureBox4;
        public PictureBox PictureBox5;
    }
}
