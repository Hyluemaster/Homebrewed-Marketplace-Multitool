namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public abstract class Item
    {
	    public ulong Id { get; set; }
	    public string Name { get; set; }
        public abstract string Type { get; }

	    public Item(ulong id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}