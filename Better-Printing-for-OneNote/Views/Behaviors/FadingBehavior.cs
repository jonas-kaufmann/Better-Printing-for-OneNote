using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using System.Windows.Media.Animation;

namespace Better_Printing_for_OneNote.Views.Behaviors
{
    public class FadingBehavior : Behavior<FrameworkElement>
    {
        public Duration AnimationDuration { get; set; }
        public Visibility InitialState { get; set; }

        DoubleAnimation FadeOut_Animation;
        DoubleAnimation FadeIn_Animation;

        protected override void OnAttached()
        {
            base.OnAttached();

            FadeIn_Animation = new DoubleAnimation(1, AnimationDuration, FillBehavior.HoldEnd);
            FadeOut_Animation = new DoubleAnimation(0, AnimationDuration, FillBehavior.HoldEnd);
            FadeOut_Animation.Completed += (sender, args) =>
            {
                if(AssociatedObject.Opacity == 0)
                    AssociatedObject.SetCurrentValue(Border.VisibilityProperty, Visibility.Collapsed);
            };

            AssociatedObject.SetCurrentValue(Border.VisibilityProperty,
                                             InitialState == Visibility.Collapsed
                                                ? Visibility.Collapsed
                                                : Visibility.Visible);

            Binding.AddTargetUpdatedHandler(AssociatedObject, Updated);
        }

        private bool _bindingInitilization = true;
        private void Updated(object sender, DataTransferEventArgs e)
        {
            if (_bindingInitilization)
                _bindingInitilization = false;
            else
            {
                var value = (Visibility)AssociatedObject.GetValue(Border.VisibilityProperty);
                switch (value)
                {
                    case Visibility.Collapsed:
                        AssociatedObject.SetCurrentValue(Border.VisibilityProperty, Visibility.Visible);
                        AssociatedObject.BeginAnimation(Border.OpacityProperty, FadeOut_Animation);
                        break;
                    case Visibility.Visible:
                        AssociatedObject.BeginAnimation(Border.OpacityProperty, FadeIn_Animation);
                        break;
                }
            }
        }
    }
}
