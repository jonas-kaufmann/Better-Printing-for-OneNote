using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Better_Printing_for_OneNote.Models;

namespace Better_Printing_for_OneNote.Views.Windows
{
    /// <summary>
    /// Interaction logic for ThirdPartyNoticesWindow.xaml
    /// </summary>
    public partial class ThirdPartyNoticesWindow : Window
    {
        public ObservableCollection<ThirdPartyNoticeModel> Notices
        {
            get => (ObservableCollection<ThirdPartyNoticeModel>)GetValue(NoticesProperty);
            set => SetValue(NoticesProperty, value);
        }
        public static DependencyProperty NoticesProperty = DependencyProperty.Register(nameof(Notices), typeof(ObservableCollection<ThirdPartyNoticeModel>), typeof(ThirdPartyNoticesWindow), new PropertyMetadata(null, NoticesProperty_Changed));
        private static void NoticesProperty_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != e.OldValue && sender is ThirdPartyNoticesWindow window)
            {
                if (e.OldValue != null)
                    window.UnsubscribeNoticesCollection((ObservableCollection<ThirdPartyNoticeModel>)e.OldValue);
                if (e.NewValue != null)
                    window.SubscribeNoticesCollection((ObservableCollection<ThirdPartyNoticeModel>)e.NewValue);

                window.RefreshNotices();
            }
        }

        private void SubscribeNoticesCollection(ObservableCollection<ThirdPartyNoticeModel> collection) => collection.CollectionChanged += Notices_CollectionChanged;

        private void UnsubscribeNoticesCollection(ObservableCollection<ThirdPartyNoticeModel> collection) => collection.CollectionChanged -= Notices_CollectionChanged;

        private void Notices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RefreshNotices();

        public ThirdPartyNoticesWindow()
        {
            InitializeComponent();
        }

        public void RefreshNotices()
        {
            MainSP.Children.Clear();

            foreach (var notice in Notices)
            {
                if (!string.IsNullOrWhiteSpace(notice.SoftwareName))
                {
                    TextBlock header = new TextBlock
                    {
                        Text = notice.SoftwareName,
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, MainSP.Children.Count > 0 ? 24 : 0, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    };
                    header.TextDecorations.Add(TextDecorations.Underline);

                    MainSP.Children.Add(header);

                    if (!string.IsNullOrWhiteSpace(notice.LicenseText))
                    {
                        TextBlock licenseText = new TextBlock
                        {
                            Text = notice.LicenseText,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 4, 0, 0)
                        };


                        MainSP.Children.Add(licenseText);
                    }
                }
            }
        }
    }
}
