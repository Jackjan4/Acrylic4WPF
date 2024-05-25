using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;


namespace Acrylic4WPF {

    internal enum AccentState {
        ACCENT_DISABLED = 1,
        ACCENT_ENABLE_GRADIENT = 0,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_INVALID_STATE = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }


    /// <summary>
    /// 
    /// </summary>
    public class AcrylWindow : Window, INotifyPropertyChanged {
        // ===== Blur things ========

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        internal void EnableBlur() {
            WindowInteropHelper windowHelper = new WindowInteropHelper(this);

            AccentPolicy accent = new AccentPolicy {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };

            int accentStructSize = Marshal.SizeOf(accent);

            IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            WindowCompositionAttributeData data = new WindowCompositionAttributeData {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }



        /// <summary>
        /// Property for changing the color of the TransparentBackground
        /// </summary>
        public Brush TransparentBackground {
            get => _transparentBackground;
            set {
                _transparentBackground = value;
                OnPropertyChanged("TransparentBackground");
            }
        }

        private Brush _transparentBackground;



        /// <summary>
        /// Changes the opacity of the Acrylic transparent background
        /// </summary>
        public double AcrylOpacity {
            get => _acrylOpacity;
            set {
                _acrylOpacity = value;
                OnPropertyChanged("AcrylOpacity");
            }
        }

        private double _acrylOpacity;



        /// <summary>
        /// 
        /// </summary>
        public Visibility ShowMinimizeButton {
            get => _showMinimizeButton;
            set {
                _showMinimizeButton = value;
                OnPropertyChanged("ShowMinimizeButton");
            }
        }

        private Visibility _showMinimizeButton;



        /// <summary>
        /// 
        /// </summary>
        public Visibility ShowFullscreenButton {
            get => _showFullscreenButton;
            set {
                _showFullscreenButton = value;
                OnPropertyChanged("ShowFullscreenButton");
            }
        }

        private Visibility _showFullscreenButton;



        /// <summary>
        /// 
        /// </summary>
        public Visibility ShowCloseButton {
            get => _showCloseButton;
            set {
                _showCloseButton = value;
                OnPropertyChanged("ShowCloseButton");
            }
        }

        private Visibility _showCloseButton;



        /// <summary>
        /// 
        /// </summary>
        public double NoiseRatio {
            get => _noiseRatio;
            set {
                _noiseRatio = value;
                OnPropertyChanged("NoiseRatio");
            }
        }
        private double _noiseRatio;



        /// <summary>
        /// Event implementation
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string info) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }



        private readonly Grid _mainGrid;
        private readonly Grid _contentGrid;
        private readonly string _internalGridName;
        private bool _nowFullScreen = false;


        /// <summary>
        /// Constructor
        /// </summary>
        public AcrylWindow() {
            Loaded += MainWindow_Loaded;
            SourceInitialized += new EventHandler(win_SourceInitialized);

            AcrylOpacity = 0.6;
            _contentGrid = new Grid();
            Grid.SetRow(_contentGrid, 1);
            _internalGridName = "internalMainGrid";
            _mainGrid = new Grid {
                Name = _internalGridName
            };

            AllowsTransparency = true;
            Background = Brushes.Transparent;
            base.WindowStyle = WindowStyle.None;

            Content = BuildBaseWindow();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="arg1">The old content</param>
        /// <param name="arg2">The new content</param>
        protected override void OnContentChanged(object arg1, object arg2) {
            // Do nothing if this is the initialize call
            if (arg2 is Grid grid && grid.Name == _internalGridName) {
                return;
            }

            Content = _mainGrid;
            _contentGrid.Children.Clear();
            _contentGrid.Children.Add((UIElement) arg2);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Grid BuildBaseWindow() {

            // Transparent effect rectangle
            Rectangle rect = new Rectangle {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            rect.SetBinding(Shape.FillProperty, new Binding {
                Path = new PropertyPath("TransparentBackground"),
                Source = this,
                FallbackValue = Brushes.LightGray,
                TargetNullValue = Brushes.LightGray
            });
            rect.SetBinding(UIElement.OpacityProperty, new Binding {
                Path = new PropertyPath("AcrylOpacity"),
                Source = this,
                FallbackValue = 0.6,
                TargetNullValue = 0.6
            });

            // Add the noise effect to the rectangle
            NoiseEffect.NoiseEffect fx = new NoiseEffect.NoiseEffect();
            BindingOperations.SetBinding(fx, NoiseEffect.NoiseEffect.RatioProperty, new Binding {
                Path = new PropertyPath("NoiseRatio"),
                TargetNullValue = 0.1,
                FallbackValue = 0.1,
                Source = this,
            });
            rect.Effect = fx;


            _mainGrid.Children.Add(rect);

            Grid windowGrid = new Grid();
            windowGrid.RowDefinitions.Add(new RowDefinition {
                MaxHeight = 30
            });
            windowGrid.RowDefinitions.Add(new RowDefinition());
            _mainGrid.Children.Add(windowGrid);

            Grid titleBar = BuildTitleBar();

            windowGrid.Children.Add(titleBar);
            windowGrid.Children.Add(_contentGrid);
            return _mainGrid;
        }



        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Grid BuildTitleBar() {

            // Build the close button
            Button closeButton = new Button {
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = "\uE711",
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                MinWidth = 45,
            };
            closeButton.SetBinding(UIElement.VisibilityProperty, new Binding {
                Source = this,
                Path = new PropertyPath("ShowCloseButton"),
                TargetNullValue = Visibility.Visible
            });
            Grid.SetColumn(closeButton, 2);
            closeButton.Click += (x, y) => Close();


            // Build the maximize/reset button
            Button fullscreenButton = new Button {
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = "\uE922",
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                MinWidth = 45
            };
            fullscreenButton.SetBinding(UIElement.VisibilityProperty, new Binding {
                Source = this,
                Path = new PropertyPath("ShowFullscreenButton"),
                TargetNullValue = Visibility.Visible
            });
            Grid.SetColumn(fullscreenButton, 1);

            fullscreenButton.Click += (x, y) => {
                if (_nowFullScreen) {
                    WindowState = WindowState.Normal;
                    _nowFullScreen = false;
                }
                else {
                    WindowState = WindowState.Maximized;
                    _nowFullScreen = true;
                }
            };



            // Build the minimize button
            Button minimizeButton = new Button {
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = "\uE921",
                FontSize = 12,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                MinWidth = 45,
            };
            minimizeButton.SetBinding(UIElement.VisibilityProperty, new Binding {
                Source = this,
                Path = new PropertyPath("ShowMinimizeButton"),
                TargetNullValue = Visibility.Visible
            });
            Grid.SetColumn(minimizeButton, 0);
            minimizeButton.Click += (x, y) => WindowState = WindowState.Minimized;

            Grid titleBar = new Grid {
                MaxHeight = 30
            };
            Grid titleBarButtons = new Grid {
                MaxHeight = 30,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            titleBarButtons.ColumnDefinitions.Add(new ColumnDefinition {
                Width = GridLength.Auto
            });
            titleBarButtons.ColumnDefinitions.Add(new ColumnDefinition {
                Width = GridLength.Auto
            });
            titleBarButtons.ColumnDefinitions.Add(new ColumnDefinition {
                Width = GridLength.Auto
            });


            // Build the drag button - workaround for still dragging/double clicking the window
            Button dragButton = new Button {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Foreground = Brushes.Transparent
            };
            ResourceDictionary res = new ResourceDictionary {
                Source = new Uri("/Acrylic4WPF;component/XAML/StyleResources.xaml", UriKind.RelativeOrAbsolute)
            };
            dragButton.Style = res["NoHoverStyle"] as Style;
            dragButton.PreviewMouseLeftButtonDown += (sender, e) => {
                DragMove();
                if (e.ClickCount == 2)
                    OnTitleBarDoubleClick();
            };

            int frames = 0;
            bool inDrag = false;
            dragButton.PreviewMouseMove += (sender, e) => {
                if (inDrag && e.LeftButton == MouseButtonState.Pressed) {
                    DragMove();
                    inDrag = false;
                    return;
                }
                if (e.LeftButton == MouseButtonState.Pressed && _nowFullScreen) {
                    if (frames == 3) {
                        Point p = Mouse.GetPosition(this);
                        WindowState = WindowState.Normal;
                        Top = p.Y - 5;
                        Left = p.X - Width / 2;
                        _nowFullScreen = false;
                        DragMove();
                        inDrag = true;
                    }
                    else {
                        frames++;
                    }
                }
                else if (e.LeftButton == MouseButtonState.Released) {
                    frames = 0;
                }
            };

            // Add all buttons the scenery
            titleBar.Children.Add(dragButton);
            titleBar.Children.Add(titleBarButtons);
            titleBarButtons.Children.Add(closeButton);
            titleBarButtons.Children.Add(fullscreenButton);
            titleBarButtons.Children.Add(minimizeButton);

            return titleBar;
        }



        /// <summary>
        /// 
        /// </summary>
        private void OnTitleBarDoubleClick() {
            if (_nowFullScreen) {
                WindowState = WindowState.Normal;
                _nowFullScreen = false;
            }
            else {
                WindowState = WindowState.Maximized;
                _nowFullScreen = true;
            }
        }






        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            EnableBlur();
        }









        // ==== Magic code from here: https://blogs.msdn.microsoft.com/llobo/2006/08/01/maximizing-window-with-windowstylenone-considering-taskbar/
        // All credits go to: LesterLobo
        // Code for preventing window to go out of area when maximizing - because windows with no WindowStyle do that; WPF bug


        private static IntPtr WindowProc(
              IntPtr hwnd,
              int msg,
              IntPtr wParam,
              IntPtr lParam,
              ref bool handled) {
            switch (msg) {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam) {

            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero) {

                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }


        /// <summary>
        /// POINT aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y) {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };


        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO {
            /// <summary>
            /// </summary>            
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>            
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>            
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>            
            public int dwFlags = 0;
        }


        /// <summary> Win32 </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new RECT();

            /// <summary> Win32 </summary>
            public int Width {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom) {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }


            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc) {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty {
                get {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString() {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj) {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode() {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }


            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2) {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2) {
                return !(rect1 == rect2);
            }


        }

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);

        /// <summary>
        /// 
        /// </summary>
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);


        void win_SourceInitialized(object sender, EventArgs e) {
            System.IntPtr handle = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            System.Windows.Interop.HwndSource.FromHwnd(handle).AddHook(new System.Windows.Interop.HwndSourceHook(WindowProc));
        }

    }
    }
