namespace Futbolio.API.Entities.Common
{
    public static class MatchStatus
    {
        public const int Scheduled = 0;   // Planlandı
        public const int Live = 1;        // Canlı
        public const int Completed = 2;   // Tamamlandı

        public static bool IsValid(int status) => status >= Scheduled && status <= Completed;
    }
}