namespace Assets.Scripts.DroonComLinks.Interfaces
{
    public interface IDisplayable
    {
        string id { get; }
        IUIListItem[,] GetInfo();
    }
}