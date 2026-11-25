namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public class Book : Item
    {
        public override string Type => "Book";
        public int OwnerId { get; set; }
        public byte[]? Content { get; set; }

        public Book(ulong id, string name, byte[]? content, int ownerId) : base(id, name)
        {
            Content = content;
            OwnerId = ownerId;
        }
    }
}