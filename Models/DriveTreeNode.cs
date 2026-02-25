using System.Collections.ObjectModel;

namespace RswareDesign.Models;

public class DriveTreeNode
{
    public string Name { get; set; } = "";
    public string IconGlyph { get; set; } = "\uE8B7"; // Default folder icon
    public bool IsExpanded { get; set; }
    public ObservableCollection<DriveTreeNode> Children { get; set; } = new();
}
