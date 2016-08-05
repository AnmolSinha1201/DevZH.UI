﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DevZH.UI.Drawing;
using DevZH.UI.Interface;
using DevZH.UI.Interop;
using DevZH.UI.Utils;

namespace DevZH.UI
{
    public class Window : Control
    {
        private string _title = "DevZH.UI";

        private static readonly Dictionary<IntPtr, Window> Windows = new Dictionary<IntPtr, Window>();

        public EventHandler<CancelEventArgs> Closing;

        public Control Child
        {
            set
            {
                if (value != null && value.Verify())
                {
                    NativeMethods.WindowSetChild(ControlHandle, value.ControlHandle);
                }
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                var title = value;
                if (!ValidTitle(title)) title = _title;
                NativeMethods.WindowSetTitle(ControlHandle, StringUtil.GetBytes(title));
                _title = title;
            }
        }

        private Point _location = new Point();

        public Point Location
        {
            get
            {
                int x, y;
                NativeMethods.WindowPosition(ControlHandle, out x, out y);
                _location.X = x;
                _location.Y = y;
                return _location;
            }
            set
            {
                _location = value;
                NativeMethods.WindowSetPosition(ControlHandle, (int) value.X, (int) value.Y);
                StartPosition = WindowStartPosition.Manual;
            }
        }

        private Size _size = new Size();
        public Size Size
        {
            get
            {
                int w, h;
                NativeMethods.WindowContentSize(ControlHandle, out w, out h);
                _size.Width = w;
                _size.Height = h;
                return _size;
            }
            set
            {
                _size = value;
                NativeMethods.WindowSetContentSize(ControlHandle, (int) value.Width, (int) value.Height);
            }
        }

        public bool AllowMargins
        {
            get { return NativeMethods.WindowMargined(ControlHandle); }
            set { NativeMethods.WindowSetMargined(ControlHandle, value);}
        }

        public bool FullScreen
        {
            get { return NativeMethods.WindowFullscreen(ControlHandle); }
            set { NativeMethods.WindowSetFullscreen(ControlHandle, value);}
        }

        public bool Borderless
        {
            get { return NativeMethods.WindowBorderless(ControlHandle); }
            set { NativeMethods.WindowSetBorderless(ControlHandle, value); }
        }

        public Window(string title, int width = 500, int height = 200, bool hasMemubar = false)
        {
            if (!ValidTitle(title)) title = _title;
            this.ControlHandle = NativeMethods.NewWindow(StringUtil.GetBytes(title), width, height, hasMemubar);
            _title = title;
            Windows.Add(this.ControlHandle.DangerousGetHandle(), this);
            this.InitializeEvents();
            this.InitializeData();
        }

        protected void InitializeEvents()
        {
            if (!this.Verify())
            {
                throw new TypeInitializationException(nameof(Window), new InvalidComObjectException());
            }
            NativeMethods.WindowOnClosing(this.ControlHandle, (window, data) =>
            {
                var args = new CancelEventArgs();
                OnClosing(args);
                // args.Cancel: True is not closing. False is to be closed and destroyed.
                // It maybe a little different to other wrapper.
                var cancel = args.Cancel;
                if (!cancel)
                {
                    if (Windows.Count > 1)
                    {
                        var intptr = this.ControlHandle.DangerousGetHandle();
                        Windows.Remove(intptr);
                    }
                    else
                    {
                        Application.Current.Exit();
                    }
                }
                return !cancel;
            }, IntPtr.Zero);

            NativeMethods.WindowOnPositionChanged(this.ControlHandle, (window, data) =>
            {
                OnLocationChanged(EventArgs.Empty);
            }, IntPtr.Zero);

            NativeMethods.WindowOnContentSizeChanged(this.ControlHandle, (window, data) =>
            {
                OnResize(EventArgs.Empty);
            }, IntPtr.Zero);
        }

        private void InitializeData()
        {
        }

        protected virtual void OnClosing(CancelEventArgs e)
        {
            Closing?.Invoke(this, e);
        }

        protected bool ValidTitle(string title) => !string.IsNullOrEmpty(title);

        public WindowStartPosition StartPosition { get; set; }

        public override void Show()
        {
            switch (StartPosition)
            {
                case WindowStartPosition.CenterScreen:
                    CenterToScreen();
                    break;
            }
            base.Show();
        }

        public void Close()
        {
            var intptr = this.ControlHandle.DangerousGetHandle();
            base.Destroy();
            Windows.Remove(intptr);
        }

        public void CenterToScreen()
        {
            NativeMethods.WindowCenter(ControlHandle);
        }
    }
}
