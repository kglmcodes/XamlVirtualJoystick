﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace WpfCustomControls
{
    /// <summary>Interaction logic for Joystick.xaml</summary>
    public partial class OnScreenJoystick : UserControl
    {
        public enum Direction : byte
        {
            NONE,
            UP,
            UP_RIGHT,
            RIGHT,
            DOWN_RIGHT,
            DOWN,
            LEFT,
            DOWN_LEFT,
            UP_LEFT,
            MAX = DOWN_RIGHT
        }

        double DesiredMaxDistance;

        // Using a DependencyProperty as the backing store for Dir.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DirProperty =
            DependencyProperty.Register("Dir", typeof(Direction), typeof(OnScreenJoystick), new PropertyMetadata(Direction.NONE));

        /// <summary>Current angle in degrees from 0 to 360</summary>
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(OnScreenJoystick), null);

        /// <summary>Current distance (or "power"), from 0 to 100</summary>
        public static readonly DependencyProperty DistanceProperty =
            DependencyProperty.Register("Distance", typeof(double), typeof(OnScreenJoystick), null);

        /// <summary>How often should be raised StickMove event in degrees</summary>
        public static readonly DependencyProperty AngleStepProperty =
            DependencyProperty.Register("AngleStep", typeof(double), typeof(OnScreenJoystick), new PropertyMetadata(1.0));

        /// <summary>How often should be raised StickMove event in distance units</summary>
        public static readonly DependencyProperty DistanceStepProperty =
            DependencyProperty.Register("DistanceStep", typeof(double), typeof(OnScreenJoystick), new PropertyMetadata(1.0));

        /* Unstable - needs work */
        ///// <summary>Indicates whether the joystick knob resets its place after being released</summary>
        //public static readonly DependencyProperty ResetKnobAfterReleaseProperty =
        //    DependencyProperty.Register(nameof(ResetKnobAfterRelease), typeof(bool), typeof(VirtualJoystick), new PropertyMetadata(true));

        /// <summary>Current angle in degrees from 0 to 360</summary>
        public double Angle
        {
            get { return Convert.ToDouble(GetValue(AngleProperty)); }
            private set { SetValue(AngleProperty, value); }
        }

        ///<summary>Current direction in Direction Enum</summary>
        public Direction Dir
        {
            get { return (Direction)GetValue(DirProperty); }
            set { SetValue(DirProperty, value); }
        }

        /// <summary>current distance (or "power"), from 0 to 100</summary>
        public double Distance
        {
            get { return Convert.ToDouble(GetValue(DistanceProperty)); }
            private set { SetValue(DistanceProperty, value); }
        }

        /// <summary>How often should be raised StickMove event in degrees</summary>
        public double AngleStep
        {
            get { return Convert.ToDouble(GetValue(AngleStepProperty)); }
            set
            {
                if (value < 1) value = 1; else if (value > 90) value = 90;
                SetValue(AngleStepProperty, Math.Round(value));
            }
        }

        /// <summary>How often should be raised StickMove event in distance units</summary>
        public double DistanceStep
        {
            get { return Convert.ToDouble(GetValue(DistanceStepProperty)); }
            set
            {
                if (value < 1) value = 1; else if (value > 50) value = 50;
                SetValue(DistanceStepProperty, value);
            }
        }

        /// <summary>Indicates whether the joystick knob resets its place after being released</summary>
        //public bool ResetKnobAfterRelease
        //{
        //    get { return Convert.ToBoolean(GetValue(ResetKnobAfterReleaseProperty)); }
        //    set { SetValue(ResetKnobAfterReleaseProperty, value); }
        //}

        /// <summary>Delegate holding data for joystick state change</summary>
        /// <param name="sender">The object that fired the event</param>
        /// <param name="args">Holds new values for angle and distance</param>
        public delegate void OnScreenJoystickEventHandler(OnScreenJoystick sender, VirtualJoystickEventArgs args);

        /// <summary>Delegate for joystick events that hold no data</summary>
        /// <param name="sender">The object that fired the event</param>
        public delegate void EmptyJoystickEventHandler(OnScreenJoystick sender);

        /// <summary>This event fires whenever the joystick moves</summary>
        public event OnScreenJoystickEventHandler Moved;

        /// <summary>This event fires once the joystick is released and its position is reset</summary>
        public event EmptyJoystickEventHandler Released;

        /// <summary>This event fires once the joystick is captured</summary>
        public event EmptyJoystickEventHandler Captured;

        private Point _startPos;
        private double _prevAngle, _prevDistance;
        private Direction _prevDir;
        private readonly Storyboard centerKnob;

        public OnScreenJoystick()
        {
            InitializeComponent();

            Knob.PreviewTouchDown += Knob_PreviewTouchDown;
            Knob.PreviewTouchUp += Knob_PreviewTouchUp;
            Knob.MouseLeftButtonDown += Knob_MouseLeftButtonDown;
            Knob.MouseLeftButtonUp += Knob_MouseLeftButtonUp;
            Knob.MouseMove += Knob_MouseMove;

            DesiredMaxDistance = base_ellipse.Height / 2;

            centerKnob = Knob.Resources["CenterKnob"] as Storyboard;
        }

        private void Knob_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            e.TouchDevice.Capture(this);
        }

        private void Knob_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            ReleaseAllTouchCaptures();
        }

        private void Knob_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPos = e.GetPosition(Base);
            _prevAngle = _prevDistance = 0;

            Captured?.Invoke(this);
            Knob.CaptureMouse();

            centerKnob.Stop();
        }

        private void Knob_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Knob.IsMouseCaptured) return;

            Point newPos = e.GetPosition(Base);

            Point deltaPos = new Point(newPos.X - _startPos.X, newPos.Y - _startPos.Y);

            double angle = Math.Atan2(deltaPos.Y, deltaPos.X) * 180 / Math.PI;
            if (angle > 0)
                angle += 90;
            else
            {
                angle = 270 + (180 + angle);
                if (angle >= 360) angle -= 360;
            }

            double distance = Math.Round(Math.Sqrt((deltaPos.X * deltaPos.X) + (deltaPos.Y * deltaPos.Y)) / 135 * 100);
            if (true)
            {
                Angle = angle;
                Distance = distance <= 100 ? distance : 100;

                if (distance <= 100)
                {
                    knobPosition.X = deltaPos.X;
                    knobPosition.Y = deltaPos.Y;
                }
                else
                {
                    Point nearestInclusivePoint = GetNearestInclusivePoint(newPos);
                    knobPosition.X = nearestInclusivePoint.X;
                    knobPosition.Y = nearestInclusivePoint.Y;
                }

                Console.WriteLine($"deltaPos.X {deltaPos.X}\t\t\t deltaPos.Y {deltaPos.Y} \t\t\tellipse.ActualWidth {base_ellipse.ActualWidth} \t\t ellipse.ActualHeight {base_ellipse.ActualHeight} \t");

                if (Moved == null ||
                    (!(Math.Abs(_prevAngle - angle) > AngleStep) && !(Math.Abs(_prevDistance - distance) > DistanceStep)))
                    return;

                //Additional code for deciding the Direction of the waypoint
                if (Angle > 337 || Angle <= 22)
                {
                    Dir = Direction.UP;
                }
                else if (Angle > 22 && Angle <= 68)
                {
                    Dir = Direction.UP_RIGHT;
                }
                else if (Angle > 68 && Angle <= 112)
                {
                    Dir = Direction.RIGHT;
                }
                else if (Angle > 112 && Angle <= 157)
                {
                    Dir = Direction.DOWN_RIGHT;
                }
                else if (Angle > 157 && Angle <= 202)
                {
                    Dir = Direction.DOWN;
                }
                else if (Angle > 202 && Angle <= 247)
                {
                    Dir = Direction.DOWN_LEFT;
                }
                else if (Angle > 247 && Angle <= 292)
                {
                    Dir = Direction.LEFT;
                }
                else if (Angle > 292 && Angle <= 337)
                {
                    Dir = Direction.UP_LEFT;
                }

                Moved?.Invoke(this, new VirtualJoystickEventArgs { Angle = Angle, Distance = Distance, Dir = Dir });
                _prevAngle = Angle;
                _prevDistance = Distance;
                _prevDir = Dir;
            }
        }

        private void Knob_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Knob.ReleaseMouseCapture();
            centerKnob.Begin();
        }

        private void centerKnob_Completed(object sender, EventArgs e)
        {
            Angle = Distance = _prevAngle = _prevDistance = 0;
            _prevDir = Direction.NONE;
            Released?.Invoke(this);
        }
        private Point GetNearestInclusivePoint(Point mouse_loc)
        {
            var rAngle = -GetAngleInRadian(mouse_loc);
            var nX = DesiredMaxDistance * Math.Cos(rAngle);
            var nY = DesiredMaxDistance * Math.Sin(rAngle);
            return new Point(nX, nY);
        }
        private double GetAngleInRadian(Point mouse_loc)
        {
            var xDiff = mouse_loc.X - _startPos.X;
            var yDiff = mouse_loc.Y - _startPos.Y;
            return -Math.Atan2(yDiff, xDiff);
        }
    }
}
