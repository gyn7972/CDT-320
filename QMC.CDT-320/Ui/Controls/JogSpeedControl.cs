using QMC.CDT320.Logging;
using QMC.Common.Motion;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QMC.CDT_320.Ui.Controls
{
    public partial class JogSpeedControl : UserControl
    {
        private int _speedPercent = 50;
        private bool _isDraggingSpeed;

        public int SpeedPercent
        {
            get
            {
                try
                {
                    return _speedPercent;
                }
                catch
                {
                    return 50;
                }
                finally
                {
                }
            }
            set
            {
                try
                {
                    _speedPercent = Math.Max(0, Math.Min(100, value));
                    lblSpeedValue.Text = _speedPercent + "%";
                    pnlSpeedSlider.Invalidate();
                }
                catch (Exception ex)
                {
                    EventLogger.Write(EventKind.Warning, "UI", "JOG-SPEED", "SpeedPercent set failed: " + ex.Message);
                }
                finally
                {
                }
            }
        }

        public JogSpeedControl()
        {
            try
            {
                InitializeComponent();
                lblSpeedValue.Text = _speedPercent + "%";
                pnlSpeedSlider.Invalidate();
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }

        public double GetCustomSpeed(BaseAxis axis)
        {
            try
            {
                if (axis == null)
                    return 1.0;

                double ratio = Math.Max(1, SpeedPercent) / 100.0;
                return Math.Max(0.1, axis.Config.JogCoarseVelocity * ratio);
            }
            catch
            {
                return 1.0;
            }
            finally
            {
            }
        }

        private void pnlSpeedSlider_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(BackColor);

                Rectangle bounds = pnlSpeedSlider.ClientRectangle;
                int knobSize = 30;
                int trackWidth = 15;
                int top = 9;
                int bottom = bounds.Height - 9;
                int trackX = Math.Max(0, (bounds.Width - trackWidth) / 2);
                Rectangle trackRect = new Rectangle(trackX, top, trackWidth, Math.Max(1, bottom - top));

                using (LinearGradientBrush brush = new LinearGradientBrush(trackRect, Color.Red, Color.RoyalBlue, LinearGradientMode.Vertical))
                {
                    ColorBlend blend = new ColorBlend();
                    blend.Positions = new[] { 0F, 0.5F, 1F };
                    blend.Colors = new[] { Color.Red, Color.Purple, Color.RoyalBlue };
                    brush.InterpolationColors = blend;
                    e.Graphics.FillRectangle(brush, trackRect);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(150, 150, 150)))
                    e.Graphics.DrawRectangle(borderPen, trackRect);

                int usableHeight = Math.Max(1, trackRect.Height);
                int centerY = trackRect.Bottom - (int)Math.Round((_speedPercent / 100.0) * usableHeight);
                Rectangle knobRect = new Rectangle(
                    trackRect.Left + (trackRect.Width / 2) - (knobSize / 2),
                    centerY - (knobSize / 2),
                    knobSize,
                    knobSize);

                using (LinearGradientBrush knobBrush = new LinearGradientBrush(knobRect, Color.White, Color.FromArgb(205, 205, 205), LinearGradientMode.Vertical))
                    e.Graphics.FillEllipse(knobBrush, knobRect);

                using (Pen knobPen = new Pen(Color.FromArgb(135, 135, 135)))
                    e.Graphics.DrawEllipse(knobPen, knobRect);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-SPEED", "Speed paint failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void pnlSpeedSlider_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                _isDraggingSpeed = true;
                SetSpeedFromMouse(e.Y);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-SPEED", "Speed mouse down failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void pnlSpeedSlider_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDraggingSpeed)
                    SetSpeedFromMouse(e.Y);
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-SPEED", "Speed mouse move failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void pnlSpeedSlider_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isDraggingSpeed)
                    SetSpeedFromMouse(e.Y);

                _isDraggingSpeed = false;
            }
            catch (Exception ex)
            {
                EventLogger.Write(EventKind.Warning, "UI", "JOG-SPEED", "Speed mouse up failed: " + ex.Message);
            }
            finally
            {
            }
        }

        private void pnlSpeedSlider_Resize(object sender, EventArgs e)
        {
            try
            {
                pnlSpeedSlider.Invalidate();
            }
            catch
            {
            }
            finally
            {
            }
        }

        private void SetSpeedFromMouse(int mouseY)
        {
            try
            {
                int top = 9;
                int bottom = pnlSpeedSlider.Height - 9;
                int usableHeight = Math.Max(1, bottom - top);
                int clampedY = Math.Max(top, Math.Min(bottom, mouseY));
                int percent = (int)Math.Round(((bottom - clampedY) / (double)usableHeight) * 100.0);

                SpeedPercent = percent;
            }
            catch
            {
                throw;
            }
            finally
            {
            }
        }
    }
}
