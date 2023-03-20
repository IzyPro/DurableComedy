namespace ComedyBot
{
    public class JokeModel
    {
        public bool error { get; set; }
        public string category { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string setup { get; set; } = string.Empty;
        public string delivery { get; set; } = string.Empty;
        public string joke { get; set; } = string.Empty;
        public bool safe { get; set; }
        public int id { get; set; }
        public string lang { get; set; } = string.Empty;
    }
}
