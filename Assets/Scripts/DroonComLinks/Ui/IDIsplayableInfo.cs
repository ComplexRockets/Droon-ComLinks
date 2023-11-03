using ModApi.Ui.Inspector;

namespace Assets.Scripts.DroonComlinks.Ui
{
    public interface IDisplayable
    {
        string id { get; }
        IUIListItem[,] GetInfo();
        void CreateInfoPanel(InspectorModel inspectorModel);
    }
}