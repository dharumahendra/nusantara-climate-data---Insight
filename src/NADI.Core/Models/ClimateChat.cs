namespace NADI.Core.Models;

public class ClimateChat
{
    public int ChatId { get; set; }
    public string UserQuestion { get; set; } = string.Empty;
    public string AiResponse { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public void SendQuestion(string question)
    {
        UserQuestion = question;
        Timestamp = DateTime.UtcNow;
    }

    public void SaveChat(string aiResponse)
    {
        AiResponse = aiResponse;
    }
}
