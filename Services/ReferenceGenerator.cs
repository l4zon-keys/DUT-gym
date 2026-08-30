namespace LoginFormASPCore6.Services
{
    // One shared human-readable reference format for anything that needs one
    // (payments, receipts, etc.) - {prefix}{yyMMdd}{6 random hex chars}.
    public static class ReferenceGenerator
    {
        private const string HexChars = "0123456789abcdef";

        public static string Generate(string prefix)
        {
            var datePart = DateTime.UtcNow.ToString("yyMMdd");
            var randomPart = new char[6];
            for (var i = 0; i < randomPart.Length; i++)
            {
                randomPart[i] = HexChars[Random.Shared.Next(HexChars.Length)];
            }
            return $"{prefix}{datePart}{new string(randomPart)}";
        }
    }
}
