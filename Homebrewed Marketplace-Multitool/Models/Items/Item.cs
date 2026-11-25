namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public abstract class Item
    {
	    public ulong Id { get; set; }
	    public string Name { get; set; }
        public abstract string Type { get; }
        public byte[]? Content { get; set; }

        public Item(ulong id, string name, byte[]? content)
        {
            Id = id;
            Name = name;
            Content = content;
        }
    }
}