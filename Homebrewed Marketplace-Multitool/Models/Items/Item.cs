namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public abstract class Item
    {
	    public int Id { get; set; }
	    public string Name { get; set; }
        public abstract string Type { get; }
        public object? Content { get; set; }

        public Item(int id, string name, object? content)
        {
            Id = id;
            Name = name;
            Content = content;
        }
    }
}