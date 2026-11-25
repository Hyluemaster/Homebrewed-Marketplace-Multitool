using Homebrewed_Marketplace_Multitool.Models;

namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public class Book : Item
    {
        public override string Type => "Book";
        public int OwnerId { get; set; }

        public Book(ulong id, string name, byte[]? content, int ownerId) : base(id, name, content)
        {
            OwnerId = ownerId;
        }
    }
}