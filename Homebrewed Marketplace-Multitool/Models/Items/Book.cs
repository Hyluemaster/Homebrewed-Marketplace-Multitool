using Homebrewed_Marketplace_Multitool.Models;

namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public class Book : Item
    {
        public override string Type => "Book";
        public Book(int id, string name, object content) : base(id, name, content)
        {
            Id = id;
            Name = name;
            Content = content;
        }
    }
}