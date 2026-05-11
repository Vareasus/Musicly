namespace Musicly.Models;

public class PrivateMessage
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    // Navigation properties
    public AppUser Sender { get; set; } = null!;
    public AppUser Receiver { get; set; } = null!;
}
