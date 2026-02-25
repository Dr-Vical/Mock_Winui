using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RswareDesign.Models;
using System;
using System.Collections.ObjectModel;

namespace RswareDesign.Controls;

public sealed partial class CompareParameterPanel : UserControl
{
    public string PanelId { get; private set; } = "";
    public event EventHandler<string>? CloseRequested;

    public CompareParameterPanel()
    {
        this.InitializeComponent();
    }

    public void Configure(string panelId, string panelLabel, SolidColorBrush accentBrush,
                          ObservableCollection<ParameterItem> parameters, string nodeName)
    {
        PanelId = panelId;
        PanelLabelText.Text = panelLabel;
        PanelLabelText.Foreground = accentBrush;
        NodeNameText.Text = nodeName;
        ParamListView.ItemsSource = parameters;
    }

    public void UpdateNodeName(string nodeName)
    {
        NodeNameText.Text = nodeName;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, PanelId);
    }
}
