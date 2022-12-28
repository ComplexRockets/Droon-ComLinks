using Assets.Scripts.DroonComLinks.Ui.ListItems;
using UI.Xml;

namespace Assets.Scripts.DroonComlinks.Ui
{
    public interface IUIListItem
    {
        void AddTo(XmlElement parent, UIListItems.strDelegate OnInteracted);
        void Update();
        string GetId(XmlElement parent);
        string Id { get; }
    }
}