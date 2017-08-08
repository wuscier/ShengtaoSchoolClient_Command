using Prism.Mvvm;
using System;
using System.Windows;
using System.Windows.Forms;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace St.Common
{
    public class ViewFrame : BindableBase
    {
        public ViewFrame(IntPtr viewIntPtr, PictureBox pictureBox, Label label)
        {
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            Height = 0;
            Width = 0;
            Row = 0;
            Column = 0;
            RowSpan = 1;
            ColumnSpan = 1;
            Visibility = Visibility.Collapsed;
            ViewType = 1;
            Hwnd = viewIntPtr;
            IsBigView = false;
            ViewOrder = 0;
            IsOpened = false;
            PictureBox = pictureBox;
            Label = label;
            PictureBox.Paint += (sender, args) =>
            {
                Label.AutoSize = true;
                Label.Text = ViewName;
                Label.Visible = !string.IsNullOrEmpty(ViewName);
                Label.Location = new System.Drawing.Point((PictureBox.Width - Label.Width) / 2,
                    PictureBox.Height - Label.Height - 10);
            };
        }


        private double width;

        public double Width
        {
            get { return width; }
            set { SetProperty(ref width, value); }
        }

        private double height;

        public double Height
        {
            get { return height; }
            set { SetProperty(ref height, value); }
        }

        private VerticalAlignment verticalAlignment;

        public VerticalAlignment VerticalAlignment
        {
            get { return verticalAlignment; }
            set { SetProperty(ref verticalAlignment, value); }
        }

        private System.Windows.HorizontalAlignment horizontalAlignment;

        public System.Windows.HorizontalAlignment HorizontalAlignment
        {
            get { return horizontalAlignment; }
            set { SetProperty(ref horizontalAlignment, value); }
        }

        private int row;

        public int Row
        {
            get { return row; }
            set { SetProperty(ref row, value); }
        }

        private int rowSpan;

        public int RowSpan
        {
            get { return rowSpan; }
            set { SetProperty(ref rowSpan, value); }
        }

        private int column;

        public int Column
        {
            get { return column; }
            set { SetProperty(ref column, value); }
        }

        private int columnSpan;

        public int ColumnSpan
        {
            get { return columnSpan; }
            set { SetProperty(ref columnSpan, value); }
        }

        private Visibility visibility;

        public Visibility Visibility
        {
            get { return visibility; }
            set { SetProperty(ref visibility, value); }
        }

        private IntPtr hwnd;

        public IntPtr Hwnd
        {
            get { return hwnd; }
            set { SetProperty(ref hwnd, value); }
        }

        private string phoneId;

        public string PhoneId
        {
            get { return phoneId; }
            set { SetProperty(ref phoneId, value); }
        }

        private int viewType;

        public int ViewType
        {
            get { return viewType; }
            set { SetProperty(ref viewType, value); }
        }

        private string viewName;

        public string ViewName
        {
            get { return viewName; }
            set { SetProperty(ref viewName, value); }
        }

        private bool isBigView;

        public bool IsBigView
        {
            get { return isBigView; }
            set { SetProperty(ref isBigView, value); }
        }

        private int viewOrder;

        public int ViewOrder
        {
            get { return viewOrder; }
            set { SetProperty(ref viewOrder, value); }
        }

        private bool isOpened;

        public bool IsOpened
        {
            get { return isOpened; }
            set { SetProperty(ref isOpened, value); }
        }

        public PictureBox PictureBox { get; set; }
        public Label Label { get; set; }
    }
}