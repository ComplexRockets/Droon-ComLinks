using UI.Xml;

namespace Assets.Scripts.DroonComLinks.Interfaces
{
    public interface IUIListItem
    {
        void AddTo(XmlElement parent, UIListItems.strDelegate OnInteracted);
        void Update();
        string GetId(XmlElement parent);
        string Id { get; }
    }
}