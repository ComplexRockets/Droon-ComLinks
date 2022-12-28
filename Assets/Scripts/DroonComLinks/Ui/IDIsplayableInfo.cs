namespace Assets.Scripts.DroonComlinks.Ui
{
    public interface IDisplayable
    {
        string id { get; }
        IUIListItem[,] GetInfo();
    }
}