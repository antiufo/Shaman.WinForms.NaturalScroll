using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Shaman.WinForms
{

    public class NaturalScroll : IMessageFilter
    {

        private const int WM_MOUSEWHEEL = 0x20A;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        private NaturalScroll() { }

        bool IMessageFilter.PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                var left = (int)m.LParam & 0xFFFF;
                var top = ((int)m.LParam >> 16) & 0xFFFF;

                var controlHandle = WindowFromPoint(new POINT(left, top));

                var args = new WheelInterceptedEventArgs(controlHandle, m.HWnd);

                if (WheelIntercepted != null)
                    WheelIntercepted(this, args);

                if (args.DestinationHandle != IntPtr.Zero)
                {
                    SendMessage(args.DestinationHandle, m.Msg, m.WParam, m.LParam);
                    return true;
                }
            }

            return false;
        }

        public static event EventHandler<WheelInterceptedEventArgs> WheelIntercepted;



        private static NaturalScroll _filter;

        public static bool Enabled
        {
            get
            {
                return _filter != null;
            }
            set
            {
                if (Enabled == value) return;

                if (value)
                {
                    _filter = new NaturalScroll();
                    Application.AddMessageFilter(_filter);
                }
                else
                {
                    Application.RemoveMessageFilter(_filter);
                    _filter = null;
                }
            }
        }



    }


    public class WheelInterceptedEventArgs : EventArgs
    {
        internal WheelInterceptedEventArgs(IntPtr destination, IntPtr originalDestination)
        {
            DestinationHandle = destination;
            _originalDestination = originalDestination;
        }

        private Control _destination;
        public Control Destination
        {
            get
            {
                return _destination ?? (_destination = Control.FromHandle(DestinationHandle));
            }
            set
            {
                _destination = value;
                DestinationHandle = value != null ? value.Handle : IntPtr.Zero;
            }
        }

        private IntPtr _handle;
        public IntPtr DestinationHandle
        {
            get
            {
                return _handle;
            }
            set
            {
                _handle = value;
                _destination = null;
            }
        }

        private IntPtr _originalDestination;
        public IntPtr OriginalDestination
        {
            get
            {
                return _originalDestination;
            }
        }

        public bool DestinationIsInCurrentProcess
        {
            get
            {
                int processId;
                var thread = GetWindowThreadProcessId(DestinationHandle, out processId);
                using (var current = System.Diagnostics.Process.GetCurrentProcess())
                {
                    return current.Id == processId;
                }
            }
        }

        public void MoveToRootWindow()
        {
            DestinationHandle = GetAncestor(DestinationHandle, GetAncestor_Flags.GetRoot);
        }

        public void KeepDefault()
        {
            DestinationHandle = IntPtr.Zero;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestor_Flags gaFlags);

        private enum GetAncestor_Flags
        {
            GetParent = 1,
            GetRoot = 2,
            GetRootOwner = 3
        }


    }

}
