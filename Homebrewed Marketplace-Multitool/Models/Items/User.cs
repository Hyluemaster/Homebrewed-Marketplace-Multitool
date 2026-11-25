namespace Homebrewed_Marketplace_Multitool.Models.Items
{
    public class User : Item
    {
        public override string Type => "User";
        public string Username { get; set; }
        public string? Group { get; set; }


        public User(ulong id, string name, string username, string? group) : base(id, name)
        {
            Username = $"@{username}";
            Group = group;
        }
    }
}